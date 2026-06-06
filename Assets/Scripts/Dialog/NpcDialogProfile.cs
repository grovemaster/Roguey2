using UnityEngine;

namespace JRogue.Dialog
{
    public static class TownNpcStoryFlags
    {
        public const string TalkedNpc1 = "talked_npc_1";
        public const string TalkedNpc2 = "talked_npc_2";
    }

    public static class TownNpcIds
    {
        public const string Npc1 = "town_npc_1";
        public const string Npc2 = "town_npc_2";
        public const string Npc3 = "town_npc_3";
        public const string Npc4 = "town_npc_4";
        public const string Npc5 = "town_npc_5";
    }

    [CreateAssetMenu(fileName = "NpcDialogProfile", menuName = "JRogue/Dialog/NPC Dialog Profile")]
    public sealed class NpcDialogProfile : ScriptableObject
    {
        public string npcId;
        public int rootNodeIndex = DialogGraph.NoNode;
        public DialogNodeData[] nodes = System.Array.Empty<DialogNodeData>();
        public string completionFlagId;
        public bool incrementTalkCountOnStart = true;
    }
}
