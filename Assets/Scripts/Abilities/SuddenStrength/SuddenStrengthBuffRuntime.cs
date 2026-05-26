using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.SuddenStrength
{
    /// <summary>
    /// Tracks Sudden Strength duration in player phases. Stat modifier source is this component instance.
    /// </summary>
    public sealed class SuddenStrengthBuffRuntime : MonoBehaviour
    {
        [SerializeField] int strengthBonus;
        [SerializeField] int durationTurns;
        [SerializeField] int turnsRemaining;

        CharacterStats _stats;

        public int StrengthBonus => strengthBonus;
        public int DurationTurns => durationTurns;
        public int TurnsRemaining => turnsRemaining;

        public void Apply(int bonus, int duration)
        {
            strengthBonus = bonus;
            durationTurns = duration;
            turnsRemaining = duration;
            _stats = GetComponent<CharacterStats>();
            _stats?.Strength.AddModifier(strengthBonus, this);
        }

        public void OnPlayerPhaseStart()
        {
            if (turnsRemaining <= 0)
                return;

            turnsRemaining--;
            if (turnsRemaining <= 0)
                Expire();
        }

        void Expire()
        {
            RemoveModifier();
            Debug.Log($"[Sudden Strength] Expired on {gameObject.name}.");
            Destroy(this);
        }

        void OnDestroy() => RemoveModifier();

        void RemoveModifier()
        {
            if (_stats == null)
                _stats = GetComponent<CharacterStats>();
            _stats?.Strength.RemoveModifiersFromSource(this);
        }
    }
}
