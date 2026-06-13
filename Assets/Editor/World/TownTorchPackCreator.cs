#if UNITY_EDITOR
using System.IO;
using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class TownTorchPackCreator
    {
        const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";
        const string TorchDefinitionSourcePath = "Assets/Prefabs/Lighting/Torch.asset";
        const string TorchDefinitionResourcesPath = "Assets/Resources/Lighting/Torch.asset";
        const string TorchSpriteArtPath = "Assets/Art/Lighting/Sprites/WallTorch_Lit.png";
        const string TorchSpriteResourcesPath = "Assets/Resources/Lighting/WallTorch_Lit.png";
        const int MinTorchSeparation = 4;

        static readonly (string markerId, string label)[] TorchPlacements =
        {
            (StampMarkerIds.TownTorchWest, "west"),
            (StampMarkerIds.TownTorchNorth, "north"),
            (StampMarkerIds.TownTorchEast, "east"),
        };

        [MenuItem("JRogue/Town/Place Town Torches")]
        public static void PlaceTownTorches()
        {
            EnsureFolders();
            EnsureTorchDefinitionInResources();
            ConfigureTexture(TorchSpriteArtPath, 32, FilterMode.Point);
            ConfigureTexture(TorchSpriteResourcesPath, 32, FilterMode.Point);

            var stamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(StampPath);
            if (stamp == null)
            {
                Debug.LogError($"[TownTorch] Missing stamp at {StampPath}.");
                return;
            }

            Vector3Int playerStart = stamp.PlayerStart;
            int warnings = 0;

            TownPlazaMarkerLayout.ApplyAll(stamp);

            for (int i = 0; i < TorchPlacements.Length; i++)
            {
                (string markerId, string label) = TorchPlacements[i];
                if (!TownPlazaMarkerLayout.TryGetCell(markerId, out Vector3Int cell))
                    continue;
                if (!stamp.IsWall(cell.x, cell.y))
                {
                    Debug.LogWarning($"[TownTorch] {label} marker {cell} is not a wall cell in the stamp.");
                    warnings++;
                }

                if (ManhattanDistance(cell, playerStart) < MinTorchSeparation)
                {
                    Debug.LogWarning(
                        $"[TownTorch] {label} torch at {cell} is within {MinTorchSeparation} tiles of playerStart {playerStart}.");
                    warnings++;
                }

                for (int j = i + 1; j < TorchPlacements.Length; j++)
                {
                    if (!TownPlazaMarkerLayout.TryGetCell(TorchPlacements[j].markerId, out Vector3Int otherCell))
                        continue;

                    if (ManhattanDistance(cell, otherCell) < MinTorchSeparation)
                    {
                        Debug.LogWarning(
                            $"[TownTorch] {label} and {TorchPlacements[j].label} torches are closer than {MinTorchSeparation} tiles.");
                        warnings++;
                    }
                }
            }

            EditorUtility.SetDirty(stamp);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                warnings == 0
                    ? "[TownTorch] Placed 3 town torch stamp markers on perimeter walls."
                    : $"[TownTorch] Updated stamp markers with {warnings} validation warning(s).");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/Lighting/Sprites");
            Directory.CreateDirectory("Assets/Resources/Lighting");
        }

        static void EnsureTorchDefinitionInResources()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TorchDefinitionResourcesPath) != null)
                return;

            if (!AssetDatabase.CopyAsset(TorchDefinitionSourcePath, TorchDefinitionResourcesPath))
                Debug.LogWarning($"[TownTorch] Could not copy torch definition to {TorchDefinitionResourcesPath}.");
        }

        static void ConfigureTexture(string path, int ppu, FilterMode filter)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[TownTorch] Missing sprite at {path}.");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePivot = new Vector2(0.5f, 0.25f);
            importer.SaveAndReimport();
        }

        static int ManhattanDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
#endif
