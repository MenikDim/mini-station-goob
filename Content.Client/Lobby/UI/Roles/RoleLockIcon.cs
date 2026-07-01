using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI.Roles;

/// <summary>
/// Locked-role marker: padlock with requirements tooltip on hover.
/// </summary>
public sealed class RoleLockIcon : Control
{
    public const string TexturePath = "/Textures/_Mini/Interface/lock.png";
    private const float DefaultLockSize = 32f;

    [Dependency] private readonly IResourceCache _cache = default!;

    public RoleLockIcon(float size = DefaultLockSize)
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Stop;
        VerticalAlignment = VAlignment.Center;
        MinSize = new Vector2(size, size);
        MaxSize = new Vector2(size, size);

        var center = new CenterContainer
        {
            MinSize = MinSize,
            MaxSize = MaxSize,
        };

        center.AddChild(new TextureRect
        {
            Texture = _cache.GetTexture(TexturePath),
            MinSize = MinSize,
            MaxSize = MaxSize,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
        });

        AddChild(center);
    }

    public void SetRequirements(FormattedMessage requirements)
    {
        var tooltip = new Tooltip();
        tooltip.SetMessage(requirements);
        TooltipSupplier = _ => tooltip;
    }
}
