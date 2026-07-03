using System.Collections.Generic;
using JRogue.Actors;
using JRogue.GridFeatures;
using JRogue.Manager.Map;
using JRogue.World.Generation.Vaults;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Interactables
{
    public sealed class InteractableTileService : MonoBehaviour
    {
        public static InteractableTileService Instance { get; private set; }

        [SerializeField] Tilemap interactableOverlayMap;

        readonly Dictionary<Vector3Int, InteractableTileInstance> _byCell =
            new Dictionary<Vector3Int, InteractableTileInstance>();

        readonly Dictionary<InteractableTileId, InteractableTileInstance> _byId =
            new Dictionary<InteractableTileId, InteractableTileInstance>();

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureOverlayMap();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetOverlayMap(Tilemap overlay) => interactableOverlayMap = overlay;

        public bool BlocksOccupancy(Vector3Int cell) =>
            TryGetInstance(cell, out InteractableTileInstance instance)
            && instance.Definition != null
            && instance.Definition.blocksOccupancy;

        public bool TryGetInstance(Vector3Int cell, out InteractableTileInstance instance) =>
            _byCell.TryGetValue(cell, out instance);

        public bool TryGetInstanceById(InteractableTileId id, out InteractableTileInstance instance) =>
            _byId.TryGetValue(id, out instance);

        public void Register(Vector3Int cell, InteractableTileDefinition definition)
        {
            if (definition == null || definition.interactableId == InteractableTileId.None)
                return;

            var instance = new InteractableTileInstance(cell, definition);
            _byCell[cell] = instance;
            _byId[definition.interactableId] = instance;
            RefreshOverlayVisual(instance);
        }

        /// <summary>True when <paramref name="to"/> is a neighboring cell (8-way, same z).</summary>
        public static bool IsAdjacent(Vector3Int from, Vector3Int to)
        {
            Vector3Int delta = to - from;
            if (delta.z != 0)
                return false;

            return Mathf.Abs(delta.x) <= 1
                && Mathf.Abs(delta.y) <= 1
                && (delta.x != 0 || delta.y != 0);
        }

        public bool ShouldAttemptPlayerBump(Vector3Int from, Vector3Int to) =>
            BlocksOccupancy(to) && IsAdjacent(from, to);

        public InteractableBumpResult TryBumpActivate(Vector3Int cell, BaseActor bumper)
        {
            if (!TryGetInstance(cell, out InteractableTileInstance instance)
                || instance.Definition == null
                || !instance.Definition.bumpEnabled)
            {
                return InteractableBumpResult.Failed;
            }

            return TryActivate(instance, bumper, InteractableActivationSource.PlayerBump);
        }

        public InteractableBumpResult ActivateById(
            InteractableTileId id,
            InteractableActivationSource source,
            BaseActor bumper = null)
        {
            if (!TryGetInstanceById(id, out InteractableTileInstance instance))
            {
                Debug.LogWarning($"[Interactable] No instance registered for {id}.");
                return InteractableBumpResult.Failed;
            }

            return TryActivate(instance, bumper, source);
        }

        public void ForceSetLeverState(InteractableTileId id, bool on)
        {
            if (!TryGetInstanceById(id, out InteractableTileInstance instance))
                return;

            if (on)
                instance.SetOn();
            else
                instance.SetOff();

            RefreshOverlayVisual(instance);
        }

        InteractableBumpResult TryActivate(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            if (instance == null || instance.Definition == null)
                return InteractableBumpResult.Failed;

            if (!instance.Definition.allowRepeatActivation && instance.IsOn)
            {
                Debug.Log(
                    $"[Interactable] {instance.Definition.displayName} at {instance.Cell} is already activated.");
                return InteractableBumpResult.AlreadyOn;
            }

            if (source == InteractableActivationSource.PlayerBump && !instance.Definition.bumpEnabled)
                return InteractableBumpResult.Failed;

            if (!EvaluatePreconditions(instance, bumper, source, out string failureReason))
            {
                Debug.Log(
                    $"[Interactable] {instance.Definition.displayName} precondition failed: {failureReason}");
                return InteractableBumpResult.PreconditionFailed;
            }

            if (!instance.Definition.allowRepeatActivation)
            {
                instance.SetOn();
                RefreshOverlayVisual(instance);
            }

            RunEffects(instance, bumper, source);

            Debug.Log(
                $"[Interactable] {instance.Definition.displayName} activated at {instance.Cell} ({source}).");

            return InteractableBumpResult.Activated;
        }

        bool EvaluatePreconditions(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source,
            out string failureReason)
        {
            InteractablePrecondition[] preconditions = instance.Definition.preconditions;
            if (preconditions == null || preconditions.Length == 0)
            {
                failureReason = null;
                return true;
            }

            for (int i = 0; i < preconditions.Length; i++)
            {
                InteractablePrecondition precondition = preconditions[i];
                if (precondition == null)
                    continue;

                if (!precondition.Evaluate(instance, bumper, source, out failureReason))
                    return false;
            }

            failureReason = null;
            return true;
        }

        void RunEffects(
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            InteractableEffect[] effects = instance.Definition.onActivateEffects;
            if (effects == null)
                return;

            for (int i = 0; i < effects.Length; i++)
            {
                InteractableEffect effect = effects[i];
                if (effect == null)
                    continue;

                effect.Execute(this, instance, bumper, source);
            }
        }

        public void RefreshAllOverlayVisuals()
        {
            foreach (KeyValuePair<Vector3Int, InteractableTileInstance> entry in _byCell)
                RefreshOverlayVisual(entry.Value);
        }

        void RefreshOverlayVisual(InteractableTileInstance instance)
        {
            if (interactableOverlayMap == null || instance?.Definition == null)
                return;

            if (!IsCellVisibleToPlayer(instance.Cell))
            {
                GridOverlayPainter.Clear(interactableOverlayMap, instance.Cell);
                return;
            }

            if (UsesTerrainTileArtOnly(instance.Definition))
            {
                GridOverlayPainter.Clear(interactableOverlayMap, instance.Cell);
                VaultStampDiagnostics.LogMonumentInteractableOverlayPaint(
                    instance.Cell,
                    instance,
                    paintedSprite: null,
                    cellVisible: true,
                    spriteSource: "skipped-terrain-only-bump");
                return;
            }

            Sprite sprite = instance.IsOn
                ? instance.Definition.spriteOn
                : instance.Definition.spriteOff;

            if (sprite == null)
                sprite = instance.IsOn
                    ? InteractablePlaceholderSprites.OnLeft
                    : InteractablePlaceholderSprites.OffRight;

            if (sprite == null)
            {
                GridOverlayPainter.Clear(interactableOverlayMap, instance.Cell);
                return;
            }

            GridOverlayPainter.Paint(interactableOverlayMap, instance.Cell, tile: null, sprite: sprite);
        }

        /// <summary>
        /// Bump interactables with no overlay art (monument inscription, altar) use stamped floor/wall tiles only.
        /// </summary>
        static bool UsesTerrainTileArtOnly(InteractableTileDefinition definition) =>
            definition != null && definition.spriteOff == null && definition.spriteOn == null;

        static bool IsCellVisibleToPlayer(Vector3Int cell)
        {
            VisibilityManager visibility = UnityEngine.Object.FindAnyObjectByType<VisibilityManager>();
            if (visibility == null)
                return true;

            return visibility.IsVisible(cell);
        }

        void EnsureOverlayMap()
        {
            if (interactableOverlayMap != null)
                return;

            if (MapManager.Instance != null && MapManager.Instance.InteractableOverlayMap != null)
            {
                interactableOverlayMap = MapManager.Instance.InteractableOverlayMap;
                return;
            }

            GameObject grid = GameObject.Find("Grid");
            if (grid == null)
                return;

            Transform existing = grid.transform.Find("Interactable_Overlay");
            if (existing != null)
            {
                interactableOverlayMap = existing.GetComponent<Tilemap>();
                return;
            }

            var overlayGo = new GameObject("Interactable_Overlay");
            overlayGo.transform.SetParent(grid.transform, false);
            interactableOverlayMap = overlayGo.AddComponent<Tilemap>();
            overlayGo.AddComponent<TilemapRenderer>();
        }

        public void ClearAllRegistrations()
        {
            if (interactableOverlayMap != null)
            {
                foreach (Vector3Int cell in _byCell.Keys)
                    GridOverlayPainter.Clear(interactableOverlayMap, cell);
            }

            _byCell.Clear();
            _byId.Clear();
        }

        public void CaptureSnapshot(System.Collections.Generic.List<JRogue.World.Generation.InteractableSnapshotEntry> dest)
        {
            if (dest == null)
                return;

            dest.Clear();
            foreach (System.Collections.Generic.KeyValuePair<Vector3Int, InteractableTileInstance> pair in _byCell)
            {
                InteractableTileInstance instance = pair.Value;
                if (instance?.Definition == null)
                    continue;

                dest.Add(new JRogue.World.Generation.InteractableSnapshotEntry
                {
                    cell = instance.Cell,
                    definition = instance.Definition,
                    isOn = instance.IsOn,
                });
            }
        }

        public void RestoreSnapshot(System.Collections.Generic.IReadOnlyList<JRogue.World.Generation.InteractableSnapshotEntry> src)
        {
            ClearAllRegistrations();
            if (src == null)
                return;

            for (int i = 0; i < src.Count; i++)
            {
                JRogue.World.Generation.InteractableSnapshotEntry entry = src[i];
                if (entry.definition == null)
                    continue;

                Register(entry.cell, entry.definition);
                if (entry.isOn && TryGetInstance(entry.cell, out InteractableTileInstance instance))
                {
                    instance.SetOn();
                    RefreshOverlayVisual(instance);
                }
            }
        }
    }
}
