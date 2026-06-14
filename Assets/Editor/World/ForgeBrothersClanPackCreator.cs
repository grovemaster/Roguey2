#if UNITY_EDITOR
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Controller.Player;
using JRogue.Dialog;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class ForgeBrothersClanPackCreator
    {
        const string DwarfPlayerPath = "Assets/Prefabs/Actor/Race/DwarfPlayer.prefab";
        const string ForgeFatherPath = "Assets/Data/Racial/Dwarf/Ancestors/ForgeFather.asset";
        const string ForgeFatherTreePath = "Assets/Data/Racial/Dwarf/Ancestors/ForgeFatherTree.asset";
        const string ResourcesClanFolder = "Assets/Resources/Racial/Dwarf/Clans";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesInteractablesFolder = "Assets/Resources/Interactables";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string EffectsFolder = "Assets/Data/Interactables/Effects";
        const string StewardSpritePath = "Assets/Art/NPC/Sprites/NPC_ForgeBrothersSteward.png";
        const string StewardPortraitPath = "Assets/Art/Portraits/NPC/Portrait_ForgeBrothersSteward.png";

        [MenuItem("JRogue/Racial/Create Forge Brothers Clan Pack")]
        public static void CreateForgeBrothersClanPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(StewardSpritePath, new Color(0.784f, 0.627f, 0.376f));
            CreatePlaceholderPortrait(StewardPortraitPath, new Color(0.784f, 0.627f, 0.376f));
            AssetDatabase.Refresh();

            ConfigureTexture(StewardSpritePath, 32, FilterMode.Point);
            ConfigureTexture(StewardPortraitPath, 128, FilterMode.Point);

            AncestorDefinition forgeFather = AssetDatabase.LoadAssetAtPath<AncestorDefinition>(ForgeFatherPath);
            if (forgeFather == null)
            {
                Debug.LogError($"[ForgeBrothers] Missing {ForgeFatherPath}.");
                return;
            }

            DwarfClanDefinition clan = CreateClanDefinition(forgeFather);
            CreateAltarAssets(clan);
            PortraitDefinition portrait = CreatePortrait("Portrait_ForgeBrothersSteward", StewardPortraitPath);
            CreateStewardNpcPrefab(clan, portrait);
            UpdateForgeFatherTreeGates();
            TownPlazaMarkerLayout.ApplyAll(
                AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(TownPlazaMarkerLayout.StampPath));
            EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(TownPlazaMarkerLayout.StampPath));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[ForgeBrothers] Created clan, Hall of Ancestors altar (plaza cell 1,5), and steward NPC (cell 1,6). "
                + "Run JRogue/Town/Fix TownTest Scene, then play with a Dwarf party member.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesClanFolder);
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesInteractablesFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
            Directory.CreateDirectory(EffectsFolder);
        }

        static DwarfClanDefinition CreateClanDefinition(AncestorDefinition patron)
        {
            string path = $"{ResourcesClanFolder}/DwarfClan_ForgeBrothers.asset";
            DwarfClanDefinition clan = LoadOrCreate<DwarfClanDefinition>(path);
            clan.clanId = DwarfClanIds.ForgeBrothersClanId;
            clan.displayName = "Forge Brothers";
            clan.shortName = "Forge Brothers";
            clan.description =
                "Smith-clan dwarves who venerate the Forge-Father. Their hall keeps the ancestral altar "
                + "where initiates learn techniques of endurance and craft.";
            clan.patronAncestor = patron;
            clan.startingPrestige = 5;
            clan.altarFlavorTitle = "The Forge-Father's ancestors await your offering.";
            EditorUtility.SetDirty(clan);
            return clan;
        }

        static void CreateAltarAssets(DwarfClanDefinition clan)
        {
            AlwaysTruePrecondition alwaysTrue = AssetDatabase.LoadAssetAtPath<AlwaysTruePrecondition>(
                "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            if (alwaysTrue == null)
            {
                alwaysTrue = ScriptableObject.CreateInstance<AlwaysTruePrecondition>();
                AssetDatabase.CreateAsset(alwaysTrue, "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            }

            DwarfHallAncestorLearnEffect effect = LoadOrCreate<DwarfHallAncestorLearnEffect>(
                $"{EffectsFolder}/DwarfHallAncestorLearn_ForgeBrothers.asset");
            effect.clan = clan;
            EditorUtility.SetDirty(effect);

            Sprite spriteOff = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Interactables/Sprites/LeverSwitch_Off.png");

            InteractableTileDefinition altar = LoadOrCreate<InteractableTileDefinition>(
                $"{ResourcesInteractablesFolder}/HallOfAncestorsAltar_ForgeBrothers.asset");
            altar.interactableId = InteractableTileId.HallOfAncestorsAltar;
            altar.displayName = "Hall of Ancestors Altar";
            altar.kind = InteractableTileKind.Shrine;
            altar.blocksOccupancy = true;
            altar.bumpEnabled = true;
            altar.allowRepeatActivation = true;
            altar.preconditions = new InteractablePrecondition[] { alwaysTrue };
            altar.onActivateEffects = new InteractableEffect[] { effect };
            altar.spriteOff = spriteOff;
            altar.spriteOn = spriteOff;
            EditorUtility.SetDirty(altar);
        }

        static void CreateStewardNpcPrefab(DwarfClanDefinition clan, PortraitDefinition portrait)
        {
            GameObject dwarfPlayer = AssetDatabase.LoadAssetAtPath<GameObject>(DwarfPlayerPath);
            if (dwarfPlayer == null)
            {
                Debug.LogError($"[ForgeBrothers] Missing {DwarfPlayerPath}.");
                return;
            }

            string path = $"{ResourcesNpcFolder}/TownNpc_ForgeBrothersSteward.prefab";
            NpcDialogPackCreator.DeletePrefabIfPresent(path);

            GameObject instance = PrefabUtility.InstantiatePrefab(dwarfPlayer) as GameObject;
            instance.name = "TownNpc_ForgeBrothersSteward";
            instance.tag = "Untagged";

            Object.DestroyImmediate(instance.GetComponent<PlayerController>(), true);
            Object.DestroyImmediate(instance.GetComponent<InventoryManager>(), true);
            Object.DestroyImmediate(instance.GetComponent<InventoryCollector>(), true);
            Object.DestroyImmediate(instance.GetComponent<EquipmentManager>(), true);

            DwarfClanStewardNpcController controller = instance.AddComponent<DwarfClanStewardNpcController>();
            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("npcId").stringValue = DwarfClanIds.ForgeBrothersStewardNpcId;
            controllerSo.FindProperty("portrait").objectReferenceValue = portrait;
            controllerSo.FindProperty("clan").objectReferenceValue = clan;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(controller);
            actorSo.FindProperty("displayName").stringValue = "Forge Brothers Steward";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            CharacterStats stats = instance.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.race = Race.Dwarf;
                stats.racialSubsystem = RacialSubsystemKind.DwarfAncestry;
            }

            ApplySprite(instance, StewardSpritePath);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void UpdateForgeFatherTreeGates()
        {
            SpiritImprintGraph graph = AssetDatabase.LoadAssetAtPath<SpiritImprintGraph>(ForgeFatherTreePath);
            if (graph?.nodes == null)
                return;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                SpiritImprintNodeData node = graph.nodes[i];
                if (node == null)
                    continue;

                if (node.nodeId == "forge_blessing")
                {
                    node.requiredCharacterLevel = 1;
                    node.requiredClanMemberRank = 0;
                    node.requiredClanPrestige = 0;
                }
                else if (node.nodeId == "stone_endurance")
                {
                    node.requiredCharacterLevel = 1;
                    node.requiredClanMemberRank = 1;
                    node.requiredClanPrestige = 0;
                }
            }

            EditorUtility.SetDirty(graph);
        }

        static PortraitDefinition CreatePortrait(string assetName, string texturePath)
        {
            string path = $"{ResourcesPortraitsFolder}/{assetName}.asset";
            PortraitDefinition portrait = LoadOrCreate<PortraitDefinition>(path);
            portrait.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(portrait);
            return portrait;
        }

        static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        static void ApplySprite(GameObject instance, string spritePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;
        }

        static void CreatePlaceholderSprite(string path, Color color)
        {
            if (File.Exists(path))
                return;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, color);
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void CreatePlaceholderPortrait(string path, Color color) =>
            CreatePlaceholderSprite(path, color);

        static void ConfigureTexture(string path, int pixelsPerUnit, FilterMode filter)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filter;
            importer.SaveAndReimport();
        }
    }
}
#endif
