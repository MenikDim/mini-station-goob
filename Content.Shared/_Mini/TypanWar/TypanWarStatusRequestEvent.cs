using Content.Shared._Mini.TypanWar;
using Robust.Shared.Serialization;

namespace Content.Shared._Mini.TypanWar;

/// <summary>
/// Client asks the server to re-send the current war HUD state.
/// </summary>
[Serializable, NetSerializable]
public sealed class TypanWarStatusRequestEvent : EntityEventArgs;
