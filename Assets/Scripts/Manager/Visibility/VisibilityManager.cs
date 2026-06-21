using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Controller.Npc;
using JRogue.Core.Actor;
using JRogue.Item.World;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Visibility.Algorithm;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Manager.Door;
using JRogue.Traps;
using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Zones;
using JRogue.World.Lighting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VisibilityManager : MonoBehaviour
{
    enum TileKnowledgeState
    {
        Unseen = 0,
        Explored = 1,
        Visible = 2
    }

    struct TerrainSnapshot
    {
        public bool hasFloor;
        public bool hasWall;
    }

    struct CellKnowledge
    {
        public TileKnowledgeState state;
        public TerrainSnapshot snapshot;
        public LightingSnapshot lightingSnapshot;
    }

    struct LightingSnapshot
    {
        public int snapshotEmitLight;
        public int snapshotReceivedLight;
        public int snapshotAmbient;
        public bool presentationWasDarkTile;
    }

    public List<Tilemap> tilemaps;
    public Color visibleColor = Color.white;
    public Color darkTileColor = new Color(0.2f, 0.22f, 0.28f, 1f);
    public Color unseenColor = new Color(0.15f, 0.15f, 0.2f, 1.0f);
    public Color memColor = new Color(0.48f, 0.48f, 0.58f, 1.0f);

    /// <summary>Terrain tiles never seen are fully hidden (DCSS void), not dimmed silhouettes.</summary>
    static readonly Color HiddenUnseenTileColor = new Color(1f, 1f, 1f, 0f);

    public int viewRange = 8;
    [Min(0)] public int baseVisibilityThreshold = 3;
    [SerializeField] bool verboseSightLogs;
    [SerializeField] bool verboseDarkTileLogs;
    [SerializeField] bool verboseFogLogs;
    [SerializeField] bool verboseGateLogs;
    [Tooltip("Master switch for cavern/dungeon lighting diagnostics ([Lighting:Diag] logs).")]
    [SerializeField] bool verboseLightingDiagnostics;

    /// <summary>When true, emits [Lighting:Diag] logs from vision refresh and zone lighting sync.</summary>
    public bool VerboseLightingDiagnostics => verboseLightingDiagnostics;

    public int BaseVisibilityThreshold => baseVisibilityThreshold;

    public static bool IsVerboseLightingDiagnosticsEnabled()
    {
        VisibilityManager manager = FindAnyObjectByType<VisibilityManager>();
        return manager != null && manager.verboseLightingDiagnostics;
    }

    readonly Dictionary<Vector3Int, CellKnowledge> _knowledge =
        new Dictionary<Vector3Int, CellKnowledge>();
    readonly HashSet<Vector3Int> _knownCells = new HashSet<Vector3Int>();
    readonly HashSet<Vector3Int> _currentlyVisible = new HashSet<Vector3Int>();
    readonly HashSet<Vector3Int> _currentlyLitVisible = new HashSet<Vector3Int>();
    bool _lastPartyHasPersonalLight;

    Transform playerTransform;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // Procedural floors paint after Start; dungeon activation calls ResetForNewFloor + refresh.
        if (!HasPaintedTilesInBoundTilemaps())
            return;

        ResetForNewFloor();
        RefreshPartyVision();
    }

    public bool IsVisible(Vector3Int cell)
    {
        cell.z = 0;
        return _knowledge.TryGetValue(cell, out CellKnowledge knowledge)
            && knowledge.state == TileKnowledgeState.Visible;
    }

    public bool IsLitVisible(Vector3Int cell)
    {
        cell.z = 0;
        return _currentlyLitVisible.Contains(cell);
    }

    public bool IsExplored(Vector3Int cell)
    {
        cell.z = 0;
        return _knowledge.TryGetValue(cell, out CellKnowledge knowledge)
            && knowledge.state == TileKnowledgeState.Explored;
    }

    public bool IsUnseen(Vector3Int cell)
    {
        return !IsVisible(cell) && !IsExplored(cell);
    }

    public void ResetForNewFloor()
    {
        _knowledge.Clear();
        _knownCells.Clear();
        _currentlyVisible.Clear();
        _currentlyLitVisible.Clear();
        _lastPartyHasPersonalLight = false;

        foreach (Tilemap tm in tilemaps)
        {
            if (tm == null)
                continue;

            foreach (Vector3Int pos in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(pos))
                    continue;

                Vector3Int p = new Vector3Int(pos.x, pos.y, 0);
                _knownCells.Add(p);

                tm.SetTileFlags(pos, TileFlags.None);
                tm.SetColor(pos, HiddenUnseenTileColor);
            }
        }
    }

    public void RefreshPartyVision() => RefreshVision();

    public void RefreshVision()
    {
        HashSet<Vector3Int> currentVisible = ComputeCurrentVisibleSet(
            out HashSet<Vector3Int> currentLitVisible,
            out HashSet<Vector3Int> losUnlit);

        MapManager activeMap = MapManager.Instance;
        if (activeMap != null && TownPortalSetupPhase.IsTownInterior(activeMap.ActiveFloorId))
            RevealAllTownInteriorCells(activeMap, currentVisible, currentLitVisible);

        if (activeMap != null && TownPortalSetupPhase.IsHubFloor(activeMap.ActiveFloorId))
        {
            TownBuildingFacadeSight.AddWithinPartySightRange(
                currentVisible,
                currentLitVisible,
                PartyManager.Instance,
                LightingService.Instance,
                activeMap,
                viewRange,
                GetEffectiveSightRange,
                IsCellLiveVisibleForMember,
                IsCellFullyVisibleForMember);
        }

        if (currentVisible.Count == 0 && losUnlit.Count == 0)
        {
            ApplyUnseenToAllKnownCells();
            _currentlyVisible.Clear();
            _currentlyLitVisible.Clear();
            ApplyEntityVisibility();
            return;
        }

        // Visible -> explored (except lightless zones without a torch — no memory there).
        bool partyHasPersonalLight = PartyLightEmitterBridge.AnyMemberHasActiveCarriedEmitter();
        DungeonFloorZoneLayout zoneLayout = GetActiveZoneLayout();
        foreach (Vector3Int prev in _currentlyVisible)
        {
            if (currentVisible.Contains(prev))
                continue;

            if (ShouldSuppressFogMemory(prev, zoneLayout, partyHasPersonalLight))
            {
                SetCellState(prev, TileKnowledgeState.Unseen);
                TintCellUnseen(prev);
                continue;
            }

            SetCellState(prev, TileKnowledgeState.Explored);
            if (_knowledge.TryGetValue(prev, out CellKnowledge exploredKnowledge))
                TintCell(prev, GetExploredSnapshotColor(exploredKnowledge));

            if (verboseFogLogs && _knowledge.TryGetValue(prev, out CellKnowledge frozen))
            {
                LightingSnapshot ls = frozen.lightingSnapshot;
                Debug.Log(
                    $"[Lighting:Fog] Snapshot frozen at {prev} " +
                    $"emit={ls.snapshotEmitLight} recv={ls.snapshotReceivedLight} ambient={ls.snapshotAmbient} dark={ls.presentationWasDarkTile}");
            }
        }

        // Unseen/explored -> visible and snapshot refresh.
        foreach (Vector3Int cell in currentVisible)
        {
            TerrainSnapshot snapshot = CaptureSnapshot(cell);
            bool isDarkTile = !currentLitVisible.Contains(cell);
            LightingSnapshot lightingSnapshot = CaptureLightingSnapshot(cell, isDarkTile);
            if (_knowledge.TryGetValue(cell, out CellKnowledge knowledge))
            {
                knowledge.snapshot = snapshot;
                knowledge.lightingSnapshot = lightingSnapshot;
                knowledge.state = TileKnowledgeState.Visible;
            }
            else
            {
                knowledge = new CellKnowledge
                {
                    state = TileKnowledgeState.Visible,
                    snapshot = snapshot,
                    lightingSnapshot = lightingSnapshot
                };
            }

            _knowledge[cell] = knowledge;
            _knownCells.Add(cell);
            if (currentLitVisible.Contains(cell))
            {
                TintCell(cell, visibleColor);
            }
            else
            {
                TintCell(cell, darkTileColor);
                if (verboseDarkTileLogs)
                    Debug.Log($"[Lighting:DarkTile] {cell} live-visible but under threshold.");
            }

            if (verboseFogLogs)
            {
                Debug.Log(
                    $"[Lighting:Fog] Snapshot capture at {cell} " +
                    $"emit={lightingSnapshot.snapshotEmitLight} recv={lightingSnapshot.snapshotReceivedLight} " +
                    $"ambient={lightingSnapshot.snapshotAmbient} dark={lightingSnapshot.presentationWasDarkTile}");
            }
        }

        foreach (Vector3Int cell in losUnlit)
        {
            if (!currentVisible.Contains(cell))
                TintCell(cell, unseenColor);
        }

        if (partyHasPersonalLight != _lastPartyHasPersonalLight)
        {
            RetintLightlessZoneFog(zoneLayout, partyHasPersonalLight, currentVisible);
            _lastPartyHasPersonalLight = partyHasPersonalLight;
        }

        _currentlyVisible.Clear();
        foreach (Vector3Int cell in currentVisible)
            _currentlyVisible.Add(cell);
        _currentlyLitVisible.Clear();
        foreach (Vector3Int cell in currentLitVisible)
            _currentlyLitVisible.Add(cell);

        if (verboseLightingDiagnostics)
            LogVisionDiagnostics(currentVisible, currentLitVisible, losUnlit);

        ApplyEntityVisibility();
    }

    void LogVisionDiagnostics(
        HashSet<Vector3Int> visible,
        HashSet<Vector3Int> litVisible,
        HashSet<Vector3Int> losUnlit)
    {
        LightingService lighting = LightingService.Instance;
        int threshold = baseVisibilityThreshold;
        PartyManager party = PartyManager.Instance;
        BaseActor lead = party?.partyMembers != null && party.partyMembers.Count > 0
            ? party.partyMembers[0]
            : null;

        Debug.Log(
            $"[Lighting:Diag] Vision summary threshold={threshold} " +
            $"visible={visible.Count} litVisible={litVisible.Count} " +
            $"dimVisible={visible.Count - litVisible.Count} losUnlit={losUnlit.Count}");

        if (lighting == null || lead == null)
            return;

        Vector3Int origin = new Vector3Int(lead.GridPosition.x, lead.GridPosition.y, 0);
        LogCellLighting("party", origin, lighting, threshold, litVisible.Contains(origin));

        Vector3Int[] offsets =
        {
            new(0, 1, 0),
            new(1, 0, 0),
            new(0, -1, 0),
            new(-1, 0, 0),
            new(1, 1, 0),
            new(1, -1, 0),
            new(-1, -1, 0),
            new(-1, 1, 0),
        };
        string[] labels = { "N", "E", "S", "W", "NE", "SE", "SW", "NW" };
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3Int cell = origin + offsets[i];
            LogCellLighting(labels[i], cell, lighting, threshold, litVisible.Contains(cell));
        }

        int[] recvHistogram = new int[LightLevel.Max + 1];
        int emitterCount = 0;
        foreach (Vector3Int cell in visible)
        {
            int recv = lighting.GetReceivedLight(cell);
            int emit = lighting.GetEmitLight(cell);
            recvHistogram[Mathf.Clamp(recv, 0, LightLevel.Max)]++;
            if (emit > 0)
                emitterCount++;
        }

        Debug.Log(
            $"[Lighting:Diag] Visible recv histogram " +
            $"r0={recvHistogram[0]} r1={recvHistogram[1]} r2={recvHistogram[2]} " +
            $"r3={recvHistogram[3]} r4={recvHistogram[4]} r5={recvHistogram[5]} " +
            $"r6+={recvHistogram[6] + recvHistogram[7] + recvHistogram[8] + recvHistogram[9] + recvHistogram[10]} " +
            $"emitterCellsInVisible={emitterCount}");
    }

    static void LogCellLighting(
        string label,
        Vector3Int cell,
        LightingService lighting,
        int threshold,
        bool isLitVisible)
    {
        int emit = lighting.GetEmitLight(cell);
        int recv = lighting.GetReceivedLight(cell);
        bool inRegistry = lighting.TryGetCellData(cell, out LightCellData cellData);
        string registryZone = inRegistry && cellData.IsReceiver ? cellData.ZoneId ?? "(empty)" : "n/a";
        string band = emit > 0
            ? "emitter"
            : recv >= threshold
                ? "lit"
                : recv > 0
                    ? "dim"
                    : "dark";
        Debug.Log(
            $"[Lighting:Diag] {label} {cell} emit={emit} recv={recv} " +
            $"threshold={threshold} band={band} litVisible={isLitVisible} registryZone={registryZone}");
    }

    void ApplyUnseenToAllKnownCells()
    {
        foreach (Vector3Int cell in _knownCells)
            TintCellUnseen(cell);
    }

    bool HasPaintedTilesInBoundTilemaps()
    {
        if (tilemaps == null)
            return false;

        for (int i = 0; i < tilemaps.Count; i++)
        {
            Tilemap tm = tilemaps[i];
            if (tm == null)
                continue;

            foreach (Vector3Int pos in tm.cellBounds.allPositionsWithin)
            {
                if (tm.HasTile(pos))
                    return true;
            }
        }

        return false;
    }

    HashSet<Vector3Int> ComputeCurrentVisibleSet(
        out HashSet<Vector3Int> litVisible,
        out HashSet<Vector3Int> losUnlit)
    {
        ShadowCaster.IsOpaque isOpaque =
            pos => MapManager.Instance != null && MapManager.Instance.BlocksLineOfSight(pos);

        var visible = new HashSet<Vector3Int>();
        litVisible = new HashSet<Vector3Int>();
        losUnlit = new HashSet<Vector3Int>();
        PartyManager party = PartyManager.Instance;
        LightingService lighting = LightingService.Instance;
        MapManager map = MapManager.Instance;
        DungeonFloorZoneLayout zoneLayout = GetActiveZoneLayout();

        if (party != null && party.partyMembers != null && party.partyMembers.Count > 0)
        {
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                Vector3Int origin = new Vector3Int(member.GridPosition.x, member.GridPosition.y, 0);
                int effectiveSight = GetEffectiveSightRange(member, origin);
                if (verboseSightLogs)
                    Debug.Log($"[Lighting:Sight] {member.DisplayName} at {origin} -> {effectiveSight}");

                List<Vector3Int> memberVisible = ShadowCaster.GetVisibleTiles(origin, effectiveSight, isOpaque);
                bool hasPersonalVisionLight = PartyLightEmitterBridge.MemberHasActiveCarriedEmitter(member);
                string originZoneId = TryGetZoneId(origin);
                bool zoneRequiresPersonalLight = ZoneVisionPolicy.ZoneRequiresPersonalLightForVision(
                    originZoneId,
                    zoneLayout);
                bool blindInPitchDark = DarknessVisibilityLogic.MemberNavigatesBlind(
                    zoneRequiresPersonalLight,
                    hasPersonalVisionLight);

                if (verboseGateLogs && blindInPitchDark)
                {
                    Debug.Log(
                        $"[Lighting:Gate] {member.DisplayName} blind in {originZoneId ?? "?"} at {origin} — " +
                        "only occupied tile visible.");
                }

                DarknessVisibilityLogic.ApplyMemberVisibility(
                    memberVisible,
                    origin,
                    blindInPitchDark,
                    cell => IsCellLiveVisibleForMember(cell, lighting, map),
                    cell => IsCellFullyVisibleForMember(member, cell, lighting),
                    cell =>
                    {
                        string zoneId = TryGetZoneId(cell);
                        int emit = lighting != null ? lighting.GetEmitLight(cell) : 0;
                        int recv = lighting != null ? lighting.GetReceivedLight(cell) : 0;
                        return ZoneVisionPolicy.IsPitchDarkForVision(
                            zoneId,
                            emit,
                            recv,
                            zoneLayout,
                            hasPersonalVisionLight);
                    },
                    visible,
                    litVisible);
            }
        }
        else if (playerTransform != null)
        {
            Vector3Int fp = Vector3Int.FloorToInt(playerTransform.position);
            Vector3Int origin = new Vector3Int(fp.x, fp.y, 0);
            List<Vector3Int> fallbackVisible = ShadowCaster.GetVisibleTiles(origin, viewRange, isOpaque);
            for (int i = 0; i < fallbackVisible.Count; i++)
            {
                visible.Add(fallbackVisible[i]);
                litVisible.Add(fallbackVisible[i]);
            }
        }

        return visible;
    }

    bool IsCellLiveVisibleForMember(Vector3Int cell, LightingService lighting, MapManager map)
    {
        bool occupied = IlluminationVisibilityLogic.IsPartyMemberOccupyingCell(cell);
        if (lighting == null)
            return true;

        int emit = lighting.GetEmitLight(cell);
        int received = lighting.GetReceivedLight(cell);
        return IlluminationVisibilityLogic.IsCellLiveVisible(emit, received, occupied);
    }

    bool IsCellFullyVisibleForMember(BaseActor member, Vector3Int cell, LightingService lighting)
    {
        if (member == null)
            return false;

        bool occupied = IlluminationVisibilityLogic.IsPartyMemberOccupyingCell(cell);

        if (lighting == null)
            return true;

        int emit = lighting.GetEmitLight(cell);
        int received = lighting.GetReceivedLight(cell);
        int threshold = GetEffectiveLightThreshold(member, cell);
        return IlluminationVisibilityLogic.IsCellFullyBright(emit, received, occupied, threshold);
    }

    static void RevealAllTownInteriorCells(
        MapManager map,
        HashSet<Vector3Int> visible,
        HashSet<Vector3Int> litVisible)
    {
        Tilemap floor = map.FloorMap;
        if (floor == null)
            return;

        foreach (Vector3Int pos in floor.cellBounds.allPositionsWithin)
        {
            if (!floor.HasTile(pos))
                continue;

            Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
            visible.Add(cell);
            litVisible.Add(cell);
        }
    }

    public int GetEffectiveSightRange(BaseActor member, Vector3Int originCell)
    {
        if (member == null || member.stats == null || member.stats.sight == null)
            return viewRange;

        // Phase B/C stub: Dark Vision bonuses hook here in Phase H.
        int baseSight = member.stats.sight.GetValue();
        return Mathf.Max(1, baseSight);
    }

    public int GetEffectiveLightThreshold(BaseActor member, Vector3Int cell)
    {
        // Phase C stub: Dark Vision / magical darkness hooks land in Phase H/L.
        return Mathf.Max(0, baseVisibilityThreshold);
    }

    void SetCellState(Vector3Int cell, TileKnowledgeState state)
    {
        if (_knowledge.TryGetValue(cell, out CellKnowledge knowledge))
        {
            knowledge.state = state;
            _knowledge[cell] = knowledge;
        }
    }

    TerrainSnapshot CaptureSnapshot(Vector3Int cell)
    {
        MapManager map = MapManager.Instance;
        return new TerrainSnapshot
        {
            hasFloor = map != null && map.FloorMap != null && map.FloorMap.HasTile(cell),
            hasWall = map != null && map.WallMap != null && map.WallMap.HasTile(cell)
        };
    }

    LightingSnapshot CaptureLightingSnapshot(Vector3Int cell, bool isDarkTile)
    {
        LightingService lighting = LightingService.Instance;
        if (lighting == null)
        {
            return new LightingSnapshot
            {
                snapshotEmitLight = 0,
                snapshotReceivedLight = 0,
                snapshotAmbient = 0,
                presentationWasDarkTile = isDarkTile
            };
        }

        if (lighting.TryGetCellData(cell, out LightCellData data))
        {
            AmbientRegion region = lighting.GetOrCreateAmbientRegion(data.AmbientRegionId);
            return new LightingSnapshot
            {
                snapshotEmitLight = data.IsEmitter ? data.EmitLight : 0,
                snapshotReceivedLight = data.ReceivedLight,
                snapshotAmbient = region != null ? LightLevel.Clamp(region.CurrentAmbientLight) : 0,
                presentationWasDarkTile = isDarkTile
            };
        }

        return new LightingSnapshot
        {
            snapshotEmitLight = lighting.GetEmitLight(cell),
            snapshotReceivedLight = lighting.GetReceivedLight(cell),
            snapshotAmbient = 0,
            presentationWasDarkTile = isDarkTile
        };
    }

    Color GetExploredSnapshotColor(CellKnowledge knowledge)
    {
        LightingSnapshot snapshot = knowledge.lightingSnapshot;
        if (snapshot.snapshotReceivedLight <= 0 && snapshot.snapshotEmitLight <= 0 && snapshot.snapshotAmbient <= 0)
            return memColor;

        float normalized = Mathf.Clamp01(snapshot.snapshotReceivedLight / (float)LightLevel.Max);
        if (snapshot.presentationWasDarkTile)
        {
            // Dark-tile memory: closer to unseen while still explored.
            return Color.Lerp(unseenColor, memColor, normalized * 0.45f);
        }

        // Brighter memory for tiles last seen in strong light.
        return Color.Lerp(memColor * 0.8f, memColor, normalized);
    }

    void TintCell(Vector3Int pos, Color color)
    {
        for (int i = 0; i < tilemaps.Count; i++)
        {
            Tilemap tm = tilemaps[i];
            if (tm == null || !tm.HasTile(pos))
                continue;

            if (tm.GetColor(pos) == color)
                continue;

            tm.SetColor(pos, color);
        }
    }

    void RetintLightlessZoneFog(
        DungeonFloorZoneLayout zoneLayout,
        bool partyHasPersonalLight,
        HashSet<Vector3Int> currentVisible)
    {
        foreach (Vector3Int cell in _knownCells)
        {
            if (currentVisible.Contains(cell))
                continue;

            string zoneId = TryGetZoneId(cell);
            if (!ZoneVisionPolicy.ZoneRequiresPersonalLightForVision(zoneId, zoneLayout))
                continue;

            if (ShouldSuppressFogMemory(cell, zoneLayout, partyHasPersonalLight))
            {
                TintCellUnseen(cell);
                continue;
            }

            if (_knowledge.TryGetValue(cell, out CellKnowledge knowledge)
                && knowledge.state == TileKnowledgeState.Explored)
            {
                TintCell(cell, GetExploredSnapshotColor(knowledge));
            }
        }
    }

    void TintCellUnseen(Vector3Int pos) => TintCell(pos, HiddenUnseenTileColor);

    static EnemyController[] GetActiveEnemies()
    {
        DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
        if (floor != null && floor.EnemyContainer != null)
            return floor.EnemyContainer.GetComponentsInChildren<EnemyController>(false);

        return Object.FindObjectsByType<EnemyController>();
    }

    static NpcController[] GetActiveNpcs()
    {
        DungeonFloorInstance floor = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
        if (floor != null && floor.DynamicViewsRoot != null)
            return floor.DynamicViewsRoot.GetComponentsInChildren<NpcController>(false);

        return Object.FindObjectsByType<NpcController>();
    }

    void ApplyEntityVisibility()
    {
        EnemyController[] enemies = GetActiveEnemies();
        for (int i = 0; i < enemies.Length; i++)
            ApplyEnemyVisibility(enemies[i]);

        NpcController[] npcs = GetActiveNpcs();
        for (int i = 0; i < npcs.Length; i++)
            ApplyNpcVisibility(npcs[i]);

        FloorItemPileService piles = FloorItemPileService.Instance;
        if (piles != null)
            piles.ApplyVisibility(this);

        FloorEssenceService essences = FloorEssenceService.Instance;
        if (essences != null)
            essences.ApplyVisibility(this);

        TrapService.Instance?.RefreshOverlayVisibility();
        HazardService.Instance?.RefreshAllOverlayVisuals();
        DoorService.Instance?.RefreshOverlayVisibility();
        InteractableTileService.Instance?.RefreshAllOverlayVisuals();
        DungeonFloorInstanceManager.Instance?.ApplyPortalVisibilityOnActiveFloor(this);
    }

    void ApplyEnemyVisibility(EnemyController enemy)
    {
        if (enemy == null)
            return;

        bool anyVisible = IsEnemyVisible(enemy);
        SetActorSpriteVisibility(enemy, anyVisible);
    }

    void ApplyNpcVisibility(NpcController npc)
    {
        if (npc == null)
            return;

        SetActorSpriteVisibility(npc, IsVisible(npc.GridPosition));
    }

    static void SetActorSpriteVisibility(BaseActor actor, bool visible)
    {
        if (actor == null)
            return;

        SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = visible;
    }

    bool IsEnemyVisible(EnemyController enemy)
    {
        if (enemy is IGridFootprint footprint)
        {
            var cells = new List<Vector3Int>(8);
            GridFootprintUtility.GetOccupiedCells(footprint, cells);
            for (int i = 0; i < cells.Count; i++)
            {
                if (IsLitVisible(cells[i]))
                    return true;
            }

            return false;
        }

        return IsLitVisible(enemy.GridPosition);
    }

    static DungeonFloorZoneLayout GetActiveZoneLayout() =>
        DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance()?.Definition?.ZoneLayout;

    static string TryGetZoneId(Vector3Int cell)
    {
        DungeonFloorInstance instance = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
        if (instance != null && instance.TryGetZoneId(cell, out string zoneId))
            return zoneId;

        return null;
    }

    static bool ShouldSuppressFogMemory(
        Vector3Int cell,
        DungeonFloorZoneLayout zoneLayout,
        bool partyHasPersonalLight)
    {
        string zoneId = TryGetZoneId(cell);
        return ZoneVisionPolicy.ShouldSuppressFogMemory(zoneId, zoneLayout, partyHasPersonalLight);
    }
}