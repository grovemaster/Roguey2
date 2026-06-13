using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.Racial
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static class PartyRacialDevSwapService
    {
        const string TieflingLoadoutResourcesPath = "Racial/Tiefling/DefaultTieflingRacialLoadout";
        const string TieflingLoadoutAssetPath = "Assets/Data/Racial/Tiefling/DefaultTieflingRacialLoadout.asset";

        public static bool TryConvertActiveMemberToTiefling(out string reason)
        {
            reason = null;
            PartyManager party = PartyManager.Instance;
            BaseActor member = party?.GetActiveMember();
            if (member == null)
            {
                reason = "No active party member.";
                return false;
            }

            return TryConvertMemberToTiefling(member, out reason);
        }

        public static bool TryConvertMemberToTiefling(BaseActor member, out string reason)
        {
            reason = null;
            if (member == null)
            {
                reason = "Invalid party member.";
                return false;
            }

            PartyManager party = PartyManager.Instance;
            if (party == null || !party.partyMembers.Contains(member))
            {
                reason = "Member is not in the current party.";
                return false;
            }

            CharacterStats stats = member.GetComponent<CharacterStats>();
            if (stats == null)
            {
                reason = "Member has no CharacterStats.";
                return false;
            }

            RacialLoadoutDefinition loadout = LoadTieflingLoadout();
            if (loadout == null)
            {
                reason = "Missing DefaultTieflingRacialLoadout asset.";
                return false;
            }

            RemoveOtherRacialRuntimes(member);
            stats.race = Race.Tiefling;
            stats.racialSubsystem = RacialSubsystemKind.TieflingImplants;
            stats.bodyCapabilities |= BodyCapabilityFlags.Horns;

            RacialLoadoutApplier loadoutApplier = member.GetComponent<RacialLoadoutApplier>();
            if (loadoutApplier == null)
                loadoutApplier = member.gameObject.AddComponent<RacialLoadoutApplier>();

            loadoutApplier.SetLoadout(loadout);

            TieflingImplantsRuntime implants = member.GetComponent<TieflingImplantsRuntime>();
            if (implants == null)
                implants = member.gameObject.AddComponent<TieflingImplantsRuntime>();

            implants.ClearAllImplants();

            Debug.Log(
                $"[PartyRacialDev] Converted {member.DisplayName} to Tiefling with empty implant roster. Visit the Fleshmetal Forgemaster in town to install grafts.");
            return true;
        }

        static RacialLoadoutDefinition LoadTieflingLoadout()
        {
#if UNITY_EDITOR
            RacialLoadoutDefinition editorAsset =
                AssetDatabase.LoadAssetAtPath<RacialLoadoutDefinition>(TieflingLoadoutAssetPath);
            if (editorAsset != null)
                return editorAsset;
#endif

            return Resources.Load<RacialLoadoutDefinition>(TieflingLoadoutResourcesPath);
        }

        static void RemoveOtherRacialRuntimes(BaseActor member)
        {
            DestroyIfPresent<SpiritImprintRuntime>(member.gameObject);
            DestroyIfPresent<ElementalSpiritContractsRuntime>(member.gameObject);
        }

        static void DestroyIfPresent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null)
                Object.Destroy(component);
        }
    }
#endif
}
