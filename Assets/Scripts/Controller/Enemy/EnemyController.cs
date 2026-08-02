using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Controller.Player;
using JRogue.Core.Actor;
using JRogue.Data.Enemy;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Loot;
using JRogue.Manager.Progression;
using JRogue.Hazards;
using JRogue.Racial;
using JRogue.World.MapPresence;
using JRogue.Status;
using Roguey2.Sensing;
using UnityEngine;

namespace JRogue.Controller.Enemy
{
    [DefaultExecutionOrder(0)]
    public class EnemyController : BaseActor, IGridFootprint
    {
        public int hp = 3;
        public int attackPower = 1;

        [Header("Footprint")]
        public FootprintLayout footprintLayout = FootprintLayout.Rectangle;
        [Min(1)] public int footprintWidth = 1;
        [Min(1)] public int footprintHeight = 1;

        [Header("Species & XP")]
        [SerializeField] EnemySpeciesDefinition species;

        public EnemySpeciesDefinition Species => species;

        [Header("Melee profiles")]
        public List<EnemyAttackProfileKind> attackProfiles = new List<EnemyAttackProfileKind>();

        [Header("Acoustics")]
        [SerializeField, Min(0)] private int meleeNoiseVolume = 5;

        [Header("Sight")]
        [SerializeField, Min(1)] private int visionRange = 8;
        [SerializeField, Range(0f, 180f)] private float primaryConeAngle = 135f;
        [SerializeField, Range(0.1f, 1f)] private float peripheralRangeMultiplier = 0.5f;

        public int VisionRange => visionRange;
        public float PrimaryConeAngle => primaryConeAngle;
        public float PeripheralRangeMultiplier => peripheralRangeMultiplier;

        FootprintLayout IGridFootprint.Layout => footprintLayout;
        int IGridFootprint.FootprintWidth => footprintWidth;
        int IGridFootprint.FootprintHeight => footprintHeight;
        FacingDirection IGridFootprint.Facing => currentFacing;

        /// <summary>For <see cref="EnemyAiBrain"/> pathing and LOS (same assembly).</summary>
        internal MapManager BrainMapManager => mapManager;

        private PlayerController player;
        private EnemyAiBrain brain;

        /// <summary>
        /// Empty/null <see cref="attackProfiles"/> defaults to <see cref="EnemyAttackProfileKind.AdjacentSingle"/>
        /// (most enemies). A non-empty list is authoritative — omit single there for rare exceptions.
        /// </summary>
        public bool HasAttackProfile(EnemyAttackProfileKind kind)
        {
            if (attackProfiles == null || attackProfiles.Count == 0)
                return kind == EnemyAttackProfileKind.AdjacentSingle;

            for (int i = 0; i < attackProfiles.Count; i++)
            {
                if (attackProfiles[i] == kind)
                    return true;
            }

            return false;
        }

        public void GetOccupiedCells(List<Vector3Int> buffer) =>
            GridFootprintUtility.GetOccupiedCells(this, buffer);

        public bool Occupies(Vector3Int cell) =>
            GridFootprintUtility.Occupies(this, cell);

        bool UsesFootprintVisual =>
            !GridFootprintUtility.IsSingleCell(footprintLayout, footprintWidth, footprintHeight);

        protected override void Awake()
        {
            if (UsesFootprintVisual)
                EnsureFootprintVisualChild();
            base.Awake();
            brain = GetComponent<EnemyAiBrain>();
            if (brain == null)
                brain = gameObject.AddComponent<EnemyAiBrain>();
            brain.Bind(this);
        }

        protected override void Start()
        {
            base.Start();
            player = FindAnyObjectByType<PlayerController>();
        }

        internal void BrainEnsureManagers() => EnsureManagers();

        internal bool ComputePlayerVisible(PlayerController playerController, out ConeVisionZone zone)
        {
            zone = ConeVisionZone.None;
            if (playerController == null || mapManager == null)
                return false;

            return ConeSightUtility.TrySenseTarget(
                this,
                playerController.GridPosition,
                mapManager,
                visionRange,
                primaryConeAngle,
                peripheralRangeMultiplier,
                out zone);
        }

        public void TakeTurn()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (player == null)
                return;

            HazardService.Instance?.TickOccupancyOnEnemyTurnStart(this);
            SoulPowerRegenerationService.TickRegeneration(gameObject);
            RacialPassiveHooks.NotifyTurnStart(gameObject);
            essenceManager?.NotifyTurnStart();
            GetComponent<StatusEffectController>()?.TickStatuses();
            brain.ExecuteTurn(player);
        }

        public override void OnHearNoise(BaseActor source, Vector3Int origin, int rawVolume, int effectiveVolume)
        {
            Debug.Log(
                $"[SENSE-HEARING] {name} heard noise of volume {rawVolume} from ({origin.x},{origin.y}). Effective Volume at Enemy: {effectiveVolume}.");
            brain.NotifyHeard(origin, rawVolume, effectiveVolume);
        }

        internal void BrainAttackPlayer() => AttackPlayer();

