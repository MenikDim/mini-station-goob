using System.Numerics;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Shared.Guidebook;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Content.Client.Lobby.UI.Roles;

/// <summary>
/// Antagonist preference card for the lobby grid (styled like the antag token shop).
/// </summary>
public sealed class AntagPreferenceCard : PanelContainer
{
    private static readonly Color CardBackgroundColor = Color.FromHex("#1e1a26").WithAlpha(0.8f);

    private const float CardIconSize = 52f;
    private const float CardMinWidth = 96f;
    private const float TitleMaxHeight = 28f;

    private readonly RichTextLabel _title;
    private readonly PixelAntagToggle _toggle;
    private readonly RoleLockIcon _lockIcon;
    private readonly Button _unlockButton;
    private readonly TextureButton _help;
    private readonly Button _loadoutButton;
    private readonly Control _imageSlot;

    private List<ProtoId<GuideEntryPrototype>>? _guides;
    private Action? _onLoadout;
    private Action? _onUnlock;

    public event Action<int>? OnSelected;
    public event Action<List<ProtoId<GuideEntryPrototype>>>? OnOpenGuidebook;

    /// <summary>0 = yes, 1 = no.</summary>
    public int Selected => _toggle.Selected;

    public AntagPreferenceCard()
    {
        IoCManager.InjectDependencies(this);

        HorizontalExpand = true;
        VerticalExpand = true;
        MinSize = new Vector2(CardMinWidth, 160);
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = CardBackgroundColor,
            BorderThickness = new Thickness(1),
            ContentMarginLeftOverride = 8,
            ContentMarginTopOverride = 8,
            ContentMarginRightOverride = 8,
            ContentMarginBottomOverride = 8,
        };

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
            VerticalExpand = true,
            HorizontalExpand = true,
        };
        AddChild(root);

        var header = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
        };
        root.AddChild(header);

        header.AddChild(new Control { HorizontalExpand = true });

        _help = new TextureButton
        {
            StyleClasses = { "HelpButton" },
            Visible = false,
        };
        _help.OnPressed += _ =>
        {
            if (_guides != null)
                OnOpenGuidebook?.Invoke(_guides);
        };

        header.AddChild(new Control
        {
            MinSize = new Vector2(21, 21),
            MaxSize = new Vector2(21, 21),
            Children = { _help },
        });

        var imageBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalAlignment = HAlignment.Center,
            MinSize = new Vector2(0, CardIconSize + 4),
        };
        root.AddChild(imageBox);

        _imageSlot = new Control
        {
            MinSize = new Vector2(CardIconSize, CardIconSize),
            MaxSize = new Vector2(CardIconSize, CardIconSize),
            HorizontalAlignment = HAlignment.Center,
        };
        imageBox.AddChild(_imageSlot);

        _title = new RichTextLabel
        {
            HorizontalAlignment = HAlignment.Center,
            HorizontalExpand = true,
            MaxHeight = TitleMaxHeight,
            RectClipContent = true,
            StyleClasses = { StyleNano.StyleClassLabelSubText },
        };
        root.AddChild(_title);

        var actions = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
            HorizontalAlignment = HAlignment.Center,
            MinSize = new Vector2(0, 64),
        };
        root.AddChild(actions);

        var toggleHost = new CenterContainer
        {
            MinSize = new Vector2(0, 28),
            HorizontalAlignment = HAlignment.Center,
        };
        actions.AddChild(toggleHost);

        _toggle = new PixelAntagToggle();
        _toggle.OnSelected += value => OnSelected?.Invoke(value);
        toggleHost.AddChild(_toggle);

        _lockIcon = new RoleLockIcon
        {
            Visible = false,
        };
        toggleHost.AddChild(_lockIcon);

        _unlockButton = new Button
        {
            Visible = false,
            HorizontalAlignment = HAlignment.Center,
        };
        _unlockButton.OnPressed += _ => _onUnlock?.Invoke();
        actions.AddChild(_unlockButton);

        _loadoutButton = new Button
        {
            Text = Loc.GetString("loadout-window"),
            Visible = false,
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
        };
        _loadoutButton.OnPressed += _ => _onLoadout?.Invoke();
        root.AddChild(_loadoutButton);
    }

    public void Setup(
        string title,
        string? description,
        Texture? icon = null,
        List<ProtoId<GuideEntryPrototype>>? guides = null,
        Action? onLoadout = null,
        bool loadoutAvailable = false)
    {
        _title.SetMessage(title, defaultColor: LobbyUiStyles.HeaderText);
        _title.ToolTip = title;
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
                MinSize = new Vector2(CardIconSize, CardIconSize),
                MaxSize = new Vector2(CardIconSize, CardIconSize),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
            });
        }
    }

    public void LockRequirements(FormattedMessage requirements)
    {
        _lockIcon.SetRequirements(requirements);
        _lockIcon.Visible = true;
        _toggle.Visible = false;
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
        _toggle.Visible = true;
        HideUnlock();
    }

    /// <param name="id">0 = yes, 1 = no.</param>
    public void Select(int id) => _toggle.Select(id);
}

/// <summary>
/// Two-position yes/no toggle using a white scroll track and colored pointer.
/// </summary>
public sealed class PixelAntagToggle : Control
{
    private const string TrackTexturePath = MiniSliderStyles.WhiteTrackPath;
    private const string PointerTexturePath = MiniSliderStyles.PointerPath;

    private const float NativeTrackWidth = MiniSliderStyles.NativeWhiteTrackWidth;
    private const float NativeTrackHeight = MiniSliderStyles.NativeTrackHeight;
    private const float NativePointerWidth = MiniSliderStyles.NativePointerWidth;
    private const float NativePointerHeight = MiniSliderStyles.NativePointerHeight;

