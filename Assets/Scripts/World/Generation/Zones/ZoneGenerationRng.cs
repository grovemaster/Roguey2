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

        public static System.Random CreatePopulationRng(int runSeed, string floorId) =>
            new System.Random(unchecked(
                runSeed * 397
                ^ (floorId != null ? floorId.GetHashCode() : 0)
                ^ 0x5EED));

        public static System.Random CreateZonePopulationRng(
            int runSeed,
            string floorId,
            string zoneInstanceId,
            string category) =>
            new System.Random(unchecked(
                runSeed * 397
                ^ (floorId != null ? floorId.GetHashCode() : 0)
                ^ (zoneInstanceId != null ? zoneInstanceId.GetHashCode() : 0)
                ^ (category != null ? category.GetHashCode() : 0)
                ^ 0x5EE1));
    }
}
