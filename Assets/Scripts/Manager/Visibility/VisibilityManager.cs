using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Visibility.Algorithm;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class VisibilityManager : MonoBehaviour
{
    // We now support multiple layers (Floor, Walls, Decorations, etc.)
    public List<Tilemap> tilemaps;
    public Color visibleColor = Color.white;
    public Color fogColor = new Color(0.15f, 0.15f, 0.2f, 1.0f); // Slightly blue-tinted dark grey
    public Color enemySightDebugTint = new Color(1f, 0.35f, 0.35f, 1f);
    [Range(0f, 1f)] public float enemySightDebugBlend = 0.3f;

    public int viewRange = 8;

    // We assume your Player is tagged "Player"
    private Transform playerTransform;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        InitializeMap();
    }

    public void InitializeMap()
    {
        foreach (Tilemap tm in tilemaps)
        {
            if (tm == null) continue;

            // Loop through every tile in this specific layer
            foreach (var pos in tm.cellBounds.allPositionsWithin)
            {
                if (tm.HasTile(pos))
                {
                    tm.SetTileFlags(pos, TileFlags.None);
                    tm.SetColor(pos, fogColor);
                }
            }
        }
    }

    public void UpdateVisibility(List<Vector3Int> visiblePositions)
    {
        // 1. Reset all layers to Fog
        foreach (Tilemap tm in tilemaps)
        {
            if (tm == null) continue;

            foreach (var pos in tm.cellBounds.allPositionsWithin)
            {
                if (tm.HasTile(pos)) tm.SetColor(pos, fogColor);
            }
        }

        // 2. Set currently visible positions to White across all layers
        foreach (Vector3Int pos in visiblePositions)
        {
            foreach (Tilemap tm in tilemaps)
            {
                if (tm == null) continue;

                if (tm.HasTile(pos))
                {
                    tm.SetColor(pos, visibleColor);
                }
            }
        }
    }

    // Call this every time the player moves
    public void RefreshVision()
    {
        // Use the controlled party member's logical grid cell (authoritative for gameplay).
        // Tilemap cells in this project use z = 0 (see MapManager floor/wall checks); mixing in
        // transform Z or a stale tagged object gives a cell key that HasTile never matches, so
        // the tile under the actor stays fog after step 1 of UpdateVisibility.
        Vector3Int playerGridPos;
        BaseActor active = PartyManager.Instance != null ? PartyManager.Instance.GetActiveMember() : null;
        if (active != null)
        {
            Vector3Int gp = active.GridPosition;
            playerGridPos = new Vector3Int(gp.x, gp.y, 0);
        }
        else if (playerTransform != null)
        {
            Vector3Int fp = Vector3Int.FloorToInt(playerTransform.position);
            playerGridPos = new Vector3Int(fp.x, fp.y, 0);
        }
        else
        {
            return;
        }

        // Run the algorithm
        // We pass MapManager.Instance.IsWalkable as the "IsOpaque" check
        // Note: You might need to flip the logic (IsOpaque = !IsWalkable)
        List<Vector3Int> visible = ShadowCaster.GetVisibleTiles(
            playerGridPos,
            viewRange,
            pos => MapManager.Instance != null && !MapManager.Instance.IsWalkable(pos)
        );

        UpdateVisibility(visible);
    }

    void Update()
    {
        // For debugging, refresh every time space is pressed
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RefreshVision();
        }

        // Debug overlay: tint tiles currently visible to enemies.
        if (Keyboard.current != null && Keyboard.current.semicolonKey.wasPressedThisFrame)
        {
            DebugOverlayEnemySight();
        }
    }

    private void DebugOverlayEnemySight()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>();
        if (enemies == null || enemies.Length == 0 || MapManager.Instance == null) return;

        HashSet<Vector3Int> enemyVisibleTiles = new HashSet<Vector3Int>();
        enemyVisibleTiles.Clear();

        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null) continue;

            Roguey2.Sensing.ConeSightUtility.CollectVisibleTiles(
                enemy,
                MapManager.Instance,
                enemy.VisionRange,
                enemy.PrimaryConeAngle,
                enemy.PeripheralRangeMultiplier,
                enemyVisibleTiles);
        }

        foreach (Vector3Int pos in enemyVisibleTiles)
        {
            foreach (Tilemap tm in tilemaps)
            {
                if (tm == null || !tm.HasTile(pos)) continue;

                Color current = tm.GetColor(pos);
                tm.SetColor(pos, Color.Lerp(current, enemySightDebugTint, enemySightDebugBlend));
            }
        }

        Debug.Log($"[SIGHT-DEBUG] Enemy FOV overlay: {enemies.Length} enemies, {enemyVisibleTiles.Count} tiles tinted.");
    }

    // Temporary Test Logic
    // void Update()
    // {
    //     // Check if the keyboard is connected and the Space key was pressed this frame
    //     if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
    //     {
    //         InitializeMap();

    //         // Test: Light up a 3x3 area at the center (0,0,0)
    //         List<Vector3Int> testArea = new List<Vector3Int>();
    //         for (int x = -1; x <= 1; x++)
    //         {
    //             for (int y = -1; y <= 1; y++)
    //             {
    //                 testArea.Add(new Vector3Int(x, y, 0));
    //             }
    //         }

    //         UpdateVisibility(testArea);
    //         Debug.Log("[Visibility-Debug] Space pressed. 3x3 area lit at origin.");
    //     }
    // }
}