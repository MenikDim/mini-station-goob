// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Networking;

/// <summary>
/// Stress test for PVS enter/exit while many entities are spawned near a moving player.
/// </summary>
[TestFixture]
public sealed class PvsStressTest
{
    private static readonly EntProtoId TestEnt = "Crowbar";

    [Test]
    public async Task SpawnAndMovePvsStressTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            EnableNetPvs = true,
            Destructive = true,
        });

        var server = pair.Server;
        var client = pair.Client;
        var map = await pair.CreateTestMap();
        var entMan = server.EntMan;
        var xformSys = entMan.System<SharedTransformSystem>();

        EntityUid player = default;
        await server.WaitPost(() =>
        {
            player = entMan.SpawnAtPosition("MobHuman", map.GridCoords);
            server.PlayerMan.SetAttachedEntity(pair.Player, player, true);
        });

        // Let PVS catch up after attaching the local player.
        await pair.RunTicksSync(30);

        var initialClientCount = client.EntMan.EntityCount;

        await server.WaitPost(() =>
        {
            for (var i = 0; i < 200; i++)
            {
                var coords = map.GridCoords.Offset(new System.Numerics.Vector2(i % 20, i / 20));
                entMan.SpawnAtPosition(TestEnt, coords);
            }
        });

        await pair.RunTicksSync(30);
        Assert.That(client.EntMan.EntityCount, Is.GreaterThan(initialClientCount));

        await server.WaitPost(() =>
        {
            for (var step = 0; step < 30; step++)
            {
                var pos = xformSys.GetWorldPosition(player);
                xformSys.SetWorldPosition(player, pos + new System.Numerics.Vector2(1, 0));
            }
        });

        await pair.RunTicksSync(30);

        var clientPlayer = pair.ToClientUid(player);
        Assert.That(client.EntMan.EntityExists(clientPlayer));

        await pair.CleanReturnAsync();
    }
}
