using System.Collections.Generic;
using JRogue.Controller.Enemy;
using JRogue.Core.Actor;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Combat
{
    public sealed class EnemyAttackProfileDefaultTests
    {
        GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void EmptyProfiles_DefaultsToAdjacentSingle_Only()
        {
            EnemyController enemy = CreateEnemy();
            enemy.attackProfiles = new List<EnemyAttackProfileKind>();

            Assert.IsTrue(enemy.HasAttackProfile(EnemyAttackProfileKind.AdjacentSingle));
            Assert.IsFalse(enemy.HasAttackProfile(EnemyAttackProfileKind.AdjacentSideSweep));
        }

        [Test]
        public void NullProfiles_DefaultsToAdjacentSingle_Only()
        {
            EnemyController enemy = CreateEnemy();
            enemy.attackProfiles = null;

            Assert.IsTrue(enemy.HasAttackProfile(EnemyAttackProfileKind.AdjacentSingle));
            Assert.IsFalse(enemy.HasAttackProfile(EnemyAttackProfileKind.AdjacentSideSweep));
        }

        [Test]
        public void AuthoredList_IsAuthoritative_CanOmitSingle()
        {
            EnemyController enemy = CreateEnemy();
            enemy.attackProfiles = new List<EnemyAttackProfileKind>
            {
                EnemyAttackProfileKind.AdjacentSideSweep,
            };

            Assert.IsFalse(enemy.HasAttackProfile(EnemyAttackProfileKind.AdjacentSingle));
            Assert.IsTrue(enemy.HasAttackProfile(EnemyAttackProfileKind.AdjacentSideSweep));
        }

        EnemyController CreateEnemy()
        {
            _go = new GameObject("EnemyAttackProfileProbe");
            return _go.AddComponent<EnemyController>();
        }
    }
}
