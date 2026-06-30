using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface;

/// <summary>
/// Shared pixel-art slider textures and stylesheet boxes for Mini Station.
/// </summary>
public static class MiniSliderStyles
{
    public const string WhiteTrackPath = "/Textures/_Mini/Interface/white_scroll_line.png";
    public const string PlainTrackPath = "/Textures/_Mini/Interface/plain_scroll_line.png";
    public const string LongPlainTrackPath = "/Textures/_Mini/Interface/long_plain_scroll_line.png";
    public const string MarkedTrackPath = "/Textures/_Mini/Interface/scroll_line.png";
    public const string PointerPath = "/Textures/_Mini/Interface/pointer.png";

    public const float NativePlainTrackWidth = 32f;
    public const float NativeWhiteTrackWidth = 16f;
    public const float NativeLongPlainTrackWidth = 64f;
    public const float NativeMarkedTrackWidth = 29f;
    public const float NativeTrackHeight = 7f;
    public const float NativePointerWidth = 7f;
    public const float NativePointerHeight = 11f;

    /// <summary>Compact sliders for settings and general UI.</summary>
    public const float UiScale = 2f;

    /// <summary>Larger sliders for lobby job priority rows.</summary>
    public const float LobbyScale = 3.5f;

    public static float GetControlMinHeight(float scale) =>
        Math.Max(NativeTrackHeight * scale, NativePointerHeight * scale);

    public static float UiControlMinHeight => GetControlMinHeight(UiScale);

    public static StyleBoxTexture CreateShortTrackBox(Texture texture, float scale) =>
        ConfigureShortTrackBox(new StyleBoxTexture
        {
            Texture = texture,
            Mode = StyleBoxTexture.StretchMode.Stretch,
            TextureScale = new Vector2(scale, scale),
        });

    public static StyleBoxTexture CreateSliderTrackBox(Texture texture, float scale)
    {
        var box = new StyleBoxTexture
        {
            Texture = texture,
            Mode = StyleBoxTexture.StretchMode.Stretch,
            TextureScale = new Vector2(scale, scale),
        };

        box.PatchMarginLeft = 4;
        box.PatchMarginRight = 4;
        box.PatchMarginTop = 2;
        box.PatchMarginBottom = 2;
        return box;
    }

    /// <summary>
    /// Horizontally tileable track for wider bars (vote timer, options rows).
    /// Rounded end caps stay fixed; center repeats.
    /// </summary>
    public static StyleBoxTexture CreateLongTrackBox(Texture texture, float scale)
    {
        var box = new StyleBoxTexture
        {
            Texture = texture,
            Mode = StyleBoxTexture.StretchMode.Tile,
            TextureScale = new Vector2(scale, scale),
        };

        box.PatchMarginLeft = 4;
        box.PatchMarginRight = 4;
        box.PatchMarginTop = 2;
        box.PatchMarginBottom = 2;
        return box;
    }

    public static StyleBoxTexture CreateGrabberBox(Texture texture, float scale)
    {
        var box = new StyleBoxTexture
        {
            Texture = texture,
            TextureScale = new Vector2(scale, scale),
        };

        box.PatchMarginLeft = 3;
        box.PatchMarginRight = 4;
        box.PatchMarginTop = 5;
        box.PatchMarginBottom = 6;
        return box;
    }

    public static StyleBoxFlat Transparent { get; } = new()
    {
        BackgroundColor = Color.Transparent,
    };

    public static (StyleBoxTexture Track, StyleBoxTexture Grabber, StyleBoxFlat Invisible) CreateStandard(
        IResourceCache cache,
        float scale = UiScale)
    {
        var trackTex = cache.GetTexture(LongPlainTrackPath);
        var pointerTex = cache.GetTexture(PointerPath);

        return (
            CreateSliderTrackBox(trackTex, scale),
            CreateGrabberBox(pointerTex, scale),
            Transparent);
    }

    private static StyleBoxTexture ConfigureShortTrackBox(StyleBoxTexture box)
    {
        box.PatchMarginTop = 2;
        box.PatchMarginBottom = 2;
        box.PatchMarginLeft = 0;
        box.PatchMarginRight = 0;
        return box;
    }
}
