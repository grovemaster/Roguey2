using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Manager.Floor;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Traps;
using JRogue.World.Lighting;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Vaults;
using JRogue.World.Generation.Zones;
using JRogue.World.Town;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation
{
    public sealed class DungeonFloorInstanceManager : MonoBehaviour
    {
        public static DungeonFloorInstanceManager Instance { get; private set; }

        [SerializeField] Transform floorsRoot;
        [SerializeField] DungeonFloorDefinition[] floorDefinitions;
        [SerializeField] bool useDontDestroyOnLoad;

        public bool UseDontDestroyOnLoad => useDontDestroyOnLoad;

        readonly Dictionary<string, DungeonFloorInstance> _instances =
            new Dictionary<string, DungeonFloorInstance>();

        static DungeonFloorDefinitionCatalog _districtHubCatalog;

        DungeonFloorInstance _activeFloor;
        bool _portalTransitionInProgress;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (useDontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (floorsRoot == null)
            {
                var floors = new GameObject("Floors");
                floors.transform.SetParent(transform, false);
                floorsRoot = floors.transform;
            }
        }

        public void ConfigureFloors(DungeonFloorDefinition[] definitions, bool replaceAll = true)
        {
            if (definitions == null || definitions.Length == 0)
                return;

            if (replaceAll)
            {
                floorDefinitions = definitions;
                return;
            }

            if (WouldDropRegisteredFloors(floorDefinitions, definitions))
            {
                floorDefinitions = MergeFloorDefinitions(floorDefinitions, definitions);
                return;
            }

            floorDefinitions = definitions;
        }

        static DungeonFloorDefinition[] MergeFloorDefinitions(
            DungeonFloorDefinition[] existing,
            DungeonFloorDefinition[] incoming)
        {
            if (existing == null || existing.Length == 0)
                return incoming;

            if (incoming == null || incoming.Length == 0)
                return existing;

            var merged = new List<DungeonFloorDefinition>(existing);
            for (int i = 0; i < incoming.Length; i++)
            {
                DungeonFloorDefinition def = incoming[i];
                if (def == null || string.IsNullOrEmpty(def.FloorId))
                    continue;

                if (ListContainsFloorId(merged, def.FloorId))
                    continue;

                merged.Add(def);
            }

            return merged.ToArray();
        }

        static bool ListContainsFloorId(List<DungeonFloorDefinition> definitions, string floorId)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                DungeonFloorDefinition def = definitions[i];
                if (def != null && def.FloorId == floorId)
                    return true;
            }

            return false;
        }

        static bool WouldDropRegisteredFloors(
            DungeonFloorDefinition[] current,
            DungeonFloorDefinition[] next)
        {
            if (current == null || current.Length == 0 || next == null)
                return false;

            for (int i = 0; i < current.Length; i++)
            {
                DungeonFloorDefinition def = current[i];
                if (def == null || string.IsNullOrEmpty(def.FloorId))
                    continue;

                if (!ContainsFloorId(next, def.FloorId))
                    return true;
            }

            return false;
        }

        static bool ContainsFloorId(DungeonFloorDefinition[] definitions, string floorId)
        {
            if (definitions == null)
                return false;

            for (int i = 0; i < definitions.Length; i++)
            {
                DungeonFloorDefinition def = definitions[i];
                if (def != null && def.FloorId == floorId)
                    return true;
            }

            return false;
        }

        public bool TryBeginRunAtFloor(string startFloorId, int runSeed)
        {
            DungeonRunState run = EnsureRunState();
            run.BeginRun(runSeed);
            DestroyAllFloors();

            DungeonFloorDefinition startDef = FindDefinition(startFloorId);
            EnsureDungeonTimeService().BeginDungeonRun(startDef, floorDefinitions);

            return TryActivateFloor(startFloorId, null, isFirstVisitSpawn: true, spawnMembers: null);
        }

        public bool TryTransitionPortal(string portalLinkId, string targetFloorId) =>
            TryTransitionPortalForWholeParty(portalLinkId, targetFloorId);

        /// <summary>
        /// Step-on portal: teleports the full party to the target floor arrival anchor.
        /// Individual member positions on the source floor are ignored.
        /// </summary>
        public bool TryTransitionPortalForWholeParty(string portalLinkId, string targetFloorId)
        {
            if (string.IsNullOrEmpty(targetFloorId) || _portalTransitionInProgress)
                return false;

            PartyFloorPresenceService.Instance?.UnparkAll();

            _portalTransitionInProgress = true;
            try
            {
                return TryActivateFloor(targetFloorId, portalLinkId, isFirstVisitSpawn: false, spawnMembers: null);
            }
            finally
            {
                _portalTransitionInProgress = false;
            }
        }

        public bool TryActivateFloorForMembers(
            string floorId,
            string portalLinkId,
            bool isFirstVisitSpawn,
            IReadOnlyList<BaseActor> spawnMembers)
        {
            if (_portalTransitionInProgress)
                return false;

            _portalTransitionInProgress = true;
            try
            {
                return TryActivateFloor(floorId, portalLinkId, isFirstVisitSpawn, spawnMembers);
            }
            finally
            {
                _portalTransitionInProgress = false;
            }
        }

        bool TryActivateFloor(
            string floorId,
            string portalLinkId,
            bool isFirstVisitSpawn,
            IReadOnlyList<BaseActor> spawnMembers)
        {
            DungeonGenerationLog.Info($"ActivateFloor begin floorId={floorId} portal={portalLinkId ?? "none"} firstVisit={isFirstVisitSpawn}");

            DungeonFloorDefinition def = FindDefinition(floorId);
            if (def == null)
            {
                DungeonGenerationLog.Error($"Unknown floor id '{floorId}'. Assign floorDefinitions on {nameof(DungeonFloorInstanceManager)}.");
                return false;
            }

            ParkActiveFloor();

            bool firstVisit = !_instances.ContainsKey(floorId);
            if (!_instances.TryGetValue(floorId, out DungeonFloorInstance instance))
            {
                instance = FindOrCreateFloorInstance(def);
                _instances[floorId] = instance;
                DungeonGenerationLog.Info(
                    $"Floor instance '{instance.gameObject.name}' under '{floorsRoot?.name ?? "null"}' " +
                    $"(EnemyContainer={(instance.EnemyContainer != null ? instance.EnemyContainer.name : "missing")}).");
            }

            DungeonRunState run = EnsureRunState();
            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;

            if (map == null)
            {
                DungeonGenerationLog.Error("MapManager.Instance is null — add MapManager to DungeonTestSystems.");
                return false;
            }

            instance.BindToMapManager(map);
            FloorItemPileService.Instance?.BindViewRoot(instance.DynamicViewsRoot);

            if (!instance.IsGenerated || !instance.HasPaintedFloorTiles())
            {
                if (instance.IsGenerated)
                {
                    DungeonGenerationLog.Warn(
                        $"Floor '{floorId}' was marked generated but has no painted floor — regenerating.");
                    instance.InvalidateGeneratedState();
                }

                DungeonGenerationPipeline.GenerateFirstVisit(instance, run.RunSeed);
            }
            else
                DungeonGenerationLog.Info($"Floor '{floorId}' already generated — reusing parked state.");

            ActivateInstance(instance, grid);
            Vector3Int anchor = ResolvePartyAnchor(instance, portalLinkId, isFirstVisitSpawn);
            DungeonGenerationLog.Info($"Party spawn anchor={anchor} floorId={floorId}");

            PartyFormationSpawnProfile profile = def.FormationProfile;
            if (spawnMembers != null && spawnMembers.Count > 0)
            {
                if (!PartySpawnService.TrySpawnFormationAtAnchor(anchor, profile, spawnMembers, out _))
                    DungeonGenerationLog.Warn("PartySpawnService failed for partial party spawn.");

                PartyFloorPresenceService presence = PartyFloorPresenceService.Instance;
                if (presence != null && floorId == HolyLandFloorIds.HolyLandProper)
                {
                    presence.ParkAllExcept(
                        spawnMembers,
                        HolyLandFloorIds.Nexus,
                        HolyLandNexusLayout.HolyLandReturnAnchor);
                }
            }
            else if (!PartySpawnService.TrySpawnFormationAtAnchor(anchor, profile, out _))
            {
                DungeonGenerationLog.Warn("PartySpawnService failed — check party roster and walkable cells.");
            }

            instance.ReregisterNpcOccupancy(grid);

            TownHubCameraUtility.ApplyForFloor(floorId);
            if (floorId == AdventureGuildExchangeLayout.InteriorFloorId)
                ShopCounterService.EnsureAdventureGuildExchangeCounters();
            else if (floorId == MarketGeneralStoreLayout.InteriorFloorId)
                ShopCounterService.EnsureMarketGeneralStoreCounters();
            else if (floorId == MarketItemShopLayout.InteriorFloorId)
                ShopCounterService.EnsureMarketItemShopCounters();
            else if (floorId == MarketBlacksmithLayout.InteriorFloorId)
                ShopCounterService.EnsureMarketBlacksmithCounters();
            else if (floorId == ResidentialInnLayout.InteriorFloorId)
                ShopCounterService.EnsureResidentialInnCounters();

            PartyManager.Instance?.RefreshCameraFollow();

            run.SetActiveFloor(floorId);
            EnsureDungeonTimeService().OnFloorActivated(def, firstVisit);
            BindVisibilityToActiveFloor(map);
            bool zoneCompositeSyncApplied = RefreshLighting();
            RefreshVisibility();
            DungeonEntryLightingDiagnostics.LogAfterFloorActivate(instance, def, zoneCompositeSyncApplied);
            if (TownPortalSetupPhase.IsHubFloor(floorId))
                TownTimeService.Instance?.OnTownFloorActivated(_activeFloor);

            DungeonGenerationLog.Info($"ActivateFloor complete floorId={floorId}");
            return true;
        }

        public bool TryGetActiveTownFloor(out DungeonFloorInstance instance)
        {
            instance = _activeFloor;
            return instance != null
                && instance.Definition != null
                && instance.Definition.FloorId == Phases.TownPortalSetupPhase.TownFloorId;
        }

        public DungeonFloorInstance GetActiveFloorInstance() => _activeFloor;

        void ActivateInstance(DungeonFloorInstance instance, GridManager grid)
        {
            _activeFloor = instance;
            instance.FinishActivation(grid);
            ZoneEnterTracker.Instance?.ResetTracking();
            // Party grid registration is done in PartySpawnService after formation placement.
        }

        Vector3Int ResolvePartyAnchor(
            DungeonFloorInstance instance,
            string portalLinkId,
            bool isFirstVisitSpawn)
        {
            if (!string.IsNullOrEmpty(portalLinkId) &&
                instance.TryGetArrivalBinding(portalLinkId, out PortalArrivalBinding binding))
                return binding.arrivalAnchor;

            if (!isFirstVisitSpawn && !string.IsNullOrEmpty(portalLinkId))
                Debug.LogWarning($"[DungeonFloor] Missing arrival binding for {portalLinkId}.");

            return instance.PlayerStart;
        }

        void ParkActiveFloor()
        {
            if (_activeFloor == null)
                return;

            _activeFloor.ParkFloor();
            _activeFloor = null;
        }

        public void DestroyAllFloors()
        {
            ParkActiveFloor();
            foreach (KeyValuePair<string, DungeonFloorInstance> pair in _instances)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            _instances.Clear();
            TownBuildingMassService.Clear();
            TownBuildingFacadeSight.Clear();
            ShopCounterService.Clear();
            DungeonFloorServiceBinder.ClearSingletonServices();
        }

        /// <summary>
        /// Tears down the current dungeon run (stub hub destination — §12 TBD).
        /// </summary>
        public void ExitDungeon()
        {
            DestroyAllFloors();
            DungeonRunState run = DungeonRunState.Instance;
            if (run != null)
                run.SetActiveFloor(null);

            DungeonGenerationLog.Info("ExitDungeon — all floor instances destroyed.");
        }

        public DungeonFloorDefinition TryFindDefinition(string floorId) => FindDefinition(floorId);

        DungeonFloorDefinition FindDefinition(string floorId)
        {
            DungeonFloorDefinition def = FindDefinitionInArray(floorDefinitions, floorId);
            if (def != null)
                return def;

            return FindDefinitionInCatalog(GetDistrictHubCatalog(), floorId);
        }

        static DungeonFloorDefinition FindDefinitionInArray(DungeonFloorDefinition[] definitions, string floorId)
        {
            if (definitions == null || string.IsNullOrEmpty(floorId))
                return null;

            for (int i = 0; i < definitions.Length; i++)
            {
                DungeonFloorDefinition def = definitions[i];
                if (def != null && def.FloorId == floorId)
                    return def;
            }

            return null;
        }

        static DungeonFloorDefinition FindDefinitionInCatalog(DungeonFloorDefinitionCatalog catalog, string floorId)
        {
            if (catalog?.Floors == null || string.IsNullOrEmpty(floorId))
                return null;

            return FindDefinitionInArray(catalog.Floors, floorId);
        }

        static DungeonFloorDefinitionCatalog GetDistrictHubCatalog()
        {
            if (_districtHubCatalog == null)
            {
                _districtHubCatalog = Resources.Load<DungeonFloorDefinitionCatalog>(
                    TownDistrictTestPaths.ResourcesCatalog);
            }

            return _districtHubCatalog;
        }

        static DungeonTimeService EnsureDungeonTimeService()
        {
            if (DungeonTimeService.Instance != null)
                return DungeonTimeService.Instance;

            if (DungeonRunState.Instance != null)
                return DungeonRunState.Instance.gameObject.AddComponent<DungeonTimeService>();

            var go = new GameObject("DungeonRunState");
            go.AddComponent<DungeonRunState>();
            return go.AddComponent<DungeonTimeService>();
        }

        DungeonRunState EnsureRunState()
        {
            if (DungeonRunState.Instance != null)
                return DungeonRunState.Instance;

            var go = new GameObject("DungeonRunState");
            go.transform.SetParent(transform, false);
            return go.AddComponent<DungeonRunState>();
        }

        static void BindVisibilityToActiveFloor(MapManager map)
        {
            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            if (visibility == null || map == null)
                return;

            var list = new System.Collections.Generic.List<Tilemap>();
            if (map.FloorMap != null)
                list.Add(map.FloorMap);
            if (map.WallMap != null)
                list.Add(map.WallMap);
            visibility.tilemaps = list;
            DungeonGenerationLog.Info($"VisibilityManager bound to {list.Count} tilemap(s).");
        }

        static void RefreshVisibility()
        {
            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            if (visibility == null)
            {
                DungeonGenerationLog.Warn("VisibilityManager not in scene — skipping fog refresh.");
                return;
            }

            visibility.ResetForNewFloor();
            visibility.RefreshPartyVision();
            RefreshWorldFeatureOverlayVisibility();

            DungeonFloorInstance activeFloor = Instance?.GetActiveFloorInstance();
            MapManager map = MapManager.Instance;
            if (activeFloor != null && map != null)
            {
                VaultStampDiagnostics.LogPlacedVaultsAudit(
                    activeFloor.VaultPlacementRecords,
                    map,
                    "afterVisibilityRefresh");
                VaultStampDiagnostics.LogVisibilityAudit(
                    activeFloor.VaultPlacementRecords,
                    map,
                    visibility,
                    "afterVisibilityRefresh");
                VaultStampDiagnostics.LogMonumentVaultRenderAudit(
                    activeFloor.VaultPlacementRecords,
                    map,
                    visibility,
                    InteractableTileService.Instance,
                    "afterVisibilityRefresh");
                VaultStampDiagnostics.LogFloorScanForVaultTiles(map, "afterVisibilityRefresh");
            }
        }

        static void RefreshWorldFeatureOverlayVisibility()
        {
            HazardService hazards = HazardService.Instance;
            if (hazards != null)
                hazards.RefreshAllOverlayVisuals();

            TrapService traps = TrapService.Instance;
            traps?.RefreshOverlayVisibility();

            Manager.Door.DoorService.Instance?.RefreshOverlayVisibility();
            Interactables.InteractableTileService.Instance?.RefreshAllOverlayVisuals();
            Instance?.ApplyPortalVisibilityOnActiveFloor();
        }

        public void ApplyPortalVisibilityOnActiveFloor(VisibilityManager visibility = null)
        {
            if (_activeFloor == null)
                return;

            if (visibility == null)
                visibility = Object.FindAnyObjectByType<VisibilityManager>();

            _activeFloor.ApplyPortalVisibility(visibility);
        }

        DungeonFloorInstance FindOrCreateFloorInstance(DungeonFloorDefinition def)
        {
            string floorId = def.FloorId;
            if (floorsRoot != null)
            {
                Transform existing = floorsRoot.Find(floorId);
                if (existing != null)
                {
                    DungeonFloorInstance instance = existing.GetComponent<DungeonFloorInstance>();
                    if (instance == null)
                        instance = existing.gameObject.AddComponent<DungeonFloorInstance>();

                    instance.Configure(def);
                    instance.EnsureHierarchyBuilt();
                    return instance;
                }
            }

            return DungeonFloorInstance.CreateUnder(floorsRoot, def);
        }

        bool RefreshLighting()
        {
            LightingService lighting = LightingService.Instance != null
                ? LightingService.Instance
                : Object.FindAnyObjectByType<LightingService>();
            if (lighting == null)
            {
                DungeonGenerationLog.Warn("LightingService not in scene — tiles may stay dark.");
                return false;
            }

            lighting.ResetForActiveFloor();

            DungeonFloorDefinition def = _activeFloor?.Definition;
            if (def != null && def.FloorId == Phases.TownTorchSetupPhase.TownFloorId)
                Phases.TownTorchSetupPhase.ApplyTownTorches(def);

            if (def != null && def.LayoutMode == FloorLayoutMode.ZoneComposite && def.ZoneLayout != null)
                ZoneCompositeLightingSync.ApplyZoneAmbientRegionDefaults(lighting, def.ZoneLayout);

            lighting.FinalizeRegistry();
            lighting.SyncFloorReceiversFromMap();

            bool zoneCompositeSyncApplied = false;
            if (def != null && def.LayoutMode == FloorLayoutMode.ZoneComposite)
            {
                int runSeed = DungeonRunState.Instance != null ? DungeonRunState.Instance.RunSeed : 0;
                zoneCompositeSyncApplied = ZoneCompositeLightingSync.Apply(
                    lighting,
                    MapManager.Instance,
                    def,
                    _activeFloor,
                    runSeed);
            }

            PartyLightEmitterBridge.RefreshParty();

            if (def != null && def.FloorId == Phases.TownTorchSetupPhase.TownFloorId)
                TownLightingSync.ApplyForCurrentPhase();
            else if (def != null && Phases.TownPortalSetupPhase.IsTownInterior(def.FloorId))
                lighting.ApplyFullInteriorDaylight();
            else
                lighting.OnPartyVisionActivity();

            return zoneCompositeSyncApplied;
        }
    }
}
