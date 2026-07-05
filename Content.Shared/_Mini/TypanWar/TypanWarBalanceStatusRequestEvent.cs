using Robust.Shared.Serialization;

namespace Content.Shared._Mini.TypanWar;

/// <summary>
/// Client requests current faction balance status for late join UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class TypanWarBalanceStatusRequestEvent : EntityEventArgs;
