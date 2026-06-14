using System.Collections.Generic;
using JRogue.Core.Actor;
using JRogue.Core.Targeting;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability
{
    public abstract class AbilityAction : ScriptableObject
    {
        public string abilityName;
        [TextArea] public string description;

        [Header("Hotbar")]
        [Tooltip("Icon shown on the ability hotbar; falls back to item/essence icon when unset.")]
        public Sprite hotbarIcon;
        public int soulPowerCost;
        public int magicPowerCost;
        public int divinePowerCost;
        public int cooldownTurns;

        [Header("Proficiency")]
        public List<ProficiencyKind> proficiencyTags = new();
        [Tooltip("When > 0, replaces default pxp tier for awards from this ability.")]
        public int proficiencyXpOverride;

        [Header("Knight")]
        [Tooltip("Links resolve to Knight skill tree node id for mastery / rank pxp awards.")]
        public string knightSkillId;

        [Header("Targeting Settings")]
        public bool requiresTarget; // Fixes 'does not have requiresTarget'
        public int range;

        [Tooltip("Splash shape for preview + AoE resolution. Preferred over splashRadius.")]
        public SplashZoneDefinition splashZone;

        [Tooltip("Legacy AoE radius when splashZone is unset.")]
        public int splashRadius;

        /// <summary>Effective splash zone for targeting preview and AoE abilities.</summary>
        public SplashZoneDefinition ResolveSplashZone()
        {
            if (splashZone != null)
                return splashZone;

            if (splashRadius > 0)
                return SplashZoneResolver.CreateLegacyDisk(splashRadius);

            return null;
        }

        [Header("Movement Settings")]
        public bool isMovementAbility; // Indicates if this ability moves the user

        [Header("Acoustics")]
        [Tooltip("Volume of noise produced when this ability successfully executes. 0 = silent.")]
        public int noiseVolume = 0;

        [Tooltip("When Execute uses a target tile (targeted abilities), originate sound at targetTile instead of the caster.")]
        public bool noiseOriginAtTargetTile = false;

        [Header("Friendly Fire")]
        [Tooltip("When true, never prompt before damaging party allies.")]
        public bool skipFriendlyFireConfirmation;

        /// <summary>Whether this ability would deal damage to <paramref name="target"/> at execute time.</summary>
        public virtual bool WouldHarm(IBattleTarget target, GameObject caster) => false;

        // New Method: Can we actually use this right now?
        public abstract bool CanExecute(GameObject user);

        /// <summary>Side-effect-free readiness check for UI polling. Must not log.</summary>
        public virtual bool IsReadyForUse(GameObject user) => CanExecute(user);

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
            if (success) EmitNoise(user, noiseOriginAtTargetTile ? targetTile : (Vector3Int?)null);
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
            EmitNoise(user, null);
        }

        private void EmitNoise(GameObject user, Vector3Int? soundOriginTile)
        {
            if (noiseVolume <= 0 || user == null) return;
            if (!user.TryGetComponent<INoiseProducer>(out INoiseProducer producer))
                return;

            if (soundOriginTile.HasValue)
                producer.ProduceNoiseAt(noiseVolume, soundOriginTile.Value);
            else
                producer.ProduceNoise(noiseVolume);
        }
    }
}
