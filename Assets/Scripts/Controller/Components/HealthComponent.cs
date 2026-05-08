using System;
using JRogue.Manager.Essence;
using JRogue.Stats;
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

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
            essenceManager = GetComponent<EssenceSlotManager>();
        }

        public void TakeDamage(int rawDamage, DamageType type)
        {
            int resistanceValue = stats.GetResistance(type);
            int damage = Mathf.Max(1, rawDamage - resistanceValue);

            // Factor in AC for physical types
            if (type == DamageType.Blunt || type == DamageType.Slash || type == DamageType.Pierce)
            {
                damage = Mathf.Max(1, damage - (stats.ArmorClass / 5));
            }

            stats.currentHP -= damage;

            // Update conditional passives whose threshold may now have flipped
            // (e.g., Heroic Spirit at half HP).
            essenceManager?.RefreshConditionalPassives();

            Debug.Log($"{gameObject.name} took {damage} {type} damage. " +
                      $"HP: {stats.currentHP}/{stats.MaxHP}");

            Damaged?.Invoke(damage, type);

            if (stats.currentHP <= 0)
            {
                Died?.Invoke();
            }
        }
    }
}
