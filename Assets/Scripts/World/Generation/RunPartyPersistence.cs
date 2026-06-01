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

        public static bool AwaitingTownArrival { get; private set; }

        public static bool HasLivingParty =>
            PartyManager.Instance != null
            && PartyManager.Instance.partyMembers != null
            && PartyManager.Instance.partyMembers.Count > 0;

        public static void MarkAwaitingTownArrival() => AwaitingTownArrival = true;

        public static bool ConsumeAwaitingTownArrival()
        {
            if (!AwaitingTownArrival)
                return false;

            AwaitingTownArrival = false;
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
    }
}
