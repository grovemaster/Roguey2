using JRogue.Actors;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(fileName = "StatMinimum", menuName = "JRogue/Interactables/Preconditions/Stat Minimum")]
    public sealed class StatMinimumPrecondition : InteractablePrecondition
    {
        public StatType statType = StatType.Strength;
        public int minimumValue = 1;

        public override bool Evaluate(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source,
            out string failureReason)
        {
            failureReason = null;
            CharacterStats stats = bumper?.stats;
            if (stats == null)
            {
                failureReason = "No stats on bumper.";
                return false;
            }

            int value = statType switch
            {
                StatType.Strength => stats.Strength.GetValue(),
                _ => 0,
            };

            if (value < minimumValue)
            {
                failureReason = $"{statType} {value} is below required {minimumValue}.";
                return false;
            }

            return true;
        }
    }
}
