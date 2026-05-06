using System.Collections.Generic;
using UnityEngine;
using JRogue.Core.Actor;

namespace JRogue.Manager.Grid
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        private static readonly Vector3Int[] EightNeighbors =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, -1, 0),
        };

        /// <summary>
        /// Eight-direction offsets (cardinals first, then diagonals) for pathfinding and grid queries.
        /// </summary>
        public static IReadOnlyList<Vector3Int> EightDirectionOffsets => EightNeighbors;

        // The Spatial Hash: Maps a coordinate to the actor standing there
        private Dictionary<Vector3Int, IBattleTarget> actorMap = new Dictionary<Vector3Int, IBattleTarget>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void UpdateActorPosition(IBattleTarget actor, Vector3Int oldPos, Vector3Int newPos)
        {
            if (actorMap.ContainsKey(oldPos) && actorMap[oldPos] == actor)
            {
                actorMap.Remove(oldPos);
            }
            actorMap[newPos] = actor;
        }

        //public void RegisterActor(Vector3Int pos, IBattleTarget actor) => actorMap[pos] = actor;

        public void RegisterActor(Vector3Int pos, IBattleTarget actor)
        {
            if (actorMap.ContainsKey(pos))
            {
                // If the occupant is NOT the one trying to register, we have a conflict
                if (actorMap[pos] != actor)
                {
                    Debug.LogError($"[GRID-CONFLICT] {actor.Owner.name} failed to register at {pos}. " +
                                   $"Occupied by {actorMap[pos].Owner.name}.");
                    return;
                }
            }

            actorMap[pos] = actor;
        }

        public void UnregisterActor(Vector3Int pos) => actorMap.Remove(pos);

        public IBattleTarget GetActorAt(Vector3Int pos)
        {
            actorMap.TryGetValue(pos, out IBattleTarget actor);
            return actor;
        }

        public bool IsOccupied(Vector3Int pos)
        {
            return actorMap.ContainsKey(pos);
        }

        // Used for AOE like Fireball
        public IEnumerable<IBattleTarget> GetAllActors() => actorMap.Values;

        /// <summary>
        /// Yields the eight neighboring grid cells (including diagonals) around <paramref name="origin"/>.
        /// </summary>
        public IEnumerable<Vector3Int> EnumerateEightNeighborCells(Vector3Int origin)
        {
            for (int i = 0; i < EightNeighbors.Length; i++)
                yield return origin + EightNeighbors[i];
        }
    }
}