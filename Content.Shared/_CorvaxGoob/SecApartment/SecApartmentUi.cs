using Content.Shared.Medical.SuitSensor;
using Content.Shared.SecApartment;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.SecApartment;

[Serializable, NetSerializable]
public enum SecApartmentUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SecApartmentUpdateState(
    string stationName,
    List<CrewMemberInfo> securityCrew,
    List<CrewMemberInfo> unassignedSecurity,
    List<Squad> squads) : BoundUserInterfaceState
{
    public string StationName { get; private set; } = stationName;
    public List<CrewMemberInfo> SecurityCrew { get; private set; } = securityCrew;
    public List<CrewMemberInfo> UnassignedSecurity { get; private set; } = unassignedSecurity;
    public List<Squad> Squads { get; private set; } = squads;
}

[Serializable, NetSerializable]
public sealed class SensorStatusUpdateState(
    Dictionary<string, SuitSensorStatus?> memberStatuses,
    Dictionary<string, (string Location, bool HasLocation)> squadLocations)
    : BoundUserInterfaceState
{
    public Dictionary<string, SuitSensorStatus?> MemberStatuses { get; private set; } = memberStatuses;
    public Dictionary<string, (string Location, bool HasLocation)> SquadLocations { get; private set; } = squadLocations;
}

[Serializable, NetSerializable]
public sealed class CreateSquadMessage(string squadName) : BoundUserInterfaceMessage
{
    public string SquadName { get; private set; } = squadName;
}

[Serializable, NetSerializable]
public sealed class DeleteSquadMessage(string squadId) : BoundUserInterfaceMessage
{
    public string SquadId { get; private set; } = squadId;

}

[Serializable, NetSerializable]
public sealed class RenameSquadMessage(string squadId, string newName) : BoundUserInterfaceMessage
{
    public string SquadId { get; private set; } = squadId;
    public string NewName { get; private set; } = newName;

}

[Serializable, NetSerializable]
public sealed class UpdateSquadDescriptionMessage(string squadId, string description)
    : BoundUserInterfaceMessage
{
    public string SquadId { get; private set; } = squadId;
    public string Description { get; private set; } = description;

}

[Serializable, NetSerializable]
public sealed class AddMemberToSquadMessage(string squadId, string memberId) : BoundUserInterfaceMessage
{
    public string SquadId { get; private set; } = squadId;
    public string MemberId { get; private set; } = memberId;

}

[Serializable, NetSerializable]
public sealed class RemoveMemberFromSquadMessage(string squadId, string memberId) : BoundUserInterfaceMessage
{
    public string SquadId { get; private set; } = squadId;
    public string MemberId { get; private set; } = memberId;

}

[Serializable, NetSerializable]
public sealed class ChangeSquadIconMessage(string squadId, SquadIconNum iconId) : BoundUserInterfaceMessage
{
    public string SquadId { get; private set; } = squadId;
    public SquadIconNum IconId { get; private set; } = iconId;

}

[Serializable, NetSerializable]
public sealed class ChangeSquadStatusMessage(string squadId, SquadStatus status) : BoundUserInterfaceMessage
{
    public string SquadId { get; private set; } = squadId;
    public SquadStatus Status { get; private set; } = status;
}

[Serializable, NetSerializable]
public sealed class TimerUpdateState(List<TimerEntry> timers) : BoundUserInterfaceState
{
    public List<TimerEntry> Timers { get; private set; } = timers;

}

[Serializable, NetSerializable]
public sealed class RemoveTimerMessage(NetEntity timerUid) : BoundUserInterfaceMessage
{
    public NetEntity TimerUid { get; private set; } = timerUid;
}
