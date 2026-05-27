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
        [Min(1)] public int maxDurationTurns = 1;
        public string[] immunityTags;
        public bool ignoresPoisonImmunity;
    }
}
