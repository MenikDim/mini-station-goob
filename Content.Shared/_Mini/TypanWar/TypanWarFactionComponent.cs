using Robust.Shared.GameStates;

namespace Content.Shared._Mini.TypanWar;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TypanWarFactionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TypanWarSide Side;
}
