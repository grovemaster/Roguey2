using JRogue.Core.Actor;
using UnityEngine;

namespace JRogue.Ability
{
    public abstract class AbilityAction : ScriptableObject
    {
        public string abilityName;
        [TextArea] public string description;
        public int soulPowerCost;
        public int cooldownTurns;

        [Header("Targeting Settings")]
        public bool requiresTarget; // Fixes 'does not have requiresTarget'
        public int range;
        public int splashRadius;

        [Header("Movement Settings")]
        public bool isMovementAbility; // Indicates if this ability moves the user

        [Header("Acoustics")]
        [Tooltip("Volume of noise produced when this ability successfully executes. 0 = silent.")]
        public int noiseVolume = 0;

        // New Method: Can we actually use this right now?
        public abstract bool CanExecute(GameObject user);

        // Public entry points are non-virtual: they handle cross-cutting
        // concerns (noise, future logging/cooldowns/etc.) and delegate the
        // ability-specific work to ExecuteCore.
        public bool Execute(GameObject user)
        {
            bool success = ExecuteCore(user);
            if (success) EmitNoise(user);
            return success;
        }

        public bool Execute(GameObject user, Vector3Int targetTile)
        {
            bool success = ExecuteCore(user, targetTile);
            if (success) EmitNoise(user);
            return success;
        }

        // Subclass hooks. Return true if the action was successful (consumes a turn).
        protected abstract bool ExecuteCore(GameObject user);

        // Default targeted hook falls back to the untargeted variant.
        protected virtual bool ExecuteCore(GameObject user, Vector3Int targetTile)
        {
            return ExecuteCore(user);
        }

        private void EmitNoise(GameObject user)
        {
            if (noiseVolume <= 0 || user == null) return;
            if (user.TryGetComponent<INoiseProducer>(out var producer))
            {
                producer.ProduceNoise(noiseVolume);
            }
        }
    }
}
