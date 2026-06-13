using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Racial
{
    static class RacialUiTheme
    {
        public const float TitleFontSize = 28f;
        public const float BannerFontSize = 17f;
        public const float SectionFontSize = 15f;
        public const float FooterFontSize = 15f;
        public const float PartyNameFontSize = 15f;
        public const float PartyKeyFontSize = 13f;
        public const float CardTitleFontSize = 21f;
        public const float CardBodyFontSize = 17f;
        public const float CardBadgeFontSize = 13f;
        public const float MessageFontSize = 18f;

        public static readonly Color PanelBackground = new Color(0.08f, 0.085f, 0.095f, 0.96f);
        public static readonly Color TitleText = new Color(0.9f, 0.92f, 0.95f);
        public static readonly Color BannerText = new Color(0.784f, 0.627f, 0.376f, 1f);
        public static readonly Color FooterText = new Color(0.7f, 0.735f, 0.76f);
        public static readonly Color SectionLabel = new Color(0.72f, 0.76f, 0.82f);
        public static readonly Color CardBackground = new Color(0.14f, 0.15f, 0.165f, 0.95f);
        public static readonly Color CardBorder = new Color(0.22f, 0.24f, 0.28f, 1f);
        public static readonly Color ActiveAccent = new Color(0.784f, 0.627f, 0.376f, 1f);
        public static readonly Color ActiveBadge = new Color(0.29f, 0.541f, 0.353f, 1f);
        public static readonly Color FocusBorder = new Color(0.91f, 0.77f, 0.28f, 1f);
        public static readonly Color InactiveBorder = new Color(0.14f, 0.16f, 0.2f, 0.95f);
        public static readonly Color BodyText = new Color(0.88f, 0.91f, 0.94f);
        public static readonly Color MutedText = new Color(0.62f, 0.66f, 0.72f);
        public static readonly Color TimelineLine = new Color(0.35f, 0.38f, 0.42f, 0.9f);
        public static readonly Color TimelineDot = new Color(0.784f, 0.627f, 0.376f, 1f);
        public static readonly Color GhostDot = new Color(0.45f, 0.48f, 0.52f, 0.55f);
        public static readonly Color BeastmanSectionAccent = new Color(0.55f, 0.78f, 0.42f, 1f);
        public static readonly Color BeastmanCardBackground = new Color(0.12f, 0.14f, 0.11f, 0.95f);
        public static readonly Color BeastmanCardAccent = new Color(0.42f, 0.62f, 0.32f, 1f);
        public static readonly Color DragonianSectionAccent = new Color(0.88f, 0.42f, 0.28f, 1f);
        public static readonly Color DragonianBudgetBackground = new Color(0.16f, 0.1f, 0.1f, 0.95f);
        public static readonly Color DragonianColumnBackground = new Color(0.11f, 0.115f, 0.12f, 0.92f);
        public static readonly Color DragonianRowBackground = new Color(0.14f, 0.12f, 0.12f, 0.95f);
        public static readonly Color DragonianRowBorder = new Color(0.28f, 0.18f, 0.16f, 0.95f);
        public static readonly Color DragonianActionButtonBackground = new Color(0.32f, 0.16f, 0.14f, 0.98f);
        public static readonly Color HumanMageSectionAccent = new Color(0.42f, 0.34f, 0.82f, 1f);
        public static readonly Color HumanMageSecondaryAccent = new Color(0.28f, 0.52f, 0.88f, 1f);
        public static readonly Color HumanMageBudgetBackground = new Color(0.11f, 0.12f, 0.18f, 0.95f);
        public static readonly Color HumanMageColumnBackground = new Color(0.11f, 0.115f, 0.14f, 0.92f);
        public static readonly Color HumanMageRowBackground = new Color(0.14f, 0.14f, 0.2f, 0.95f);
        public static readonly Color HumanMageRowBorder = new Color(0.24f, 0.22f, 0.38f, 0.95f);
        public static readonly Color HumanMageActionButtonBackground = new Color(0.22f, 0.18f, 0.38f, 0.98f);

        static Sprite _placeholderSprite;
        static Sprite _imprintEmblemSprite;
        static Sprite _soulBeastEmptyEmblemSprite;
        static Sprite _soulBeastBondEmblemSprite;
        static Sprite _soulBeastPassiveEmblemSprite;
        static Sprite _soulBeastActiveEmblemSprite;
        static Sprite _dragonianSpellEmblemSprite;
        static Sprite _humanMageSpellEmblemSprite;
        static TMP_FontAsset _font;

        static TMP_FontAsset UiFont
        {
            get
            {
                if (_font == null)
                    _font = TMP_Settings.defaultFontAsset;

                return _font;
            }
        }

        public static Sprite PlaceholderSprite
        {
            get
            {
                if (_placeholderSprite == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                }

                return _placeholderSprite;
            }
        }

        public static Sprite ImprintEmblemSprite
        {
            get
            {
                if (_imprintEmblemSprite == null)
                    _imprintEmblemSprite = CreateImprintEmblemSprite();

                return _imprintEmblemSprite;
            }
        }

        public static Sprite SoulBeastEmptyEmblemSprite
        {
            get
            {
                if (_soulBeastEmptyEmblemSprite == null)
                    _soulBeastEmptyEmblemSprite = CreateRingEmblemSprite(new Color(0.45f, 0.52f, 0.4f, 0.35f), dashed: true);

                return _soulBeastEmptyEmblemSprite;
            }
        }

        public static Sprite SoulBeastBondEmblemSprite
        {
            get
            {
                if (_soulBeastBondEmblemSprite == null)
                    _soulBeastBondEmblemSprite = CreateRingEmblemSprite(new Color(0.82f, 0.58f, 0.28f, 0.95f), dashed: false);

                return _soulBeastBondEmblemSprite;
            }
        }

        public static Sprite SoulBeastPassiveEmblemSprite
        {
            get
            {
                if (_soulBeastPassiveEmblemSprite == null)
                    _soulBeastPassiveEmblemSprite = CreateRingEmblemSprite(new Color(0.55f, 0.78f, 0.42f, 0.95f), dashed: false);

                return _soulBeastPassiveEmblemSprite;
            }
        }

        public static Sprite SoulBeastActiveEmblemSprite
        {
            get
            {
                if (_soulBeastActiveEmblemSprite == null)
                    _soulBeastActiveEmblemSprite = CreateRingEmblemSprite(new Color(0.91f, 0.55f, 0.24f, 0.95f), dashed: false);

                return _soulBeastActiveEmblemSprite;
            }
        }

        public static Sprite DragonianSpellEmblemSprite
        {
            get
            {
                if (_dragonianSpellEmblemSprite == null)
                    _dragonianSpellEmblemSprite = CreateDragonianSpellEmblemSprite();

                return _dragonianSpellEmblemSprite;
            }
        }

        public static Sprite HumanMageSpellEmblemSprite
        {
            get
            {
                if (_humanMageSpellEmblemSprite == null)
                    _humanMageSpellEmblemSprite = CreateHumanMageSpellEmblemSprite();

                return _humanMageSpellEmblemSprite;
            }
        }

        public static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float size,
            TextAlignmentOptions alignment,
            FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            if (UiFont != null)
                text.font = UiFont;

            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = BodyText;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.richText = true;
            return text;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Sprite CreateImprintEmblemSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outer = size * 0.42f;
            float inner = size * 0.24f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    Color c = Color.clear;
                    if (dist <= outer && dist >= inner)
                        c = new Color(0.784f, 0.627f, 0.376f, 0.95f);
                    else if (dist < inner * 0.45f)
                        c = new Color(0.95f, 0.82f, 0.35f, 0.85f);

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite CreateDragonianSpellEmblemSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Color ember = new Color(0.88f, 0.42f, 0.28f, 0.95f);
            Color core = new Color(0.98f, 0.72f, 0.28f, 0.9f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    Color c = Color.clear;
                    if (dist <= size * 0.42f && dist >= size * 0.24f)
                        c = ember;
                    else if (dist < size * 0.18f)
                        c = core;

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite CreateHumanMageSpellEmblemSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Color ring = new Color(0.42f, 0.34f, 0.82f, 0.95f);
            Color core = new Color(0.28f, 0.52f, 0.88f, 0.9f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    Color c = Color.clear;
                    if (dist <= size * 0.42f && dist >= size * 0.24f)
                        c = ring;
                    else if (dist < size * 0.18f)
                        c = core;

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite CreateRingEmblemSprite(Color ringColor, bool dashed)
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outer = size * 0.42f;
            float inner = size * 0.28f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    Color c = Color.clear;
                    bool inRing = dist <= outer && dist >= inner;
                    if (inRing)
                    {
                        if (!dashed || ((x + y) / 3) % 2 == 0)
                            c = ringColor;
                    }
                    else if (dist < inner * 0.45f)
                    {
                        c = new Color(ringColor.r, ringColor.g, ringColor.b, ringColor.a * 0.45f);
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
