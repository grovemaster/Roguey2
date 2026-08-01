using JRogue.Actors;
using JRogue.Stats;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Ability.Progression
{
    public enum PermanentStatBoostKind
    {
        Attribute = 0,
        Resistance = 1,
    }

    /// <summary>Consumable pill effect: permanent +N to one attribute or one resistance on the consumer only.</summary>
    [CreateAssetMenu(
        fileName = "PermanentStatBoost",
        menuName = "JRogue/Abilities/Permanent Stat Boost")]
    public sealed class PermanentStatBoostAbility : AbilityAction
    {
        public PermanentStatBoostKind boostKind = PermanentStatBoostKind.Attribute;
        public StatType attribute = StatType.Strength;
        public DamageType resistance = DamageType.Poison;
        [Min(1)]
        public int amount = 1;

        public override bool CanExecute(GameObject user)
        {
            if (user == null || amount == 0)
                return false;
            if (!user.TryGetComponent(out CharacterStats stats))
                return false;

            if (boostKind == PermanentStatBoostKind.Attribute)
                return stats.GetStatByType(attribute) != null;

            return true;
        }

        protected override bool ExecuteCore(GameObject user)
        {
            if (!CanExecute(user))
                return false;

            PermanentStatBoostRuntime runtime = PermanentStatBoostRuntime.Ensure(user);
            if (runtime == null)
                return false;

            bool ok;
            string targetLabel;
            if (boostKind == PermanentStatBoostKind.Attribute)
                ok = runtime.TryApplyAttribute(attribute, amount, out targetLabel);
            else
                ok = runtime.TryApplyResistance(resistance, amount, out targetLabel);

            if (!ok)
                return false;

            string displayName = ResolveDisplayName(user);
            string signed = amount > 0 ? $"+{amount}" : amount.ToString();
            Debug.Log(
                $"{PermanentStatBoostRuntime.LogPrefix} {displayName} permanently gained {signed} {targetLabel}.");

            string playerLine = boostKind == PermanentStatBoostKind.Attribute
                ? $"{displayName}'s {attribute} permanently increased by {amount}."
                : $"{displayName}'s {resistance} resistance permanently increased by {amount}.";
            GameLogService.ActiveSession.Append(playerLine);
            return true;
        }

        public string FormatInspectLine()
        {
            string signed = amount > 0 ? $"+{amount}" : amount.ToString();
            if (boostKind == PermanentStatBoostKind.Attribute)
                return $"Permanently increases {attribute} by {amount} ({signed}).";
            return $"Permanently increases {resistance} resistance by {amount} ({signed}).";
        }

        static string ResolveDisplayName(GameObject user)
        {
            if (user != null && user.TryGetComponent(out BaseActor actor) && !string.IsNullOrEmpty(actor.DisplayName))
                return actor.DisplayName;
            return user != null ? user.name : "Someone";
        }
    }
}
