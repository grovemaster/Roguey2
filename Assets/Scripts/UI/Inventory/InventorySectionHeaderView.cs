using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Inventory
{
    public sealed class InventorySectionHeaderView : MonoBehaviour
    {
        TextMeshProUGUI _tmp;

        public static InventorySectionHeaderView Create(Transform parent, string richText)
        {
            var go = new GameObject("CategoryHeader", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.11f, 0.118f, 0.132f, 0.94f);

            var leRoot = go.AddComponent<LayoutElement>();
            leRoot.minHeight = 24f;
            leRoot.preferredHeight = 28f;
            leRoot.flexibleWidth = 1f;

            var textGo =
                new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(10f, 0f);
            tr.offsetMax = new Vector2(-8f, 0f);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.richText = true;
            tmp.fontSize = 13f;
            tmp.color = new Color(0.78f, 0.835f, 0.885f);
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.text = richText;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var v = go.AddComponent<InventorySectionHeaderView>();
            v._tmp = tmp;
            return v;
        }
    }
}
