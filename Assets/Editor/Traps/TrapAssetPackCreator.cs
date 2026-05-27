#if UNITY_EDITOR
using System.IO;
using JRogue.Traps;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Traps
{
    public static class TrapAssetPackCreator
    {
        const string Root = "Assets/Data/Traps";
        const string PlacementSetsPath = Root + "/PlacementSets";
        const string SpritesPath = "Assets/Art/Traps/Sprites";

        const string CreateMenuPath = "Assets/Create/JRogue/Traps/Create QA Trap Asset Pack";
        const string TopMenuPath = "JRogue/Traps/Create QA Trap Asset Pack";

        [MenuItem(CreateMenuPath, false, 0)]
        [MenuItem(TopMenuPath, false, 0)]
        public static void CreateQaTrapAssetPack()
        {
            EnsureFolders();

            Sprite spikeSprite = LoadSprite(SpritesPath + "/SpikeTrap_Revealed.png");
            Sprite bearSprite = LoadSprite(SpritesPath + "/BearTrap_Revealed_Triggered.png");
            Sprite dartIdle = LoadSprite(SpritesPath + "/DartTrap_Revealed_Idle.png");
            Sprite dartFire = LoadSprite(SpritesPath + "/DartTrap_Revealed_Fire.png");

            TrapDefinition spikeVisible = CreateDefinition(
                Root + "/TrapDefinition_Spike_Visible.asset",
                TrapId.Spike,
                "Spike Trap",
                TrapPlacement.Floor,
                TrapVisibility.Visible,
                TrapTriggerLimit.Infinite,
                finiteCharges: 0,
                triggerRange: 1,
                piercingDamage: 8,
                spikeSprite);

            TrapDefinition spikeInvisible = CreateDefinition(
                Root + "/TrapDefinition_Spike_Invisible.asset",
                TrapId.Spike,
                "Spike Trap",
                TrapPlacement.Floor,
                TrapVisibility.Invisible,
                TrapTriggerLimit.Infinite,
                finiteCharges: 0,
                triggerRange: 1,
                piercingDamage: 8,
                spikeSprite);

            TrapDefinition bear = CreateDefinition(
                Root + "/TrapDefinition_Bear.asset",
                TrapId.Bear,
                "Bear Trap",
                TrapPlacement.Floor,
                TrapVisibility.Visible,
                TrapTriggerLimit.Once,
                finiteCharges: 0,
                triggerRange: 1,
                piercingDamage: 15,
                bearSprite);

            TrapDefinition dart = CreateDartDefinition(
                Root + "/TrapDefinition_Dart.asset",
                dartIdle,
                dartFire);

            TrapPlacementSet placementSet = GetOrCreate<TrapPlacementSet>(
                PlacementSetsPath + "/SampleScene_Traps.asset");
            placementSet.placements = new[]
            {
                new TrapPlacementEntry { cell = new Vector3Int(-3, -2, 0), definition = spikeVisible },
                new TrapPlacementEntry { cell = new Vector3Int(-2, -3, 0), definition = spikeInvisible },
                new TrapPlacementEntry { cell = new Vector3Int(-1, -2, 0), definition = bear },
                new TrapPlacementEntry { cell = new Vector3Int(-6, -3, 0), definition = dart },
            };
            EditorUtility.SetDirty(placementSet);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = placementSet;
            Debug.Log(
                $"[Trap] QA trap pack created under {Root}. " +
                $"Assign '{placementSet.name}' on TrapBootstrap in SampleScene.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Traps/PlacementSets"));
            AssetDatabase.Refresh();
        }

        static T GetOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        static TrapDefinition CreateDefinition(
            string assetPath,
            TrapId id,
            string displayName,
            TrapPlacement placement,
            TrapVisibility visibility,
            TrapTriggerLimit triggerLimit,
            int finiteCharges,
            int triggerRange,
            int piercingDamage,
            Sprite revealedSprite)
        {
            TrapDefinition def = GetOrCreate<TrapDefinition>(assetPath);
            def.trapId = id;
            def.displayName = displayName;
            def.placement = placement;
            def.initialVisibility = visibility;
            def.detectionThreshold = 12;
            def.triggerLimit = triggerLimit;
            def.finiteCharges = finiteCharges;
            def.triggerRange = triggerRange;
            def.piercingDamage = piercingDamage;
            def.revealedSprite = revealedSprite;
            EditorUtility.SetDirty(def);
            return def;
        }

        static TrapDefinition CreateDartDefinition(
            string assetPath,
            Sprite idleSprite,
            Sprite fireSprite)
        {
            TrapDefinition def = GetOrCreate<TrapDefinition>(assetPath);
            def.trapId = TrapId.Dart;
            def.displayName = "Dart Trap";
            def.placement = TrapPlacement.Wall;
            def.initialVisibility = TrapVisibility.Visible;
            def.detectionThreshold = 12;
            def.triggerLimit = TrapTriggerLimit.Finite;
            def.finiteCharges = 3;
            def.triggerRange = 1;
            def.piercingDamage = 10;
            def.revealedSprite = idleSprite;
            def.revealedTriggeredSprite = fireSprite;
            EditorUtility.SetDirty(def);
            return def;
        }

        static Sprite LoadSprite(string assetPath)
        {
            ConfigureTrapSpriteImporter(assetPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        static void ConfigureTrapSpriteImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            bool dirty = false;
            if (importer.spritePixelsPerUnit != 32f)
            {
                importer.spritePixelsPerUnit = 32f;
                dirty = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }
    }
}
#endif
