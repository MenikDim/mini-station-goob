using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.Lobby.UI.Roles;

/// <summary>
/// Collapsible heading with an optional department icon before the title.
/// </summary>
public sealed class DepartmentCollapsibleHeading : CollapsibleHeading
{
    public DepartmentCollapsibleHeading(string title, Texture? iconTexture)
    {
        Title = title;

        if (iconTexture == null || GetChild(0) is not BoxContainer box)
            return;

        var icon = new TextureRect
        {
            Texture = iconTexture,
            TextureScale = new Vector2(3.75f, 3.75f),
            Stretch = TextureRect.StretchMode.KeepCentered,
            CanShrink = false,
            MinSize = new Vector2(30, 30),
            Margin = new Thickness(4, 0, 8, 0),
            VerticalAlignment = VAlignment.Center,
        };

        box.AddChild(icon);
        icon.SetPositionInParent(1);
    }
}
