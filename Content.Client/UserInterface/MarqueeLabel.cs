using System;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface;

/// <summary>
/// Single-line label that scrolls horizontally when text overflows its bounds.
/// </summary>
public sealed class MarqueeLabel : Control
{
    private const float ScrollSpeed = 32f;
    private const float LoopGap = 24f;

    private readonly BoxContainer _scrollRow;
    private readonly Label _labelA;
    private readonly Label _labelB;
    private readonly Control _loopGap;
    private float _scrollPos;
    private float _loopLength;
    private bool _scrolling;
    private string _text = string.Empty;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
                return;

            _text = value;
            _labelA.Text = value;
            _labelB.Text = value;
            ResetScroll();
            InvalidateMeasure();
        }
    }

    public new Color Modulate
    {
        get => _labelA.Modulate;
        set
        {
            _labelA.Modulate = value;
            _labelB.Modulate = value;
        }
    }

    public MarqueeLabel()
    {
        RectClipContent = true;
        MinSize = new Vector2(0, 18);
        MouseFilter = MouseFilterMode.Ignore;

        _scrollRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 0,
            VerticalAlignment = VAlignment.Center,
        };

        _labelA = CreateLabel();
        _loopGap = new Control
        {
            MinSize = new Vector2(LoopGap, 0),
            MouseFilter = MouseFilterMode.Ignore,
        };
        _labelB = CreateLabel();

        _scrollRow.AddChild(_labelA);
        _scrollRow.AddChild(_loopGap);
        _scrollRow.AddChild(_labelB);
        AddChild(_scrollRow);
    }

    public void SetStyleClass(string styleClass)
    {
        _labelA.StyleClasses.Clear();
        _labelA.StyleClasses.Add(styleClass);
        _labelB.StyleClasses.Clear();
        _labelB.StyleClasses.Add(styleClass);
        InvalidateMeasure();
    }

    private static Label CreateLabel()
    {
        return new Label
        {
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Center,
            ClipText = false,
        };
    }

    private void ResetScroll()
    {
        _scrollPos = 0f;
        _loopLength = 0f;
        _scrolling = false;
        _scrollRow.Margin = default;
        _labelB.Visible = false;
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        _labelA.Measure(new Vector2(float.PositiveInfinity, float.PositiveInfinity));
        var labelSize = _labelA.DesiredSize;
        var height = Math.Max(MinSize.Y, labelSize.Y);

        // Expanding siblings in a horizontal box must not consume width during measure.
        if (HorizontalExpand)
            return new Vector2(0, height);

        var width = availableSize.X > 0 ? availableSize.X : labelSize.X;
        return new Vector2(width, height);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!IsInsideTree)
            return;

        var avail = PixelWidth;
        if (avail <= 0)
            avail = (int) MathF.Ceiling(Width);
        if (avail <= 0)
            return;

        _labelA.Measure(new Vector2(float.PositiveInfinity, float.PositiveInfinity));
        var textWidth = _labelA.DesiredPixelSize.X;
        var overflow = textWidth - avail;

        if (overflow <= 0)
        {
            ResetScroll();
            return;
        }

        if (!_scrolling)
        {
            _scrolling = true;
            _labelB.Visible = true;
            _loopLength = textWidth + LoopGap;
        }

        _scrollPos += ScrollSpeed * args.DeltaSeconds;
        if (_scrollPos >= _loopLength)
            _scrollPos -= _loopLength;

        _scrollRow.Margin = new Thickness(-_scrollPos, 0, 0, 0);
    }
}
