using JRogue.Ability;
using JRogue.Actors;
using JRogue.Input;
using JRogue.Item;

namespace JRogue.UI.Hotbar
{
    public struct HotbarResolvedAction
    {
        public bool IsValid;
        public bool IsStale;
        public AbilityAction Ability;
        public PlayerAbilitySource Source;
        public int SlotIndex;
        public int AbilityIndex;
        public ItemInstance ItemInstance;
        public BaseActor ItemOwner;
        public string DenyReason;
        public HotbarEntryKind Kind;
        public string RacialBindingKey;
        public string ContractInstanceId;
        public string KnightNodeId;
    }
}
