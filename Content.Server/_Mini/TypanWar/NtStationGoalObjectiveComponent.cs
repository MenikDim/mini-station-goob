using Content.Server._CorvaxGoob.StationGoal;
using Robust.Shared.Prototypes;

namespace Content.Server._Mini.TypanWar;

[RegisterComponent]
public sealed partial class NtStationGoalObjectiveComponent : Component
{
    [DataField]
    public ProtoId<StationGoalPrototype> GoalId;
}
