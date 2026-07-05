using Content.Shared._Mini.TypanWar;
using Robust.Shared.Timing;

namespace Content.Client._Mini.TypanWar;

/// <summary>
/// Caches Typan war HUD state from server broadcasts.
/// </summary>
public sealed class TypanWarUiSystem : EntitySystem
{
    public TypanWarPhase Phase { get; private set; } = TypanWarPhase.Inactive;
    public int NtAlive { get; private set; }
    public int TypanAlive { get; private set; }
    public float TimeRemainingSeconds { get; private set; }

    public event Action? StatusUpdated;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TypanWarStatusEvent>(OnStatus);
    }

    private void OnStatus(TypanWarStatusEvent ev)
    {
        Phase = ev.Phase;
        NtAlive = ev.NtAlive;
        TypanAlive = ev.TypanAlive;
        TimeRemainingSeconds = ev.TimeRemainingSeconds;
        StatusUpdated?.Invoke();
    }
}
