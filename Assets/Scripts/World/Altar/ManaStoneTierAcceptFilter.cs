using UnityEngine;

namespace JRogue.World.Altar
{
    [CreateAssetMenu(
        fileName = "ManaStoneTierAcceptFilter",
        menuName = "JRogue/World/Altar/Filters/Mana Stone Tier")]
    public sealed class ManaStoneTierAcceptFilter : AltarSlotAcceptFilter
    {
        [Range(1, 9)]
        public int tier = 9;

        public override bool AcceptsManaStone(int tier, string sourceSpeciesId) =>
            this.tier == tier;
    }
}
