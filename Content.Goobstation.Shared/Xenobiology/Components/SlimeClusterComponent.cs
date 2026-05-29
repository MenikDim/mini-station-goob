// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Xenobiology.Components;

/// <summary>
/// A xenobiology slime formed from many slimes of the same breed merged together.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeClusterComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Count = 1;

    [DataField]
    public EntProtoId PeelPrototype = "MobSlimeXenobioAdult";

    [DataField]
    public TimeSpan PeelDelay = TimeSpan.FromSeconds(2);
}
