using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;

namespace Content.Shared.RPSX.DarkForces.Ratvar.Righteous.Roles;

[RegisterComponent, NetworkedComponent]
public sealed partial class RatvarRighteousComponent : Component
{

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "RatvarRighteousIcon";

}
