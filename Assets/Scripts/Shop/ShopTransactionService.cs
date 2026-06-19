using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Shop
{
    public enum ShopTransactionResult
    {
        Success = 0,
        InvalidQuantity = 1,
        InsufficientPlayerGold = 2,
        InsufficientShopGold = 3,
        InsufficientStock = 4,
        InventoryFull = 5,
        InvalidOffer = 6,
    }

    public readonly struct ShopPurchaseLine
    {
        public ItemData Item { get; }
        public int Quantity { get; }

        public ShopPurchaseLine(ItemData item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    public readonly struct ShopSellLine
    {
        public ShopSellOffer Offer { get; }
        public int Quantity { get; }

        public ShopSellLine(ShopSellOffer offer, int quantity)
        {
            Offer = offer;
            Quantity = quantity;
        }
    }

    public static class ShopTransactionService
    {
        public static ShopTransactionResult TryBuyBatch(
            ShopStateSnapshot snapshot,
            IReadOnlyList<ShopPurchaseLine> lines,
            out string message)
        {
            message = string.Empty;
            if (snapshot == null || lines == null || lines.Count == 0)
            {
                message = "Cart is empty.";
                return ShopTransactionResult.InvalidQuantity;
            }

            int totalCost = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                ShopPurchaseLine line = lines[i];
                if (line.Item == null || line.Quantity <= 0)
                {
                    message = "Invalid purchase.";
                    return ShopTransactionResult.InvalidQuantity;
                }

                int stockQty = TownShopStateService.GetStockQuantity(snapshot.stock, line.Item);
                if (stockQty < line.Quantity)
                {
                    message = $"{line.Item.itemName}: not enough stock.";
                    return ShopTransactionResult.InsufficientStock;
                }

                totalCost += ShopPriceResolver.GetBuyPrice(line.Item) * line.Quantity;
            }

            if (ShopGoldUtility.GetPartyGoldTotal() < totalCost)
            {
                message = "Not enough gold.";
                return ShopTransactionResult.InsufficientPlayerGold;
            }

            BaseActor recipient = GetActiveShopper();
            if (recipient == null)
            {
                message = "No party member to receive items.";
                return ShopTransactionResult.InventoryFull;
            }

            InventoryManager inventory = recipient.GetComponent<InventoryManager>();
            if (inventory == null)
            {
                message = "Party member cannot carry items.";
                return ShopTransactionResult.InventoryFull;
            }

            float addedWeight = 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                ShopPurchaseLine line = lines[i];
                addedWeight += new ItemInstance(line.Item, line.Quantity).TotalWeight;
            }

            CharacterStats stats = recipient.GetComponent<CharacterStats>();
            float maxWeight = stats != null ? stats.EncumbranceLimit : 0f;
            if (inventory.GetTotalWeight() + addedWeight > maxWeight)
            {
                message = "Too heavy to carry.";
                return ShopTransactionResult.InventoryFull;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                ShopPurchaseLine line = lines[i];
                if (!inventory.CanCarry(new ItemInstance(line.Item, line.Quantity)))
                {
                    message = $"Cannot carry {line.Item.itemName}.";
                    return ShopTransactionResult.InventoryFull;
                }
            }

            if (!ShopGoldUtility.TrySpendPartyGold(totalCost))
            {
                message = "Not enough gold.";
                return ShopTransactionResult.InsufficientPlayerGold;
            }

            var added = new List<ItemInstance>(lines.Count);
            for (int i = 0; i < lines.Count; i++)
            {
                ShopPurchaseLine line = lines[i];
                var purchase = new ItemInstance(line.Item, line.Quantity);
                if (!inventory.AddItem(purchase))
                {
                    RollbackAddedItems(inventory, added);
                    ShopGoldUtility.AddPartyGold(totalCost);
                    message = $"Could not add {line.Item.itemName} to inventory.";
                    return ShopTransactionResult.InventoryFull;
                }

                added.Add(purchase);
            }

            for (int i = 0; i < lines.Count; i++)
            {
                ShopPurchaseLine line = lines[i];
                if (TownShopStateService.TryRemoveStock(snapshot.stock, line.Item, line.Quantity))
                    continue;

                RollbackAddedItems(inventory, added);
                ShopGoldUtility.AddPartyGold(totalCost);
                message = "Shop stock changed.";
                return ShopTransactionResult.InsufficientStock;
            }

            snapshot.goldOnHand += totalCost;
            message = $"Purchased {lines.Count} line(s) for {totalCost} gold.";
            return ShopTransactionResult.Success;
        }

        static void RollbackAddedItems(InventoryManager inventory, List<ItemInstance> added)
        {
            for (int i = added.Count - 1; i >= 0; i--)
                inventory.TryRemoveCarried(added[i]);
        }

        public static ShopTransactionResult TryBuy(
            ShopStateSnapshot snapshot,
            ItemData item,
            int quantity,
            out string message)
        {
            message = string.Empty;
            if (snapshot == null || item == null || quantity <= 0)
            {
                message = "Invalid purchase.";
                return ShopTransactionResult.InvalidQuantity;
            }

            int stockQty = TownShopStateService.GetStockQuantity(snapshot.stock, item);
            if (stockQty < quantity)
            {
                message = "Shop is out of stock.";
                return ShopTransactionResult.InsufficientStock;
            }

            int unitPrice = ShopPriceResolver.GetBuyPrice(item);
            int totalCost = unitPrice * quantity;
            if (ShopGoldUtility.GetPartyGoldTotal() < totalCost)
            {
                message = "Not enough gold.";
                return ShopTransactionResult.InsufficientPlayerGold;
            }

            BaseActor recipient = GetActiveShopper();
            if (recipient == null)
            {
                message = "No party member to receive the item.";
                return ShopTransactionResult.InventoryFull;
            }

            InventoryManager inventory = recipient.GetComponent<InventoryManager>();
            if (inventory == null)
            {
                message = "Party member cannot carry items.";
                return ShopTransactionResult.InventoryFull;
            }

            var purchase = new ItemInstance(item, quantity);
            if (!inventory.CanCarry(purchase))
            {
                message = "Too heavy to carry.";
                return ShopTransactionResult.InventoryFull;
            }

            if (!ShopGoldUtility.TrySpendPartyGold(totalCost))
            {
                message = "Not enough gold.";
                return ShopTransactionResult.InsufficientPlayerGold;
            }

            if (!inventory.AddItem(purchase))
            {
                ShopGoldUtility.AddPartyGold(totalCost);
                message = "Could not add item to inventory.";
                return ShopTransactionResult.InventoryFull;
            }

            if (!TownShopStateService.TryRemoveStock(snapshot.stock, item, quantity))
            {
                inventory.TryRemoveCarried(purchase);
                ShopGoldUtility.AddPartyGold(totalCost);
                message = "Shop stock changed.";
                return ShopTransactionResult.InsufficientStock;
            }

            snapshot.goldOnHand += totalCost;
            message = $"Purchased {quantity} × {item.itemName} for {totalCost} gold.";
            return ShopTransactionResult.Success;
        }

        public static ShopTransactionResult TrySellBatch(
            ShopStateSnapshot snapshot,
            IReadOnlyList<ShopSellLine> lines,
            out string message)
        {
            message = string.Empty;
            if (snapshot == null || lines == null || lines.Count == 0)
            {
                message = "Cart is empty.";
                return ShopTransactionResult.InvalidQuantity;
            }

            int totalPayment = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                ShopSellLine line = lines[i];
                if (line.Offer == null || line.Offer.Definition == null || line.Quantity <= 0)
                {
                    message = "Invalid sale.";
                    return ShopTransactionResult.InvalidQuantity;
                }

                if (line.Quantity > line.Offer.Quantity)
                {
                    message = $"{line.Offer.DisplayName}: not enough to sell.";
                    return ShopTransactionResult.InvalidQuantity;
                }

                totalPayment += line.Offer.UnitSellPrice * line.Quantity;
            }

            if (totalPayment <= 0)
            {
                message = "Nothing to sell.";
                return ShopTransactionResult.InvalidOffer;
            }

            if (snapshot.goldOnHand < totalPayment)
            {
                message = "Shop cannot afford that.";
                return ShopTransactionResult.InsufficientShopGold;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                ShopSellLine line = lines[i];
                ShopTransactionResult result = TrySell(snapshot, line.Offer, line.Quantity, out message);
                if (result != ShopTransactionResult.Success)
                    return result;
            }

            message = $"Sold {lines.Count} line(s) for {totalPayment} gold.";
            return ShopTransactionResult.Success;
        }

        public static ShopTransactionResult TrySell(
            ShopStateSnapshot snapshot,
            ShopSellOffer offer,
            int quantity,
            out string message)
        {
            message = string.Empty;
            if (snapshot == null || offer == null || quantity <= 0)
            {
                message = "Invalid sale.";
                return ShopTransactionResult.InvalidQuantity;
            }

            if (quantity > offer.Quantity)
            {
                message = "Not enough to sell.";
                return ShopTransactionResult.InvalidQuantity;
            }

            int unitPrice = offer.UnitSellPrice;
            int totalPayment = unitPrice * quantity;
            if (totalPayment <= 0)
            {
                message = "This item has no sell value.";
                return ShopTransactionResult.InvalidOffer;
            }

            if (snapshot.goldOnHand < totalPayment)
            {
                message = "Shop cannot afford that.";
                return ShopTransactionResult.InsufficientShopGold;
            }

            if (offer.Kind == ShopSellKind.ManaStoneStack)
            {
                PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
                if (ledger == null || !ledger.TrySpend(offer.ManaTier, offer.ManaSpeciesId, quantity))
                {
                    message = "Could not remove mana stones.";
                    return ShopTransactionResult.InvalidOffer;
                }
            }
            else
            {
                if (offer.Owner == null || offer.Instance == null)
                {
                    message = "Item no longer available.";
                    return ShopTransactionResult.InvalidOffer;
                }

                InventoryManager inventory = offer.Owner.GetComponent<InventoryManager>();
                if (inventory == null || !inventory.TryConsumeCarriedQuantity(offer.Instance, quantity))
                {
                    message = "Could not remove item from inventory.";
                    return ShopTransactionResult.InvalidOffer;
                }
            }

            snapshot.goldOnHand -= totalPayment;
            ShopGoldUtility.AddPartyGold(totalPayment);
            TownShopStateService.AddStock(snapshot.boughtFromPlayer, offer.Definition, quantity);

            message = $"Sold {quantity} × {offer.DisplayName} for {totalPayment} gold.";
            return ShopTransactionResult.Success;
        }

        static BaseActor GetActiveShopper()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers == null || party.partyMembers.Count == 0)
                return null;

            int index = party.ActiveShopperMemberIndex;
            if (index < 0 || index >= party.partyMembers.Count)
                return party.partyMembers[0];

            return party.partyMembers[index];
        }
    }
}
