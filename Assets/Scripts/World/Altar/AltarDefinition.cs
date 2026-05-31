using UnityEngine;

namespace JRogue.World.Altar
{
    [CreateAssetMenu(fileName = "AltarDefinition", menuName = "JRogue/World/Altar Definition")]
    public sealed class AltarDefinition : ScriptableObject
    {
        public string altarId = "altar";
        public string displayName = "Altar";
        [TextArea(2, 4)]
        public string descriptionTemplate =
            "This altar has places for offerings.";

        [TextArea(2, 4)]
        public string usedDescriptionTemplate = "This altar has been used. Its power is spent.";

        public AltarSlotDefinition[] slots = System.Array.Empty<AltarSlotDefinition>();
        public AltarCompletionRule[] completionRules = System.Array.Empty<AltarCompletionRule>();
        public Sprite overlaySprite;
        public bool blocksOccupancy = true;
        public int pickerSortOrder;
    }
}
