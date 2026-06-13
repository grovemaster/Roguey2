using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Racial;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    [TestFixture]
    public sealed class TieflingImplantForgemasterLogicTests
    {
        readonly List<Object> _cleanup = new List<Object>();
        PartyCurrencyLedger _ledger;

        [SetUp]
        public void SetUp()
        {
            var ledgerGo = new GameObject("ForgemasterTestLedger");
            _cleanup.Add(ledgerGo);
            _ledger = ledgerGo.AddComponent<PartyCurrencyLedger>();
        }

        [TearDown]
        public void TearDown()
        {
            TieflingImplantForgemasterService.ResetDefaultCatalogForTests();
            foreach (Object asset in _cleanup)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _cleanup.Clear();
        }

        [Test]
        public void IsSpeakerEligible_RejectsNonTiefling()
        {
            BaseActor human = CreateActor(Race.Human, RacialSubsystemKind.None, withImplants: false);

            bool ok = TieflingImplantForgemasterLogic.IsSpeakerEligible(
                human,
                out TieflingImplantsRuntime runtime,
                out string rejectLine);

            Assert.IsFalse(ok);
            Assert.IsNull(runtime);
            Assert.AreEqual("This forge works fleshmetal for Tieflings only.", rejectLine);
        }

        [Test]
        public void TryExecuteInstall_InstallsIntoEmptySlot_AndSpendsGold()
        {
            BaseActor tiefling = CreateTiefling(out TieflingImplantsRuntime runtime);
            CyborgImplantDefinition implant = CreateImplant("iron_sleeve", "Iron Sleeve", ImplantSlot.LeftArm, gold: 40);
            TieflingForgemasterDefinition catalog = CreateCatalog(implant);
            TieflingImplantForgemasterService.SetDefaultCatalogForTests(catalog);

            EnsurePartyGold(100);

            bool ok = TieflingImplantForgemasterLogic.TryExecuteInstall(
                tiefling,
                runtime,
                implant.implantId,
                catalog,
                new[] { tiefling },
                null,
                out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsTrue(runtime.TryGetInstalled(ImplantSlot.LeftArm, out CyborgImplantDefinition installed));
            Assert.AreSame(implant, installed);
            Assert.AreEqual(60, ShopGoldUtility.GetPartyGoldTotal());
        }

        [Test]
        public void TryExecuteInstall_ReplacesExistingImplant()
        {
            BaseActor tiefling = CreateTiefling(out TieflingImplantsRuntime runtime);
            CyborgImplantDefinition first = CreateImplant("iron_sleeve", "Iron Sleeve", ImplantSlot.LeftArm, gold: 40);
            CyborgImplantDefinition second = CreateImplant("chrome_sleeve", "Chrome Sleeve", ImplantSlot.LeftArm, gold: 50);
            runtime.TryInstallImplant(ImplantSlot.LeftArm, first, out _);

            TieflingForgemasterDefinition catalog = CreateCatalog(first, second);
            EnsurePartyGold(100);

            bool ok = TieflingImplantForgemasterLogic.TryExecuteInstall(
                tiefling,
                runtime,
                second.implantId,
                catalog,
                new[] { tiefling },
                null,
                out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsTrue(runtime.TryGetInstalled(ImplantSlot.LeftArm, out CyborgImplantDefinition installed));
            Assert.AreSame(second, installed);
            Assert.IsFalse(runtime.HasImplantId(first.implantId));
        }

        [Test]
        public void TryExecuteRemove_ChargesHalfGold()
        {
            BaseActor tiefling = CreateTiefling(out TieflingImplantsRuntime runtime);
            CyborgImplantDefinition implant = CreateImplant("iron_sleeve", "Iron Sleeve", ImplantSlot.LeftArm, gold: 40);
            runtime.TryInstallImplant(ImplantSlot.LeftArm, implant, out _);
            EnsurePartyGold(100);

            bool ok = TieflingImplantForgemasterLogic.TryExecuteRemove(
                tiefling,
                runtime,
                ImplantSlot.LeftArm,
                new[] { tiefling },
                null,
                out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsFalse(runtime.TryGetInstalled(ImplantSlot.LeftArm, out _));
            Assert.AreEqual(80, ShopGoldUtility.GetPartyGoldTotal());
        }

        [Test]
        public void ResolveRemoveCost_DefaultsToHalfInstallGold()
        {
            CyborgImplantDefinition implant = CreateImplant("iron_sleeve", "Iron Sleeve", ImplantSlot.LeftArm, gold: 41);
            CyborgImplantRemoveCost removeCost = TieflingImplantForgemasterLogic.ResolveRemoveCost(implant);
            Assert.AreEqual(20, removeCost.gold);
        }

        [Test]
        public void IsInstallChoiceEnabled_GreysAlreadyInstalledImplant()
        {
            BaseActor tiefling = CreateTiefling(out TieflingImplantsRuntime runtime);
            CyborgImplantDefinition implant = CreateImplant("iron_sleeve", "Iron Sleeve", ImplantSlot.LeftArm, gold: 10);
            runtime.TryInstallImplant(ImplantSlot.LeftArm, implant, out _);
            EnsurePartyGold(100);

            var offer = new TieflingImplantInstallOffer
            {
                Implant = implant,
                Slot = ImplantSlot.LeftArm,
            };

            bool enabled = TieflingImplantForgemasterLogic.IsInstallChoiceEnabled(
                tiefling,
                runtime,
                offer,
                null,
                out string reason);

            Assert.IsFalse(enabled);
            Assert.AreEqual("already installed", reason);
        }

        BaseActor CreateTiefling(out TieflingImplantsRuntime runtime)
        {
            BaseActor actor = CreateActor(Race.Tiefling, RacialSubsystemKind.TieflingImplants, withImplants: true);
            runtime = actor.GetComponent<TieflingImplantsRuntime>();
            runtime.ClearAllImplants();
            return actor;
        }

        BaseActor CreateActor(Race race, RacialSubsystemKind subsystem, bool withImplants)
        {
            var go = new GameObject("ForgemasterTestActor");
            _cleanup.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = race;
            stats.racialSubsystem = subsystem;

            if (withImplants)
                go.AddComponent<TieflingImplantsRuntime>();

            return go.AddComponent<BaseActor>();
        }

        CyborgImplantDefinition CreateImplant(string id, string displayName, ImplantSlot slot, int gold)
        {
            var implant = ScriptableObject.CreateInstance<CyborgImplantDefinition>();
            _cleanup.Add(implant);
            implant.implantId = id;
            implant.displayName = displayName;
            implant.allowedSlots = new List<ImplantSlot> { slot };
            implant.installCost = new CyborgImplantInstallCost { gold = gold };
            return implant;
        }

        static TieflingForgemasterDefinition CreateCatalog(params CyborgImplantDefinition[] implants)
        {
            var catalog = ScriptableObject.CreateInstance<TieflingForgemasterDefinition>();
            catalog.offeredImplants = new List<CyborgImplantDefinition>(implants);
            return catalog;
        }

        static void EnsurePartyGold(int amount)
        {
            ShopGoldUtility.AddPartyGold(amount);
        }
    }
}
