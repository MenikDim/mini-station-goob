using System.Numerics;
using Content.Client.Lobby.UI;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Guidebook;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI.Roles;

/// <summary>
/// Compact job row with a pixel-style priority slider.
/// </summary>
public sealed class JobPrioritySelector : BoxContainer
{
    // Job RSI icons are 8x8; scale 3.75 → 30px on screen.
    private const float JobIconScale = 3f;
    private const int JobIconBoxSize = 30;
    private const int JobRowHeight = 38;

    private readonly Label _title;
    private readonly PixelJobPrioritySlider _prioritySlider;
    private readonly RoleLockIcon _lockIcon;
    private readonly Button _unlockButton;
    private readonly TextureButton _help;
    private readonly Button _loadoutButton;

    private List<ProtoId<GuideEntryPrototype>>? _guides;
    private Action? _onLoadout;
    private Action? _onUnlock;

    public event Action<int>? OnSelected;
    public event Action<List<ProtoId<GuideEntryPrototype>>>? OnOpenGuidebook;

    public int Selected => _prioritySlider.Selected;

    public JobPrioritySelector()
    {
        IoCManager.InjectDependencies(this);
        var cache = IoCManager.Resolve<IResourceCache>();

        Orientation = LayoutOrientation.Horizontal;
        HorizontalExpand = true;
        MinSize = new Vector2(0, JobRowHeight);
        Margin = new Thickness(0, 0, 0, 0);
        SeparationOverride = 6;

        _title = new Label
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(4, 0, 10, 0),
            MouseFilter = MouseFilterMode.Stop,
            StyleClasses = { StyleNano.StyleClassLabelBig },
            FontColorOverride = LobbyUiStyles.HeaderText,
        };

        _prioritySlider = new PixelJobPrioritySlider
        {
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };
        _prioritySlider.OnPriorityChanged += priority => OnSelected?.Invoke(priority);

        _lockIcon = new RoleLockIcon
        {
            Visible = false,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };

        _unlockButton = new Button
        {
            Visible = false,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };
        _unlockButton.OnPressed += _ => _onUnlock?.Invoke();
        _unlockButton.AddChild(MiniCoinUi.CreateUnlockPriceRow(cache, 0, "job-unlock-button-prefix"));

        _help = new TextureButton
        {
            StyleClasses = { "HelpButton" },
        };
        _help.OnPressed += _ =>
        {
            if (_guides != null)
                OnOpenGuidebook?.Invoke(_guides);
        };

        var helpWrapper = new Control
        {
            MinSize = new Vector2(21, 21),
            MaxSize = new Vector2(21, 21),
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
            Children = { _help },
        };

        _loadoutButton = new Button
        {
            Text = Loc.GetString("loadout-window"),
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
        };
        _loadoutButton.OnPressed += _ => _onLoadout?.Invoke();

        AddChild(_title);
        AddChild(_prioritySlider);
        AddChild(_unlockButton);
        AddChild(_lockIcon);
        AddChild(helpWrapper);
        AddChild(_loadoutButton);
    }

    public void Setup(
        string title,
        string? description,
        TextureRect? icon = null,
        List<ProtoId<GuideEntryPrototype>>? guides = null,
        Action? onLoadout = null,
        bool loadoutAvailable = true)
    {
        _title.Text = title;
        _title.ToolTip = description;
        _guides = guides;
        _help.Visible = guides != null;

        _onLoadout = onLoadout;
        _loadoutButton.Disabled = !loadoutAvailable;
        _loadoutButton.Visible = loadoutAvailable;

        if (icon != null)
        {
            icon.TextureScale = new Vector2(JobIconScale, JobIconScale);
            icon.Stretch = TextureRect.StretchMode.KeepCentered;
            icon.CanShrink = false;
            icon.MinSize = new Vector2(JobIconBoxSize, JobIconBoxSize);
            icon.VerticalAlignment = VAlignment.Center;
            icon.Margin = new Thickness(0, 0, 8, 0);
            AddChild(icon);
            icon.SetPositionFirst();
        }
    }

    public void LockRequirements(FormattedMessage requirements)
    {
        _lockIcon.SetRequirements(requirements);
        _lockIcon.Visible = true;
        _prioritySlider.Visible = false;
    }

    public void ShowUnlock(int cost, Action onUnlock)
    {
        _onUnlock = onUnlock;
        _unlockButton.RemoveAllChildren();
        _unlockButton.AddChild(MiniCoinUi.CreateUnlockPriceRow(
            IoCManager.Resolve<IResourceCache>(),
            cost,
            "job-unlock-button-prefix"));
        _unlockButton.Visible = true;
    }

    public void HideUnlock()
    {
        _onUnlock = null;
        _unlockButton.Visible = false;
    }

    public void UnlockRequirements()
    {
        _lockIcon.Visible = false;
        _prioritySlider.Visible = true;
        HideUnlock();
    }

    public void Select(int id) => _prioritySlider.Select(id);
}
