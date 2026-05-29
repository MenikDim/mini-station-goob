using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Containers;
using Content.Shared.DragDrop;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Revenant.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._Orion.Bitrunning.Systems;

public abstract class SharedNetpodSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public const string BodyContainerId = "netpod-body";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetpodComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<NetpodComponent, CanDropTargetEvent>(OnCanDropTarget, after: [typeof(DragInsertContainerSystem)]);
    }

    private void OnInsertAttempt(Entity<NetpodComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != BodyContainerId)
            return;

        if (!CanAcceptOccupant(args.EntityUid, ent.Comp))
            args.Cancel();
    }

    private void OnCanDropTarget(Entity<NetpodComponent> ent, ref CanDropTargetEvent args)
    {
        if (!CanManipulateNetpod(args.User) || !CanAcceptOccupant(args.Dragged, ent.Comp))
            args.CanDrop = false;
    }

    public bool CanAcceptOccupant(EntityUid entity, NetpodComponent pod)
    {
        if (!HasComp<MobStateComponent>(entity))
            return false;

        if (_mobState.IsDead(entity))
            return false;

        if (!TryComp<MindContainerComponent>(entity, out var mindContainer))
            return false;

        if (!_mind.TryGetMind(entity, out _, out _, mindContainer))
            return false;

        if (pod.OccupantBlacklist != null && _whitelist.IsValid(pod.OccupantBlacklist, entity))
            return false;

        return true;
    }

    public bool CanManipulateNetpod(EntityUid user)
    {
        return !(HasComp<RevenantComponent>(user) && !HasComp<CorporealComponent>(user));
    }
}
