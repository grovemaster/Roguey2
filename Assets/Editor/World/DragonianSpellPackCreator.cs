#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Ability;
using JRogue.Ability.Fireball;
using JRogue.Ability.SuddenStrength;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.Quest;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class DragonianSpellPackCreator
    {
        const string DragonianPlayerPath = "Assets/Prefabs/Actor/Race/DragonianPlayer.prefab";
        const string DataDragonianFolder = "Assets/Data/Racial/Dragonian";
        const string ResourcesCatalogFolder = "Assets/Resources/Racial/Dragonian";
        const string SuddenStrengthAbilityPath = "Assets/Resources/Item/Ability/SuddenStrength_Standard.asset";
        const string FireballAbilityPath = "Assets/Resources/Item/Ability/Fireball_Standard.asset";

        [MenuItem("JRogue/Racial/Create Dragonian Spell Pack")]
        public static void CreateDragonianSpellPack()
        {
            Directory.CreateDirectory(DataDragonianFolder);

            SuddenStrengthAbility suddenStrength =
                AssetDatabase.LoadAssetAtPath<SuddenStrengthAbility>(SuddenStrengthAbilityPath);
            FireballAbility fireball = AssetDatabase.LoadAssetAtPath<FireballAbility>(FireballAbilityPath);

            DragonianSpellDefinition draconicSurge = CreateSpell(
                $"{DataDragonianFolder}/Spell_DraconicSurge.asset",
                "dragonian_spell_sudden_strength",
                "Draconic Surge",
                "Internalize draconic might — a sudden burst of strength.",
                memorizeCost: 3,
                soulPowerCastCost: 1,
                suddenStrength);

            DragonianSpellDefinition dragonFlame = CreateSpell(
                $"{DataDragonianFolder}/Spell_DragonFlame.asset",
                "dragonian_spell_fireball",
                "Dragon Flame",
                "Breathe condensed flame at a distant tile.",
                memorizeCost: 7,
                soulPowerCastCost: 5,
                fireball);

            WireDragonianPlayerRuntime();
            CreateSpellCatalog(draconicSurge, dragonFlame);
            WireDragonianPlayerPartyMemberId();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dragonian] Created Dragonian spell pack (Draconic Surge + Dragon Flame).");
        }

        static DragonianSpellDefinition CreateSpell(
            string assetPath,
            string spellId,
            string displayName,
            string description,
            int memorizeCost,
            int soulPowerCastCost,
            AbilityAction ability)
        {
            var spell = LoadOrCreate<DragonianSpellDefinition>(assetPath);
            spell.spellId = spellId;
            spell.displayName = displayName;
            spell.description = description;
            spell.memorizeCost = memorizeCost;
            spell.soulPowerCastCost = soulPowerCastCost;
            spell.ability = ability;
            EditorUtility.SetDirty(spell);
            return spell;
        }

        static void WireDragonianPlayerRuntime()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DragonianPlayerPath);
            if (prefab == null)
            {
                Debug.LogWarning("[Dragonian] Missing DragonianPlayer prefab.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            CharacterStats stats = instance.GetComponent<CharacterStats>();
            if (stats != null)
                stats.racialSubsystem = RacialSubsystemKind.DragonianSpells;

            RacialLoadoutApplier loadoutApplier = instance.GetComponent<RacialLoadoutApplier>();
            if (loadoutApplier != null)
                loadoutApplier.SetLoadout(null);

            DragonianSpellsRuntime runtime = instance.GetComponent<DragonianSpellsRuntime>();
            if (runtime == null)
                runtime = instance.AddComponent<DragonianSpellsRuntime>();

            runtime.SetKnownAndMemorized(
                new List<DragonianSpellDefinition>(),
                new List<string>());

            PrefabUtility.SaveAsPrefabAsset(instance, DragonianPlayerPath);
            Object.DestroyImmediate(instance);
        }

        static void CreateSpellCatalog(
            DragonianSpellDefinition draconicSurge,
            DragonianSpellDefinition dragonFlame)
        {
            Directory.CreateDirectory(ResourcesCatalogFolder);
            DragonianSpellDefinition resourcesSurge =
                SyncResourcesSpellCopy(draconicSurge, "Spell_DraconicSurge.asset");
            DragonianSpellDefinition resourcesFlame =
                SyncResourcesSpellCopy(dragonFlame, "Spell_DragonFlame.asset");

            string path = $"{ResourcesCatalogFolder}/DragonianSpellCatalog.asset";
            var catalog = LoadOrCreate<DragonianSpellCatalog>(path);
            catalog.spells.Clear();
            if (resourcesSurge != null)
                catalog.spells.Add(resourcesSurge);
            if (resourcesFlame != null)
                catalog.spells.Add(resourcesFlame);
            EditorUtility.SetDirty(catalog);
        }

        static DragonianSpellDefinition SyncResourcesSpellCopy(
            DragonianSpellDefinition source,
            string fileName)
        {
            if (source == null)
                return null;

            string path = $"{ResourcesCatalogFolder}/{fileName}";
            var copy = LoadOrCreate<DragonianSpellDefinition>(path);
            copy.spellId = source.spellId;
            copy.displayName = source.displayName;
            copy.description = source.description;
            copy.memorizeCost = source.memorizeCost;
            copy.soulPowerCastCost = source.soulPowerCastCost;
            copy.ability = source.ability;
            EditorUtility.SetDirty(copy);
            return copy;
        }

        static void WireDragonianPlayerPartyMemberId()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DragonianPlayerPath);
            if (prefab == null)
                return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            PartyMemberId marker = instance.GetComponent<PartyMemberId>();
            if (marker == null)
                marker = instance.AddComponent<PartyMemberId>();

            marker.ConfigureMemberId("DragonianPlayer");
            EditorUtility.SetDirty(marker);
            PrefabUtility.SaveAsPrefabAsset(instance, DragonianPlayerPath);
            Object.DestroyImmediate(instance);
        }

        static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                return asset;

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }
    }
}
#endif
