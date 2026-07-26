using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Traps;
using JRogue.UI.Gameplay;
using JRogue.World.Generation;
using JRogue.World.MapInteract;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.World.Rift
{
    /// <summary>Host-floor rift portal open/close, timers, wandering.</summary>
    public static class RiftPortalService
    {
        static string _openHostFloorId;
        static Vector3Int _openCell;
        static RiftDefinition _openRift;
        static int _openTurnsRemaining;
        static bool _isWandering;
        static int _wanderingDelayRemaining;
        static bool _wanderingCycleActive;

        /// <summary>
        /// After a player-triggered open, ignore step-on entry until move input is released once.
        /// Prevents sticky WASD/arrows from walking into the new portal on the same key-hold as the offering UI.
        /// </summary>
        static bool _hostEntryRequiresMoveRelease;
        static bool _hostEntryArmed;

        public static bool HasOpenPortal => !string.IsNullOrEmpty(_openHostFloorId);
        public static RiftDefinition OpenRift => _openRift;
        public static Vector3Int OpenCell => _openCell;

        public static void ResetForNewRun()
        {
            if (HasOpenPortal)
                ClosePortalOnHost(_openHostFloorId, removeVisual: true);
            ClearOpenState();
            _wanderingDelayRemaining = 0;
            _wanderingCycleActive = false;
        }

        public static void OnDungeonRunBegun(DungeonFloorDefinition startFloor)
        {
            ResetForNewRun();
            // Wandering evaluates when Floor 1 is active and day/gates allow — also try at run start
            // after floor activates via Tick / explicit call from floor activation.
        }

        public static void OnHostFloorActivated(DungeonFloorDefinition hostFloor)
        {
            if (hostFloor?.RiftPolicy == null || !hostFloor.RiftPolicy.HasRifts)
                return;
            if (RiftService.IsInsideRift)
                return;
            TrySpawnWanderingPortal(hostFloor, skipEligibility: false);
        }

        public static bool TryOpenPlayerTriggeredPortal(
            string hostFloorId,
            Vector3Int cell,
            RiftDefinition rift,
            out string denyReason)
        {
            denyReason = null;
            DungeonFloorDefinition host = DungeonFloorInstanceManager.Instance?.TryFindDefinition(hostFloorId);
            DungeonFloorRiftPolicy policy = host?.RiftPolicy;
            RiftSessionMeta meta = RiftSessionMeta.EnsureInstance();

            if (policy == null || !policy.HasRifts)
            {
                denyReason = "No rifts are available on this floor.";
                return false;
            }

            int day = ResolveDungeonDay();
            if (!RiftPortalGateLogic.PassesPlayerTrigger(
                    policy.HasRifts,
                    day,
                    policy.minDungeonDayToOpenPortal,
                    meta.WasPortalConsumedThisRun(hostFloorId),
                    meta.DungeonRunIndex,
                    meta.GetLastPortalOpenedRun(hostFloorId),
                    policy.minDungeonRunsBetweenPortals,
                    out denyReason))
                return false;

            if (rift == null)
                rift = policy.rifts[0];

            if (HasOpenPortal)
                ClosePortalOnHost(_openHostFloorId, removeVisual: true);

            if (!PlacePortal(hostFloorId, cell, rift, isWandering: false, policy.riftPortalOpenTurns))
            {
                denyReason = "Could not place rift portal.";
                return false;
            }

            meta.MarkPlayerPortalOpened(hostFloorId);
            _wanderingCycleActive = false;
            _wanderingDelayRemaining = 0;
            BeginPlayerTriggeredEntryGate();
            GameLogService.ActiveSession.Append("A rift portal opens.");
            Debug.Log($"[Rift] Player-triggered portal opened at {cell} → '{rift.riftId}'.");
            return true;
        }

        public static void TickAfterPlayerPhase()
        {
            if (RiftService.IsInsideRift)
            {
                RiftService.TickConditionalSummons();
                return;
            }

            DungeonFloorInstance active = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            DungeonFloorDefinition def = active?.Definition;
            if (def?.RiftPolicy == null || !def.RiftPolicy.HasRifts)
                return;

            string floorId = def.FloorId;
            DungeonFloorRiftPolicy policy = def.RiftPolicy;

            if (HasOpenPortal && _openHostFloorId == floorId)
            {
                _openTurnsRemaining--;
                if (_openTurnsRemaining > 0)
                    return;

                bool wasWandering = _isWandering;
                ClosePortalOnHost(floorId, removeVisual: true);
                if (wasWandering && !RiftSessionMeta.EnsureInstance().WasPortalConsumedThisRun(floorId))
                {
                    _wanderingCycleActive = true;
                    _wanderingDelayRemaining = policy.wanderingRespawnDelayTurns;
                }

                return;
            }

            if (_wanderingCycleActive && _wanderingDelayRemaining > 0)
            {
                _wanderingDelayRemaining--;
                if (_wanderingDelayRemaining <= 0)
                    TrySpawnWanderingPortal(def, skipEligibility: true);
            }
        }

        public static void ClosePortalOnHost(string hostFloorId, bool removeVisual)
        {
            if (!HasOpenPortal)
                return;
            if (!string.IsNullOrEmpty(hostFloorId) && _openHostFloorId != hostFloorId)
                return;

            DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            if (floor != null
                && floor.Definition != null
                && floor.Definition.FloorId == _openHostFloorId)
            {
                floor.UnregisterPortalAt(_openCell);
                AdjacentMapInteractableService.Instance?.Unregister(_openCell);
                if (removeVisual)
                    AdjacentMapInteractableService.Instance?.ClearOverlay(_openCell);
            }

            PortalPathingBan.UnregisterPortalCell(_openCell);
            ClearOpenState();
        }

        public static bool TryActivateHostPortal(BaseActor member, PortalInteractable portal)
        {
            if (member == null || portal == null || _openRift == null)
                return false;

            if (!IsHostPortalEntryArmed())
                return false;

            string riftId = RiftTransitionIds.ParseRiftIdFromHostLink(portal.PortalLinkId);
            if (string.IsNullOrEmpty(riftId) || _openRift.riftId != riftId)
                return false;

            string hostId = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance()?.Definition?.FloorId;
            bool ok = RiftService.TryEnterFromHostPortal(_openRift, hostId, member.GridPosition, member);
            if (ok)
                PartyPlayerActionCompletion.CompleteActiveMemberAction(member);
            return ok;
        }

        /// <summary>
        /// Arms entry after move keys are released; if a party member is already standing on the portal, activates.
        /// </summary>
        public static void TickHostPortalEntryArming()
        {
            if (!HasOpenPortal || !_hostEntryRequiresMoveRelease || _hostEntryArmed)
                return;

            if (IsAnyMoveDirectionHeld())
                return;

            _hostEntryArmed = true;
            TryActivateIfPartyStandingOnOpenPortal();
        }

        public static bool IsHostPortalEntryArmed()
        {
            if (!_hostEntryRequiresMoveRelease)
                return true;

            if (!_hostEntryArmed && !IsAnyMoveDirectionHeld())
                _hostEntryArmed = true;

            return _hostEntryArmed;
        }

        public static bool ShouldBlockHostPortalOccupancy(Vector3Int cell)
        {
            if (!HasOpenPortal || cell != _openCell)
                return false;

            return _hostEntryRequiresMoveRelease && !IsHostPortalEntryArmed();
        }

        static void BeginPlayerTriggeredEntryGate()
        {
            _hostEntryRequiresMoveRelease = true;
            _hostEntryArmed = !IsAnyMoveDirectionHeld();
        }

        static void ClearHostPortalEntryGate()
        {
            _hostEntryRequiresMoveRelease = false;
            _hostEntryArmed = true;
        }

        static void TryActivateIfPartyStandingOnOpenPortal()
        {
            AdjacentMapInteractableService mapInteract = AdjacentMapInteractableService.Instance;
            if (mapInteract == null
                || !mapInteract.TryGetAtCell(_openCell, out IAdjacentMapInteractable interactable)
                || interactable is not PortalInteractable portal)
                return;

            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.GridPosition != _openCell)
                    continue;
                if (!PortalEntryService.CanMemberTriggerStepOnPortal(member, party))
                    continue;

                TryActivateHostPortal(member, portal);
                return;
            }
        }

        static bool IsAnyMoveDirectionHeld()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null
                && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed
                    || kb.upArrowKey.isPressed || kb.downArrowKey.isPressed
                    || kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed))
                return true;

            Gamepad pad = Gamepad.current;
            if (pad == null)
                return false;

            if (pad.leftStick.ReadValue().sqrMagnitude > 0.25f)
                return true;

            return pad.dpad.ReadValue().sqrMagnitude > 0.25f;
        }

        static void TrySpawnWanderingPortal(DungeonFloorDefinition hostFloor, bool skipEligibility)
        {
            if (hostFloor?.RiftPolicy == null || !hostFloor.RiftPolicy.HasRifts || HasOpenPortal)
                return;

            DungeonFloorRiftPolicy policy = hostFloor.RiftPolicy;
            RiftSessionMeta meta = RiftSessionMeta.EnsureInstance();
            string floorId = hostFloor.FloorId;

            if (meta.WasPortalConsumedThisRun(floorId))
                return;

            if (!skipEligibility)
            {
                if (!RiftPortalGateLogic.PassesWandering(
                        policy.HasRifts,
                        ResolveDungeonDay(),
                        policy.minDungeonDayToOpenPortal,
                        meta.WasPortalConsumedThisRun(floorId),
                        meta.DungeonRunIndex,
                        meta.GetLastRiftEnteredRun(floorId),
                        policy.minDungeonRunsBeforeWandering,
                        out _))
                    return;
            }

            if (!TryPickRandomCell(hostFloor, out Vector3Int cell))
                return;

            var rng = new System.Random(meta.DungeonRunIndex * 397 ^ floorId.GetHashCode());
            RiftDefinition rift = policy.PickRandomRift(rng);
            if (rift == null)
                return;

            if (!PlacePortal(floorId, cell, rift, isWandering: true, policy.riftPortalOpenTurns))
                return;

            meta.MarkWanderingPortalOpened(floorId);
            _wanderingCycleActive = true;
            Debug.Log($"[Rift] Wandering portal at {cell} → '{rift.riftId}'.");
        }

        static bool PlacePortal(
            string hostFloorId,
            Vector3Int cell,
            RiftDefinition rift,
            bool isWandering,
            int openTurns)
        {
            DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            if (floor?.Definition == null || floor.Definition.FloorId != hostFloorId)
                return false;
            if (rift?.riftFloorDefinition == null)
                return false;

            InteractableTileService.Instance?.UnregisterAtCell(cell);
            AdjacentMapInteractableService.Instance?.Unregister(cell);
            AdjacentMapInteractableService.Instance?.ClearOverlay(cell);
            floor.UnregisterMapInteractableAt(cell);

            floor.PlacePortalVisual(cell);
            string linkId = RiftTransitionIds.HostToRift(rift.riftId);
            var portal = new PortalInteractable(
                cell,
                linkId,
                rift.riftFloorDefinition.FloorId,
                "Portal (Rift)");
            floor.RegisterPortal(portal);

            AdjacentMapInteractableService service = AdjacentMapInteractableService.Instance;
            if (service != null)
            {
                service.SetOverlayMap(floor.InteractableOverlayMap);
                service.Register(cell, portal);
            }

            DungeonFloorInstanceManager.Instance?.ConfigureFloors(
                new[] { rift.riftFloorDefinition },
                replaceAll: false);

            PortalPathingBan.RegisterPortalCell(cell);
            DungeonFloorInstanceManager.Instance?.ApplyPortalVisibilityOnActiveFloor();

            _openHostFloorId = hostFloorId;
            _openCell = cell;
            _openRift = rift;
            _openTurnsRemaining = openTurns;
            _isWandering = isWandering;
            return true;
        }

        static bool TryPickRandomCell(DungeonFloorDefinition hostFloor, out Vector3Int cell)
        {
            cell = default;
            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            if (map == null)
                return false;

            int w = 50;
            int h = 80;
            if (hostFloor.LayoutStamp != null)
            {
                w = hostFloor.LayoutStamp.Width;
                h = hostFloor.LayoutStamp.Height;
            }

            var candidates = new List<Vector3Int>();
            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    Vector3Int c = new Vector3Int(x, y, 0);
                    if (!map.IsWalkable(c))
                        continue;
                    if (grid != null && grid.GetActorAt(c) != null)
                        continue;
                    if (TrapService.Instance != null && TrapService.Instance.IsFloorTrapAt(c))
                        continue;
                    if (HazardService.Instance != null && HazardService.Instance.HasHazardAt(c))
                        continue;
                    if (JRogue.GridFeatures.MapCellOccupancy.BlocksActorEntry(c))
                        continue;
                    if (PortalPathingBan.IsPortalCell(c))
                        continue;
                    candidates.Add(c);
                }
            }

            if (candidates.Count == 0)
                return false;

            cell = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        static void ClearOpenState()
        {
            _openHostFloorId = null;
            _openCell = default;
            _openRift = null;
            _openTurnsRemaining = 0;
            _isWandering = false;
            ClearHostPortalEntryGate();
        }

        static int ResolveDungeonDay()
        {
            DungeonTimeService time = DungeonTimeService.Instance;
            return time == null ? 1 : time.ElapsedCycles + 1;
        }
    }
}
