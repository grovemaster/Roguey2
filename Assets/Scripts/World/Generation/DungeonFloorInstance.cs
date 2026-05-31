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
        static readonly Vector3 FloorWallTileAnchor = Vector3.zero;

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
            {
                Debug.Log(
                    $"[TileDebug] EnsureHierarchyBuilt early-exit on '{name}' " +
                    $"floorMapId={floorMap.GetInstanceID()} anchorBefore={floorMap.tileAnchor}");
                ApplyFloorWallTileAnchor(floorMap);
                ApplyFloorWallTileAnchor(wallMap);
                return;
            }

            Debug.Log($"[TileDebug] EnsureHierarchyBuilt full BuildHierarchy on '{name}'");
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

            ApplyFloorWallTileAnchor(floorMap);
            ApplyFloorWallTileAnchor(wallMap);

            Debug.Log(
                $"[TileDebug] BuildHierarchy done '{name}' " +
                $"floor={DescribeTilemap(floorMap)} wall={DescribeTilemap(wallMap)}");
        }

        static void ApplyFloorWallTileAnchor(Tilemap tilemap)
        {
            if (tilemap == null)
                return;

            Vector3 before = tilemap.tileAnchor;
            tilemap.tileAnchor = FloorWallTileAnchor;
            Debug.Log(
                $"[TileDebug] ApplyFloorWallTileAnchor '{tilemap.name}' id={tilemap.GetInstanceID()} " +
                $"before={before} after={tilemap.tileAnchor} path={GetTransformPath(tilemap.transform)}");
        }

        static string DescribeTilemap(Tilemap tilemap)
        {
            if (tilemap == null)
                return "null";

            UnityEngine.Grid grid = tilemap.layoutGrid;
            return $"id={tilemap.GetInstanceID()} anchor={tilemap.tileAnchor} " +
                $"pos={tilemap.transform.position} scale={tilemap.transform.lossyScale} " +
                $"gridRef={(grid != null ? grid.GetInstanceID().ToString() : "null")} " +
                $"gridCellSize={(grid != null ? grid.cellSize.ToString() : "n/a")}";
        }

        static string GetTransformPath(Transform t)
        {
            if (t == null)
                return "null";

            var parts = new System.Collections.Generic.List<string>();
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
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
            if (objectName is "Floor" or "Wall")
                tilemap.tileAnchor = FloorWallTileAnchor;
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

        public void PlacePortalVisual(Vector3Int cell)
        {
            Sprite sprite = DungeonPortalVisuals.PortalSprite;
            if (sprite == null)
            {
                DungeonGenerationLog.Warn(
                    "Portal visual skipped — missing Resources/Dungeon/PortalSprite (see Assets/Art/Portal).");
                return;
            }

            if (dynamicViewsRoot == null)
                return;

            var portalGo = new GameObject($"Portal_{cell.x}_{cell.y}");
            portalGo.transform.SetParent(dynamicViewsRoot, false);
            portalGo.transform.position = GridCellWorld.GetCellCenter(floorMap, cell);

            SpriteRenderer renderer = portalGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            if (floorMap != null && floorMap.TryGetComponent(out TilemapRenderer floorRenderer))
            {
                renderer.sortingLayerID = floorRenderer.sortingLayerID;
                renderer.sortingOrder = floorRenderer.sortingOrder + 5;
            }
            else
                renderer.sortingOrder = 10;
        }

        public void BindToMapManager(MapManager mapManager)
        {
            gameObject.SetActive(true);
            Debug.Log(
                $"[TileDebug] BindToMapManager '{FloorId}' before SetActiveFloor " +
                $"floor={DescribeTilemap(floorMap)} wall={DescribeTilemap(wallMap)}");
            mapManager?.SetActiveFloor(Tilemaps, FloorId);
            mapManager?.ConfigurePaintTiles(definition?.FloorTile, definition?.WallTile);
            Debug.Log($"[TileDebug] BindToMapManager '{FloorId}' after ConfigurePaintTiles");
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
