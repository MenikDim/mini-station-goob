using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._Mini.TypanWar;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Mini.TypanWar;

[UsedImplicitly]
public sealed class TypanWarHudController : UIController,
    IOnStateEntered<GameplayState>,
    IOnSystemChanged<TypanWarUiSystem>
{
    [UISystemDependency] private readonly TypanWarUiSystem _war = default!;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
    }

    private void OnScreenLoad()
    {
        _war.RequestStatus();
        Refresh();
    }

    public void OnStateEntered(GameplayState state)
    {
        _war.RequestStatus();
        Refresh();
    }

    public void OnSystemLoaded(TypanWarUiSystem system)
    {
        system.StatusUpdated += Refresh;
        Refresh();
    }

    public void OnSystemUnloaded(TypanWarUiSystem system)
    {
        system.StatusUpdated -= Refresh;
    }

    private void Refresh()
    {
        var hud = UIManager.ActiveScreen?.FindControl<TypanWarHudControl>("TypanWarHud");
        if (hud == null)
            return;

        hud.Update(
            _war.Phase,
            _war.NtAlive,
            _war.TypanAlive,
            _war.TimeRemainingSeconds);
    }
}
