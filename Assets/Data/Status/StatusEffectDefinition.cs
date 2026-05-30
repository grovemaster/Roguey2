using UnityEngine;

namespace JRogue.Status
{
    [CreateAssetMenu(
        menuName = "JRogue/Status/Status Effect Definition",
        fileName = "Status_")]
    public class StatusEffectDefinition : ScriptableObject
    {
        public StatusEffectId statusId = StatusEffectId.None;
        public string displayName = "Status";
        [TextArea] public string description;

        [Tooltip("Negative statuses block rest and similar recovery; positive do not.")]
        public StatusPolarity polarity = StatusPolarity.Neutral;

        [Min(1)] public int maxDurationTurns = 1;
        public string[] immunityTags;
        public bool ignoresPoisonImmunity;

        public bool IsNegative => StatusEffectPolarityRules.IsNegative(polarity);
        public bool IsPositive => StatusEffectPolarityRules.IsPositive(polarity);

        protected virtual void OnValidate()
        {
            if (statusId != StatusEffectId.None)
                polarity = StatusEffectPolarityRules.GetDefaultPolarity(statusId);
        }
    }
}
