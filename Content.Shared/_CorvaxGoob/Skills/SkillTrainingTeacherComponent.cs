using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Skills;

/// <summary>
/// Tracks an in-progress training session and post-success cooldown on the trainer entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SkillTrainingTeacherComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? ActiveStudent;

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownUntil;
}
