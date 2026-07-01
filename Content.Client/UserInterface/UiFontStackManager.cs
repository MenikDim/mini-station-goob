using Content.Client.Resources;
using Content.Shared.CCVar;
using Content.Shared.UserInterface;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface;

public interface IUiFontStackManager
{
    UiFontStylePrototype ActiveStyle { get; }

    void Initialize();

    Font GetStack(IResourceCache cache, string variation = "Regular", int size = 10, bool display = false);

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
    }

    public Font GetStack(IResourceCache cache, string variation = "Regular", int size = 10, bool display = false)
    {
        EnsureInitialized();

        var primary = ResolvePrimaryPath(variation, display);

        if (ActiveStyle.ID == DefaultStyleId && !ActiveStyle.NotoFallback)
            return BuildDefaultStack(cache, primary, variation, size);

        if (!ActiveStyle.NotoFallback)
            return cache.GetFont(primary, size);

        return BuildStack(cache, primary, variation, size);
    }

    public Font GetStackWithPrimary(IResourceCache cache, string path, int size = 10)
    {
        EnsureInitialized();

        if (ActiveStyle.ID == DefaultStyleId && !ActiveStyle.NotoFallback)
            return cache.GetFont(NotoStackWithPrimary(path), size);

        return BuildStack(cache, path, "Regular", size);
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
