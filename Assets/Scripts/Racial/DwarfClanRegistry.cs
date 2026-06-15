using System;
using UnityEngine;

namespace JRogue.Racial
{
    public static class DwarfClanRegistry
    {
        public static DwarfClanDefinition TryLoadByClanId(string clanId)
        {
            if (string.IsNullOrWhiteSpace(clanId))
                return null;

            string trimmed = clanId.Trim();
            DwarfClanDefinition[] clans = Resources.LoadAll<DwarfClanDefinition>("Racial/Dwarf/Clans");
            if (clans == null || clans.Length == 0)
                return null;

            foreach (DwarfClanDefinition clan in clans)
            {
                if (clan != null && string.Equals(clan.clanId, trimmed, StringComparison.Ordinal))
                    return clan;
            }

            return null;
        }
    }
}