    private const float Scale = 2.5f * 1.1f;
    private const float ClickToggleThreshold = 4f;
    private const float SnapLerpSpeed = 18f;

    private static readonly Color TrackColor = Color.FromHex("#B5B3BD");
    private static readonly Color YesPointerColor = Color.FromHex("#5CBF6A");
    private static readonly Color NoPointerColor = Color.FromHex("#E04848");

    private static float ScaledTrackWidth => NativeTrackWidth * Scale;
    private static float ScaledTrackHeight => NativeTrackHeight * Scale;
    private static float ScaledPointerWidth => NativePointerWidth * Scale;
    private static float ScaledPointerHeight => NativePointerHeight * Scale;
    private static float ControlHeight => Math.Max(ScaledTrackHeight, ScaledPointerHeight);

    [Dependency] private readonly IResourceCache _cache = default!;

    private readonly LayoutContainer _layout;
    private readonly TextureRect _track;
    private readonly TextureRect _pointer;

    private float _displayValue;
    private float _targetValue;
    private int _selected;
    private bool _dragging;
    private float _dragStartX;
    private bool _movedWhileDragging;

    public event Action<int>? OnSelected;
    public event Action<int>? OnPreview;

    public int Selected => _selected;

    public PixelAntagToggle()
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Stop;
        MinSize = new Vector2(ScaledTrackWidth, ControlHeight);
        MaxSize = new Vector2(ScaledTrackWidth, ControlHeight);

        _layout = new LayoutContainer
        {
            MinSize = MinSize,
        };

        _track = new TextureRect
        {
            Stretch = TextureRect.StretchMode.Keep,
            TextureScale = new Vector2(Scale, Scale),
        };

        _pointer = new TextureRect
        {
            Stretch = TextureRect.StretchMode.Keep,
            TextureScale = new Vector2(Scale, Scale),
        };

        _layout.AddChild(_track);
        _layout.AddChild(_pointer);
        AddChild(_layout);

        SetAnchorPreset(_track, LayoutPreset.TopLeft);
        SetAnchorPreset(_pointer, LayoutPreset.TopLeft);

        var trackMarginTop = (ControlHeight - ScaledTrackHeight) / 2f;
        SetMarginTop(_track, trackMarginTop);

        var trackCenterY = trackMarginTop + ScaledTrackHeight / 2f;
        SetMarginTop(_pointer, trackCenterY - ScaledPointerHeight / 2f);

        _track.Texture = _cache.GetTexture(TrackTexturePath);
        _track.ModulateSelfOverride = TrackColor;
        _pointer.Texture = _cache.GetTexture(PointerTexturePath);

        UpdatePointerVisuals();
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _dragging = true;
        _dragStartX = args.RelativePosition.X;
        _movedWhileDragging = false;
        SetValueFromMouse(args.RelativePosition.X);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick || !_dragging)
            return;

        _dragging = false;

        if (!_movedWhileDragging && Math.Abs(args.RelativePosition.X - _dragStartX) < ClickToggleThreshold)
        {
            Toggle();
            return;
        }

        SnapToNearest();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        if (!_dragging)
            return;

        if (Math.Abs(args.RelativePosition.X - _dragStartX) >= ClickToggleThreshold)
            _movedWhileDragging = true;

        SetValueFromMouse(args.RelativePosition.X);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_dragging)
            return;

        if (Math.Abs(_displayValue - _targetValue) < 0.001f)
            return;

        _displayValue = MathHelper.Lerp(_displayValue, _targetValue, args.DeltaSeconds * SnapLerpSpeed);

        if (Math.Abs(_displayValue - _targetValue) < 0.01f)
            _displayValue = _targetValue;

        UpdatePointerVisuals();
        OnPreview?.Invoke((int) Math.Round(_displayValue));
    }

    public void Select(int value, bool animate = false)
    {
        value = Math.Clamp(value, 0, 1);
        _selected = value;
        _targetValue = value;

        if (animate)
        {
            OnPreview?.Invoke(value);
            return;
        }

        _displayValue = value;
        UpdatePointerVisuals();
        OnPreview?.Invoke(value);
    }

    private void SetValueFromMouse(float mouseX)
    {
        var ratio = GetRatioFromMouse(mouseX);
        _displayValue = ratio;

        UpdatePointerVisuals();
        OnPreview?.Invoke((int) Math.Round(_displayValue));
    }

    private float GetRatioFromMouse(float mouseX)
    {
        var travel = GetThumbTravel();
        if (travel <= 0f)
            return 0f;

        var thumbLeft = mouseX - ScaledPointerWidth / 2f;
        return Math.Clamp(thumbLeft / travel, 0f, 1f);
    }

    private float GetThumbTravel() =>
        Math.Max(0f, ScaledTrackWidth - ScaledPointerWidth);

    private void SnapToNearest()
    {
        var snapped = (int) Math.Clamp(Math.Round(_displayValue), 0, 1);
        _targetValue = snapped;

        if (snapped != _selected)
        {
            _selected = snapped;
            OnSelected?.Invoke(snapped);
        }

        OnPreview?.Invoke(snapped);
    }

    private void Toggle()
    {
        var next = _selected == 0 ? 1 : 0;
        Select(next, animate: true);
        OnSelected?.Invoke(next);
    }

    private void UpdatePointerVisuals()
    {
        var travel = GetThumbTravel();
        var ratio = Math.Clamp(_displayValue, 0f, 1f);
        var thumbLeft = travel * ratio;

        SetMarginLeft(_pointer, thumbLeft);

        var preview = (int) Math.Clamp(Math.Round(_displayValue), 0, 1);
        _pointer.ModulateSelfOverride = preview == 0 ? YesPointerColor : NoPointerColor;
    }
}
