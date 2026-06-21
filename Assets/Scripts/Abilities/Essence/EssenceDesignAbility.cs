using UnityEngine;

namespace JRogue.Ability.Essence
{
    /// <summary>
    /// Authoring stub for essence actives whose gameplay is not implemented yet.
    /// All tuning lives on this asset so designers can edit without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "EssenceDesignAbility", menuName = "JRogue/Abilities/Essence Design Stub")]
    public sealed class EssenceDesignAbility : AbilityAction
    {
        [Header("Design (implementation pending)")]
        [Tooltip("When false, hotbar use should not consume a player turn (wired in Phase 5).")]
        public bool consumesPlayerTurn;

        [Min(0)] public int effectDurationTurns;
        [Range(0f, 1f)] public float procChance;
        public int strengthDelta;
        public int defenseDelta;
        [Min(1)] public int movementTilesPerTurn = 1;

        public override bool CanExecute(GameObject user) => user != null;

        protected override bool ExecuteCore(GameObject user)
        {
            Debug.LogWarning(
                $"[EssenceDesignAbility] '{abilityName}' is a design stub — gameplay not implemented yet.");
            return false;
        }
    }
}
