using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Data.Door;
using JRogue.Data.Enemy;
using UnityEngine;

namespace JRogue.Manager.Door
{
    public static class EnemyDoorInteraction
    {
        public static bool TryInteractBeforeMove(BaseActor actor, Vector3Int targetCell)
        {
            DoorService doors = DoorService.Instance;
            if (doors == null || actor == null)
                return false;

            if (!doors.TryGetAtCell(targetCell, out DoorInstance door))
                return false;

            if (door.State == DoorState.Open || door.State == DoorState.Broken)
                return false;

            EnemyDoorCapability capability = ResolveCapability(actor);
            if (capability == EnemyDoorCapability.CanBreak)
            {
                if (doors.TryBreak(door, actor.name))
                {
                    Debug.Log($"{DoorService.LogPrefix} {actor.name} broke door '{door.DoorId}'.");
                    return true;
                }

                return false;
            }

            if (capability == EnemyDoorCapability.CanOpen && door.IsUnlocked)
            {
                if (doors.TryOpen(door))
                {
                    Debug.Log($"{DoorService.LogPrefix} {actor.name} opened door '{door.DoorId}'.");
                    return true;
                }
            }

            return false;
        }

        static EnemyDoorCapability ResolveCapability(BaseActor actor)
        {
            if (actor is EnemyController enemy && enemy.Species != null)
                return enemy.Species.doorCapability;

            return EnemyDoorCapability.None;
        }
    }
}
