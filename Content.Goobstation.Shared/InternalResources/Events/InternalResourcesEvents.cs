using Content.Goobstation.Shared.InternalResources.Data;
using Content.Shared.Inventory;

namespace Content.Goobstation.Shared.InternalResources.Events;

[ByRefEvent]
public sealed class InternalResourcesAmountChangeAttemptEvent(EntityUid uid, InternalResourcesData data, float amount) : CancellableEntityEventArgs
{
    public EntityUid EntityUid { get; private set; } = uid;
    public InternalResourcesData InternalResources { get; private set; } = data;
    public float ChangeAmount { get; private set; } = amount;

}

[ByRefEvent]
public record struct InternalResourcesRegenModifierEvent(EntityUid Uid, InternalResourcesData Data, float Modifier) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}

[ByRefEvent]
public record struct InternalResourcesThresholdMetEvent(EntityUid Uid, InternalResourcesData Data, string Threshold);

public record struct InternalResourcesAmountChangedEvent(EntityUid Uid, InternalResourcesData Data, float PreviousAmount, float NewAmount, float Delta);

public record struct InternalResourcesCapacityChangedEvent(EntityUid Uid, InternalResourcesData Data, float PreviousAmount, float NewAmount, float Delta);
