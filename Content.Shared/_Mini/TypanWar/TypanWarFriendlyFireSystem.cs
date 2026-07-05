using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Toggleable;
using Robust.Shared.Physics.Events;

namespace Content.Shared._Mini.TypanWar;

public sealed class TypanWarFriendlyFireSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TypanWarFriendlyFireComponent, ToggleActionEvent>(OnToggle);
        SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(OnProjectilePreventCollide);
    }

    public void SetupCombatant(EntityUid uid, TypanWarSide side, bool enabled = true)
    {
        var faction = EnsureComp<TypanWarFactionComponent>(uid);
        faction.Side = side;
        Dirty(uid, faction);

        var ff = EnsureComp<TypanWarFriendlyFireComponent>(uid);
        ff.Enabled = enabled;

        if (ff.ActionEntity == null || !Exists(ff.ActionEntity))
            _actions.AddAction(uid, ref ff.ActionEntity, "ActionTypanWarFriendlyFire");

        if (ff.ActionEntity is { } action)
            _actions.SetToggled(action, ff.Enabled);

        Dirty(uid, ff);
    }

    public void RemoveCombatant(EntityUid uid)
    {
        if (TryComp<TypanWarFriendlyFireComponent>(uid, out var ff) && ff.ActionEntity is { } action)
            _actions.RemoveAction(uid, action);

        RemComp<TypanWarFriendlyFireComponent>(uid);
        RemComp<TypanWarFactionComponent>(uid);
    }

    public bool ShouldPassThrough(EntityUid shooter, EntityUid target)
    {
        if (shooter == target)
            return true;

        if (!TryComp<TypanWarFriendlyFireComponent>(shooter, out var ff) || !ff.Enabled)
            return false;

        return IsSameFaction(shooter, target);
    }

    public bool IsSameFaction(EntityUid a, EntityUid b)
    {
        if (!TryComp<TypanWarFactionComponent>(a, out var factionA))
            return false;

        if (!TryComp<TypanWarFactionComponent>(b, out var factionB))
            return false;

        return factionA.Side == factionB.Side;
    }

    private void OnToggle(Entity<TypanWarFriendlyFireComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Enabled = !ent.Comp.Enabled;

        if (ent.Comp.ActionEntity is { } action)
            _actions.SetToggled(action, ent.Comp.Enabled);

        Dirty(ent);

        var msg = ent.Comp.Enabled
            ? Loc.GetString("typan-war-ff-enabled")
            : Loc.GetString("typan-war-ff-disabled");
        _popup.PopupClient(msg, ent, args.Performer);
    }

    private void OnProjectilePreventCollide(Entity<ProjectileComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        var shooter = ent.Comp.Shooter;
        if (shooter == null)
            return;

        if (ShouldPassThrough(shooter.Value, args.OtherEntity))
            args.Cancelled = true;
    }
}
