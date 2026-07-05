using System.Numerics;
using Content.Client.Lobby.UI;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Guidebook;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI.Roles;

/// <summary>
/// Compact antagonist preference row for the lobby list.
/// </summary>
public sealed class AntagPreferenceCard : PanelContainer
{
    private const float IconSize = 36f;
    private const float RowHeight = 58f;

    private static readonly Color RowBackground = Color.FromHex("#1e1a26").WithAlpha(0.65f);

    private readonly Label _title;
    private readonly AntagYesNoButtons _yesNoButtons;
    private readonly RoleLockIcon _lockIcon;
    private readonly Button _unlockButton;
    private readonly TextureButton _help;
    private readonly Button _loadoutButton;

    private List<ProtoId<GuideEntryPrototype>>? _guides;
    private Action? _onLoadout;
    private Action? _onUnlock;

    public event Action<int>? OnSelected;
    public event Action<List<ProtoId<GuideEntryPrototype>>>? OnOpenGuidebook;

    /// <summary>0 = yes, 1 = no.</summary>
    public int Selected => _yesNoButtons.Selected;

    public AntagPreferenceCard()
    {
        IoCManager.InjectDependencies(this);

        HorizontalExpand = true;
        MinSize = new Vector2(0, RowHeight);
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = RowBackground,
            BorderThickness = new Thickness(1),
            BorderColor = Color.FromHex("#3A3648").WithAlpha(0.5f),
            ContentMarginLeftOverride = 8,
            ContentMarginTopOverride = 4,
            ContentMarginRightOverride = 8,
            ContentMarginBottomOverride = 4,
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            VerticalExpand = true,
            HorizontalExpand = true,
        };
        AddChild(row);

        var iconHost = new Control
        {
            MinSize = new Vector2(IconSize, IconSize),
            MaxSize = new Vector2(IconSize, IconSize),
            VerticalAlignment = VAlignment.Center,
        };
        row.AddChild(iconHost);
        _imageSlot = iconHost;

        _title = new Label
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            ClipText = true,
            StyleClasses = { StyleNano.StyleClassLabelBig },
            FontColorOverride = LobbyUiStyles.HeaderText,
        };
        row.AddChild(_title);

        var controls = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            VerticalAlignment = VAlignment.Center,
        };
        row.AddChild(controls);

        _yesNoButtons = new AntagYesNoButtons();
        _yesNoButtons.OnSelected += value => OnSelected?.Invoke(value);
        controls.AddChild(_yesNoButtons);

        _lockIcon = new RoleLockIcon
        {
            Visible = false,
            VerticalAlignment = VAlignment.Center,
        };
        controls.AddChild(_lockIcon);

        _unlockButton = new Button
        {
            Visible = false,
            VerticalAlignment = VAlignment.Center,
        };
        _unlockButton.OnPressed += _ => _onUnlock?.Invoke();
        controls.AddChild(_unlockButton);

        _loadoutButton = new Button
        {
            Text = Loc.GetString("loadout-window"),
            Visible = false,
            VerticalAlignment = VAlignment.Center,
            MinWidth = 88,
            MinHeight = 26,
        };
        _loadoutButton.OnPressed += _ => _onLoadout?.Invoke();
        controls.AddChild(_loadoutButton);

        _help = new TextureButton
        {
            StyleClasses = { "HelpButton" },
            Visible = false,
            VerticalAlignment = VAlignment.Center,
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
            Children = { _help },
        };
        controls.AddChild(helpWrapper);
    }

    private readonly Control _imageSlot;

    public void Setup(
        string title,
        string? description,
        Texture? icon = null,
        List<ProtoId<GuideEntryPrototype>>? guides = null,
        Action? onLoadout = null,
        bool loadoutAvailable = false)
    {
        _title.Text = title;
        _title.ToolTip = description;
        ToolTip = description;
        _guides = guides;
        _help.Visible = guides != null;

        _onLoadout = onLoadout;
        _loadoutButton.Disabled = !loadoutAvailable;
        _loadoutButton.Visible = loadoutAvailable;

        _imageSlot.RemoveAllChildren();
        if (icon != null)
        {
            _imageSlot.AddChild(new TextureRect
            {
                Texture = icon,
                MinSize = new Vector2(IconSize, IconSize),
                MaxSize = new Vector2(IconSize, IconSize),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
            });
        }
    }

    public void LockRequirements(FormattedMessage requirements)
    {
        _lockIcon.SetRequirements(requirements);
        _lockIcon.Visible = true;
        _yesNoButtons.Visible = false;
    }

    public void ShowUnlock(int cost, Action onUnlock)
    {
        _onUnlock = onUnlock;
        _unlockButton.RemoveAllChildren();
        _unlockButton.AddChild(MiniCoinUi.CreateUnlockPriceRow(
            IoCManager.Resolve<IResourceCache>(),
            cost,
            "antag-unlock-button-prefix"));
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
        _yesNoButtons.Visible = true;
        HideUnlock();
    }

    /// <param name="id">0 = yes, 1 = no.</param>
    public void Select(int id) => _yesNoButtons.Select(id);
}

