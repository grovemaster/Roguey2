using System;
using JRogue.Stats;

namespace JRogue.Stats.Racial
{
    /// <summary>
    /// Serializable identity slice for save games and networking. Does not include Spirit Imprint node ids
    /// (those live in a subsystem-specific blob in a later phase).
    /// </summary>
    [Serializable]
    public struct RacialIdentitySnapshot
    {
        /// <summary>Schema version for migration. Use <see cref="RacialStackingContract.CurrentIdentitySnapshotVersion"/>.</summary>
        public byte snapshotVersion;

        /// <summary>Ancestry / folk. Stable <see cref="byte"/> values — do not renumber without save migration.</summary>
        public Race race;

        /// <summary>Human-only class commitment. Must be <see cref="HumanClass.None"/> for non-Humans.</summary>
        public HumanClass humanClass;

        /// <summary>Active racial progression framework, if any.</summary>
        public RacialSubsystemKind subsystemKind;

        /// <summary>Intrinsic anatomy flags (saved); runtime essences may OR additional capabilities.</summary>
        public BodyCapabilityFlags bodyCapabilities;

        public static RacialIdentitySnapshot CreateDefaultHuman()
        {
            return new RacialIdentitySnapshot
            {
                snapshotVersion = RacialStackingContract.CurrentIdentitySnapshotVersion,
                race = Race.Human,
                humanClass = HumanClass.None,
                subsystemKind = RacialSubsystemKind.None,
                bodyCapabilities = BodyCapabilityFlags.None
            };
        }

        public static RacialIdentitySnapshot From(CharacterStats stats)
        {
            return stats != null ? stats.GetRacialIdentitySnapshot() : CreateDefaultHuman();
        }

        public readonly RacialCommitmentPolicy CommitmentPolicy =>
            RacialSubsystemCatalog.GetCommitmentPolicy(subsystemKind);

        public void ApplyTo(CharacterStats stats)
        {
            if (stats == null)
                return;

            stats.race = race;
            stats.humanClass = humanClass;
            stats.racialSubsystem = subsystemKind;
            stats.bodyCapabilities = bodyCapabilities;
        }
    }

    /// <summary>Validation for <see cref="RacialIdentitySnapshot"/> and live <see cref="CharacterStats"/> identity fields.</summary>
    public static class RacialIdentityRules
    {
        public static bool TryValidate(RacialIdentitySnapshot snapshot, out string error)
        {
            if (snapshot.snapshotVersion > RacialStackingContract.CurrentIdentitySnapshotVersion)
            {
                error = $"Unsupported racial identity snapshot version {snapshot.snapshotVersion}.";
                return false;
            }

            if (snapshot.race != Race.Human && snapshot.humanClass != HumanClass.None)
            {
                error = "humanClass is only valid when race is Human.";
                return false;
            }

            if (!RacialSubsystemCatalog.IsSubsystemValidForRace(snapshot.subsystemKind, snapshot.race))
            {
                error = $"Subsystem {snapshot.subsystemKind} is not valid for race {snapshot.race}.";
                return false;
            }

            if (snapshot.humanClass != HumanClass.None
                && snapshot.subsystemKind != RacialSubsystemKind.HumanSpecialization)
            {
                error = "Committed HumanClass requires HumanSpecialization subsystem.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryValidateClassChange(CharacterStats stats, HumanClass newClass, out string error)
        {
            if (stats == null)
            {
                error = "CharacterStats is null.";
                return false;
            }

            if (stats.race != Race.Human)
            {
                error = "humanClass is only valid when race is Human.";
                return false;
            }

            return HumanClassRules.CanApplyHumanClassFromSnapshot(stats.humanClass, newClass, out error);
        }

        public static bool TryValidate(CharacterStats stats, out string error)
        {
            if (stats == null)
            {
                error = "CharacterStats is null.";
                return false;
            }

            return TryValidate(stats.GetRacialIdentitySnapshot(), out error);
        }
    }
}
