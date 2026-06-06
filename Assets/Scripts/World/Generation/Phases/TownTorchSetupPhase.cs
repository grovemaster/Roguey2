using JRogue.Manager.Map;
using JRogue.World.Generation;
using JRogue.World.Lighting;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.World.Generation.Phases
{
    /// <summary>Places always-lit wall torches on town_main from stamp markers.</summary>
    public sealed class TownTorchSetupPhase : IDungeonGenerationPhase
    {
        public const string TownFloorId = "town_main";
        public const string LogPrefix = "[TownTorch]";

        const string TorchDefinitionResourcesPath = "Lighting/Torch";
        const string TorchDefinitionEditorPath = "Assets/Prefabs/Lighting/Torch.asset";
        const string TorchSpriteResourcesPath = "Lighting/WallTorch_Lit";
        const string TorchSpriteEditorPath = "Assets/Resources/Lighting/WallTorch_Lit.png";

        static readonly (string markerId, string label)[] TorchMarkers =
        {
            (StampMarkerIds.TownTorchWest, "west"),
            (StampMarkerIds.TownTorchNorth, "north"),
            (StampMarkerIds.TownTorchEast, "east"),
        };

        static Tile _torchWallTile;

        public void Execute(DungeonGenerationContext context)
        {
            if (context?.Definition == null || context.Definition.FloorId != TownFloorId)
                return;

            int placed = ApplyTownTorches(context.Definition, context.Definition.LayoutStamp);
            DungeonGenerationLog.Phase(nameof(TownTorchSetupPhase), $"placed {placed} wall torch(s).");
        }

        /// <summary>Idempotent torch wall tiles + live emitters (generation and floor re-activation).</summary>
        public static int ApplyTownTorches(DungeonFloorDefinition def, DungeonLayoutStamp stamp = null)
        {
            if (def == null || def.FloorId != TownFloorId)
                return 0;

            stamp ??= def.LayoutStamp;
            MapManager map = MapManager.Instance;
            LightingService lighting = LightingService.Instance;
            if (map == null || map.WallMap == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownTorchSetupPhase)} missing MapManager or WallMap.");
                return 0;
            }

            if (lighting == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownTorchSetupPhase)} missing LightingService.");
                return 0;
            }

            LightEmitterDefinition torchDefinition = LoadTorchDefinition();
            if (torchDefinition == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownTorchSetupPhase)} missing torch emitter definition.");
                return 0;
            }

            Tile torchTile = GetOrCreateTorchWallTile(def.WallTile);
            int placed = 0;

            for (int i = 0; i < TorchMarkers.Length; i++)
            {
                (string markerId, string label) = TorchMarkers[i];
                if (!TryResolveMarkerCell(stamp, markerId, out Vector3Int cell))
                {
                    DungeonGenerationLog.Warn($"{nameof(TownTorchSetupPhase)} missing marker '{markerId}'.");
                    continue;
                }

                if (!map.WallMap.HasTile(cell) && def.WallTile != null)
                    map.WallMap.SetTile(cell, def.WallTile);

                if (torchTile != null)
                    map.WallMap.SetTile(cell, torchTile);

                lighting.EnableEmitter(
                    cell,
                    torchDefinition,
                    LightLevel.TorchEmission,
                    $"town-torch-{label}");

                placed++;
                Debug.Log($"{LogPrefix} Registered torch ({label}) at {cell}.");
            }

            return placed;
        }

        static LightEmitterDefinition LoadTorchDefinition()
        {
            LightEmitterDefinition def = Resources.Load<LightEmitterDefinition>(TorchDefinitionResourcesPath);
#if UNITY_EDITOR
            if (def == null)
                def = AssetDatabase.LoadAssetAtPath<LightEmitterDefinition>(TorchDefinitionEditorPath);
#endif
            return def;
        }

        static Tile GetOrCreateTorchWallTile(TileBase fallbackWallTile)
        {
            if (_torchWallTile != null)
                return _torchWallTile;

            Sprite sprite = Resources.Load<Sprite>(TorchSpriteResourcesPath);
#if UNITY_EDITOR
            if (sprite == null)
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TorchSpriteEditorPath);
#endif
            if (sprite == null)
                return fallbackWallTile as Tile;

            _torchWallTile = ScriptableObject.CreateInstance<Tile>();
            _torchWallTile.sprite = sprite;
            return _torchWallTile;
        }

        static bool TryResolveMarkerCell(DungeonLayoutStamp stamp, string markerId, out Vector3Int cell)
        {
            cell = default;
            return stamp != null && stamp.TryGetMarker(markerId, out cell);
        }
    }
}
