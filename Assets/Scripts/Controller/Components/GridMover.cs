using System;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using UnityEngine;

namespace JRogue.Actors.Components
{
    /// <summary>
    /// Owns the grid coordinate of an actor, the transform mirror, and all
    /// register/unregister bookkeeping with <see cref="GridManager"/>.
    /// Knows nothing about combat, bumping, or turn order — those policies
    /// live on the actor that drives this component.
    /// </summary>
    [DisallowMultipleComponent]
    public class GridMover : MonoBehaviour
    {
        private IBattleTarget self;
        private GridManager gridManager;
        private Vector3Int gridPosition;

        public Vector3Int GridPosition => gridPosition;

        public event Action<Vector3Int, Vector3Int> Moved;

        private void Awake()
        {
            self = GetComponent<IBattleTarget>();
        }

        private void Start()
        {
            EnsureGridManager();
            gridPosition = Vector3Int.FloorToInt(transform.position);
            gridManager?.RegisterActor(gridPosition, self);
            SyncPosition();
        }

        private void OnDestroy()
        {
            // Re-read Instance instead of using the cached field: GridManager
            // may already have been destroyed during scene teardown, in which
            // case GridManager.Instance is null but the cached ref isn't.
            GridManager.Instance?.UnregisterActor(gridPosition);
        }

        public void SetGridPosition(Vector3Int newPos) => gridPosition = newPos;

        /// <summary>
        /// Attempts to move the actor to <paramref name="newPosition"/> and
        /// keeps <see cref="GridManager"/> consistent. If registration is
        /// rejected (e.g., another actor already occupies the cell) the move
        /// is aborted and the old registration is restored.
        /// </summary>
        public bool ApplyPositionChange(Vector3Int newPosition)
        {
            EnsureGridManager();
            if (gridManager == null) return false;

            Vector3Int oldPosition = gridPosition;
            if (oldPosition == newPosition) return true;

            // Only unregister if WE are the ones currently listed at the old cell.
            if (gridManager.GetActorAt(oldPosition) == self)
            {
                gridManager.UnregisterActor(oldPosition);
            }

            gridManager.RegisterActor(newPosition, self);

            // Verification: if registration failed (target cell was blocked),
            // restore the old cell and abort.
            if (gridManager.GetActorAt(newPosition) != self)
            {
                gridManager.RegisterActor(oldPosition, self);
                Debug.LogWarning($"[MOVE-ABORTED] {name} could not claim {newPosition}. Reverting to {oldPosition}.");
                return false;
            }

            gridPosition = newPosition;
            SyncPosition();

            Debug.Log($"{gameObject.name} moved from {oldPosition} to {newPosition}");
            Moved?.Invoke(oldPosition, newPosition);
            return true;
        }

        public void SyncPosition() =>
            transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0);

        /// <summary>
        /// Atomically swap the grid positions of <paramref name="a"/> and
        /// <paramref name="b"/> through the spatial hash, preserving the
        /// register/verify/revert invariants that <see cref="ApplyPositionChange"/>
        /// already enforces. Fires <see cref="Moved"/> on both actors.
        ///
        /// Use this for any "two actors trade tiles" operation (party swap,
        /// future displacement effects, etc.) so listeners and registration
        /// logic stay consistent with single-actor moves.
        /// </summary>
        public static bool TrySwap(GridMover a, GridMover b)
        {
            if (a == null || b == null || a == b) return false;

            GridManager grid = GridManager.Instance;
            if (grid == null) return false;

            Vector3Int posA = a.gridPosition;
            Vector3Int posB = b.gridPosition;
            if (posA == posB) return false;

            // 1. Lift both off the spatial hash so the swap doesn't transiently
            //    fail RegisterActor's conflict check.
            if (grid.GetActorAt(posA) == a.self) grid.UnregisterActor(posA);
            if (grid.GetActorAt(posB) == b.self) grid.UnregisterActor(posB);

            // 2. Place each at the other's old tile.
            grid.RegisterActor(posB, a.self);
            grid.RegisterActor(posA, b.self);

            // 3. Verify both registrations took.
            if (grid.GetActorAt(posB) != a.self || grid.GetActorAt(posA) != b.self)
            {
                grid.UnregisterActor(posA);
                grid.UnregisterActor(posB);
                grid.RegisterActor(posA, a.self);
                grid.RegisterActor(posB, b.self);
                Debug.LogWarning($"[SWAP-ABORTED] {a.name} <-> {b.name} swap rejected by GridManager.");
                return false;
            }

            // 4. Update internal state, sync visuals, fire Moved on both.
            a.gridPosition = posB;
            b.gridPosition = posA;
            a.SyncPosition();
            b.SyncPosition();

            Debug.Log($"[SWAP] {a.name} {posA} <-> {b.name} {posB}");
            a.Moved?.Invoke(posA, posB);
            b.Moved?.Invoke(posB, posA);
            return true;
        }

        private void EnsureGridManager()
        {
            if (gridManager == null) gridManager = GridManager.Instance;
        }
    }
}
