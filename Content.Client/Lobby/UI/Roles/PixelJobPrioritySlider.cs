using System.Numerics;
using Content.Client.UserInterface;
using Content.Client.Resources;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.ResourceManagement;
using Robust.Shared.Timing;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Content.Client.Lobby.UI.Roles;

/// <summary>
/// Discrete 4-step priority slider using custom pixel-art track and pointer sprites.
/// </summary>
public sealed class PixelJobPrioritySlider : BoxContainer
{
    private static readonly (string LocId, JobPriority Value)[] Priorities =
    [
        ("humanoid-profile-editor-job-priority-never-button", JobPriority.Never),
        ("humanoid-profile-editor-job-priority-low-button", JobPriority.Low),
        ("humanoid-profile-editor-job-priority-medium-button", JobPriority.Medium),
        ("humanoid-profile-editor-job-priority-high-button", JobPriority.High),
    ];

    private readonly Label _valueLabel;
    private readonly JobPriorityTrackControl _track;

    public event Action<int>? OnPriorityChanged;

    public int Selected => _track.SnappedValue;

    public PixelJobPrioritySlider()
    {
        Orientation = BoxContainer.LayoutOrientation.Horizontal;
        VerticalAlignment = VAlignment.Center;
        SeparationOverride = 8;

        _valueLabel = new Label
        {
            MinSize = new Vector2(76, 0),
            MaxSize = new Vector2(76, 0),
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Right,
            FontColorOverride = LobbyUiStyles.SubduedText,
            ClipText = true,
        };

        _track = new JobPriorityTrackControl();
        _track.OnSnapped += priority =>
        {
            UpdateLabel(priority);
            OnPriorityChanged?.Invoke(priority);
        };
        _track.OnPreview += UpdateLabel;

        AddChild(_valueLabel);
        AddChild(_track);

        UpdateLabel(0);
    }

    public void Select(int priority) => _track.SetValue(priority, animate: false);

    private void UpdateLabel(int priority)
    {
        priority = Math.Clamp(priority, 0, 3);
        var jobPriority = (JobPriority) priority;

        _valueLabel.Text = Loc.GetString(Priorities[priority].LocId);
        _valueLabel.FontColorOverride = priority == 0
            ? LobbyUiStyles.SubduedText
            : LobbyUiStyles.PriorityPointerModulate(jobPriority);
        _valueLabel.ToolTip = Loc.GetString(Priorities[priority].LocId);
    }
}

/// <summary>
/// Custom draggable track with smooth pointer movement and snap-on-release.
/// </summary>
public sealed class JobPriorityTrackControl : Control
{
    private const string TrackTexturePath = MiniSliderStyles.MarkedTrackPath;
    private const string PointerTexturePath = MiniSliderStyles.PointerPath;

    private const float NativeTrackWidth = MiniSliderStyles.NativeMarkedTrackWidth;
    private const float NativeTrackHeight = MiniSliderStyles.NativeTrackHeight;
    private const float NativePointerWidth = MiniSliderStyles.NativePointerWidth;
    private const float NativePointerHeight = MiniSliderStyles.NativePointerHeight;

    private const float Scale = MiniSliderStyles.LobbyScale;
    private const float SnapLerpSpeed = 16f;

    private static float ScaledTrackWidth => NativeTrackWidth * Scale;
    private static float ScaledTrackHeight => NativeTrackHeight * Scale;
    private static float ScaledPointerWidth => NativePointerWidth * Scale;
    private static float ScaledPointerHeight => NativePointerHeight * Scale;
    private static float ControlHeight => Math.Max(ScaledTrackHeight, ScaledPointerHeight);

    [Dependency] private readonly IResourceCache _cache = default!;

    private readonly LayoutContainer _layout;
    private readonly TextureRect _track;
    private readonly TextureRect _pointer;

    private Texture? _trackTexture;
    private Texture? _pointerTexture;

    private float _displayValue;
    private float _targetValue;
    private int _snappedValue;
    private bool _dragging;

    public event Action<int>? OnSnapped;
    public event Action<int>? OnPreview;

    public int SnappedValue => _snappedValue;

    public JobPriorityTrackControl()
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Stop;
        MinSize = new Vector2(ScaledTrackWidth, ControlHeight);

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

        LayoutContainer.SetAnchorPreset(_track, LayoutPreset.TopLeft);
        LayoutContainer.SetAnchorPreset(_pointer, LayoutPreset.TopLeft);

        var trackMarginTop = (ControlHeight - ScaledTrackHeight) / 2f;
        LayoutContainer.SetMarginTop(_track, trackMarginTop);

        var trackCenterY = trackMarginTop + ScaledTrackHeight / 2f;
        LayoutContainer.SetMarginTop(_pointer, trackCenterY - ScaledPointerHeight / 2f);

        _displayValue = 0;
        _targetValue = 0;
        _snappedValue = 0;

        _trackTexture = _cache.GetTexture(TrackTexturePath);
        _pointerTexture = _cache.GetTexture(PointerTexturePath);
        _track.Texture = _trackTexture;
        _pointer.Texture = _pointerTexture;

        UpdatePointerVisuals();
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _dragging = true;
        SetValueFromMouse(args.RelativePosition.X);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick || !_dragging)
            return;

        _dragging = false;
        SnapToNearest();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        if (!_dragging)
            return;

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

    public void SetValue(int priority, bool animate)
    {
        priority = Math.Clamp(priority, 0, 3);
        _snappedValue = priority;
        _targetValue = priority;

        if (animate)
        {
            OnPreview?.Invoke(priority);
            return;
        }

        _displayValue = priority;
        UpdatePointerVisuals();
        OnPreview?.Invoke(priority);
    }

    private void SetValueFromMouse(float mouseX)
    {
        var ratio = GetRatioFromMouse(mouseX);
        _displayValue = ratio * 3f;

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
        var snapped = (int) Math.Clamp(Math.Round(_displayValue), 0, 3);
        _targetValue = snapped;

        if (snapped != _snappedValue)
        {
            _snappedValue = snapped;
            OnSnapped?.Invoke(snapped);
        }

        OnPreview?.Invoke(snapped);
    }

    private void UpdatePointerVisuals()
    {
        var travel = GetThumbTravel();
        var ratio = Math.Clamp(_displayValue / 3f, 0f, 1f);
        var thumbLeft = travel * ratio;

        LayoutContainer.SetMarginLeft(_pointer, thumbLeft);

        var preview = (int) Math.Clamp(Math.Round(_displayValue), 0, 3);
        _pointer.ModulateSelfOverride = LobbyUiStyles.PriorityPointerModulate((JobPriority) preview);
    }
}
