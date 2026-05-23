using System;
using System.Collections.Generic;
using System.Text;
using JRogue.Data.Item;
using JRogue.Item;
using JRogue.Manager.Party;

namespace JRogue.UI.Inventory
{
  public static class InventoryCurrencyDisplay
  {
    public static string FormatSpeciesDisplayName(string speciesId)
    {
      if (string.IsNullOrWhiteSpace(speciesId))
        return "Unknown";

      string[] parts = speciesId.Split('_');
      var sb = new StringBuilder();
      for (int i = 0; i < parts.Length; i++)
      {
        string p = parts[i];
        if (p.Length == 0)
          continue;
        if (sb.Length > 0)
          sb.Append(' ');
        sb.Append(char.ToUpperInvariant(p[0]));
        if (p.Length > 1)
          sb.Append(p.Substring(1));
      }

      return sb.Length > 0 ? sb.ToString() : speciesId;
    }

    public static int GetPartyManaTotal()
    {
      PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
      return ledger != null ? ledger.GetTotalCount() : 0;
    }

    public static int GetPartyGoldTotal()
    {
      PartyCurrencyLedger ledger = PartyCurrencyLedger.Instance;
      return ledger != null ? ledger.GetTotalCount() : 0;
    }

    public static string FormatManaStoneDetail(int tier, string speciesId, int count, int tierTotal)
    {
      string display = FormatSpeciesDisplayName(speciesId);
      var sb = new StringBuilder();
      sb.AppendLine("<color=#8a97a3>Party currency · weight 0 · read-only</color>");
      sb.AppendLine();
      sb.AppendLine($"<color=#8a97a3>Source species:</color> <b>{display}</b>");
      sb.AppendLine($"<color=#8a97a3>Species id:</color> {speciesId}");
      sb.AppendLine($"<color=#8a97a3>Quantity:</color> <b>{count}</b>");
      sb.AppendLine($"<color=#8a97a3>Tier total:</color> {tierTotal} across all sources");
      return sb.ToString();
    }

    public static ManaStoneItemData GetManaStoneDefinition(int tier)
    {
      ManaStoneTierCatalog catalog = ManaStoneTierCatalog.LoadDefault();
      return catalog != null ? catalog.GetByTier(tier) : null;
    }

    public static void CopyFilteredSourcesForTier(
      int tier,
      string filterNeedle,
      List<(string speciesId, string displayName, int count)> dest)
    {
      dest.Clear();
      PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
      if (ledger == null)
        return;

      var raw = new List<(string speciesId, int count)>();
      ledger.CopyStacksForTier(tier, raw);

      string needle = string.IsNullOrWhiteSpace(filterNeedle) ? string.Empty : filterNeedle.Trim();
      for (int i = 0; i < raw.Count; i++)
      {
        (string speciesId, int count) = raw[i];
        string display = FormatSpeciesDisplayName(speciesId);
        if (needle.Length > 0 &&
            display.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0 &&
            speciesId.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
          continue;

        dest.Add((speciesId, display, count));
      }
    }
  }
}
