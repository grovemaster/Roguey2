using System.Collections.Generic;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace JRogue.UI.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        static InventoryUI _instance;

        [Header("System Links")]
        public InventoryManager playerInventory;

        public GameObject inventoryPanel;
        [SerializeField] Transform itemContainer;
        [SerializeField] TextMeshProUGUI weightText;
        [SerializeField] GameObject itemRowPrefab;

        [Header("Optional")]
        [SerializeField] TextMeshProUGUI footerText;

        [Header("Dark theme")]
        [SerializeField] Color panelBackgroundColor = new Color(0.08f, 0.085f, 0.095f, 0.96f);

        [SerializeField] Color rowNormalTint = new Color(0.16f, 0.166f, 0.177f, 0.94f);

        [SerializeField] Color rowSelectedTint = new Color(0.22f, 0.285f, 0.34f, 0.96f);

        InventoryViewModel _vm;
        readonly List<InventoryItemRowView> _rowViews = new List<InventoryItemRowView>();
        int _selection;

        Sprite _placeholderSprite;
        EquipmentManager _equipManager;
        Image _panelImage;
        Transform _weightBarRoot;

        public static bool BlocksGameplay =>
            _instance != null && _instance.inventoryPanel != null && _instance.inventoryPanel.activeSelf;

        public bool IsOpen =>
            inventoryPanel != null && inventoryPanel.activeSelf;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"{nameof(InventoryUI)} duplicate on '{gameObject.name}' ignored.");
                return;
            }

            _instance = this;

            EnsurePlaceholderSprite();

            if (!footerText)
            {
                var footGo = new GameObject("FooterHints", typeof(RectTransform), typeof(TextMeshProUGUI));
                footGo.transform.SetParent(inventoryPanel.transform, false);

                footerText = footGo.GetComponent<TextMeshProUGUI>();
                footerText.fontSize = 14;
                footerText.textWrappingMode = TextWrappingModes.Normal;
                footerText.overflowMode = TextOverflowModes.Truncate;
                footerText.margin = new Vector4(0, 10, 0, 10);
                footerText.color = new Color(0.68f, 0.71f, 0.74f);

                LayoutElement footerLayout = footGo.AddComponent<LayoutElement>();
                footerLayout.minHeight = 40;
                footerLayout.preferredHeight = 54;
                footerLayout.flexibleWidth = 1;
            }

            _panelImage = inventoryPanel.GetComponent<Image>();
            if (playerInventory != null)
                _equipManager = playerInventory.GetComponent<EquipmentManager>();

            if (weightText != null)
                _weightBarRoot = weightText.transform.parent;

            if (inventoryPanel.TryGetComponent<VerticalLayoutGroup>(out var outerVlg))
                outerVlg.childForceExpandWidth = true;

            ResolveItemContainer();
            ApplyFooterCopy();
            ApplyDarkPanelTheme();
        }

        void ResolveItemContainer()
        {
            if (inventoryPanel != null && itemContainer == null)
                itemContainer = inventoryPanel.transform;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void ApplyDarkPanelTheme()
        {
            if (_panelImage != null)
            {
                _panelImage.sprite = null;
                _panelImage.color = panelBackgroundColor;
            }

            if (weightText != null)
                weightText.color = new Color(0.88f, 0.91f, 0.93f);

            ApplyFooterCopy();
            if (!footerText) return;

            footerText.color = new Color(0.7f, 0.735f, 0.76f);
        }

        void EnsurePlaceholderSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _placeholderSprite =
                Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        void ApplyFooterCopy()
        {
            if (!footerText) return;

            footerText.text =
                "↑↓ or W/S — move   •   a–z / Shift+A–Z — jump to row   •   Enter or E — equip   •   D — drop (no world pile yet)"
                + "   •   X — inspect   •   I / Esc — toggle / close";
        }

        static Color ResolveNameTint(ItemData item, bool equipped)
        {
            if (equipped)
                return new Color(0.6f, 0.93f, 1f);

            bool weapon = item.damageModules != null && item.damageModules.Count > 0;
            if (weapon)
                return new Color(0.98f, 0.75f, 0.45f);

            switch (item.slotType)
            {
                case EquipmentSlot.Head:
                case EquipmentSlot.Torso:
                case EquipmentSlot.Legs:
                case EquipmentSlot.Feet:
                    return new Color(0.7f, 0.82f, 1f);

                default:
                    if (item.activeAbilities != null && item.activeAbilities.Count > 0)
                        return new Color(0.65f, 1f, 0.78f);

                    break;
            }

            return new Color(0.86f, 0.895f, 0.92f);
        }

        static void LogInspect(ItemData item)
        {
            if (!item)
            {
                Debug.Log("[Inspect] (no item)");
                return;
            }

            Debug.Log($"[Inspect] <b>{item.itemName}</b> | slot:{item.slotType} | wt:{item.weight:0.#} dmg:{item.damageModules?.Count ?? 0} mods:{item.statModifiers?.Count ?? 0} passives:{item.passiveEffects?.Count ?? 0} actives:{item.activeAbilities?.Count ?? 0}");
        }

        void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
                if (IsOpen)
                {
                    _selection = 0;
                    RefreshInventoryDisplay();
                }

                return;
            }

            if (!IsOpen)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                inventoryPanel.SetActive(false);
                return;
            }

            HandleMenuNavigation();
        }

        void HandleMenuNavigation()
        {
            if (_vm == null || _vm.Rows.Count == 0)
                return;

            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                TryEquipSelection();
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryEquipSelection();
                return;
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                TryDropSelection();
                return;
            }

            if (Keyboard.current.xKey.wasPressedThisFrame)
            {
                int i = Mathf.Clamp(_selection, 0, _vm.Rows.Count - 1);
                LogInspect(_vm.Rows[i].Item);
                return;
            }

            if (TryConsumeLetterShortcuts())
                return;

            PollArrowAndWasdMovement();
        }

        bool TryConsumeLetterShortcuts()
        {
            var kb = Keyboard.current;
            if (kb == null) return false;

            bool moved = false;
            for (int letterIndex = 0; letterIndex < 26; letterIndex++)
            {
                Key k = (Key)((int)Key.A + letterIndex);
                if (!kb[k].wasPressedThisFrame)
                    continue;

                int shiftedIndex = letterIndex + 26;

                bool shiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                if (shiftHeld && shiftedIndex < _vm.Rows.Count)
                    SetSelection(shiftedIndex);
                else
                    SetSelection(letterIndex);

                moved = true;
                break;
            }

            return moved;
        }

        void PollArrowAndWasdMovement()
        {
            var kb = Keyboard.current;
            int delta = 0;

            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                delta = -1;
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                delta = 1;

            if (delta == 0)
                return;

            int next = Mathf.Clamp(_selection + delta, 0, _vm.Rows.Count - 1);
            SetSelection(next);
        }

        void SetSelection(int index)
        {
            if (_vm == null || _vm.Rows.Count == 0)
                return;

            _selection = Mathf.Clamp(index, 0, _vm.Rows.Count - 1);
            ApplySelectionVisuals();
        }

        void ApplySelectionVisuals()
        {
            for (int i = 0; i < _rowViews.Count; i++)
            {
                bool sel = i == _selection;
                _rowViews[i].SetSelected(sel, rowSelectedTint, rowNormalTint);
            }
        }

        void TryEquipSelection()
        {
            if (_vm == null || _equipManager == null || _vm.Rows.Count == 0)
                return;

            var row = _vm.Rows[Mathf.Clamp(_selection, 0, _vm.Rows.Count - 1)];
            _equipManager.EquipItem(row.Item.slotType, row.Item);
            RefreshInventoryDisplay();
        }

        void TryDropSelection()
        {
            if (_vm == null || playerInventory == null || _vm.Rows.Count == 0)
                return;

            var row = _vm.Rows[Mathf.Clamp(_selection, 0, _vm.Rows.Count - 1)];
            if (!playerInventory.TryRemoveAt(row.FirstInventoryIndex))
                return;

            Debug.Log($"[Drop] Removed {row.Item.itemName} from bag (Phase 1 stub — no world drop).");
            RefreshInventoryDisplay();
        }

        public void RefreshInventoryDisplay()
        {
            if (playerInventory == null || _equipManager == null || itemContainer == null || itemRowPrefab == null)
                return;

            foreach (Transform child in itemContainer)
            {
                if (footerText && child == footerText.transform)
                    continue;

                if (_weightBarRoot != null && child == _weightBarRoot)
                    continue;

                Destroy(child.gameObject);
            }

            _rowViews.Clear();

            _vm = InventoryViewModel.Build(playerInventory.items, _equipManager);

            int count = _vm.Rows.Count;
            _selection = Mathf.Clamp(_selection, 0, Mathf.Max(0, count - 1));

            int insertRowSibling = footerText != null ? footerText.transform.GetSiblingIndex() : itemContainer.childCount;

            for (int i = 0; i < count; i++)
            {
                GameObject rowGo = Instantiate(itemRowPrefab, itemContainer);
                rowGo.transform.SetSiblingIndex(insertRowSibling + i);
                var view = rowGo.GetComponent<InventoryItemRowView>() ?? rowGo.AddComponent<InventoryItemRowView>();
                view.EnsureLayoutBuilt();

                var btn = view.Button;
                btn.transition = Selectable.Transition.None;

                int captured = i;
                InventoryViewModel.Row row = _vm.Rows[i];
                view.Bind(
                    row,
                    ResolveNameTint(row.Item, row.IsEquipped),
                    () =>
                    {
                        _selection = captured;
                        ApplySelectionVisuals();
                    },
                    row.Item.icon,
                    _placeholderSprite);

                _rowViews.Add(view);
            }

            if (footerText != null)
                footerText.transform.SetAsLastSibling();

            ApplyDarkPanelTheme();
            ApplySelectionVisuals();

            CharacterStats stats = playerInventory.GetComponent<CharacterStats>();
            float currentWeight = playerInventory.GetTotalWeight();
            weightText.text = $"Weight: {currentWeight:0.#} / {stats.EncumbranceLimit:0.#}";
            weightText.color = currentWeight > stats.EncumbranceLimit
                ? new Color(1f, 0.35f, 0.35f)
                : new Color(0.88f, 0.91f, 0.93f);
        }
    }
}
