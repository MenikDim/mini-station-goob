using System.Numerics;
using Content.Client.Resources;
using Content.Shared.Administration;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.Administration.UI.Bwoink;

/// <summary>
/// Admin rating widget in AHelp: stars + submit, or cooldown clock when daily limit is reached.
/// </summary>
public sealed class AHelpRatingPanel : BoxContainer
{
    public const string StarTexturePath = AdminHelpRatingPaths.StarIconPath;

    private const float StarScale = 2f;
    private const float StarSlotSize = 24f;

    private static readonly Color StarActiveColor = Color.White;
    private static readonly Color StarInactiveColor = Color.FromHex("#5A5868").WithAlpha(0.45f);

    private readonly Label _title;
    private readonly OptionButton _adminSelect;
    private readonly BoxContainer _ratingRow;
    private readonly BoxContainer _starsRow;
    private readonly TextureButton[] _starButtons = new TextureButton[5];
    private readonly Button _submitButton;
    private readonly BoxContainer _cooldownRow;
    private readonly SpinningClockControl _clock;
    private readonly Label _cooldownLabel;

    private readonly List<AdminHelpRatingParticipant> _participants = [];
    private int _selectedStars = 5;
    private bool _pendingSubmit;
    private DateTime _resetAtUtc = DateTime.UtcNow.Date.AddDays(1);

    public event Action? OnRequestState;
    public event Action<NetUserId, byte>? OnSubmit;

    public AHelpRatingPanel()
    {
        IoCManager.InjectDependencies(this);
        var cache = IoCManager.Resolve<IResourceCache>();
        var starTexture = cache.GetTexture(StarTexturePath);

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 3;
        Margin = new Thickness(0, 2, 0, 2);
        MaxHeight = 84;
        Visible = false;

        _title = new Label
        {
            StyleClasses = { "LabelSubText" },
            HorizontalAlignment = HAlignment.Center,
        };

        _adminSelect = new OptionButton
        {
            HorizontalExpand = true,
            Visible = false,
        };

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
                Scale = new Vector2(StarScale, StarScale),
                MinSize = new Vector2(StarSlotSize, StarSlotSize),
                MaxSize = new Vector2(StarSlotSize, StarSlotSize),
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
        };
        _submitButton.OnPressed += _ => TrySubmit();

        _ratingRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 3,
        };
        _ratingRow.AddChild(_starsRow);
        _ratingRow.AddChild(_submitButton);

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

        AddChild(_title);
        AddChild(_adminSelect);
        AddChild(_ratingRow);
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

        if (_pendingSubmit && state.RatingsToday > 0)
        {
            _pendingSubmit = false;
            if (_participants.Count == 0)
            {
                Visible = false;
                return;
            }
        }

        _pendingSubmit = false;

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
        _cooldownRow.Visible = false;
        _ratingRow.Visible = true;
        _title.Text = Loc.GetString("admin-help-rating-prompt-remaining",
            ("remaining", maxRatings - ratingsToday),
            ("max", maxRatings));
        _adminSelect.Visible = _participants.Count > 1;
        _adminSelect.Clear();

        for (var i = 0; i < _participants.Count; i++)
            _adminSelect.AddItem(_participants[i].DisplayName, i);

        if (_adminSelect.ItemCount > 0)
            _adminSelect.SelectId(0);

        SelectStars(_selectedStars);
        UpdateInteractable();
    }

    private void ShowCooldown()
    {
        Visible = true;
        _ratingRow.Visible = false;
        _adminSelect.Visible = false;
        _cooldownRow.Visible = true;
        _title.Text = Loc.GetString("admin-help-rating-limit-reached");
        UpdateCooldownLabel();
    }

    private void UpdateCooldownLabel()
    {
        var remaining = _resetAtUtc - DateTime.UtcNow;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        var hours = (int) remaining.TotalHours;
        var minutes = remaining.Minutes;
        _cooldownLabel.Text = Loc.GetString("admin-help-rating-cooldown", ("hours", hours), ("minutes", minutes));
    }

    private void SelectStars(int stars)
    {
        _selectedStars = Math.Clamp(stars, 1, 5);

        for (var i = 0; i < _starButtons.Length; i++)
            _starButtons[i].Modulate = i < _selectedStars ? StarActiveColor : StarInactiveColor;
    }

    private void TrySubmit()
    {
        if (_participants.Count == 0 || _selectedStars is < 1 or > 5)
            return;

        var participant = GetSelectedParticipant();
        if (participant == null)
            return;

        _pendingSubmit = true;
        OnSubmit?.Invoke(participant.UserId, (byte) _selectedStars);
    }

    private AdminHelpRatingParticipant? GetSelectedParticipant()
    {
        if (_participants.Count == 0)
            return null;

        if (!_adminSelect.Visible)
            return _participants[0];

        var selectedId = _adminSelect.SelectedId;
        if (selectedId < 0 || selectedId >= _participants.Count)
            return null;

        return _participants[selectedId];
    }

    private void UpdateInteractable()
    {
        var enabled = _participants.Count > 0;

        _submitButton.Disabled = !enabled;

        foreach (var button in _starButtons)
            button.Disabled = !enabled;
    }
}
