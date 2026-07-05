using System;
using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._Mini.TypanWar;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Content.Client._Mini.TypanWar;

[UsedImplicitly]
public sealed class TypanWarHudController : UIController,
    IOnStateEntered<GameplayState>,
    IOnStateExited<GameplayState>,
    IOnSystemChanged<TypanWarUiSystem>
{
    private const float HudWidth = 520f;
    /// <summary>Offset above the hands / item status hotbar row.</summary>
    private const float HudBottomMargin = 168f;

    [UISystemDependency] private readonly TypanWarUiSystem _war = default!;

    private TypanWarHudControl? _hud;

    public void OnStateEntered(GameplayState state)
    {
        _hud = new TypanWarHudControl();

        if (UIManager.ActiveScreen != null)
        {
            UIManager.ActiveScreen.AddChild(_hud);
            SetAnchorPreset(_hud, LayoutPreset.CenterBottom);
            SetMarginBottom(_hud, HudBottomMargin);
            _hud.MinWidth = HudWidth;
            _hud.MaxWidth = HudWidth;
            _hud.SetPositionLast();
        }

        Refresh();
    }

    public void OnStateExited(GameplayState state)
    {
        _hud?.Orphan();
        _hud = null;
    }

    public void OnSystemLoaded(TypanWarUiSystem system)
    {
        system.StatusUpdated += Refresh;
        Refresh();
    }

    public void OnSystemUnloaded(TypanWarUiSystem system)
    {
        system.StatusUpdated -= Refresh;
    }

    private void Refresh()
    {
        if (_hud == null)
            return;

        _hud.Update(
            _war.Phase,
            _war.NtAlive,
            _war.TypanAlive,
            _war.TimeRemainingSeconds);
    }
}

public sealed class TypanWarHudControl : PanelContainer
{
    private static readonly Color PanelBackground = Color.FromHex("#14141C").WithAlpha(0.42f);

    private readonly Label _titleLabel;
    private readonly Label _ntCountLabel;
    private readonly Label _typanCountLabel;
    private readonly Label _timerLabel;
    private readonly TypanWarScrollBarControl _bar;

    public TypanWarHudControl()
    {
        MinHeight = 42;
        MaxHeight = 42;
        MouseFilter = MouseFilterMode.Ignore;
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = PanelBackground,
            BorderThickness = new Thickness(1),
            BorderColor = Color.FromHex("#6E6A82").WithAlpha(0.28f),
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

        _bar = new TypanWarScrollBarControl
        {
            MinSize = new Vector2(200, 14),
            MaxSize = new Vector2(200, 14),
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
            MinWidth = 52,
            MaxWidth = 52,
            Align = Label.AlignMode.Right,
            HorizontalExpand = true,
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
    private int _ntCount = 1;
    private int _typanCount = 1;

    public TypanWarScrollBarControl()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
        MinHeight = MiniSliderStyles.NativeTrackHeight * BarScale;
        MaxHeight = MiniSliderStyles.NativeTrackHeight * BarScale;

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
        return new Vector2(200, MiniSliderStyles.NativeTrackHeight * BarScale);
    }
}
