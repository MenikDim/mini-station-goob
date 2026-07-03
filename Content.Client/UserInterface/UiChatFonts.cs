using Content.Client.Resources;
using Content.Goobstation.UIKit.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface;

/// <summary>
/// Font sizes for chat output panels (game chat, ahelp, etc.).
/// </summary>
public static class UiChatFonts
{
    public const int BaseSize = 12;
    public const float LineHeightScale = 1f;

    public static Font Get(IResourceCache cache)
    {
        var fonts = IoCManager.Resolve<IUiFontStackManager>();
        return cache.GetChatStack("Regular", fonts.GetChatFontSize(BaseSize));
    }

    public static void ApplyToOutput(Control output, IResourceCache? cache = null)
    {
        cache ??= IoCManager.Resolve<IResourceCache>();
        var font = Get(cache);

        switch (output)
        {
            case CustomOutputPanel custom:
                custom.FontOverride = font;
                custom.LineHeightScale = LineHeightScale;
                custom.InvalidateStyleSheet();
                custom.InvalidateLayout();
                break;
            case OutputPanel panel:
                panel.FontOverride = font;
                panel.LineHeightScale = LineHeightScale;
                panel.InvalidateStyleSheet();
                panel.InvalidateLayout();
                break;
        }
    }
}
