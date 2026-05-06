using JRogue.Actors;
using JRogue.Controller.Player;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Pathfinding;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Controller.Enemy
{
    public class EnemyController : BaseActor
    {
        public int hp = 3;
        public int attackPower = 1;
        private PlayerController player;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        new void Start()
        {
            base.Start();
            player = FindAnyObjectByType<PlayerController>();
            // gridPosition = Vector3Int.FloorToInt(transform.position);
            // SyncPosition();
        }

        // This will be called by the TurnManager
        public void TakeTurn()
        {
            Vector3Int playerPos = player.GetGridPosition();

            // 8-way adjacency (Chebyshev): matches diagonal movement and melee range.
            Vector3Int diff = playerPos - gridPosition;
            int cheb = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
            if (cheb <= 1)
            {
                AttackPlayer();
                return;
            }

            if (mapManager != null
                && GridManager.Instance != null
                && GridAStarPathfinder.TryGetFirstStepTowards(
                    gridPosition,
                    playerPos,
                    gameObject,
                    mapManager,
                    GridManager.Instance,
                    out Vector3Int firstStep))
            {
                Vector3Int step = firstStep - gridPosition;
                TryMove(step);
                return;
            }

            Vector3Int direction = GetFallbackCardinalStep(playerPos);
            TryMove(direction);
        }

        private Vector3Int GetFallbackCardinalStep(Vector3Int target)
        {
            Vector3Int diff = target - gridPosition;
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                return new Vector3Int(diff.x > 0 ? 1 : -1, 0, 0);
            return new Vector3Int(0, diff.y > 0 ? 1 : -1, 0);
        }

        private void AttackPlayer()
        {
            Debug.Log("The Enemy hits you!");
            // Future: player.TakeDamage(attackPower);
        }

        // public void TakeDamage(int damage)
        // {
        //     hp -= damage;
        //     Debug.Log($"Enemy hit! HP left: {hp}");
        //     if (hp <= 0) Die();
        // }

        // public void TakeDamage(int rawDamage, DamageType type)
        // {
        //     CharacterStats stats = GetComponent<CharacterStats>();
        //     int resistanceValue = stats.GetResistance(type);

        //     // Calculation: Damage - Resistance. 
        //     // Positive resistance reduces damage. Negative resistance (Vulnerability) increases it.
        //     int damageAfterResistance = Mathf.Max(1, rawDamage - resistanceValue);

        //     // Apply Armor Class only for Physical damage
        //     if (type == DamageType.Blunt || type == DamageType.Slash || type == DamageType.Pierce)
        //     {
        //         damageAfterResistance = Mathf.Max(1, damageAfterResistance - (stats.ArmorClass / 5));
        //     }

        //     stats.currentHP -= damageAfterResistance;
        //     Debug.Log($"{gameObject.name} hit! Raw: {rawDamage} | After Res({resistanceValue}): {rawDamage - resistanceValue} | Final after AC: {damageAfterResistance}");
        //     Debug.Log($"{gameObject.name} took {damageAfterResistance} {type} damage. HP: {stats.currentHP}");

        //     if (stats.currentHP <= 0) Die();
        // }

        protected override void Die()
        {
            Debug.Log($"{gameObject.name} was defeated!");
            Destroy(gameObject);
        }

        // private void SyncPosition() =>
        //     transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0);
    }
}