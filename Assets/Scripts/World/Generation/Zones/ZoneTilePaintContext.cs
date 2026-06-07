namespace JRogue.World.Generation.Zones
{
    public readonly struct ZoneTilePaintContext
    {
        public int RunSeed { get; }
        public string FloorId { get; }
        public int FloorSalt { get; }

        public ZoneTilePaintContext(int runSeed, string floorId, int floorSalt)
        {
            RunSeed = runSeed;
            FloorId = floorId;
            FloorSalt = floorSalt;
        }

        public static ZoneTilePaintContext From(int runSeed, string floorId) =>
            new ZoneTilePaintContext(runSeed, floorId, floorId != null ? floorId.GetHashCode() : 0);

        public static ZoneTilePaintContext From(DungeonGenerationContext context)
        {
            if (context == null)
                return default;

            string floorId = context.Definition != null ? context.Definition.FloorId : null;
            return From(context.RunSeed, floorId);
        }
    }
}
