// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using Content.Shared._Mini.AntagUnlock;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client._Mini.AntagUnlock;

public sealed class AntagUnlockClientSystem : EntitySystem
{
    public void RequestUnlock(ProtoId<AntagPrototype> antagId)
    {
        RaiseNetworkEvent(new AntagUnlockPurchaseRequestEvent(antagId));
    }
}
