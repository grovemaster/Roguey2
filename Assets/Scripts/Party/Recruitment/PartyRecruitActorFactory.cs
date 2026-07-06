using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.Organizations;
using JRogue.View;
using UnityEngine;

namespace JRogue.Party.Recruitment
{
  public static class PartyRecruitActorFactory
  {
    public static BaseActor Create(
      PartyRecruitDefinition recruit,
      Transform parent,
      OrganizationDefinition guild)
    {
      if (recruit?.actorPrefab == null)
        return null;

      GameObject instance = Object.Instantiate(recruit.actorPrefab, parent);
      GridMover mover = instance.GetComponent<GridMover>();
      if (mover != null)
        mover.enabled = false;

      PlayerRaceWorldSpriteApplier.Apply(instance);

      BaseActor actor = instance.GetComponent<BaseActor>();
      if (actor == null)
      {
        Object.Destroy(instance);
        return null;
      }

      if (!string.IsNullOrWhiteSpace(recruit.displayName))
        actor.SetDisplayName(recruit.displayName);

      OrganizationMembershipRuntime membership = OrganizationMembershipRuntime.EnsureOn(instance);
      membership.EnsureMembership(guild, recruit.guildRank);

      EssenceSlotManager essenceSlots = instance.GetComponent<EssenceSlotManager>();
      if (essenceSlots != null && recruit.essences != null)
      {
        for (int i = 0; i < recruit.essences.Length; i++)
        {
          EssenceData essence = recruit.essences[i];
          if (essence != null)
            essenceSlots.EquipEssence(essence, i);
        }
      }

      return actor;
    }
  }
}
