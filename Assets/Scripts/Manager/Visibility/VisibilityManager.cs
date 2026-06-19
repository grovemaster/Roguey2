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

    readonly Dictionary<Vector3Int, CellKnowledge> _knowledge =
        new Dictionary<Vector3Int, CellKnowledge>();
    readonly HashSet<Vector3Int> _knownCells = new HashSet<Vector3Int>();
    readonly HashSet<Vector3Int> _currentlyVisible = new HashSet<Vector3Int>();
    readonly HashSet<Vector3Int> _currentlyLitVisible = new HashSet<Vector3Int>();

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

        // Visible -> explored.
        foreach (Vector3Int prev in _currentlyVisible)
        {
            if (currentVisible.Contains(prev))
                continue;

            SetCellState(prev, TileKnowledgeState.Explored);
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

        // Apply explored tint for known non-visible cells.
        foreach (Vector3Int cell in _knownCells)
        {
            if (currentVisible.Contains(cell))
                continue;

            if (losUnlit.Contains(cell))
            {
                TintCell(cell, unseenColor);
                continue;
            }

            if (_knowledge.TryGetValue(cell, out CellKnowledge knowledge)
                && knowledge.state == TileKnowledgeState.Explored)
            {
                TintCell(cell, GetExploredSnapshotColor(knowledge));
            }
            else
            {
                TintCellUnseen(cell);
            }
        }

        _currentlyVisible.Clear();
        foreach (Vector3Int cell in currentVisible)
            _currentlyVisible.Add(cell);
        _currentlyLitVisible.Clear();
        foreach (Vector3Int cell in currentLitVisible)
            _currentlyLitVisible.Add(cell);

        ApplyEntityVisibility();
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
                for (int j = 0; j < memberVisible.Count; j++)
                {
                    Vector3Int cell = memberVisible[j];
                    if (!IsCellLiveVisibleForMember(cell, lighting, map))
                    {
                        if (map == null || !map.IsWall(cell))
                            losUnlit.Add(cell);

                        if (verboseGateLogs)
                            Debug.Log($"[Lighting:Gate] {cell} in LOS but receivedLight=0 — excluded.");
                        continue;
                    }

                    visible.Add(cell);
                    if (IsCellFullyVisibleForMember(member, cell, lighting))
                        litVisible.Add(cell);
                }
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

        bool isWallInLos = map != null && map.IsWall(cell);
        int emit = lighting.GetEmitLight(cell);
        int received = lighting.GetReceivedLight(cell);
        return IlluminationVisibilityLogic.IsCellLiveVisible(emit, received, occupied, isWallInLos);
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
            tm.SetColor(pos, color);
        }
    }

    void TintCellUnseen(Vector3Int pos) => TintCell(pos, HiddenUnseenTileColor);

    void ApplyEntityVisibility()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>();
        for (int i = 0; i < enemies.Length; i++)
            ApplyEnemyVisibility(enemies[i]);

        NpcController[] npcs = FindObjectsByType<NpcController>();
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
}