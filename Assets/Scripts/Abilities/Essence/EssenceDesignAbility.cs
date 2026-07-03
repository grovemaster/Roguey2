using UnityEngine;

namespace JRogue.Ability.Essence
{
    /// <summary>
    /// Data-driven essence active whose gameplay is implemented by companion runtime components.
    /// </summary>
    [CreateAssetMenu(fileName = "EssenceDesignAbility", menuName = "JRogue/Abilities/Essence Design Stub")]
    public sealed class EssenceDesignAbility : AbilityAction
    {
        [Header("Design")]
        [Tooltip("When false, hotbar use does not consume the player's turn.")]
        public bool consumesPlayerTurn;

        [Min(0)] public int effectDurationTurns;
        [Range(0f, 1f)] public float procChance;
        public int strengthDelta;
        public int defenseDelta;
        [Min(1)] public int movementTilesPerTurn = 1;

        public override bool CanExecute(GameObject user)
        {
            if (user == null)
                return false;

            return ResolveKind() switch
            {
                EssenceAbilityKind.PoisonWeapon =>
                    user.GetComponent<PoisonWeaponEssenceRuntime>() == null
                    || !user.GetComponent<PoisonWeaponEssenceRuntime>().IsActive,
                EssenceAbilityKind.Dash =>
                    user.GetComponent<DashEssenceRuntime>() == null
                    || !user.GetComponent<DashEssenceRuntime>().IsActive,
                EssenceAbilityKind.AdrenalineRush =>
                    user.GetComponent<AdrenalineRushEssenceRuntime>() == null
                    || !user.GetComponent<AdrenalineRushEssenceRuntime>().IsActive,
                _ => false,
            };
        }

        protected override bool ExecuteCore(GameObject user)
        {
            switch (ResolveKind())
            {
                case EssenceAbilityKind.PoisonWeapon:
                {
                    var runtime = user.AddComponent<PoisonWeaponEssenceRuntime>();
                    runtime.Apply(this);
                    return true;
                }
                case EssenceAbilityKind.Dash:
                {
                    var runtime = user.AddComponent<DashEssenceRuntime>();
                    runtime.Apply(this);
                    return true;
                }
                case EssenceAbilityKind.AdrenalineRush:
                {
                    var runtime = user.AddComponent<AdrenalineRushEssenceRuntime>();
                    runtime.Apply(this);
                    return true;
                }
                default:
                    Debug.LogWarning($"[EssenceDesignAbility] Unrecognized tuning on '{abilityName}'.");
                    return false;
            }
        }

        EssenceAbilityKind ResolveKind()
        {
            if (movementTilesPerTurn > 1)
                return EssenceAbilityKind.Dash;

            if (procChance > 0f)
                return EssenceAbilityKind.PoisonWeapon;

            if (strengthDelta != 0 || defenseDelta != 0)
                return EssenceAbilityKind.AdrenalineRush;

            return EssenceAbilityKind.Unknown;
        }

        enum EssenceAbilityKind
        {
            Unknown = 0,
            PoisonWeapon = 1,
            Dash = 2,
            AdrenalineRush = 3,
        }
    }
}
