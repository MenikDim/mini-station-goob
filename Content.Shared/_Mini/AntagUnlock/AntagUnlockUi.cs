// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared._Mini.AntagUnlock;

[Serializable, NetSerializable]
public sealed class AntagUnlockPurchaseRequestEvent(string antagId) : EntityEventArgs
{
    public string AntagId { get; private set; } = antagId;
}
