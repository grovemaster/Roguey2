using System.Linq;
using System.Text;
using JRogue.Item;
using JRogue.Manager.Equipment;
using UnityEngine;

namespace JRogue.UI.Inventory
{
    public static class InventoryDetailFormatter
    {
        public static string Format(ItemData item, InventoryViewModel.Row selectedRow = default)
        {
            if (item == null)
                return "<color=#7a8690>(no item)</color>";

            var sb = new StringBuilder();
            sb.AppendLine($"<size=18><b>{item.itemName}</b></size>");
            sb.AppendLine(
                $"<color=#8a97a3>Category:</color> {item.category}    <color=#8a97a3>Slot:</color> {item.slotType}");
            sb.AppendLine(
                $"<color=#8a97a3>Weight:</color> {item.weight:0.#} ea.    <color=#8a97a3>Location:</color> {DescribeLocation(selectedRow.Instance)}");

            if ((item.inventoryRiskHints & ItemInventoryRiskHint.Rare) != 0)
                sb.AppendLine("<color=#c4a35a>Rare</color>");
            if ((item.inventoryRiskHints & ItemInventoryRiskHint.Cursed) != 0)
                sb.AppendLine("<color=#c45a7a>Cursed</color>");
            if ((item.inventoryRiskHints & ItemInventoryRiskHint.StoryTagged) != 0)
                sb.AppendLine("<color=#7cb8ff>Story</color>");
            if ((item.inventoryRiskHints & ItemInventoryRiskHint.HighValue) != 0)
                sb.AppendLine("<color=#82e0b8>High value</color>");

            if (selectedRow.Instance != null && selectedRow.Instance.Quantity > 1)
                sb.AppendLine($"<color=#8a97a3>Quantity:</color> {selectedRow.Instance.Quantity}");

            if (item.damageModules is { Count: > 0 })
            {
                sb.AppendLine("<color=#cfd6dd><b>Damage</b></color>");
                foreach (DamageEntry d in item.damageModules)
                    sb.AppendLine($" • {d.type}: <b>{d.value}</b>");
            }

            if (item.statModifiers is { Count: > 0 })
            {
                sb.AppendLine("<color=#cfd6dd><b>Stat modifiers</b></color>");
                foreach (var m in item.statModifiers)
                    sb.AppendLine($" • {m.targetStat}: <b>{m.modifierAmount:+#;-#;0}</b>");
            }

            if (item.passiveEffects is { Count: > 0 })
            {
                sb.AppendLine($"<color=#cfd6dd><b>Passive</b></color> ({item.passiveEffects.Count})");
            }

            if (item.activeAbilities is { Count: > 0 })
            {
                sb.AppendLine("<color=#cfd6dd><b>Active</b></color>");
                for (int i = 0; i < item.activeAbilities.Count; i++)
                {
                    var a = item.activeAbilities[i];
                    string label = !string.IsNullOrEmpty(a.abilityName) ? a.abilityName : $"Ability{i + 1}";
                    sb.AppendLine($" • {label}");
                }
            }

            if (selectedRow.Instance != null)
            {
                ItemUserMark m = selectedRow.Instance.UserMarks;
                if (m != ItemUserMark.None)
                {
                    var bits = new System.Collections.Generic.List<string>();
                    if ((m & ItemUserMark.Favorite) != 0)
                        bits.Add("<color=#e8c56c>Fav</color>");
                    if ((m & ItemUserMark.Protected) != 0)
                        bits.Add("<color=#7ec8ff>Protected</color>");
                    if ((m & ItemUserMark.Junk) != 0)
                        bits.Add("<color=#9aa7b0>Junk</color>");
                    sb.AppendLine($"<color=#8a97a3>Marks:</color> {string.Join(" · ", bits)}");
                }

                string ins = selectedRow.Instance.UserInscription;
                if (!string.IsNullOrWhiteSpace(ins))
                    sb.AppendLine($"<color=#8a97a3>Inscription:</color> {ins}");

                InventoryInscriptionGuards.ParsedGuards g =
                    InventoryInscriptionGuards.Parse(selectedRow.Instance.UserInscription);
                if (g != InventoryInscriptionGuards.ParsedGuards.None)
                    sb.AppendLine($"<color=#6a7884>Active guards:</color> {g}");
                else
                    sb.AppendLine("<color=#5a6974>Inscription guards: <i>stub</i> (!d / !u etc. Phase 3+).</color>");
            }

            return sb.ToString();
        }

        static string DescribeLocation(ItemInstance instance)
        {
            if (instance == null)
                return "—";

            switch (instance.StorageLocation)
            {
                case ItemStorageLocation.OnGround:
                    return "<color=#9aabbe>On ground (pre-pickup)</color>";
                case ItemStorageLocation.Equipped:
                    return "<color=#8ed4ff>Equipped</color>";
                case ItemStorageLocation.Carried:
                    return "<color=#c8dae8>Carried</color>";
                default:
                    return instance.StorageLocation.ToString();
            }
        }

        public static string FormatCompareEquippedSameSlot(ItemData equippedDef, InventoryViewModel.Row baseline)
        {
            if (baseline.Item == null)
                return string.Empty;

            ItemData cand = baseline.Item;

            if (equippedDef == null || equippedDef == cand)
                return "<color=#6a7884>No other equipped item in this slot axis to compare.</color>";

            var sb = new StringBuilder();
            sb.AppendLine("<color=#cfd6dd><b>vs equipped</b></color>");
            int sumOld = DamageSum(equippedDef);
            int sumNew = DamageSum(cand);
            sb.AppendLine(
                $"DMG Δ: <color=#cfd6dd>{sumNew}</color> − <color=#cfd6dd>{sumOld}</color> = <b>{sumNew - sumOld:+##;-##;0}</b>");
            int modOld = equippedDef.statModifiers?.Count ?? 0;
            int modNew = cand.statModifiers?.Count ?? 0;
            sb.AppendLine($"Stat lines: equipped {modOld}, selected {modNew}");

            sb.Append($"<color=#5a6974>Deeper equip delta (reqs/resists) Phase 2+.</color>");
            return sb.ToString();
        }

        static int DamageSum(ItemData d) =>
            d.damageModules?.Sum(dm => dm.value) ?? 0;
    }
}
