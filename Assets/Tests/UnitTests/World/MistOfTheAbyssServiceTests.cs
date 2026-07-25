using JRogue.UI.Gameplay;
using JRogue.World.MapPresence;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.World
{
    public class MistOfTheAbyssServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            MistOfTheAbyssService.ResetForNewRun();
            GameLogService.ClearSession();
        }

        [TearDown]
        public void TearDown()
        {
            MistOfTheAbyssService.ResetForNewRun();
        }

        [Test]
        public void RegisterMist_TracksFloorActive()
        {
            Assert.IsFalse(MistOfTheAbyssService.IsMistActiveOnFloor("dungeon_floor_01"));
            MistOfTheAbyssService.RegisterMist("dungeon_floor_01");
            Assert.IsTrue(MistOfTheAbyssService.IsMistActiveOnFloor("dungeon_floor_01"));
            Assert.IsFalse(MistOfTheAbyssService.IsMistActiveOnFloor("dungeon_floor_02"));
        }

        [Test]
        public void UnregisterMist_ClearsWhenLastHostGone()
        {
            MistOfTheAbyssService.RegisterMist("dungeon_floor_01");
            MistOfTheAbyssService.RegisterMist("dungeon_floor_01");
            MistOfTheAbyssService.UnregisterMist("dungeon_floor_01");
            Assert.IsTrue(MistOfTheAbyssService.IsMistActiveOnFloor("dungeon_floor_01"));
            MistOfTheAbyssService.UnregisterMist("dungeon_floor_01");
            Assert.IsFalse(MistOfTheAbyssService.IsMistActiveOnFloor("dungeon_floor_01"));
        }

        [Test]
        public void MistEffect_ApplyAndRevert_RegistersAndClears()
        {
            var effect = ScriptableObject.CreateInstance<MistOfTheAbyssMapEffect>();
            effect.hostFloorId = "dungeon_floor_01";
            var profile = ScriptableObject.CreateInstance<MonsterMapPresenceProfile>();
            profile.effects = new MonsterMapPresenceEffect[] { effect };
            var ctx = new MonsterMapPresenceContext(null, profile);

            effect.Apply(ctx);
            Assert.IsTrue(MistOfTheAbyssService.IsMistActiveOnFloor("dungeon_floor_01"));

            ctx.RevertAll();
            Assert.IsFalse(MistOfTheAbyssService.IsMistActiveOnFloor("dungeon_floor_01"));

            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(profile);
        }
    }
}
