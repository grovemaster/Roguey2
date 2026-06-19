namespace JRogue.World.Generation
{
    public enum FloorLayoutMode
    {
        PreBakedStamp = 0,
        ZoneComposite = 1,
        /// <summary>Tiles are painted on scene tilemaps; generation only spawns dynamics (portal, NPCs).</summary>
        ScenePainted = 2,
    }
}
