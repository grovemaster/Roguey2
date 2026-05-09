using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;

namespace JRogue.Tests
{
    /// <summary>
    /// Runs once for the whole test assembly. Clears static manager slots so the first test in any namespace
    /// does not inherit a destroyed <see cref="JRogue.Manager.Grid.GridManager"/> / <see cref="JRogue.Manager.Map.MapManager"/> from a prior run.
    /// </summary>
    [SetUpFixture]
    public sealed class JRogueTestsRootSetupFixture
    {
        [OneTimeSetUp]
        public void BeforeAssembly()
        {
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [OneTimeTearDown]
        public void AfterAssembly()
        {
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }
    }
}
