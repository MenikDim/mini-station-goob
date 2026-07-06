using Content.Shared._Mini.TypanWar;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using System.Threading;

namespace Content.Server._Mini.TypanWar;

[RegisterComponent, Access(typeof(TypanStationWarRuleSystem), typeof(TypanWarBalanceSystem))]
public sealed partial class TypanStationWarRuleComponent : Component
{
    [DataField]
    public float AnnouncementDelaySeconds = 15f;

    [DataField]
    public float WarStartDelaySeconds = 30f;

    [DataField]
    public float WarDurationSeconds = 1800f;

    [DataField]
    public float WarMusicDelaySeconds = 90f;

    /// <summary>Length of station_war.ogg — used to restart the track when it ends.</summary>
    [DataField]
    public float WarMusicDurationSeconds = 90f;

    [DataField]
    public float RoundEndDelaySeconds = 120f;

    [DataField]
    public int MinNtAlive = 0;

    [DataField]
    public int MinTypanAlive = 0;

    /// <summary>Max allowed faction headcount ratio (e.g. 2 means at most 2× players on one side).</summary>
    [DataField]
    public int MaxFactionRatio = 2;

    [DataField]
    public float PrepInsufficientCheckIntervalSeconds = 30f;

    [DataField]
    public float WarIntelEventDelaySeconds = 900f;

    [DataField]
    public float WarSupplyEventDelaySeconds = 600f;

    /// <summary>Shuttle map spawned and docked to Typan when combat begins.</summary>
    [DataField]
    public ResPath DropShuttlePath = new("/Maps/_Mini/Shuttles/typan_drop_shuttle.yml");

    /// <summary>Shuttle map spawned and docked to NanoTrasen when combat begins.</summary>
    [DataField]
    public ResPath NtDropShuttlePath = new("/Maps/_Mini/Shuttles/nt_drop_shuttle.yml");

    [DataField]
    public float WarEndWarningSeconds = 60f;

    [DataField]
    public float PrepCountdownSoundSeconds = 10f;

    [DataField]
    public string? NtStationGoalTitle;

    [DataField]
    public string? TypanStationGoalTitle;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? AnnouncementTime;

    [DataField]
    public bool AnnouncementSent;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? WarStartTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? WarEndTime;

    [DataField]
    public TypanWarPhase Phase = TypanWarPhase.Pending;

    [DataField]
    public EntityUid? NtStation;

    [DataField]
    public EntityUid? TypanStation;

    [DataField]
    public TypanWarWinner Winner = TypanWarWinner.None;

    /// <summary>Unique players who spawned on the NT station during this war (includes late join).</summary>
    public HashSet<NetUserId> NtJoinedUsers = new();

    /// <summary>Unique players who spawned on the Typan station during this war (includes late join).</summary>
    public HashSet<NetUserId> TypanJoinedUsers = new();

    [DataField]
    public bool WarMusicStarted;

    [DataField]
    public bool PrepCountdownPlayed;

    [DataField]
    public bool WarEndWarningPlayed;

    [DataField]
    public bool WarIntelEventSent;

    [DataField]
    public bool WarSupplyEventSent;

    [DataField]
    public float PrepInsufficientCheckAccumulator;

    public EntityUid? WarMusicAudio;

    public CancellationTokenSource? WarMusicLoopCancel;
}
