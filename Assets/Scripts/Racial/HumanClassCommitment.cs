using JRogue.Manager.Essence;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// One-way Human class commitment and subsystem bootstrap.
    /// </summary>
    public static class HumanClassCommitment
    {
        public static bool TryCommit(GameObject actor, HumanClass targetClass, out string error)
        {
            error = null;
            if (actor == null)
            {
                error = "Actor is null.";
                return false;
            }

            CharacterStats stats = actor.GetComponent<CharacterStats>();
            if (stats == null)
            {
                error = "CharacterStats is missing.";
                return false;
            }

            if (stats.race != Race.Human)
            {
                error = "Class commitment is only valid for Humans.";
                return false;
            }

            if (!HumanClassRules.CanCommitToClass(stats.humanClass, targetClass, out error))
                return false;

            BootstrapClass(actor, stats, targetClass);
            return true;
        }

        /// <summary>Ensures <see cref="HumanMageSpellsRuntime"/> exists for committed Mages (runtime bootstrap).</summary>
        public static HumanMageSpellsRuntime ResolveMageSpellsRuntime(GameObject actor)
        {
            if (actor == null)
                return null;

            CharacterStats stats = actor.GetComponent<CharacterStats>();
            if (stats == null || stats.humanClass != HumanClass.Mage)
                return actor.GetComponent<HumanMageSpellsRuntime>();

            return EnsureMageSpellsRuntime(actor, HumanClass.Mage);
        }

        public static void BootstrapClass(GameObject actor, CharacterStats stats, HumanClass targetClass)
        {
            stats.humanClass = targetClass;
            stats.racialSubsystem = RacialSubsystemKind.HumanSpecialization;

            EssenceSlotManager essence = actor.GetComponent<EssenceSlotManager>();
            essence?.ApplyMaxSlotsFromClass();

            stats.RefreshResourcePoolsToMax();

            HumanClassSkillTreeRuntime[] trees = actor.GetComponents<HumanClassSkillTreeRuntime>();
            for (int i = 0; i < trees.Length; i++)
                trees[i]?.TryApplyFromSerializedState();

            HumanMageSpellsRuntime mageSpells = EnsureMageSpellsRuntime(actor, targetClass);
            mageSpells?.RebuildEquippedFromPreset();

            if (targetClass == HumanClass.Knight)
                EnsureKnightRuntimes(actor);

            if (!HumanClassRules.CanGainEssences(targetClass))
            {
                Debug.Log(
                    $"[HumanClass] Committed {actor.name} to {targetClass}: essences disabled, Soul Power max {stats.MaxSoulPower}.");
            }
        }

        static HumanMageSpellsRuntime EnsureMageSpellsRuntime(GameObject actor, HumanClass targetClass)
        {
            if (actor == null || targetClass != HumanClass.Mage)
                return actor?.GetComponent<HumanMageSpellsRuntime>();

            HumanMageSpellsRuntime runtime = actor.GetComponent<HumanMageSpellsRuntime>();
            if (runtime == null)
                runtime = actor.AddComponent<HumanMageSpellsRuntime>();

            return runtime;
        }

        static void EnsureKnightRuntimes(GameObject actor)
        {
            if (actor == null)
                return;

            KnightSkillMasteryRuntime.EnsureOn(actor);
            KnightAuraStateRuntime.EnsureOn(actor);

            HumanClassSkillTreeRuntime[] trees = actor.GetComponents<HumanClassSkillTreeRuntime>();
            for (int i = 0; i < trees.Length; i++)
            {
                if (trees[i]?.SkillTree != null && trees[i].SkillTree.humanClass == HumanClass.Knight)
                    trees[i].TryApplyFromSerializedState();
            }
        }
    }
}
