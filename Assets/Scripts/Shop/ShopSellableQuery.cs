using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Data.Item;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;

namespace JRogue.Shop
{
    public enum ShopSellKind
    {
        CarriedItem = 0,
        ManaStoneStack = 1,
    }

    public sealed class ShopSellOffer
    {
        public ShopSellKind Kind;
        public ItemData Definition;
        public ItemInstance Instance;
        public BaseActor Owner;
        public int CarriedListIndex = -1;
        public int ManaTier;
        public string ManaSpeciesId = string.Empty;
        public int Quantity;
        public int UnitSellPrice;

        public string DisplayName =>
            Definition != null ? Definition.itemName : "Item";

        public int MaxAffordableQuantity(int shopGoldOnHand)
        {
            if (UnitSellPrice <= 0)
                return Quantity;

            int byGold = shopGoldOnHand / UnitSellPrice;
            return UnityEngine.Mathf.Min(Quantity, byGold);
        }
    }

    public static class ShopSellableQuery
    {
        public static void BuildPartySellOffers(
            IReadOnlyList<BaseActor> partyMembers,
            List<ShopSellOffer> results,
            bool allowManaStones = true)
        {
            results.Clear();
            if (partyMembers == null)
                return;

            for (int m = 0; m < partyMembers.Count; m++)
                AppendMemberOffers(partyMembers[m], results);

            if (allowManaStones)
                AppendManaStoneOffers(results);
        }

        static void AppendMemberOffers(BaseActor member, List<ShopSellOffer> results)
        {
            if (member == null)
                return;

            InventoryManager inventory = member.GetComponent<InventoryManager>();
            EquipmentManager equipment = member.GetComponent<EquipmentManager>();
            if (inventory == null)
                return;

            IReadOnlyList<ItemInstance> carried = inventory.CarriedItems;
            for (int i = 0; i < carried.Count; i++)
            {
                ItemInstance instance = carried[i];
                if (!IsSellableCarriedItem(instance, equipment))
                    continue;

                results.Add(new ShopSellOffer
                {
                    Kind = ShopSellKind.CarriedItem,
                    Definition = instance.Definition,
                    Instance = instance,
                    Owner = member,
                    CarriedListIndex = i,
                    Quantity = instance.Quantity,
                    UnitSellPrice = ShopPriceResolver.GetSellPrice(instance.Definition),
                });
            }
        }

        static void AppendManaStoneOffers(List<ShopSellOffer> results)
        {
            PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
            if (ledger == null)
                return;

            foreach (var kv in ledger.Snapshot)
            {
                if (kv.Value <= 0)
                    continue;

                ManaStoneItemData definition = ShopManaStoneCatalog.GetDefinition(kv.Key.Tier);
                if (definition == null)
                    continue;

                results.Add(new ShopSellOffer
                {
                    Kind = ShopSellKind.ManaStoneStack,
                    Definition = definition,
                    ManaTier = kv.Key.Tier,
                    ManaSpeciesId = kv.Key.SourceSpeciesId,
                    Quantity = kv.Value,
                    UnitSellPrice = ShopPriceResolver.GetManaStoneSellPrice(kv.Key.Tier),
                });
            }
        }

        static bool IsSellableCarriedItem(ItemInstance instance, EquipmentManager equipment)
        {
            if (instance?.Definition == null)
                return false;

            ItemData def = instance.Definition;
            if (def.category == ItemCategory.Currency
                || def.category == ItemCategory.Essence
                || def.category == ItemCategory.QuestItem
                || def.category == ItemCategory.PlotItem)
                return false;

            if (equipment != null && equipment.TryGetEquippedSlot(instance, out _))
                return false;

            return ShopPriceResolver.GetSellPrice(def) > 0;
        }
    }

    static class ShopManaStoneCatalog
    {
        public static ManaStoneItemData GetDefinition(int tier) =>
            ManaStoneTierCatalog.LoadDefault()?.GetByTier(tier);
    }
}
