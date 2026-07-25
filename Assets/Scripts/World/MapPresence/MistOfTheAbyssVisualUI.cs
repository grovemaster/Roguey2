using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.World.MapPresence
{
    /// <summary>
    /// HUD badge + screen-edge vignette while Mist of the Abyss applies to the active floor.
    /// </summary>
    public sealed class MistOfTheAbyssVisualUI : MonoBehaviour
    {
        static MistOfTheAbyssVisualUI _instance;

        GameObject _root;
        GameObject _badge;
        Image _vignette;

        public static MistOfTheAbyssVisualUI Instance => _instance;

        public static MistOfTheAbyssVisualUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(MistOfTheAbyssVisualUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MistOfTheAbyssVisualUI>();
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
            DontDestroyOnLoad(gameObject);
            BuildUi();
            SetVisible(false);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public void SetVisible(bool visible)
        {
            if (_root == null)
                BuildUi();

            if (_root != null)
                _root.SetActive(visible);
        }

        void BuildUi()
        {
            if (_root != null)
                return;

            var canvasGo = new GameObject(
                "MistOfTheAbyssCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _root = canvasGo;

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above playfield, below hotbar/console/modals (calendar is 47; hotbar typically higher).
            canvas.sortingOrder = 40;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Vignette: full-screen image with transparent center via simple dark edges.
            var vignetteGo = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
            vignetteGo.transform.SetParent(canvasGo.transform, false);
            RectTransform vignetteRt = (RectTransform)vignetteGo.transform;
            vignetteRt.anchorMin = Vector2.zero;
            vignetteRt.anchorMax = Vector2.one;
            vignetteRt.offsetMin = Vector2.zero;
            vignetteRt.offsetMax = Vector2.zero;
            _vignette = vignetteGo.GetComponent<Image>();
            _vignette.raycastTarget = false;
            _vignette.sprite = CreateVignetteSprite();
            _vignette.type = Image.Type.Sliced;
            _vignette.color = new Color(0.35f, 0.05f, 0.12f, 0.55f);

            // Badge top-left
            _badge = new GameObject("MistBadge", typeof(RectTransform), typeof(Image));
            _badge.transform.SetParent(canvasGo.transform, false);
            RectTransform badgeRt = (RectTransform)_badge.transform;
            badgeRt.anchorMin = new Vector2(0f, 1f);
            badgeRt.anchorMax = new Vector2(0f, 1f);
            badgeRt.pivot = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(16f, -12f);
            badgeRt.sizeDelta = new Vector2(280f, 40f);
            Image badgeBg = _badge.GetComponent<Image>();
            badgeBg.raycastTarget = false;
            badgeBg.color = new Color(0.12f, 0.04f, 0.08f, 0.88f);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(_badge.transform, false);
            TextMeshProUGUI label = textGo.GetComponent<TextMeshProUGUI>();
            label.text = "Mist of the Abyss";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.92f, 0.78f, 0.82f, 1f);
            label.raycastTarget = false;
            RectTransform textRt = (RectTransform)label.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 4f);
            textRt.offsetMax = new Vector2(-12f, -4f);
        }

        static Sprite CreateVignetteSprite()
        {
            const int size = 64;
            const int border = 18;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < border || y < border || x >= size - border || y >= size - border;
                    float dist = 0f;
                    if (x < border)
                        dist = Mathf.Max(dist, (border - x) / (float)border);
                    if (y < border)
                        dist = Mathf.Max(dist, (border - y) / (float)border);
                    if (x >= size - border)
                        dist = Mathf.Max(dist, (x - (size - border - 1)) / (float)border);
                    if (y >= size - border)
                        dist = Mathf.Max(dist, (y - (size - border - 1)) / (float)border);

                    float a = edge ? Mathf.Clamp01(dist) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply();
            return Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }
    }
}
