using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;

namespace JRogue.Organizations
{
    public sealed class EssenceSlotScoreContributor : IOrganizationRankScoreContributor
    {
        public static readonly EssenceSlotScoreContributor Instance = new EssenceSlotScoreContributor();

        public string OrganizationId => OrganizationIds.AdventurersGuild;

        public int Contribute(BaseActor actor)
        {
            if (actor == null)
                return 0;

            EssenceSlotManager slots = actor.GetComponent<EssenceSlotManager>();
            if (slots == null)
                return 0;

            int total = 0;
            for (int i = 0; i < slots.totalSlots; i++)
            {
                EssenceData essence = slots.GetEssenceInSlot(i);
                if (essence == null)
                    continue;

                int tier = UnityEngine.Mathf.Clamp(essence.tier, 1, 9);
                total += 10 - tier;
            }

            return total;
        }
    }
}
