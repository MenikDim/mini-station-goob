using Content.Server._Mini.Typan.StationGoal;
using Content.Server._Mini.TypanWar;
using Content.Server._TT.StationHandleJob;
using Content.Server.AlertLevel;
using Content.Server.Antag.Components;
using Content.Server.Audio;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.RoundEnd;
using Content.Server.StationEvents;
using Content.Server.StationEvents.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared._Mini.TypanWar;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Mini.TypanWar;

public sealed class TypanStationWarRuleSystem : GameRuleSystem<TypanStationWarRuleComponent>
{
    public static bool IsWarActive { get; private set; }

    /// <summary>True while the Typan station war gamemode rule is running (prep + combat).</summary>
    public static bool IsModeActive { get; private set; }

    private static readonly SoundPathSpecifier WarDeclarationSound =
        new("/Audio/_Mini/TypanWar/war_declaration.ogg");

    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly TTStationHandleJobSystem _typanJobs = default!;
    [Dependency] private readonly TypanWarFriendlyFireSystem _friendlyFire = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _statusBroadcastAccumulator;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleAddedEvent>(OnGameRuleAdded);
        SubscribeLocalEvent<ConsoleFTLAttemptEvent>(OnConsoleFtlAttempt);
        SubscribeLocalEvent<ShuttleFTLAttemptEvent>(OnShuttleFtlAttempt);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!IsWarActive)
            return;

        if (!_mind.TryGetMind(args.Mob, out var mindId, out var mind))
            return;

        if (TryGetWarSide((mindId, mind), out var side))
            _friendlyFire.SetupCombatant(args.Mob, side);
    }

    private void OnConsoleFtlAttempt(ref ConsoleFTLAttemptEvent ev)
    {
        if (!IsModeActive || ev.Cancelled)
            return;

        ev.Cancelled = true;
        ev.Reason = Loc.GetString("typan-war-ftl-blocked");
    }

    private void OnShuttleFtlAttempt(ref ShuttleFTLAttemptEvent ev)
    {
        if (!IsModeActive || ev.Cancelled)
            return;

        ev.Cancelled = true;
        ev.Reason = Loc.GetString("typan-war-ftl-blocked");
    }

    protected override void Started(EntityUid uid, TypanStationWarRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryResolveStations(component, out var ntStation, out var typanStation))
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        component.NtStation = ntStation;
        component.TypanStation = typanStation;
        component.Phase = TypanWarPhase.Pending;
        component.AnnouncementSent = false;
        component.AnnouncementTime = _timing.CurTime + TimeSpan.FromSeconds(component.AnnouncementDelaySeconds);
        component.WarStartTime = _timing.CurTime + TimeSpan.FromSeconds(component.WarStartDelaySeconds);
        component.WarEndTime = component.WarStartTime + TimeSpan.FromSeconds(component.WarDurationSeconds);

        IsModeActive = true;
        BlockStationEvents(component);
        BroadcastStatus(component);
    }

    protected override void Ended(EntityUid uid, TypanStationWarRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        IsWarActive = false;
        IsModeActive = false;
        component.Phase = TypanWarPhase.Inactive;
        ClearWarCombatants();
        BroadcastStatus(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TypanStationWarRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var component, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (component.Phase == TypanWarPhase.Ended || component.Phase == TypanWarPhase.Inactive)
                continue;

            if (component.Phase == TypanWarPhase.Pending &&
                !component.AnnouncementSent &&
                component.AnnouncementTime != null &&
                _timing.CurTime >= component.AnnouncementTime)
            {
                SendPrepAnnouncement(component);
            }

            if (component.Phase == TypanWarPhase.Pending &&
                component.WarStartTime != null &&
                _timing.CurTime >= component.WarStartTime)
            {
                StartWar(uid, component);
            }

            if (component.Phase == TypanWarPhase.Active)
            {
                var ntAliveNow = CountNtAlive();
                var typanAliveNow = CountTypanAlive();

                if (typanAliveNow < 1 || ntAliveNow < 1)
                {
                    component.Winner = typanAliveNow < 1
                        ? TypanWarWinner.Nanotrasen
                        : TypanWarWinner.Typan;
                    EndWar(uid, component, elimination: true);
                    continue;
                }
            }

            if (component.Phase == TypanWarPhase.Active &&
                component.WarEndTime != null &&
                _timing.CurTime >= component.WarEndTime)
            {
                EndWar(uid, component);
            }

            _statusBroadcastAccumulator += frameTime;
            if (_statusBroadcastAccumulator >= 1f)
            {
                _statusBroadcastAccumulator = 0f;
                BroadcastStatus(component);
            }
        }
    }

    protected override void AppendRoundEndText(EntityUid uid, TypanStationWarRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        if (component.Phase == TypanWarPhase.Inactive)
            return;

        var ntAlive = CountNtAlive();
        var typanAlive = CountTypanAlive();

        args.AddLine(Loc.GetString("typan-war-round-end-header"));
        args.AddLine(Loc.GetString("typan-war-round-end-initial",
            ("nt", component.InitialNtAlive),
            ("typan", component.InitialTypanAlive)));
        args.AddLine(Loc.GetString("typan-war-round-end-final",
            ("nt", ntAlive),
            ("typan", typanAlive)));

        var winnerKey = component.Winner switch
        {
            TypanWarWinner.Nanotrasen => "typan-war-round-end-winner-nt",
            TypanWarWinner.Typan => "typan-war-round-end-winner-typan",
            _ => "typan-war-round-end-stalemate",
        };
        args.AddLine(Loc.GetString(winnerKey));
    }

    private void SendPrepAnnouncement(TypanStationWarRuleComponent component)
    {
        if (component.AnnouncementSent)
            return;

        component.AnnouncementSent = true;
        SendManifestAnnouncement();
        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("typan-war-prep-announce"),
            Loc.GetString("typan-war-sender"),
            colorOverride: Color.OrangeRed);
    }

    private void SendManifestAnnouncement()
    {
        SendMarkupGlobalAnnouncement(Loc.GetString("typan-war-manifest"));
    }

    private void SendMarkupGlobalAnnouncement(string message)
    {
        var wrappedMessage = Loc.GetString(
            "chat-manager-sender-announcement-wrap-message",
            ("sender", Loc.GetString("typan-war-sender")),
            ("message", message));
        _chatManager.ChatMessageToAll(ChatChannel.Radio, message, wrappedMessage, default, false, true, Color.Gold);
    }

    private void StartWar(EntityUid ruleUid, TypanStationWarRuleComponent component)
    {
        var ntAlive = CountNtAlive();
        var typanAlive = CountTypanAlive();

        if (ntAlive < 1 || typanAlive < 1)
        {
            component.Phase = TypanWarPhase.Ended;
            component.Winner = TypanWarWinner.Stalemate;
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("typan-war-start-cancelled"),
                Loc.GetString("typan-war-sender"),
                colorOverride: Color.Orange);
            BroadcastStatus(component);
            ForceEndSelf(ruleUid);
            return;
        }

        component.InitialNtAlive = ntAlive;
        component.InitialTypanAlive = typanAlive;
        component.Phase = TypanWarPhase.Active;
        IsWarActive = true;

        if (component.NtStation is { } nt)
            _alertLevel.SetLevel(nt, "gamma", true, true, true, locked: true);

        if (component.TypanStation is { } typan)
            _alertLevel.SetLevel(typan, "omega", true, true, true, locked: true);

        var announcement = Loc.GetString("typan-war-declaration");
        _chat.DispatchGlobalAnnouncement(announcement, Loc.GetString("typan-war-sender"), colorOverride: Color.OrangeRed);
        _audio.PlayGlobal(WarDeclarationSound, Filter.Broadcast(), false, AudioParams.Default.WithVolume(-2f));

        AssignWarObjectives(component);
        SetupWarCombatants();
        BroadcastStatus(component);
    }

    private void EndWar(EntityUid ruleUid, TypanStationWarRuleComponent component, bool elimination = false)
    {
        component.Phase = TypanWarPhase.Ended;
        IsWarActive = false;

        var ntAlive = CountNtAlive();
        var typanAlive = CountTypanAlive();

        if (!elimination)
            component.Winner = DetermineWinner(component, ntAlive, typanAlive);

        var winnerKey = component.Winner switch
        {
            TypanWarWinner.Nanotrasen when elimination => "typan-war-end-announce-nt-elimination",
            TypanWarWinner.Typan when elimination => "typan-war-end-announce-typan-elimination",
            TypanWarWinner.Nanotrasen => "typan-war-end-announce-nt",
            TypanWarWinner.Typan => "typan-war-end-announce-typan",
            _ => "typan-war-end-announce-stalemate",
        };

        SendMarkupGlobalAnnouncement(Loc.GetString(winnerKey));

        BroadcastStatus(component);
        _roundEnd.RequestRoundEnd(checkCooldown: false);
    }

    private static TypanWarWinner DetermineWinner(TypanStationWarRuleComponent component, int ntAlive, int typanAlive)
    {
        if (typanAlive < 1)
            return TypanWarWinner.Nanotrasen;

        if (ntAlive < 1)
            return TypanWarWinner.Typan;

        var ntLoss = (component.InitialNtAlive - ntAlive) / (float) Math.Max(component.InitialNtAlive, 1);
        var typanLoss = (component.InitialTypanAlive - typanAlive) / (float) Math.Max(component.InitialTypanAlive, 1);

        if (Math.Abs(ntLoss - typanLoss) < 0.001f)
        {
            if (ntAlive > typanAlive)
                return TypanWarWinner.Nanotrasen;
            if (typanAlive > ntAlive)
                return TypanWarWinner.Typan;
            return TypanWarWinner.Stalemate;
        }

        return ntLoss < typanLoss ? TypanWarWinner.Nanotrasen : TypanWarWinner.Typan;
    }

    private void AssignWarObjectives(TypanStationWarRuleComponent component)
    {
        var minds = EntityQueryEnumerator<MindComponent>();
        while (minds.MoveNext(out var mindId, out var mind))
        {
            if (!IsMindAlive(mind))
                continue;

            if (_typanJobs.MindHasHandledJob(mindId))
                TryAddObjective(mindId, mind, "TypanWarObjective", Loc.GetString("typan-war-objective-typan"));
            else if (_jobs.MindTryGetJobId(mindId, out var jobId) && jobId != null)
                TryAddObjective(mindId, mind, "NtWarObjective", Loc.GetString("typan-war-objective-nt"));
        }
    }

    private void TryAddObjective(EntityUid mindId, MindComponent mind, string proto, string text)
    {
        if (_mind.TryFindObjective((mindId, mind), proto, out _))
            return;

        if (!_mind.TryAddObjective(mindId, mind, proto))
            return;

        if (!_mind.TryFindObjective((mindId, mind), proto, out var objective) || objective == null)
            return;

        _metaData.SetEntityDescription(objective.Value, text, MetaData(objective.Value));
    }

    private void SetupWarCombatants()
    {
        var minds = EntityQueryEnumerator<MindComponent>();
        while (minds.MoveNext(out var mindId, out var mind))
        {
            if (!IsMindAlive(mind) || mind.CurrentEntity is not { } mob)
                continue;

            if (!TryGetWarSide((mindId, mind), out var side))
                continue;

            _friendlyFire.SetupCombatant(mob, side);
        }
    }

    private void ClearWarCombatants()
    {
        var query = EntityQueryEnumerator<TypanWarFriendlyFireComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _))
            _friendlyFire.RemoveCombatant(uid);
    }

    private bool TryGetWarSide(Entity<MindComponent> mind, out TypanWarSide side)
    {
        side = default;

        if (_typanJobs.MindHasHandledJob(mind.Owner))
        {
            side = TypanWarSide.Typan;
            return true;
        }

        if (_jobs.MindTryGetJobId(mind.Owner, out var jobId) && jobId != null)
        {
            side = TypanWarSide.Nanotrasen;
            return true;
        }

        return false;
    }

    private int CountTypanAlive()
    {
        var count = 0;
        var minds = EntityQueryEnumerator<MindComponent>();
        while (minds.MoveNext(out var mindId, out var mind))
        {
            if (!IsMindAlive(mind) || !_typanJobs.MindHasHandledJob(mindId))
                continue;

            count++;
        }

        return count;
    }

    private int CountNtAlive()
    {
        var count = 0;
        var minds = EntityQueryEnumerator<MindComponent>();
        while (minds.MoveNext(out var mindId, out var mind))
        {
            if (!IsMindAlive(mind))
                continue;

            if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId == null)
                continue;

            if (_typanJobs.IsHandledJob(jobId.Value))
                continue;

            count++;
        }

        return count;
    }

    private bool IsMindAlive(MindComponent mind)
    {
        var entity = mind.CurrentEntity;
        if (entity == null || !entity.Value.IsValid())
            return false;

        if (HasComp<GhostComponent>(entity))
            return false;

        if (TryComp<MobStateComponent>(entity, out var mobState))
            return mobState.CurrentState != MobState.Dead;

        return true;
    }

    private void BroadcastStatus(TypanStationWarRuleComponent component)
    {
        var phase = component.Phase;
        var ntAlive = phase >= TypanWarPhase.Active ? CountNtAlive() : 0;
        var typanAlive = phase >= TypanWarPhase.Active ? CountTypanAlive() : 0;

        float remaining = 0f;
        if (phase == TypanWarPhase.Pending && component.WarStartTime != null)
            remaining = (float) (component.WarStartTime.Value - _timing.CurTime).TotalSeconds;
        else if (phase == TypanWarPhase.Active && component.WarEndTime != null)
            remaining = (float) (component.WarEndTime.Value - _timing.CurTime).TotalSeconds;

        remaining = Math.Max(0f, remaining);

        RaiseNetworkEvent(new TypanWarStatusEvent(phase, ntAlive, typanAlive, remaining));
    }

    private void BlockStationEvents(TypanStationWarRuleComponent component)
    {
        if (component.EventsBlocked)
            return;

        component.EventsBlocked = true;

        var query = EntityQueryEnumerator<GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            if (HasComp<TypanStationWarRuleComponent>(uid))
                continue;

            if (HasComp<BasicStationEventSchedulerComponent>(uid) ||
                HasComp<RampingStationEventSchedulerComponent>(uid))
            {
                GameTicker.EndGameRule(uid, rule);
            }
        }
    }

    private void OnGameRuleAdded(ref GameRuleAddedEvent args)
    {
        if (!IsModeActive)
            return;

        if (HasComp<TypanStationWarRuleComponent>(args.RuleEntity))
            return;

        if (!TryComp<GameRuleComponent>(args.RuleEntity, out var rule))
            return;

        if (HasComp<AntagSelectionComponent>(args.RuleEntity) ||
            HasComp<StationEventComponent>(args.RuleEntity))
        {
            GameTicker.EndGameRule(args.RuleEntity, rule);
        }
    }

    private bool TryResolveStations(TypanStationWarRuleComponent component, out EntityUid ntStation, out EntityUid typanStation)
    {
        ntStation = EntityUid.Invalid;
        typanStation = EntityUid.Invalid;

        var stations = EntityQueryEnumerator<StationDataComponent>();
        while (stations.MoveNext(out var uid, out _))
        {
            if (HasComp<TTStationHandleJobComponent>(uid))
                typanStation = uid;
            else
                ntStation = uid;
        }

        return ntStation.IsValid() && typanStation.IsValid();
    }
}
