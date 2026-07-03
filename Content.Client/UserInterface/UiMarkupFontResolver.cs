using System;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface;

public sealed class UiMarkupFontResolver : IMarkupFontResolver
{
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IUiFontStackManager _fonts = default!;

    public Font ResolveFont(string fontId, int size)
    {
        var variation = fontId switch
        {
            BoldItalicTag.BoldItalicFont or "DefaultBoldItalic" or "NotoSansDisplayBoldItalic" => "BoldItalic",
            BoldTag.BoldFont or "DefaultBold" or "NotoSansDisplayBold" => "Bold",
            "DefaultItalic" or "NotoSansDisplayItalic" => "Italic",
            "Monospace" => "Mono-Regular",
            _ => "Regular",
        };

        var display = fontId.Contains("Display", StringComparison.Ordinal);
        return _fonts.GetChatStack(_cache, variation, size, display);
    }
}
