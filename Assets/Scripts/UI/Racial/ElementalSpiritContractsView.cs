using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.UI.Hotbar;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    public sealed class ElementalSpiritContractsView : MonoBehaviour
    {
        Transform _contentRoot;
        BaseActor _focusedElf;
        readonly List<GameObject> _rows = new List<GameObject>();
        readonly List<NicknameFieldBinding> _nicknameFields = new List<NicknameFieldBinding>();

        sealed class NicknameFieldBinding
        {
            public string ContractInstanceId;
            public TMP_InputField Input;
            public string SavedValue;
        }

        public static ElementalSpiritContractsView Create(Transform scrollContentParent)
        {
            Transform existing = scrollContentParent.Find("SpiritContractsContent");
            ElementalSpiritContractsView view;
            if (existing != null)
            {
                view = existing.GetComponent<ElementalSpiritContractsView>() ??
                       existing.gameObject.AddComponent<ElementalSpiritContractsView>();
                view._contentRoot = existing;
            }
            else
            {
                var content = new GameObject("SpiritContractsContent", typeof(RectTransform));
                content.transform.SetParent(scrollContentParent, false);
                var rt = content.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, 0f);

                var layout = content.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.padding = new RectOffset(0, 0, 4, 8);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                view = content.AddComponent<ElementalSpiritContractsView>();
                view._contentRoot = content.transform;
            }

            return view;
        }

        public void Rebuild(BaseActor elf, IReadOnlyList<ElfElementalSpiritContractCard> cards)
        {
            _focusedElf = elf;
            ClearRows();

            if (cards == null || cards.Count == 0)
            {
                CreateMessageRow(ElfElementalSpiritViewModel.EmptyRosterMessage);
                return;
            }

            for (int i = 0; i < cards.Count; i++)
                _rows.Add(CreateContractRow(cards[i]));
        }

        public void SetPlainMessage(string message)
        {
            _focusedElf = null;
            ClearRows();
            CreateMessageRow(message);
        }

        public bool IsNicknameFieldFocused()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null)
                return false;

            foreach (NicknameFieldBinding binding in _nicknameFields)
            {
                if (binding.Input != null && binding.Input.gameObject == selected)
                    return true;
            }

            return false;
        }

        public bool TryRevertFocusedNickname()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null)
                return false;

            foreach (NicknameFieldBinding binding in _nicknameFields)
            {
                if (binding.Input == null || binding.Input.gameObject != selected)
                    continue;

                binding.Input.text = binding.SavedValue ?? string.Empty;
                binding.Input.DeactivateInputField(clearSelection: true);
                return true;
            }

            return false;
        }

        void ClearRows()
        {
            foreach (GameObject row in _rows)
            {
                if (row != null)
                    Destroy(row);
            }

            _rows.Clear();
            _nicknameFields.Clear();
        }

        void CreateMessageRow(string message)
        {
            var row = new GameObject("Message", typeof(RectTransform));
            row.transform.SetParent(_contentRoot, false);
            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 80f;

            TextMeshProUGUI text = RacialUiTheme.CreateText(
                row.transform,
                "Text",
                message,
                RacialUiTheme.MessageFontSize,
                TextAlignmentOptions.TopLeft);
            RacialUiTheme.Stretch(text.rectTransform);
            text.color = RacialUiTheme.MutedText;
            _rows.Add(row);
        }

        GameObject CreateContractRow(ElfElementalSpiritContractCard card)
        {
            var row = new GameObject("ContractRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_contentRoot, false);
            row.GetComponent<Image>().sprite = RacialUiTheme.PlaceholderSprite;
            row.GetComponent<Image>().color = RacialUiTheme.CardBackground;

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 120f;

            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            string header = card.Title ?? "Spirit";
            if (card.ContractLevel > 0)
                header += $" · Lv {card.ContractLevel}";
            if (card.IsSummoned)
                header += " · SUMMONED";

            TextMeshProUGUI title = RacialUiTheme.CreateText(
                row.transform,
                "Title",
                header,
                RacialUiTheme.CardTitleFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            title.color = RacialUiTheme.TitleText;

            if (!string.IsNullOrWhiteSpace(card.Subtitle))
            {
                TextMeshProUGUI subtitle = RacialUiTheme.CreateText(
                    row.transform,
                    "Subtitle",
                    card.Subtitle,
                    RacialUiTheme.FooterFontSize,
                    TextAlignmentOptions.MidlineLeft);
                subtitle.color = RacialUiTheme.MutedText;
            }

            CreateNicknameRow(row.transform, card);

            AppendBodyLine(row.transform, "Progress", card.ProgressLine);
            AppendBodyLine(row.transform, "Cap", card.CapLine);
            AppendBodyLine(row.transform, "Element", card.ElementLine);
            AppendBodyLine(row.transform, "Costs", card.CostsLine);
            AppendPayloadSection(row.transform, card);

            return row;
        }

        void CreateNicknameRow(Transform parent, ElfElementalSpiritContractCard card)
        {
            var row = new GameObject("NicknameRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            TextMeshProUGUI label = RacialUiTheme.CreateText(
                row.transform,
                "Label",
                "Nickname:",
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft);
            label.color = RacialUiTheme.BodyText;
            var labelLe = label.gameObject.AddComponent<LayoutElement>();
            labelLe.minWidth = 88f;
            labelLe.preferredWidth = 88f;

            TMP_InputField input = CreateNicknameInput(row.transform);
            input.text = card.Nickname ?? string.Empty;
            ((TextMeshProUGUI)input.placeholder).text = "Nickname (optional)";

            var binding = new NicknameFieldBinding
            {
                ContractInstanceId = card.ContractInstanceId,
                Input = input,
                SavedValue = card.Nickname ?? string.Empty,
            };
            _nicknameFields.Add(binding);

            input.onSelect.AddListener(_ => binding.SavedValue = binding.Input.text ?? string.Empty);
            input.onEndEdit.AddListener(text => CommitNickname(binding, text));
        }

        static TMP_InputField CreateNicknameInput(Transform parent)
        {
            var fieldGo = new GameObject("NicknameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            fieldGo.transform.SetParent(parent, false);
            var fieldLe = fieldGo.AddComponent<LayoutElement>();
            fieldLe.flexibleWidth = 1f;
            fieldLe.minHeight = 28f;
            fieldLe.preferredHeight = 28f;

            Image bg = fieldGo.GetComponent<Image>();
            bg.sprite = RacialUiTheme.PlaceholderSprite;
            bg.color = new Color(0.1f, 0.11f, 0.13f, 0.95f);

            var viewport = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(fieldGo.transform, false);
            RacialUiTheme.Stretch(viewport.GetComponent<RectTransform>());

            var text = RacialUiTheme.CreateText(
                viewport.transform,
                "Text",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft);
            text.color = RacialUiTheme.BodyText;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            RacialUiTheme.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(8f, 2f);
            text.rectTransform.offsetMax = new Vector2(-8f, -2f);

            var placeholder = RacialUiTheme.CreateText(
                viewport.transform,
                "Placeholder",
                string.Empty,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft,
                FontStyles.Italic);
            placeholder.color = RacialUiTheme.MutedText;
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            RacialUiTheme.Stretch(placeholder.rectTransform);
            placeholder.rectTransform.offsetMin = new Vector2(8f, 2f);
            placeholder.rectTransform.offsetMax = new Vector2(-8f, -2f);

            TMP_InputField input = fieldGo.GetComponent<TMP_InputField>();
            input.textViewport = viewport.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = ElementalSpiritDisplayNames.MaxNicknameLength;
            input.onFocusSelectAll = false;
            return input;
        }

        void CommitNickname(NicknameFieldBinding binding, string text)
        {
            if (_focusedElf == null || binding == null || string.IsNullOrEmpty(binding.ContractInstanceId))
                return;

            if (!ElementalSpiritNicknameService.TrySetNickname(
                    _focusedElf,
                    binding.ContractInstanceId,
                    text,
                    out _))
            {
                binding.Input.text = binding.SavedValue ?? string.Empty;
                return;
            }

            binding.SavedValue = ElementalSpiritDisplayNames.NormalizeNickname(text);
            binding.Input.text = binding.SavedValue;

            AbilityHotbarUI.EnsureInstance().RefreshAll();
            Rebuild(_focusedElf, ElfElementalSpiritViewModel.Build(_focusedElf));
        }

        static void AppendBodyLine(Transform parent, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            TextMeshProUGUI line = RacialUiTheme.CreateText(
                parent,
                name,
                value,
                RacialUiTheme.CardBodyFontSize,
                TextAlignmentOptions.MidlineLeft);
            line.color = RacialUiTheme.BodyText;
        }

        static void AppendPayloadSection(Transform parent, ElfElementalSpiritContractCard card)
        {
            if (card.Passives.Count > 0)
            {
                AppendBodyLine(parent, "PassivesHeader", $"PASSIVES ({card.Passives.Count})");
                for (int i = 0; i < card.Passives.Count; i++)
                {
                    ElfElementalSpiritPassiveLine passive = card.Passives[i];
                    string line = string.IsNullOrWhiteSpace(passive.Description)
                        ? $"• {passive.Name}"
                        : $"• {passive.Name}\n{passive.Description.Trim()}";
                    AppendBodyLine(parent, $"Passive_{i}", line);
                }
            }

            if (card.Actives.Count > 0)
            {
                AppendBodyLine(parent, "ActivesHeader", $"ACTIVES ({card.Actives.Count})");
                for (int i = 0; i < card.Actives.Count; i++)
                {
                    ElfElementalSpiritActiveLine active = card.Actives[i];
                    var sb = new System.Text.StringBuilder();
                    sb.Append("• ").Append(active.Name);
                    if (!string.IsNullOrWhiteSpace(active.Description))
                    {
                        sb.Append('\n');
                        sb.Append(active.Description.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(active.Meta))
                    {
                        sb.Append('\n');
                        sb.Append(active.Meta.Trim());
                    }

                    sb.Append("\nAssign on the ability hotbar to use in combat.");
                    AppendBodyLine(parent, $"Active_{i}", sb.ToString());
                }
            }
        }
    }
}
