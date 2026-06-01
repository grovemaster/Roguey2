using System;
using System.Collections.Generic;
using JRogue.Data.Door;
using JRogue.GridFeatures;
using JRogue.Manager.Map;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Manager.Door
{
    public sealed class DoorService : MonoBehaviour
    {
        public const string LogPrefix = "[Door]";
        public const string VaultDoorLogPrefix = "[VaultDoor]";

        public static DoorService Instance { get; private set; }

        public event Action<DoorInstance, DoorState, DoorState> StateChanged;

        [SerializeField] Tilemap doorOverlayMap;

        readonly Dictionary<Vector3Int, DoorInstance> _byCell = new Dictionary<Vector3Int, DoorInstance>();
        readonly Dictionary<string, DoorInstance> _byId = new Dictionary<string, DoorInstance>();

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

        public void SetOverlayMap(Tilemap overlay, string logContext = null)
        {
            doorOverlayMap = overlay;
            if (doorOverlayMap != null)
                GridOverlayPainter.ConfigureRenderer(doorOverlayMap, sortingOrder: 5);

            Debug.Log(
                $"{VaultDoorLogPrefix} SetOverlayMap context={logContext ?? "none"} " +
                $"overlay={(doorOverlayMap != null ? doorOverlayMap.name : "null")} " +
                $"registeredCells={_byCell.Count}");
        }

        public void ClearAllRegistrations()
        {
            _byCell.Clear();
            _byId.Clear();
        }

        /// <summary>Repaint every registered door on the current overlay (after bind or regen).</summary>
        public void RefreshAllOverlays(string logContext = null) => RefreshOverlayVisibility(logContext);

        public void RefreshOverlayVisibility(string logContext = null)
        {
            if (doorOverlayMap == null)
            {
                Debug.LogWarning(
                    $"{VaultDoorLogPrefix} RefreshOverlayVisibility skipped context={logContext ?? "none"}: doorOverlayMap null.");
                return;
            }

            int painted = 0;
            int clearedVisibility = 0;
            int clearedMissing = 0;
            foreach (DoorInstance instance in _byCell.Values)
            {
                bool? paintedThis = RefreshOverlayCore(instance, logDecisions: false);
                if (paintedThis == null)
                    clearedMissing++;
                else if (paintedThis.Value)
                    painted++;
                else
                    clearedVisibility++;
            }

            Debug.Log(
                $"{VaultDoorLogPrefix} RefreshOverlayVisibility context={logContext ?? "none"} " +
                $"total={_byCell.Count} painted={painted} clearedNotVisible={clearedVisibility} skipped={clearedMissing} " +
                $"overlay={doorOverlayMap.name}");
        }

        public int RegisteredCellCount => _byCell.Count;

        public bool TryGetAtCell(Vector3Int cell, out DoorInstance instance) =>
            _byCell.TryGetValue(cell, out instance);

        public bool TryGetById(string doorId, out DoorInstance instance)
        {
            instance = null;
            return !string.IsNullOrEmpty(doorId) && _byId.TryGetValue(doorId, out instance);
        }

        public bool BlocksMovement(Vector3Int cell) =>
            TryGetAtCell(cell, out DoorInstance door) && door.BlocksMovement;

        public void Register(DoorPlacement placement)
        {
            if (placement.definition == null || string.IsNullOrEmpty(placement.definition.doorId))
            {
                Debug.LogWarning(
                    $"{VaultDoorLogPrefix} Register rejected cell=({placement.cell.x},{placement.cell.y}): " +
                    $"missing definition or doorId.");
                return;
            }

            bool unlocked = placement.overrideLocked
                ? !placement.startsLocked
                : !placement.definition.startsLocked;

            DoorState state = DoorState.Closed;
            if (placement.overrideOpenState)
                state = placement.initialState;
            else if (!unlocked)
                state = DoorState.Closed;
            else if (placement.definition.startsOpen)
                state = DoorState.Open;
            else
                state = DoorState.Closed;

            if (state == DoorState.Open && !unlocked)
                state = DoorState.Closed;

            var instance = new DoorInstance(placement.definition, placement.cell, state, unlocked);
            _byCell[placement.cell] = instance;
            _byId[placement.definition.doorId] = instance;
            Debug.Log(
                $"{VaultDoorLogPrefix} Register ok cell=({placement.cell.x},{placement.cell.y}) " +
                $"doorId={placement.definition.doorId} def={placement.definition.name} state={state} unlocked={unlocked} " +
                $"orient={instance.Orientation} overlay={(doorOverlayMap != null ? doorOverlayMap.name : "null")} " +
                $"overrideOpen={placement.overrideOpenState} initial={placement.initialState}");
            RefreshOverlay(instance, logDecisions: true);
        }

        public bool Unlock(string doorId, string source = null)
        {
            if (!TryGetById(doorId, out DoorInstance door))
            {
                Debug.LogWarning($"{LogPrefix} Unlock failed — unknown door '{doorId}'.");
                return false;
            }

            if (door.IsUnlocked)
            {
                Debug.Log($"{LogPrefix} '{doorId}' already unlocked.");
                return false;
            }

            door.SetUnlocked(true);
            Debug.Log($"{LogPrefix} Unlocked '{doorId}' ({source ?? "unknown"}).");
            return true;
        }

        public bool TryOpen(string doorId)
        {
            if (!TryGetById(doorId, out DoorInstance door))
                return false;

            return TryOpen(door);
        }

        public bool TryOpen(DoorInstance door)
        {
            if (door == null)
                return false;

            if (!door.IsUnlocked)
            {
                Debug.Log($"{LogPrefix} '{door.DoorId}' is locked.");
                return false;
            }

            if (door.State != DoorState.Closed)
                return false;

            return SetState(door, DoorState.Open, "open");
        }

        public bool TryClose(DoorInstance door)
        {
            if (door == null)
                return false;

            if (!door.IsUnlocked)
                return false;

            if (door.State != DoorState.Open)
                return false;

            return SetState(door, DoorState.Closed, "close");
        }

        public bool TryBreak(DoorInstance door, string source = null)
        {
            if (door == null || door.Definition == null)
                return false;

            if (!door.Definition.canBeBroken)
            {
                Debug.Log($"{LogPrefix} '{door.DoorId}' cannot be broken.");
                return false;
            }

            if (door.State == DoorState.Broken)
                return false;

            return SetState(door, DoorState.Broken, source ?? "break");
        }

        bool SetState(DoorInstance door, DoorState newState, string reason)
        {
            DoorState old = door.State;
            if (old == newState)
                return false;

            door.SetState(newState);
            RefreshOverlay(door);
            Debug.Log($"{LogPrefix} '{door.DoorId}' at {door.Cell}: {old} → {newState} ({reason}).");
            StateChanged?.Invoke(door, old, newState);
            return true;
        }

        public void RefreshOverlay(DoorInstance instance, bool logDecisions = false) =>
            RefreshOverlayCore(instance, logDecisions);

        bool? RefreshOverlayCore(DoorInstance instance, bool logDecisions)
        {
            if (doorOverlayMap == null || instance?.Definition == null)
            {
                if (logDecisions)
                {
                    Debug.LogWarning(
                        $"{VaultDoorLogPrefix} RefreshOverlay skip cell=({instance?.Cell.x},{instance?.Cell.y}): " +
                        $"overlay={(doorOverlayMap != null)} def={(instance?.Definition != null)}.");
                }

                return null;
            }

            bool visible = IsCellVisibleToPlayer(instance.Cell, out bool visibilityManagerFound);
            if (!visible)
            {
                GridOverlayPainter.Clear(doorOverlayMap, instance.Cell);
                if (logDecisions)
                {
                    Debug.Log(
                        $"{VaultDoorLogPrefix} RefreshOverlay CLEAR cell=({instance.Cell.x},{instance.Cell.y}) " +
                        $"doorId={instance.DoorId} state={instance.State}: not visible " +
                        $"(visibilityManager={visibilityManagerFound}).");
                }

                return false;
            }

            Sprite sprite = instance.Definition.GetSprite(instance.State, instance.Orientation);
            bool usedPlaceholder = false;
            if (sprite == null)
            {
                usedPlaceholder = true;
                sprite = instance.Orientation == DoorOrientation.Vertical
                    ? instance.State switch
                    {
                        DoorState.Open => DoorPlaceholderSprites.OpenVertical,
                        DoorState.Broken => DoorPlaceholderSprites.BrokenVertical,
                        _ => DoorPlaceholderSprites.ClosedVertical,
                    }
                    : instance.State switch
                    {
                        DoorState.Open => DoorPlaceholderSprites.OpenHorizontal,
                        DoorState.Broken => DoorPlaceholderSprites.BrokenHorizontal,
                        _ => DoorPlaceholderSprites.ClosedHorizontal,
                    };
            }

            GridOverlayPainter.Paint(doorOverlayMap, instance.Cell, tile: null, sprite: sprite);
            if (logDecisions)
            {
                Debug.Log(
                    $"{VaultDoorLogPrefix} RefreshOverlay PAINT cell=({instance.Cell.x},{instance.Cell.y}) " +
                    $"doorId={instance.DoorId} state={instance.State} orient={instance.Orientation} " +
                    $"sprite={(sprite != null ? sprite.name : "null")} placeholder={usedPlaceholder} " +
                    $"overlay={doorOverlayMap.name} hasTile={doorOverlayMap.HasTile(instance.Cell)}");
            }

            return true;
        }

        static bool IsCellVisibleToPlayer(Vector3Int cell) =>
            IsCellVisibleToPlayer(cell, out _);

        static bool IsCellVisibleToPlayer(Vector3Int cell, out bool visibilityManagerFound)
        {
            VisibilityManager visibility = UnityEngine.Object.FindAnyObjectByType<VisibilityManager>();
            visibilityManagerFound = visibility != null;
            if (visibility == null)
                return true;

            return visibility.IsVisible(cell);
        }

        void EnsureOverlayMap()
        {
            if (doorOverlayMap != null)
                return;

            if (MapManager.Instance != null)
            {
                doorOverlayMap = MapManager.Instance.DoorOverlayMap;
                if (doorOverlayMap != null)
                    return;
            }

            GameObject grid = GameObject.Find("Grid");
            if (grid == null)
                return;

            Transform existing = grid.transform.Find("Door_Overlay");
            if (existing != null)
            {
                doorOverlayMap = existing.GetComponent<Tilemap>();
                return;
            }

            var overlayGo = new GameObject("Door_Overlay");
            overlayGo.transform.SetParent(grid.transform, false);
            doorOverlayMap = overlayGo.AddComponent<Tilemap>();
            var renderer = overlayGo.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 5;
        }
    }
}
