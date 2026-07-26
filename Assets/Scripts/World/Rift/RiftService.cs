using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Spawn;
using JRogue.UI.Gameplay;
using JRogue.World.Generation;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Rift
{
    /// <summary>Rift enter/exit lifecycle, boss → exit portal, conditional summons.</summary>
    public static class RiftService
    {
        static RiftDefinition _activeRift;
        static string _hostFloorId;
        static Vector3Int _hostEntryCell;
        static bool _insideRift;
        static bool _dungeonEndedWhileInRift;
        static bool _exitPortalOpened;
        static readonly System.Collections.Generic.HashSet<string> _firedConditions =
            new System.Collections.Generic.HashSet<string>();

        public static bool IsInsideRift => _insideRift;
        public static bool DungeonEndedWhileInRift => _dungeonEndedWhileInRift;
        public static RiftDefinition ActiveRift => _activeRift;
        public static string HostFloorId => _hostFloorId;
        public static Vector3Int HostEntryCell => _hostEntryCell;

        public static void ResetForNewRun()
        {
            // Keep session meta; clear only active rift session state if not inside.
            if (_insideRift)
                return;
            ClearActiveSession();
        }

        public static void ClearActiveSession()
        {
            _activeRift = null;
            _hostFloorId = null;
            _hostEntryCell = default;
            _insideRift = false;
            _dungeonEndedWhileInRift = false;
            _exitPortalOpened = false;
            _firedConditions.Clear();
        }

        public static void NotifyDungeonTimeExpiredWhilePossiblyInRift()
        {
            if (_insideRift)
            {
                _dungeonEndedWhileInRift = true;
                Debug.Log("[Rift] Dungeon time expired while inside rift — forced town deferred.");
            }
        }

        public static bool TryEnterFromHostPortal(
            RiftDefinition rift,
            string hostFloorId,
            Vector3Int hostEntryCell,
            BaseActor triggeringMember)
        {
            if (rift == null || rift.riftFloorDefinition == null || triggeringMember == null)
                return false;

            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            if (manager == null)
                return false;

            _activeRift = rift;
            _hostFloorId = hostFloorId;
            _hostEntryCell = hostEntryCell;
            _exitPortalOpened = false;
            _firedConditions.Clear();

            string linkId = RiftTransitionIds.HostToRift(rift.riftId);
            EnsureRiftFloorRegistered(manager, rift);

            RiftPortalService.ClosePortalOnHost(hostFloorId, removeVisual: true);
            RiftSessionMeta.EnsureInstance().MarkRiftEntered(hostFloorId);

            EnsureRiftEntryBinding(rift, linkId);

            bool ok = manager.TryTransitionPortalForWholeParty(linkId, rift.riftFloorDefinition.FloorId);
            if (!ok)
            {
                ClearActiveSession();
                return false;
            }

            _insideRift = true;
            GameLogService.ActiveSession.Append(rift.EnterCombatLogLine);
            Debug.Log($"[Rift] Entered '{rift.riftId}' from host '{hostFloorId}' at {hostEntryCell}.");

            SpawnInitialContent(rift);
            return true;
        }

        public static bool TryExitToHost(BaseActor triggeringMember)
        {
            if (!_insideRift || _activeRift == null || triggeringMember == null)
                return false;

            if (!_exitPortalOpened)
            {
                Debug.Log("[Rift] Exit blocked — boss not defeated.");
                return false;
            }

            if (_dungeonEndedWhileInRift
                || (DungeonTimeService.Instance != null
                    && DungeonTimeService.Instance.DungeonRunActive == false))
            {
                ClearActiveSession();
                DungeonExitService.RequestForcedExitToTown();
                return true;
            }

            // Also check if time already expired on the clock while we deferred.
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            if (manager == null)
                return false;

            string hostId = _hostFloorId ?? DungeonFloorTransitionIds.Floor01Id;
            string exitLink = RiftTransitionIds.RiftExitToHost(_activeRift.riftId);

            Vector3Int returnCell = ResolveReturnCell(hostId, _hostEntryCell);

            // Host may be parked — bind return arrival on instance and definition.
            if (manager.TryGetFloorInstance(hostId, out DungeonFloorInstance hostInstance) && hostInstance != null)
            {
                hostInstance.StoreArrivalBinding(new PortalArrivalBinding
                {
                    portalLinkId = exitLink,
                    arrivalAnchor = returnCell,
                });
            }

            DungeonFloorDefinition hostDef = manager.TryFindDefinition(hostId);
            hostDef?.SetOrReplaceArrivalBinding(exitLink, returnCell);

            RiftDefinition leaving = _activeRift;
            string host = hostId;
            ClearActiveSession();

            bool ok = manager.TryTransitionPortalForWholeParty(exitLink, host);
            if (ok)
            {
                PartyPlayerActionCompletion.CompleteActiveMemberAction(triggeringMember);
                Debug.Log($"[Rift] Exited '{leaving.riftId}' to host '{host}' at {returnCell}.");
            }

            return ok;
        }

        public static void NotifyBossDied(RiftDefinition rift)
        {
            if (!_insideRift || rift == null || _activeRift == null)
                return;
            if (rift.riftId != _activeRift.riftId)
                return;
            if (_exitPortalOpened)
                return;

            _exitPortalOpened = true;
            OpenExitPortal(rift);
            GameLogService.ActiveSession.Append("An exit portal opens.");
            Debug.Log($"[Rift] Boss defeated in '{rift.riftId}' — exit portal opened.");
        }

        public static void TickConditionalSummons()
        {
            if (!_insideRift || _activeRift?.conditionalSummons == null)
                return;

            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return;

            for (int i = 0; i < _activeRift.conditionalSummons.Length; i++)
            {
                RiftConditionalSummonSpec spec = _activeRift.conditionalSummons[i];
                if (spec.spawns == null || string.IsNullOrEmpty(spec.conditionId))
                    continue;
                if (_firedConditions.Contains(spec.conditionId))
                    continue;

                if (!AnyMemberInRoom(party, spec.roomMinInclusive, spec.roomMaxInclusive))
                    continue;

                _firedConditions.Add(spec.conditionId);
                SpawnSpecs(spec.spawns, _activeRift);
                Debug.Log($"[Rift] Conditional summon '{spec.conditionId}' fired.");
            }
        }

        static bool AnyMemberInRoom(PartyManager party, Vector3Int min, Vector3Int max)
        {
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor m = party.partyMembers[i];
                if (m == null || m.stats == null || m.stats.currentHP <= 0)
                    continue;
                Vector3Int c = m.GridPosition;
                if (c.x >= min.x && c.x <= max.x && c.y >= min.y && c.y <= max.y)
                    return true;
            }

            return false;
        }

        static void SpawnInitialContent(RiftDefinition rift)
        {
            if (rift.initialSpawns == null)
                return;
            SpawnSpecs(rift.initialSpawns, rift);
        }

        static void SpawnSpecs(RiftEnemySpawnSpec[] specs, RiftDefinition rift)
        {
            DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            Transform parent = floor != null ? floor.EnemyContainer : null;

            for (int i = 0; i < specs.Length; i++)
            {
                RiftEnemySpawnSpec spec = specs[i];
                if (spec.spawnDefinition == null)
                    continue;

                if (!EnemySpawnService.TrySpawnAtExactCell(spec.spawnDefinition, spec.cell, out EnemyController enemy, parent))
                {
                    Debug.LogWarning($"[Rift] Failed to spawn at {spec.cell}");
                    continue;
                }

                if (spec.isBoss && enemy != null)
                {
                    RiftBossHost host = enemy.gameObject.GetComponent<RiftBossHost>();
                    if (host == null)
                        host = enemy.gameObject.AddComponent<RiftBossHost>();
                    host.Initialize(rift);
                }
            }
        }

        static void OpenExitPortal(RiftDefinition rift)
        {
            DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            if (floor == null)
                return;

            Vector3Int cell = rift.exitPortalCell;
            floor.PlacePortalVisual(cell);
            string linkId = RiftTransitionIds.RiftExitToHost(rift.riftId);
            string hostId = _hostFloorId ?? DungeonFloorTransitionIds.Floor01Id;
            var portal = new PortalInteractable(cell, linkId, hostId, "Portal (Rift Exit)");
            floor.RegisterPortal(portal);

            AdjacentMapInteractableService service = AdjacentMapInteractableService.Instance;
            if (service != null)
            {
                service.SetOverlayMap(floor.InteractableOverlayMap);
                service.Register(cell, portal);
            }

            PortalPathingBan.RegisterPortalCell(cell);
        }

        static Vector3Int ResolveReturnCell(string hostFloorId, Vector3Int preferred)
        {
            DungeonFloorInstance active = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            // Host map is parked while inside the rift — cannot validate walkability here.
            if (active?.Definition == null || active.Definition.FloorId != hostFloorId)
                return preferred;

            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            if (map == null)
                return preferred;

            if (IsValidReturn(map, grid, preferred))
                return preferred;

            // Spiral search for nearest valid tile
            for (int radius = 1; radius <= 12; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                            continue;
                        Vector3Int c = preferred + new Vector3Int(dx, dy, 0);
                        if (IsValidReturn(map, grid, c))
                            return c;
                    }
                }
            }

            return preferred;
        }

        static bool IsValidReturn(MapManager map, GridManager grid, Vector3Int cell)
        {
            if (!map.IsWalkable(cell))
                return false;
            if (JRogue.Traps.TrapService.Instance != null
                && JRogue.Traps.TrapService.Instance.IsFloorTrapAt(cell))
                return false;
            if (JRogue.Hazards.HazardService.Instance != null
                && JRogue.Hazards.HazardService.Instance.HasHazardAt(cell))
                return false;
            if (grid != null && grid.GetActorAt(cell) != null)
                return false;
            return true;
        }

        static void EnsureRiftFloorRegistered(DungeonFloorInstanceManager manager, RiftDefinition rift)
        {
            if (manager.TryFindDefinition(rift.riftFloorDefinition.FloorId) != null)
                return;
            manager.ConfigureFloors(new[] { rift.riftFloorDefinition }, replaceAll: false);
        }

        static void EnsureRiftEntryBinding(RiftDefinition rift, string linkId)
        {
            DungeonFloorDefinition riftFloor = rift.riftFloorDefinition;
            if (riftFloor == null)
                return;

            riftFloor.SetOrReplaceArrivalBinding(linkId, rift.entryAnchor);

            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            if (manager != null
                && manager.TryGetFloorInstance(riftFloor.FloorId, out DungeonFloorInstance instance)
                && instance != null)
            {
                instance.StoreArrivalBinding(new PortalArrivalBinding
                {
                    portalLinkId = linkId,
                    arrivalAnchor = rift.entryAnchor,
                });
            }
        }

        static DungeonFloorInstance FindInstance(DungeonFloorInstanceManager manager, string floorId)
        {
            if (manager != null && manager.TryGetFloorInstance(floorId, out DungeonFloorInstance instance))
                return instance;
            return null;
        }
    }
}
