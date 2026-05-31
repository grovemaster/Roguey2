using System.Collections.Generic;
using JRogue.Actors;
using JRogue.GridFeatures;
using JRogue.World.Altar;
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

            return false;
        }

        public void PaintOverlay(Vector3Int cell, Sprite sprite)
        {
            if (altarOverlayMap == null || sprite == null)
                return;

            GridOverlayPainter.Paint(altarOverlayMap, cell, tile: null, sprite: sprite);
        }

        public void ClearOverlay(Vector3Int cell)
        {
            if (altarOverlayMap == null)
                return;

            GridOverlayPainter.Clear(altarOverlayMap, cell);
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
