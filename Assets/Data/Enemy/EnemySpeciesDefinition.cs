using JRogue.Data.Door;
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

        [Header("Soul Power")]
        [Tooltip("Regen rate per enemy turn. < 0 uses global default (0.5).")]
        public float soulPowerRegenRate = -1f;

        [Header("Doors")]
        public EnemyDoorCapability doorCapability;
    }
}
