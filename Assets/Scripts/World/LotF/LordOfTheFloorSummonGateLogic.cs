namespace JRogue.World.LotF
{
    /// <summary>Pure summon-gate evaluation for Lords of the Floor.</summary>
    public static class LordOfTheFloorSummonGateLogic
    {
        public const int DefaultMinimumDungeonDay = 3;
        public const int DefaultMinimumLivingPartyMembers = 4;

        public static bool Passes(
            int dungeonDay,
            int minimumDungeonDay,
            string activeFloorId,
            string hostFloorId,
            int livingPartyMembers,
            int minimumLivingPartyMembers,
            LordOfTheFloorRunSlot runSlot,
            out string failReason)
        {
            if (runSlot != LordOfTheFloorRunSlot.Available)
            {
                failReason = $"run slot is {runSlot}";
                return false;
            }

            if (dungeonDay < minimumDungeonDay)
            {
                failReason = $"day {dungeonDay} < {minimumDungeonDay}";
                return false;
            }

            if (string.IsNullOrEmpty(hostFloorId)
                || !string.Equals(activeFloorId, hostFloorId, System.StringComparison.Ordinal))
            {
                failReason = $"active floor '{activeFloorId}' is not host '{hostFloorId}'";
                return false;
            }

            if (livingPartyMembers < minimumLivingPartyMembers)
            {
                failReason = $"living party {livingPartyMembers} < {minimumLivingPartyMembers}";
                return false;
            }

            failReason = null;
            return true;
        }
    }
}
