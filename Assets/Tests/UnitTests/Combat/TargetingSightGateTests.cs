using JRogue.Combat.Targeting;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Combat
{
    [TestFixture]
    public sealed class TargetingSightGateTests
    {
        [Test]
        public void TryAllowConfirm_FailsClosedWhenVisibilityManagerMissing()
        {
            Assert.IsFalse(TargetingSightGate.TryAllowConfirm(
                new Vector3Int(4, 7, 0),
                out string denyReason));
            Assert.AreEqual("Cannot designate 4,7: tile is out of sight.", denyReason);
        }

        [Test]
        public void IsPrimaryTileDesignatable_FalseWhenVisibilityManagerMissing()
        {
            Assert.IsFalse(TargetingSightGate.IsPrimaryTileDesignatable(new Vector3Int(1, 2, 0)));
        }

        [Test]
        public void TryAllowConfirm_NormalizesZ()
        {
            Assert.IsFalse(TargetingSightGate.TryAllowConfirm(
                new Vector3Int(3, 5, 9),
                out string denyReason));
            Assert.AreEqual("Cannot designate 3,5: tile is out of sight.", denyReason);
        }
    }
}
