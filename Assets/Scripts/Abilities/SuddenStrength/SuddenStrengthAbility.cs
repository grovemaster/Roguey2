using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.SuddenStrength
{
    [CreateAssetMenu(fileName = "SuddenStrength_Standard", menuName = "JRogue/Abilities/Sudden Strength")]
    public class SuddenStrengthAbility : AbilityAction
    {
        public int strengthBonus = 100;
        public int durationTurns = 10;

        public override bool IsReadyForUse(GameObject user) => MeetsRequirements(user);

        public override bool CanExecute(GameObject user)
        {
            if (!MeetsRequirements(user))
            {
                if (HasActiveBuff(user))
                    Debug.Log($"[Sudden Strength] Already active on {user.name}.");
                return false;
            }

            return true;
        }

        static bool MeetsRequirements(GameObject user)
        {
            if (user == null)
                return false;

            CharacterStats stats = user.GetComponent<CharacterStats>();
            if (stats == null)
                return false;

            SuddenStrengthBuffRuntime existing = user.GetComponent<SuddenStrengthBuffRuntime>();
            if (existing == null)
                return true;

            if (!stats.Strength.HasModifierFromSource(existing))
            {
                if (Application.isPlaying)
                    Object.Destroy(existing);
                else
                    Object.DestroyImmediate(existing);
                return true;
            }

            return false;
        }

        static bool HasActiveBuff(GameObject user)
        {
            if (user == null)
                return false;

            CharacterStats stats = user.GetComponent<CharacterStats>();
            SuddenStrengthBuffRuntime existing = user.GetComponent<SuddenStrengthBuffRuntime>();
            return stats != null
                && existing != null
                && stats.Strength.HasModifierFromSource(existing);
        }

        protected override bool ExecuteCore(GameObject user)
        {
            if (!CanExecute(user))
                return false;

            SuddenStrengthBuffRuntime runtime = user.AddComponent<SuddenStrengthBuffRuntime>();
            runtime.Apply(strengthBonus, durationTurns);
            Debug.Log(
                $"[Sudden Strength] Applied +{strengthBonus} STR to {user.name} for {durationTurns} player phases.");
            return true;
        }
    }
}
