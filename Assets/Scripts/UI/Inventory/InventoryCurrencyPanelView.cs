using System;
using System.Collections.Generic;
using JRogue.Manager.Party;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
  /// <summary>Currency tab: mana tier summary with expandable per-species stacks.</summary>
  public sealed class InventoryCurrencyPanelView : MonoBehaviour
  {
    Transform _listRoot;
    TextMeshProUGUI _totalManaLabel;
    TextMeshProUGUI _goldLabel;

    readonly List<(Button btn, Image bg)> _tierRows = new List<(Button, Image)>();
    readonly List<(Button btn, Image bg)> _sourceRows = new List<(Button, Image)>();

    Action<int> _onTierClicked;
    Action<int, string> _onSourceClicked;

    public static InventoryCurrencyPanelView Ensure(Transform parent)
    {
      Transform existing = parent.Find("CurrencyPanel");
      if (existing != null)
      {
        var view = existing.GetComponent<InventoryCurrencyPanelView>() ??
                   existing.gameObject.AddComponent<InventoryCurrencyPanelView>();
        view.EnsureBuilt(parent);
        return view;
      }

      var root = new GameObject("CurrencyPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
      root.transform.SetParent(parent, false);

      var vlg = root.GetComponent<VerticalLayoutGroup>();
      vlg.spacing = 8;
      vlg.padding = new RectOffset(4, 8, 4, 8);
      vlg.childAlignment = TextAnchor.UpperLeft;
      vlg.childControlWidth = true;
      vlg.childControlHeight = true;
      vlg.childForceExpandWidth = true;
      vlg.childForceExpandHeight = false;

      var le = root.AddComponent<LayoutElement>();
      le.flexibleWidth = 1f;
      le.minHeight = 120f;

      var viewNew = root.AddComponent<InventoryCurrencyPanelView>();
      viewNew.EnsureBuilt(parent);
      return viewNew;
    }

    void EnsureBuilt(Transform parent)
    {
      if (_listRoot != null)
        return;

      transform.SetParent(parent, false);

      AddHeaderLabel(transform, "Mana stones by tier", 14f, out _);

      var tableGo = new GameObject("TierTable", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image));
      tableGo.transform.SetParent(transform, false);
      var tableImg = tableGo.GetComponent<Image>();
      tableImg.color = new Color(0.1f, 0.105f, 0.115f, 0.92f);
      var tableV = tableGo.GetComponent<VerticalLayoutGroup>();
      tableV.spacing = 0;
      tableV.childControlWidth = true;
      tableV.childForceExpandWidth = true;
      tableV.childControlHeight = true;
      tableV.childForceExpandHeight = false;

      var tableLe = tableGo.AddComponent<LayoutElement>();
      tableLe.flexibleWidth = 1f;

      _listRoot = tableGo.transform;

      AddColumnHeader(_listRoot, "Tier", "Count", 11f);

      var totalsGo = new GameObject("Totals", typeof(RectTransform));
      totalsGo.transform.SetParent(transform, false);
      var totalsLe = totalsGo.AddComponent<LayoutElement>();
      totalsLe.minHeight = 22f;

      _totalManaLabel = AddPlainLabel(totalsGo.transform, string.Empty, 12f);

      var goldGo = new GameObject("GoldRow", typeof(RectTransform));
      goldGo.transform.SetParent(transform, false);
      var goldLe = goldGo.AddComponent<LayoutElement>();
      goldLe.minHeight = 22f;
      _goldLabel = AddPlainLabel(goldGo.transform, string.Empty, 12f);
    }

    public void BindCallbacks(Action<int> onTier, Action<int, string> onSource)
    {
      _onTierClicked = onTier;
      _onSourceClicked = onSource;
    }

    public void Rebuild(
      IReadOnlyList<(int tier, int count)> tierTotals,
      IReadOnlyCollection<int> expandedTiers,
      int selectedTier,
      string selectedSpeciesId,
      string sourceFilter,
      float fontScale)
    {
      EnsureBuilt(transform.parent);

      ClearDynamicRows();

      var sourcesBuffer = new List<(string speciesId, string displayName, int count)>();

      for (int i = 0; i < tierTotals.Count; i++)
      {
        (int tier, int count) = tierTotals[i];
        bool expanded = expandedTiers != null && TierIsExpanded(expandedTiers, tier);
        AddTierRow(tier, count, expanded, tier == selectedTier, fontScale);

        if (!expanded)
          continue;

        InventoryCurrencyDisplay.CopyFilteredSourcesForTier(tier, sourceFilter, sourcesBuffer);

        if (sourcesBuffer.Count == 0)
        {
          AddSubheader(_listRoot, $"Tier {tier} — no matching sources");
          continue;
        }

        AddSubheader(_listRoot, $"Tier {tier} — by source species");

        for (int s = 0; s < sourcesBuffer.Count; s++)
        {
          (string speciesId, string displayName, int stackCount) = sourcesBuffer[s];
          bool selected = tier == selectedTier &&
                          string.Equals(speciesId, selectedSpeciesId, StringComparison.OrdinalIgnoreCase);
          AddSourceRow(tier, displayName, stackCount, selected, speciesId, fontScale);
        }
      }

      int manaTotal = InventoryCurrencyDisplay.GetPartyManaTotal();
      _totalManaLabel.fontSize = 12f * fontScale;
      _totalManaLabel.text = $"<color=#8a97a3>Total mana stones:</color> <b>{manaTotal}</b>";

      int goldTotal = InventoryCurrencyDisplay.GetPartyGoldTotal();
      _goldLabel.fontSize = 12f * fontScale;
      _goldLabel.text = goldTotal > 0
        ? $"<color=#8a97a3>Party gold:</color> <b>{goldTotal}</b>"
        : string.Empty;

      Canvas.ForceUpdateCanvases();
      LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    static bool TierIsExpanded(IReadOnlyCollection<int> expandedTiers, int tier)
    {
      foreach (int t in expandedTiers)
      {
        if (t == tier)
          return true;
      }

      return false;
    }

    void ClearDynamicRows()
    {
      if (_listRoot == null)
        return;

      for (int i = _listRoot.childCount - 1; i >= 1; i--)
        DestroyImmediate(_listRoot.GetChild(i).gameObject);

      _tierRows.Clear();
      _sourceRows.Clear();
    }

        void AddTierRow(int tier, int count, bool expanded, bool selectedTier, float fontScale)
        {
            var go = CreateRowButton(_listRoot, out Button btn, out Image bg);
            if (expanded)
                bg.color = new Color(0.22f, 0.285f, 0.34f, 0.98f);
            else if (selectedTier)
                bg.color = new Color(0.18f, 0.22f, 0.26f, 0.98f);

            var h = go.GetComponent<HorizontalLayoutGroup>() ?? go.AddComponent<HorizontalLayoutGroup>();
      h.padding = new RectOffset(10, 8, 6, 6);
      h.spacing = 8;
      h.childAlignment = TextAnchor.MiddleLeft;

      AddFlexLabel(go.transform, $"Tier {tier}", 13f, fontScale, TextAlignmentOptions.MidlineLeft);
      AddFixedLabel(go.transform, count.ToString(), 72f, 13f, fontScale, TextAlignmentOptions.MidlineRight);
      AddFixedLabel(go.transform, expanded ? "v" : ">", 24f, 11f, fontScale, TextAlignmentOptions.Center);

      int captured = tier;
      btn.onClick.AddListener(() => _onTierClicked?.Invoke(captured));
      _tierRows.Add((btn, bg));
    }

    void AddSourceRow(
      int tier,
      string displayName,
      int count,
      bool selected,
      string speciesId,
      float fontScale)
    {
      var go = CreateRowButton(_listRoot, out Button btn, out Image bg);
      bg.color = selected
        ? new Color(0.18f, 0.22f, 0.26f, 0.98f)
        : new Color(0.12f, 0.125f, 0.135f, 0.95f);

      var h = go.GetComponent<HorizontalLayoutGroup>() ?? go.AddComponent<HorizontalLayoutGroup>();
      h.padding = new RectOffset(28, 8, 5, 5);
      h.spacing = 8;

      AddFlexLabel(go.transform, displayName, 12f, fontScale, TextAlignmentOptions.MidlineLeft);
      AddFixedLabel(go.transform, $"×{count}", 56f, 12f, fontScale, TextAlignmentOptions.MidlineRight);

      int capturedTier = tier;
      string capturedSpecies = speciesId;
      btn.onClick.AddListener(() => _onSourceClicked?.Invoke(capturedTier, capturedSpecies));
      _sourceRows.Add((btn, bg));
    }

    static GameObject CreateRowButton(Transform parent, out Button btn, out Image bg)
    {
      var go = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
      go.transform.SetParent(parent, false);

      bg = go.GetComponent<Image>();
      bg.color = new Color(0.14f, 0.15f, 0.165f, 0.95f);

      btn = go.GetComponent<Button>();
      btn.transition = Selectable.Transition.None;

      var le = go.GetComponent<LayoutElement>();
      le.minHeight = 30f;
      le.preferredHeight = 32f;
      le.flexibleWidth = 1f;

      return go;
    }

    static void AddColumnHeader(Transform parent, string colA, string colB, float fontSize)
    {
      var go = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image));
      go.transform.SetParent(parent, false);
      go.GetComponent<Image>().color = new Color(0.11f, 0.115f, 0.125f, 0.98f);

      var h = go.GetComponent<HorizontalLayoutGroup>();
      h.padding = new RectOffset(10, 8, 4, 4);
      h.spacing = 8;

      var le = go.AddComponent<LayoutElement>();
      le.minHeight = 24f;

      var tmpA = AddFixedLabel(go.transform, colA, 0f, fontSize, 1f, TextAlignmentOptions.MidlineLeft);
      tmpA.color = new Color(0.55f, 0.6f, 0.65f);
      var tmpB = AddFixedLabel(go.transform, colB, 72f, fontSize, 1f, TextAlignmentOptions.MidlineRight);
      tmpB.color = new Color(0.55f, 0.6f, 0.65f);
      AddFixedLabel(go.transform, string.Empty, 24f, fontSize, 1f, TextAlignmentOptions.Center);
    }

    static void AddSubheader(Transform parent, string text)
    {
      var go = new GameObject("Subheader", typeof(RectTransform));
      go.transform.SetParent(parent, false);
      var le = go.AddComponent<LayoutElement>();
      le.minHeight = 22f;
      var tmp = go.AddComponent<TextMeshProUGUI>();
      tmp.text = text;
      tmp.fontSize = 11f;
      tmp.color = new Color(0.55f, 0.6f, 0.65f);
      tmp.margin = new Vector4(12, 4, 8, 0);
    }

    static void AddHeaderLabel(Transform parent, string text, float size, out TextMeshProUGUI tmp)
    {
      tmp = AddPlainLabel(parent, text, size);
      tmp.fontStyle = FontStyles.Bold;
      tmp.color = new Color(0.88f, 0.91f, 0.94f);
    }

    static TextMeshProUGUI AddPlainLabel(Transform parent, string text, float size)
    {
      var go = new GameObject("Label", typeof(RectTransform));
      go.transform.SetParent(parent, false);
      var le = go.AddComponent<LayoutElement>();
      le.minHeight = size + 8f;
      var tmp = go.AddComponent<TextMeshProUGUI>();
      tmp.text = text;
      tmp.fontSize = size;
      tmp.richText = true;
      tmp.alignment = TextAlignmentOptions.MidlineLeft;
      tmp.color = new Color(0.82f, 0.86f, 0.9f);
      return tmp;
    }

    static TextMeshProUGUI AddFlexLabel(
      Transform parent,
      string text,
      float size,
      float scale,
      TextAlignmentOptions align)
    {
      var go = new GameObject("Label", typeof(RectTransform));
      go.transform.SetParent(parent, false);
      var le = go.AddComponent<LayoutElement>();
      le.flexibleWidth = 1f;
      var tmp = go.AddComponent<TextMeshProUGUI>();
      tmp.text = text;
      tmp.fontSize = size * scale;
      tmp.alignment = align;
      tmp.color = new Color(0.88f, 0.91f, 0.94f);
      return tmp;
    }

    static TextMeshProUGUI AddFixedLabel(
      Transform parent,
      string text,
      float width,
      float size,
      float scale,
      TextAlignmentOptions align)
    {
      var go = new GameObject("Label", typeof(RectTransform));
      go.transform.SetParent(parent, false);
      var le = go.AddComponent<LayoutElement>();
      if (width > 0f)
      {
        le.minWidth = width;
        le.preferredWidth = width;
      }
      else
      {
        le.flexibleWidth = 1f;
      }

      var tmp = go.AddComponent<TextMeshProUGUI>();
      tmp.text = text;
      tmp.fontSize = size * scale;
      tmp.alignment = align;
      tmp.color = new Color(0.88f, 0.91f, 0.94f);
      return tmp;
    }
  }
}
