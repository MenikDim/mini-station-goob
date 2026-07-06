using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Skills;

[Serializable, NetSerializable]
public enum SkillTrainingUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SkillTrainingBoundUserInterfaceState(List<Skills> skills) : BoundUserInterfaceState
{
    public List<Skills> Skills { get; private set; } = skills;
}

[Serializable, NetSerializable]
public sealed class SkillTrainingSelectSkillMessage(Skills skill) : BoundUserInterfaceMessage
{
    public Skills Skill { get; private set; } = skill;
}
