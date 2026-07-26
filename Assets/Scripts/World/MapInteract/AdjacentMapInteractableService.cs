using System.Collections.Generic;
using JRogue.Actors;
using JRogue.GridFeatures;
using JRogue.World.Altar;
using JRogue.World.Generation;
using JRogue.World.Rift;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.MapInteract
{
    public sealed class AdjacentMapInteractableService : MonoBehaviour
    {
        public static AdjacentMapInteractableService Instance { get; private set; }

        [SerializeField] Tilemap altarOverlayMap;

        readonly Dictionary<Vector3Int, IAdjacentMapInteractable> _byCell =
            new Dictionary<Vector3Int, IAdjacentMapInteractable>();

        readonly Dictionary<Vector3Int, Sprite> _overlaySprites =
            new Dictionary<Vector3Int, Sprite>();

        readonly List<Vector3Int> _overlayRefreshScratch = new List<Vector3Int>(16);
        readonly List<Vector3Int> _neighborScratch = new List<Vector3Int>(4);
        readonly List<IAdjacentMapInteractable> _adjacentScratch = new List<IAdjacentMapInteractable>(4);
        readonly List<IAdjacentMapInteractable> _candidateScratch = new List<IAdjacentMapInteractable>(4);

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (altarOverlayMap != null)
                GridOverlayPainter.ConfigureRenderer(altarOverlayMap);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetOverlayMap(Tilemap overlay)
        {
            altarOverlayMap = overlay;
            if (altarOverlayMap != null)
                GridOverlayPainter.ConfigureRenderer(altarOverlayMap);
        }

        public void Register(Vector3Int cell, IAdjacentMapInteractable interactable)
        {
            if (interactable == null)
                return;

            _byCell[cell] = interactable;
        }

        public void Unregister(Vector3Int cell) => _byCell.Remove(cell);

        public bool TryGetAtCell(Vector3Int cell, out IAdjacentMapInteractable interactable) =>
            _byCell.TryGetValue(cell, out interactable);

        public bool BlocksOccupancy(Vector3Int cell)
        {
            if (!_byCell.TryGetValue(cell, out IAdjacentMapInteractable interactable))
                return false;

            if (interactable is AltarInteractable altar)
            {
                AltarDefinition def = altar.Instance?.Definition;
                return def == null || def.blocksOccupancy;
            }

            if (interactable is PortalInteractable
                && RiftPortalService.ShouldBlockHostPortalOccupancy(cell))
                return true;

            return false;
        }

        public void PaintOverlay(Vector3Int cell, Sprite sprite)
        {
            if (altarOverlayMap == null || sprite == null)
                return;

            _overlaySprites[cell] = sprite;
            if (IsCellCurrentlyVisible(cell))
                GridOverlayPainter.Paint(altarOverlayMap, cell, tile: null, sprite: sprite);
            else
                GridOverlayPainter.Clear(altarOverlayMap, cell);
        }

        public void ClearOverlay(Vector3Int cell)
        {
            _overlaySprites.Remove(cell);
            if (altarOverlayMap == null)
                return;

            GridOverlayPainter.Clear(altarOverlayMap, cell);
        }

        /// <summary>
        /// Show altar/map-interact overlays only on currently visible cells (same fog rule as doors/traps).
        /// </summary>
        public void RefreshOverlayVisibility()
        {
            if (altarOverlayMap == null || _overlaySprites.Count == 0)
                return;

            _overlayRefreshScratch.Clear();
            foreach (Vector3Int cell in _overlaySprites.Keys)
                _overlayRefreshScratch.Add(cell);

            for (int i = 0; i < _overlayRefreshScratch.Count; i++)
            {
                Vector3Int cell = _overlayRefreshScratch[i];
                if (!_overlaySprites.TryGetValue(cell, out Sprite sprite) || sprite == null)
                    continue;

                if (IsCellCurrentlyVisible(cell))
                    GridOverlayPainter.Paint(altarOverlayMap, cell, tile: null, sprite: sprite);
                else
                    GridOverlayPainter.Clear(altarOverlayMap, cell);
            }
        }

        static bool IsCellCurrentlyVisible(Vector3Int cell)
        {
            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            if (visibility == null)
                return true;

            return visibility.IsVisible(cell);
        }

        public IReadOnlyList<IAdjacentMapInteractable> GetOrthogonalAdjacentInteractables(Vector3Int actorCell)
        {
            _adjacentScratch.Clear();
            MapInteractOrthogonal.CopyNeighborCells(actorCell, _neighborScratch);

            for (int i = 0; i < _neighborScratch.Count; i++)
            {
                Vector3Int cell = _neighborScratch[i];
                if (!_byCell.TryGetValue(cell, out IAdjacentMapInteractable interactable))
                    continue;

                _adjacentScratch.Add(interactable);
            }

            _adjacentScratch.Sort(CompareInteractables);
            return _adjacentScratch;
        }

        public IReadOnlyList<IAdjacentMapInteractable> GetInteractableCandidates(BaseActor actor)
        {
            _candidateScratch.Clear();
            if (actor == null)
                return _candidateScratch;

            IReadOnlyList<IAdjacentMapInteractable> adjacent =
                GetOrthogonalAdjacentInteractables(actor.GridPosition);

            for (int i = 0; i < adjacent.Count; i++)
            {
                IAdjacentMapInteractable interactable = adjacent[i];
                if (interactable != null && interactable.CanInteract(actor))
                    _candidateScratch.Add(interactable);
            }

            return _candidateScratch;
        }

        static int CompareInteractables(IAdjacentMapInteractable a, IAdjacentMapInteractable b)
        {
            int order = a.SortOrder.CompareTo(b.SortOrder);
            if (order != 0)
                return order;

            return string.Compare(a.ListLabel, b.ListLabel, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
