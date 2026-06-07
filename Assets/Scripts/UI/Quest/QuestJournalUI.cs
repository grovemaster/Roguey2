using System.Collections.Generic;
using System.Text;
using JRogue.Item;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JRogue.UI.Quest
{
    public sealed class QuestJournalUI : MonoBehaviour
    {
        enum JournalTab
        {
            Active = 0,
            Completed = 1,
            Failed = 2,
        }

        static QuestJournalUI _instance;

        static readonly Color PanelBackgroundColor = new Color(0.08f, 0.085f, 0.095f, 0.96f);
        static readonly Color RowNormalTint = new Color(0.16f, 0.166f, 0.177f, 0.94f);
        static readonly Color RowSelectedTint = new Color(0.22f, 0.285f, 0.34f, 0.96f);
        static readonly Color ActiveBorderColor = new Color(0.784f, 0.627f, 0.376f, 1f);
        static readonly Color ReadyBorderColor = new Color(0.95f, 0.82f, 0.35f, 1f);
        static readonly Color CompletedBorderColor = new Color(0.29f, 0.541f, 0.353f, 1f);
        static readonly Color FailedBorderColor = new Color(0.541f, 0.29f, 0.29f, 1f);
        static readonly Color AccentTextColor = new Color(0.95f, 0.82f, 0.35f, 1f);

        GameObject _panelRoot;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _detailText;
        TextMeshProUGUI _hintText;
        RectTransform _listContent;
        Outline _detailBorder;
        readonly List<TabButtonChrome> _tabButtons = new List<TabButtonChrome>();
        readonly List<GameObject> _rowObjects = new List<GameObject>();

        sealed class TabButtonChrome
        {
            public JournalTab Tab;
            public Image Background;
        }

        JournalTab _tab = JournalTab.Active;
        int _selectedIndex = -1;
        bool _open;

        public static QuestJournalUI Instance => _instance;

        public static bool BlocksGameplay =>
            _instance != null && _instance._open;

        public static void ForceCloseIfOpen()
        {
            if (_instance != null && _instance._open)
                _instance.Close();
        }

        public static void TogglePanelFromGameplayInput()
        {
            if (_instance == null)
                return;

            if (GameOverService.IsGameOver)
                return;

            if (_instance._open)
                _instance.Close();
            else
                _instance.Open();
        }

        public static QuestJournalUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(QuestJournalUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<QuestJournalUI>();
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
            EnsurePanelBuilt();
            Close();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void OnEnable()
        {
            QuestService quests = QuestService.Instance;
            if (quests != null)
                quests.Changed += RefreshDisplay;
        }

        void OnDisable()
        {
            QuestService quests = QuestService.Instance;
            if (quests != null)
                quests.Changed -= RefreshDisplay;
        }

        void Update()
        {
            if (!_open || Keyboard.current == null)
                return;

            Keyboard kb = Keyboard.current;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (kb.pKey.wasPressedThisFrame)
                TogglePinSelected();
        }

        public void Open()
        {
            InventoryUI.ForceCloseIfOpen();
            JRogue.UI.Racial.RacialAbilitiesUI.ForceCloseIfOpen();

            EnsurePanelBuilt();
            _open = true;
            _panelRoot.SetActive(true);
            RefreshDisplay();
        }

        public void Close()
        {
            _open = false;
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        void TogglePinSelected()
        {
            QuestService quests = QuestService.Instance;
            if (quests == null)
                return;

            IReadOnlyList<QuestInstance> entries = GetEntriesForTab(_tab);
            if (_selectedIndex < 0 || _selectedIndex >= entries.Count)
                return;

            QuestInstance selected = entries[_selectedIndex];
            if (selected.isPinned)
                quests.UnpinQuest(selected.questId);
            else
                quests.PinQuest(selected.questId);
        }

        void RefreshDisplay()
        {
            if (!_open)
                return;

            IReadOnlyList<QuestInstance> entries = GetEntriesForTab(_tab);
            if (_selectedIndex >= entries.Count)
                _selectedIndex = entries.Count - 1;

            RebuildList(entries);
            UpdateDetail(entries);
            UpdateHint();
            UpdateTabButtonStyles();
        }

        IReadOnlyList<QuestInstance> GetEntriesForTab(JournalTab tab)
        {
            QuestService quests = QuestService.Instance;
            if (quests == null)
                return System.Array.Empty<QuestInstance>();

            switch (tab)
            {
                case JournalTab.Completed:
                    return quests.GetCompletedQuests();
                case JournalTab.Failed:
                    return quests.GetFailedQuests();
                default:
                    return quests.GetActiveQuests();
            }
        }

        void RebuildList(IReadOnlyList<QuestInstance> entries)
        {
            ClearRows();
            if (_listContent == null)
                return;

            if (entries.Count == 0)
            {
                CreateRowLabel(GetEmptyMessage(_tab), RowNormalTint, ActiveBorderColor, -1, false);
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                QuestInstance entry = entries[i];
                QuestDefinition definition = QuestService.Instance?.GetDefinition(entry.questId);
                string title = definition != null ? definition.displayTitle : entry.questId;
                if (entry.isPinned)
                    title = $"📌 {title}";
                else if (entry.isNew)
                    title = $"• {title}";

                Color border = ResolveBorderColor(entry.state);
                Color rowTint = i == _selectedIndex ? RowSelectedTint : RowNormalTint;
                CreateRowLabel(title, rowTint, border, i, true);
            }
        }

        void UpdateDetail(IReadOnlyList<QuestInstance> entries)
        {
            if (_detailText == null || _detailBorder == null)
                return;

            if (_selectedIndex < 0 || _selectedIndex >= entries.Count)
            {
                _detailText.text = "Select a quest.";
                _detailBorder.effectColor = ActiveBorderColor;
                return;
            }

            QuestInstance entry = entries[_selectedIndex];
            QuestDefinition definition = QuestService.Instance?.GetDefinition(entry.questId);
            if (definition == null)
            {
                _detailText.text = entry.questId;
                _detailBorder.effectColor = ResolveBorderColor(entry.state);
                return;
            }

            QuestService.Instance?.ClearNewMarker(entry.questId);

            var sb = new StringBuilder();
            sb.AppendLine(definition.displayTitle);
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(definition.journalDescription))
                sb.AppendLine(definition.journalDescription);

            sb.AppendLine();
            sb.AppendLine("Objectives:");
            if (definition.objectives != null && entry.progress != null)
            {
                for (int i = 0; i < definition.objectives.Length; i++)
                {
                    QuestObjectiveDefinition objective = definition.objectives[i];
                    string objectiveId = QuestLogic.ResolveObjectiveId(objective, i);
                    QuestObjectiveProgress progress = QuestLogic.FindProgress(entry.progress, objectiveId);
                    sb.AppendLine(QuestLogic.FormatJournalObjectiveLine(objective, progress, i));
                }
            }

            sb.AppendLine();
            sb.AppendLine(BuildRewardSummary(definition));
            if (!string.IsNullOrWhiteSpace(definition.giverNpcId))
            {
                string giver = string.IsNullOrWhiteSpace(definition.giverDisplayName)
                    ? definition.giverNpcId
                    : definition.giverDisplayName;
                sb.AppendLine($"Giver: {giver}");
            }

            if (entry.state == QuestRuntimeState.ReadyToTurnIn)
            {
                string giver = string.IsNullOrWhiteSpace(definition.giverDisplayName)
                    ? definition.giverNpcId
                    : definition.giverDisplayName;
                sb.AppendLine();
                sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(AccentTextColor)}>Return to {giver}</color>");
            }

            if (entry.state == QuestRuntimeState.Failed && !string.IsNullOrWhiteSpace(entry.failReason))
            {
                sb.AppendLine();
                sb.AppendLine(entry.failReason);
            }

            _detailText.text = sb.ToString();
            _detailBorder.effectColor = ResolveBorderColor(entry.state);
        }

        static string BuildRewardSummary(QuestDefinition definition)
        {
            var sb = new StringBuilder("Rewards:");
            QuestRewardBundle rewards = definition.rewards;
            bool any = false;
            if (rewards.gold > 0)
            {
                sb.Append(' ').Append(rewards.gold).Append(" gold");
                any = true;
            }

            if (rewards.items != null)
            {
                for (int i = 0; i < rewards.items.Length; i++)
                {
                    ItemData item = rewards.items[i];
                    if (item == null)
                        continue;

                    if (any)
                        sb.Append(',');
                    sb.Append(' ').Append(QuestLogic.ResolveRewardQuantity(rewards.itemQuantities, i))
                        .Append(" × ").Append(item.itemName);
                    any = true;
                }
            }

            if (!any)
                sb.Append(" none");

            return sb.ToString();
        }

        static Color ResolveBorderColor(QuestRuntimeState state)
        {
            switch (state)
            {
                case QuestRuntimeState.ReadyToTurnIn:
                    return ReadyBorderColor;
                case QuestRuntimeState.Completed:
                    return CompletedBorderColor;
                case QuestRuntimeState.Failed:
                    return FailedBorderColor;
                default:
                    return ActiveBorderColor;
            }
        }

        static string GetEmptyMessage(JournalTab tab)
        {
            switch (tab)
            {
                case JournalTab.Completed:
                    return "No completed quests this run.";
                case JournalTab.Failed:
                    return "No failed quests.";
                default:
                    return "No active quests.";
            }
        }

        void CreateRowLabel(string label, Color rowTint, Color borderColor, int index, bool selectable)
        {
            var row = new GameObject($"QuestRow_{index}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            row.transform.SetParent(_listContent, false);

            Image image = row.GetComponent<Image>();
            image.color = rowTint;

            Outline outline = row.GetComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 44f);

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(row.transform, false);
            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 22f;
            text.color = Color.white;
            text.margin = new Vector4(12f, 6f, 12f, 6f);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            if (selectable)
            {
                Button button = row.AddComponent<Button>();
                button.targetGraphic = image;
                int captured = index;
                button.onClick.AddListener(() =>
                {
                    _selectedIndex = captured;
                    RefreshDisplay();
                });
            }

            _rowObjects.Add(row);
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

        void SetTab(JournalTab tab)
        {
            _tab = tab;
            _selectedIndex = -1;
            RefreshDisplay();
        }

        void UpdateTabButtonStyles()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                TabButtonChrome chrome = _tabButtons[i];
                if (chrome.Background == null)
                    continue;

                chrome.Background.color = chrome.Tab == _tab
                    ? ResolveTabSelectedTint(chrome.Tab)
                    : RowNormalTint;
            }
        }

        static Color ResolveTabSelectedTint(JournalTab tab)
        {
            Color accent = tab switch
            {
                JournalTab.Completed => CompletedBorderColor,
                JournalTab.Failed => FailedBorderColor,
                _ => ActiveBorderColor,
            };

            return Color.Lerp(RowNormalTint, accent, 0.55f);
        }

        void UpdateHint()
        {
            if (_hintText == null)
                return;

            _hintText.text = _tab == JournalTab.Active
                ? "Esc close · P pin/unpin · J journal"
                : "Esc close · J journal";
        }

        void EnsurePanelBuilt()
        {
            if (_panelRoot != null)
                return;

            var canvasGo = new GameObject("QuestJournalCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _panelRoot = new GameObject("QuestJournalPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panelRoot.transform.SetParent(canvasGo.transform, false);
            Image panelImage = _panelRoot.GetComponent<Image>();
            panelImage.color = PanelBackgroundColor;
            RectTransform panelRt = _panelRoot.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = new Vector2(24f, 24f);
            panelRt.offsetMax = new Vector2(-24f, -24f);

            _titleText = CreateText(_panelRoot.transform, "Title", "QUESTS", 34f, TextAlignmentOptions.TopLeft);
            RectTransform titleRt = _titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.sizeDelta = new Vector2(-32f, 48f);
            titleRt.anchoredPosition = new Vector2(16f, -12f);

            CreateTabBar(_panelRoot.transform);

            var body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(_panelRoot.transform, false);
            RectTransform bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(16f, 48f);
            bodyRt.offsetMax = new Vector2(-16f, -72f);

            var listPane = new GameObject("ListPane", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            listPane.transform.SetParent(body.transform, false);
            Image listPaneImage = listPane.GetComponent<Image>();
            listPaneImage.color = new Color(0.12f, 0.125f, 0.135f, 0.92f);
            RectTransform listPaneRt = listPane.GetComponent<RectTransform>();
            listPaneRt.anchorMin = new Vector2(0f, 0f);
            listPaneRt.anchorMax = new Vector2(0.38f, 1f);
            listPaneRt.offsetMin = Vector2.zero;
            listPaneRt.offsetMax = Vector2.zero;

            var listViewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            listViewport.transform.SetParent(listPane.transform, false);
            Image viewportImage = listViewport.GetComponent<Image>();
            viewportImage.color = Color.white;
            listViewport.GetComponent<Mask>().showMaskGraphic = false;
            RectTransform viewportRt = listViewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(4f, 4f);
            viewportRt.offsetMax = new Vector2(-4f, -4f);

            var listContentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContentGo.transform.SetParent(listViewport.transform, false);
            _listContent = listContentGo.GetComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0f, 1f);
            _listContent.anchorMax = new Vector2(1f, 1f);
            _listContent.pivot = new Vector2(0.5f, 1f);
            VerticalLayoutGroup layout = listContentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = listContentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = listPane.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = _listContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var detailPane = new GameObject("DetailPane", typeof(RectTransform), typeof(Image), typeof(Outline));
            detailPane.transform.SetParent(body.transform, false);
            Image detailImage = detailPane.GetComponent<Image>();
            detailImage.color = new Color(0.12f, 0.125f, 0.135f, 0.92f);
            _detailBorder = detailPane.GetComponent<Outline>();
            _detailBorder.effectColor = ActiveBorderColor;
            _detailBorder.effectDistance = new Vector2(3f, -3f);
            RectTransform detailRt = detailPane.GetComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.4f, 0f);
            detailRt.anchorMax = new Vector2(1f, 1f);
            detailRt.offsetMin = new Vector2(8f, 0f);
            detailRt.offsetMax = Vector2.zero;

            _detailText = CreateText(detailPane.transform, "Detail", string.Empty, 24f, TextAlignmentOptions.TopLeft);
            _detailText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform detailTextRt = _detailText.rectTransform;
            detailTextRt.anchorMin = Vector2.zero;
            detailTextRt.anchorMax = Vector2.one;
            detailTextRt.offsetMin = new Vector2(16f, 16f);
            detailTextRt.offsetMax = new Vector2(-16f, -16f);

            _hintText = CreateText(_panelRoot.transform, "Hint", string.Empty, 18f, TextAlignmentOptions.BottomLeft);
            RectTransform hintRt = _hintText.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0f, 0f);
            hintRt.sizeDelta = new Vector2(-32f, 32f);
            hintRt.anchoredPosition = new Vector2(16f, 12f);
            _hintText.color = new Color(0.65f, 0.68f, 0.72f, 1f);
        }

        void CreateTabBar(Transform parent)
        {
            var tabBar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabBar.transform.SetParent(parent, false);
            RectTransform tabRt = tabBar.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(1f, 1f);
            tabRt.anchorMax = new Vector2(1f, 1f);
            tabRt.pivot = new Vector2(1f, 1f);
            tabRt.sizeDelta = new Vector2(420f, 40f);
            tabRt.anchoredPosition = new Vector2(-16f, -12f);
            HorizontalLayoutGroup layout = tabBar.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateTabButton(tabBar.transform, "Active", JournalTab.Active);
            CreateTabButton(tabBar.transform, "Completed", JournalTab.Completed);
            CreateTabButton(tabBar.transform, "Failed", JournalTab.Failed);
        }

        void CreateTabButton(Transform parent, string label, JournalTab tab)
        {
            var buttonGo = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            Image image = buttonGo.GetComponent<Image>();
            image.color = RowNormalTint;
            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = image;
            JournalTab captured = tab;
            button.onClick.AddListener(() => SetTab(captured));
            _tabButtons.Add(new TabButtonChrome { Tab = tab, Background = image });

            TextMeshProUGUI text = CreateText(buttonGo.transform, "Label", label, 20f, TextAlignmentOptions.Center);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }
    }
}
