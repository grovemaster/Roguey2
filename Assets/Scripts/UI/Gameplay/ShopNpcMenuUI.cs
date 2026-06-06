using System;
using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Party;
using JRogue.Shop;
using JRogue.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class ShopNpcMenuUI : MonoBehaviour
    {
        enum ShopMode
        {
            Buy = 0,
            Sell = 1,
        }

        enum InputFocus
        {
            Stock = 0,
            Cart = 1,
        }

        sealed class ShopCartEntry
        {
            public ShopMode Mode;
            public ItemData Item;
            public int Quantity;
            public int UnitPrice;

            public ShopSellKind SellKind;
            public ItemInstance SellInstance;
            public BaseActor SellOwner;
            public int SellCarriedListIndex = -1;
            public int SellManaTier;
            public string SellManaSpeciesId = string.Empty;
            public int SellSourceQuantity;

            public static ShopCartEntry ForBuy(ItemData item, int quantity) =>
                new ShopCartEntry
                {
                    Mode = ShopMode.Buy,
                    Item = item,
                    Quantity = quantity,
                    UnitPrice = ShopPriceResolver.GetBuyPrice(item),
                };

            public static ShopCartEntry ForSell(ShopSellOffer offer, int quantity) =>
                new ShopCartEntry
                {
                    Mode = ShopMode.Sell,
                    Item = offer.Definition,
                    Quantity = quantity,
                    UnitPrice = offer.UnitSellPrice,
                    SellKind = offer.Kind,
                    SellInstance = offer.Instance,
                    SellOwner = offer.Owner,
                    SellCarriedListIndex = offer.CarriedListIndex,
                    SellManaTier = offer.ManaTier,
                    SellManaSpeciesId = offer.ManaSpeciesId ?? string.Empty,
                    SellSourceQuantity = offer.Quantity,
                };

            public ShopSellOffer ToSellOffer() =>
                new ShopSellOffer
                {
                    Kind = SellKind,
                    Definition = Item,
                    Instance = SellInstance,
                    Owner = SellOwner,
                    CarriedListIndex = SellCarriedListIndex,
                    ManaTier = SellManaTier,
                    ManaSpeciesId = SellManaSpeciesId,
                    Quantity = SellSourceQuantity,
                    UnitSellPrice = UnitPrice,
                };

            public bool MatchesSellOffer(ShopSellOffer offer)
            {
                if (Mode != ShopMode.Sell || offer == null || Item != offer.Definition || SellKind != offer.Kind)
                    return false;

                if (SellKind == ShopSellKind.ManaStoneStack)
                    return SellManaTier == offer.ManaTier && SellManaSpeciesId == offer.ManaSpeciesId;

                return SellInstance != null
                    && offer.Instance != null
                    && SellInstance.Id == offer.Instance.Id;
            }
        }

        const float OuterMargin = 12f;
        const float PanelBorderWidth = 2f;
        const float FontScale = 1.5f;
        const int LayoutVersion = 6;

        static readonly Color PanelBorderColor = new Color(0.34f, 0.42f, 0.52f, 1f);
        static readonly Color PanelFillColor = new Color(0.08f, 0.1f, 0.13f, 1f);
        static readonly Color HeaderFillColor = new Color(0.1f, 0.12f, 0.16f, 1f);
        static readonly Color TransactionFillColor = new Color(0.06f, 0.08f, 0.11f, 0.98f);

        static ShopNpcMenuUI _instance;

        GameObject _root;
        TextMeshProUGUI _headerText;
        TextMeshProUGUI _playerGoldText;
        TextMeshProUGUI _shopGoldText;
        Button _buyTabButton;
        Button _sellTabButton;
        RectTransform _listContent;
        InventoryInspectPaneView _inspectPane;
        Sprite _placeholderSprite;
        TextMeshProUGUI _qtyText;
        TextMeshProUGUI _totalText;
        TextMeshProUGUI _messageText;
        TextMeshProUGUI _footerText;
        Button _confirmButton;
        GameObject _cartColumn;
        RectTransform _cartContent;
        TextMeshProUGUI _cartHeaderText;

        readonly List<GameObject> _rowObjects = new List<GameObject>();
        readonly List<GameObject> _cartRowObjects = new List<GameObject>();
        readonly List<ShopStockSnapshot> _buyRows = new List<ShopStockSnapshot>();
        readonly List<ShopSellOffer> _sellRows = new List<ShopSellOffer>();
        readonly List<ShopCartEntry> _cart = new List<ShopCartEntry>();
        readonly List<ShopPurchaseLine> _purchaseScratch = new List<ShopPurchaseLine>();
        readonly List<ShopSellLine> _sellScratch = new List<ShopSellLine>();

        ShopNpcController _shopNpc;
        ShopNpcDefinition _definition;
        ShopStateSnapshot _snapshot;
        Action _onClosed;
        ShopMode _mode;
        InputFocus _inputFocus;
        int _selectedIndex;
        int _cartSelectedIndex;
        int _quantity;
        bool _blocking;
        int _layoutVersion;

        public static bool BlocksGameplay =>
            _instance != null && _instance._blocking;

        public static ShopNpcMenuUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(ShopNpcMenuUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ShopNpcMenuUI>();
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
            EnsureBuilt();
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
            if (kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
            {
                if (_inputFocus == InputFocus.Cart)
                    MoveCartSelection(-1);
                else
                    MoveSelection(-1);
            }
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            {
                if (_inputFocus == InputFocus.Cart)
                    MoveCartSelection(1);
                else
                    MoveSelection(1);
            }
            else if (kb.tabKey.wasPressedThisFrame)
                ToggleInputFocus();
            else if (kb.commaKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
                AdjustQuantity(-1);
            else if (kb.periodKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
                AdjustQuantity(1);
            else if (kb.aKey.wasPressedThisFrame)
                AddSelectedToCart();
            else if (kb.dKey.wasPressedThisFrame)
                RemoveFromCartSelection();
            else if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                ConfirmTransaction();
            else if (kb.bKey.wasPressedThisFrame && _definition != null && _definition.allowPlayerBuy)
                SetMode(ShopMode.Buy);
            else if (kb.vKey.wasPressedThisFrame && _definition != null && _definition.allowPlayerSell)
                SetMode(ShopMode.Sell);
        }

        public void Show(ShopNpcController shopNpc, Action onClosed = null)
        {
            if (shopNpc == null || shopNpc.ShopDefinition == null)
                return;

            EnsureBuilt();
            _shopNpc = shopNpc;
            _onClosed = onClosed;
            _definition = shopNpc.ShopDefinition;
            TownShopStateService.EnsureRunService();
            _snapshot = TownShopStateService.Instance.GetOrCreateSnapshot(_definition);
            _blocking = true;
            _selectedIndex = 0;
            _cartSelectedIndex = 0;
            _inputFocus = InputFocus.Stock;
            _quantity = 0;
            _cart.Clear();

            _mode = _definition.allowPlayerBuy ? ShopMode.Buy : ShopMode.Sell;
            RefreshAll();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close()
        {
            Action onClosed = _onClosed;
            _blocking = false;
            _onClosed = null;
            _shopNpc = null;
            _definition = null;
            _snapshot = null;
            if (_root != null)
                _root.SetActive(false);

            onClosed?.Invoke();
        }

        void SetMode(ShopMode mode)
        {
            if (_definition == null)
                return;

            if (mode == ShopMode.Buy && !_definition.allowPlayerBuy)
                return;
            if (mode == ShopMode.Sell && !_definition.allowPlayerSell)
                return;

            _mode = mode;
            _selectedIndex = 0;
            _cartSelectedIndex = 0;
            _inputFocus = InputFocus.Stock;
            _quantity = 0;
            _cart.Clear();
            RefreshAll();
        }

        void RefreshAll()
        {
            if (_definition == null || _snapshot == null)
                return;

            _headerText.text = $"SHOP — {_definition.displayName}";
            _playerGoldText.text = $"Player gold: {ShopGoldUtility.GetPartyGoldTotal()}";
            _shopGoldText.text = _definition.allowPlayerSell
                ? $"Shop gold: {_snapshot.goldOnHand}"
                : string.Empty;

            _buyTabButton.gameObject.SetActive(_definition.allowPlayerBuy);
            _sellTabButton.gameObject.SetActive(_definition.allowPlayerSell);
            SetTabHighlight(_buyTabButton, _mode == ShopMode.Buy);
            SetTabHighlight(_sellTabButton, _mode == ShopMode.Sell);

            RebuildList();
            RefreshDetail();
            RefreshTransactionPanel();
            RefreshFooterHints();
        }

        void RebuildList()
        {
            ClearRows();
            _buyRows.Clear();
            _sellRows.Clear();

            if (_mode == ShopMode.Buy)
            {
                for (int i = 0; i < _snapshot.stock.Count; i++)
                {
                    ShopStockSnapshot row = _snapshot.stock[i];
                    if (row?.item == null || row.quantity <= 0)
                        continue;

                    _buyRows.Add(row);
                    CreateRow(
                        row.item,
                        row.item.itemName,
                        row.quantity,
                        ShopPriceResolver.GetBuyPrice(row.item),
                        _buyRows.Count - 1);
                }
            }
            else
            {
                IReadOnlyList<BaseActor> party = PartyManager.Instance?.partyMembers;
                ShopSellableQuery.BuildPartySellOffers(party, _sellRows);
                for (int i = 0; i < _sellRows.Count; i++)
                {
                    ShopSellOffer offer = _sellRows[i];
                    CreateRow(
                        offer.Definition,
                        offer.DisplayName,
                        offer.Quantity,
                        offer.UnitSellPrice,
                        i);
                }
            }

            if (_selectedIndex >= GetRowCount())
                _selectedIndex = Mathf.Max(0, GetRowCount() - 1);
        }

        int GetRowCount() => _mode == ShopMode.Buy ? _buyRows.Count : _sellRows.Count;

        void RefreshDetail()
        {
            int rowCount = GetRowCount();
            if (rowCount == 0 || _selectedIndex < 0 || _selectedIndex >= rowCount)
            {
                string emptyBody = _mode == ShopMode.Buy
                    ? "This shop has nothing for sale."
                    : "You have nothing this shop will buy.";
                if (_inspectPane != null)
                {
                    _inspectPane.SetContent(
                        null,
                        "<size=20><b>No items</b></size>",
                        $"<color=#8a97a3>{emptyBody}</color>",
                        FontScale);
                }
                _qtyText.text = "Qty: 0 / 0";
                _totalText.text = "Total: 0 gold";
                _confirmButton.interactable = false;
                HighlightRows();
                RefreshTransactionPanel();
                return;
            }

            ItemData item;
            int unitPrice;
            if (_mode == ShopMode.Buy)
            {
                ShopStockSnapshot row = _buyRows[_selectedIndex];
                item = row.item;
                unitPrice = ShopPriceResolver.GetBuyPrice(row.item);
            }
            else
            {
                ShopSellOffer offer = _sellRows[_selectedIndex];
                item = offer.Definition;
                unitPrice = offer.UnitSellPrice;
            }

            RefreshInspectPane(item, unitPrice);
            HighlightRows();
            RefreshTransactionPanel();
        }

        void RefreshTransactionPanel()
        {
            if (_cartHeaderText != null)
                _cartHeaderText.text = _cart.Count > 0 ? $"CART ({_cart.Count})" : "CART";

            RebuildCartRows();
            HighlightCartRows();
            _totalText.text = $"Total: {GetCartTotalGold()} gold";
            _confirmButton.interactable = _cart.Count > 0;

            if (_inputFocus == InputFocus.Cart && _cart.Count > 0)
            {
                _cartSelectedIndex = Mathf.Clamp(_cartSelectedIndex, 0, _cart.Count - 1);
                ShopCartEntry entry = _cart[_cartSelectedIndex];
                int availableQty = GetAvailableQuantityForCartEntry(entry, _cartSelectedIndex);
                _qtyText.text = $"Qty: {entry.Quantity} / {availableQty + entry.Quantity}";
            }
            else if (GetRowCount() > 0)
            {
                int availableQty = GetStockOrOfferQuantity();
                int maxStaging = GetMaxStagingQuantity();
                _quantity = Mathf.Clamp(_quantity, 0, maxStaging);
                _qtyText.text = $"Qty: {_quantity} / {availableQty}";
            }
            else
            {
                _qtyText.text = "Qty: 0 / 0";
            }
        }

        void RefreshFooterHints()
        {
            if (_footerText == null)
                return;

            _footerText.text = _mode == ShopMode.Buy
                ? "↑↓ select   Tab cart   A add   D remove   ,/. qty   Enter purchase   B buy   V sell   Esc leave"
                : "↑↓ select   Tab cart   A add   D remove   ,/. qty   Enter sell   B buy   V sell   Esc leave";
        }

        void RefreshInspectPane(ItemData item, int unitPrice)
        {
            if (_inspectPane == null || item == null)
                return;

            InventoryViewModel.Row row = BuildInspectRow(item);
            var sb = new StringBuilder();
            sb.AppendLine(InventoryDetailFormatter.FormatInspectBody(item, row));
            sb.AppendLine();
            AppendShopInspectSection(sb, item, unitPrice);
            sb.AppendLine();
            sb.AppendLine(InventoryDetailFormatter.FormatCompareEquippedSameSlot(
                GetEquippedInSameSlot(item),
                row));

            string hero = InventoryDetailFormatter.FormatHeroTitle(item, row.Instance) + "\n" +
                          $"<color=#8a97a3>{InventoryDetailFormatter.FormatHeroSubtitle(item, row)}</color>";

            _inspectPane.SetContent(item.icon ?? _placeholderSprite, hero, sb.ToString(), FontScale);
        }

        InventoryViewModel.Row BuildInspectRow(ItemData item)
        {
            if (_mode == ShopMode.Sell)
            {
                ShopSellOffer offer = _sellRows[_selectedIndex];
                ItemInstance instance = offer.Instance;
                if (instance == null && offer.Kind == ShopSellKind.ManaStoneStack)
                    instance = new ItemInstance(item, offer.Quantity);

                float weight = instance != null ? instance.TotalWeight : item.weight;
                return new InventoryViewModel.Row(
                    'a',
                    instance,
                    offer.Owner,
                    offer.Owner?.DisplayName ?? string.Empty,
                    isEquipped: false,
                    equippedSlot: null,
                    carriedListIndex: offer.CarriedListIndex,
                    stackedWeight: weight);
            }

            var preview = new ItemInstance(item, 1);
            return new InventoryViewModel.Row(
                'a',
                preview,
                owner: null,
                ownerDisplayName: "Shop stock",
                isEquipped: false,
                equippedSlot: null,
                carriedListIndex: -1,
                stackedWeight: preview.TotalWeight);
        }

        void AppendShopInspectSection(StringBuilder sb, ItemData item, int unitPrice)
        {
            sb.AppendLine("<color=#cfd6dd><b>Shop</b></color>");
            if (_mode == ShopMode.Buy)
            {
                ShopStockSnapshot stock = _buyRows[_selectedIndex];
                sb.AppendLine($"<color=#8a97a3>Buy price:</color> {unitPrice} gold each");
                sb.AppendLine($"<color=#8a97a3>In stock:</color> {stock.quantity}");
                int inCart = GetCartQuantityForItem(item);
                if (inCart > 0)
                    sb.AppendLine($"<color=#8a97a3>In cart:</color> {inCart}");
                sb.AppendLine($"<color=#8a97a3>Add amount:</color> {_quantity} × {unitPrice} = {unitPrice * _quantity} gold");
                return;
            }

            ShopSellOffer offer = _sellRows[_selectedIndex];
            sb.AppendLine($"<color=#8a97a3>Sell price:</color> {unitPrice} gold each");
            if (offer.Kind == ShopSellKind.ManaStoneStack)
                sb.AppendLine($"<color=#8a97a3>Party stack:</color> tier {offer.ManaTier} × {offer.Quantity}");
            else if (offer.Owner != null)
                sb.AppendLine($"<color=#8a97a3>Owner:</color> {offer.Owner.DisplayName}");
            int sellInCart = GetCartQuantityForSellOffer(offer);
            if (sellInCart > 0)
                sb.AppendLine($"<color=#8a97a3>In cart:</color> {sellInCart}");
            sb.AppendLine($"<color=#8a97a3>Add amount:</color> {_quantity} × {unitPrice} = {unitPrice * _quantity} gold");
        }

        ItemData GetEquippedInSameSlot(ItemData item)
        {
            if (item == null)
                return null;

            IReadOnlyList<BaseActor> party = PartyManager.Instance?.partyMembers;
            if (party == null)
                return null;

            for (int i = 0; i < party.Count; i++)
            {
                BaseActor member = party[i];
                if (member == null)
                    continue;

                EquipmentManager equipment = member.GetComponent<EquipmentManager>();
                if (equipment == null)
                    continue;

                ItemInstance equipped = equipment.GetEquippedInstance(item.slotType);
                if (equipped?.Definition != null)
                    return equipped.Definition;
            }

            return null;
        }

        int GetStockOrOfferQuantity()
        {
            if (_mode == ShopMode.Buy)
                return _buyRows[_selectedIndex].quantity;

            return _sellRows[_selectedIndex].Quantity;
        }

        int GetMaxStagingQuantity()
        {
            if (GetRowCount() == 0)
                return 0;

            if (_mode == ShopMode.Buy)
                return GetMaxCartQuantityForBuyItem(_buyRows[_selectedIndex].item);

            return GetMaxCartQuantityForSellOffer(_sellRows[_selectedIndex]);
        }

        int GetCartQuantityForItem(ItemData item, int excludeCartIndex = -1)
        {
            if (item == null)
                return 0;

            int total = 0;
            for (int i = 0; i < _cart.Count; i++)
            {
                if (i == excludeCartIndex)
                    continue;

                ShopCartEntry entry = _cart[i];
                if (entry?.Mode == ShopMode.Buy && entry.Item == item)
                    total += entry.Quantity;
            }

            return total;
        }

        int GetCartQuantityForSellOffer(ShopSellOffer offer, int excludeCartIndex = -1)
        {
            if (offer == null)
                return 0;

            int total = 0;
            for (int i = 0; i < _cart.Count; i++)
            {
                if (i == excludeCartIndex)
                    continue;

                ShopCartEntry entry = _cart[i];
                if (entry?.Mode == ShopMode.Sell && entry.MatchesSellOffer(offer))
                    total += entry.Quantity;
            }

            return total;
        }

        int GetCartTotalGold(int excludeCartIndex = -1)
        {
            int total = 0;
            for (int i = 0; i < _cart.Count; i++)
            {
                if (i == excludeCartIndex)
                    continue;

                ShopCartEntry entry = _cart[i];
                if (entry?.Item == null)
                    continue;

                total += entry.UnitPrice * entry.Quantity;
            }

            return total;
        }

        int GetMaxCartQuantityForBuyItem(ItemData item, int excludeCartIndex = -1)
        {
            if (item == null || _snapshot == null)
                return 0;

            int stock = TownShopStateService.GetStockQuantity(_snapshot.stock, item);
            int inCart = GetCartQuantityForItem(item, excludeCartIndex);
            int available = stock - inCart;
            int unit = ShopPriceResolver.GetBuyPrice(item);
            if (unit <= 0)
                return Mathf.Max(0, available);

            int goldLeft = ShopGoldUtility.GetPartyGoldTotal() - GetCartTotalGold(excludeCartIndex);
            int byGold = goldLeft / unit;
            return Mathf.Max(0, Mathf.Min(available, byGold));
        }

        int GetMaxCartQuantityForSellOffer(ShopSellOffer offer, int excludeCartIndex = -1)
        {
            if (offer == null || _snapshot == null)
                return 0;

            int inCart = GetCartQuantityForSellOffer(offer, excludeCartIndex);
            int available = offer.Quantity - inCart;
            if (offer.UnitSellPrice <= 0)
                return Mathf.Max(0, available);

            int goldLeft = _snapshot.goldOnHand - GetCartTotalGold(excludeCartIndex);
            int byGold = goldLeft / offer.UnitSellPrice;
            return Mathf.Max(0, Mathf.Min(available, byGold));
        }

        int GetAvailableQuantityForCartEntry(ShopCartEntry entry, int cartIndex)
        {
            if (entry == null)
                return 0;

            if (entry.Mode == ShopMode.Buy)
                return GetMaxCartQuantityForBuyItem(entry.Item, cartIndex);

            return GetMaxCartQuantityForSellOffer(entry.ToSellOffer(), cartIndex);
        }

        int GetMaxCartQuantityForEntry(ShopCartEntry entry, int excludeCartIndex)
        {
            if (entry == null)
                return 0;

            if (entry.Mode == ShopMode.Buy)
                return GetMaxCartQuantityForBuyItem(entry.Item, excludeCartIndex);

            return GetMaxCartQuantityForSellOffer(entry.ToSellOffer(), excludeCartIndex);
        }

        int FindCartIndexForItem(ItemData item)
        {
            for (int i = 0; i < _cart.Count; i++)
            {
                if (_cart[i]?.Mode == ShopMode.Buy && _cart[i].Item == item)
                    return i;
            }

            return -1;
        }

        int FindCartIndexForSellOffer(ShopSellOffer offer)
        {
            for (int i = 0; i < _cart.Count; i++)
            {
                if (_cart[i]?.MatchesSellOffer(offer) == true)
                    return i;
            }

            return -1;
        }

        void ToggleInputFocus()
        {
            _inputFocus = _inputFocus == InputFocus.Stock ? InputFocus.Cart : InputFocus.Stock;
            if (_inputFocus == InputFocus.Cart && _cart.Count == 0)
                _inputFocus = InputFocus.Stock;

            RefreshTransactionPanel();
        }

        void MoveCartSelection(int delta)
        {
            if (_cart.Count == 0)
            {
                _inputFocus = InputFocus.Stock;
                RefreshTransactionPanel();
                return;
            }

            _cartSelectedIndex = (_cartSelectedIndex + delta + _cart.Count) % _cart.Count;
            RefreshTransactionPanel();
        }

        void AddSelectedToCart()
        {
            if (GetRowCount() == 0)
                return;

            int addQty = _quantity > 0 ? _quantity : 1;
            int existingIndex;
            string displayName;

            if (_mode == ShopMode.Buy)
            {
                ItemData item = _buyRows[_selectedIndex].item;
                int maxAdd = GetMaxCartQuantityForBuyItem(item);
                if (maxAdd <= 0)
                {
                    _messageText.text = "Cannot add more of that item.";
                    return;
                }

                addQty = Mathf.Min(addQty, maxAdd);
                existingIndex = FindCartIndexForItem(item);
                if (existingIndex >= 0)
                    _cart[existingIndex].Quantity += addQty;
                else
                {
                    _cart.Add(ShopCartEntry.ForBuy(item, addQty));
                    _cartSelectedIndex = _cart.Count - 1;
                }

                displayName = item.itemName;
            }
            else
            {
                ShopSellOffer offer = _sellRows[_selectedIndex];
                int maxAdd = GetMaxCartQuantityForSellOffer(offer);
                if (maxAdd <= 0)
                {
                    _messageText.text = "Cannot add more of that item.";
                    return;
                }

                addQty = Mathf.Min(addQty, maxAdd);
                existingIndex = FindCartIndexForSellOffer(offer);
                if (existingIndex >= 0)
                    _cart[existingIndex].Quantity += addQty;
                else
                {
                    _cart.Add(ShopCartEntry.ForSell(offer, addQty));
                    _cartSelectedIndex = _cart.Count - 1;
                }

                displayName = offer.DisplayName;
            }

            _quantity = 0;
            _messageText.text = $"Added {addQty} × {displayName} to cart.";
            RefreshAll();
        }

        void RemoveFromCartSelection()
        {
            if (_inputFocus == InputFocus.Cart && _cart.Count > 0)
            {
                AdjustCartLineQuantity(_cartSelectedIndex, -1);
                return;
            }

            if (GetRowCount() == 0)
                return;

            if (_mode == ShopMode.Buy)
            {
                int cartIndex = FindCartIndexForItem(_buyRows[_selectedIndex].item);
                if (cartIndex >= 0)
                    AdjustCartLineQuantity(cartIndex, -1);
                return;
            }

            int sellCartIndex = FindCartIndexForSellOffer(_sellRows[_selectedIndex]);
            if (sellCartIndex >= 0)
                AdjustCartLineQuantity(sellCartIndex, -1);
        }

        void RemoveCartLineAt(int cartIndex)
        {
            if (cartIndex < 0 || cartIndex >= _cart.Count)
                return;

            _cart.RemoveAt(cartIndex);
            if (_cart.Count == 0)
                _inputFocus = InputFocus.Stock;
            else
                _cartSelectedIndex = Mathf.Clamp(_cartSelectedIndex, 0, _cart.Count - 1);

            RefreshAll();
        }

        void AdjustCartLineQuantity(int cartIndex, int delta)
        {
            if (cartIndex < 0 || cartIndex >= _cart.Count)
                return;

            ShopCartEntry entry = _cart[cartIndex];
            if (entry?.Item == null)
                return;

            if (delta > 0)
            {
                int maxQty = GetMaxCartQuantityForEntry(entry, cartIndex) + entry.Quantity;
                entry.Quantity = Mathf.Min(entry.Quantity + delta, maxQty);
                RefreshAll();
            }
            else
            {
                entry.Quantity += delta;
                if (entry.Quantity <= 0)
                    RemoveCartLineAt(cartIndex);
                else
                    RefreshAll();
            }
        }

        void ConfirmTransaction()
        {
            if (_snapshot == null)
                return;

            if (_mode == ShopMode.Buy)
                ConfirmBuyCart();
            else
                ConfirmSellCart();
        }

        void ConfirmBuyCart()
        {
            if (_cart.Count == 0)
            {
                _messageText.text = "Cart is empty.";
                return;
            }

            _purchaseScratch.Clear();
            for (int i = 0; i < _cart.Count; i++)
            {
                ShopCartEntry entry = _cart[i];
                if (entry?.Item == null || entry.Quantity <= 0 || entry.Mode != ShopMode.Buy)
                    continue;

                _purchaseScratch.Add(new ShopPurchaseLine(entry.Item, entry.Quantity));
            }

            ShopTransactionResult result =
                ShopTransactionService.TryBuyBatch(_snapshot, _purchaseScratch, out string message);
            _messageText.text = message;
            if (result == ShopTransactionResult.Success)
            {
                TownShopStateService.Instance.SaveSnapshot(_snapshot);
                _cart.Clear();
                _cartSelectedIndex = 0;
                _inputFocus = InputFocus.Stock;
                _quantity = 0;
            }

            RefreshAll();
        }

        void ConfirmSellCart()
        {
            if (_cart.Count == 0)
            {
                _messageText.text = "Cart is empty.";
                return;
            }

            _sellScratch.Clear();
            for (int i = 0; i < _cart.Count; i++)
            {
                ShopCartEntry entry = _cart[i];
                if (entry?.Item == null || entry.Quantity <= 0 || entry.Mode != ShopMode.Sell)
                    continue;

                _sellScratch.Add(new ShopSellLine(entry.ToSellOffer(), entry.Quantity));
            }

            ShopTransactionResult result =
                ShopTransactionService.TrySellBatch(_snapshot, _sellScratch, out string message);
            _messageText.text = message;
            if (result == ShopTransactionResult.Success)
            {
                TownShopStateService.Instance.SaveSnapshot(_snapshot);
                _cart.Clear();
                _cartSelectedIndex = 0;
                _inputFocus = InputFocus.Stock;
                _quantity = 0;
            }

            RefreshAll();
        }

        void MoveSelection(int delta)
        {
            if (GetRowCount() == 0)
                return;

            _inputFocus = InputFocus.Stock;
            _selectedIndex = (_selectedIndex + delta + GetRowCount()) % GetRowCount();
            _quantity = 0;
            RefreshDetail();
        }

        void AdjustQuantity(int delta)
        {
            if (_inputFocus == InputFocus.Cart && _cart.Count > 0)
            {
                AdjustCartLineQuantity(_cartSelectedIndex, delta);
                return;
            }

            if (GetRowCount() == 0)
                return;

            int maxStaging = GetMaxStagingQuantity();
            _quantity = Mathf.Clamp(_quantity + delta, 0, maxStaging);
            RefreshDetail();
        }

        void ClearRows()
        {
            for (int i = 0; i < _rowObjects.Count; i++)
            {
                if (_rowObjects[i] != null)
                    Destroy(_rowObjects[i]);
            }

            _rowObjects.Clear();
        }

        void ClearCartRows()
        {
            for (int i = 0; i < _cartRowObjects.Count; i++)
            {
                if (_cartRowObjects[i] != null)
                    Destroy(_cartRowObjects[i]);
            }

            _cartRowObjects.Clear();
        }

        void RebuildCartRows()
        {
            if (_cartContent == null)
                return;

            ClearCartRows();
            if (_cart.Count == 0)
            {
                CreateCartPlaceholderRow();
                return;
            }

            for (int i = 0; i < _cart.Count; i++)
                CreateCartRow(_cart[i], i);
        }

        void CreateCartPlaceholderRow()
        {
            var go = new GameObject("CartEmpty", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(_cartContent, false);
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = ScaledFont(40f);
            le.preferredHeight = ScaledFont(40f);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = ScaledFont(16f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.62f, 0.68f, 0.74f, 1f);
            tmp.text = _mode == ShopMode.Buy
                ? "Cart is empty — select stock and press A to add."
                : "Cart is empty — select items and press A to add.";
            _cartRowObjects.Add(go);
        }

        void CreateCartRow(ShopCartEntry entry, int index)
        {
            if (entry?.Item == null)
                return;

            EnsurePlaceholderSprite();
            int unitPrice = entry.UnitPrice;
            int lineTotal = unitPrice * entry.Quantity;
            string itemLabel = entry.Item.itemName;
            if (entry.Mode == ShopMode.Sell && entry.SellOwner != null)
                itemLabel = $"{itemLabel} ({entry.SellOwner.DisplayName})";

            var go = new GameObject($"CartRow_{index}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(_cartContent, false);
            LayoutElement rowLe = go.GetComponent<LayoutElement>();
            rowLe.minHeight = ScaledFont(48f);
            rowLe.preferredHeight = ScaledFont(48f);

            HorizontalLayoutGroup rowLayout = go.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 4, 4);
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            Button button = go.GetComponent<Button>();
            int captured = index;
            button.onClick.AddListener(() =>
            {
                _inputFocus = InputFocus.Cart;
                _cartSelectedIndex = captured;
                RefreshTransactionPanel();
            });

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            LayoutElement iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = ScaledFont(32f);
            iconLe.minHeight = iconLe.preferredHeight = ScaledFont(32f);
            Image icon = iconGo.GetComponent<Image>();
            Sprite sprite = entry.Item.icon != null ? entry.Item.icon : _placeholderSprite;
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.color = sprite != null ? Color.white : new Color(0.45f, 0.45f, 0.48f, 0.9f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelGo.transform.SetParent(go.transform, false);
            LayoutElement labelLe = labelGo.GetComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            labelLe.minWidth = 120f;
            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.fontSize = ScaledFont(16f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.text = $"{itemLabel}  ×{entry.Quantity}  @ {unitPrice}g  = {lineTotal}g";

            var removeButton = CreateActionButton(go.transform, "Remove", "×", () =>
            {
                _inputFocus = InputFocus.Cart;
                _cartSelectedIndex = captured;
                RemoveCartLineAt(captured);
            });
            LayoutElement removeLe = removeButton.gameObject.AddComponent<LayoutElement>();
            removeLe.minWidth = ScaledFont(36f);
            removeLe.preferredWidth = ScaledFont(36f);

            _cartRowObjects.Add(go);
        }

        void HighlightCartRows()
        {
            for (int i = 0; i < _cartRowObjects.Count; i++)
            {
                Image bg = _cartRowObjects[i].GetComponent<Image>();
                if (bg == null)
                    continue;

                bool selected = _inputFocus == InputFocus.Cart
                    && _cart.Count > 0
                    && i == _cartSelectedIndex;
                bg.color = selected
                    ? new Color(0.28f, 0.42f, 0.58f, 0.98f)
                    : new Color(0.12f, 0.14f, 0.18f, 0.95f);
            }
        }

        void CreateRow(ItemData item, string name, int qty, int unitPrice, int index)
        {
            EnsurePlaceholderSprite();

            var go = new GameObject($"Row_{index}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(_listContent, false);
            RectTransform rt = (RectTransform)go.transform;
            LayoutElement rowLe = go.GetComponent<LayoutElement>();
            rowLe.minHeight = ScaledFont(56f);
            rowLe.preferredHeight = ScaledFont(56f);

            HorizontalLayoutGroup rowLayout = go.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 8, 6, 6);
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            Button button = go.GetComponent<Button>();
            int captured = index;
            button.onClick.AddListener(() =>
            {
                _inputFocus = InputFocus.Stock;
                _selectedIndex = captured;
                _quantity = 0;
                RefreshDetail();
            });

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            LayoutElement iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = ScaledFont(40f);
            iconLe.minHeight = iconLe.preferredHeight = ScaledFont(40f);
            Image icon = iconGo.GetComponent<Image>();
            Sprite sprite = item != null && item.icon != null ? item.icon : _placeholderSprite;
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.color = sprite != null ? Color.white : new Color(0.45f, 0.45f, 0.48f, 0.9f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelGo.transform.SetParent(go.transform, false);
            LayoutElement labelLe = labelGo.GetComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            labelLe.minWidth = 120f;
            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.fontSize = ScaledFont(18f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            int inCart = _mode == ShopMode.Buy
                ? GetCartQuantityForItem(item)
                : GetCartQuantityForSellOffer(_sellRows[index]);
            string cartSuffix = inCart > 0 ? $"  [cart:{inCart}]" : string.Empty;
            label.text = $"{name}  ×{qty}  @ {unitPrice}g{cartSuffix}";

            _rowObjects.Add(go);
        }

        void HighlightRows()
        {
            for (int i = 0; i < _rowObjects.Count; i++)
            {
                Image bg = _rowObjects[i].GetComponent<Image>();
                if (bg == null)
                    continue;

                bg.color = i == _selectedIndex
                    ? new Color(0.28f, 0.42f, 0.58f, 0.98f)
                    : new Color(0.12f, 0.14f, 0.18f, 0.95f);
            }
        }

        static void SetTabHighlight(Button button, bool active)
        {
            if (button == null)
                return;

            Image bg = button.GetComponent<Image>();
            if (bg != null)
                bg.color = active
                    ? new Color(0.35f, 0.55f, 0.75f, 0.95f)
                    : new Color(0.18f, 0.2f, 0.26f, 0.95f);
        }

        void EnsureBuilt()
        {
            if (_root != null && _layoutVersion == LayoutVersion)
                return;

            if (_root != null)
            {
                Destroy(_root.transform.parent.gameObject);
                _root = null;
            }

            _layoutVersion = LayoutVersion;
            EnsurePlaceholderSprite();

            var canvasGo = new GameObject("ShopNpcCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 520;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _root = CreatePanel(canvasGo.transform, "ShopRoot", new Color(0.06f, 0.08f, 0.11f, 0.98f));
            RectTransform rootRt = (RectTransform)_root.transform;
            StretchFull(rootRt, OuterMargin);

            var headerFrame = CreateBorderedPanel(_root.transform, "Header", HeaderFillColor, out Transform headerContent);
            RectTransform headerRt = (RectTransform)headerFrame.transform;
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 96f);
            headerRt.anchoredPosition = Vector2.zero;
            Transform header = headerContent;

            _headerText = CreateText(header, "ShopTitle", ScaledFont(24f), FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            SetAnchored(
                (RectTransform)_headerText.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0.45f, 1f),
                new Vector2(16f, 8f),
                new Vector2(-8f, -8f));

            _playerGoldText = CreateText(header, "PlayerGold", ScaledFont(18f), FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            SetAnchored(
                (RectTransform)_playerGoldText.transform,
                new Vector2(0.45f, 0.5f),
                new Vector2(0.72f, 1f),
                new Vector2(8f, 8f),
                new Vector2(-8f, -8f));

            _shopGoldText = CreateText(header, "ShopGold", ScaledFont(18f), FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            SetAnchored(
                (RectTransform)_shopGoldText.transform,
                new Vector2(0.72f, 0.5f),
                new Vector2(1f, 1f),
                new Vector2(8f, 8f),
                new Vector2(-16f, -8f));

            _buyTabButton = CreateTabButton(header, "BuyTab", "Buy", () => SetMode(ShopMode.Buy));
            SetAnchored(
                (RectTransform)_buyTabButton.transform,
                new Vector2(0.02f, 0f),
                new Vector2(0.12f, 0.45f),
                new Vector2(0f, 4f),
                new Vector2(0f, -4f));

            _sellTabButton = CreateTabButton(header, "SellTab", "Sell", () => SetMode(ShopMode.Sell));
            SetAnchored(
                (RectTransform)_sellTabButton.transform,
                new Vector2(0.13f, 0f),
                new Vector2(0.23f, 0.45f),
                new Vector2(0f, 4f),
                new Vector2(0f, -4f));

            var body = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup));
            body.transform.SetParent(_root.transform, false);
            RectTransform bodyRt = (RectTransform)body.transform;
            SetAnchored(bodyRt, Vector2.zero, Vector2.one, new Vector2(12f, 56f), new Vector2(-12f, -96f));

            VerticalLayoutGroup bodyLayout = body.GetComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 8f;
            bodyLayout.padding = new RectOffset(0, 0, 0, 0);
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;

            var bodyColumns = new GameObject("BodyColumns", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            bodyColumns.transform.SetParent(body.transform, false);
            LayoutElement bodyColumnsLe = bodyColumns.GetComponent<LayoutElement>();
            bodyColumnsLe.flexibleHeight = 1f;
            bodyColumnsLe.flexibleWidth = 1f;
            bodyColumnsLe.minHeight = 280f;

            HorizontalLayoutGroup bodyColumnsLayout = bodyColumns.GetComponent<HorizontalLayoutGroup>();
            bodyColumnsLayout.spacing = 12f;
            bodyColumnsLayout.padding = new RectOffset(0, 0, 0, 0);
            bodyColumnsLayout.childControlWidth = true;
            bodyColumnsLayout.childControlHeight = true;
            bodyColumnsLayout.childForceExpandWidth = true;
            bodyColumnsLayout.childForceExpandHeight = true;
            bodyColumnsLayout.childAlignment = TextAnchor.UpperLeft;

            var listCol = new GameObject("ListColumn", typeof(RectTransform), typeof(LayoutElement));
            listCol.transform.SetParent(bodyColumns.transform, false);
            LayoutElement listColLe = listCol.GetComponent<LayoutElement>();
            listColLe.flexibleWidth = 1f;
            listColLe.flexibleHeight = 1f;
            listColLe.minWidth = 420f;

            GameObject listPanelFrame = CreateBorderedPanel(listCol.transform, "ListPanel", PanelFillColor, out Transform listPanelContent);
            StretchFull((RectTransform)listPanelFrame.transform);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(listPanelContent, false);
            StretchFull((RectTransform)scrollGo.transform, new Vector2(12f, 8f), new Vector2(-8f, -8f));
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            RectTransform viewportRt = (RectTransform)viewport.transform;
            StretchFull(viewportRt);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewport.transform, false);
            _listContent = (RectTransform)contentGo.transform;
            _listContent.anchorMin = new Vector2(0f, 1f);
            _listContent.anchorMax = new Vector2(1f, 1f);
            _listContent.pivot = new Vector2(0f, 1f);
            _listContent.anchoredPosition = Vector2.zero;
            _listContent.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = _listContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var detailCol = new GameObject("DetailColumn", typeof(RectTransform), typeof(LayoutElement));
            detailCol.transform.SetParent(bodyColumns.transform, false);
            LayoutElement detailColLe = detailCol.GetComponent<LayoutElement>();
            detailColLe.flexibleWidth = 1f;
            detailColLe.flexibleHeight = 1f;
            detailColLe.minWidth = 420f;

            GameObject detailPanelFrame = CreateBorderedPanel(detailCol.transform, "DetailPanel", PanelFillColor, out Transform detailPanelContent);
            StretchFull((RectTransform)detailPanelFrame.transform);

            VerticalLayoutGroup detailLayout = detailPanelContent.gameObject.AddComponent<VerticalLayoutGroup>();
            detailLayout.padding = new RectOffset(8, 8, 8, 8);
            detailLayout.spacing = 0;
            detailLayout.childControlWidth = true;
            detailLayout.childControlHeight = true;
            detailLayout.childForceExpandWidth = true;
            detailLayout.childForceExpandHeight = true;
            detailLayout.childAlignment = TextAnchor.UpperLeft;

            var inspectHost = new GameObject("InspectHost", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            inspectHost.transform.SetParent(detailPanelContent, false);
            LayoutElement inspectHostLe = inspectHost.GetComponent<LayoutElement>();
            inspectHostLe.flexibleHeight = 1f;
            inspectHostLe.flexibleWidth = 1f;
            inspectHostLe.minHeight = 220f;
            VerticalLayoutGroup inspectHostLayout = inspectHost.GetComponent<VerticalLayoutGroup>();
            inspectHostLayout.childControlWidth = true;
            inspectHostLayout.childControlHeight = true;
            inspectHostLayout.childForceExpandWidth = true;
            inspectHostLayout.childForceExpandHeight = true;
            inspectHostLayout.childAlignment = TextAnchor.UpperLeft;
            _inspectPane = InventoryInspectPaneView.Create(inspectHost.transform, _placeholderSprite);

            GameObject transactionFrame = CreateBorderedPanel(body.transform, "TransactionPanel", TransactionFillColor, out Transform transactionContent);
            LayoutElement transactionLe = transactionFrame.AddComponent<LayoutElement>();
            transactionLe.minHeight = ScaledFont(210f);
            transactionLe.preferredHeight = ScaledFont(210f);
            transactionLe.flexibleHeight = 0f;

            HorizontalLayoutGroup transactionLayout = transactionContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            transactionLayout.padding = new RectOffset(12, 12, 10, 10);
            transactionLayout.spacing = 12;
            transactionLayout.childControlWidth = true;
            transactionLayout.childControlHeight = true;
            transactionLayout.childForceExpandWidth = true;
            transactionLayout.childForceExpandHeight = true;
            transactionLayout.childAlignment = TextAnchor.UpperLeft;

            _cartColumn = new GameObject("CartColumn", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            _cartColumn.transform.SetParent(transactionContent, false);
            LayoutElement cartColLe = _cartColumn.GetComponent<LayoutElement>();
            cartColLe.flexibleWidth = 1f;
            cartColLe.flexibleHeight = 1f;
            cartColLe.minWidth = 420f;
            VerticalLayoutGroup cartColLayout = _cartColumn.GetComponent<VerticalLayoutGroup>();
            cartColLayout.spacing = 6;
            cartColLayout.childControlWidth = true;
            cartColLayout.childControlHeight = true;
            cartColLayout.childForceExpandWidth = true;
            cartColLayout.childForceExpandHeight = true;
            cartColLayout.childAlignment = TextAnchor.UpperLeft;

            _cartHeaderText = CreateText(_cartColumn.transform, "CartHeader", ScaledFont(16f), FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            _cartHeaderText.gameObject.AddComponent<LayoutElement>().preferredHeight = ScaledFont(22f);
            _cartHeaderText.text = "CART";

            var cartScrollGo = new GameObject("CartScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(LayoutElement));
            cartScrollGo.transform.SetParent(_cartColumn.transform, false);
            LayoutElement cartScrollLe = cartScrollGo.GetComponent<LayoutElement>();
            cartScrollLe.flexibleHeight = 1f;
            cartScrollLe.flexibleWidth = 1f;
            cartScrollLe.minHeight = ScaledFont(72f);
            cartScrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);

            var cartViewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            cartViewport.transform.SetParent(cartScrollGo.transform, false);
            RectTransform cartViewportRt = (RectTransform)cartViewport.transform;
            StretchFull(cartViewportRt);

            var cartContentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            cartContentGo.transform.SetParent(cartViewport.transform, false);
            _cartContent = (RectTransform)cartContentGo.transform;
            _cartContent.anchorMin = new Vector2(0f, 1f);
            _cartContent.anchorMax = new Vector2(1f, 1f);
            _cartContent.pivot = new Vector2(0f, 1f);
            _cartContent.anchoredPosition = Vector2.zero;
            _cartContent.sizeDelta = Vector2.zero;
            VerticalLayoutGroup cartVlg = cartContentGo.GetComponent<VerticalLayoutGroup>();
            cartVlg.padding = new RectOffset(4, 4, 4, 4);
            cartVlg.spacing = 4f;
            cartVlg.childAlignment = TextAnchor.UpperLeft;
            cartVlg.childControlWidth = true;
            cartVlg.childForceExpandWidth = true;
            cartVlg.childControlHeight = true;
            cartVlg.childForceExpandHeight = false;
            ContentSizeFitter cartFitter = cartContentGo.GetComponent<ContentSizeFitter>();
            cartFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cartFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect cartScroll = cartScrollGo.GetComponent<ScrollRect>();
            cartScroll.viewport = cartViewportRt;
            cartScroll.content = _cartContent;
            cartScroll.horizontal = false;
            cartScroll.vertical = true;
            cartScroll.movementType = ScrollRect.MovementType.Clamped;

            var summaryCol = new GameObject("SummaryColumn", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            summaryCol.transform.SetParent(transactionContent, false);
            LayoutElement summaryLe = summaryCol.GetComponent<LayoutElement>();
            summaryLe.flexibleWidth = 0.42f;
            summaryLe.minWidth = 320f;
            summaryLe.flexibleHeight = 1f;
            VerticalLayoutGroup summaryLayout = summaryCol.GetComponent<VerticalLayoutGroup>();
            summaryLayout.padding = new RectOffset(8, 0, 0, 0);
            summaryLayout.spacing = 8;
            summaryLayout.childControlWidth = true;
            summaryLayout.childControlHeight = true;
            summaryLayout.childForceExpandWidth = true;
            summaryLayout.childForceExpandHeight = false;
            summaryLayout.childAlignment = TextAnchor.UpperLeft;

            _qtyText = CreateFooterLine(summaryCol.transform, "Qty", ScaledFont(18f), FontStyles.Normal);
            _totalText = CreateFooterLine(summaryCol.transform, "Total", ScaledFont(20f), FontStyles.Bold);

            var buttonRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(summaryCol.transform, false);
            LayoutElement buttonRowLe = buttonRow.AddComponent<LayoutElement>();
            buttonRowLe.minHeight = ScaledFont(44f);
            buttonRowLe.preferredHeight = ScaledFont(44f);
            HorizontalLayoutGroup buttonRowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonRowLayout.spacing = 10;
            buttonRowLayout.childControlWidth = true;
            buttonRowLayout.childControlHeight = true;
            buttonRowLayout.childForceExpandWidth = true;
            buttonRowLayout.childForceExpandHeight = true;

            _confirmButton = CreateActionButton(buttonRow.transform, "Confirm", "Confirm", ConfirmTransaction);
            LayoutElement confirmLe = _confirmButton.gameObject.AddComponent<LayoutElement>();
            confirmLe.flexibleWidth = 0.35f;
            confirmLe.minWidth = 160f;

            _messageText = CreateText(buttonRow.transform, "Message", ScaledFont(16f), FontStyles.Italic, TextAlignmentOptions.TopLeft);
            _messageText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0.65f;
            _messageText.color = new Color(0.75f, 0.82f, 0.88f, 1f);
            _messageText.textWrappingMode = TextWrappingModes.Normal;
            _messageText.overflowMode = TextOverflowModes.Ellipsis;

            _footerText = CreateText(_root.transform, "Footer", ScaledFont(15f), FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            RectTransform footerRt = (RectTransform)_footerText.transform;
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0f, 0f);
            footerRt.sizeDelta = new Vector2(0f, ScaledFont(32f));
            footerRt.anchoredPosition = new Vector2(16f, 12f);
            _footerText.color = new Color(0.7f, 0.74f, 0.78f, 1f);
            RefreshFooterHints();

            _root.SetActive(false);
        }

        static GameObject CreateBorderedPanel(Transform parent, string name, Color fillColor, out Transform contentRoot)
        {
            var frame = new GameObject(name, typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(parent, false);
            frame.GetComponent<Image>().color = PanelBorderColor;

            var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(frame.transform, false);
            inner.GetComponent<Image>().color = fillColor;
            StretchFull((RectTransform)inner.transform, PanelBorderWidth);

            contentRoot = inner.transform;
            return frame;
        }

        static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            float size,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        static Button CreateTabButton(Transform parent, string name, string label, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.2f, 0.26f, 0.95f);
            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            StretchFull((RectTransform)textGo.transform, 4f);
            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = ScaledFont(16f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return button;
        }

        static Button CreateActionButton(Transform parent, string name, string label, Action onClick)
        {
            Button button = CreateTabButton(parent, name, label, onClick);
            return button;
        }

        static float ScaledFont(float size) => size * FontScale;

        void EnsurePlaceholderSprite()
        {
            if (_placeholderSprite != null)
                return;

            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.35f, 0.38f, 0.42f, 1f));
            tex.Apply();
            _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        static TextMeshProUGUI CreateFooterLine(Transform parent, string name, float size, FontStyles style)
        {
            TextMeshProUGUI tmp = CreateText(parent, name, size, style, TextAlignmentOptions.MidlineLeft);
            LayoutElement le = tmp.gameObject.AddComponent<LayoutElement>();
            le.minHeight = size * 1.35f;
            le.preferredHeight = size * 1.35f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        static void StretchFull(RectTransform rt) =>
            StretchFull(rt, Vector2.zero, Vector2.zero);

        static void StretchFull(RectTransform rt, float inset) =>
            StretchFull(rt, new Vector2(inset, inset), new Vector2(-inset, -inset));

        static void StretchFull(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        static void SetAnchored(
            RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
