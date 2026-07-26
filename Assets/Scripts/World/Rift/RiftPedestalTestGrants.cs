#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.World.Rift
{
    /// <summary>Dev/test grants for the Floor 1 Northern Dark rift pedestal offerings.</summary>
    public static class RiftPedestalTestGrants
    {
        public const string LogPrefix = "[Rift:TestGrant]";

        public static readonly string[] RequiredSpeciesIds =
        {
            "goblin",
            "ghoul",
            "dire_wolf",
        };

        /// <summary>Adds one stone of each pedestal species if the party is missing that stack.</summary>
        public static void EnsureOneOfEachRequiredSpecies()
        {
            PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
            if (ledger == null)
            {
                Debug.LogWarning($"{LogPrefix} No PartyManaStoneLedger — cannot grant pedestal stones.");
                return;
            }

            int granted = 0;
            for (int i = 0; i < RequiredSpeciesIds.Length; i++)
            {
                string speciesId = RequiredSpeciesIds[i];
                if (ledger.GetAmount(1, speciesId) > 0)
                    continue;

                ledger.Add(1, speciesId, 1);
                granted++;
            }

            if (granted > 0)
            {
                Debug.Log(
                    $"{LogPrefix} Ensured goblin + ghoul + dire wolf mana stones " +
                    $"(added {granted} missing stack(s)).");
            }
        }
    }
}
#endif
