using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Core.Actor;
using JRogue.Item.World;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Visibility.Algorithm;
using JRogue.Traps;
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

    public int viewRange = 8;
    [Min(0)] public int baseVisibilityThreshold = 3;
    [SerializeField] bool verboseSightLogs;
    [SerializeField] bool verboseDarkTileLogs;
    [SerializeField] bool verboseFogLogs;

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
                tm.SetColor(pos, unseenColor);
            }
        }
    }

    public void RefreshPartyVision() => RefreshVision();

    public void RefreshVision()
    {
        HashSet<Vector3Int> currentVisible = ComputeCurrentVisibleSet(out HashSet<Vector3Int> currentLitVisible);
        if (currentVisible.Count == 0)
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
                    Debug.Log($"[Lighting:DarkTile] {cell} LOS-visible but under threshold.");
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

            if (_knowledge.TryGetValue(cell, out CellKnowledge knowledge)
                && knowledge.state == TileKnowledgeState.Explored)
            {
                TintCell(cell, GetExploredSnapshotColor(knowledge));
            }
            else
            {
                TintCell(cell, unseenColor);
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
            TintCell(cell, unseenColor);
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

    HashSet<Vector3Int> ComputeCurrentVisibleSet(out HashSet<Vector3Int> litVisible)
    {
        ShadowCaster.IsOpaque isOpaque =
            pos => MapManager.Instance != null && !MapManager.Instance.IsWalkable(pos);

        var visible = new HashSet<Vector3Int>();
        litVisible = new HashSet<Vector3Int>();
        PartyManager party = PartyManager.Instance;
        LightingService lighting = LightingService.Instance;

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

    bool IsCellFullyVisibleForMember(BaseActor member, Vector3Int cell, LightingService lighting)
    {
        if (member == null)
            return false;

        // R7.1 occupied party member cell is always fully bright.
        if (PartyManager.Instance != null)
        {
            for (int i = 0; i < PartyManager.Instance.partyMembers.Count; i++)
            {
                BaseActor partyMember = PartyManager.Instance.partyMembers[i];
                if (partyMember != null && partyMember.GridPosition == cell)
                    return true;
            }
        }

        if (lighting == null)
            return true;

        // R7.1 emitter in LOS with active emission is always fully bright.
        if (lighting.GetEmitLight(cell) > 0)
            return true;

        int received = lighting.GetReceivedLight(cell);
        int threshold = GetEffectiveLightThreshold(member, cell);
        return received >= threshold;
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

    void ApplyEntityVisibility()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>();
        for (int i = 0; i < enemies.Length; i++)
            ApplyEnemyVisibility(enemies[i]);

        FloorItemPileService piles = FloorItemPileService.Instance;
        if (piles != null)
            piles.ApplyVisibility(this);

        FloorEssenceService essences = FloorEssenceService.Instance;
        if (essences != null)
            essences.ApplyVisibility(this);

        TrapService.Instance?.RefreshOverlayVisibility();
    }

    void ApplyEnemyVisibility(EnemyController enemy)
    {
        if (enemy == null)
            return;

        bool anyVisible = IsEnemyVisible(enemy);
        SpriteRenderer[] renderers = enemy.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = anyVisible;
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