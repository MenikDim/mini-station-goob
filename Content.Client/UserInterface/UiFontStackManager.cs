using System;
using System.Collections.Generic;
using Content.Client.Resources;
using Content.Shared.CCVar;
using Content.Shared.UserInterface;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface;

public interface IUiFontStackManager
{
    UiFontStylePrototype ActiveStyle { get; }

    void Initialize();

    Font GetStack(IResourceCache cache, string variation = "Regular", int size = 10, bool display = false);

    /// <summary>
    /// Chat/output font at an exact pixel size — no <see cref="UiFontStylePrototype.SizeOffset"/>.
    /// </summary>
    Font GetChatStack(IResourceCache cache, string variation = "Regular", int size = 12, bool display = false);

    int GetChatFontSize(int baseSize = 12);

    bool UsesPrimaryChatFontOverride { get; }

    Font GetStackWithPrimary(IResourceCache cache, string path, int size = 10);
}

public sealed class UiFontStackManager : IUiFontStackManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private const string DefaultStyleId = "Default";

    private static readonly string[] NotoRegularFallback =
    [
        "/Fonts/NotoSans/NotoSans-Regular.ttf",
        "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
        "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf",
        "/Fonts/NotoSans/NotoSansSC-Regular.ttf",
    ];

    private static readonly string[] NotoBoldFallback =
    [
        "/Fonts/NotoSans/NotoSans-Bold.ttf",
        "/Fonts/NotoSans/NotoSansSymbols-Bold.ttf",
        "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf",
        "/Fonts/NotoSans/NotoSansSC-Regular.ttf",
    ];

    private static readonly string[] NotoItalicFallback =
    [
        "/Fonts/NotoSans/NotoSans-Italic.ttf",
        "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
        "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf",
        "/Fonts/NotoSans/NotoSansSC-Regular.ttf",
    ];

    private static readonly string[] NotoBoldItalicFallback =
    [
        "/Fonts/NotoSans/NotoSans-BoldItalic.ttf",
        "/Fonts/NotoSans/NotoSansSymbols-Bold.ttf",
        "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf",
        "/Fonts/NotoSans/NotoSansSC-Regular.ttf",
    ];

    public UiFontStylePrototype ActiveStyle { get; private set; } = default!;

    private bool _initialized;
    private string _cachedStyleId = DefaultStyleId;
    private readonly Dictionary<(string Path, int Size), int> _normalizedSizeCache = new();

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _configurationManager.OnValueChanged(CCVars.UiFontStyle, OnStyleChanged, invokeImmediately: true);
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            Initialize();
    }

    private void OnStyleChanged(string styleId)
    {
        if (!_prototypeManager.TryIndex(styleId, out UiFontStylePrototype? style))
            style = _prototypeManager.Index<UiFontStylePrototype>(DefaultStyleId);

        ActiveStyle = style;
        if (_cachedStyleId != style.ID)
        {
            _cachedStyleId = style.ID;
            _normalizedSizeCache.Clear();
        }
    }

    public Font GetStack(IResourceCache cache, string variation = "Regular", int size = 10, bool display = false)
    {
        EnsureInitialized();
        return BuildStackInternal(cache, variation, size, display, applyUiOffset: true);
    }

    public Font GetChatStack(IResourceCache cache, string variation = "Regular", int size = 12, bool display = false)
    {
        EnsureInitialized();
        return BuildStackInternal(cache, variation, size, display, applyUiOffset: false);
    }

    public int GetChatFontSize(int baseSize = 12)
    {
        EnsureInitialized();
        return baseSize switch
        {
            12 => 13,
            _ => baseSize,
        };
    }

    public bool UsesPrimaryChatFontOverride =>
        ActiveStyle?.ID != DefaultStyleId;

    public Font GetStackWithPrimary(IResourceCache cache, string path, int size = 10)
    {
        EnsureInitialized();
        size = NormalizeToNotoLineHeight(cache, path, ApplySizeOffset(size));

        if (ActiveStyle.ID == DefaultStyleId && !ActiveStyle.NotoFallback)
            return cache.GetFont(NotoStackWithPrimary(path), size);

        return BuildStack(cache, path, "Regular", size);
    }

    private Font BuildStackInternal(
        IResourceCache cache,
        string variation,
        int size,
        bool display,
        bool applyUiOffset)
    {
        if (applyUiOffset)
            size = ApplySizeOffset(size);

        var primary = ResolvePrimaryPath(variation, display);
        size = applyUiOffset
            ? NormalizeToNotoLineHeight(cache, primary, size)
            : NormalizeChatSize(cache, primary, size);

        if (ActiveStyle.ID == DefaultStyleId && !ActiveStyle.NotoFallback)
            return BuildDefaultStack(cache, primary, variation, size);

        if (!ActiveStyle.NotoFallback)
            return cache.GetFont(primary, size);

        return BuildStack(cache, primary, variation, size);
    }

    private string ResolvePrimaryPath(string variation, bool display)
    {
        if (variation == "Mono-Regular")
            return ActiveStyle.ResolveMono();

        if (display)
        {
            return variation.StartsWith("Bold", StringComparison.Ordinal)
                ? ActiveStyle.ResolveDisplayBold()
                : ActiveStyle.ResolveDisplayRegular();
        }

        return variation switch
        {
            "Italic" => ActiveStyle.ResolveItalic(),
            "Bold" => ActiveStyle.ResolveBold(),
            "BoldItalic" => ActiveStyle.ResolveBoldItalic(),
            _ when variation.StartsWith("Bold", StringComparison.Ordinal) => ActiveStyle.ResolveBold(),
            _ => ActiveStyle.Regular,
        };
    }

    private Font BuildStack(IResourceCache cache, string primary, string variation, int size)
    {
        if (!ActiveStyle.NotoFallback)
            return cache.GetFont(primary, size);

        var fallback = variation switch
        {
            "Italic" => NotoItalicFallback,
            "Bold" => NotoBoldFallback,
            "BoldItalic" => NotoBoldItalicFallback,
            _ when variation.StartsWith("Bold", StringComparison.Ordinal) => NotoBoldFallback,
            _ => NotoRegularFallback,
        };

        var paths = new string[fallback.Length + 1];
        paths[0] = primary;
        fallback.CopyTo(paths, 1);
        return cache.GetFont(paths, size);
    }

    private static Font BuildDefaultStack(IResourceCache cache, string primary, string variation, int size)
    {
        var sv = variation.StartsWith("Bold", StringComparison.Ordinal) ? "Bold" : "Regular";

        return cache.GetFont(
        [
            primary,
            $"/Fonts/NotoSans/NotoSansSymbols-{sv}.ttf",
            "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf",
            "/Fonts/NotoSans/NotoSansSC-Regular.ttf",
        ], size);
    }

    private int ApplySizeOffset(int size)
    {
        var offset = ActiveStyle?.SizeOffset ?? 0;
        return offset == 0 ? size : size + offset;
    }

    /// <summary>
    /// Scales non-Noto fonts so their line height matches Noto Sans at the requested pixel size.
    /// </summary>
    private int NormalizeToNotoLineHeight(IResourceCache cache, string primaryPath, int size)
    {
        if (ActiveStyle.ID == DefaultStyleId || size <= 0)
            return size;

        var cacheKey = (primaryPath, size);
        if (_normalizedSizeCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var normalized = size;
        try
        {
            var notoFont = cache.GetFont(NotoRegularFallback[0], size);
            var notoLineHeight = notoFont.GetLineHeight(1f);
            if (notoLineHeight > 0)
            {
                var styleFont = cache.GetFont(primaryPath, size);
                var styleLineHeight = styleFont.GetLineHeight(1f);
                if (styleLineHeight > 0)
                {
                    normalized = Math.Max(1, (int)Math.Round(size * (notoLineHeight / (float)styleLineHeight)));
                }
            }
        }
        catch (Exception)
        {
            normalized = size;
        }

        _normalizedSizeCache[cacheKey] = normalized;
        return normalized;
    }

    private int NormalizeChatSize(IResourceCache cache, string primaryPath, int size)
    {
        if (ActiveStyle.ID == DefaultStyleId || size <= 0)
            return size;

        var cacheKey = ($"chat:{primaryPath}", size);
        if (_normalizedSizeCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var normalized = size;
        try
        {
            var notoFont = cache.GetFont(NotoRegularFallback[0], size);
            var notoHeight = notoFont.GetHeight(1f);
            if (notoHeight > 0)
            {
                var styleFont = cache.GetFont(primaryPath, size);
                var styleHeight = styleFont.GetHeight(1f);
                if (styleHeight > 0)
                {
                    // Only shrink — pixel fonts must never be enlarged to match metrics.
                    if (styleHeight > notoHeight)
                    {
                        normalized = Math.Max(1, (int)Math.Round(size * (notoHeight / (float)styleHeight)));
                    }
                }
            }

            var scale = ActiveStyle.ChatSizeScale;
            if (scale > 0f && !MathHelper.CloseToPercent(scale, 1f))
                normalized = Math.Max(1, (int)Math.Round(normalized * scale));
        }
        catch (Exception)
        {
            normalized = size;
        }

        _normalizedSizeCache[cacheKey] = normalized;
        return normalized;
    }

    private static string[] NotoStackWithPrimary(string path)
    {
        return
        [
            path,
            "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
            "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf",
            "/Fonts/NotoSans/NotoSansSC-Regular.ttf",
        ];
    }
}
