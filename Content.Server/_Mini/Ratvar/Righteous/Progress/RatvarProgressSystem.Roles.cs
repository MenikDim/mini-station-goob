using Content.Shared.RPSX.DarkForces.Ratvar.Righteous.Roles;
using Content.Shared.Access.Components;
using Content.Server.EUI;
using Content.Server.RPSX.DarkForces.Ratvar.UI;
using Robust.Shared.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.RPSX.DarkForces.Ratvar.Righteous.Progress;

public sealed partial class RatvarProgressSystem
{
    [Dependency] private readonly EuiManager _euiMan = default!;

    public void SetupRighteous(EntityUid uid)
    {
        if (_progressEntity?.Comp is not { } comp)
            return;

        // Выдаём доступ к дверям Ратвара
        var access = EnsureComp<AccessComponent>(uid);
        access.Tags.Add("RatvarRighteous");

        AddObjectivesToRighteous(
            uid,
            comp.RatvarBeaconsObjective,
            comp.RatvarConvertObjective,
            comp.RatvarPowerObjective,
            comp.RatvarSummonObjective
        );

        // Открываем приветственное окно
        if (TryComp<ActorComponent>(uid, out var actor))
        {
            _euiMan.OpenEui(new RatvarRoundStartEui(), actor.PlayerSession);
        }
    }

    private bool CanUseRatvarItems(EntityUid uid)
    {
        return HasComp<RatvarRighteousComponent>(uid);
    }
}
