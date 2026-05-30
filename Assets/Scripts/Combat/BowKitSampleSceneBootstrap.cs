using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using UnityEngine;

namespace JRogue.Combat
{
    /// <summary>SampleScene QA: on play, equips short bow + stone arrows and leaves steel in the bag.</summary>
    public class BowKitSampleSceneBootstrap : MonoBehaviour
    {
        const int StoneQty = 20;
        const int SteelQty = 10;

        [SerializeField] bool applyOnAwake = true;
        [SerializeField] ItemData shortBow;
        [SerializeField] ItemData stoneArrow;
        [SerializeField] ItemData steelArrow;

        void Start()
        {
            if (!applyOnAwake || shortBow == null || stoneArrow == null || steelArrow == null)
                return;

            InventoryManager inv = GetComponentInChildren<InventoryManager>(true);
            EquipmentManager equip = GetComponentInChildren<EquipmentManager>(true);
            if (inv == null || equip == null)
            {
                Debug.LogWarning("[Bow] BowKitSampleSceneBootstrap: missing InventoryManager or EquipmentManager.");
                return;
            }

            if (equip.GetItemFromEquipmentSlot(EquipmentSlot.MainHand)?.IsBowWeapon == true)
                return;

            RemoveKitFromCarried(inv);

            var bow = new ItemInstance(shortBow, 1);
            var stone = new ItemInstance(stoneArrow, StoneQty);
            var steel = new ItemInstance(steelArrow, SteelQty);

            inv.AddItem(bow);
            inv.AddItem(stone);
            inv.AddItem(steel);
            equip.EquipItem(EquipmentSlot.MainHand, bow);
            equip.EquipItem(EquipmentSlot.OffHand, stone);
        }

#if UNITY_EDITOR
        public void EditorConfigure(ItemData bow, ItemData stone, ItemData steel)
        {
            applyOnAwake = true;
            shortBow = bow;
            stoneArrow = stone;
            steelArrow = steel;
        }
#endif

        static void RemoveKitFromCarried(InventoryManager inv)
        {
            for (int i = inv.CarriedItems.Count - 1; i >= 0; i--)
            {
                ItemData def = inv.CarriedItems[i]?.Definition;
                if (def != null && (def.IsBowWeapon || def.IsBowAmmo))
                    inv.TryRemoveCarriedAt(i);
            }
        }
    }
}
