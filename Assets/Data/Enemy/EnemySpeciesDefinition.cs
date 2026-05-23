using UnityEngine;

namespace JRogue.Data.Enemy
{
    [CreateAssetMenu(fileName = "EnemySpecies", menuName = "JRogue/Enemy/Species Definition")]
    public class EnemySpeciesDefinition : ScriptableObject
    {
        [Tooltip("Stable id for journal / saves (e.g. skeleton, giant_skeleton).")]
        public string speciesId;

        public string displayName;

        [Min(0)]
        public int firstKillExperience = 25;

        [Header("Death loot")]
        public EnemyLootTable lootTable;
    }
}
