using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.GridFeatures;
using JRogue.Manager.Map;
using JRogue.Stats;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Traps
{
    public sealed class TrapService : MonoBehaviour
    {
        public static TrapService Instance { get; private set; }

        [SerializeField] Tilemap trapOverlayMap;

        readonly Dictionary<Vector3Int, TrapInstance> _floorTrapsByCell =
            new Dictionary<Vector3Int, TrapInstance>();

        readonly Dictionary<Vector3Int, TrapInstance> _wallTrapsByHost =
            new Dictionary<Vector3Int, TrapInstance>();

        readonly Dictionary<Vector3Int, List<TrapInstance>> _wallTrapsByTriggerCell =
            new Dictionary<Vector3Int, List<TrapInstance>>();

        readonly List<TrapInstance> _allInstances = new List<TrapInstance>();
        readonly List<Vector3Int> _triggerTileBuffer = new List<Vector3Int>(4);

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

        public void SetOverlayMap(Tilemap overlay)
        {
            trapOverlayMap = overlay;
            if (trapOverlayMap != null)
                GridOverlayPainter.ConfigureRenderer(trapOverlayMap);
        }

        public bool IsFloorTrapAt(Vector3Int cell) => _floorTrapsByCell.ContainsKey(cell);

        public bool IsVisibleFloorTrapAt(Vector3Int cell) =>
            _floorTrapsByCell.TryGetValue(cell, out TrapInstance instance)
            && instance.Definition != null
            && instance.Definition.placement == TrapPlacement.Floor
            && instance.IsVisibleToPlayer;

        public bool IsVisibleWallTrapTriggerAt(Vector3Int cell)
        {
            if (!_wallTrapsByTriggerCell.TryGetValue(cell, out List<TrapInstance> traps))
                return false;

            for (int i = 0; i < traps.Count; i++)
            {
                if (traps[i] != null && traps[i].IsVisibleToPlayer)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Visible floor trap that would still fire if entered.
        /// </summary>
        public bool IsDangerousVisibleFloorTrapAt(Vector3Int cell) =>
            _floorTrapsByCell.TryGetValue(cell, out TrapInstance instance)
            && WouldHarmOnEnter(instance);

        /// <summary>
        /// Visible wall-trap trigger tile where at least one trap would still fire if entered.
        /// </summary>
        public bool IsDangerousVisibleWallTrapTriggerAt(Vector3Int cell)
        {
            if (!_wallTrapsByTriggerCell.TryGetValue(cell, out List<TrapInstance> traps))
                return false;

            for (int i = 0; i < traps.Count; i++)
            {
                if (WouldHarmOnEnter(traps[i]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Revealed traps that would still harm on entry — party pathing and move confirm use this.
        /// </summary>
        public bool IsPathingAvoidCell(Vector3Int cell) =>
            IsDangerousVisibleFloorTrapAt(cell) || IsDangerousVisibleWallTrapTriggerAt(cell);

        public bool RequiresEnterConfirm(Vector3Int cell) => IsPathingAvoidCell(cell);

        public bool TryGetEnterConfirmTrap(Vector3Int cell, out TrapInstance instance)
        {
            if (_floorTrapsByCell.TryGetValue(cell, out TrapInstance floor)
                && WouldHarmOnEnter(floor))
            {
                instance = floor;
                return true;
            }

            if (_wallTrapsByTriggerCell.TryGetValue(cell, out List<TrapInstance> traps))
            {
                for (int i = 0; i < traps.Count; i++)
                {
                    TrapInstance trap = traps[i];
                    if (WouldHarmOnEnter(trap))
                    {
                        instance = trap;
                        return true;
                    }
                }
            }

            instance = null;
            return false;
        }

        static bool WouldHarmOnEnter(TrapInstance instance) =>
            instance != null
            && instance.IsVisibleToPlayer
            && instance.Definition != null
            && instance.CanFire();

        public bool TryGetFloorTrap(Vector3Int cell, out TrapInstance instance) =>
            _floorTrapsByCell.TryGetValue(cell, out instance);

        public bool TryGetWallTrap(Vector3Int hostCell, out TrapInstance instance) =>
            _wallTrapsByHost.TryGetValue(hostCell, out instance);

        public IReadOnlyList<TrapInstance> GetWallTrapsTriggeredBy(Vector3Int triggerCell)
        {
            if (_wallTrapsByTriggerCell.TryGetValue(triggerCell, out List<TrapInstance> traps))
                return traps;

            return System.Array.Empty<TrapInstance>();
        }

        public void Register(Vector3Int hostCell, TrapDefinition definition)
        {
            if (definition == null || definition.trapId == TrapId.None)
                return;

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                Debug.LogWarning("[Trap] Cannot register without MapManager.");
                return;
            }

            if (definition.placement == TrapPlacement.Floor)
            {
                if (!map.IsWalkable(hostCell))
                {
                    Debug.LogWarning($"[Trap] Floor trap {definition.displayName} rejected at {hostCell}: not walkable.");
                    return;
                }

                if (_floorTrapsByCell.ContainsKey(hostCell))
                {
                    Debug.LogWarning($"[Trap] Floor trap already registered at {hostCell}.");
                    return;
                }
            }
            else
            {
                if (!map.IsWall(hostCell))
                {
                    Debug.LogWarning($"[Trap] Wall trap {definition.displayName} rejected at {hostCell}: not a wall.");
                    return;
                }

                if (TrapWallTopology.IsCornerWall(hostCell, map))
                {
                    Debug.LogWarning($"[Trap] Wall trap {definition.displayName} rejected at {hostCell}: corner wall.");
                    return;
                }

                if (_wallTrapsByHost.ContainsKey(hostCell))
                {
                    Debug.LogWarning($"[Trap] Wall trap already registered at {hostCell}.");
                    return;
                }
            }

            var instance = new TrapInstance(definition, hostCell);
            _allInstances.Add(instance);

            if (definition.placement == TrapPlacement.Floor)
            {
                _floorTrapsByCell[hostCell] = instance;
            }
            else
            {
                _wallTrapsByHost[hostCell] = instance;
                TrapWallTopology.CollectTriggerTiles(
                    hostCell,
                    definition.triggerRange,
                    map,
                    _triggerTileBuffer);

                for (int i = 0; i < _triggerTileBuffer.Count; i++)
                    AddWallTriggerMapping(_triggerTileBuffer[i], instance);
            }

            RefreshOverlayVisual(instance);
            Debug.Log($"[Trap] Registered {definition.displayName} at {hostCell} ({definition.placement}).");
        }

        public void EvaluateDetection()
        {
            MapManager map = MapManager.Instance;
            if (map == null)
                return;

            for (int i = 0; i < _allInstances.Count; i++)
            {
                TrapInstance instance = _allInstances[i];
                if (instance == null
                    || instance.Definition == null
                    || instance.Definition.initialVisibility != TrapVisibility.Invisible
                    || instance.IsDetected
                    || instance.IsRevealed)
                {
                    continue;
                }

                if (!PartySkillDetection.CanAnyPartyMemberMeetSkillThreshold(
                        SkillType.Perception,
                        instance.Definition.detectionThreshold,
                        requireLineOfSight: false,
                        instance.HostCell,
                        map))
                {
                    continue;
                }

                instance.IsDetected = true;
                RefreshOverlayVisual(instance);
                Debug.Log(
                    $"[Trap] {instance.Definition.displayName} at {instance.HostCell} detected (Perception).");
            }
        }

        public void NotifyActorEntered(BaseActor actor)
        {
            if (actor == null)
                return;

            EvaluateDetection();
            Vector3Int cell = actor.GridPosition;
            TryTriggerFloorTrap(actor, cell);
            TryTriggerWallTraps(actor, cell);
        }

        public void TryTriggerFloorTrap(BaseActor actor, Vector3Int cell)
        {
            if (!_floorTrapsByCell.TryGetValue(cell, out TrapInstance instance))
                return;

            FireTrap(instance, actor, cell);
        }

        public void TryTriggerWallTraps(BaseActor actor, Vector3Int triggerCell)
        {
            if (!_wallTrapsByTriggerCell.TryGetValue(triggerCell, out List<TrapInstance> traps))
                return;

            for (int i = 0; i < traps.Count; i++)
                FireTrap(traps[i], actor, triggerCell);
        }

        void FireTrap(TrapInstance instance, BaseActor actor, Vector3Int triggerCell)
        {
            if (instance == null || actor == null || !instance.CanFire())
                return;

            TrapDefinition def = instance.Definition;
            if (def == null)
                return;

            instance.HasTriggered = true;

            if (def.triggerLimit == TrapTriggerLimit.Finite)
                instance.ChargesRemaining = Mathf.Max(0, instance.ChargesRemaining - 1);

            RefreshOverlayVisual(instance);

            HealthComponent health = actor.GetComponent<HealthComponent>();
            if (health != null && def.piercingDamage > 0)
            {
                health.TakeDamage(def.piercingDamage, DamageType.Pierce, gameObject);
                Debug.Log(
                    $"[Trap] {def.displayName} triggered by {actor.DisplayName} at {triggerCell} for {def.piercingDamage} Pierce.");
            }
            else if (def.piercingDamage > 0)
            {
                Debug.Log(
                    $"[Trap] {def.displayName} triggered by {actor.DisplayName} at {triggerCell} (no HealthComponent).");
            }
        }

        void AddWallTriggerMapping(Vector3Int triggerCell, TrapInstance instance)
        {
            if (!_wallTrapsByTriggerCell.TryGetValue(triggerCell, out List<TrapInstance> list))
            {
                list = new List<TrapInstance>(1);
                _wallTrapsByTriggerCell[triggerCell] = list;
            }

            if (!list.Contains(instance))
                list.Add(instance);
        }

        void RefreshOverlayVisual(TrapInstance instance)
        {
            if (trapOverlayMap == null || instance?.Definition == null)
                return;

            if (!instance.IsRevealed)
            {
                GridOverlayPainter.Clear(trapOverlayMap, instance.HostCell);
                return;
            }

            TrapDefinition def = instance.Definition;
            Sprite sprite = def.revealedSprite;
            if (instance.HasTriggered && def.revealedTriggeredSprite != null)
                sprite = def.revealedTriggeredSprite;

            GridOverlayPainter.Paint(
                trapOverlayMap,
                instance.HostCell,
                tile: null,
                sprite: sprite);
        }

        void EnsureOverlayMap()
        {
            if (trapOverlayMap != null)
            {
                GridOverlayPainter.ConfigureRenderer(trapOverlayMap);
                return;
            }

            if (MapManager.Instance != null && MapManager.Instance.TrapOverlayMap != null)
            {
                trapOverlayMap = MapManager.Instance.TrapOverlayMap;
                GridOverlayPainter.ConfigureRenderer(trapOverlayMap);
                return;
            }

            GameObject grid = GameObject.Find("Grid");
            if (grid == null)
                return;

            Transform existing = grid.transform.Find("Trap_Overlay");
            if (existing != null)
            {
                trapOverlayMap = existing.GetComponent<Tilemap>();
                GridOverlayPainter.ConfigureRenderer(trapOverlayMap);
                return;
            }

            var overlayGo = new GameObject("Trap_Overlay");
            overlayGo.transform.SetParent(grid.transform, false);
            trapOverlayMap = overlayGo.AddComponent<Tilemap>();
            overlayGo.AddComponent<TilemapRenderer>();
            GridOverlayPainter.ConfigureRenderer(trapOverlayMap);
        }
    }
}
