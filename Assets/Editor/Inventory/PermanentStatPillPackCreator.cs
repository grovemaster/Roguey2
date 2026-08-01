#if UNITY_EDITOR
using JRogue.Ability.Progression;
using JRogue.Item;
using JRogue.Progression;
using JRogue.Stats;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Inventory
{
    public static class PermanentStatPillPackCreator
    {
        const string AbilityRoot = "Assets/Resources/Item/Ability";
        const string PotionRoot = "Assets/Resources/Item/Potion";
        const string StrengthAbilityPath = AbilityRoot + "/PermanentStatBoost_Strength.asset";
        const string PoisonAbilityPath = AbilityRoot + "/PermanentStatBoost_PoisonResistance.asset";
        const string StrengthPillPath = PotionRoot + "/Pill_Strength.asset";
        const string PoisonPillPath = PotionRoot + "/Pill_PoisonResistance.asset";

        [MenuItem("JRogue/Inventory/Create Permanent Stat Pill Pack", false, 55)]
        public static void CreatePack()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Item");
            EnsureFolder(AbilityRoot);
            EnsureFolder(PotionRoot);

            PermanentStatBoostAbility strengthAbility =
                LoadOrCreateAbility(StrengthAbilityPath, PermanentStatBoostKind.Attribute);
            strengthAbility.abilityName = "Pill of Strength";
            strengthAbility.description = "Permanently increases Strength by 1.";
            strengthAbility.boostKind = PermanentStatBoostKind.Attribute;
            strengthAbility.attribute = StatType.Strength;
            strengthAbility.amount = 1;
            strengthAbility.requiresTarget = false;
            EditorUtility.SetDirty(strengthAbility);

            PermanentStatBoostAbility poisonAbility =
                LoadOrCreateAbility(PoisonAbilityPath, PermanentStatBoostKind.Resistance);
            poisonAbility.abilityName = "Pill of Poison Resistance";
            poisonAbility.description = "Permanently increases Poison resistance by 1.";
            poisonAbility.boostKind = PermanentStatBoostKind.Resistance;
            poisonAbility.resistance = DamageType.Poison;
            poisonAbility.amount = 1;
            poisonAbility.requiresTarget = false;
            EditorUtility.SetDirty(poisonAbility);

            ItemData strengthPill = LoadOrCreateItem(StrengthPillPath);
            strengthPill.itemName = "Pill of Strength";
            strengthPill.category = ItemCategory.Potion;
            strengthPill.weight = 0.2f;
            strengthPill.goldValue = 40;
            strengthPill.buyValue = 5;
            strengthPill.sellValue = 2;
            strengthPill.allowUseInSafeZone = true;
            strengthPill.activeAbilities = new System.Collections.Generic.List<JRogue.Ability.AbilityAction>
            {
                strengthAbility,
            };
            EditorUtility.SetDirty(strengthPill);

            ItemData poisonPill = LoadOrCreateItem(PoisonPillPath);
            poisonPill.itemName = "Pill of Poison Resistance";
            poisonPill.category = ItemCategory.Potion;
            poisonPill.weight = 0.2f;
            poisonPill.goldValue = 40;
            poisonPill.buyValue = 5;
            poisonPill.sellValue = 2;
            poisonPill.allowUseInSafeZone = true;
            poisonPill.activeAbilities = new System.Collections.Generic.List<JRogue.Ability.AbilityAction>
            {
                poisonAbility,
            };
            EditorUtility.SetDirty(poisonPill);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[PermanentStat] Created/updated Pill of Strength and Pill of Poison Resistance under Resources/Item/Potion.");
            Selection.activeObject = strengthPill;
        }

        [MenuItem("JRogue/Inventory/Seed Permanent Stat Pills on Party", false, 56)]
        public static void SeedPillsOnParty()
        {
            CreatePack();

            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[PermanentStat] Enter Play Mode with a party, then run Seed Permanent Stat Pills on Party again. " +
                    "(Assets were ensured.)");
                return;
            }

            PermanentStatPillTestGrants.GrantOneOfEachToParty();
        }

        static PermanentStatBoostAbility LoadOrCreateAbility(string path, PermanentStatBoostKind kind)
        {
            PermanentStatBoostAbility existing = AssetDatabase.LoadAssetAtPath<PermanentStatBoostAbility>(path);
            if (existing != null)
                return existing;

            PermanentStatBoostAbility created = ScriptableObject.CreateInstance<PermanentStatBoostAbility>();
            created.boostKind = kind;
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static ItemData LoadOrCreateItem(string path)
        {
            ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (existing != null)
                return existing;

            ItemData created = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
