// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared._Mini.JobUnlock;

[Serializable, NetSerializable]
public sealed class JobUnlockPurchaseRequestEvent(string jobId) : EntityEventArgs
{
    public string JobId { get; } = jobId;
}
