using JRogue.Actors;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    public static class ElementalSpiritContractService
    {
        public static bool TryFormContract(
            BaseActor elf,
            ElementalSpiritDefinition spirit,
            int initialLevel,
            out string contractInstanceId,
            out string failureReason)
        {
            contractInstanceId = null;
            failureReason = null;

            if (elf == null)
            {
                failureReason = "No Elf selected.";
                return false;
            }

            if (spirit == null)
            {
                failureReason = "No spirit definition.";
                return false;
            }

            CharacterStats stats = elf.stats;
            if (stats == null || stats.race != Race.Elf)
            {
                failureReason = "Target is not an Elf.";
                return false;
            }

            if (stats.racialSubsystem != RacialSubsystemKind.ElfElementalContracts)
            {
                failureReason = "Elf cannot form elemental contracts.";
                return false;
            }

            ElementalSpiritContractsRuntime runtime = elf.GetComponent<ElementalSpiritContractsRuntime>();
            if (runtime == null)
            {
                failureReason = "No elemental spirit runtime.";
                return false;
            }

            return runtime.TryFormContract(spirit, initialLevel, out contractInstanceId, out failureReason);
        }
    }
}
