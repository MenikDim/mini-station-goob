#nullable enable
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.GameRules;

/// <summary>
///     Regression tests for preset game rule handling in round flow:
///     1. Preset rules must be added before stations post-init during map load
///        (roundstart station variation depends on this ordering).
///     2. Rules queued separately before roundstart (e.g. admin addgamerule in the lobby)
///        must survive and start with the round.
/// </summary>
[TestFixture]
public sealed class PresetRuleOrderTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: gamePreset
  id: PresetRuleOrderTestPreset
  name: Preset Rule Order Test Preset
  description: """"
  showInVote: false
  rules:
  - PresetRuleOrderTestRule

- type: entity
  id: PresetRuleOrderTestRule
  parent: BaseGameRule
  categories: [ GameRules ]
  components:
  - type: GameRule
    minPlayers: 0
  - type: PresetRuleOrderTestRule

- type: entity
  id: PresetRuleOrderAdminQueuedRule
  parent: BaseGameRule
  categories: [ GameRules ]
  components:
  - type: GameRule
    minPlayers: 0
";

    [Test]
    public async Task PresetRulesAddedBeforeStationsAndQueuedRulesSurviveTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        var ticker = server.System<GameTicker>();
        var orderSystem = server.System<PresetRuleOrderTestRuleSystem>();

        var defaultPreset = server.CfgMan.GetCVar(CCVars.GameLobbyDefaultPreset);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

        orderSystem.RuleAddedAtStationPostInit = false;

        // Queue an extra rule in the lobby, like an admin using addgamerule.
        var queuedRule = default(EntityUid);
        await server.WaitPost(() => queuedRule = ticker.AddGameRule("PresetRuleOrderAdminQueuedRule"));

        await pair.WaitClientCommand("toggleready True");
        await pair.WaitCommand("setgamepreset PresetRuleOrderTestPreset");
        await pair.WaitCommand("startround");
        await pair.RunTicksSync(10);

        Assert.Multiple(() =>
        {
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

            // The preset's rules have to exist by the time stations post-init,
            // otherwise roundstart station variation silently never runs.
            Assert.That(orderSystem.RuleAddedAtStationPostInit, Is.True,
                "Preset game rules were not added before stations were initialized.");

            // The lobby-queued rule must have been started with the round, not wiped.
            Assert.That(entMan.EntityExists(queuedRule), Is.True,
                "Admin-queued game rule was deleted before roundstart.");
            Assert.That(entMan.HasComponent<ActiveGameRuleComponent>(queuedRule), Is.True,
                "Admin-queued game rule was not started with the round.");
        });

        await server.WaitPost(() => ticker.SetGamePreset((GamePresetPrototype?) null));
        server.CfgMan.SetCVar(CCVars.GameLobbyDefaultPreset, defaultPreset);
        await pair.CleanReturnAsync();
    }
}

public sealed class PresetRuleOrderTestRuleSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public bool RuleAddedAtStationPostInit;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        // Mimics RoundstartStationVariationRuleSystem: variation only runs if the rule
        // is already added when the station initializes.
        if (_gameTicker.IsGameRuleAdded<PresetRuleOrderTestRuleComponent>())
            RuleAddedAtStationPostInit = true;
    }
}

[RegisterComponent]
public sealed partial class PresetRuleOrderTestRuleComponent : Component;
