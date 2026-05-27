using System;
using System.Collections.Generic;
using JRogue.Core.Actor;
using JRogue.Interactables;
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
    [DefaultExecutionOrder(100)]
    public class GridMover : MonoBehaviour
    {
        private static readonly List<Vector3Int> CellBufferA = new List<Vector3Int>(16);
        private static readonly List<Vector3Int> CellBufferB = new List<Vector3Int>(16);

        private IBattleTarget self;
        private IGridFootprint footprint;
        private GridManager gridManager;
        private Vector3Int gridPosition;

        public Vector3Int GridPosition => gridPosition;

        public event Action<Vector3Int, Vector3Int> Moved;

        private void Awake()
        {
            self = GetComponent<IBattleTarget>();
            footprint = GetComponent<IGridFootprint>();
        }

        private void Start()
        {
            EnsureGridManager();
            if (footprint == null)
                footprint = GetComponent<IGridFootprint>();

            gridPosition = footprint != null
                ? GridFootprintUtility.ResolvePlacementAnchor(transform.position, footprint)
                : Vector3Int.FloorToInt(transform.position - new Vector3(0.5f, 0.5f, 0f));

            RegisterAtCurrentAnchor();
            SyncFootprintPose();
        }

        private void OnDestroy()
        {
            GridManager grid = GridManager.Instance;
            if (grid == null || self == null)
                return;

            if (footprint != null)
                grid.UnregisterFootprint(self);
            else
                grid.UnregisterActor(gridPosition);
        }

        public void SetGridPosition(Vector3Int newPos) => gridPosition = newPos;

        /// <summary>
        /// Attempts to move the actor to <paramref name="newPosition"/> (anchor) and
        /// keeps <see cref="GridManager"/> consistent. If registration is
        /// rejected the move is aborted and the old registration is restored.
        /// </summary>
        public bool ApplyPositionChange(Vector3Int newPosition)
        {
            GridManager grid = GridManager.Instance;
            if (grid == null) return false;

            if (self == null) self = GetComponent<IBattleTarget>();
            if (footprint == null) footprint = GetComponent<IGridFootprint>();

            Vector3Int oldPosition = gridPosition;
            if (oldPosition == newPosition) return true;

            if (footprint != null)
            {
                GridFootprintUtility.GetOccupiedCells(
                    oldPosition,
                    footprint.Layout,
                    footprint.FootprintWidth,
                    footprint.FootprintHeight,
                    footprint.Facing,
                    CellBufferA);
                GridFootprintUtility.GetOccupiedCells(
                    newPosition,
                    footprint.Layout,
                    footprint.FootprintWidth,
                    footprint.FootprintHeight,
                    footprint.Facing,
                    CellBufferB);

                if (BlocksInteractableOccupancy(CellBufferB))
                    return false;

                if (!grid.TryMoveFootprint(self, CellBufferA, CellBufferB))
                {
                    Debug.LogWarning(
                        $"[MOVE-ABORTED] {name} footprint could not claim anchor {newPosition}. Reverting to {oldPosition}.");
                    return false;
                }
            }
            else if (BlocksInteractableOccupancy(newPosition))
            {
                return false;
            }
            else if (!grid.TryMoveRegistration(self, oldPosition, newPosition))
            {
                Debug.LogWarning($"[MOVE-ABORTED] {name} could not claim {newPosition}. Reverting to {oldPosition}.");
                return false;
            }

            gridManager = grid;
            gridPosition = newPosition;
            SyncFootprintPose();

            Debug.Log($"{gameObject.name} moved from {oldPosition} to {newPosition}");
            Moved?.Invoke(oldPosition, newPosition);
            return true;
        }

        public void SyncPosition()
        {
            if (footprint == null)
                footprint = GetComponent<IGridFootprint>();

            if (footprint != null && !GridFootprintUtility.IsSingleCell(footprint))
                transform.position = GridFootprintUtility.GetFootprintAnchorWorldPosition(gridPosition);
            else
                transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0);
        }

        /// <summary>
        /// Snaps the transform to the footprint anchor and aligns the <see cref="FootprintPoseUtility.VisualChildName"/> child.
        /// </summary>
        public void SyncFootprintPose()
        {
            if (footprint == null)
                footprint = GetComponent<IGridFootprint>();

            SyncPosition();
            if (footprint != null)
            {
                FootprintPoseUtility.ApplyVisual(
                    gridPosition,
                    footprint.Layout,
                    footprint.FootprintWidth,
                    footprint.FootprintHeight,
                    footprint.Facing,
                    transform);
            }
        }

        void RegisterAtCurrentAnchor()
        {
            if (gridManager == null || self == null)
                return;

            if (footprint != null)
            {
                GridFootprintUtility.GetOccupiedCells(footprint, CellBufferA);
                gridManager.TryRegisterFootprint(self, CellBufferA);
            }
            else
                gridManager.RegisterActor(gridPosition, self);
        }

        /// <summary>
        /// Atomically swap the grid positions of <paramref name="a"/> and
        /// <paramref name="b"/> through the spatial hash, preserving the
        /// register/verify/revert invariants that <see cref="ApplyPositionChange"/>
        /// already enforces. Fires <see cref="Moved"/> on both actors.
        /// Party swap remains 1×1 only in v0.
        /// </summary>
        public static bool TrySwap(GridMover a, GridMover b)
        {
            if (a == null || b == null || a == b) return false;

            GridManager grid = GridManager.Instance;
            if (grid == null) return false;

            if (a.self == null) a.self = a.GetComponent<IBattleTarget>();
            if (b.self == null) b.self = b.GetComponent<IBattleTarget>();
            if (a.footprint != null || b.footprint != null)
            {
                Debug.LogWarning("[SWAP-ABORTED] Multi-tile footprint actors cannot swap tiles in v0.");
                return false;
            }

            Vector3Int posA = a.gridPosition;
            Vector3Int posB = b.gridPosition;
            if (posA == posB) return false;

            if (IsSameBattleTarget(grid.GetActorAt(posA), a.self)) grid.UnregisterActor(posA);
            if (IsSameBattleTarget(grid.GetActorAt(posB), b.self)) grid.UnregisterActor(posB);

            grid.RegisterActor(posB, a.self);
            grid.RegisterActor(posA, b.self);

            if (!IsSameBattleTarget(grid.GetActorAt(posB), a.self) || !IsSameBattleTarget(grid.GetActorAt(posA), b.self))
            {
                grid.UnregisterActor(posA);
                grid.UnregisterActor(posB);
                grid.RegisterActor(posA, a.self);
                grid.RegisterActor(posB, b.self);
                Debug.LogWarning($"[SWAP-ABORTED] {a.name} <-> {b.name} swap rejected by GridManager.");
                return false;
            }

            a.gridPosition = posB;
            b.gridPosition = posA;
            a.SyncFootprintPose();
            b.SyncFootprintPose();

            Debug.Log($"[SWAP] {a.name} {posA} <-> {b.name} {posB}");
            a.Moved?.Invoke(posA, posB);
            b.Moved?.Invoke(posB, posA);
            return true;
        }

        private void EnsureGridManager()
        {
            gridManager = GridManager.Instance;
        }

        private static bool IsSameBattleTarget(IBattleTarget a, IBattleTarget b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return true;
            GameObject ownerA = a.Owner;
            GameObject ownerB = b.Owner;
            return ownerA != null && ownerB != null && ownerA == ownerB;
        }

        static bool BlocksInteractableOccupancy(Vector3Int cell)
        {
            InteractableTileService interactables = InteractableTileService.Instance;
            return interactables != null && interactables.BlocksOccupancy(cell);
        }

        static bool BlocksInteractableOccupancy(List<Vector3Int> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (BlocksInteractableOccupancy(cells[i]))
                    return true;
            }

            return false;
        }
    }
}
