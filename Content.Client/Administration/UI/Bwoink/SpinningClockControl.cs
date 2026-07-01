using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Administration.UI.Bwoink;

/// <summary>
/// Animated clock from a 128×32 horizontal spritesheet (4×32×32 frames).
/// </summary>
public sealed class SpinningClockControl : TextureRect
{
    public const string ClockTexturePath = "/Textures/_Mini/Interface/clock.png";

    private const int FrameCount = 4;
    private const int FrameWidth = 32;
    private const int FrameHeight = 32;
    private const float FrameDuration = 0.12f;
    private const float DisplaySize = 24f;

    private readonly Texture[] _frames = new Texture[FrameCount];
    private float _timer;
    private int _frameIndex;

    public SpinningClockControl()
    {
        var cache = IoCManager.Resolve<IResourceCache>();
        var sheet = cache.GetTexture(ClockTexturePath);

        for (var i = 0; i < FrameCount; i++)
        {
            _frames[i] = new AtlasTexture(
                sheet,
                new UIBox2(i * FrameWidth, 0, (i + 1) * FrameWidth, FrameHeight));
        }

        Texture = _frames[0];
        Stretch = StretchMode.KeepAspectCentered;
        MinSize = new Vector2(DisplaySize, DisplaySize);
        MaxSize = new Vector2(DisplaySize, DisplaySize);
        VerticalAlignment = VAlignment.Center;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _timer += args.DeltaSeconds;
        if (_timer < FrameDuration)
            return;

        _timer -= FrameDuration;
        _frameIndex = (_frameIndex + 1) % FrameCount;
        Texture = _frames[_frameIndex];
    }
}
