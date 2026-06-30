using Robust.Shared.Serialization;

namespace Content.Shared.RPSX.DarkForces.Ratvar.Construction;

[Serializable, NetSerializable]
public sealed class RatvarConstructionSelectedMessage(string selectedItem) : BoundUserInterfaceMessage
{
    public string SelectedItem { get; private set; } = selectedItem;
}

[Serializable, NetSerializable]
public enum RatvarConstructionUiKey : byte
{
    Key,
}
