using UnityEngine;

namespace JRogue.Ability
{
    [CreateAssetMenu(fileName = "GraveTouch", menuName = "JRogue/Abilities/Undead/Grave Touch")]
    public class UndeadGraveTouchAbility : AbilityAction
    {
        public override bool CanExecute(GameObject user) => user != null;

        protected override bool ExecuteCore(GameObject user)
        {
            Debug.Log($"{user.name} uses Grave Touch (data-only active).");
            return true;
        }
    }
}
