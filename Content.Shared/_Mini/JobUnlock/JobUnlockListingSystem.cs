// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Mini.JobUnlock;

public sealed class JobUnlockListingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly Dictionary<ProtoId<JobPrototype>, JobUnlockListingEntry> _byJob = new();

    public override void Initialize()
    {
        base.Initialize();
        RebuildCache();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReload);
    }

    private void OnProtoReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<JobUnlockCatalogPrototype>() || args.WasModified<JobPrototype>())
            RebuildCache();
    }

    private void RebuildCache()
    {
        _byJob.Clear();

        if (!_proto.TryIndex<JobUnlockCatalogPrototype>(JobUnlockCatalogPrototype.DefaultId, out var catalog))
            return;

        var catalogJobs = new HashSet<ProtoId<JobPrototype>>();
        foreach (var tier in catalog.Tiers)
        {
            if (tier.Jobs == null)
                continue;

            foreach (var jobId in tier.Jobs)
                catalogJobs.Add(jobId);
        }

        foreach (var job in _proto.EnumeratePrototypes<JobPrototype>())
        {
            if (!job.SetPreference && !catalogJobs.Contains(job.ID))
                continue;

            _byJob[job.ID] = new JobUnlockListingEntry
            {
                JobId = job.ID,
            };
        }
    }

    public bool TryGetListing(ProtoId<JobPrototype> jobId, [NotNullWhen(true)] out JobUnlockListingEntry? entry)
    {
        return _byJob.TryGetValue(jobId, out entry);
    }
}
