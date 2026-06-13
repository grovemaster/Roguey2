#if UNITY_EDITOR
using System.IO;
using JRogue.Ability;
using JRogue.Controller.Npc;
using JRogue.Controller.Player;
using JRogue.Dialog;
using JRogue.Item.Essence;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class FleshmetalForgemasterPackCreator
    {
        const string TieflingPlayerPrefabPath = "Assets/Prefabs/Actor/Race/TieflingPlayer.prefab";
        const string TieflingNpcPrefabPath = "Assets/Prefabs/Actor/Npc/TieflingNpc.prefab";
        const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string ResourcesRacialFolder = "Assets/Resources/Racial/Tiefling";
        const string DataRacialFolder = "Assets/Data/Racial/Tiefling";
        const string IronSleevePath = "Assets/Data/Racial/Tiefling/Implants/IronSleeveArm.asset";
        const string ThoracicPlatePath = "Assets/Data/Racial/Tiefling/Implants/ThoracicPlate.asset";
        const string SuddenStrengthAbilityPath = "Assets/Resources/Item/Ability/SuddenStrength_Standard.asset";
        const string DefaultLoadoutPath = "Assets/Data/Racial/Tiefling/DefaultTieflingRacialLoadout.asset";
        const string ForgemasterSpritePath = "Assets/Art/NPC/Sprites/NPC_FleshmetalForgemaster.png";
        const string ForgemasterPortraitPath = "Assets/Art/Portraits/NPC/Portrait_FleshmetalForgemaster.png";

        [MenuItem("JRogue/Town/Create Fleshmetal Forgemaster Pack")]
        public static void CreateFleshmetalForgemasterPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(ForgemasterSpritePath, new Color(0.72f, 0.22f, 0.18f));
            CreatePlaceholderPortrait(ForgemasterPortraitPath, new Color(0.72f, 0.22f, 0.18f));
            AssetDatabase.Refresh();

            ConfigureTexture(ForgemasterSpritePath, 32, FilterMode.Point);
            ConfigureTexture(ForgemasterPortraitPath, 128, FilterMode.Point);

            EnsureResourcesLoadoutCopy();
            TieflingForgemasterDefinition catalog = CreateDefaultCatalog();
            UpdateImplantCosts();
            WireIronSleeveGameplay();

            PortraitDefinition portrait = CreatePortrait("Portrait_FleshmetalForgemaster", ForgemasterPortraitPath);
            GameObject tieflingNpcBase = CreateTieflingNpcBasePrefab();
            CreateForgemasterTownNpcPrefab(portrait, tieflingNpcBase, catalog);
            UpdateTownStampMarker();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FleshmetalForgemaster] Created Tiefling Fleshmetal Forgemaster town NPC pack.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
            Directory.CreateDirectory(ResourcesRacialFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(TieflingNpcPrefabPath)!);
        }

        static void EnsureResourcesLoadoutCopy()
        {
            RacialLoadoutDefinition source =
                AssetDatabase.LoadAssetAtPath<RacialLoadoutDefinition>(DefaultLoadoutPath);
            if (source == null)
                return;

            string destPath = $"{ResourcesRacialFolder}/DefaultTieflingRacialLoadout.asset";
            RacialLoadoutDefinition existing = AssetDatabase.LoadAssetAtPath<RacialLoadoutDefinition>(destPath);
            if (existing == null)
            {
                if (!AssetDatabase.CopyAsset(DefaultLoadoutPath, destPath))
                    Debug.LogWarning("[FleshmetalForgemaster] Could not copy DefaultTieflingRacialLoadout to Resources.");
            }
        }

        static TieflingForgemasterDefinition CreateDefaultCatalog()
        {
            string path = $"{ResourcesRacialFolder}/DefaultFleshmetalForgemaster.asset";
            TieflingForgemasterDefinition catalog = LoadOrCreate<TieflingForgemasterDefinition>(path);
            catalog.forgemasterId = TieflingForgemasterIds.NpcId;
            catalog.offeredImplants = new System.Collections.Generic.List<CyborgImplantDefinition>();

            CyborgImplantDefinition ironSleeve = AssetDatabase.LoadAssetAtPath<CyborgImplantDefinition>(IronSleevePath);
            if (ironSleeve != null)
                catalog.offeredImplants.Add(ironSleeve);

            CyborgImplantDefinition thoracic = AssetDatabase.LoadAssetAtPath<CyborgImplantDefinition>(ThoracicPlatePath);
            if (thoracic != null)
                catalog.offeredImplants.Add(thoracic);

            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static void UpdateImplantCosts()
        {
            SetInstallCost(IronSleevePath, gold: 40);
            SetInstallCost(ThoracicPlatePath, gold: 60);
        }

        static void SetInstallCost(string assetPath, int gold)
        {
            CyborgImplantDefinition implant = AssetDatabase.LoadAssetAtPath<CyborgImplantDefinition>(assetPath);
            if (implant == null)
                return;

            implant.installCost = new CyborgImplantInstallCost { gold = gold };
            EditorUtility.SetDirty(implant);
        }

        static void WireIronSleeveGameplay()
        {
            CyborgImplantDefinition ironSleeve =
                AssetDatabase.LoadAssetAtPath<CyborgImplantDefinition>(IronSleevePath);
            AbilityAction suddenStrength =
                AssetDatabase.LoadAssetAtPath<AbilityAction>(SuddenStrengthAbilityPath);
            if (ironSleeve == null || suddenStrength == null)
            {
                Debug.LogWarning(
                    "[FleshmetalForgemaster] Could not wire Iron Sleeve active — missing implant or SuddenStrength_Standard asset.");
                return;
            }

            ironSleeve.statModifiers = new System.Collections.Generic.List<AttributeModifier>
            {
                new AttributeModifier { attribute = StatType.Strength, value = 10 }
            };
            ironSleeve.activeAbilities = new System.Collections.Generic.List<AbilityAction> { suddenStrength };
            EditorUtility.SetDirty(ironSleeve);
        }

        static PortraitDefinition CreatePortrait(string assetName, string texturePath)
        {
            string path = $"{ResourcesPortraitsFolder}/{assetName}.asset";
            PortraitDefinition portrait = LoadOrCreate<PortraitDefinition>(path);
            portrait.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(portrait);
            return portrait;
        }

        static GameObject CreateTieflingNpcBasePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(TieflingNpcPrefabPath);
            if (existing != null)
                return existing;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(TieflingPlayerPrefabPath);
            if (source == null)
                throw new FileNotFoundException($"Missing {TieflingPlayerPrefabPath}");

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            instance.name = "TieflingNpc";
            instance.tag = "Untagged";

            Object.DestroyImmediate(instance.GetComponent<PlayerController>(), true);
            instance.AddComponent<NpcController>();

            DestroyIfPresent<InventoryManager>(instance);
            DestroyIfPresent<InventoryCollector>(instance);
            DestroyIfPresent<EquipmentManager>(instance);
            DestroyIfPresent<RacialLoadoutApplier>(instance);
            DestroyIfPresent<TieflingImplantsRuntime>(instance);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, TieflingNpcPrefabPath);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        static void CreateForgemasterTownNpcPrefab(
            PortraitDefinition portrait,
            GameObject tieflingNpcBase,
            TieflingForgemasterDefinition catalog)
        {
            string path = $"{ResourcesNpcFolder}/TownNpc_FleshmetalForgemaster.prefab";
            GameObject instance = PrefabUtility.InstantiatePrefab(tieflingNpcBase) as GameObject;
            instance.name = "TownNpc_FleshmetalForgemaster";

            Object.DestroyImmediate(instance.GetComponent<NpcController>(), true);
            TieflingForgemasterNpcController controller = instance.AddComponent<TieflingForgemasterNpcController>();

            SerializedObject npcSo = new SerializedObject(controller);
            npcSo.FindProperty("npcId").stringValue = TieflingForgemasterIds.NpcId;
            npcSo.FindProperty("portrait").objectReferenceValue = portrait;
            npcSo.FindProperty("forgemasterCatalog").objectReferenceValue = catalog;
            npcSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(controller);
            actorSo.FindProperty("displayName").stringValue = "Tiefling Fleshmetal Forgemaster";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ForgemasterSpritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void UpdateTownStampMarker()
        {
            var stamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(StampPath);
            if (stamp == null)
                return;

            stamp.SetMarker(StampMarkerIds.FleshmetalForgemaster, new Vector3Int(8, 5, 0));
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
