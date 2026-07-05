using Robust.Shared.Serialization;

namespace Content.Shared._Mini.TypanWar;

[Serializable, NetSerializable]
public sealed class TypanWarStatusEvent : EntityEventArgs
{
    public TypanWarPhase Phase;
    public int NtAlive;
    public int TypanAlive;
    public float TimeRemainingSeconds;

    public TypanWarWinner Winner;

    public TypanWarStatusEvent(
        TypanWarPhase phase,
        int ntAlive,
        int typanAlive,
        float timeRemainingSeconds,
        TypanWarWinner winner = TypanWarWinner.None)
    {
        Phase = phase;
        NtAlive = ntAlive;
        TypanAlive = typanAlive;
        TimeRemainingSeconds = timeRemainingSeconds;
        Winner = winner;
    }
}