        private void AttackPlayer()
        {
            PartyManager party = PartyManager.Instance ?? partyManager;
            if (party != null && EnemyMeleeCombat.TryExecuteMeleeAttack(this, party))
            {
                ProduceNoise(meleeNoiseVolume);
                return;
            }

            if (party != null && EnemyMeleeCombat.TryExecuteFallbackMelee(this, party))
            {
                ProduceNoise(meleeNoiseVolume);
                return;
            }

            Debug.LogWarning($"[ENEMY-MELEE] {name} attack turn did not damage any party member.");
            ProduceNoise(meleeNoiseVolume);
        }

        protected override void Die()
        {
            GetComponent<MonsterMapPresenceHost>()?.Unbind();

            PartyManager party = PartyManager.Instance ?? partyManager;
            GameObject killer = PartyExperienceService.ResolveKillCredit(health, party);
            PartyExperienceService.Instance?.HandleEnemyDeath(this, killer);

            EnemyLootService.Instance?.SpawnDeathLoot(this);

            string speciesId = species != null ? species.speciesId : null;
            BaseActor killerActor = killer != null ? killer.GetComponent<BaseActor>() : null;
            JRogue.Quest.QuestService.Instance?.NotifyEnemyKilled(speciesId, killerActor);

            if (gridManager != null)
                gridManager.UnregisterFootprint(this);
            Debug.Log($"{gameObject.name} was defeated!");
            Destroy(gameObject);
        }

        void EnsureFootprintVisualChild()
        {
            if (FootprintPoseUtility.FindVisual(transform) != null)
                return;

#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
                return;
#endif

            SpriteRenderer rootSprite = GetComponent<SpriteRenderer>();
            if (rootSprite == null)
                return;

            var child = new GameObject(FootprintPoseUtility.VisualChildName);
            child.transform.SetParent(transform, false);

            SpriteRenderer childSprite = child.AddComponent<SpriteRenderer>();
            CopySpriteRenderer(rootSprite, childSprite);
            FootprintPoseUtility.ApplyVisual(
                GridFootprintUtility.ResolvePlacementAnchor(transform.position, this),
                footprintLayout,
                footprintWidth,
                footprintHeight,
                currentFacing,
                transform);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Undo.RegisterCreatedObjectUndo(child, "Create Footprint Visual");
                UnityEditor.Undo.DestroyObjectImmediate(rootSprite);
            }
            else
#endif
                Destroy(rootSprite);
        }

        static void CopySpriteRenderer(SpriteRenderer from, SpriteRenderer to)
        {
            to.sprite = from.sprite;
            to.color = from.color;
            to.flipX = from.flipX;
            to.flipY = from.flipY;
            to.sortingLayerID = from.sortingLayerID;
            to.sortingOrder = from.sortingOrder;
            to.sharedMaterial = from.sharedMaterial;
        }

        void SyncFootprintPlacement()
        {
            if (!UsesFootprintVisual)
                return;

            GridMover mover = GetComponent<GridMover>();
            Vector3Int anchor = GridFootprintUtility.ResolvePlacementAnchor(transform.position, this);
            if (mover != null)
            {
                mover.SetGridPosition(anchor);
                mover.SyncFootprintPose();
                return;
            }

            transform.position = GridFootprintUtility.IsSingleCell(footprintLayout, footprintWidth, footprintHeight)
                ? GridCellWorld.GetSingleCellActorPosition(anchor)
                : FootprintPoseUtility.GetRootWorldPosition(
                    anchor,
                    footprintLayout,
                    footprintWidth,
                    footprintHeight,
                    currentFacing);
            FootprintPoseUtility.ApplyVisual(anchor, footprintLayout, footprintWidth, footprintHeight, currentFacing, transform);
        }

#if UNITY_EDITOR
        static readonly HashSet<UnityEngine.EntityId> PendingEditorFootprintSync = new HashSet<UnityEngine.EntityId>();

        void OnValidate()
        {
            if (!UsesFootprintVisual)
                return;

            if (Application.isPlaying)
            {
                SyncFootprintPlacement();
                return;
            }

            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
                return;

            ScheduleEditorFootprintSync();
        }

        void ScheduleEditorFootprintSync()
        {
            UnityEngine.EntityId id = gameObject.GetEntityId();
            if (!PendingEditorFootprintSync.Add(id))
                return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                PendingEditorFootprintSync.Remove(id);
                if (this == null || !UsesFootprintVisual)
                    return;

                EnsureFootprintVisualChild();
                SyncFootprintPlacement();
            };
        }

        private void OnDrawGizmosSelected()
        {
            Vector3Int anchor = Application.isPlaying
                ? GridPosition
                : GridFootprintUtility.ResolvePlacementAnchor(transform.position, this);

            var cells = new List<Vector3Int>(8);
            GridFootprintUtility.GetOccupiedCells(
                anchor,
                footprintLayout,
                footprintWidth,
                footprintHeight,
                currentFacing,
                cells);
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.85f);
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 c = new Vector3(cells[i].x + 0.5f, cells[i].y + 0.5f, 0f);
                Gizmos.DrawWireCube(c, Vector3.one * 0.92f);
            }
        }
#endif
    }
}
