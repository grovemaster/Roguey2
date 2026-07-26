using UnityEngine;

namespace JRogue.World.Altar
{
    [CreateAssetMenu(
        fileName = "ManaStoneSpeciesAcceptFilter",
        menuName = "JRogue/World/Altar/Filters/Mana Stone Species")]
    public sealed class ManaStoneSpeciesAcceptFilter : AltarSlotAcceptFilter
    {
        public string requiredSpeciesId = "goblin";

        [Tooltip("If > 0, also require this mana stone tier. 0 = any tier.")]
        [Min(0)]
        public int requiredTierOrZero;

        public override bool AcceptsManaStone(int tier, string sourceSpeciesId)
        {
            if (string.IsNullOrEmpty(requiredSpeciesId))
                return false;
            if (!string.Equals(requiredSpeciesId, sourceSpeciesId))
                return false;
            if (requiredTierOrZero > 0 && tier != requiredTierOrZero)
                return false;
            return true;
        }
    }
}
