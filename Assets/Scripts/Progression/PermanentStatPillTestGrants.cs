using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Progression
{
    /// <summary>Dev/test grants for permanent stat pills (Strength + Poison Resistance).</summary>
    public static class PermanentStatPillTestGrants
    {
        public const string LogPrefix = "[PermanentStat:TestGrant]";

        public const string StrengthPillResourcesPath = "Item/Potion/Pill_Strength";
        public const string PoisonPillResourcesPath = "Item/Potion/Pill_PoisonResistance";

        /// <summary>Adds one of each v0 pill to the first living party member (always increments).</summary>
        public static void GrantOneOfEachToParty()
        {
            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null || party.partyMembers.Count == 0)
            {
                Debug.LogWarning($"{LogPrefix} No party members — cannot grant pills.");
                return;
            }

            BaseActor target = null;
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member != null && member.gameObject.activeInHierarchy)
                {
                    target = member;
                    break;
                }
            }

            if (target == null)
            {
                Debug.LogWarning($"{LogPrefix} No living party member — cannot grant pills.");
                return;
            }

            InventoryManager inv = target.GetComponent<InventoryManager>();
            if (inv == null)
            {
                Debug.LogWarning($"{LogPrefix} {target.name} has no InventoryManager.");
                return;
            }

            ItemData strength = Resources.Load<ItemData>(StrengthPillResourcesPath);
            ItemData poison = Resources.Load<ItemData>(PoisonPillResourcesPath);
            if (strength == null || poison == null)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Missing pill assets. Run JRogue → Inventory → Create Permanent Stat Pill Pack. " +
                    $"strength={(strength != null)} poison={(poison != null)}");
                return;
            }

            GrantOrIncrement(inv, strength, 1);
            GrantOrIncrement(inv, poison, 1);
            Debug.Log(
                $"{LogPrefix} Granted Pill of Strength ×1 and Pill of Poison Resistance ×1 to {target.DisplayName}.");
        }

        public static void GrantOrIncrement(InventoryManager inventory, ItemData definition, int quantity)
        {
            if (inventory == null || definition == null || quantity < 1)
                return;

            for (int i = 0; i < inventory.CarriedItems.Count; i++)
            {
                ItemInstance existing = inventory.CarriedItems[i];
                if (existing?.Definition != definition)
                    continue;

                existing.Quantity += quantity;
                existing.StorageLocation = ItemStorageLocation.Carried;
                existing.IsAppraised = true;
                return;
            }

            var instance = new ItemInstance(definition, quantity)
            {
                StorageLocation = ItemStorageLocation.Carried,
                IsAppraised = true,
            };

            if (!inventory.AddItem(instance))
            {
                Debug.LogWarning($"{LogPrefix} Could not add {definition.itemName} (encumbrance?).");
            }
        }
    }
}
