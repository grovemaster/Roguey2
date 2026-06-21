using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>Syncs equipped light-source items to <see cref="LightingService"/> carried emitters.</summary>
    public static class PartyLightEmitterBridge
    {
        public static void RefreshParty()
        {
            LightingService lighting = LightingService.Instance;
            if (lighting == null)
                return;

            var desired = new Dictionary<string, LightingService.CarriedEmitterEntry>();
            PartyManager party = PartyManager.Instance;
            if (party != null && party.partyMembers != null)
            {
                for (int i = 0; i < party.partyMembers.Count; i++)
                    CollectMemberEmitters(party.partyMembers[i], desired);
            }

            lighting.SyncCarriedEmitters(desired);
        }

        public static void RefreshMember(BaseActor actor)
        {
            RefreshParty();
        }

        static void CollectMemberEmitters(BaseActor actor, Dictionary<string, LightingService.CarriedEmitterEntry> desired)
        {
            if (actor == null || desired == null)
                return;

            EquipmentManager equipment = actor.GetComponent<EquipmentManager>();
            if (equipment == null)
                return;

            if (actor.stats == null || actor.stats.currentHP <= 0)
                return;

            Vector3Int cell = actor.GridPosition;

            foreach (KeyValuePair<EquipmentSlot, ItemInstance> pair in equipment.EquippedSnapshot)
            {
                ItemInstance instance = pair.Value;
                if (instance?.Definition is not LightSourceItemData lightSource
                    || lightSource.emitterDefinition == null
                    || !LightSourceItemRules.ShouldEmitCarriedLight(instance, pair.Key, isEquipped: true))
                {
                    continue;
                }

                string emitterId = BuildEmitterId(instance);
                desired[emitterId] = new LightingService.CarriedEmitterEntry(
                    cell,
                    lightSource.emitterDefinition,
                    lightSource.emitterDefinition.BaseEmissionMax);
            }
        }

        static string BuildEmitterId(ItemInstance instance) => $"carried:{instance.Id}";

        public static bool AnyMemberHasActiveCarriedEmitter()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers == null)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor actor = party.partyMembers[i];
                if (actor == null || actor.stats == null || actor.stats.currentHP <= 0)
                    continue;

                EquipmentManager equipment = actor.GetComponent<EquipmentManager>();
                if (equipment == null)
                    continue;

                foreach (KeyValuePair<EquipmentSlot, ItemInstance> pair in equipment.EquippedSnapshot)
                {
                    ItemInstance instance = pair.Value;
                    if (instance?.Definition is LightSourceItemData
                        && LightSourceItemRules.ShouldEmitCarriedLight(instance, pair.Key, isEquipped: true))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
