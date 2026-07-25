using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Interactables;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Spawn;
using JRogue.UI.Gameplay;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.LotF
{
    /// <summary>
    /// Day-start LotF evaluation, once-per-run ledger, and random footprint spawn.
    /// Orthogonal to monster spawn schedules.
    /// </summary>
    public static class LordOfTheFloorService
    {
        public const string LogPrefix = "[LotF]";

        static readonly LordOfTheFloorRunLedger Ledger = new LordOfTheFloorRunLedger();
        static readonly List<Vector3Int> CandidateScratch = new List<Vector3Int>(256);

        public static LordOfTheFloorRunLedger RunLedger => Ledger;

        public static void ResetForNewRun()
        {
            Ledger.Reset();
            Debug.Log($"{LogPrefix} Run ledger reset.");
        }

        public static void NotifyHostEnded(string lotfId)
        {
            if (string.IsNullOrEmpty(lotfId))
                return;

            Ledger.MarkConsumed(lotfId);
            Debug.Log($"{LogPrefix} Host ended — slot consumed for '{lotfId}'.");
        }

        /// <summary>Called after the day-schedule spawn pass when a new dungeon day begins.</summary>
        public static void EvaluateOnDayStarted(int dungeonDay, int runSeed)
        {
            DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            DungeonFloorDefinition def = floor?.Definition;
            if (def == null)
            {
                Debug.Log($"{LogPrefix} Day {dungeonDay}: no active floor definition — skip.");
                return;
            }

            LordOfTheFloorDefinition[] lords = def.LordsOfTheFloor;
            if (lords == null || lords.Length == 0)
                return;

            string activeFloorId = DungeonRunState.Instance != null
                ? DungeonRunState.Instance.ActiveFloorId
                : def.FloorId;

            int living = ResolveLivingPartyCount();
            var rng = new System.Random(unchecked(runSeed * 397 ^ dungeonDay * 31 ^ activeFloorId.GetHashCode()));

            for (int i = 0; i < lords.Length; i++)
            {
                LordOfTheFloorDefinition lord = lords[i];
                if (lord == null || string.IsNullOrEmpty(lord.LotfId))
                    continue;

                TrySummon(lord, dungeonDay, living, activeFloorId, floor, rng);
            }
        }

        public static bool TrySummon(
            LordOfTheFloorDefinition lord,
            int dungeonDay,
            int livingPartyMembers,
            string activeFloorId,
            DungeonFloorInstance floor,
            System.Random rng)
        {
            if (lord == null)
                return false;

            LordOfTheFloorRunSlot slot = Ledger.Get(lord.LotfId);
            if (!LordOfTheFloorSummonGateLogic.Passes(
                    dungeonDay,
                    lord.MinimumDungeonDay,
                    activeFloorId,
                    lord.HostFloorId,
                    livingPartyMembers,
                    lord.MinimumLivingPartyMembers,
                    slot,
                    out string failReason))
            {
                Debug.Log($"{LogPrefix} '{lord.LotfId}' gate fail day={dungeonDay}: {failReason}");
                return false;
            }

            if (lord.SpawnDefinition == null || lord.SpawnDefinition.enemyPrefab == null)
            {
                Debug.LogWarning($"{LogPrefix} '{lord.LotfId}' missing spawn definition/prefab.");
                return false;
            }

            if (floor == null)
            {
                Debug.LogWarning($"{LogPrefix} '{lord.LotfId}' no floor instance.");
                return false;
            }

            if (!TryPickRandomAnchor(lord.SpawnDefinition.enemyPrefab, rng, out Vector3Int anchor))
            {
                Debug.LogWarning(
                    $"{LogPrefix} '{lord.LotfId}' no valid 2×2 (or footprint) cell — slot remains Available.");
                return false;
            }

            if (!EnemySpawnService.TrySpawnAtExactCell(
                    lord.SpawnDefinition,
                    anchor,
                    out EnemyController spawned,
                    floor.EnemyContainer))
            {
                Debug.LogWarning(
                    $"{LogPrefix} '{lord.LotfId}' spawn failed at ({anchor.x},{anchor.y}) — slot remains Available.");
                return false;
            }

            if (!Ledger.TryMarkSummoned(lord.LotfId))
            {
                Debug.LogWarning($"{LogPrefix} '{lord.LotfId}' ledger race — destroying spawn.");
                Object.Destroy(spawned.gameObject);
                return false;
            }

            LordOfTheFloorHost host = spawned.GetComponent<LordOfTheFloorHost>();
            if (host == null)
                host = spawned.gameObject.AddComponent<LordOfTheFloorHost>();
            host.Initialize(lord.LotfId);

            spawned.SetDisplayName(lord.AppearanceExamineName);

            GameLogService.ActiveSession.Append(lord.AppearanceCombatLogLine);
            Debug.Log(
                $"{LogPrefix} Summoned '{lord.LotfId}' ({lord.DisplayName}) at ({anchor.x},{anchor.y}) day={dungeonDay}.");
            return true;
        }

        static int ResolveLivingPartyCount()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return 0;

            PartyCapacityService capacity = PartyCapacityService.Instance;
            if (capacity != null)
                return capacity.GetLivingMemberCount(party);

            int count = 0;
            if (party.partyMembers == null)
                return 0;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null)
                    continue;
                if (member.stats != null && member.stats.currentHP <= 0)
                    continue;
                count++;
            }

            return count;
        }

        static bool TryPickRandomAnchor(EnemyController prefab, System.Random rng, out Vector3Int anchor)
        {
            anchor = default;
            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            if (map == null || grid == null || map.FloorMap == null || prefab == null || rng == null)
                return false;

            CandidateScratch.Clear();
            BoundsInt bounds = map.FloorMap.cellBounds;
            InteractableTileService interactables = InteractableTileService.Instance;

            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (!EnemySpawnPlacementResolver.CanPlaceFootprintAt(
                            cell,
                            prefab.footprintLayout,
                            prefab.footprintWidth,
                            prefab.footprintHeight,
                            prefab.currentFacing,
                            map,
                            grid,
                            interactables))
                    {
                        continue;
                    }

                    CandidateScratch.Add(cell);
                }
            }

            if (CandidateScratch.Count == 0)
                return false;

            int index = rng.Next(CandidateScratch.Count);
            anchor = CandidateScratch[index];
            return true;
        }
    }
}
