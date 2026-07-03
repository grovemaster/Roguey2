using JRogue.Ability.Essence;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Status;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.Essence
{
    public static class EssenceWeaponProcService
    {
        public const string DefaultPoisonDefinitionPath = "Assets/Data/Status/Status_Poisoned_Default.asset";

        public static void TryApplyPoisonWeaponOnHit(GameObject attacker, GameObject target)
        {
            if (attacker == null || target == null)
                return;

            PoisonWeaponEssenceRuntime poison = attacker.GetComponent<PoisonWeaponEssenceRuntime>();
            if (poison == null || !poison.IsActive || !poison.RollProc())
                return;

            if (!QualifiesForPoisonWeaponProc(attacker))
                return;

            PoisonStatusEffectDefinition definition = LoadPoisonDefinition();
            if (definition == null)
                return;

            if (target.TryGetComponent(out BaseActor actor))
                StatusEffectService.TryApply(actor, definition, attacker);
        }

        static bool QualifiesForPoisonWeaponProc(GameObject attacker)
        {
            if (!attacker.TryGetComponent(out EquipmentManager equipment))
                return true;

            ItemData weapon = equipment.GetItemFromEquipmentSlot(EquipmentSlot.MainHand);
            if (weapon == null)
                return true;

            return weapon.weaponType != WeaponType.Staff;
        }

        static PoisonStatusEffectDefinition LoadPoisonDefinition()
        {
#if UNITY_EDITOR
            PoisonStatusEffectDefinition editorAsset =
                UnityEditor.AssetDatabase.LoadAssetAtPath<PoisonStatusEffectDefinition>(DefaultPoisonDefinitionPath);
            if (editorAsset != null)
                return editorAsset;
#endif
            return Resources.Load<PoisonStatusEffectDefinition>("Status/Status_Poisoned_Default");
        }
    }
}
