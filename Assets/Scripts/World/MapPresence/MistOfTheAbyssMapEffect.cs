using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.MapPresence
{
    [CreateAssetMenu(
        fileName = "MistOfTheAbyssMapEffect",
        menuName = "JRogue/World/Map Presence/Mist of the Abyss")]
    public sealed class MistOfTheAbyssMapEffect : MonsterMapPresenceEffect
    {
        [Tooltip("Floor id this mist blankets while the host lives.")]
        public string hostFloorId = DungeonFloorTransitionIds.Floor01Id;

        public override void Apply(MonsterMapPresenceContext context)
        {
            string floorId = string.IsNullOrEmpty(hostFloorId)
                ? DungeonFloorTransitionIds.Floor01Id
                : hostFloorId;

            MistOfTheAbyssService.RegisterMist(floorId);
            Debug.Log(
                $"{MonsterMapPresenceService.LogPrefix} Apply MistOfTheAbyss on '{floorId}' for '{context.LogLabel}'.");

            string captured = floorId;
            context.RegisterRevert(() =>
            {
                MistOfTheAbyssService.UnregisterMist(captured);
                Debug.Log(
                    $"{MonsterMapPresenceService.LogPrefix} Revert MistOfTheAbyss on '{captured}'.");
            });
        }

        public override void Revert(MonsterMapPresenceContext context)
        {
            // Revert stack runs from context.RevertAll().
        }
    }
}
