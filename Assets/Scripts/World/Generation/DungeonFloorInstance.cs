using System.Collections.Generic;
using JRogue.Actors.Components;
using JRogue.Controller.Enemy;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.World.MapInteract;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation
{
    public sealed class DungeonFloorInstance : MonoBehaviour
    {
        [SerializeField] DungeonFloorDefinition definition;
        [SerializeField] Transform enemyContainer;
        [SerializeField] Transform dynamicViewsRoot;
        [SerializeField] Tilemap floorMap;
        [SerializeField] Tilemap wallMap;
        [SerializeField] Tilemap hazardOverlayMap;
        [SerializeField] Tilemap interactableOverlayMap;
        [SerializeField] Tilemap trapOverlayMap;
        [SerializeField] Tilemap doorOverlayMap;

        readonly Dictionary<string, PortalArrivalBinding> _arrivalBindings =
            new Dictionary<string, PortalArrivalBinding>();
        readonly List<PortalInteractable> _portals = new List<PortalInteractable>();

        bool _isGenerated;
        Vector3Int _playerStart;

        public DungeonFloorDefinition Definition => definition;
        public string FloorId => definition != null ? definition.FloorId : name;
        public bool IsGenerated => _isGenerated;
        public Vector3Int PlayerStart => _playerStart;
        public Transform EnemyContainer => enemyContainer;
        public Transform DynamicViewsRoot => dynamicViewsRoot;

        public DungeonFloorTilemaps Tilemaps => new DungeonFloorTilemaps(
            floorMap,
            wallMap,
            hazardOverlayMap,
            interactableOverlayMap,
            trapOverlayMap,
            doorOverlayMap);

        public void Configure(DungeonFloorDefinition floorDefinition)
        {
            definition = floorDefinition;
            gameObject.name = floorDefinition != null ? floorDefinition.FloorId : name;
        }

        public static DungeonFloorInstance CreateUnder(Transform parent, DungeonFloorDefinition floorDefinition)
        {
            var root = new GameObject(floorDefinition != null ? floorDefinition.FloorId : "dungeon_floor");
            root.transform.SetParent(parent, false);

            var instance = root.AddComponent<DungeonFloorInstance>();
            instance.Configure(floorDefinition);
            instance.BuildHierarchy();
            return instance;
        }

        public void EnsureHierarchyBuilt()
        {
            if (floorMap != null && enemyContainer != null)
                return;

            BuildHierarchy();
        }

        void BuildHierarchy()
        {
            Transform gridRootTransform = transform.Find("Grid");
            GameObject gridRoot;
            if (gridRootTransform != null)
                gridRoot = gridRootTransform.gameObject;
            else
            {
                gridRoot = new GameObject("Grid");
                gridRoot.transform.SetParent(transform, false);
            }

            Grid grid = gridRoot.GetComponent<Grid>();
            if (grid == null)
                grid = gridRoot.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            floorMap = floorMap != null ? floorMap : FindOrCreateTilemap(gridRoot.transform, "Floor", sortingOrder: 0);
            wallMap = wallMap != null ? wallMap : FindOrCreateTilemap(gridRoot.transform, "Wall", sortingOrder: 1);
            hazardOverlayMap = hazardOverlayMap != null
                ? hazardOverlayMap
                : FindOrCreateTilemap(gridRoot.transform, "HazardOverlay", sortingOrder: 2);
            interactableOverlayMap = interactableOverlayMap != null
                ? interactableOverlayMap
                : FindOrCreateTilemap(gridRoot.transform, "InteractableOverlay", sortingOrder: 3);
            trapOverlayMap = trapOverlayMap != null
                ? trapOverlayMap
                : FindOrCreateTilemap(gridRoot.transform, "TrapOverlay", sortingOrder: 4);
            doorOverlayMap = doorOverlayMap != null
                ? doorOverlayMap
                : FindOrCreateTilemap(gridRoot.transform, "DoorOverlay", sortingOrder: 5);

            if (enemyContainer == null)
            {
                Transform existingEnemy = transform.Find("EnemyContainer");
                if (existingEnemy != null)
                    enemyContainer = existingEnemy;
                else
                {
                    var enemyRoot = new GameObject("EnemyContainer");
                    enemyRoot.transform.SetParent(transform, false);
                    enemyContainer = enemyRoot.transform;
                }
            }

            if (dynamicViewsRoot == null)
            {
                Transform existingViews = transform.Find("DynamicViews");
                if (existingViews != null)
                    dynamicViewsRoot = existingViews;
                else
                {
                    var viewsRoot = new GameObject("DynamicViews");
                    viewsRoot.transform.SetParent(transform, false);
                    dynamicViewsRoot = viewsRoot.transform;
                }
            }
        }

        static Tilemap FindOrCreateTilemap(Transform gridParent, string objectName, int sortingOrder)
        {
            Transform child = gridParent.Find(objectName);
            if (child != null && child.TryGetComponent(out Tilemap existing))
                return existing;

            return CreateTilemapChild(gridParent, objectName, sortingOrder);
        }

        static Tilemap CreateTilemapChild(Transform parent, string objectName, int sortingOrder)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        public void MarkGenerated(
            Vector3Int playerStart,
            Dictionary<string, PortalArrivalBinding> arrivals)
        {
            _isGenerated = true;
            _playerStart = playerStart;
            _arrivalBindings.Clear();
            if (arrivals == null)
                return;

            foreach (KeyValuePair<string, PortalArrivalBinding> pair in arrivals)
                _arrivalBindings[pair.Key] = pair.Value;
        }

        public void StoreArrivalBinding(PortalArrivalBinding binding) =>
            _arrivalBindings[binding.portalLinkId] = binding;

        public bool TryGetArrivalBinding(string portalLinkId, out PortalArrivalBinding binding) =>
            _arrivalBindings.TryGetValue(portalLinkId, out binding);

        public void RegisterPortal(PortalInteractable portal) => _portals.Add(portal);

        public void BindToMapManager(MapManager mapManager)
        {
            gameObject.SetActive(true);
            mapManager?.SetActiveFloor(Tilemaps, FloorId);
            mapManager?.ConfigurePaintTiles(definition?.FloorTile, definition?.WallTile);
        }

        public void FinishActivation(GridManager gridManager)
        {
            gridManager?.ClearAllOccupancy();
            ReregisterEnemyOccupancy(gridManager);
            RegisterPortalsWithService();
        }

        public void ParkFloor()
        {
            UnregisterPortalsFromService();
            gameObject.SetActive(false);
        }

        public void ReregisterEnemyOccupancy(GridManager gridManager)
        {
            if (gridManager == null || enemyContainer == null)
                return;

            EnemyController[] enemies = enemyContainer.GetComponentsInChildren<EnemyController>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null)
                    continue;

                IBattleTarget target = enemy.GetComponent<IBattleTarget>();
                GridMover mover = enemy.GetComponent<GridMover>();
                if (target == null || mover == null)
                    continue;

                gridManager.RegisterActor(mover.GridPosition, target);
            }
        }

        void RegisterPortalsWithService()
        {
            AdjacentMapInteractableService service = AdjacentMapInteractableService.Instance;
            if (service == null)
                return;

            service.SetOverlayMap(interactableOverlayMap);
            for (int i = 0; i < _portals.Count; i++)
                service.Register(_portals[i].Cell, _portals[i]);
        }

        void UnregisterPortalsFromService()
        {
            AdjacentMapInteractableService service = AdjacentMapInteractableService.Instance;
            if (service == null)
                return;

            for (int i = 0; i < _portals.Count; i++)
                service.Unregister(_portals[i].Cell);
        }
    }
}
