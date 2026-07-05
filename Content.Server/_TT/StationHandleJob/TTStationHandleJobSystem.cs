using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server._TT.StationHandleJob;

public sealed class TTStationHandleJobSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: [typeof(ArrivalsSystem), typeof(ContainerSpawnPointSystem), typeof(SpawnPointSystem)]);
    }

    /// <summary>
    /// Returns true if the job belongs to a TTStationHandleJob station (e.g. Typan roles).
    /// </summary>
    public bool IsHandledJob(ProtoId<JobPrototype> job) => GetStation(job) != null;

    public bool MindHasHandledJob(EntityUid mindId)
    {
        return _jobs.MindTryGetJobId(mindId, out var jobId)
               && jobId != null
               && IsHandledJob(jobId.Value);
    }

    /// <summary>
    /// Ensures handled jobs are assigned to their dedicated station at roundstart,
    /// and that non-handled jobs are not assigned to handled-only stations.
    /// </summary>
    public void FixJobStationAssignments(ref Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)> assignedJobs)
    {
        var handledStations = GetHandledStations();
        var defaultStation = FindDefaultStation(assignedJobs, handledStations);

        foreach (var userId in assignedJobs.Keys.ToList())
        {
            var (job, station) = assignedJobs[userId];
            if (job == null)
                continue;

            if (GetStation(job.Value) is { } requiredStation)
            {
                if (station != requiredStation)
                    assignedJobs[userId] = (job, requiredStation);
                continue;
            }

            if (station == EntityUid.Invalid || !handledStations.Contains(station))
                continue;

            if (defaultStation is { } fallback)
                assignedJobs[userId] = (job, fallback);
        }
    }

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult is not null)
        {
            Log.Error("The spawn result has already been received");
            return;
        }

        if (ev.Job is not { } job)
        {
            Log.Debug("The job does not exist");
            return;
        }

        var handledStation = GetStation(job);
        var requestedStation = ev.Station;
        var requestedIsHandledStation = requestedStation is { } requestedStationUid &&
            HasComp<TTStationHandleJobComponent>(requestedStationUid);

        if (handledStation is null)
        {
            if (requestedIsHandledStation)
            {
                AbortSpawn(ev,
                    $"Blocked spawn for job {job} on station {GetStationName(requestedStation)}: this station only accepts TTStationHandleJob roles.",
                    Loc.GetString("game-ticker-player-job-spawn-invalid-station"));
            }

            return;
        }

        if (requestedStation is { } req && req != handledStation.Value)
        {
            Log.Warning(
                $"Redirecting spawn for job {job} from {GetStationName(requestedStation)} to {GetStationName(handledStation)}.");
        }

        var possiblePositions = CollectSpawnLocations(handledStation.Value, job, lateJoin: _gameTicker.RunLevel == GameRunLevel.InRound);

        if (possiblePositions.Count == 0 && _gameTicker.RunLevel != GameRunLevel.InRound)
            possiblePositions = CollectSpawnLocations(handledStation.Value, job, lateJoin: true);

        if (possiblePositions.Count == 0)
        {
            AbortSpawn(ev,
                $"No spawn points found for role {job} on station {GetStationName(handledStation)}.",
                Loc.GetString("game-ticker-player-job-spawn-no-spawn-point"));
            return;
        }

        var spawnLoc = _random.Pick(possiblePositions);
        ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            job,
            ev.HumanoidCharacterProfile,
            handledStation);
    }

    private List<EntityCoordinates> CollectSpawnLocations(EntityUid handledStation, ProtoId<JobPrototype> job, bool lateJoin)
    {
        var possiblePositions = new List<EntityCoordinates>();
        var expectedSpawnType = lateJoin ? SpawnPointType.LateJoin : SpawnPointType.Job;

        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (!IsSpawnOnStation(uid, xform, handledStation))
                continue;

            if (spawnPoint.Job != job)
                continue;

            if (spawnPoint.SpawnType != expectedSpawnType)
                continue;

            possiblePositions.Add(xform.Coordinates);
        }

        return possiblePositions;
    }

    private bool IsSpawnOnStation(EntityUid spawnEnt, TransformComponent xform, EntityUid station)
    {
        if (_station.GetOwningStation(spawnEnt, xform) == station)
            return true;

        if (!TryComp<StationDataComponent>(station, out var data))
            return false;

        if (xform.GridUid is not { } gridUid)
            return false;

        foreach (var grid in data.Grids)
        {
            if (grid == gridUid)
                return true;
        }

        return false;
    }

    private HashSet<EntityUid> GetHandledStations()
    {
        var stations = new HashSet<EntityUid>();
        var query = EntityQueryEnumerator<TTStationHandleJobComponent>();
        while (query.MoveNext(out var uid, out _))
            stations.Add(uid);
        return stations;
    }

    private EntityUid? FindDefaultStation(
        Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)> assignedJobs,
        HashSet<EntityUid> handledStations)
    {
        foreach (var (_, (_, station)) in assignedJobs)
        {
            if (station == EntityUid.Invalid || handledStations.Contains(station))
                continue;

            return station;
        }

        var query = EntityQueryEnumerator<StationJobsComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (handledStations.Contains(uid))
                continue;

            return uid;
        }

        return null;
    }

    private EntityUid? GetStation(ProtoId<JobPrototype> job)
    {
        var query = EntityQueryEnumerator<TTStationHandleJobComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Jobs.Contains(job))
                continue;

            return uid;
        }

        return null;
    }

    private void AbortSpawn(PlayerSpawningEvent ev, string reason, string failureMessage)
    {
        ev.PreventFallback = true;
        ev.FailureMessage = failureMessage;
        Log.Warning(reason);
    }

    private string GetStationName(EntityUid? station)
    {
        return station is { } stationUid
            ? Name(stationUid)
            : "<null>";
    }
}
