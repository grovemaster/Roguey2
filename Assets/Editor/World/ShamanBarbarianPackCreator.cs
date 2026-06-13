#if UNITY_EDITOR
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Controller.Player;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class ShamanBarbarianPackCreator
    {
        const string BarbarianPlayerPrefabPath = "Assets/Prefabs/Actor/Race/BarbarianPlayer.prefab";
        const string BarbarianNpcPrefabPath = "Assets/Prefabs/Actor/Npc/BarbarianNpc.prefab";
        const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string ShamanSpritePath = "Assets/Art/NPC/Sprites/NPC_ShamanBarbarian.png";
        const string ShamanPortraitPath = "Assets/Art/Portraits/NPC/Portrait_ShamanBarbarian.png";

        [MenuItem("JRogue/Town/Create Shaman Barbarian Pack")]
        public static void CreateShamanBarbarianPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(ShamanSpritePath, new Color(0.58f, 0.36f, 0.72f));
            CreatePlaceholderPortrait(ShamanPortraitPath, new Color(0.58f, 0.36f, 0.72f));
            AssetDatabase.Refresh();

            ConfigureTexture(ShamanSpritePath, 32, FilterMode.Point);
            ConfigureTexture(ShamanPortraitPath, 128, FilterMode.Point);

            PortraitDefinition portrait = CreatePortrait("Portrait_ShamanBarbarian", ShamanPortraitPath);
            GameObject barbarianNpcBase = CreateBarbarianNpcBasePrefab();
            CreateShamanTownNpcPrefab(portrait, barbarianNpcBase);
            UpdateSampleGraphCosts();
            UpdateTownStampMarker();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ShamanBarbarian] Created Shaman Barbarian town NPC pack.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(BarbarianNpcPrefabPath)!);
        }

        static PortraitDefinition CreatePortrait(string assetName, string texturePath)
        {
            string path = $"{ResourcesPortraitsFolder}/{assetName}.asset";
            PortraitDefinition portrait = LoadOrCreate<PortraitDefinition>(path);
            portrait.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(portrait);
            return portrait;
        }

        static GameObject CreateBarbarianNpcBasePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BarbarianNpcPrefabPath);
            if (existing != null)
                return existing;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(BarbarianPlayerPrefabPath);
            if (source == null)
                throw new FileNotFoundException($"Missing {BarbarianPlayerPrefabPath}");

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            instance.name = "BarbarianNpc";
            instance.tag = "Untagged";

            Object.DestroyImmediate(instance.GetComponent<PlayerController>(), true);
            instance.AddComponent<NpcController>();

            DestroyIfPresent<InventoryManager>(instance);
            DestroyIfPresent<InventoryCollector>(instance);
            DestroyIfPresent<EquipmentManager>(instance);
            DestroyIfPresent<RacialLoadoutApplier>(instance);
            DestroyIfPresent<SpiritImprintRuntime>(instance);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, BarbarianNpcPrefabPath);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        static void CreateShamanTownNpcPrefab(PortraitDefinition portrait, GameObject barbarianNpcBase)
        {
            string path = $"{ResourcesNpcFolder}/TownNpc_ShamanBarbarian.prefab";
            GameObject instance = PrefabUtility.InstantiatePrefab(barbarianNpcBase) as GameObject;
            instance.name = "TownNpc_ShamanBarbarian";

            Object.DestroyImmediate(instance.GetComponent<NpcController>(), true);
            SpiritImprintShamanNpcController shaman = instance.AddComponent<SpiritImprintShamanNpcController>();

            SerializedObject npcSo = new SerializedObject(shaman);
            npcSo.FindProperty("npcId").stringValue = SpiritImprintShamanIds.NpcId;
            npcSo.FindProperty("portrait").objectReferenceValue = portrait;
            npcSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(shaman);
            actorSo.FindProperty("displayName").stringValue = "Shaman Barbarian";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShamanSpritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void UpdateSampleGraphCosts()
        {
            var graph = AssetDatabase.LoadAssetAtPath<SpiritImprintGraph>(
                "Assets/Data/Racial/SpiritImprint/BarbarianSpiritImprintSample.asset");
            if (graph?.nodes == null)
                return;

            ItemData giantsBlade = AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Resources/Item/Weapon/Giants_Blade.asset");

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                SpiritImprintNodeData node = graph.nodes[i];
                if (node == null)
                    continue;

                switch (node.nodeId)
                {
                    case "tier1_str":
                        node.unlockCost = new SpiritImprintUnlockCost { gold = 30 };
                        break;
                    case "tier1_dex":
                        node.unlockCost = new SpiritImprintUnlockCost
                        {
                            gold = 20,
                            items = giantsBlade != null
                                ? new[]
                                {
                                    new SpiritImprintItemCost { item = giantsBlade, quantity = 1 },
                                }
                                : null,
                        };
                        break;
                    case "tier2_constitution":
                        node.unlockCost = new SpiritImprintUnlockCost
                        {
                            gold = 50,
                            storyFlags = new[]
                            {
                                new SpiritImprintFlagCost
                                {
                                    flagId = "quest_skeleton_proof",
                                    expectedValue = true,
                                },
                            },
                        };
                        break;
                }
            }

            EditorUtility.SetDirty(graph);
        }

        static void UpdateTownStampMarker()
        {
            var stamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(StampPath);
            if (stamp == null)
                return;

            TownPlazaMarkerLayout.ApplyAll(stamp);
            EditorUtility.SetDirty(stamp);
        }

        static void CreatePlaceholderSprite(string assetPath, Color color) =>
            CreatePlaceholderTexture(assetPath, 32, color, true);

        static void CreatePlaceholderPortrait(string assetPath, Color color) =>
            CreatePlaceholderTexture(assetPath, 128, color, false);

        static void CreatePlaceholderTexture(string assetPath, int size, Color color, bool feetPivot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = size <= 32 ? 32 : 128;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            if (feetPivot)
                importer.spritePivot = new Vector2(0.5f, 0.25f);
            importer.SaveAndReimport();
        }

        static void ConfigureTexture(string path, int ppu, FilterMode filter)
        {
            if (!File.Exists(path))
                return;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            if (ppu == 32)
                importer.spritePivot = new Vector2(0.5f, 0.25f);
            importer.SaveAndReimport();
        }

        static void DestroyIfPresent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null)
                Object.DestroyImmediate(component, true);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
