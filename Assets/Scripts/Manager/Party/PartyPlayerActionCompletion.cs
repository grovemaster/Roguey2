using JRogue.Actors;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Turn;
using JRogue.Service.Formation;
using UnityEngine;

namespace JRogue.Manager.Party
{
    /// <summary>
    /// Ends the active party member's player action consistently for formation on/off
    /// (movement, abilities, inventory consumables).
    /// </summary>
    public static class PartyPlayerActionCompletion
    {
        public static void CompleteActiveMemberAction(BaseActor activeMember)
        {
            if (activeMember == null)
                return;

            TurnManager turn = TurnManager.Instance;
            if (turn == null || turn.currentState != GameState.PLAYER_TURN)
                return;

            PartyManager party = PartyManager.Instance;
            if (party != null && party.IsFormationActive)
            {
                party.RecordMemberMove(activeMember, activeMember.GridPosition);

                GridManager grid = GridManager.Instance;
                MapManager map = MapManager.Instance;
                if (grid != null && map != null)
                    FormationRushService.Rush(party, turn, grid, map);
                else
                    turn.ForceEndPlayerTurn();

                return;
            }

            turn.OnPlayerActionComplete(activeMember.gameObject);
        }
    }
}
