using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.View;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Places the surviving DDOL party in town after a forced dungeon exit.
    /// </summary>
    public static class TownArrivalService
    {
        const string LogPrefix = "[TownArrival]";

        public static bool TryCompleteArrival(
            DungeonFloorInstanceManager manager,
            string townFloorId,
            int runSeed)
        {
            if (manager == null)
            {
                Debug.LogError($"{LogPrefix} Missing floor manager.");
                return false;
            }

            if (!manager.TryBeginRunAtFloor(townFloorId, runSeed))
            {
                Debug.LogError($"{LogPrefix} Failed to activate town floor '{townFloorId}'.");
                return false;
            }

            TownTimeService.EnsureRunService();
            TownTimeService.Instance?.ApplyDungeonReturnPhase();

            TurnManager turn = TurnManager.Instance;
            if (turn != null)
                turn.currentState = GameState.PLAYER_TURN;

            EnsurePlayCamera();
            Debug.Log($"{LogPrefix} Party arrived in town (survivors from dungeon exit).");
            return true;
        }

        static void EnsurePlayCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            camera.orthographic = true;
            if (camera.orthographicSize < 8f)
                camera.orthographicSize = 12f;

            if (camera.GetComponent<CameraFollow>() == null)
                camera.gameObject.AddComponent<CameraFollow>();

            PartyManager.Instance?.RefreshCameraFollow();
        }
    }
}
