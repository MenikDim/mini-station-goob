using Content.Shared.Doors;
using Content.Shared.RPSX.DarkForces.Ratvar.Righteous.Roles;

namespace Content.Server.RPSX.DarkForces.Ratvar;

public sealed class RatvarDoorSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PinionDoorComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpen);
    }

    private void OnBeforeDoorOpen(EntityUid uid, PinionDoorComponent comp, BeforeDoorOpenedEvent args)
    {
        if (args.User != null && !HasComp<RatvarRighteousComponent>(args.User.Value))
            args.Cancel();
    }
}

[RegisterComponent]
public sealed partial class PinionDoorComponent : Component { }
