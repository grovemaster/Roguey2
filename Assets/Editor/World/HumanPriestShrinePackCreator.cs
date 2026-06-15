#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Ability.ArcaneMight;
using JRogue.Ability.Heal;
using JRogue.Ability.LightningBolt;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class HumanPriestShrinePackCreator
    {
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesQuestFolder = "Assets/Resources/Quest";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string ResourcesPatronFolder = "Assets/Resources/Racial/Human/Patrons";
        const string ResourcesInvocationFolder = "Assets/Resources/Racial/Human/Invocations";
        const string ResourcesVowFolder = "Assets/Resources/Racial/Human/Vows";
        const string ResourcesAbilityFolder = "Assets/Resources/Abilities/Priest";
        const string ResourcesCatalogFolder = "Assets/Resources/Racial/Human";
        const string StewardSpritePath = "Assets/Art/NPC/Sprites/NPC_PriestShrineSteward.png";
        const string StewardPortraitPath = "Assets/Art/Portraits/NPC/Portrait_PriestShrineSteward.png";

        [MenuItem("JRogue/Racial/Create Human Priest Shrine Pack")]
        public static void CreateHumanPriestShrinePack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(StewardSpritePath, new Color(0.82f, 0.84f, 0.9f));
            CreatePlaceholderPortrait(StewardPortraitPath, new Color(0.82f, 0.84f, 0.9f));
            AssetDatabase.Refresh();

            ConfigureTexture(StewardSpritePath, 32, FilterMode.Point);
            ConfigureTexture(StewardPortraitPath, 128, FilterMode.Point);

            PortraitDefinition portrait = CreatePortrait("Portrait_PriestShrineSteward", StewardPortraitPath);
            PriestPietyProgressionDefinition progression = CreatePietyProgression();
            PatronGodDefinition patron = CreateArgentVigilPatron();
            PriestInvocationDefinition layOnHands = CreateInvocation(
                "PriestInvocation_LayOnHands",
                "priest_lay_on_hands",
                "Lay on Hands",
                0,
                1,
                5,
                CreateHealAbility());
            PriestInvocationDefinition ward = CreateInvocation(
                "PriestInvocation_Ward",
                "priest_ward",
                "Ward",
                0,
                1,
                4,
                CreateWardAbility());
            PriestInvocationDefinition smiteUndead = CreateInvocation(
                "PriestInvocation_SmiteUndead",
                "priest_smites_undead",
                "Smite Undead",
                10,
                1,
                6,
                CreateSmiteAbility());
            PriestInvocationDefinition sanctuary = CreateInvocation(
                "PriestInvocation_Sanctuary",
                "priest_sanctuary",
                "Sanctuary",
                20,
                3,
                8,
                CreateSanctuaryAbility());

            CreateInvocationCatalog(layOnHands, ward, smiteUndead, sanctuary);
            CreateVows();
            CreateInitiationQuest();
            UpdatePatronInvocationIds(patron, layOnHands, ward, smiteUndead, sanctuary);

            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogError($"[PriestShrine] Missing base NPC prefab at {HumanNpcPrefabPath}.");
                return;
            }

            CreateShrineStewardNpcPrefab(portrait, humanNpc);
            UpdateTownStampMarkers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PriestShrine] Human Priest shrine pack created.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesQuestFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
            Directory.CreateDirectory(ResourcesPatronFolder);
            Directory.CreateDirectory(ResourcesInvocationFolder);
            Directory.CreateDirectory(ResourcesVowFolder);
            Directory.CreateDirectory(ResourcesAbilityFolder);
            Directory.CreateDirectory(ResourcesCatalogFolder);
        }

        static PriestPietyProgressionDefinition CreatePietyProgression()
        {
            string path = $"{ResourcesCatalogFolder}/PriestPietyProgression_Default.asset";
            var progression = LoadOrCreate<PriestPietyProgressionDefinition>(path);
            progression.maxPiety = 100;
            progression.startingPietyOnCommit = 10;
            progression.bands = new List<PriestPietyBandData>
            {
                new() { minPietyInclusive = 0, devotionSlots = 2, starLabel = "★☆☆☆☆" },
                new() { minPietyInclusive = 20, devotionSlots = 3, starLabel = "★★☆☆☆" },
                new() { minPietyInclusive = 40, devotionSlots = 4, starLabel = "★★★☆☆" },
                new() { minPietyInclusive = 60, devotionSlots = 6, starLabel = "★★★★☆" },
                new() { minPietyInclusive = 80, devotionSlots = 8, starLabel = "★★★★★" },
            };
            EditorUtility.SetDirty(progression);
            return progression;
        }

        static PatronGodDefinition CreateArgentVigilPatron()
        {
            string path = $"{ResourcesPatronFolder}/PatronGod_ArgentVigil.asset";
            var patron = LoadOrCreate<PatronGodDefinition>(path);
            patron.godId = HumanPriestShrineIds.ArgentVigilGodId;
            patron.displayName = "Argent Vigil";
            patron.description = "Patron of watchful light, undead-slaying, and covenant oaths.";
            patron.conductRules = new List<DivineConductRuleData>
            {
                new()
                {
                    conductId = "argent_slay_undead",
                    kind = DivineConductKind.PietyGain,
                    triggerId = DivineConductTriggers.KillUndead,
                    pietyDelta = 2,
                    description = "Slay the undead.",
                },
                new()
                {
                    conductId = "argent_explore",
                    kind = DivineConductKind.PietyGain,
                    triggerId = DivineConductTriggers.ExploreNewTile,
                    pietyDelta = 1,
                    description = "Explore new ground.",
                },
                new()
                {
                    conductId = "argent_no_poison",
                    kind = DivineConductKind.Taboo,
                    triggerId = DivineConductTriggers.ItemUsePoison,
                    pietyDelta = 5,
                    description = "Poison is anathema to the Vigil.",
                },
            };
            patron.vowIds = new List<string> { "vow_peacebound", "vow_essence_abstinence" };
            EditorUtility.SetDirty(patron);
            return patron;
        }

        static void UpdatePatronInvocationIds(
            PatronGodDefinition patron,
            params PriestInvocationDefinition[] invocations)
        {
            if (patron == null)
                return;

            patron.invocationIds = new List<string>();
            for (int i = 0; i < invocations.Length; i++)
            {
                if (invocations[i] != null && !string.IsNullOrWhiteSpace(invocations[i].invocationId))
                    patron.invocationIds.Add(invocations[i].invocationId);
            }

            EditorUtility.SetDirty(patron);
        }

        static HealAbility CreateHealAbility()
        {
            string path = $"{ResourcesAbilityFolder}/Priest_LayOnHands.asset";
            var ability = LoadOrCreate<HealAbility>(path);
            ability.abilityName = "Lay on Hands";
            ability.requiresTarget = true;
            ability.healAmount = 20;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        static ArcaneMightAbility CreateWardAbility()
        {
            string path = $"{ResourcesAbilityFolder}/Priest_Ward.asset";
            var ability = LoadOrCreate<ArcaneMightAbility>(path);
            ability.abilityName = "Ward";
            ability.requiresTarget = true;
            ability.strengthBonus = 25;
            ability.durationTurns = 5;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        static LightningBoltAbility CreateSmiteAbility()
        {
            string path = $"{ResourcesAbilityFolder}/Priest_SmiteUndead.asset";
            var ability = LoadOrCreate<LightningBoltAbility>(path);
            ability.abilityName = "Smite Undead";
            ability.requiresTarget = true;
            ability.lightningDamage = 14;
            ability.splashRadius = 0;
            ability.splashZone = null;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        static ArcaneMightAbility CreateSanctuaryAbility()
        {
            string path = $"{ResourcesAbilityFolder}/Priest_Sanctuary.asset";
            var ability = LoadOrCreate<ArcaneMightAbility>(path);
            ability.abilityName = "Sanctuary";
            ability.requiresTarget = false;
            ability.strengthBonus = 0;
            ability.durationTurns = 3;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        static PriestInvocationDefinition CreateInvocation(
            string fileName,
            string invocationId,
            string displayName,
            int requiredPiety,
            int requiredLevel,
            int divinePowerCost,
            JRogue.Ability.AbilityAction ability)
        {
            string path = $"{ResourcesInvocationFolder}/{fileName}.asset";
            var invocation = LoadOrCreate<PriestInvocationDefinition>(path);
            invocation.invocationId = invocationId;
            invocation.displayName = displayName;
            invocation.requiredPiety = requiredPiety;
            invocation.requiredCharacterLevel = requiredLevel;
            invocation.divinePowerCost = divinePowerCost;
            invocation.pietyInvokeCost = 0;
            invocation.ability = ability;
            EditorUtility.SetDirty(invocation);
            return invocation;
        }

        static void CreateInvocationCatalog(params PriestInvocationDefinition[] invocations)
        {
            string path = $"{ResourcesCatalogFolder}/PriestInvocationCatalog.asset";
            var catalog = LoadOrCreate<PriestInvocationCatalog>(path);
            catalog.invocations = new List<PriestInvocationDefinition>(invocations);
            EditorUtility.SetDirty(catalog);
        }

        static void CreateVows()
        {
            CreateVow(
                "PriestVow_Peacebound",
                "vow_peacebound",
                "Peacebound",
                PriestVowScope.Personal,
                PriestVowRuleKind.NoBladedWeapons);
            CreateVow(
                "PriestVow_EssenceAbstinence",
                "vow_essence_abstinence",
                "Essence Abstinence",
                PriestVowScope.Party,
                PriestVowRuleKind.PartyNoEssenceConsumption);
        }

        static void CreateVow(
            string fileName,
            string vowId,
            string displayName,
            PriestVowScope scope,
            PriestVowRuleKind ruleKind)
        {
            string path = $"{ResourcesVowFolder}/{fileName}.asset";
            var vow = LoadOrCreate<PriestVowDefinition>(path);
            vow.vowId = vowId;
            vow.displayName = displayName;
            vow.scope = scope;
            vow.ruleKind = ruleKind;
            vow.pietyRewardOnSuccess = 10;
            EditorUtility.SetDirty(vow);
        }

        static QuestDefinition CreateInitiationQuest()
        {
            string path = $"{ResourcesQuestFolder}/quest_priest_shrine_initiation.asset";
            var quest = LoadOrCreate<QuestDefinition>(path);
            quest.questId = HumanPriestShrineIds.InitiationQuestId;
            quest.displayTitle = "Shrine Initiation";
            quest.journalDescription =
                "Pay the Argent Vigil shrine steward 5 gold to swear a divine covenant.";
            quest.giverNpcId = HumanPriestShrineIds.ShrineStewardNpcId;
            quest.giverDisplayName = "Shrine Steward";
            quest.ownership = QuestOwnership.PerPartyMember;
            quest.requiredMinLevel = 0;
            quest.requiredRace = Race.Human;
            quest.requiresHumanClassNone = true;
            quest.requiresNoConsumedEssences = true;
            quest.turnInGoldCost = HumanPriestClassCommitService.InitiationGoldCost;
            quest.commitHumanClass = HumanClass.Priest;
            quest.learnDragonianSpellId = null;
            quest.acceptPrerequisites = System.Array.Empty<QuestPrerequisite>();
            quest.objectives = System.Array.Empty<QuestObjectiveDefinition>();
            quest.autoCompleteOnObjectives = false;
            quest.sortOrder = 7;
            EditorUtility.SetDirty(quest);
            return quest;
        }

        static PortraitDefinition CreatePortrait(string assetName, string texturePath)
        {
            string path = $"{ResourcesPortraitsFolder}/{assetName}.asset";
            var portrait = LoadOrCreate<PortraitDefinition>(path);
            portrait.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(portrait);
            return portrait;
        }

        static void CreateShrineStewardNpcPrefab(PortraitDefinition portrait, GameObject humanNpcBase)
        {
            string path = $"{ResourcesNpcFolder}/TownNpc_PriestShrineSteward.prefab";
            NpcDialogPackCreator.DeletePrefabIfPresent(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(humanNpcBase) as GameObject;
            instance.name = "TownNpc_PriestShrineSteward";

            Object.DestroyImmediate(instance.GetComponent<NpcController>(), true);
            HumanPriestShrineNpcController controller =
                instance.AddComponent<HumanPriestShrineNpcController>();

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("npcId").stringValue = HumanPriestShrineIds.ShrineStewardNpcId;
            controllerSo.FindProperty("portrait").objectReferenceValue = portrait;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(controller);
            actorSo.FindProperty("displayName").stringValue = "Shrine Steward";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            ApplySprite(instance, StewardSpritePath);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void UpdateTownStampMarkers()
        {
            var stamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(TownPlazaMarkerLayout.StampPath);
            if (stamp == null)
                return;

            TownPlazaMarkerLayout.ApplyAll(stamp);
            EditorUtility.SetDirty(stamp);
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

        static void CreatePlaceholderSprite(string path, Color color)
        {
            if (File.Exists(path))
                return;

            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void CreatePlaceholderPortrait(string path, Color color)
        {
            if (File.Exists(path))
                return;

            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void ConfigureTexture(string path, int pixelsPerUnit, FilterMode filterMode)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filterMode;
            importer.SaveAndReimport();
        }

        static void ApplySprite(GameObject instance, string spritePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
                return;

            var renderer = instance.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
                renderer.sprite = sprite;
        }
    }
}
#endif
