using UnityEngine;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;

namespace JRogue.Testing.Essence
{
    public class EssenceTestHarness : MonoBehaviour
    {
        public EssenceSlotManager targetManager;
        public EssenceData testEssence;
        public int slotToTest = 0;

        [ContextMenu("Test Equip")]
        public void TestEquip()
        {
            if (targetManager != null && testEssence != null)
            {
                targetManager.EquipEssence(testEssence, slotToTest);
            }
        }

        [ContextMenu("Test Unequip")]
        public void TestUnequip()
        {
            if (targetManager != null)
            {
                targetManager.UnequipEssence(slotToTest);
            }
        }
    }
}