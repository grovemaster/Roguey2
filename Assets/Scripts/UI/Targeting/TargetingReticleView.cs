using UnityEngine;

namespace JRogue.UI.Targeting
{
    /// <summary>
    /// View component that owns the on-screen targeting reticle: prefab
    /// instantiation, current tile position, and the transform mirror.
    /// Game logic (e.g. <see cref="JRogue.Input.InputHandler"/>) interacts only
    /// through <see cref="Show"/> / <see cref="Move"/> / <see cref="Hide"/> /
    /// <see cref="Position"/>. Future targeting feedback (range halo, AoE
    /// preview, line-of-fire) belongs in this component as well.
    /// </summary>
    public class TargetingReticleView : MonoBehaviour
    {
        [SerializeField] private GameObject reticlePrefab;
        private GameObject activeReticle;
        private Vector3Int position;

        public Vector3Int Position => position;

        public void Show(Vector3Int initialPosition)
        {
            position = initialPosition;
            EnsureReticleInstance();
            if (activeReticle != null) activeReticle.SetActive(true);
            UpdateVisual();
        }

        public void Move(Vector3Int direction)
        {
            position += direction;
            UpdateVisual();
        }

        public void Hide()
        {
            if (activeReticle != null) activeReticle.SetActive(false);
        }

        private void EnsureReticleInstance()
        {
            if (activeReticle != null) return;

            if (reticlePrefab != null)
            {
                activeReticle = Instantiate(reticlePrefab);
                return;
            }

            Debug.LogWarning(
                $"{nameof(TargetingReticleView)} on '{gameObject.name}' has no reticlePrefab; using a procedural fallback quad.");
            activeReticle = BuildRuntimeFallbackReticle();
        }

        private static GameObject BuildRuntimeFallbackReticle()
        {
            Texture2D white = Texture2D.whiteTexture;
            Sprite quad = Sprite.Create(
                white,
                new Rect(0f, 0f, white.width, white.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(1f, Mathf.Max(white.width, white.height)));

            GameObject go = new GameObject("TargetingReticle_RuntimeFallback");
            go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = quad;
            spriteRenderer.color = new Color(1f, 1f, 0.2f, 0.9f);
            spriteRenderer.sortingOrder = 500;

            Transform t = go.transform;
            t.localScale = new Vector3(0.08f, 0.08f, 1f);

            go.SetActive(false);
            return go;
        }

        private void UpdateVisual()
        {
            if (activeReticle == null) return;
            // Add 0.5f to align with tile centers (matches GridMover.SyncPosition).
            activeReticle.transform.position =
                new Vector3(position.x + 0.5f, position.y + 0.5f, 0f);
        }
    }
}
