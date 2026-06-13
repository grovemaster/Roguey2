using System;

namespace JRogue.Quest
{
    public static class QuestInstanceKey
    {
        public const char Separator = '\u001f';

        public static string StorageKey(string questId, string ownerPartyMemberId)
        {
            string id = questId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ownerPartyMemberId))
                return id;

            return $"{id}{Separator}{ownerPartyMemberId.Trim()}";
        }

        public static bool TryParseStorageKey(string storageKey, out string questId, out string ownerPartyMemberId)
        {
            questId = null;
            ownerPartyMemberId = string.Empty;
            if (string.IsNullOrWhiteSpace(storageKey))
                return false;

            int separator = storageKey.IndexOf(Separator);
            if (separator < 0)
            {
                questId = storageKey.Trim();
                return !string.IsNullOrEmpty(questId);
            }

            questId = storageKey.Substring(0, separator).Trim();
            ownerPartyMemberId = storageKey.Substring(separator + 1).Trim();
            return !string.IsNullOrEmpty(questId);
        }
    }
}
