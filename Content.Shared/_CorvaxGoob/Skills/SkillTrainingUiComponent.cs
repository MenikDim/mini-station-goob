using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Skills;

[RegisterComponent, NetworkedComponent]
public sealed partial class SkillTrainingUiComponent : Component
{
    [DataField]
    public EntityUid Teacher;
}
