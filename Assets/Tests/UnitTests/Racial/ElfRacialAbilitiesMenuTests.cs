using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using JRogue.UI.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    [TestFixture]
    public sealed class ElementalSpiritDisplayNamesTests
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
        public void GetDisplayLabel_UsesNicknameWhenSet()
        {
            ElementalSpiritContractsRuntime runtime = CreateRuntimeWithTwoSameSpirits();
            ElementalSpiritContractPreset preset = runtime.ContractedSpirits[0];
            preset.nickname = "Blaze";

            string label = ElementalSpiritDisplayNames.GetDisplayLabel(preset, runtime.ContractedSpirits);

            Assert.AreEqual("Blaze", label);
        }

        [Test]
        public void GetDisplayLabel_UsesCanonicalNameWhenNicknameBlank()
        {
            ElementalSpiritContractsRuntime runtime = CreateRuntimeWithTwoSameSpirits();
            ElementalSpiritContractPreset second = runtime.ContractedSpirits[1];

            string label = ElementalSpiritDisplayNames.GetDisplayLabel(second, runtime.ContractedSpirits);

            Assert.AreEqual("Ember Warden (2)", label);
        }

        [Test]
        public void NormalizeNickname_TrimsAndTruncates()
        {
            string normalized = ElementalSpiritDisplayNames.NormalizeNickname("  Blaze  ");
            Assert.AreEqual("Blaze", normalized);

            string longName = new string('a', 30);
            normalized = ElementalSpiritDisplayNames.NormalizeNickname(longName);
            Assert.AreEqual(24, normalized.Length);
        }

        [Test]
        public void BuildSummonHotbarLabel_UsesNickname()
        {
            ElementalSpiritContractsRuntime runtime = CreateRuntimeWithTwoSameSpirits();
            ElementalSpiritContractPreset preset = runtime.ContractedSpirits[0];
            preset.nickname = "Blaze";

            string label = ElementalSpiritDisplayNames.BuildSummonHotbarLabel(
                preset,
                runtime.ContractedSpirits,
                summoned: false);

            Assert.AreEqual("Blaze — Summon", label);
        }

        ElementalSpiritContractsRuntime CreateRuntimeWithTwoSameSpirits()
        {
            var go = new GameObject("DisplayNameTest");
            _cleanup.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Elf;
            stats.racialSubsystem = RacialSubsystemKind.ElfElementalContracts;

            var runtime = go.AddComponent<ElementalSpiritContractsRuntime>();
            ElementalSpiritDefinition spirit = BuildSpirit("ember_warden", "Ember Warden");
            runtime.TryFormContract(spirit, 1, out _, out _);
            runtime.TryFormContract(spirit, 1, out _, out _);
            return runtime;
        }

        static ElementalSpiritDefinition BuildSpirit(string id, string displayName)
        {
            var spirit = ScriptableObject.CreateInstance<ElementalSpiritDefinition>();
            spirit.spiritId = id;
            spirit.displayName = displayName;
            spirit.maxLevel = 1;
            return spirit;
        }
    }

    [TestFixture]
    public sealed class ElementalSpiritRosterSortTests
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
        public void Apply_SortsByLevelDescThenNameAsc()
        {
            ElementalSpiritContractsRuntime runtime = CreateRuntime();
            runtime.ContractedSpirits[0].contractLevel = 1;
            runtime.ContractedSpirits[1].contractLevel = 2;
            runtime.ContractedSpirits[2].contractLevel = 1;

            List<ElementalSpiritContractPreset> sorted =
                ElementalSpiritRosterSort.Apply(runtime.ContractedSpirits);

            Assert.AreEqual("tide_shard", sorted[0].spirit.spiritId);
            Assert.AreEqual(2, sorted[0].contractLevel);
            Assert.AreEqual("ember_warden", sorted[1].spirit.spiritId);
            Assert.AreEqual("wind_scout", sorted[2].spirit.spiritId);
        }

        ElementalSpiritContractsRuntime CreateRuntime()
        {
            var go = new GameObject("SortTest");
            _cleanup.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Elf;
            stats.racialSubsystem = RacialSubsystemKind.ElfElementalContracts;

            var runtime = go.AddComponent<ElementalSpiritContractsRuntime>();
            runtime.TryFormContract(BuildSpirit("wind_scout", "Wind Scout"), 1, out _, out _);
            runtime.TryFormContract(BuildSpirit("tide_shard", "Tide Shard"), 1, out _, out _);
            runtime.TryFormContract(BuildSpirit("ember_warden", "Ember Warden"), 1, out _, out _);
            return runtime;
        }

        static ElementalSpiritDefinition BuildSpirit(string id, string displayName)
        {
            var spirit = ScriptableObject.CreateInstance<ElementalSpiritDefinition>();
            spirit.spiritId = id;
            spirit.displayName = displayName;
            spirit.maxLevel = 1;
            return spirit;
        }
    }

    [TestFixture]
    public sealed class ElementalSpiritNicknameServiceTests
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
        public void TrySetNickname_StoresTrimmedNickname()
        {
            BaseActor elf = CreateElf(out ElementalSpiritContractsRuntime runtime);
            string instanceId = runtime.ContractedSpirits[0].contractInstanceId;

            bool ok = ElementalSpiritNicknameService.TrySetNickname(elf, instanceId, "  Blaze  ", out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual("Blaze", runtime.ContractedSpirits[0].nickname);
        }

        [Test]
        public void TrySetNickname_ClearsWhenBlank()
        {
            BaseActor elf = CreateElf(out ElementalSpiritContractsRuntime runtime);
            string instanceId = runtime.ContractedSpirits[0].contractInstanceId;
            runtime.ContractedSpirits[0].nickname = "Blaze";

            bool ok = ElementalSpiritNicknameService.TrySetNickname(elf, instanceId, "   ", out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(string.Empty, runtime.ContractedSpirits[0].nickname);
        }

        BaseActor CreateElf(out ElementalSpiritContractsRuntime runtime)
        {
            var go = new GameObject("NicknameTest");
            _cleanup.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Elf;
            stats.racialSubsystem = RacialSubsystemKind.ElfElementalContracts;

            runtime = go.AddComponent<ElementalSpiritContractsRuntime>();
            var spirit = ScriptableObject.CreateInstance<ElementalSpiritDefinition>();
            spirit.spiritId = "ember_warden";
            spirit.displayName = "Ember Warden";
            spirit.maxLevel = 1;
            _cleanup.Add(spirit);
            runtime.TryFormContract(spirit, 1, out _, out _);

            return go.AddComponent<BaseActor>();
        }
    }

    [TestFixture]
    public sealed class ElfElementalSpiritViewModelTests
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
        public void Build_OrdersHigherLevelFirst()
        {
            BaseActor elf = CreateElf(out ElementalSpiritContractsRuntime runtime);
            runtime.ContractedSpirits[0].contractLevel = 1;
            runtime.ContractedSpirits[1].contractLevel = 2;

            IReadOnlyList<ElfElementalSpiritContractCard> cards = ElfElementalSpiritViewModel.Build(elf);

            Assert.AreEqual(2, cards.Count);
            Assert.AreEqual(2, cards[0].ContractLevel);
        }

        [Test]
        public void Build_UsesNicknameAsTitle()
        {
            BaseActor elf = CreateElf(out ElementalSpiritContractsRuntime runtime);
            runtime.ContractedSpirits[0].nickname = "Blaze";

            IReadOnlyList<ElfElementalSpiritContractCard> cards = ElfElementalSpiritViewModel.Build(elf);

            Assert.AreEqual("Blaze", cards[0].Title);
            Assert.AreEqual("Ember Warden", cards[0].Subtitle);
        }

        BaseActor CreateElf(out ElementalSpiritContractsRuntime runtime)
        {
            var go = new GameObject("ViewModelTest");
            _cleanup.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Elf;
            stats.racialSubsystem = RacialSubsystemKind.ElfElementalContracts;
            stats.level = 5;

            runtime = go.AddComponent<ElementalSpiritContractsRuntime>();
            var spirit = ScriptableObject.CreateInstance<ElementalSpiritDefinition>();
            spirit.spiritId = "ember_warden";
            spirit.displayName = "Ember Warden";
            spirit.maxLevel = 3;
            _cleanup.Add(spirit);
            runtime.TryFormContract(spirit, 1, out _, out _);
            runtime.TryFormContract(spirit, 1, out _, out _);

            return go.AddComponent<BaseActor>();
        }
    }

    [TestFixture]
    public sealed class ElementalSpiritHotbarNicknameTests
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
        public void BuildPool_UsesNicknameForSummonLabel()
        {
            BaseActor elf = CreateElf(out ElementalSpiritContractsRuntime runtime);
            runtime.ContractedSpirits[0].nickname = "Blaze";

            List<(HotbarEntry entry, string displayName, string group)> pool =
                HotbarAssignabilityService.BuildPool(elf);

            Assert.IsTrue(pool.Exists(item =>
                item.entry.Kind == HotbarEntryKind.ElementalSpiritSummon
                && item.displayName == "Blaze — Summon"));
        }

        BaseActor CreateElf(out ElementalSpiritContractsRuntime runtime)
        {
            var go = new GameObject("HotbarNicknameTest");
            _cleanup.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Elf;
            stats.racialSubsystem = RacialSubsystemKind.ElfElementalContracts;

            runtime = go.AddComponent<ElementalSpiritContractsRuntime>();
            var spirit = ScriptableObject.CreateInstance<ElementalSpiritDefinition>();
            spirit.spiritId = "ember_warden";
            spirit.displayName = "Ember Warden";
            spirit.maxLevel = 1;
            _cleanup.Add(spirit);
            runtime.TryFormContract(spirit, 1, out _, out _);

            return go.AddComponent<BaseActor>();
        }
    }
}
