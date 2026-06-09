using System.Collections.Generic;
using JRogue.Ability.SuddenStrength;
using JRogue.Actors;
using JRogue.Manager.Turn;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UI
{
    [TestFixture]
    public sealed class ElementalSpiritHotbarTests
    {
        readonly List<Object> _cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _cleanup)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _cleanup.Clear();
        }

        [Test]
        public void BuildPool_IncludesSuddenStrengthFromContractedRosterWithoutSummon()
        {
            BaseActor elf = CreateElfWithTwoContracts(out ElementalSpiritContractsRuntime runtime);
            var ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.name = "SuddenStrength_Standard";
            _cleanup.Add(ability);
            InjectSuddenStrength(runtime, GetFirstInstanceId(runtime), ability);

            List<(HotbarEntry entry, string displayName, string group)> pool =
                HotbarAssignabilityService.BuildPool(elf);

            Assert.IsTrue(pool.Exists(entry =>
                entry.entry.Kind == HotbarEntryKind.RacialActive
                && entry.entry.racialBindingKey == HotbarResolver.BuildElementalSpiritActiveBindingKey(ability.name)));
        }

        [Test]
        public void Resolve_ElementalSpiritActive_ValidOnRosterWithoutSummon()
        {
            BaseActor elf = CreateElfWithTwoContracts(out ElementalSpiritContractsRuntime runtime);
            var ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.name = "SuddenStrength_Standard";
            _cleanup.Add(ability);
            InjectSuddenStrength(runtime, GetFirstInstanceId(runtime), ability);

            HotbarResolvedAction resolved = HotbarResolver.Resolve(
                elf,
                new HotbarEntry
                {
                    Kind = HotbarEntryKind.RacialActive,
                    racialBindingKey = HotbarResolver.BuildElementalSpiritActiveBindingKey(ability.name),
                });

            Assert.IsTrue(resolved.IsValid);
            Assert.IsFalse(resolved.IsStale);
            Assert.AreSame(ability, resolved.Ability);
        }

        [Test]
        public void Evaluate_ElementalSpiritActive_GreyWhenNotSummoned_UsableWhenSummoned()
        {
            BaseActor elf = CreateElfWithTwoContracts(out ElementalSpiritContractsRuntime runtime);
            var ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.name = "SuddenStrength_Standard";
            _cleanup.Add(ability);
            string instanceId = GetFirstInstanceId(runtime);
            InjectSuddenStrength(runtime, instanceId, ability);

            var entry = new HotbarEntry
            {
                Kind = HotbarEntryKind.RacialActive,
                racialBindingKey = HotbarResolver.BuildElementalSpiritActiveBindingKey(ability.name),
            };
            HotbarResolvedAction resolved = HotbarResolver.Resolve(elf, entry);

            CreateTurnManager();
            (bool usableBefore, _, _) = HotbarUsabilityService.Evaluate(elf, resolved);
            Assert.IsFalse(usableBefore);

            runtime.TrySummonInstance(instanceId, out _);
            (bool usableAfter, _, _) = HotbarUsabilityService.Evaluate(elf, resolved);
            Assert.IsTrue(usableAfter);
        }

        static void CreateTurnManager()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.currentState = GameState.PLAYER_TURN;
                return;
            }

            var go = new GameObject("TurnManagerTest");
            Object.DontDestroyOnLoad(go);
            var turn = go.AddComponent<TurnManager>();
            turn.currentState = GameState.PLAYER_TURN;
        }

        [Test]
        public void BuildPool_DedupesSuddenStrengthAcrossSummonedInstances()
        {
            BaseActor elf = CreateElfWithTwoContracts(out ElementalSpiritContractsRuntime runtime);
            runtime.TrySummonInstance(GetFirstInstanceId(runtime), out _);
            runtime.TrySummonInstance(GetSecondInstanceId(runtime), out _);

            List<(HotbarEntry entry, string displayName, string group)> pool =
                HotbarAssignabilityService.BuildPool(elf);

            int suddenStrengthCount = 0;
            foreach ((HotbarEntry entry, _, _) in pool)
            {
                if (entry.Kind != HotbarEntryKind.RacialActive)
                    continue;

                if (HotbarResolver.IsElementalSpiritActiveBinding(entry.racialBindingKey)
                    && entry.racialBindingKey.Contains("SuddenStrength_Standard"))
                {
                    suddenStrengthCount++;
                }
            }

            Assert.AreEqual(1, suddenStrengthCount);
        }

        [Test]
        public void Resolve_ElementalSpiritSummon_ReturnsInstanceId()
        {
            BaseActor elf = CreateElfWithTwoContracts(out ElementalSpiritContractsRuntime runtime);
            string instanceId = GetFirstInstanceId(runtime);

            HotbarResolvedAction resolved = HotbarResolver.Resolve(
                elf,
                new HotbarEntry
                {
                    Kind = HotbarEntryKind.ElementalSpiritSummon,
                    contractInstanceId = instanceId,
                });

            Assert.IsTrue(resolved.IsValid);
            Assert.AreEqual(HotbarEntryKind.ElementalSpiritSummon, resolved.Kind);
            Assert.AreEqual(instanceId, resolved.ContractInstanceId);
        }

        [Test]
        public void Resolve_ElementalSpiritActive_UsesAbilityAssetBinding()
        {
            BaseActor elf = CreateElfWithTwoContracts(out ElementalSpiritContractsRuntime runtime);
            var ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.name = "SuddenStrength_Standard";
            _cleanup.Add(ability);

            string instanceId = GetFirstInstanceId(runtime);
            InjectSuddenStrength(runtime, instanceId, ability);
            runtime.TrySummonInstance(instanceId, out _);

            HotbarResolvedAction resolved = HotbarResolver.Resolve(
                elf,
                new HotbarEntry
                {
                    Kind = HotbarEntryKind.RacialActive,
                    racialBindingKey = HotbarResolver.BuildElementalSpiritActiveBindingKey(ability.name),
                });

            Assert.IsTrue(resolved.IsValid);
            Assert.AreSame(ability, resolved.Ability);
        }

        BaseActor CreateElfWithTwoContracts(out ElementalSpiritContractsRuntime runtime)
        {
            var go = new GameObject("ElfHotbarTest");
            _cleanup.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Elf;
            stats.racialSubsystem = RacialSubsystemKind.ElfElementalContracts;
            stats.currentSoulPower = 20;

            runtime = go.AddComponent<ElementalSpiritContractsRuntime>();
            ElementalSpiritDefinition spirit = BuildSpirit();
            runtime.TryFormContract(spirit, 1, out _, out _);
            runtime.TryFormContract(spirit, 1, out _, out _);

            return go.AddComponent<BaseActor>();
        }

        static ElementalSpiritDefinition BuildSpirit()
        {
            var spirit = ScriptableObject.CreateInstance<ElementalSpiritDefinition>();
            spirit.spiritId = "test_spirit";
            spirit.displayName = "Test Spirit";
            spirit.maxLevel = 1;
            spirit.summonSoulPowerCost = 1;
            spirit.upkeepSoulPowerPerTurn = 1;
            spirit.levels = new List<ElementalSpiritLevelData>
            {
                new ElementalSpiritLevelData
                {
                    activeEntries = new List<ElementalSpiritActiveEntry>(),
                },
            };
            return spirit;
        }

        static void InjectSuddenStrength(
            ElementalSpiritContractsRuntime runtime,
            string instanceId,
            SuddenStrengthAbility ability)
        {
            if (!runtime.TryGetPreset(instanceId, out ElementalSpiritContractPreset preset))
                return;

            preset.spirit.levels[0].activeEntries = new List<ElementalSpiritActiveEntry>
            {
                new ElementalSpiritActiveEntry
                {
                    ability = ability,
                    consumesTurn = true,
                },
            };
        }

        static string GetFirstInstanceId(ElementalSpiritContractsRuntime runtime)
        {
            presetEnsure(runtime.ContractedSpirits[0]);
            return runtime.ContractedSpirits[0].contractInstanceId;
        }

        static string GetSecondInstanceId(ElementalSpiritContractsRuntime runtime)
        {
            presetEnsure(runtime.ContractedSpirits[1]);
            return runtime.ContractedSpirits[1].contractInstanceId;
        }

        static void presetEnsure(ElementalSpiritContractPreset preset) => preset?.EnsureInstanceId();
    }
}
