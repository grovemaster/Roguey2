namespace JRogue.World.Generation.Zones
{
    public static class ZoneGenerationRng
    {
        public static System.Random CreateZoneSelectionRng(int runSeed, string floorId) =>
            new System.Random(unchecked(runSeed * 397 ^ (floorId != null ? floorId.GetHashCode() : 0) ^ 0x5A0E));

        public static System.Random CreateZoneFillRng(int runSeed, string floorId, string pieceId) =>
            new System.Random(unchecked(
                runSeed * 397
                ^ (floorId != null ? floorId.GetHashCode() : 0)
                ^ (pieceId != null ? pieceId.GetHashCode() : 0)
                ^ 0xF117));
    }
}
