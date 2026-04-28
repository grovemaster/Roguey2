using JRogue.Actors;
using JRogue.Controller.Player;
using JRogue.Manager.Map;
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
            Vector3Int direction = GetMoveDirection(playerPos);

            // If adjacent to player, attack!
            if (Vector3Int.Distance(gridPosition, playerPos) <= 1.1f)
            {
                AttackPlayer();
            }
            else
            {
                MoveTowards(direction);
            }
        }

        private Vector3Int GetMoveDirection(Vector3Int target)
        {
            Vector3Int diff = target - gridPosition;
            // Simple "Cardinal Only" AI
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                return new Vector3Int(diff.x > 0 ? 1 : -1, 0, 0);
            else
                return new Vector3Int(0, diff.y > 0 ? 1 : -1, 0);
        }

        private void MoveTowards(Vector3Int direction)
        {
            TryMove(direction);
            // Vector3Int target = gridPosition + dir;
            // // The enemy also obeys the MapManager!
            // if (FindAnyObjectByType<MapManager>().IsWalkable(target))
            // {
            //     gridPosition = target;
            //     SyncPosition();
            // }
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