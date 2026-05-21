using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Skills;

[RegisterComponent]
public sealed partial class SkillTrainingUiComponent : Component
{
    [DataField]
    public EntityUid Teacher;
}
