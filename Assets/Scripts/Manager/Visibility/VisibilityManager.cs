using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class VisibilityManager : MonoBehaviour
{
    // We now support multiple layers (Floor, Walls, Decorations, etc.)
    public List<Tilemap> tilemaps;
    public Color visibleColor = Color.white;
    public Color fogColor = new Color(0.15f, 0.15f, 0.2f, 1.0f); // Slightly blue-tinted dark grey

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