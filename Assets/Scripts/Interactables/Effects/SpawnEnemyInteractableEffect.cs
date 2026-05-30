using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Spawn;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "SpawnEnemy",
        menuName = "JRogue/Interactables/Effects/Spawn Enemy")]
    public sealed class SpawnEnemyInteractableEffect : InteractableEffect
    {
        public EnemySpawnDefinition spawnDefinition;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            if (spawnDefinition == null)
            {
                Debug.LogWarning("[Interactable] SpawnEnemy effect has no EnemySpawnDefinition.");
                return;
            }

            if (instance == null)
                return;

            EnemySpawnService.TrySpawn(spawnDefinition, instance.Cell, out EnemyController _);
        }
    }
}
