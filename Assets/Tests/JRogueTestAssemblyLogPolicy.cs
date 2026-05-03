using NUnit.Framework;
using UnityEngine.TestTools;

namespace JRogue.Tests.UnitTests
{
    /// <summary>
    /// Play mode runs production <see cref="UnityEngine.Debug.Log"/> calls from party/rush code.
    /// NUnit only applies a SetUpFixture to tests in this namespace and child namespaces
    /// (e.g. <c>JRogue.Tests.UnitTests.Input</c>), not to sibling namespaces like <c>JRogue.Tests.Other</c>.
    /// </summary>
    [SetUpFixture]
    public sealed class JRogueTestAssemblyLogPolicy
    {
        [OneTimeSetUp]
        public void BeforeAllTests()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        [OneTimeTearDown]
        public void AfterAllTests()
        {
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
