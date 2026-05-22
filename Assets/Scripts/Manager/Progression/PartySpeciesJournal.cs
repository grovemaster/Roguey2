using System.Collections.Generic;

namespace JRogue.Manager.Progression
{
    /// <summary>Party-wide first-kill species ids (STBGB-style journal).</summary>
    public sealed class PartySpeciesJournal
    {
        readonly HashSet<string> _defeatedSpecies = new HashSet<string>();

        public bool HasDefeated(string speciesId) =>
            !string.IsNullOrEmpty(speciesId) && _defeatedSpecies.Contains(speciesId);

        /// <returns>True if this is the first time the party defeated this species.</returns>
        public bool TryRegisterFirstKill(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId))
                return false;
            return _defeatedSpecies.Add(speciesId);
        }

        public void Clear() => _defeatedSpecies.Clear();

        public IReadOnlyCollection<string> DefeatedSpecies => _defeatedSpecies;
    }
}
