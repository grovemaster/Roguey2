using System;
using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Floor;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class AutoPickupConfirmDialogUI : MonoBehaviour
    {
        const string Title = "Enter tile and pick up?";

        static AutoPickupConfirmDialogUI _instance;

        GameObject _modalRoot;
        TextMeshProUGUI _bodyText;
        Action _onYes;
        bool _blocking;

        public static bool BlocksGameplay => _instance != null && _instance._blocking;

        public static AutoPickupConfirmDialogUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(AutoPickupConfirmDialogUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AutoPickupConfirmDialogUI>();
            return _instance;
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            EnsureModalBuilt();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Update()
        {
            if (!_blocking || Keyboard.current == null)
                return;

            Keyboard kb = Keyboard.current;
            if (kb.yKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                CommitYes();
            else if (kb.nKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                Cancel();
        }

        public void Show(
            BaseActor mover,
            Vector3Int destination,
            IReadOnlyList<FloorItemEntry> pileEntries,
            IReadOnlyList<WorldItem> worldItems,
            Action onYes)
        {
            EnsureModalBuilt();
            _onYes = onYes;
            _blocking = true;

            if (_bodyText != null)
                _bodyText.text = BuildBody(mover, destination, pileEntries, worldItems);

            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }

        static string BuildBody(
            BaseActor mover,
            Vector3Int destination,
            IReadOnlyList<FloorItemEntry> pileEntries,
            IReadOnlyList<WorldItem> worldItems)
        {
            var sb = new StringBuilder();
            string moverName = mover != null ? mover.DisplayName : "Party member";
            sb.AppendLine($"<b>{moverName}</b> would move to ({destination.x}, {destination.y})");
            sb.AppendLine();
            sb.AppendLine("The following will be picked up:");

            if (pileEntries != null)
            {
                for (int i = 0; i < pileEntries.Count; i++)
                {
                    ItemInstance inst = pileEntries[i]?.instance;
                    ItemData def = inst?.Definition;
                    if (def == null)
                        continue;

                    sb.AppendLine(FormatItemLine(def.itemName, inst.Quantity));
                }
            }

            if (worldItems != null)
            {
                for (int i = 0; i < worldItems.Count; i++)
                {
                    ItemData def = worldItems[i]?.data;
                    if (def == null)
                        continue;

                    sb.AppendLine(FormatItemLine(def.itemName, 1));
                }
            }

            sb.AppendLine();
            sb.AppendLine("Move onto this tile and take these items?");
            sb.AppendLine();
            sb.Append("<size=14><color=#9bbdff>Y</color> confirm   ·   <color=#ffb28a>N</color> cancel</size>");
            return sb.ToString();
        }

        static string FormatItemLine(string name, int qty) =>
            qty > 1 ? $"  • {name} ×{qty}" : $"  • {name}";

        void CommitYes()
        {
            Action act = _onYes;
            Close();
            act?.Invoke();
        }

        void Cancel() => Close();

        void Close()
        {
            _blocking = false;
            _onYes = null;
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
        }

        void EnsureModalBuilt()
        {
            if (_modalRoot != null)
                return;

            var canvasGo = new GameObject("AutoPickupConfirmCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _modalRoot = new GameObject("Modal",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            _modalRoot.transform.SetParent(canvasGo.transform, false);

            RectTransform modalRt = (RectTransform)_modalRoot.transform;
            modalRt.anchorMin = Vector2.zero;
            modalRt.anchorMax = Vector2.one;
            modalRt.offsetMin = Vector2.zero;
            modalRt.offsetMax = Vector2.zero;

            Image dim = _modalRoot.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.65f);
            dim.raycastTarget = true;

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            bubble.transform.SetParent(_modalRoot.transform, false);
            RectTransform bubbleRt = (RectTransform)bubble.transform;
            bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRt.sizeDelta = new Vector2(520f, 360f);

            Image bubbleBg = bubble.GetComponent<Image>();
            bubbleBg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

            VerticalLayoutGroup vlg = bubble.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            CreateTmp(bubble.transform, Title, 22, FontStyles.Bold, out _);

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            bodyGo.transform.SetParent(bubble.transform, false);
            _bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
            _bodyText.fontSize = 16f;
            _bodyText.color = Color.white;
            _bodyText.alignment = TextAlignmentOptions.TopLeft;
            _bodyText.textWrappingMode = TextWrappingModes.Normal;

            LayoutElement bodyLe = bodyGo.GetComponent<LayoutElement>();
            bodyLe.minHeight = 180f;
            bodyLe.flexibleHeight = 1f;

            _modalRoot.SetActive(false);
        }

        static void CreateTmp(
            Transform parent,
            string text,
            float size,
            FontStyles style,
            out TextMeshProUGUI tmp)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}
