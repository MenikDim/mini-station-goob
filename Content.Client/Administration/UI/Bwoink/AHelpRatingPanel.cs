using System.Linq;
using System.Numerics;
using Content.Client.Resources;
using Content.Shared.Administration;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.Administration.UI.Bwoink;

/// <summary>
/// Admin rating widget in AHelp: collapsible stars + submit, or cooldown when daily limit is reached.
/// </summary>
public sealed class AHelpRatingPanel : BoxContainer
{
    public const string StarTexturePath = AdminHelpRatingPaths.StarIconPath;

    private const float StarDisplaySize = 22f;

    private static readonly Color StarActiveColor = Color.White;
    private static readonly Color StarInactiveColor = Color.FromHex("#8A8898");

    private readonly Collapsible _collapsible;
    private readonly CollapsibleBody _ratingBody;
    private readonly Label _promptLabel;
    private readonly Label _remainingLabel;
    private readonly BoxContainer _adminRow;
    private readonly Label _adminLabel;
    private readonly OptionButton _adminSelect;
    private readonly BoxContainer _starsRow;
    private readonly TextureButton[] _starButtons = new TextureButton[5];
    private readonly Button _submitButton;
    private readonly BoxContainer _cooldownRow;
    private readonly SpinningClockControl _clock;
    private readonly Label _cooldownLabel;

    private readonly List<AdminHelpRatingParticipant> _participants = [];
    private NetUserId? _selectedAdminId;
    private int _selectedStars = 5;
    private DateTime _resetAtUtc = DateTime.UtcNow.Date.AddDays(1);

    public event Action? OnRequestState;
    public event Action<NetUserId, byte>? OnSubmit;

    public AHelpRatingPanel()
    {
        IoCManager.InjectDependencies(this);
        var cache = IoCManager.Resolve<IResourceCache>();
        var starTexture = cache.GetTexture(StarTexturePath);

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;
        Margin = new Thickness(0, 2, 0, 2);
        Visible = false;
        VerticalExpand = false;

        _promptLabel = new Label
        {
            Text = Loc.GetString("admin-help-rating-prompt"),
            HorizontalAlignment = HAlignment.Center,
            HorizontalExpand = true,
            StyleClasses = { "LabelSubText" },
        };

        _remainingLabel = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            HorizontalExpand = true,
            StyleClasses = { "LabelSubText" },
        };

        _adminLabel = new Label
        {
            Text = Loc.GetString("admin-help-rating-select-admin"),
            StyleClasses = { "LabelSubText" },
            VerticalAlignment = VAlignment.Center,
        };

        _adminSelect = new OptionButton
        {
            HorizontalExpand = true,
        };
        _adminSelect.OnItemSelected += args => OnAdminSelected(args.Id);

