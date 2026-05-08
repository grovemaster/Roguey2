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

        private void EnsureGridManager()
        {
            if (gridManager == null) gridManager = GridManager.Instance;
        }
    }
}
