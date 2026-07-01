// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using Content.Shared._Mini.JobUnlock;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client._Mini.JobUnlock;

public sealed class JobUnlockClientSystem : EntitySystem
{
    public void RequestUnlock(ProtoId<JobPrototype> jobId)
    {
        RaiseNetworkEvent(new JobUnlockPurchaseRequestEvent(jobId));
    }
}
