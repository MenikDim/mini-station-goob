using System;
using System.Numerics;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface;
using Content.Shared._Mini.TypanWar;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client._Mini.TypanWar;

public sealed class TypanWarHudControl : PanelContainer
{
    public const float PreferredWidth = 580f;
    private const float BarWidth = 240f;

    private static readonly Color PanelBackground = Color.FromHex("#14141C").WithAlpha(0.72f);

    private readonly Label _titleLabel;
    private readonly Label _ntCountLabel;
    private readonly Label _typanCountLabel;
    private readonly Label _timerLabel;
    private readonly TypanWarScrollBarControl _bar;

    public TypanWarHudControl()
    {
        MinHeight = 42;
        MaxHeight = 42;
        HorizontalAlignment = HAlignment.Center;
        MouseFilter = MouseFilterMode.Ignore;
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = PanelBackground,
            BorderThickness = new Thickness(1),
            BorderColor = Color.FromHex("#6E6A82").WithAlpha(0.45f),
            ContentMarginLeftOverride = 12,
            ContentMarginRightOverride = 12,
            ContentMarginTopOverride = 6,
            ContentMarginBottomOverride = 6,
        };

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10,
            VerticalAlignment = VAlignment.Center,
        };
        AddChild(row);

        _titleLabel = new Label
        {
            FontColorOverride = Color.FromHex("#E8E6F0"),
            MinWidth = 88,
            MaxWidth = 88,
        };

        _ntCountLabel = new Label
        {
            FontColorOverride = Color.FromHex("#A8C8FF"),
            MinWidth = 56,
            MaxWidth = 56,
            Align = Label.AlignMode.Right,
        };

        _bar = new TypanWarScrollBarControl(BarWidth)
        {
            VerticalAlignment = VAlignment.Center,
        };

        _typanCountLabel = new Label
        {
            FontColorOverride = Color.FromHex("#FFB0B0"),
            MinWidth = 72,
            MaxWidth = 72,
        };

        _timerLabel = new Label
        {
            FontColorOverride = Color.FromHex("#F0D890"),
            MinWidth = 56,
            MaxWidth = 56,
            Align = Label.AlignMode.Right,
            Margin = new Thickness(6, 0, 0, 0),
        };

        row.AddChild(_titleLabel);
        row.AddChild(_ntCountLabel);
        row.AddChild(_bar);
        row.AddChild(_typanCountLabel);
        row.AddChild(_timerLabel);

        Visible = false;
    }

    public void Update(TypanWarPhase phase, int ntAlive, int typanAlive, float timeRemainingSeconds)
    {
        Visible = phase is TypanWarPhase.Pending or TypanWarPhase.Active or TypanWarPhase.Ended;
        if (!Visible)
            return;

        _titleLabel.Text = phase switch
        {
            TypanWarPhase.Pending => Loc.GetString("typan-war-hud-pending"),
            TypanWarPhase.Ended => Loc.GetString("typan-war-hud-ended"),
            _ => Loc.GetString("typan-war-hud-active"),
        };

        var ntLabel = Loc.GetString("typan-war-hud-nt");
        var syndicateLabel = Loc.GetString("typan-war-hud-typan");

        if (phase == TypanWarPhase.Pending)
        {
            _ntCountLabel.Text = ntLabel;
            _typanCountLabel.Text = syndicateLabel;
            _bar.SetCounts(1, 1);
        }
        else
        {
            _ntCountLabel.Text = ntLabel + " " + ntAlive;
            _typanCountLabel.Text = typanAlive + " " + syndicateLabel;
            _bar.SetCounts(ntAlive, typanAlive);
        }

        var span = TimeSpan.FromSeconds(Math.Max(0, timeRemainingSeconds));
        _timerLabel.Text = $"{(int) span.TotalMinutes:00}:{(int) (span.TotalSeconds % 60):00}";
    }
}

/// <summary>
/// Dual-faction bar using long_white_scroll_line.png.
/// </summary>
public sealed class TypanWarScrollBarControl : Control
{
    private const float BarScale = 2f;

    private static readonly Color NtColor = Color.FromHex("#4A7FD4").WithAlpha(0.85f);
    private static readonly Color TypanColor = Color.FromHex("#C84848").WithAlpha(0.85f);
    private static readonly Color EmptyModulate = Color.FromHex("#252530").WithAlpha(0.55f);

    [Dependency] private readonly IResourceCache _cache = default!;

    private readonly StyleBoxTexture _trackStyle;
    private readonly float _width;
    private int _ntCount = 1;
    private int _typanCount = 1;

    public TypanWarScrollBarControl(float width)
    {
        _width = width;
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
        MinHeight = MiniSliderStyles.NativeTrackHeight * BarScale;
        MaxHeight = MiniSliderStyles.NativeTrackHeight * BarScale;
        MinSize = new Vector2(_width, MiniSliderStyles.NativeTrackHeight * BarScale);
        MaxSize = MinSize;

        var tex = _cache.GetTexture(MiniSliderStyles.LongWhiteTrackPath);
        _trackStyle = MiniSliderStyles.CreateLongTrackBox(tex, BarScale);
    }

    public void SetCounts(int nt, int typan)
    {
        _ntCount = Math.Max(0, nt);
        _typanCount = Math.Max(0, typan);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var box = PixelSizeBox;
        if (box.Width <= 0 || box.Height <= 0)
            return;

        var empty = new StyleBoxTexture(_trackStyle) { Modulate = EmptyModulate };
        empty.Draw(handle, box, UIScale);

        var total = _ntCount + _typanCount;
        if (total <= 0)
            return;

        var ntWidth = box.Width * (_ntCount / (float) total);
        if (ntWidth > 0.5f)
        {
            var fill = new StyleBoxTexture(_trackStyle) { Modulate = NtColor };
            fill.Draw(handle, UIBox2.FromDimensions(box.Left, box.Top, ntWidth, box.Height), UIScale);
        }

        if (box.Width - ntWidth > 0.5f)
        {
            var fill = new StyleBoxTexture(_trackStyle) { Modulate = TypanColor };
            fill.Draw(handle, UIBox2.FromDimensions(box.Left + ntWidth, box.Top, box.Width - ntWidth, box.Height), UIScale);
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return new Vector2(_width, MiniSliderStyles.NativeTrackHeight * BarScale);
    }
}
