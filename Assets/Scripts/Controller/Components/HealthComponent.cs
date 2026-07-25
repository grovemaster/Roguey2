using System;
using JRogue.Ability.Essence;
using JRogue.Manager.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Actors.Components
{
    /// <summary>
    /// Owns the damage-reduction calculation and death notification for an actor.
    /// Reads/writes HP via <see cref="CharacterStats"/>; raises events instead of
    /// directly invoking actor logic so listeners (BaseActor, UI, sounds) can hook
    /// in without coupling to one another.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats))]
    public class HealthComponent : MonoBehaviour
    {
        private CharacterStats stats;
        private EssenceSlotManager essenceManager;

        public event Action<int, DamageType> Damaged;
        public event Action Died;

        /// <summary>Last GameObject that dealt damage (for kill credit).</summary>
        public GameObject LastDamageSource { get; private set; }

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
            essenceManager = GetComponent<EssenceSlotManager>();
        }

        /// <summary>
        /// Legacy overload: defaults to <see cref="ArmorInteraction.Full"/> for Blunt/Slash/Pierce
        /// callers that have not yet authored an interaction. Prefer the overload with
        /// <see cref="ArmorInteraction"/>.
        /// </summary>
        public void TakeDamage(int rawDamage, DamageType type, GameObject damageSource = null)
        {
            ArmorInteraction interaction = IsLegacyPhysicalType(type)
                ? ArmorInteraction.Full
                : ArmorInteraction.None;
            TakeDamage(rawDamage, type, interaction, damageSource);
        }

        public void TakeDamage(
            int rawDamage,
            DamageType type,
            ArmorInteraction armorInteraction,
            GameObject damageSource = null)
        {
            if (SafeZonePolicyService.ShouldSuppressPlayerDamage(gameObject, damageSource))
                return;

            if (damageSource != null)
                LastDamageSource = damageSource;

            int resistanceValue = stats.GetResistance(type);
            int armorClass = stats.ArmorClass + ResolveFlatArmorClassBonus();
            int damage = DamageApplicationLogic.ComputeFinalDamage(
                rawDamage,
                resistanceValue,
                armorClass,
                armorInteraction);

            stats.currentHP = Mathf.Max(0, stats.currentHP - damage);

            // Update conditional passives whose threshold may now have flipped
            // (e.g., Heroic Spirit at half HP).
            essenceManager?.RefreshConditionalPassives();
            RacialPassiveHooks.RefreshPassives(gameObject);

            Debug.Log($"{gameObject.name} took {damage} {type} damage " +
                      $"(armor={armorInteraction}). HP: {stats.currentHP}/{stats.MaxHP}");

            Damaged?.Invoke(damage, type);

            if (stats.currentHP <= 0)
            {
                Died?.Invoke();
            }
        }

        static bool IsLegacyPhysicalType(DamageType type) =>
            type == DamageType.Blunt || type == DamageType.Slash || type == DamageType.Pierce;

        int ResolveFlatArmorClassBonus()
        {
            AdrenalineRushEssenceRuntime adrenaline = GetComponent<AdrenalineRushEssenceRuntime>();
            return adrenaline != null ? adrenaline.ArmorClassBonus : 0;
        }
    }
}
