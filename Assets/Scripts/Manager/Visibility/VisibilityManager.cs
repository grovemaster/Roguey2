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
    }

    public List<Tilemap> tilemaps;
    public Color visibleColor = Color.white;
    public Color unseenColor = new Color(0.15f, 0.15f, 0.2f, 1.0f);
    public Color memColor = new Color(0.48f, 0.48f, 0.58f, 1.0f);

    public int viewRange = 8;

    readonly Dictionary<Vector3Int, CellKnowledge> _knowledge =
        new Dictionary<Vector3Int, CellKnowledge>();
    readonly HashSet<Vector3Int> _knownCells = new HashSet<Vector3Int>();
    readonly HashSet<Vector3Int> _currentlyVisible = new HashSet<Vector3Int>();

    Transform playerTransform;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        ResetForNewFloor();
        RefreshPartyVision();
    }

    public bool IsVisible(Vector3Int cell)
    {
        cell.z = 0;
        return _knowledge.TryGetValue(cell, out CellKnowledge knowledge)
            && knowledge.state == TileKnowledgeState.Visible;
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
        HashSet<Vector3Int> currentVisible = ComputeCurrentVisibleSet();
        if (currentVisible.Count == 0)
            return;

        // Visible -> explored.
        foreach (Vector3Int prev in _currentlyVisible)
        {
            if (currentVisible.Contains(prev))
                continue;

            SetCellState(prev, TileKnowledgeState.Explored);
        }

        // Unseen/explored -> visible and snapshot refresh.
        foreach (Vector3Int cell in currentVisible)
        {
            TerrainSnapshot snapshot = CaptureSnapshot(cell);
            if (_knowledge.TryGetValue(cell, out CellKnowledge knowledge))
            {
                knowledge.snapshot = snapshot;
                knowledge.state = TileKnowledgeState.Visible;
            }
            else
            {
                knowledge = new CellKnowledge
                {
                    state = TileKnowledgeState.Visible,
                    snapshot = snapshot
                };
            }

            _knowledge[cell] = knowledge;
            _knownCells.Add(cell);
            TintCell(cell, visibleColor);
        }

        // Apply explored tint for known non-visible cells.
        foreach (Vector3Int cell in _knownCells)
        {
            if (currentVisible.Contains(cell))
                continue;

            if (_knowledge.TryGetValue(cell, out CellKnowledge knowledge)
                && knowledge.state == TileKnowledgeState.Explored)
            {
                TintCell(cell, memColor);
            }
            else
            {
                TintCell(cell, unseenColor);
            }
        }

        _currentlyVisible.Clear();
        foreach (Vector3Int cell in currentVisible)
            _currentlyVisible.Add(cell);

        ApplyEntityVisibility();
    }

    HashSet<Vector3Int> ComputeCurrentVisibleSet()
    {
        ShadowCaster.IsOpaque isOpaque =
            pos => MapManager.Instance != null && !MapManager.Instance.IsWalkable(pos);

        var visible = new HashSet<Vector3Int>();
        PartyManager party = PartyManager.Instance;

        if (party != null && party.partyMembers != null && party.partyMembers.Count > 0)
        {
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                Vector3Int origin = new Vector3Int(member.GridPosition.x, member.GridPosition.y, 0);
                List<Vector3Int> memberVisible = ShadowCaster.GetVisibleTiles(origin, viewRange, isOpaque);
                for (int j = 0; j < memberVisible.Count; j++)
                    visible.Add(memberVisible[j]);
            }
        }
        else if (playerTransform != null)
        {
            Vector3Int fp = Vector3Int.FloorToInt(playerTransform.position);
            Vector3Int origin = new Vector3Int(fp.x, fp.y, 0);
            List<Vector3Int> fallbackVisible = ShadowCaster.GetVisibleTiles(origin, viewRange, isOpaque);
            for (int i = 0; i < fallbackVisible.Count; i++)
                visible.Add(fallbackVisible[i]);
        }

        return visible;
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
                if (IsVisible(cells[i]))
                    return true;
            }

            return false;
        }

        return IsVisible(enemy.GridPosition);
    }
}