        _adminRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalExpand = true,
            Visible = false,
        };
        _adminRow.AddChild(_adminLabel);
        _adminRow.AddChild(_adminSelect);

        _starsRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 3,
            HorizontalAlignment = HAlignment.Center,
        };

        for (var i = 0; i < _starButtons.Length; i++)
        {
            var starIndex = i + 1;
            var button = new TextureButton
            {
                TextureNormal = starTexture,
                MinSize = new Vector2(StarDisplaySize, StarDisplaySize),
                VerticalAlignment = VAlignment.Center,
            };
            button.OnPressed += _ => SelectStars(starIndex);
            _starButtons[i] = button;
            _starsRow.AddChild(button);
        }

        _submitButton = new Button
        {
            Text = Loc.GetString("admin-help-rating-submit"),
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0),
        };
        _submitButton.OnPressed += _ => TrySubmit();

        _ratingBody = new CollapsibleBody();
        var ratingContent = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            Margin = new Thickness(4, 2, 4, 4),
        };
        ratingContent.AddChild(_promptLabel);
        ratingContent.AddChild(_remainingLabel);
        ratingContent.AddChild(_adminRow);
        ratingContent.AddChild(_starsRow);
        ratingContent.AddChild(_submitButton);
        _ratingBody.AddChild(ratingContent);

        var heading = new CollapsibleHeading(Loc.GetString("admin-help-rating-collapsible-title"));
        _collapsible = new Collapsible(heading, _ratingBody)
        {
            BodyVisible = true,
        };

        _clock = new SpinningClockControl();
        _cooldownLabel = new Label
        {
            StyleClasses = { "LabelSubText" },
            HorizontalAlignment = HAlignment.Left,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };

        _cooldownRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            HorizontalExpand = true,
            Visible = false,
        };
        _cooldownRow.AddChild(_clock);
        _cooldownRow.AddChild(_cooldownLabel);

        AddChild(_collapsible);
        AddChild(_cooldownRow);

        SelectStars(5);
    }

    public void RequestRefresh() => OnRequestState?.Invoke();

    public void UpdateState(AdminHelpRatingStateEvent state)
    {
        _participants.Clear();
        _participants.AddRange(state.Participants);
        _resetAtUtc = DateTime.UtcNow + state.TimeUntilReset;

        if (state.RatingsToday >= state.MaxRatingsPerDay)
        {
            ShowCooldown();
            return;
        }

        if (_participants.Count == 0)
        {
            Visible = false;
            return;
        }

        ShowRating(state.RatingsToday, state.MaxRatingsPerDay);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_cooldownRow.Visible)
            return;

        UpdateCooldownLabel();
    }

    private void ShowRating(int ratingsToday, int maxRatings)
    {
        Visible = true;
        _collapsible.Visible = true;
        _cooldownRow.Visible = false;
        _remainingLabel.Text = Loc.GetString("admin-help-rating-prompt-remaining",
            ("remaining", maxRatings - ratingsToday),
            ("max", maxRatings));

        RefreshAdminSelect();
        SelectStars(_selectedStars);
        UpdateInteractable();
    }

    private void ShowCooldown()
    {
        Visible = true;
        _collapsible.Visible = false;
        _cooldownRow.Visible = true;
        UpdateCooldownLabel();
    }

    private void RefreshAdminSelect()
    {
        _adminRow.Visible = _participants.Count > 1;
        _adminSelect.Clear();

        if (_participants.Count == 0)
            return;

        var selectedIndex = 0;

        if (_participants.Count == 1)
        {
            _selectedAdminId = _participants[0].UserId;
        }
        else if (_selectedAdminId == null || _participants.All(p => p.UserId != _selectedAdminId))
        {
            _selectedAdminId = _participants[0].UserId;
        }

        for (var i = 0; i < _participants.Count; i++)
        {
            _adminSelect.AddItem(_participants[i].DisplayName, i);
            if (_participants[i].UserId == _selectedAdminId)
                selectedIndex = i;
        }

        if (_adminSelect.ItemCount > 0)
            _adminSelect.SelectId(selectedIndex);
    }

    private void OnAdminSelected(int id)
    {
        if (id < 0 || id >= _participants.Count)
            return;

        _selectedAdminId = _participants[id].UserId;
    }

    private void UpdateCooldownLabel()
    {
        var remaining = _resetAtUtc - DateTime.UtcNow;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        var hours = (int) remaining.TotalHours;
        var minutes = remaining.Minutes;
        _cooldownLabel.Text = Loc.GetString("admin-help-rating-limit-reached") + " — "
            + Loc.GetString("admin-help-rating-cooldown", ("hours", hours), ("minutes", minutes));
    }

    private void SelectStars(int stars)
    {
        _selectedStars = Math.Clamp(stars, 1, 5);

        for (var i = 0; i < _starButtons.Length; i++)
            _starButtons[i].Modulate = i < _selectedStars ? StarActiveColor : StarInactiveColor;
    }

    private void TrySubmit()
    {
        var participant = GetSelectedParticipant();
        if (participant == null || _selectedStars is < 1 or > 5)
            return;

        OnSubmit?.Invoke(participant.UserId, (byte) _selectedStars);
    }

    private AdminHelpRatingParticipant? GetSelectedParticipant()
    {
        if (_participants.Count == 0)
            return null;

        if (_selectedAdminId != null)
        {
            foreach (var participant in _participants)
            {
                if (participant.UserId == _selectedAdminId)
                    return participant;
            }
        }

        return _participants[0];
    }

    private void UpdateInteractable()
    {
        var enabled = _participants.Count > 0;

        _submitButton.Disabled = !enabled;

        foreach (var button in _starButtons)
            button.Disabled = !enabled;
    }
}
