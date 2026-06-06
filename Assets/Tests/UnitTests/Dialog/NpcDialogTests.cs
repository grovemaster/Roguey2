using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Controller.Player;
using JRogue.Core.Actor;
using JRogue.Dialog;
using JRogue.Manager.Grid;
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
    }
}
