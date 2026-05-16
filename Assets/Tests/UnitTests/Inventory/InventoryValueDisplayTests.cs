using JRogue.Item;
using JRogue.UI.Inventory;
using NUnit.Framework;

namespace JRogue.Tests.Inventory
{
    public class InventoryValueDisplayTests
    {
        [Test]
        public void FormatListColumn_NoMonetaryValue_ReturnsDash()
        {
            var def = UnityEngine.ScriptableObject.CreateInstance<ItemData>();
            def.goldValue = 0;
            def.requiresAppraisal = false;

            Assert.AreEqual(InventoryValueDisplay.NoValue, InventoryValueDisplay.FormatListColumn(null, def));

            UnityEngine.Object.DestroyImmediate(def);
        }

        [Test]
        public void FormatListColumn_Unappraised_ReturnsQuestion()
        {
            var def = UnityEngine.ScriptableObject.CreateInstance<ItemData>();
            def.goldValue = 50;
            def.requiresAppraisal = true;
            var inst = new ItemInstance(def);

            Assert.AreEqual(InventoryValueDisplay.Unknown, InventoryValueDisplay.FormatListColumn(inst, def));

            UnityEngine.Object.DestroyImmediate(def);
        }

        [Test]
        public void FormatListColumn_Appraised_StackValue()
        {
            var def = UnityEngine.ScriptableObject.CreateInstance<ItemData>();
            def.goldValue = 10;
            def.requiresAppraisal = true;
            var inst = new ItemInstance(def, 3);
            inst.IsAppraised = true;

            Assert.AreEqual("30", InventoryValueDisplay.FormatListColumn(inst, def));

            UnityEngine.Object.DestroyImmediate(def);
        }
    }
}
