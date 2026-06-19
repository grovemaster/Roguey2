using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Controller.Player;
using JRogue.Core.Actor;
using JRogue.Dialog;
using JRogue.Manager.Grid;
using JRogue.World.Town;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Dialog
{
    [TestFixture]
    public sealed class DialogParameterResolverTests
    {
        [Test]
        public void Resolve_ReplacesNpcAndPartyTokens()
        {
            var speakerGo = new GameObject("Aria");
            var speaker = speakerGo.AddComponent<PlayerController>();

            var npcGo = new GameObject("Mira");
            var npc = npcGo.AddComponent<PlayerController>();

            var context = new DialogContext
            {
                Speaker = speaker,
                Npc = npc,
            };

            string result = DialogParameterResolver.Resolve(
                "My name is {npcName}. Hello, {partyName}.",
                context);

            Assert.AreEqual("My name is Mira. Hello, Aria.", result);

            Object.DestroyImmediate(speakerGo);
            Object.DestroyImmediate(npcGo);
        }
    }

    [TestFixture]
    public sealed class DialogGraphEvaluatorTests
    {
        GameObject _flagGo;
        GameObject _counterGo;

        [SetUp]
        public void SetUp()
        {
            _flagGo = new GameObject("Flags");
            _flagGo.AddComponent<GameStoryFlagService>();
            _counterGo = new GameObject("Counters");
            _counterGo.AddComponent<NpcTalkCounterService>();
        }

        [TearDown]
        public void TearDown()
        {
            GameStoryFlagService.Instance.ClearAll();
            NpcTalkCounterService.Instance.ClearAll();
            Object.DestroyImmediate(_flagGo);
            Object.DestroyImmediate(_counterGo);
        }

        [Test]
        public void ResolveEntryNodeIndex_MiraFirstTalk_UsesZeroCountBranch()
        {
            var profile = ScriptableObject.CreateInstance<NpcDialogProfile>();
            profile.npcId = TownNpcIds.Npc1;
            profile.rootNodeIndex = 0;
            profile.nodes = new[]
            {
                new DialogNodeData
                {
                    kind = DialogNodeKind.Conditional,
                    conditionKind = DialogConditionKind.NpcTalkCount,
                    npcIdForTalkCount = TownNpcIds.Npc1,
                    talkCountMin = 0,
                    talkCountMax = 0,
                    trueNodeIndex = 1,
                    falseNodeIndex = 2,
                },
                Line("First"),
                Line("Second"),
            };

            DialogContext context = BuildContext(profile);
            int nodeIndex = DialogGraphEvaluator.ResolveEntryNodeIndex(profile, context);
            DialogNodeData node = DialogGraphEvaluator.GetNode(profile, nodeIndex);
            Assert.AreEqual("First", node.line.textTemplate);
        }

        [Test]
        public void ResolveEntryNodeIndex_Edda_UsesStoryFlags()
        {
            var profile = ScriptableObject.CreateInstance<NpcDialogProfile>();
            profile.rootNodeIndex = 0;
            profile.nodes = new[]
            {
                new DialogNodeData
                {
                    kind = DialogNodeKind.Conditional,
                    conditionKind = DialogConditionKind.AnyNpcTalked,
                    anyTalkedNpcIds = new[] { TownNpcStoryFlags.TalkedNpc1 },
                    trueNodeIndex = 1,
                    falseNodeIndex = 2,
                },
                Line("Greetings."),
                Line("Hello World."),
            };

            DialogContext context = BuildContext(profile);
            int nodeIndex = DialogGraphEvaluator.ResolveEntryNodeIndex(profile, context);
            Assert.AreEqual("Hello World.", DialogGraphEvaluator.GetNode(profile, nodeIndex).line.textTemplate);

            GameStoryFlagService.Instance.Set(TownNpcStoryFlags.TalkedNpc1);
            nodeIndex = DialogGraphEvaluator.ResolveEntryNodeIndex(profile, context);
            Assert.AreEqual("Greetings.", DialogGraphEvaluator.GetNode(profile, nodeIndex).line.textTemplate);
        }

        static DialogNodeData Line(string text) =>
            new DialogNodeData
            {
                kind = DialogNodeKind.Line,
                line = new DialogLineData { textTemplate = text },
            };

        static DialogContext BuildContext(NpcDialogProfile profile) =>
            new DialogContext
            {
                Profile = profile,
                Flags = GameStoryFlagService.Instance,
                Counters = NpcTalkCounterService.Instance,
            };
    }

    [TestFixture]
    public sealed class NpcTalkFacingUtilityTests
    {
        [Test]
        public void IsFacingToward_RequiresMatchingCardinalFacing()
        {
            var go = new GameObject("Actor");
            var actor = go.AddComponent<PlayerController>();

            actor.currentFacing = FacingDirection.East;
            Assert.IsTrue(NpcTalkFacingUtility.IsFacingToward(actor, new Vector3Int(1, 0, 0)));
            Assert.IsFalse(NpcTalkFacingUtility.IsFacingToward(actor, new Vector3Int(0, 1, 0)));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void IsFacingAcrossShopCounter_AllowsAnyCustomerTileAlongCounterRow()
        {
            foreach (Vector3Int cell in AdventureGuildExchangeLayout.EnumerateCounterCells())
                JRogue.World.Generation.ShopCounterService.RegisterCounter(cell);

            var go = new GameObject("Customer");
            var actor = go.AddComponent<PlayerController>();
            var mover = go.AddComponent<JRogue.Actors.Components.GridMover>();

            Vector3Int npcCell = AdventureGuildExchangeLayout.ClerkNpcCell;
            mover.InitializeAtGridAnchor(new Vector3Int(2, AdventureGuildExchangeLayout.CustomerRowY, 0));
            actor.currentFacing = FacingDirection.North;
            Assert.IsTrue(NpcTalkFacingUtility.IsFacingAcrossShopCounter(
                actor,
                npcCell,
                AdventureGuildExchangeLayout.CustomerRowY,
                AdventureGuildExchangeLayout.CounterRowY));

            mover.InitializeAtGridAnchor(AdventureGuildExchangeLayout.CustomerTalkCell);
            Assert.IsTrue(NpcTalkFacingUtility.IsFacingAcrossShopCounter(
                actor,
                npcCell,
                AdventureGuildExchangeLayout.CustomerRowY,
                AdventureGuildExchangeLayout.CounterRowY));

            actor.currentFacing = FacingDirection.South;
            Assert.IsFalse(NpcTalkFacingUtility.IsFacingAcrossShopCounter(
                actor,
                npcCell,
                AdventureGuildExchangeLayout.CustomerRowY,
                AdventureGuildExchangeLayout.CounterRowY));

            JRogue.World.Generation.ShopCounterService.Clear();
            Object.DestroyImmediate(go);
        }

        [Test]
        public void IsFacingAcrossCounter_RequiresCustomerCellCounterBetweenAndFacingNpc()
        {
            var go = new GameObject("Customer");
            var actor = go.AddComponent<PlayerController>();
            var mover = go.AddComponent<JRogue.Actors.Components.GridMover>();

            Vector3Int npcCell = new Vector3Int(3, 4, 0);
            Vector3Int counterCell = new Vector3Int(3, 3, 0);
            Vector3Int customerCell = new Vector3Int(3, 2, 0);

            mover.InitializeAtGridAnchor(customerCell);
            actor.currentFacing = FacingDirection.North;
            Assert.IsTrue(NpcTalkFacingUtility.IsFacingAcrossCounter(actor, npcCell, counterCell, customerCell));

            actor.currentFacing = FacingDirection.South;
            Assert.IsFalse(NpcTalkFacingUtility.IsFacingAcrossCounter(actor, npcCell, counterCell, customerCell));

            mover.InitializeAtGridAnchor(new Vector3Int(2, 2, 0));
            actor.currentFacing = FacingDirection.North;
            Assert.IsFalse(NpcTalkFacingUtility.IsFacingAcrossCounter(actor, npcCell, counterCell, customerCell));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryTalkFacing_CounterClerk_AutoFacesWhenDirectlyAcross()
        {
            var gridGo = new GameObject("GridManager");
            gridGo.AddComponent<GridManager>();

            foreach (Vector3Int cell in AdventureGuildExchangeLayout.EnumerateCounterCells())
                JRogue.World.Generation.ShopCounterService.RegisterCounter(cell);

            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<PlayerController>();
            player.currentFacing = FacingDirection.South;

            var clerkGo = new GameObject("GuildClerk");
            clerkGo.AddComponent<NpcController>();
            var counterBinding = clerkGo.AddComponent<NpcCounterTalkBinding>();
            counterBinding.Configure(
                AdventureGuildExchangeLayout.CustomerRowY,
                AdventureGuildExchangeLayout.CounterRowY);

            var playerMover = playerGo.AddComponent<JRogue.Actors.Components.GridMover>();
            var clerkMover = clerkGo.AddComponent<JRogue.Actors.Components.GridMover>();
            playerMover.InitializeAtGridAnchor(AdventureGuildExchangeLayout.CustomerTalkCell);
            clerkMover.InitializeAtGridAnchor(AdventureGuildExchangeLayout.ClerkNpcCell);

            Assert.IsTrue(NpcTalkInteraction.TryTalkFacing(player));
            Assert.AreEqual(FacingDirection.North, player.currentFacing);

            JRogue.World.Generation.ShopCounterService.Clear();
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(clerkGo);
            Object.DestroyImmediate(gridGo);
        }

        [Test]
        public void TryTalkFacing_CounterClerk_WorksFromCustomerCell()
        {
            var gridGo = new GameObject("GridManager");
            gridGo.AddComponent<GridManager>();

            foreach (Vector3Int cell in AdventureGuildExchangeLayout.EnumerateCounterCells())
                JRogue.World.Generation.ShopCounterService.RegisterCounter(cell);

            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<PlayerController>();
            player.currentFacing = FacingDirection.North;

            var clerkGo = new GameObject("GuildClerk");
            clerkGo.AddComponent<NpcController>();
            var counterBinding = clerkGo.AddComponent<NpcCounterTalkBinding>();
            counterBinding.Configure(
                AdventureGuildExchangeLayout.CustomerRowY,
                AdventureGuildExchangeLayout.CounterRowY);

            var playerMover = playerGo.AddComponent<JRogue.Actors.Components.GridMover>();
            var clerkMover = clerkGo.AddComponent<JRogue.Actors.Components.GridMover>();
            playerMover.InitializeAtGridAnchor(AdventureGuildExchangeLayout.CustomerTalkCell);
            clerkMover.InitializeAtGridAnchor(AdventureGuildExchangeLayout.ClerkNpcCell);

            Assert.IsTrue(NpcTalkInteraction.TryTalkFacing(player));
            Assert.AreEqual(FacingDirection.North, player.currentFacing);

            JRogue.World.Generation.ShopCounterService.Clear();
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(clerkGo);
            Object.DestroyImmediate(gridGo);
        }

        [Test]
        public void TryTalkFacing_SingleAdjacentNpc_AutoFacesAndTalks()
        {
            var gridGo = new GameObject("GridManager");
            gridGo.AddComponent<GridManager>();

            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<PlayerController>();
            player.currentFacing = FacingDirection.North;

            var npcGo = new GameObject("Npc");
            var npc = npcGo.AddComponent<NpcController>();

            var playerMover = playerGo.AddComponent<JRogue.Actors.Components.GridMover>();
            var npcMover = npcGo.AddComponent<JRogue.Actors.Components.GridMover>();
            playerMover.InitializeAtGridAnchor(new Vector3Int(1, 0, 0));
            npcMover.InitializeAtGridAnchor(new Vector3Int(0, 0, 0));

            Assert.IsTrue(NpcTalkInteraction.TryTalkFacing(player));
            Assert.AreEqual(FacingDirection.West, player.currentFacing);

            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(npcGo);
            Object.DestroyImmediate(gridGo);
        }

        [Test]
        public void TryTalkFacing_FindsNpcViaSceneQueryWhenGridMissesRegistration()
        {
            var gridGo = new GameObject("GridManager");
            gridGo.AddComponent<GridManager>();

            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<PlayerController>();

            var npcGo = new GameObject("Mira");
            var npc = npcGo.AddComponent<NpcController>();

            var playerMover = playerGo.AddComponent<JRogue.Actors.Components.GridMover>();
            var npcMover = npcGo.AddComponent<JRogue.Actors.Components.GridMover>();
            playerMover.InitializeAtGridAnchor(new Vector3Int(4, 9, 0));
            npcMover.InitializeAtGridAnchor(new Vector3Int(4, 8, 0));

            GridManager.Instance.UnregisterActor(new Vector3Int(4, 8, 0));

            Assert.IsTrue(NpcTalkInteraction.TryTalkFacing(player));

            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(npcGo);
            Object.DestroyImmediate(gridGo);
        }

        [Test]
        public void TryTalkFacing_MultipleAdjacentNpcs_OpensPickerWhenFacingIsAmbiguous()
        {
            var gridGo = new GameObject("GridManager");
            gridGo.AddComponent<GridManager>();

            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<PlayerController>();
            player.currentFacing = FacingDirection.North;

            var miraGo = new GameObject("Mira");
            miraGo.AddComponent<NpcController>();
            var lucGo = new GameObject("Luc");
            lucGo.AddComponent<NpcController>();

            var playerMover = playerGo.AddComponent<JRogue.Actors.Components.GridMover>();
            var miraMover = miraGo.AddComponent<JRogue.Actors.Components.GridMover>();
            var lucMover = lucGo.AddComponent<JRogue.Actors.Components.GridMover>();
            playerMover.InitializeAtGridAnchor(new Vector3Int(5, 8, 0));
            miraMover.InitializeAtGridAnchor(new Vector3Int(4, 8, 0));
            lucMover.InitializeAtGridAnchor(new Vector3Int(6, 8, 0));

            Assert.IsTrue(NpcTalkInteraction.TryTalkFacing(player));
            Assert.IsTrue(JRogue.UI.Gameplay.NpcTalkPickerModalUI.BlocksGameplay);

            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(miraGo);
            Object.DestroyImmediate(lucGo);
            Object.DestroyImmediate(gridGo);
        }
    }
}