/// <summary>
/// Semi-transparent colored yes/no buttons for antagonist preferences.
/// </summary>
public sealed class AntagYesNoButtons : BoxContainer
{
    private const float ButtonWidth = 56f;
    private const float ButtonHeight = 30f;

    private static readonly Color YesColor = Color.FromHex("#4A8F62");
    private static readonly Color NoColor = Color.FromHex("#A84848");
    private static readonly Color ActiveText = Color.FromHex("#F4FFF6");
    private static readonly Color InactiveText = Color.FromHex("#C8C4D8");

    private readonly Button _yesButton;
    private readonly Button _noButton;

    private int _selected;

    public event Action<int>? OnSelected;

    /// <summary>0 = yes, 1 = no.</summary>
    public int Selected => _selected;

    public AntagYesNoButtons()
    {
        Orientation = LayoutOrientation.Horizontal;
        SeparationOverride = 4;
        VerticalAlignment = VAlignment.Center;

        _yesButton = CreateButton(Loc.GetString("humanoid-profile-editor-antag-preference-yes-button"));
        _yesButton.OnPressed += _ => Select(0, notify: true);

        _noButton = CreateButton(Loc.GetString("humanoid-profile-editor-antag-preference-no-button"));
        _noButton.OnPressed += _ => Select(1, notify: true);

        AddChild(_yesButton);
        AddChild(_noButton);

        Select(1, notify: false);
    }

    public void Select(int value, bool notify = false)
    {
        _selected = Math.Clamp(value, 0, 1);
        UpdateVisuals();

        if (notify && OnSelected != null)
            OnSelected.Invoke(_selected);
    }

    private static Button CreateButton(string text)
    {
        return new Button
        {
            Text = text,
            MinWidth = ButtonWidth,
            MaxWidth = ButtonWidth,
            MinHeight = ButtonHeight,
            MaxHeight = ButtonHeight,
            StyleClasses = { StyleNano.StyleClassLabelSmall },
        };
    }

    private void UpdateVisuals()
    {
        ApplyButtonStyle(_yesButton, YesColor, _selected == 0);
        ApplyButtonStyle(_noButton, NoColor, _selected == 1);
    }

    private static void ApplyButtonStyle(Button button, Color color, bool active)
    {
        button.ModulateSelfOverride = Color.White;
        button.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = color.WithAlpha(active ? 0.72f : 0.28f),
            BorderThickness = new Thickness(active ? 1 : 0),
            BorderColor = color.WithAlpha(active ? 0.95f : 0f),
            ContentMarginLeftOverride = 6,
            ContentMarginRightOverride = 6,
            ContentMarginTopOverride = 2,
            ContentMarginBottomOverride = 2,
        };
        button.Label.FontColorOverride = active ? ActiveText : InactiveText;
    }
}
