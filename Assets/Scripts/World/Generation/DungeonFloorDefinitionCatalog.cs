using UnityEngine;

namespace JRogue.World.Generation
{
    [CreateAssetMenu(fileName = "DungeonFloorDefinitionCatalog", menuName = "JRogue/World/Dungeon Floor Definition Catalog")]
    public sealed class DungeonFloorDefinitionCatalog : ScriptableObject
    {
        [SerializeField] DungeonFloorDefinition[] floors;

        public DungeonFloorDefinition[] Floors => floors;
    }
}
