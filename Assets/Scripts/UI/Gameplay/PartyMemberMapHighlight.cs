using JRogue.Actors;
using JRogue.Core.Actor;
using UnityEngine;

namespace JRogue.UI.Gameplay
{
    public sealed class PartyMemberMapHighlight : MonoBehaviour
    {
        static readonly Color HighlightColor = new Color(0.91f, 0.77f, 0.28f, 0.95f);

        static PartyMemberMapHighlight _instance;

        GameObject _ringRoot;
        SpriteRenderer _ringRenderer;
        Sprite _ringSprite;
        BaseActor _attachedActor;

        public static PartyMemberMapHighlight Instance => _instance;

        public static PartyMemberMapHighlight EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(PartyMemberMapHighlight));
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PartyMemberMapHighlight>();
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
            BuildRing();
            SetVisible(false);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void LateUpdate()
        {
            if (_ringRoot == null || !_ringRoot.activeSelf || _attachedActor == null)
                return;

            SyncRingToActor(_attachedActor.transform);
        }

        public void AttachTo(BaseActor actor)
        {
            _attachedActor = actor;
            if (actor == null)
            {
                SetVisible(false);
                return;
            }

            SyncRingToActor(actor.transform);
            SetVisible(true);
        }

        public void Clear()
        {
            _attachedActor = null;
            SetVisible(false);
        }

        void BuildRing()
        {
            _ringRoot = new GameObject("SelectionRing");
            _ringRoot.transform.SetParent(transform, false);
            _ringRenderer = _ringRoot.AddComponent<SpriteRenderer>();
            _ringSprite = CreateRingSprite();
            _ringRenderer.sprite = _ringSprite;
            _ringRenderer.color = HighlightColor;
            _ringRenderer.sortingOrder = 120;
        }

        void SyncRingToActor(Transform actorRoot)
        {
            SpriteRenderer source = GridFootprintUtility.FindPrimarySpriteRenderer(actorRoot);
            if (source == null || source.sprite == null || _ringSprite == null)
            {
                _ringRoot.transform.position = actorRoot.position;
                _ringRoot.transform.localScale = Vector3.one * 1.35f;
                return;
            }

            if (_ringRenderer.sprite != _ringSprite)
                _ringRenderer.sprite = _ringSprite;

            _ringRenderer.sortingLayerID = source.sortingLayerID;
            _ringRenderer.sortingOrder = source.sortingOrder - 1;
            _ringRenderer.flipX = source.flipX;

            // Center on rendered sprite bounds so pivot mismatches (DCSS foot vs ring) do not offset the frame.
            _ringRoot.transform.position = source.bounds.center;

            Vector2 targetSize = source.bounds.size * 1.18f;
            Vector2 ringSize = _ringSprite.bounds.size;
            _ringRoot.transform.localScale = new Vector3(
                targetSize.x / ringSize.x,
                targetSize.y / ringSize.y,
                1f);
        }

        void SetVisible(bool visible)
        {
            if (_ringRoot != null)
                _ringRoot.SetActive(visible);
        }

        static Sprite CreateRingSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color white = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    bool innerClear = x >= 3 && x <= size - 4 && y >= 3 && y <= size - 4;
                    texture.SetPixel(x, y, border && !innerClear ? white : clear);
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }
    }
}
