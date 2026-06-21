using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Visibility.Algorithm;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// One-shot diagnostics after dungeon floor activation — party light inventory, zone sync, and visibility.
    /// Tagged <c>[Lighting:EntryDiag]</c> so logs are easy to filter in the Console.
    /// </summary>
    public static class DungeonEntryLightingDiagnostics
    {
        public const string Tag = "[Lighting:EntryDiag]";

        public static void LogAfterFloorActivate(
            DungeonFloorInstance instance,
            DungeonFloorDefinition definition,
            bool zoneCompositeSyncApplied)
        {
            LogPartyLightInventoryAudit();
            LogLightingServiceState();
            LogZoneCompositeState(instance, definition, zoneCompositeSyncApplied);
            LogPartyCellLighting(instance);
            LogVisibilitySummary(instance);
        }

        public static void LogPartyLightInventoryAudit()
        {
            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null || party.partyMembers.Count == 0)
            {
                Debug.LogWarning($"{Tag} Party light audit: no party members.");
                return;
            }

            var log = new StringBuilder();
            log.Append("Party light inventory audit — ");
            int lightSourcesInBag = 0;
            int equippedLightSources = 0;
            int activeCarriedEmitters = 0;

            for (int m = 0; m < party.partyMembers.Count; m++)
            {
                BaseActor member = party.partyMembers[m];
                if (member == null)
                    continue;

                string memberName = member.DisplayName ?? member.name;
                InventoryManager inventory = member.GetComponent<InventoryManager>();
                EquipmentManager equipment = member.GetComponent<EquipmentManager>();

                if (inventory != null)
                {
                    IReadOnlyList<ItemInstance> carried = inventory.CarriedItems;
                    for (int i = 0; i < carried.Count; i++)
                    {
                        ItemInstance item = carried[i];
                        if (item?.Definition is not LightSourceItemData lightDef)
                            continue;

                        lightSourcesInBag++;
                        log.Append('\n').Append($"  {memberName} bag: {FormatLightItem(item, lightDef, slot: null, emitting: false)}");
                    }
                }

                if (equipment == null)
                    continue;

                foreach (KeyValuePair<EquipmentSlot, ItemInstance> pair in equipment.EquippedSnapshot)
                {
                    ItemInstance item = pair.Value;
                    if (item?.Definition is not LightSourceItemData lightDef)
                        continue;

                    equippedLightSources++;
                    bool emitting = LightSourceItemRules.ShouldEmitCarriedLight(item, pair.Key, isEquipped: true);
                    if (emitting)
                        activeCarriedEmitters++;

                    log.Append('\n').Append(
                        $"  {memberName} equipped [{pair.Key}]: {FormatLightItem(item, lightDef, pair.Key, emitting)}");
                }
            }

            LightingService lighting = LightingService.Instance;
            int serviceCarriedCount = lighting != null ? lighting.CarriedEmitterCount : 0;

            log.Append('\n').Append(
                $"Summary: bagLightSources={lightSourcesInBag} equippedLightSources={equippedLightSources} " +
                $"rulesActiveEmitters={activeCarriedEmitters} serviceCarriedEmitters={serviceCarriedCount}");

            if (lightSourcesInBag == 0 && equippedLightSources == 0 && serviceCarriedCount == 0)
                log.Append("\n  CONFIRMED: no torch or light-source item in party inventory or equipment.");
            else if (activeCarriedEmitters == 0 && serviceCarriedCount == 0)
                log.Append("\n  Light-source item(s) present but none actively emitting carried light.");
            else
                log.Append("\n  WARNING: party has active carried light — visibility may exceed zone ambient.");

            Debug.Log($"{Tag}{log}");
        }

        static string FormatLightItem(
            ItemInstance item,
            LightSourceItemData definition,
            EquipmentSlot? slot,
            bool emitting)
        {
            string slotLabel = slot.HasValue ? slot.Value.ToString() : "bag";
            return $"{definition.itemName} (slot={slotLabel} passive={definition.IsPassiveEquippedEmitter} " +
                   $"helmetTurns={item.HelmetLightTurnsRemaining} emitting={emitting})";
        }

        static void LogLightingServiceState()
        {
            LightingService lighting = LightingService.Instance;
            if (lighting == null)
            {
                Debug.LogWarning($"{Tag} LightingService missing.");
                return;
            }

            AmbientRegion region0 = lighting.GetOrCreateAmbientRegion(lighting.DefaultFloorAmbientRegionId);
            Debug.Log(
                $"{Tag} LightingService defaultRegion={lighting.DefaultFloorAmbientRegionId} " +
                $"serializedDefaultAmbient={lighting.DefaultFloorAmbientLight} " +
                $"region0CurrentAmbient={region0.CurrentAmbientLight} " +
                $"carriedEmitters={lighting.CarriedEmitterCount}");
        }

        static void LogZoneCompositeState(
            DungeonFloorInstance instance,
            DungeonFloorDefinition definition,
            bool zoneCompositeSyncApplied)
        {
            if (definition == null || definition.LayoutMode != FloorLayoutMode.ZoneComposite)
            {
                Debug.Log($"{Tag} Floor layout={definition?.LayoutMode} — zone composite sync N/A.");
                return;
            }

            int zoneMapCells = instance?.ZoneCellMapSnapshot?.Count ?? 0;
            string regionSummary = ZoneCompositeLightingSync.DescribeZoneAmbientRegionsForLog(
                LightingService.Instance,
                definition.ZoneLayout);

            Debug.Log(
                $"{Tag} ZoneComposite syncApplied={zoneCompositeSyncApplied} " +
                $"zoneMapCells={zoneMapCells} ambientRegions={regionSummary}");

            if (zoneMapCells == 0)
            {
                Debug.LogWarning(
                    $"{Tag} Zone cell map snapshot is EMPTY — zone ambient/tags were not applied. " +
                    "Regenerate the floor (new expedition) to rebuild zone lighting.");
            }
            else if (!zoneCompositeSyncApplied)
            {
                Debug.LogWarning(
                    $"{Tag} ZoneCompositeLightingSync returned false despite zone map — check applicator counts.");
            }
        }

        static void LogPartyCellLighting(DungeonFloorInstance instance)
        {
            PartyManager party = PartyManager.Instance;
            LightingService lighting = LightingService.Instance;
            if (party?.partyMembers == null || party.partyMembers.Count == 0 || lighting == null)
                return;

            BaseActor lead = party.partyMembers[0];
            Vector3Int origin = new Vector3Int(lead.GridPosition.x, lead.GridPosition.y, 0);
            string partyZone = null;
            if (instance != null)
                instance.TryGetZoneId(origin, out partyZone);

            LogDetailedCell("party", origin, partyZone, lighting);

            Vector3Int[] offsets =
            {
                new(0, 1, 0),
                new(1, 0, 0),
                new(0, -1, 0),
                new(-1, 0, 0),
            };
            string[] labels = { "party+N", "party+E", "party+S", "party+W" };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3Int cell = origin + offsets[i];
                string zoneId = null;
                if (instance != null)
                    instance.TryGetZoneId(cell, out zoneId);
                LogDetailedCell(labels[i], cell, zoneId, lighting);
            }

            if (partyZone == "northern_dark")
                LogNorthernDarkSample(instance, lighting, origin);
        }

        static void LogNorthernDarkSample(DungeonFloorInstance instance, LightingService lighting, Vector3Int origin)
        {
            int sampled = 0;
            int recvNonZero = 0;
            int emitNonZero = 0;
            int unregistered = 0;
            int missingZoneTag = 0;

            for (int dy = -5; dy <= 5; dy++)
            {
                for (int dx = -5; dx <= 5; dx++)
                {
                    Vector3Int cell = new Vector3Int(origin.x + dx, origin.y + dy, 0);
                    if (!instance.TryGetZoneId(cell, out string zoneId) || zoneId != "northern_dark")
                        continue;

                    sampled++;
                    if (!lighting.TryGetCellData(cell, out LightCellData data) || !data.IsReceiver)
                        unregistered++;
                    else if (string.IsNullOrEmpty(data.ZoneId))
                        missingZoneTag++;

                    int recv = lighting.GetReceivedLight(cell);
                    int emit = lighting.GetEmitLight(cell);
                    if (recv > 0)
                        recvNonZero++;
                    if (emit > 0)
                        emitNonZero++;
                }
            }

            Debug.Log(
                $"{Tag} northern_dark 11x11 sample around {origin}: cells={sampled} recv>0={recvNonZero} " +
                $"emit>0={emitNonZero} unregisteredReceivers={unregistered} missingRegistryZoneTag={missingZoneTag}");
        }

        static void LogDetailedCell(string label, Vector3Int cell, string zoneId, LightingService lighting)
        {
            int recv = lighting.GetReceivedLight(cell);
            int emit = lighting.GetEmitLight(cell);
            bool inRegistry = lighting.TryGetCellData(cell, out LightCellData data);
            string registryZone = inRegistry ? data.ZoneId ?? "(empty)" : "n/a";
            bool isReceiver = inRegistry && data.IsReceiver;
            bool isEmitter = inRegistry && data.IsEmitter;
            int storedRecv = isReceiver ? data.ReceivedLight : -1;
            int ambientRegion = isReceiver ? data.AmbientRegionId : -1;

            Debug.Log(
                $"{Tag} {label} {cell} zone={zoneId ?? "?"} emit={emit} recv={recv} " +
                $"registry=[receiver={isReceiver} emitter={isEmitter} storedRecv={storedRecv} " +
                $"ambientRegion={ambientRegion} registryZone={registryZone}]");
        }

        static void LogVisibilitySummary(DungeonFloorInstance instance)
        {
            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            LightingService lighting = LightingService.Instance;
            MapManager map = MapManager.Instance;
            PartyManager party = PartyManager.Instance;
            if (visibility == null || lighting == null || map == null || party?.partyMembers == null
                || party.partyMembers.Count == 0)
            {
                return;
            }

            BaseActor lead = party.partyMembers[0];
            Vector3Int origin = new Vector3Int(lead.GridPosition.x, lead.GridPosition.y, 0);
            int threshold = visibility.BaseVisibilityThreshold;

            ShadowCaster.IsOpaque isOpaque = pos => map.BlocksLineOfSight(pos);
            int sight = visibility.GetEffectiveSightRange(lead, origin);
            List<Vector3Int> losCells = ShadowCaster.GetVisibleTiles(origin, sight, isOpaque);

            int liveVisible = 0;
            int litVisible = 0;
            int unregisteredInLos = 0;
            int fallbackLitInLos = 0;
            int northernDarkLitInLos = 0;

            for (int i = 0; i < losCells.Count; i++)
            {
                Vector3Int cell = losCells[i];
                bool occupied = IlluminationVisibilityLogic.IsPartyMemberOccupyingCell(cell);
                int emit = lighting.GetEmitLight(cell);
                int recv = lighting.GetReceivedLight(cell);
                bool inRegistry = lighting.TryGetCellData(cell, out LightCellData data) && data.IsReceiver;

                if (!inRegistry)
                    unregisteredInLos++;

                if (!IlluminationVisibilityLogic.IsCellLiveVisible(emit, recv, occupied))
                    continue;

                liveVisible++;
                if (IlluminationVisibilityLogic.IsCellFullyBright(emit, recv, occupied, threshold))
                    litVisible++;

                if (!inRegistry && recv > 0)
                    fallbackLitInLos++;

                if (instance != null && instance.TryGetZoneId(cell, out string zoneId)
                    && zoneId == "northern_dark" && recv > 0)
                {
                    northernDarkLitInLos++;
                }
            }

            Debug.Log(
                $"{Tag} LOS from {origin} sight={sight} losCells={losCells.Count} " +
                $"liveVisible={liveVisible} litVisible={litVisible} dimVisible={liveVisible - litVisible} " +
                $"unregisteredInLos={unregisteredInLos} unregisteredWithRecv>0={fallbackLitInLos} " +
                $"northernDarkLitInLos={northernDarkLitInLos} threshold={threshold}");
        }
    }
}
