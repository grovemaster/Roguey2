using JRogue.Stats;
using UnityEngine;

namespace JRogue.Effects.Timed
{
    /// <summary>
    /// Reusable timed effect lifecycle: initialize, phase-tick, expire, and cleanup.
    /// </summary>
    public abstract class ActorTimedEffectRuntime : MonoBehaviour
    {
        [SerializeField] int durationTurns;
        [SerializeField] int turnsRemaining;

        protected CharacterStats Stats { get; private set; }
        protected bool IsApplied { get; private set; }

        public int DurationTurns => durationTurns;
        public int TurnsRemaining => turnsRemaining;

        public void Initialize(int duration)
        {
            durationTurns = Mathf.Max(1, duration);
            turnsRemaining = durationTurns;
            Stats = GetComponent<CharacterStats>();

            if (Stats == null)
                return;

            ApplyEffect();
            IsApplied = true;
        }

        public void OnPlayerPhaseStart()
        {
            if (!IsApplied || turnsRemaining <= 0)
                return;

            turnsRemaining--;
            if (turnsRemaining <= 0)
                Expire();
        }

        protected virtual void OnEffectExpired()
        {
        }

        protected abstract void ApplyEffect();
        protected abstract void RemoveEffect();

        void Expire()
        {
            CleanupEffect();
            OnEffectExpired();

            if (Application.isPlaying)
                Destroy(this);
            else
                DestroyImmediate(this);
        }

        void OnDestroy()
        {
            CleanupEffect();
        }

        void CleanupEffect()
        {
            if (!IsApplied)
                return;

            RemoveEffect();
            IsApplied = false;
        }
    }
}
