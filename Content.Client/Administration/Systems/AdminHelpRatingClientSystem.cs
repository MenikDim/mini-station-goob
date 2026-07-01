using Content.Shared.Administration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client.Administration.Systems;

public sealed class AdminHelpRatingClientSystem : EntitySystem
{
    public event Action<AdminHelpRatingStateEvent>? StateUpdated;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AdminHelpRatingStateEvent>(OnState);
    }

    public void RequestState()
    {
        RaiseNetworkEvent(new AdminHelpRatingOpenRequestEvent());
    }

    public void Submit(NetUserId adminUserId, byte stars)
    {
        RaiseNetworkEvent(new AdminHelpRatingSubmitEvent(adminUserId, stars));
    }

    private void OnState(AdminHelpRatingStateEvent ev, EntitySessionEventArgs args)
    {
        StateUpdated?.Invoke(ev);
    }
}
