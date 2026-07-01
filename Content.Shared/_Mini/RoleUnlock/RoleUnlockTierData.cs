// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using System.Collections.Generic;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Mini.RoleUnlock;

[DataDefinition]
public sealed partial class RoleUnlockTierData
{
    [DataField]
    public List<ProtoId<JobPrototype>>? Jobs;

    [DataField]
    public List<ProtoId<AntagPrototype>>? Antags;

    [DataField(required: true)]
    public int MaxCost;

    [DataField]
    public bool Command;

    /// <summary>
    /// For jobs not listed explicitly: minimum display weight (inclusive).
    /// </summary>
    [DataField]
    public int? MinWeight;
}

[DataDefinition]
public sealed partial class RoleUnlockDefaultTierData
{
    [DataField]
    public int MaxCost = 10;

    [DataField]
    public bool Command;
}

[DataDefinition]
public sealed partial class RoleUnlockPricingSettings
{
    [DataField]
    public float HoursPerCoinStep = 4f;

    [DataField]
    public int MinCost = 1;

    [DataField]
    public int RegularCoinsPerStep = 2;

    [DataField]
    public int CommandCoinsPerStep = 4;
}
