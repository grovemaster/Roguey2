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

        // New Method: Can we actually use this right now?
        public abstract bool CanExecute(GameObject user);

        // The logic for what the ability actually DOES.
        // Returns true if the action was successful (consumes a turn).
        public abstract bool Execute(GameObject user);

        // Milestone 16: Overload for targeted execution
        public virtual bool Execute(GameObject user, Vector3Int targetTile)
        {
            return Execute(user); // Default fallback
        }
    }
}