using JRogue.Data.Enemy;
using JRogue.Spawn;
using UnityEngine;

namespace JRogue.World.LotF
{
    [CreateAssetMenu(
        fileName = "LordOfTheFloor",
        menuName = "JRogue/World/Lord of the Floor Definition")]
    public sealed class LordOfTheFloorDefinition : ScriptableObject
    {
        [SerializeField] string lotfId = "lotf_giant_skeleton_king";
        [SerializeField] string displayName = "Giant Skeleton King";
        [SerializeField] string title = "Lord of Giant Skeletons";
        [SerializeField] string hostFloorId = "dungeon_floor_01";
        [SerializeField] EnemySpeciesDefinition species;
        [SerializeField] EnemySpawnDefinition spawnDefinition;
        [SerializeField] int minimumDungeonDay = LordOfTheFloorSummonGateLogic.DefaultMinimumDungeonDay;
        [SerializeField] int minimumLivingPartyMembers =
            LordOfTheFloorSummonGateLogic.DefaultMinimumLivingPartyMembers;

        public string LotfId => lotfId;
        public string DisplayName => displayName;
        public string Title => title;
        public string HostFloorId => hostFloorId;
        public EnemySpeciesDefinition Species => species;
        public EnemySpawnDefinition SpawnDefinition => spawnDefinition;
        public int MinimumDungeonDay => minimumDungeonDay;
        public int MinimumLivingPartyMembers => minimumLivingPartyMembers;

        public string AppearanceCombatLogLine =>
            $"The {displayName}, {title}, has appeared!";

        public string AppearanceExamineName =>
            string.IsNullOrWhiteSpace(title) ? displayName : $"{displayName}, {title}";
    }
}
