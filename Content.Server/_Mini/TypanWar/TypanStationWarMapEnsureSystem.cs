// SPDX-FileCopyrightText: 2025 Mini Station
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._TT.AdditionalMap;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Prototypes;

namespace Content.Server._Mini.TypanWar;

/// <summary>
/// Ensures Typan Station War always has both an NT and a Typan station, even when the selected
/// main map has no <see cref="AdditionalMapPrototype"/> entry (e.g. Dev).
/// </summary>
public sealed class TypanStationWarMapEnsureSystem : EntitySystem
{
    private const string WarPresetId = "TypanStationWar";
    private static readonly ProtoId<GameMapPrototype> TypanMapId = "Typan";
    private static readonly ProtoId<GameMapPrototype> NtFallbackMapId = "Empty";

    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LoadingMapsEvent>(OnLoadingMaps, after: [typeof(AdditionalMapLoaderSystem)]);
    }

    private void OnLoadingMaps(LoadingMapsEvent ev)
    {
        if (_ticker.CurrentPreset?.ID != WarPresetId || ev.Maps.Count == 0)
            return;

        var mainMapId = ev.Maps[0].ID;

        // typanpool.yml handles supplemental Typan for maps that declare additionalMap.
        if (_prototypes.HasIndex<AdditionalMapPrototype>(mainMapId))
            return;

        if (mainMapId == TypanMapId)
            LoadSupplemental(NtFallbackMapId);
        else
            LoadSupplemental(TypanMapId);
    }

    private void LoadSupplemental(ProtoId<GameMapPrototype> mapId)
    {
        if (!_prototypes.TryIndex(mapId, out var map))
        {
            Log.Error($"Typan Station War: failed to load supplemental map '{mapId}' — prototype missing.");
            return;
        }

        Log.Info($"Typan Station War: loading supplemental map '{mapId}'.");
        _ticker.LoadGameMap(map, out _, options: new DeserializationOptions { InitializeMaps = true });
    }
}
