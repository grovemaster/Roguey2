using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Data.Door;
using JRogue.Manager.Floor;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Manager.Door
{
    public enum DoorPlayerActionResult
    {
        NotHandled = 0,
        FailedNoTurn = 1,
        Succeeded = 2,
    }

    /// <summary>Player door bump, open/close commands, and turn completion (formation-aware).</summary>
    public static class DoorPlayerInteraction
    {
        public static bool IsOrthogonallyAdjacent(Vector3Int from, Vector3Int doorCell)
        {
            Vector3Int delta = doorCell - from;
            if (delta.z != 0)
                return false;

            return (Mathf.Abs(delta.x) + Mathf.Abs(delta.y)) == 1;
        }

        public static DoorPlayerActionResult TryBumpOpenAndMove(BaseActor actor, Vector3Int doorCell)
        {
            DoorService doors = DoorService.Instance;
            if (doors == null || actor == null)
                return DoorPlayerActionResult.NotHandled;

            if (!doors.TryGetAtCell(doorCell, out DoorInstance door))
                return DoorPlayerActionResult.NotHandled;

            if (!IsOrthogonallyAdjacent(actor.GridPosition, doorCell))
                return DoorPlayerActionResult.NotHandled;

            if (door.State == DoorState.Open || door.State == DoorState.Broken)
                return DoorPlayerActionResult.NotHandled;

            if (!door.IsUnlocked)
            {
                Debug.Log($"{DoorService.LogPrefix} The door is locked.");
                return DoorPlayerActionResult.FailedNoTurn;
            }

            if (!doors.TryOpen(door))
                return DoorPlayerActionResult.FailedNoTurn;

            if (CanEnterDoorCell(actor, doorCell))
                actor.ApplyPositionChange(doorCell);

            Debug.Log($"{DoorService.LogPrefix} Bump-open-and-move at {doorCell} by {actor.name}.");
            CompletePlayerDoorAction(actor);
            return DoorPlayerActionResult.Succeeded;
        }

        public static DoorPlayerActionResult TryOpenAdjacent(BaseActor actor)
        {
            if (!TryFindAdjacentDoor(actor, DoorState.Closed, requireUnlocked: true, out DoorInstance door))
            {
                Debug.Log($"{DoorService.LogPrefix} No closed unlocked door adjacent.");
                return DoorPlayerActionResult.FailedNoTurn;
            }

            if (!DoorService.Instance.TryOpen(door))
                return DoorPlayerActionResult.FailedNoTurn;

            Debug.Log($"{DoorService.LogPrefix} Open command on '{door.DoorId}'.");
            CompletePlayerDoorAction(actor);
            return DoorPlayerActionResult.Succeeded;
        }

        public static DoorPlayerActionResult TryCloseAdjacent(BaseActor actor)
        {
            if (!TryFindAdjacentDoor(actor, DoorState.Open, requireUnlocked: true, out DoorInstance door))
            {
                Debug.Log($"{DoorService.LogPrefix} No open door adjacent.");
                return DoorPlayerActionResult.FailedNoTurn;
            }

            if (!CanClose(door))
            {
                Debug.Log($"{DoorService.LogPrefix} Something is in the way.");
                return DoorPlayerActionResult.FailedNoTurn;
            }

            if (!DoorService.Instance.TryClose(door))
                return DoorPlayerActionResult.FailedNoTurn;

            Debug.Log($"{DoorService.LogPrefix} Close command on '{door.DoorId}'.");
            CompletePlayerDoorAction(actor);
            return DoorPlayerActionResult.Succeeded;
        }

        public static bool CanClose(DoorInstance door)
        {
            if (door == null || door.State != DoorState.Open)
                return false;

            GridManager grid = GridManager.Instance;
            if (grid != null)
            {
                IBattleTarget occupant = grid.GetActorAt(door.Cell);
                if (occupant != null)
                    return false;
            }

            FloorItemPileService piles = FloorItemPileService.Instance;
            if (piles != null && piles.GetEntries(door.Cell).Count > 0)
                return false;

            return true;
        }

        static bool TryFindAdjacentDoor(
            BaseActor actor,
            DoorState requiredState,
            bool requireUnlocked,
            out DoorInstance found)
        {
            found = null;
            DoorService doors = DoorService.Instance;
            if (doors == null || actor == null)
                return false;

            Vector3Int[] ortho =
            {
                Vector3Int.up,
                Vector3Int.down,
                Vector3Int.left,
                Vector3Int.right,
            };

            for (int i = 0; i < ortho.Length; i++)
            {
                Vector3Int cell = actor.GridPosition + ortho[i];
                if (!doors.TryGetAtCell(cell, out DoorInstance door))
                    continue;

                if (door.State != requiredState)
                    continue;

                if (requireUnlocked && !door.IsUnlocked)
                    continue;

                found = door;
                return true;
            }

            return false;
        }

        static bool CanEnterDoorCell(BaseActor actor, Vector3Int doorCell)
        {
            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            if (map == null || !map.IsWalkable(doorCell))
                return false;

            IBattleTarget occupant = grid != null ? grid.GetActorAt(doorCell) : null;
            if (occupant != null && occupant.Owner != actor.gameObject)
                return false;

            return true;
        }

        public static void CompletePlayerDoorAction(BaseActor actor)
        {
            PartyManager party = PartyManager.Instance;
            BaseActor active = party != null ? party.GetActiveMember() : actor;
            PartyPlayerActionCompletion.CompleteActiveMemberAction(active ?? actor);
        }
    }
}
