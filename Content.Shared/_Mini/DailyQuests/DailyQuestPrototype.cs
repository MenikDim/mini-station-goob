// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Mini.DailyQuests;

[Prototype]
public sealed partial class DailyQuestPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public DailyQuestType QuestType;

    [DataField]
    public ProtoId<JobIconPrototype>? Icon;

    /// <summary>
    /// Optional sprite override when a job icon prototype is not suitable.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Sprite;

    [DataField]
    public LocId Name = string.Empty;

    [DataField]
    public LocId Description = string.Empty;

    [DataField]
    public ProtoId<JobPrototype>? RequiredJob;

    [DataField]
    public int TargetCount = 1;

    [DataField]
    public int RewardCoins = 1;

    /// <summary>
    /// Minimum active playtime required for round-based passive quests.
    /// </summary>
    [DataField]
    public TimeSpan MinRoundPlaytime = TimeSpan.FromMinutes(15);

    [DataField]
    public float Weight = 1f;
}
