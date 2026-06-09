using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Item;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>Central gate for gameplay safe-zone rules on the active floor.</summary>
    public static class SafeZonePolicyService
    {
        public const string LogPrefix = "[SafeZone]";
        public const string DenyMessage = "You can't do that here.";

        public static FloorCombatPolicy GetPolicyAt(Vector3Int worldCell)
        {
            DungeonFloorInstance instance = DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance();
            DungeonFloorDefinition def = instance?.Definition;
            if (def == null)
                return FloorCombatPolicy.Normal;

            return SafeZonePolicyLogic.ResolvePolicyAt(
                def.CombatPolicy,
                def.SafeZoneRegions,
                worldCell);
        }

        public static bool IsSafeZoneAt(Vector3Int cell) =>
            SafeZonePolicyLogic.IsSafeZone(GetPolicyAt(cell));

        public static bool IsSafeZoneForActiveParty()
        {
            BaseActor leader = PartyManager.Instance?.GetActiveMember();
            return leader != null && IsSafeZoneAt(leader.GridPosition);
        }

        public static bool TryAllowEssenceAbility(out string denyReason, bool logDeny = true)
        {
            denyReason = null;
            if (!IsSafeZoneForActiveParty())
                return true;

            denyReason = DenyMessage;
            if (logDeny)
                LogDeny("essence ability");
            return false;
        }

        public static bool TryAllowHostileAction(out string denyReason, bool logDeny = true)
        {
            denyReason = null;
            if (!IsSafeZoneForActiveParty())
                return true;

            denyReason = DenyMessage;
            if (logDeny)
                LogDeny("hostile action");
            return false;
        }

        public static bool TryAllowInventoryUse(ItemData item, out string denyReason, bool logDeny = true)
        {
            denyReason = null;
            if (!IsSafeZoneForActiveParty())
                return true;

            if (SafeZonePolicyLogic.IsUtilityInventoryUse(item))
                return true;

            denyReason = DenyMessage;
            if (logDeny)
                LogDeny($"inventory use ({item?.itemName ?? "item"})");
            return false;
        }

        public static bool IsUtilityInventoryUse(ItemData item) =>
            SafeZonePolicyLogic.IsUtilityInventoryUse(item);

        public static bool IsProtectedTarget(BaseActor actor)
        {
            if (actor == null || actor is not NpcController)
                return false;

            return IsSafeZoneAt(actor.GridPosition);
        }

        public static bool ShouldSuppressPlayerDamage(GameObject target, GameObject damageSource)
        {
            if (target == null || damageSource == null || !IsPartyMember(damageSource))
                return false;

            BaseActor targetActor = target.GetComponent<BaseActor>();
            if (!IsProtectedTarget(targetActor))
                return false;

            Debug.Log($"{LogPrefix} Damage suppressed on {target.name} from party action.");
            return true;
        }

        public static void LogDeny(string context)
        {
            if (string.IsNullOrEmpty(context))
                Debug.Log($"{LogPrefix} {DenyMessage}");
            else
                Debug.Log($"{LogPrefix} {DenyMessage} ({context})");
        }

        static bool IsPartyMember(GameObject actor)
        {
            if (actor == null)
                return false;

            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return false;

            BaseActor baseActor = actor.GetComponent<BaseActor>();
            return baseActor != null && party.partyMembers.Contains(baseActor);
        }
    }
}
