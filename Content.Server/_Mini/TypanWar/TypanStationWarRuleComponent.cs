using Content.Shared._Mini.TypanWar;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System.Threading;

namespace Content.Server._Mini.TypanWar;

[RegisterComponent, Access(typeof(TypanStationWarRuleSystem))]
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
    public int MinNtAlive = 8;

    [DataField]
    public int MinTypanAlive = 3;

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
    public int InitialNtAlive;

    [DataField]
    public int InitialTypanAlive;

    [DataField]
    public TypanWarWinner Winner = TypanWarWinner.None;

    [DataField]
    public bool EventsBlocked;

    [DataField]
    public bool WarMusicStarted;

    public CancellationTokenSource? WarMusicLoopCancel;
}
