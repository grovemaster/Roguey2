using JRogue.Controller.Enemy;
using JRogue.Spawn;
using UnityEngine;

namespace JRogue.World.Altar
{
    [CreateAssetMenu(
        fileName = "SpawnEnemyOnAltarComplete",
        menuName = "JRogue/World/Altar/Effects/Spawn Enemy")]
    public sealed class SpawnEnemyAltarCompletionEffect : AltarCompletionEffect
    {
        public EnemySpawnDefinition spawnDefinition;

        public override void Execute(AltarInstance instance)
        {
            if (spawnDefinition == null)
            {
                Debug.LogWarning("[Altar:Spawn] SpawnEnemyAltarCompletionEffect has no definition.");
                return;
            }

            if (instance == null)
                return;

            if (!EnemySpawnService.TrySpawn(spawnDefinition, instance.Cell, out EnemyController _))
                Debug.LogWarning($"[Altar:Spawn] Failed to spawn near altar at {instance.Cell}.");
        }
    }
}
