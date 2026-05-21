using UnityEngine;

namespace JRogue.Ability.Passive
{
    [CreateAssetMenu(fileName = "UndeadGraveTouchLinger", menuName = "JRogue/Passives/Undead Grave Touch Linger")]
    public class UndeadGraveTouchLingerPassive : PassiveEffect
    {
        public override void OnApply(GameObject user) =>
            Debug.Log($"{user.name}: Grave Touch — Linger (upgrade passive).");

        public override void OnRemove(GameObject user) { }
    }
}
