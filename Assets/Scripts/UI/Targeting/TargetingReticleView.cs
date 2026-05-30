using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Core.Targeting;
using UnityEngine;

namespace JRogue.UI.Targeting
{
    /// <summary>
    /// Targeting reticle: white primary tile + red splash preview tiles.
    /// </summary>
    public class TargetingReticleView : MonoBehaviour
    {
        const int SplashSortingOrder = 499;
        const int PrimarySortingOrder = 500;

        [SerializeField] private GameObject reticlePrefab;
        [SerializeField] private GameObject splashMarkerPrefab;

        private GameObject activeReticle;
        private readonly List<GameObject> splashMarkers = new List<GameObject>();
        private Vector3Int position;
        private SplashZoneDefinition splashZone;
        private Vector3Int casterCell;
        private FacingDirection casterFacing;

        public Vector3Int Position => position;

        public void Show(Vector3Int initialPosition) =>
            Show(initialPosition, null, initialPosition, FacingDirection.North);

        public void Show(
            Vector3Int initialPosition,
            SplashZoneDefinition zone,
            BaseActor caster)
        {
            if (caster == null)
            {
                Show(initialPosition, zone, initialPosition, FacingDirection.North);
                return;
            }

            Show(initialPosition, zone, caster.GridPosition, caster.currentFacing);
        }

        public void Show(
            Vector3Int initialPosition,
            SplashZoneDefinition zone,
            Vector3Int casterGridCell,
            FacingDirection facing)
        {
            position = initialPosition;
            splashZone = zone;
            casterCell = casterGridCell;
            casterFacing = facing;

            EnsurePrimaryReticle();
            if (activeReticle != null)
            {
                activeReticle.SetActive(true);
                ApplyPrimaryTint();
            }

            RefreshSplashMarkers();
            UpdatePrimaryVisual();
        }

        public void Move(Vector3Int direction)
        {
            position += direction;
            RefreshSplashMarkers();
            UpdatePrimaryVisual();
        }

        public void Hide()
        {
            if (activeReticle != null)
                activeReticle.SetActive(false);

            for (int i = 0; i < splashMarkers.Count; i++)
            {
                if (splashMarkers[i] != null)
                    splashMarkers[i].SetActive(false);
            }
        }

        void RefreshSplashMarkers()
        {
            var ctx = new SplashZoneContext(casterCell, position, casterFacing);
            IReadOnlyList<Vector3Int> splash = SplashZoneResolver.GetSplashPreviewCells(splashZone, ctx);
            int needed = splash.Count;

            while (splashMarkers.Count < needed)
                splashMarkers.Add(CreateSplashMarkerInstance());

            for (int i = 0; i < needed; i++)
            {
                GameObject marker = splashMarkers[i];
                marker.SetActive(true);
                PositionMarker(marker, splash[i], SplashSortingOrder);
            }

            for (int i = needed; i < splashMarkers.Count; i++)
            {
                if (splashMarkers[i] != null)
                    splashMarkers[i].SetActive(false);
            }
        }

        void EnsurePrimaryReticle()
        {
            if (activeReticle != null)
                return;

            if (reticlePrefab != null)
            {
                activeReticle = Instantiate(reticlePrefab);
                return;
            }

            Debug.LogWarning(
                $"{nameof(TargetingReticleView)} on '{gameObject.name}' has no reticlePrefab; using procedural fallback.");
            activeReticle = BuildRuntimeFallbackReticle(Color.white, PrimarySortingOrder, 0.08f);
        }

        GameObject CreateSplashMarkerInstance()
        {
            if (splashMarkerPrefab != null)
                return Instantiate(splashMarkerPrefab);

            return BuildRuntimeFallbackReticle(
                new Color(1f, 0.2f, 0.2f, 0.65f),
                SplashSortingOrder,
                0.075f);
        }

        void ApplyPrimaryTint()
        {
            SpriteRenderer sr = activeReticle.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white;
                sr.sortingOrder = PrimarySortingOrder;
            }
        }

        static GameObject BuildRuntimeFallbackReticle(Color color, int sortingOrder, float scale)
        {
            Texture2D white = Texture2D.whiteTexture;
            Sprite quad = Sprite.Create(
                white,
                new Rect(0f, 0f, white.width, white.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(1f, Mathf.Max(white.width, white.height)));

            var go = new GameObject("TargetingReticle_RuntimeFallback");
            go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = quad;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.SetActive(false);
            return go;
        }

        void UpdatePrimaryVisual() => PositionMarker(activeReticle, position, PrimarySortingOrder);

        static void PositionMarker(GameObject marker, Vector3Int cell, int sortingOrder)
        {
            if (marker == null)
                return;

            marker.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            SpriteRenderer sr = marker.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = sortingOrder;
        }
    }
}
