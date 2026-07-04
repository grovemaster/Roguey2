using System.Collections.Generic;
using JRogue.Actors.Components;
using JRogue.Controller.Enemy;
using JRogue.Controller.Npc;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Vaults;
using JRogue.World.Generation.Zones;
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
        readonly List<ZoneCellMapEntry> _zoneCellMapSnapshot = new List<ZoneCellMapEntry>();
        readonly Dictionary<Vector3Int, string> _zoneIdByCell = new Dictionary<Vector3Int, string>();
        readonly List<ResolvedZonePiece> _resolvedZonePieces = new List<ResolvedZonePiece>();
        readonly List<PortalInteractable> _portals = new List<PortalInteractable>();
        readonly List<JRogue.World.MapInteract.IAdjacentMapInteractable> _extraMapInteractables =
            new List<JRogue.World.MapInteract.IAdjacentMapInteractable>();
        readonly List<VaultPlacementRecord> _vaultPlacementRecords = new List<VaultPlacementRecord>();
        readonly List<PortalVisual> _portalVisuals = new List<PortalVisual>();
        readonly DungeonFloorFeatureSnapshot _featureSnapshot = new DungeonFloorFeatureSnapshot();
        readonly HashSet<string> _monsterSpawnOnceLedger = new HashSet<string>();

        struct PortalVisual
        {
            public Vector3Int Cell;
            public SpriteRenderer Renderer;
            public bool RequiresTownTimeOpen;
        }

        bool _isGenerated;
        bool _featuresLiveOnServices;
        int _lastAppliedMonsterSpawnDay;
        Vector3Int _playerStart;

        public DungeonFloorFeatureSnapshot FeatureSnapshot => _featureSnapshot;

        public DungeonFloorDefinition Definition => definition;
        public string FloorId => definition != null ? definition.FloorId : name;
        public bool IsGenerated => _isGenerated;

        public bool HasPaintedFloorTiles()
        {
            if (floorMap == null)
                return false;

            BoundsInt bounds = floorMap.cellBounds;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    if (floorMap.HasTile(new Vector3Int(x, y, bounds.zMin)))
                        return true;
                }
            }

            return false;
        }

        public void MarkNeedsRegeneration()
        {
            _isGenerated = false;
            _featuresLiveOnServices = false;
            _portals.Clear();
            _extraMapInteractables.Clear();
            _portalVisuals.Clear();
        }

        public void InvalidateGeneratedState()
        {
            _isGenerated = false;
            _featuresLiveOnServices = false;
            floorMap?.ClearAllTiles();
            wallMap?.ClearAllTiles();
            hazardOverlayMap?.ClearAllTiles();
            interactableOverlayMap?.ClearAllTiles();
            trapOverlayMap?.ClearAllTiles();
            doorOverlayMap?.ClearAllTiles();
            _portals.Clear();
            _extraMapInteractables.Clear();
            _portalVisuals.Clear();
        }

        public bool FeaturesLiveOnServices => _featuresLiveOnServices;
        public Vector3Int PlayerStart => _playerStart;
        public Transform EnemyContainer => enemyContainer;
        public Transform DynamicViewsRoot => dynamicViewsRoot;
        public IReadOnlyList<ZoneCellMapEntry> ZoneCellMapSnapshot => _zoneCellMapSnapshot;
        public IReadOnlyList<ResolvedZonePiece> ResolvedZonePieces => _resolvedZonePieces;
        public IReadOnlyList<VaultPlacementRecord> VaultPlacementRecords => _vaultPlacementRecords;

        public int GetLastAppliedMonsterSpawnDay() => _lastAppliedMonsterSpawnDay;

        public HashSet<string> GetMonsterSpawnOnceLedger() => _monsterSpawnOnceLedger;

        public void SetLastAppliedMonsterSpawnDay(int dungeonDay) =>
            _lastAppliedMonsterSpawnDay = dungeonDay;

        public bool TryGetZoneId(Vector3Int cell, out string zoneId)
        {
            cell.z = 0;
            return _zoneIdByCell.TryGetValue(cell, out zoneId);
        }

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
                ApplyDungeonTileAnchors();
                return;
            }

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

            ApplyDungeonTileAnchors();
        }

        void ApplyDungeonTileAnchors()
        {
            ApplyTileAnchor(floorMap);
            ApplyTileAnchor(wallMap);
            ApplyTileAnchor(hazardOverlayMap);
            ApplyTileAnchor(interactableOverlayMap);
            ApplyTileAnchor(trapOverlayMap);
            ApplyTileAnchor(doorOverlayMap);
        }

        static void ApplyTileAnchor(Tilemap tilemap)
        {
            if (tilemap == null)
                return;

            tilemap.tileAnchor = FloorWallTileAnchor;
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
            tilemap.tileAnchor = FloorWallTileAnchor;
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        public void MarkFeaturesLiveOnServices() => _featuresLiveOnServices = true;

        public void ClearFeaturesLiveOnServices() => _featuresLiveOnServices = false;

        public void MarkGenerated(
            Vector3Int playerStart,
            Dictionary<string, PortalArrivalBinding> arrivals,
            Dictionary<Vector3Int, string> zoneCellMap = null,
            ResolvedZonePiece[] resolvedZonePieces = null)
        {
            _isGenerated = true;
            _playerStart = playerStart;
            _arrivalBindings.Clear();
            if (arrivals != null)
            {
                foreach (KeyValuePair<string, PortalArrivalBinding> pair in arrivals)
                    _arrivalBindings[pair.Key] = pair.Value;
            }

            _zoneCellMapSnapshot.Clear();
            _zoneIdByCell.Clear();
            if (zoneCellMap != null)
            {
                foreach (KeyValuePair<Vector3Int, string> pair in zoneCellMap)
                {
                    Vector3Int cell = new Vector3Int(pair.Key.x, pair.Key.y, 0);
                    _zoneCellMapSnapshot.Add(new ZoneCellMapEntry
                    {
                        x = cell.x,
                        y = cell.y,
                        zoneId = pair.Value,
                    });
                    _zoneIdByCell[cell] = pair.Value;
                }
            }

            _resolvedZonePieces.Clear();
            if (resolvedZonePieces != null)
            {
                for (int i = 0; i < resolvedZonePieces.Length; i++)
                    _resolvedZonePieces.Add(resolvedZonePieces[i]);
            }
        }

        public void StoreVaultPlacementRecords(IReadOnlyList<VaultPlacementRecord> records)
        {
            _vaultPlacementRecords.Clear();
            if (records == null)
                return;

            for (int i = 0; i < records.Count; i++)
            {
                VaultPlacementRecord record = records[i];
                _vaultPlacementRecords.Add(new VaultPlacementRecord
                {
                    VaultId = record.VaultId,
                    Origin = record.Origin,
                    FootprintCells = record.FootprintCells != null
                        ? new List<Vector3Int>(record.FootprintCells)
                        : new List<Vector3Int>(),
                });
            }
        }

        public void StoreArrivalBinding(PortalArrivalBinding binding) =>
            _arrivalBindings[binding.portalLinkId] = binding;

        public bool TryGetArrivalBinding(string portalLinkId, out PortalArrivalBinding binding) =>
            _arrivalBindings.TryGetValue(portalLinkId, out binding);

        public void RegisterPortal(PortalInteractable portal) => _portals.Add(portal);

        public void RegisterMapInteractable(JRogue.World.MapInteract.IAdjacentMapInteractable interactable)
        {
            if (interactable != null)
                _extraMapInteractables.Add(interactable);
        }

        public void PlacePortalVisual(Vector3Int cell, bool requiresTownTimeOpen = false)
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
            renderer.enabled = false;
            if (floorMap != null && floorMap.TryGetComponent(out TilemapRenderer floorRenderer))
            {
                renderer.sortingLayerID = floorRenderer.sortingLayerID;
                renderer.sortingOrder = floorRenderer.sortingOrder + 5;
            }
            else
                renderer.sortingOrder = 10;

            _portalVisuals.Add(new PortalVisual
            {
                Cell = cell,
                Renderer = renderer,
                RequiresTownTimeOpen = requiresTownTimeOpen,
            });
        }

        public void RefreshTownDungeonPortalVisual(bool portalOpen)
        {
            for (int i = 0; i < _portalVisuals.Count; i++)
            {
                PortalVisual visual = _portalVisuals[i];
                if (!visual.RequiresTownTimeOpen || visual.Renderer == null)
                    continue;

                visual.Renderer.color = portalOpen ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }
        }

        public void ApplyPortalVisibility(VisibilityManager visibility)
        {
            bool townPortalOpen = TownTimeService.Instance == null
                || TownTimeService.Instance.IsDungeonPortalOpen();

            for (int i = 0; i < _portalVisuals.Count; i++)
            {
                PortalVisual visual = _portalVisuals[i];
                if (visual.Renderer == null)
                    continue;

                bool open = !visual.RequiresTownTimeOpen || townPortalOpen;
                bool visible = visibility != null && visibility.IsVisible(visual.Cell);
                if (!visible && TownPortalSetupPhase.IsHubFloor(FloorId))
                    visible = true;

                visual.Renderer.enabled = open && visible;
            }
        }

        public void BindToMapManager(MapManager mapManager)
        {
            gameObject.SetActive(true);
            mapManager?.SetActiveFloor(Tilemaps, FloorId);
            mapManager?.ConfigurePaintTiles(definition?.FloorTile, definition?.WallTile);
        }

        public void FinishActivation(GridManager gridManager)
        {
            bool restoreFeatures = !_featuresLiveOnServices;
            DungeonGenerationLog.Info(
                $"FinishActivation floorId={FloorId} restoreFeaturesFromSnapshot={restoreFeatures} " +
                $"featuresLiveOnServices={_featuresLiveOnServices}");

            DungeonFloorServiceBinder.BindActiveFloor(this, restoreFeaturesFromSnapshot: restoreFeatures);
            if (_featuresLiveOnServices)
                _featuresLiveOnServices = false;

            gridManager?.ClearAllOccupancy();
            ReregisterEnemyOccupancy(gridManager);
            ReregisterNpcOccupancy(gridManager);
            RegisterPortalsWithService();
        }

        public void ParkFloor()
        {
            DungeonFloorServiceBinder.CaptureFeatureState(this);
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

        public void ReregisterNpcOccupancy(GridManager gridManager)
        {
            if (gridManager == null || dynamicViewsRoot == null)
                return;

            NpcController[] npcs = dynamicViewsRoot.GetComponentsInChildren<NpcController>(true);
            for (int i = 0; i < npcs.Length; i++)
            {
                NpcController npc = npcs[i];
                if (npc == null)
                    continue;

                IBattleTarget target = npc.GetComponent<IBattleTarget>();
                GridMover mover = npc.GetComponent<GridMover>();
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

            for (int i = 0; i < _extraMapInteractables.Count; i++)
            {
                JRogue.World.MapInteract.IAdjacentMapInteractable interactable = _extraMapInteractables[i];
                if (interactable != null)
                    service.Register(interactable.Cell, interactable);
            }
        }

        void UnregisterPortalsFromService()
        {
            AdjacentMapInteractableService service = AdjacentMapInteractableService.Instance;
            if (service == null)
                return;

            for (int i = 0; i < _portals.Count; i++)
                service.Unregister(_portals[i].Cell);

            for (int i = 0; i < _extraMapInteractables.Count; i++)
            {
                JRogue.World.MapInteract.IAdjacentMapInteractable interactable = _extraMapInteractables[i];
                if (interactable != null)
                    service.Unregister(interactable.Cell);
            }
        }
    }
}
