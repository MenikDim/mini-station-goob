// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Shared.Xenobiology;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Server.NPC.HTN;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Xenobiology;

public sealed class SlimeOvercrowdingSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SlimeLatchSystem _latch = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<SlimeComponent> _slimeQuery;
    private EntityQuery<SlimeClusterComponent> _clusterQuery;

    private float _radius = 4f;
    private int _htnThreshold = 8;
    private int _mergeThreshold = 10;
    private float _checkInterval = 3f;
    private TimeSpan _nextCheck = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        _slimeQuery = GetEntityQuery<SlimeComponent>();
        _clusterQuery = GetEntityQuery<SlimeClusterComponent>();

        Subs.CVar(_cfg, GoobCVars.OvercrowdingRadius, val => _radius = val, true);
        Subs.CVar(_cfg, GoobCVars.OvercrowdingHtnThreshold, val => _htnThreshold = val, true);
        Subs.CVar(_cfg, GoobCVars.OvercrowdingMergeThreshold, val => _mergeThreshold = val, true);
        Subs.CVar(_cfg, GoobCVars.OvercrowdingCheckInterval, val => _checkInterval = val, true);

        SubscribeLocalEvent<SlimeClusterComponent, ExaminedEvent>(OnClusterExamined);
        SubscribeLocalEvent<SlimeClusterComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SlimeClusterComponent, SlimeClusterPeelDoAfterEvent>(OnPeelDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + TimeSpan.FromSeconds(_checkInterval);
        ProcessOvercrowding();
    }

    private void ProcessOvercrowding()
    {
        var visited = new HashSet<EntityUid>();
        var overcrowdedNow = new HashSet<EntityUid>();

        var slimeEnum = EntityQueryEnumerator<SlimeComponent, MobStateComponent>();
        while (slimeEnum.MoveNext(out var uid, out _, out _))
        {
            if (visited.Contains(uid) || !CanParticipate(uid))
                continue;

            var group = BuildGroup(uid, visited);
            if (group.Count == 0)
                continue;

            var totalCount = CountSlimes(group);
            var uniformBreed = TryGetUniformBreed(group, out var breed);

            if (uniformBreed && totalCount >= _mergeThreshold)
            {
                if (!(group.Count == 1
                    && _clusterQuery.TryComp(group[0], out var existing)
                    && existing.Count == totalCount))
                {
                    MergeGroup(group, breed!, totalCount);
                }

                foreach (var member in group)
                    overcrowdedNow.Add(member);

                continue;
            }

            if (totalCount >= _htnThreshold)
            {
                var showedPopup = false;
                foreach (var member in group)
                {
                    overcrowdedNow.Add(member);
                    SetOvercrowded(member, ref showedPopup);
                }
            }
        }

        var overcrowdedEnum = EntityQueryEnumerator<SlimeOvercrowdedComponent, SlimeComponent>();
        while (overcrowdedEnum.MoveNext(out var uid, out _, out _))
        {
            if (overcrowdedNow.Contains(uid))
                continue;

            ClearOvercrowded(uid);
        }
    }

    private void MergeGroup(List<EntityUid> group, ProtoId<BreedPrototype> breed, int totalCount)
    {
        if (group.Count == 0)
            return;

        var anchor = group[0];
        foreach (var uid in group)
        {
            if (HasComp<SlimeClusterComponent>(uid))
            {
                anchor = uid;
                break;
            }
        }

        foreach (var uid in group)
        {
            if (uid == anchor)
                continue;

            if (_slimeQuery.TryComp(uid, out var slime) && _latch.IsLatched((uid, slime)))
                _latch.Unlatch((uid, slime));

            QueueDel(uid);
        }

        var cluster = EnsureComp<SlimeClusterComponent>(anchor);
        cluster.Count = totalCount;
        Dirty(anchor, cluster);

        if (_slimeQuery.TryComp(anchor, out var anchorSlime) && anchorSlime.Breed != breed)
        {
            anchorSlime.Breed = breed;
            Dirty(anchor, anchorSlime);
        }

        UpdateClusterScale(anchor, cluster.Count);
        EnsureComp<SlimeOvercrowdedComponent>(anchor);
        _htn.SetHTNEnabled(anchor, false);

        _popup.PopupCoordinates(Loc.GetString("slime-overcrowding-merged"), Transform(anchor).Coordinates, PopupType.MediumCaution);
    }

    private List<EntityUid> BuildGroup(EntityUid start, HashSet<EntityUid> visited)
    {
        var group = new List<EntityUid>();
        var queue = new Queue<EntityUid>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var uid = queue.Dequeue();
            if (!visited.Add(uid) || !CanParticipate(uid))
                continue;

            group.Add(uid);

            foreach (var nearby in _lookup.GetEntitiesInRange<SlimeComponent>(Transform(uid).Coordinates, _radius))
            {
                if (!visited.Contains(nearby))
                    queue.Enqueue(nearby);
            }
        }

        return group;
    }

    private bool CanParticipate(EntityUid uid)
    {
        if (!_slimeQuery.HasComp(uid) || _mobState.IsDead(uid))
            return false;

        if (_container.IsEntityInContainer(uid))
            return false;

        if (HasComp<BeingLatchedComponent>(uid))
            return false;

        if (_slimeQuery.TryComp(uid, out var slime) && slime.LatchedTarget != null)
            return false;

        return true;
    }

    private int CountSlimes(IReadOnlyCollection<EntityUid> group)
    {
        var total = 0;
        foreach (var uid in group)
        {
            if (_clusterQuery.TryComp(uid, out var cluster))
                total += cluster.Count;
            else
                total += 1;
        }

        return total;
    }

    private bool TryGetUniformBreed(IReadOnlyCollection<EntityUid> group, out ProtoId<BreedPrototype> breed)
    {
        breed = default;
        ProtoId<BreedPrototype>? expected = null;

        foreach (var uid in group)
        {
            if (!_slimeQuery.TryComp(uid, out var slime))
                return false;

            if (expected == null)
                expected = slime.Breed;
            else if (expected != slime.Breed)
                return false;
        }

        if (expected == null)
            return false;

        breed = expected.Value;
        return true;
    }

    private void SetOvercrowded(EntityUid uid, ref bool showedPopup)
    {
        EnsureComp<SlimeOvercrowdedComponent>(uid);

        if (TryComp<HTNComponent>(uid, out _))
            _htn.SetHTNEnabled(uid, false);

        if (showedPopup)
            return;

        showedPopup = true;
        _popup.PopupCoordinates(Loc.GetString("slime-overcrowding-htn-off"), Transform(uid).Coordinates, PopupType.MediumCaution);
    }

    private void ClearOvercrowded(EntityUid uid)
    {
        RemComp<SlimeOvercrowdedComponent>(uid);

        if (!TryComp<HTNComponent>(uid, out _))
            return;

        if (_clusterQuery.TryComp(uid, out var cluster) && cluster.Count > 1)
            return;

        _htn.SetHTNEnabled(uid, true, 2f);
    }

    public void UpdateClusterScale(EntityUid uid, int count)
    {
        var scale = Math.Clamp(1f + (count - 1) * 0.12f, 1f, 3f);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, XenoSlimeVisuals.ClusterScale, scale, appearance);

        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.Fixtures.TryGetValue("fix1", out var fixture))
            _physics.SetRadius(uid, "fix1", fixture, fixture.Shape, MathF.Min(0.3f * scale, 0.9f));
    }

    private void OnClusterExamined(Entity<SlimeClusterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("slime-cluster-examine", ("count", ent.Comp.Count)));
    }

    private void OnInteractUsing(Entity<SlimeClusterComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<UtensilComponent>(args.Used, out var utensil) || (utensil.Types & UtensilType.Knife) == 0)
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.PeelDelay, new SlimeClusterPeelDoAfterEvent(), ent, ent, args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnPeelDoAfter(Entity<SlimeClusterComponent> ent, ref SlimeClusterPeelDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<UtensilComponent>(args.Used, out var utensil) || (utensil.Types & UtensilType.Knife) == 0)
            return;

        if (!_slimeQuery.TryComp(ent, out var template))
            return;

        if (!_proto.TryIndex(template.Breed, out var breed))
            return;

        var spawned = SpawnNextToOrDrop(ent.Comp.PeelPrototype, ent.Owner, null, breed.Components);
        if (!TryComp<SlimeComponent>(spawned, out var newSlime))
            return;

        CopySlimeState(template, newSlime);
        Dirty(spawned, newSlime);

        if (newSlime.ShouldHaveShader && newSlime.Shader != null)
            _appearance.SetData(spawned, XenoSlimeVisuals.Shader, newSlime.Shader);

        _appearance.SetData(spawned, XenoSlimeVisuals.Color, newSlime.SlimeColor);
        _meta.SetEntityName(spawned, breed.BreedName);

        ent.Comp.Count--;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("slime-cluster-peel-success", ("target", ent)), ent, args.User);

        if (ent.Comp.Count <= 0)
        {
            RemComp<SlimeClusterComponent>(ent);
            UpdateClusterScale(ent, 1);
            RemComp<SlimeOvercrowdedComponent>(ent);
            _htn.SetHTNEnabled(ent, true, 2f);
            return;
        }

        UpdateClusterScale(ent, ent.Comp.Count);
    }

    private static void CopySlimeState(SlimeComponent source, SlimeComponent target)
    {
        target.Breed = source.Breed;
        target.SlimeColor = source.SlimeColor;
        target.Tamer = source.Tamer;
        target.MaxOffspring = source.MaxOffspring;
        target.ExtractsProduced = source.ExtractsProduced;
        target.MutationChance = source.MutationChance;
        target.PotentialMutations = new HashSet<ProtoId<BreedPrototype>>(source.PotentialMutations);
        target.DefaultSlimeProto = source.DefaultSlimeProto;
        target.DefaultExtract = source.DefaultExtract;
        target.ShouldHaveShader = source.ShouldHaveShader;
        target.Shader = source.Shader;
    }
}
