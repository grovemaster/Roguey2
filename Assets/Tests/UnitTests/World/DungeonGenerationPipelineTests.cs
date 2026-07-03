using System.Linq;
using System.Reflection;
using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.World
{
    [TestFixture]
    public sealed class DungeonGenerationPipelineTests
    {
        [Test]
        public void ZoneCompositePhases_PlayerSpawnRunsAfterVaultPlacementBeforePortalSetup()
        {
            var def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
            try
            {
                typeof(DungeonFloorDefinition)
                    .GetField("layoutMode", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(def, FloorLayoutMode.ZoneComposite);

                var phases = DungeonGenerationPipeline.PhasesFor(def).Select(p => p.GetType().Name).ToList();

                int vaultIndex = phases.IndexOf(nameof(VaultPlacementPhase));
                int spawnIndex = phases.IndexOf(nameof(PlayerSpawnAnchorPhase));
                int portalIndex = phases.IndexOf(nameof(PortalPlacementPhase));
                int validationIndex = phases.IndexOf(nameof(FloorConnectivityValidationPhase));
                int setupIndex = phases.IndexOf(nameof(PortalSetupPhase));

                Assert.Greater(vaultIndex, -1);
                Assert.Greater(spawnIndex, vaultIndex);
                Assert.Greater(portalIndex, spawnIndex);
                Assert.Greater(validationIndex, portalIndex);
                Assert.Greater(setupIndex, validationIndex);
                Assert.Less(validationIndex, phases.IndexOf(nameof(EnemyPopulationPhase)));
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }
    }
}
