using Content.Shared._White.RadialSelector;
using Content.Shared.RPSX.DarkForces.Ratvar.Righteous.Roles;
using Content.Shared.UserInterface;

namespace Content.Shared.RPSX.DarkForces.Ratvar.Construction;

public sealed class SharedRatvarConstructionSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RatvarConstructionComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<RatvarConstructionComponent, BeforeActivatableUIOpenEvent>(BeforeUiOpen);
        SubscribeLocalEvent<RatvarConstructionComponent, BoundUIOpenedEvent>(OnUiOpened);
    }

    private void OnOpenAttempt(Entity<RatvarConstructionComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<RatvarRighteousComponent>(args.User))
            args.Cancel();
    }

    private void BeforeUiOpen(Entity<RatvarConstructionComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        SetUiState(ent);
    }

    private void OnUiOpened(Entity<RatvarConstructionComponent> ent, ref BoundUIOpenedEvent args)
    {
        SetUiState(ent);
    }

    private void SetUiState(Entity<RatvarConstructionComponent> ent)
    {
        _ui.SetUiState(ent.Owner, RadialSelectorUiKey.Key, new RadialSelectorState(ent.Comp.Entries));
    }
}
