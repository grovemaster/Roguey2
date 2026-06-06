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
            sb.AppendLine(FormatHeroTitle(item, selectedRow.Instance));
            sb.AppendLine(
                $"<color=#8a97a3>{FormatHeroSubtitle(item, selectedRow)}</color>");
            sb.Append(FormatInspectBody(item, selectedRow));
            return sb.ToString();
        }

        /// <summary>Scrollable inspect body (stats, marks, inscription) — excludes hero header.</summary>
        public static string FormatInspectBody(ItemData item, InventoryViewModel.Row selectedRow = default)
        {
            if (item == null)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine(InventoryValueDisplay.FormatInspectValue(selectedRow.Instance, item, richText: true));
            sb.AppendLine(
                $"<color=#8a97a3>Weight (stack):</color> {StackWeight(selectedRow):0.#} kg    <color=#8a97a3>Location:</color> {DescribeLocation(selectedRow.Instance, selectedRow.OwnerDisplayName)}");

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

            if (item is EvocableItemData evocable)
                AppendEvocableInspect(sb, evocable, selectedRow.Instance);

            if (item is LightSourceItemData lightSource)
                AppendLightSourceInspect(sb, lightSource, selectedRow.Instance);

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

            return sb.ToString().TrimEnd();
        }

        static void AppendLightSourceInspect(StringBuilder sb, LightSourceItemData lightSource, ItemInstance instance)
        {
            string line = LightSourceItemRules.FormatInspectSubtitle(instance, lightSource);
            if (string.IsNullOrEmpty(line))
                return;

            sb.AppendLine($"<color=#cfd6dd><b>Light</b></color> {line}");
        }

        static void AppendEvocableInspect(StringBuilder sb, EvocableItemData evocable, ItemInstance instance)
        {
            if (instance != null)
                EvocableChargeRules.ClampCharges(instance);

            int current = instance != null ? instance.CurrentCharges : evocable.startingCharges;
            int max = instance != null ? instance.MaxCharges : evocable.maxCharges;
            sb.AppendLine($"<color=#cfd6dd><b>Charges</b></color> {current} / {max}");

            if (evocable.consumesWhenEmpty)
                sb.AppendLine("<color=#8a97a3>Recharge:</color> Consumable (removed at 0 charges)");
            else
                sb.AppendLine(
                    $"<color=#8a97a3>Recharge:</color> +1 every {evocable.rechargeIntervalPlayerPhases} player phases");

            if (evocable.invokeAbility != null)
            {
                string abilityName = !string.IsNullOrEmpty(evocable.invokeAbility.abilityName)
                    ? evocable.invokeAbility.abilityName
                    : evocable.invokeAbility.name;
                string targetNote = evocable.invokeAbility.requiresTarget ? " (targeted)" : string.Empty;
                sb.AppendLine($"<color=#8a97a3>Invoke:</color> {abilityName}{targetNote}");
            }
        }

        public static string FormatHeroSubtitle(ItemData item, InventoryViewModel.Row row)
        {
            if (item == null)
                return string.Empty;

            var parts = new System.Collections.Generic.List<string>();
            if (item.damageModules is { Count: > 0 })
                parts.Add(item.damageModules[0].type.ToString());
            parts.Add(item.slotType.ToString());
            if ((item.inventoryRiskHints & ItemInventoryRiskHint.Rare) != 0)
                parts.Add("Rare");
            if (!string.IsNullOrEmpty(row.OwnerDisplayName))
                parts.Add(row.OwnerDisplayName);
            return string.Join(" · ", parts);
        }

        public static string FormatHeroTitle(ItemData item, ItemInstance instance)
        {
            if (item == null)
                return "(no item)";

            string marks = string.Empty;
            if (instance != null && (instance.UserMarks & ItemUserMark.Favorite) != 0)
                marks = "  <color=#e8c56c>★</color>";

            return $"<size=20><b>{item.itemName}</b></size>{marks}";
        }

        static float StackWeight(InventoryViewModel.Row row)
        {
            if (row.Instance != null)
                return row.Instance.TotalWeight;
            return row.Item != null ? row.Item.weight : 0f;
        }

        static string DescribeLocation(ItemInstance instance, string ownerName)
        {
            if (instance == null)
                return "—";

            string place;
            switch (instance.StorageLocation)
            {
                case ItemStorageLocation.OnGround:
                    place = "<color=#9aabbe>On ground</color>";
                    break;
                case ItemStorageLocation.Equipped:
                    place = "<color=#8ed4ff>Equipped</color>";
                    break;
                case ItemStorageLocation.Carried:
                    place = "<color=#c8dae8>Carried</color>";
                    break;
                default:
                    place = instance.StorageLocation.ToString();
                    break;
            }

            if (!string.IsNullOrEmpty(ownerName))
                return $"{place} · {ownerName}";
            return place;
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
