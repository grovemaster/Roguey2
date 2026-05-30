namespace JRogue.Spawn
{
    public enum EnemySpawnPlacementPolicy
    {
        /// <summary>
        /// Try <see cref="EnemySpawnDefinition.primaryOffset"/> from origin (default north),
        /// then nearest walkable unoccupied anchor that fits the prefab footprint.
        /// </summary>
        NorthOfOriginThenNearestUnoccupiedFloor = 0,

        /// <summary>Skip primary offset; search outward from origin only.</summary>
        NearestUnoccupiedFloorFromOrigin = 1,
    }
}
