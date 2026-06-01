using System.IO;
using JRogue.World.Generation.Vaults;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class VaultSourceTextTests
    {
        [Test]
        public void TryReadFileAtAssetPath_DataRelative_ResolvesUnderApplicationDataPath()
        {
            string relative = "Data/Vaults/Floor1/vault_shrine_5x5.vault";
            string expected = Path.Combine(Application.dataPath, relative);

            Assert.IsTrue(File.Exists(expected), $"Expected vault at {expected}");
            Assert.IsTrue(
                VaultSourceText.TryReadFileAtAssetPath(relative, out string text, out string error),
                error);
            Assert.That(text, Does.Contain("VAULT vault_shrine_5x5"));
        }

        [Test]
        public void TryReadFileAtAssetPath_LegacyAssetsPrefix_StillResolves()
        {
            string legacy = "Assets/Data/Vaults/Floor1/vault_shrine_5x5.vault";
            Assert.IsTrue(
                VaultSourceText.TryReadFileAtAssetPath(legacy, out string text, out string error),
                error);
            Assert.That(text, Does.Contain("VAULT"));
        }
    }
}
