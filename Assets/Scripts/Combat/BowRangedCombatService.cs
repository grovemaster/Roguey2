using System.Collections.Generic;
using JRogue.Combat.FriendlyFire;
using System.Linq;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Core.Actor;
using JRogue.Core.Targeting;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Progression.Proficiency;
using JRogue.Stats;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Combat
{
    /// <summary>DCSS-style bow + arrow ranged shots (single tile, ammo consume).</summary>
    public static class BowRangedCombatService
    {
        public const int ShotNoiseVolume = 15;
        const string LogPrefix = "[Bow]";

        public static bool HasBowEquipped(BaseActor actor)
        {
            if (actor == null)
                return false;

            EquipmentManager equip = actor.GetComponent<EquipmentManager>();
            ItemData main = equip?.GetItemFromEquipmentSlot(EquipmentSlot.MainHand);
            return main != null && main.IsBowWeapon;
        }

        public static bool TryGetEquippedArrowStack(BaseActor actor, out ItemInstance stack)
        {
            stack = null;
            if (actor == null)
                return false;

            EquipmentManager equip = actor.GetComponent<EquipmentManager>();
            stack = equip?.GetEquippedInstance(EquipmentSlot.OffHand);
            if (stack?.Definition == null || !stack.Definition.IsBowAmmo || stack.Quantity < 1)
            {
                stack = null;
                return false;
            }

            return true;
        }

        public static bool HasAnyArrowAvailable(BaseActor actor)
        {
            if (TryGetEquippedArrowStack(actor, out _))
                return true;

            InventoryManager inv = actor?.GetComponent<InventoryManager>();
            if (inv == null)
                return false;

            foreach (ItemInstance inst in inv.CarriedItems)
            {
                if (inst?.Definition != null && inst.Definition.IsBowAmmo && inst.Quantity > 0)
                    return true;
            }

            return false;
        }

        public static int ComputeBowShotDamage(BaseActor actor, ItemData bow, ItemData arrow)
        {
            if (actor == null || bow == null || arrow == null)
                return 1;

            return ProficiencyCombatResolver.ComputeBowShotDamage(actor, bow, arrow);
        }

        static int SumDamage(ItemData item) =>
            item?.damageModules == null ? 0 : item.damageModules.Sum(m => m.value);

        /// <summary>Single-tile shot; damages all occupants (friendly fire). Consumes ammo on success.</summary>
        public static bool TryExecuteBowShot(BaseActor shooter, Vector3Int targetTile, int ammoCount = 1)
        {
            if (shooter == null || ammoCount < 1)
                return false;

            if (!SafeZonePolicyService.TryAllowHostileAction(out string denyReason))
            {
                Debug.Log($"{SafeZonePolicyService.LogPrefix} {denyReason}");
                return false;
            }

            EquipmentManager equip = shooter.GetComponent<EquipmentManager>();
            ItemData bow = equip?.GetItemFromEquipmentSlot(EquipmentSlot.MainHand);
            if (bow == null || !bow.IsBowWeapon)
                return false;

            if (!TryConsumeAmmo(shooter, ammoCount, out ItemData arrowUsed))
            {
                Debug.Log($"{LogPrefix} Cannot shoot: no arrows.");
                return false;
            }

            List<IBattleTarget> targets = TargetingResolver.GetTargetsOnTile(targetTile);
            if (targets.Count == 0)
                return false;

            int damage = ComputeBowShotDamage(shooter, bow, arrowUsed);
            DamageType damageType = arrowUsed.damageModules != null && arrowUsed.damageModules.Count > 0
                ? arrowUsed.damageModules[0].type
                : DamageType.Pierce;

            foreach (IBattleTarget target in targets)
            {
                if (target == null)
                    continue;

                if (target is BaseActor actor)
                    actor.TakeDamage(damage, damageType, shooter.gameObject);
                else
                    target.TakeDamage(damage, shooter.gameObject);
            }

            shooter.ProduceNoiseAt(ShotNoiseVolume, targetTile);

            Debug.Log(
                $"{LogPrefix} Shot at {targetTile} for {damage} with {arrowUsed.itemName} ({targets.Count} target(s)).");

            ProficiencyResolvedAction trainAction =
                ProficiencyStrikePayloadBuilder.FromBowShot(bow, arrowUsed, damage);
            ProficiencyXpDispatcher.Dispatch(shooter, trainAction);
            return true;
        }

        public static string GetBowShotActionLabel(BaseActor shooter)
        {
            EquipmentManager equip = shooter?.GetComponent<EquipmentManager>();
            ItemData bow = equip?.GetItemFromEquipmentSlot(EquipmentSlot.MainHand);
            if (bow != null && !string.IsNullOrWhiteSpace(bow.itemName))
                return $"{bow.itemName.Trim()} shot";

            return "Bow shot";
        }

        public static bool WouldHarmPartyAlly(
            BaseActor shooter,
            Vector3Int targetTile,
            out List<BaseActor> affectedAllies)
        {
            affectedAllies = new List<BaseActor>();
            if (shooter == null)
                return false;

            List<IBattleTarget> targets = TargetingResolver.GetTargetsOnTile(targetTile);
            PartyManager party = PartyManager.Instance;
            var harmed = new HashSet<BaseActor>();

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is not BaseActor actor)
                    continue;

                if (!FriendlyFirePreview.IsLivingPartyAlly(shooter, actor, party))
                    continue;

                harmed.Add(actor);
            }

            affectedAllies = FriendlyFirePreview.OrderByPartyRoster(harmed, party);
            return affectedAllies.Count > 0;
        }

        public static bool TryConsumeAmmo(BaseActor actor, int amount, out ItemData consumedDefinition)
        {
            consumedDefinition = null;
            if (actor == null || amount < 1)
                return false;

            EquipmentManager equip = actor.GetComponent<EquipmentManager>();
            if (equip == null)
                return false;

            return equip.TryConsumeEquippedAmmo(amount, out consumedDefinition);
        }

        public static void LogNoArrowsRemaining() =>
            Debug.Log($"{LogPrefix} No arrows remaining.");

        public static void LogArrowsRequireBow() =>
            Debug.Log($"{LogPrefix} Arrows require a bow.");

        public static void LogBumpUnarmed() =>
            Debug.Log($"{LogPrefix} No arrows; bump uses unarmed.");

        public static void LogDefaultAmmo(ItemData arrow, int quantity) =>
            Debug.Log($"{LogPrefix} Default ammo: {arrow.itemName} ×{quantity}.");

        public static void LogPromotedAmmo(ItemData arrow, int quantity) =>
            Debug.Log($"{LogPrefix} Promoted ammo: {arrow.itemName} ×{quantity}.");
    }
}
