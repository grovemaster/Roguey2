using JRogue.Actors;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    public static class ElementalSpiritNicknameService
    {
        public static bool TrySetNickname(
            BaseActor elf,
            string contractInstanceId,
            string nickname,
            out string failureReason)
        {
            failureReason = null;

            if (elf == null)
            {
                failureReason = "Invalid character.";
                return false;
            }

            CharacterStats stats = elf.stats;
            if (stats == null || stats.race != Race.Elf)
            {
                failureReason = "Only Elves can nickname elemental spirits.";
                return false;
            }

            if (stats.racialSubsystem != RacialSubsystemKind.ElfElementalContracts)
            {
                failureReason = "This character has no elemental spirit contracts.";
                return false;
            }

            ElementalSpiritContractsRuntime runtime = elf.GetComponent<ElementalSpiritContractsRuntime>();
            if (runtime == null)
            {
                failureReason = "Elemental spirit contracts are unavailable.";
                return false;
            }

            if (string.IsNullOrEmpty(contractInstanceId))
            {
                failureReason = "Invalid spirit instance.";
                return false;
            }

            if (!runtime.TryGetPreset(contractInstanceId, out ElementalSpiritContractPreset preset))
            {
                failureReason = "Spirit contract not found.";
                return false;
            }

            string normalized = ElementalSpiritDisplayNames.NormalizeNickname(nickname);
            if (normalized.Length > ElementalSpiritDisplayNames.MaxNicknameLength)
            {
                failureReason =
                    $"Nickname must be {ElementalSpiritDisplayNames.MaxNicknameLength} characters or fewer.";
                return false;
            }

            preset.nickname = normalized;
            return true;
        }
    }
}
