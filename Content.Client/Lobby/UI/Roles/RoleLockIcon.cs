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
/// Locked-role marker: 32×32 padlock with requirements tooltip on hover.
/// </summary>
public sealed class RoleLockIcon : Control
{
    public const string TexturePath = "/Textures/_Mini/Interface/lock.png";
    private const float LockSize = 32f;

    [Dependency] private readonly IResourceCache _cache = default!;

    public RoleLockIcon()
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Stop;
        VerticalAlignment = VAlignment.Center;
        MinSize = new Vector2(LockSize, LockSize);
        MaxSize = new Vector2(LockSize, LockSize);

        var center = new CenterContainer
        {
            MinSize = new Vector2(LockSize, LockSize),
            MaxSize = new Vector2(LockSize, LockSize),
        };

        center.AddChild(new TextureRect
        {
            Texture = _cache.GetTexture(TexturePath),
            MinSize = new Vector2(LockSize, LockSize),
            MaxSize = new Vector2(LockSize, LockSize),
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
