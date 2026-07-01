// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using System.Collections.Generic;
using Content.Shared._Mini.RoleUnlock;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Mini.AntagUnlock;

[DataDefinition]
public sealed partial class AntagUnlockListingEntry
{
    [DataField(required: true)]
    public ProtoId<AntagPrototype> AntagId;
}

[Prototype("antagUnlockCatalog")]
public sealed partial class AntagUnlockCatalogPrototype : IPrototype
{
    public const string DefaultId = "Default";

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public RoleUnlockPricingSettings Pricing = new();

    [DataField]
    public RoleUnlockDefaultTierData DefaultTier = new();

    [DataField]
    public List<RoleUnlockTierData> Tiers = new();

    [DataField]
    public bool UseShopCostMultiplier = true;

    [DataField]
    public int ShopCostMultiplier = 5;
}
