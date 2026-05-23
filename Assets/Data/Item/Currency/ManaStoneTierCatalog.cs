using JRogue.Item;
using UnityEngine;

namespace JRogue.Data.Item
{
    [CreateAssetMenu(fileName = "ManaStoneTierCatalog", menuName = "JRogue/Item/Mana Stone Tier Catalog")]
    public class ManaStoneTierCatalog : ScriptableObject
    {
        [Tooltip("Index by tier: element 1 = tier 1, … element 9 = tier 9.")]
        [SerializeField] ManaStoneItemData[] tiersByNumber = new ManaStoneItemData[10];

        public ManaStoneItemData GetByTier(int tier)
        {
            if (tier < 1 || tier > 9 || tiersByNumber == null || tier >= tiersByNumber.Length)
                return null;
            return tiersByNumber[tier];
        }

        public static ManaStoneTierCatalog LoadDefault() =>
            Resources.Load<ManaStoneTierCatalog>("Item/Currency/ManaStoneTierCatalog");
    }
}
