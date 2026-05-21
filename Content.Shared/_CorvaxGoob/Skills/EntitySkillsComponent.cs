using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Skills;

/// <summary>
/// Skills stored on the mob body, not on the player mind.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntitySkillsComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<Skills> Skills = new();
}
