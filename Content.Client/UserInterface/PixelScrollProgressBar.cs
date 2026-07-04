using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface;

/// <summary>
/// Pixel-art progress bar: black border always visible, yellow interior fills left-to-right.
/// Uses <see cref="MiniSliderStyles.LongPlainTrackPath"/>.
/// </summary>
public sealed class PixelScrollProgressBar : Control
{
    public const float Scale = 2.5f;

    public static float PreferredWidth => MiniSliderStyles.NativeLongPlainTrackWidth * Scale;

  /// <summary>Dim track behind the fill so empty interior reads as unfilled.</summary>
    public static readonly Color EmptyModulate = Color.FromHex("#252530");

    [Dependency] private readonly IResourceCache _cache = default!;

    private StyleBoxTexture _trackStyle = default!;
    private float _value;

    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (MathHelper.CloseToPercent(_value, clamped))
                return;

            _value = clamped;
        }
    }

    public PixelScrollProgressBar()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
        MinSize = new System.Numerics.Vector2(PreferredWidth, MiniSliderStyles.NativeTrackHeight * Scale);
        MaxSize = new System.Numerics.Vector2(PreferredWidth, MiniSliderStyles.NativeTrackHeight * Scale);

        var tex = _cache.GetTexture(MiniSliderStyles.GreenPlainTrackPath);
        _trackStyle = MiniSliderStyles.CreateLongTrackBox(tex, Scale);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var box = PixelSizeBox;
        if (box.Width <= 0 || box.Height <= 0)
            return;

        var empty = new StyleBoxTexture(_trackStyle) { Modulate = EmptyModulate };
        empty.Draw(handle, box, UIScale);

        if (_value <= 0f)
            return;

        var fillWidth = box.Width * _value;
        if (fillWidth < 1f)
            return;

        var fill = new StyleBoxTexture(_trackStyle) { Modulate = Color.White };
        fill.Draw(handle, UIBox2.FromDimensions(box.Left, box.Top, fillWidth, box.Height), UIScale);
    }

    protected override System.Numerics.Vector2 MeasureOverride(System.Numerics.Vector2 availableSize)
    {
        return new System.Numerics.Vector2(
            PreferredWidth,
            MiniSliderStyles.NativeTrackHeight * Scale);
    }
}
