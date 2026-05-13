using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Applies a <see cref="RacialLoadoutDefinition"/> once at runtime (after <see cref="CharacterStats"/> Awake).
    /// Optional: leave loadout null for actors with no racial modifiers.
    /// </summary>
    public class RacialLoadoutApplier : MonoBehaviour
    {
        [SerializeField] RacialLoadoutDefinition loadout;

        bool _applied;

        public RacialLoadoutDefinition Loadout => loadout;

        void Start()
        {
            TryApply();
        }

        /// <summary>Runtime swap of loadout (removes previous if applied). Phase 1+ hook for respec systems.</summary>
        public void SetLoadout(RacialLoadoutDefinition newLoadout)
        {
            if (_applied && loadout != null)
                loadout.Remove(gameObject);

            _applied = false;
            loadout = newLoadout;
            TryApply();
        }

        void TryApply()
        {
            if (_applied || loadout == null) return;

            var stats = GetComponent<CharacterStats>();
            if (stats == null)
            {
                Debug.LogWarning($"[Racial] {name} has no CharacterStats; skipping racial loadout.");
                return;
            }

            if (!loadout.CanApplyTo(stats))
            {
                Debug.LogWarning(
                    $"[Racial] Loadout '{loadout.name}' requires race {loadout.requiredRace} but actor is {stats.race}.");
                return;
            }

            loadout.Apply(gameObject);
            _applied = true;
        }

        void OnDestroy()
        {
            if (_applied && loadout != null)
                loadout.Remove(gameObject);
        }

        public void RefreshPassives()
        {
            if (!_applied || loadout == null) return;
            loadout.RefreshPassives(gameObject);
        }

        public void NotifyPassivesTurnStart()
        {
            if (!_applied || loadout == null) return;
            loadout.NotifyPassivesTurnStart(gameObject);
        }
    }
}
