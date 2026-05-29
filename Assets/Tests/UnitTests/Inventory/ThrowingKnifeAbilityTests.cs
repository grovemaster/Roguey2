using System.Collections.Generic;
using JRogue.Ability.ThrowingKnife;
using JRogue.Actors;
using JRogue.Manager.Grid;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Inventory
{
    [TestFixture]
    public sealed class ThrowingKnifeAbilityTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp() => InputTestSceneBuilder.ResetSingletonManagersForTests();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();

            foreach (Object asset in _assets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _assets.Clear();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void Execute_EmptyTile_ReturnsFalse()
        {
            SetupCombatants(out BaseActor thrower, out BaseActor _, out Vector3Int enemyTile);
            ThrowingKnifeAbility knife = CreateKnifeAbility();

            Assert.IsFalse(knife.Execute(thrower.gameObject, enemyTile + Vector3Int.up));
        }

        [Test]
        public void Execute_EnemyOnTile_DealsDamage()
        {
            SetupCombatants(out BaseActor thrower, out BaseActor enemy, out Vector3Int enemyTile);
            ThrowingKnifeAbility knife = CreateKnifeAbility();
            int hpBefore = enemy.stats.currentHP;

            Assert.IsTrue(knife.Execute(thrower.gameObject, enemyTile));
            Assert.AreEqual(hpBefore - knife.pierceDamage, enemy.stats.currentHP);
        }

        [Test]
        public void Execute_AllyOnTile_Skipped_ReturnsFalse()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(2, _created);
            BaseActor thrower = party.partyMembers[0];
            BaseActor ally = party.partyMembers[1];
            Vector3Int allyTile = thrower.GridPosition + Vector3Int.right;
            ally.SetGridPosition(allyTile);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            ThrowingKnifeAbility knife = CreateKnifeAbility();
            Assert.IsFalse(knife.Execute(thrower.gameObject, allyTile));
        }

        void SetupCombatants(out BaseActor thrower, out BaseActor enemy, out Vector3Int enemyTile)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            thrower = party.partyMembers[0];
            enemyTile = thrower.GridPosition + Vector3Int.right;

            GameObject enemyObject = new GameObject("TestEnemy");
            _created.Add(enemyObject);
            var stats = enemyObject.AddComponent<CharacterStats>();
            stats.Constitution = new Stat(3);
            stats.currentHP = 30;
            enemy = enemyObject.AddComponent<TestKnifeTargetActor>();
            enemy.SetGridPosition(enemyTile);
            GridManager.Instance.RegisterActor(enemyTile, enemy);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
        }

        ThrowingKnifeAbility CreateKnifeAbility()
        {
            var knife = ScriptableObject.CreateInstance<ThrowingKnifeAbility>();
            knife.pierceDamage = 10;
            knife.canHurtAllies = false;
            knife.canHurtCaster = false;
            _assets.Add(knife);
            return knife;
        }

        sealed class TestKnifeTargetActor : BaseActor
        {
            protected override void Die() { }
        }
    }
}
