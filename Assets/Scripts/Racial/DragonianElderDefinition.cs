using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "DragonianElder", menuName = "JRogue/Racial/Dragonian Elder Definition")]
    public sealed class DragonianElderDefinition : ScriptableObject
    {
        public string elderId;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public string npcId;
        public string[] chainQuestIds = System.Array.Empty<string>();
        public string[] unlockStoryFlags = System.Array.Empty<string>();

        public string ResolvedElderId =>
            string.IsNullOrWhiteSpace(elderId) ? name : elderId.Trim();
    }
}
