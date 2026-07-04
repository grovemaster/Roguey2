using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Keeps the party on the DDOL run layer across dungeon → town scene loads.
    /// </summary>
    public static class RunPartyPersistence
    {
        public const string PartyObjectName = "Party";
        public const string InputObjectName = "InputSystem";

        public const string DefaultReturnTownSceneName = "TownTest";

        public static bool AwaitingTownArrival { get; private set; }
        public static bool EnteringDungeonFromTown { get; private set; }
        public static bool ForcedDungeonExpiryPending { get; private set; }

        static string _returnTownSceneName = DefaultReturnTownSceneName;

        public static string ReturnTownSceneName => _returnTownSceneName;

        public static bool HasLivingParty =>
            PartyManager.Instance != null
            && PartyManager.Instance.partyMembers != null
            && PartyManager.Instance.partyMembers.Count > 0;

        public static void MarkAwaitingTownArrival() => AwaitingTownArrival = true;

        public static void MarkForcedDungeonExpiryPending() => ForcedDungeonExpiryPending = true;

        public static void MarkEnteringDungeonFromTown() => EnteringDungeonFromTown = true;

        public static void SetReturnTownSceneName(string sceneName)
        {
            _returnTownSceneName = string.IsNullOrEmpty(sceneName)
                ? DefaultReturnTownSceneName
                : sceneName;
        }

        public static bool ConsumeEnteringDungeonFromTown()
        {
            if (!EnteringDungeonFromTown)
                return false;

            EnteringDungeonFromTown = false;
            return true;
        }

        public static bool ConsumeAwaitingTownArrival()
        {
            if (!AwaitingTownArrival)
                return false;

            AwaitingTownArrival = false;
            return true;
        }

        public static bool ConsumeForcedDungeonExpiryPending()
        {
            if (!ForcedDungeonExpiryPending)
                return false;

            ForcedDungeonExpiryPending = false;
            return true;
        }

        public static void EnsurePartySurvivesSceneLoad()
        {
            PartyManager party = PartyManager.Instance;
            if (party != null)
                Object.DontDestroyOnLoad(party.gameObject);

            GameObject partyRoot = GameObject.Find(PartyObjectName);
            if (partyRoot != null && partyRoot != party?.gameObject)
                Object.DontDestroyOnLoad(partyRoot);

            GameObject inputRoot = GameObject.Find(InputObjectName);
            if (inputRoot != null)
                Object.DontDestroyOnLoad(inputRoot);
        }

        internal static void ResetForTests()
        {
            AwaitingTownArrival = false;
            EnteringDungeonFromTown = false;
            ForcedDungeonExpiryPending = false;
            _returnTownSceneName = DefaultReturnTownSceneName;
        }
    }
}
