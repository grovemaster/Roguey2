using JRogue.Manager.Inventory;
using JRogue.Status;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.Heal
{
    [CreateAssetMenu(fileName = "HealingPotionAbility", menuName = "JRogue/Abilities/Healing Potion")]
    public class HealingPotionAbility : AbilityAction
    {
        [SerializeField] StatusEffectDefinition stunnedDefinition;

        public override bool CanExecute(GameObject user)
        {
            if (user == null)
                return false;

            if (!HealingPotionRules.CanUseWithoutCombatBan(user))
            {
                Debug.Log($"[HealingPotion] {CombatBanMessageFor(user)}");
                return false;
            }

            if (!user.TryGetComponent(out CharacterStats stats))
                return false;

            return stats.currentHP < stats.MaxHP;
        }

        protected override bool ExecuteCore(GameObject user)
        {
            if (user == null || !user.TryGetComponent(out CharacterStats stats))
                return false;

            if (!HealingPotionRules.CanUseWithoutCombatBan(user))
            {
                Debug.Log($"[HealingPotion] {CombatBanMessageFor(user)}");
                return false;
            }

            if (stats.currentHP >= stats.MaxHP)
            {
                Debug.Log("[HealingPotion] Health already full.");
                return false;
            }

            int before = stats.currentHP;
            stats.currentHP = Mathf.Min(stats.currentHP + HealingPotionRules.HealAmount, stats.MaxHP);
            int healed = stats.currentHP - before;

            if (HealingPotionRules.IsExemptFromPainStun(user))
            {
                Debug.Log($"[HealingPotion] {user.name} healed for {healed} HP (Warrior Willpower — no stun).");
                return true;
            }

            int stunTurns = HealingPotionRules.ComputeStunTurns(stats);
            StatusEffectController statuses = user.GetComponent<StatusEffectController>();
            if (statuses != null && stunnedDefinition != null)
                StatusEffectService.TryApplyWithDuration(statuses, stunnedDefinition, stunTurns, user);

            Debug.Log(
                $"[HealingPotion] {user.name} healed for {healed} HP and is Stunned for {stunTurns} turns (Pain Tolerance {stats.painTolerance.GetValue()}).");
            return true;
        }

        static string CombatBanMessageFor(GameObject user) =>
            $"{HealingPotionRules.CombatBanMessage} ({user.name})";
    }
}
