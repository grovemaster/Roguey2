using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Ability.Teleport
{
    [CreateAssetMenu(menuName = "JRogue/Abilities/Teleport")]
    public class TeleportAbility : AbilityAction
    {
        public override bool CanExecute(GameObject user) => true;

        protected override bool ExecuteCore(GameObject user) => false; // Requires target!

        // This is the one Milestone 16 uses:
        // public override bool Execute(GameObject user, Vector3Int targetTile)
        // {
        //     // Add your logic to move the player to targetTile
        //     user.transform.position = targetTile;
        //     return true;
        // }

        // public override bool Execute(GameObject user, Vector3Int targetTile)
        // {
        //     // For 'Self' Teleport, the user is the target
        //     IBattleTarget target = user.GetComponent<IBattleTarget>();

        //     if (target != null)
        //     {
        //         target.ApplyPositionChange(targetTile);
        //         return true;
        //     }
        //     return false;
        // }

        // public override bool Execute(GameObject user, Vector3Int targetTile)
        // {
        //     IBattleTarget target = user.GetComponent<IBattleTarget>();

        //     if (target != null)
        //     {
        //         target.ApplyPositionChange(targetTile);

        //         // ADDED: Realign history so the next 'Rush' knows where the leader is
        //         if (user.TryGetComponent<BaseActor>(out var actor))
        //         {
        //             if (PartyManager.Instance.GetActiveMember() == actor)
        //             {
        //                 PartyManager.Instance.SnapHistoryToCurrentPositions();
        //                 Debug.Log("[TELEPORT] Leader teleported. History snapped to new location.");
        //             }
        //         }
        //         return true;
        //     }
        //     return false;
        // }

        protected override bool ExecuteCore(GameObject user, Vector3Int targetTile)
        {
            IBattleTarget target = user.GetComponent<IBattleTarget>();

            if (target != null)
            {
                // 1. Move the actor physically and update the GridManager
                target.ApplyPositionChange(targetTile);

                // 2. Identify the actor to check party status[cite: 5]
                if (user.TryGetComponent<BaseActor>(out var actor))
                {
                    // Only snap if the actor is the leader and formation is active[cite: 2, 5]
                    if (PartyManager.Instance.GetActiveMember() == actor && PartyManager.Instance.IsFormationActive)
                    {
                        // Snap ensures history[0] is the new teleport tile and others are current positions
                        PartyManager.Instance.SnapHistoryToCurrentPositions();
                        Debug.Log($"[TELEPORT] {actor.name} (Leader) teleported. History snapped to {targetTile}.");
                    }

                    // 3. IMPORTANT: Inform the TurnManager that this actor's action is spent
                    // This prevents a 'Rush' from moving the caster again in the same turn.
                    JRogue.Manager.Turn.TurnManager.Instance.OnPlayerActionComplete(user);
                }

                return true;
            }
            return false;
        }
    }
}