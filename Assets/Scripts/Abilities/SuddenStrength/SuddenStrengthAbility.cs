using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.SuddenStrength
{
    [CreateAssetMenu(fileName = "SuddenStrength_Standard", menuName = "JRogue/Abilities/Sudden Strength")]
    public class SuddenStrengthAbility : AbilityAction
    {
        public int strengthBonus = 100;
        public int durationTurns = 10;

        public override bool CanExecute(GameObject user)
        {
            if (user == null)
                return false;

            CharacterStats stats = user.GetComponent<CharacterStats>();
            if (stats == null)
                return false;

            SuddenStrengthBuffRuntime existing = user.GetComponent<SuddenStrengthBuffRuntime>();
            if (existing != null)
            {
                if (!stats.Strength.HasModifierFromSource(existing))
                {
                    Object.DestroyImmediate(existing);
                }
                else
                {
                    Debug.Log($"[Sudden Strength] Already active on {user.name}.");
                    return false;
                }
            }

            return true;
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
