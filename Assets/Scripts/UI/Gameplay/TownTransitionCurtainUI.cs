using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JRogue.UI.Gameplay
{
    public sealed class TownTransitionCurtainUI : MonoBehaviour
    {
        const float FadeOutSeconds = 0.2f;
        const float FadeInSeconds = 0.25f;

        static TownTransitionCurtainUI _instance;

        CanvasGroup _canvasGroup;
        bool _busy;

        public static TownTransitionCurtainUI Instance => _instance;

        public bool IsBusy => _busy;

        public static TownTransitionCurtainUI EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(TownTransitionCurtainUI));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<TownTransitionCurtainUI>();
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
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public bool RunTransition(Func<bool> swapAction)
        {
            if (_busy || swapAction == null)
                return false;

            StartCoroutine(TransitionRoutine(swapAction));
            return true;
        }

        IEnumerator TransitionRoutine(Func<bool> swapAction)
        {
            _busy = true;
            if (_canvasGroup != null)
                _canvasGroup.gameObject.SetActive(true);

            yield return FadeTo(1f, FadeOutSeconds);

            bool swapped = false;
            try
            {
                swapped = swapAction();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            yield return null;
            yield return FadeTo(0f, FadeInSeconds);

            if (_canvasGroup != null)
                _canvasGroup.gameObject.SetActive(false);

            _busy = false;

            if (!swapped)
                Debug.LogWarning("[TownTransition] Floor swap failed during building transition.");
        }

        IEnumerator FadeTo(float alpha, float duration)
        {
            if (_canvasGroup == null)
                yield break;

            float start = _canvasGroup.alpha;
            if (duration <= 0f)
            {
                _canvasGroup.alpha = alpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(start, alpha, t);
                yield return null;
            }

            _canvasGroup.alpha = alpha;
        }

        void BuildUi()
        {
            var canvasGo = new GameObject(
                "TownTransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var curtainGo = new GameObject("Curtain", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            curtainGo.transform.SetParent(canvasGo.transform, false);
            RectTransform rt = (RectTransform)curtainGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            Image image = curtainGo.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            _canvasGroup = curtainGo.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = false;
            curtainGo.SetActive(false);
        }
    }
}
