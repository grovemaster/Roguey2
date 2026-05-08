using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Manager.Map;
using JRogue.Manager.Turn;
using JRogue.Stats;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.Controller.Player
{
    public class PlayerController : BaseActor
    {
        // private MapManager mapManager;
        private EquipmentManager equipment;
        public int baseAttack = 1;

        [Header("Acoustics")]
        [SerializeField, Min(0)] private int meleeNoiseVolume = 5;

        protected override void Awake() // Run before Start
        {
            base.Awake();
            equipment = GetComponent<EquipmentManager>();

            //     // ItemData loadedSword = Resources.Load<ItemData>("Item/Weapon/Giants_Blade");

            //     // if (loadedSword != null)
            //     // {
            //     //     equipment.EquipWeapon(loadedSword);
            //     // }
            //     // else
            //     // {
            //     //     Debug.Log("Could not find Giant's Blade in Resources! Check your path string.");
            //     // }

            //     // if (equipment.equippedWeapon != null)
            //     // {
            //     //     equipment.EquipWeapon(equipment.equippedWeapon);
            //     // }
        }

        protected override void Die()
        {
            Debug.Log("Game Over! The Player has fallen.");
            // Trigger Game Over UI
        }

        protected override void OnBump(BaseActor target)
        {
            Debug.Log($"{gameObject.name} bumped into {target.gameObject.name} at {target.GridPosition}.");
            // Check if the thing we bumped is an enemy
            if (target is EnemyController enemy)
            {
                AttackEnemy(enemy);
            }
        }

        private void AttackEnemy(EnemyController enemy)
        {
            // Calculate damage using your EquipmentManager logic
            int damage = equipment.GetTotalAttack(baseAttack);

            // Call the TakeDamage method we unified in BaseActor
            // Using DamageType.Slash as a default for now
            enemy.TakeDamage(damage, DamageType.Slash);

            Debug.Log($"Player attacked {enemy.name} for {damage} damage!");

            ProduceNoise(meleeNoiseVolume);
        }

        public override void OnHearNoise(BaseActor source, Vector3Int origin, int rawVolume, int effectiveVolume)
        {
            Debug.Log($"[SENSE-HEARING] {name} heard noise of volume {rawVolume} from ({origin.x},{origin.y}). Effective Volume at Player: {effectiveVolume}.");
        }

        // // Start is called once before the first execution of Update after the MonoBehaviour is created
        // void Start()
        // {
        //     // Find the MapManager in the scene
        //     mapManager = FindAnyObjectByType<MapManager>();

        //     // Snap the player to the nearest integer grid coordinate at start
        //     gridPosition = Vector3Int.FloorToInt(transform.position);
        //     SyncPosition();
        // }

        // // Update is called once per frame
        // void Update()
        // {
        //     // NEW: Check the TurnManager before allowing input
        //     if (TurnManager.Instance.currentState != GameState.PLAYER_TURN)
        //     {
        //         return;
        //     }

        //     Vector3Int moveDir = Vector3Int.zero;

        //     // Discrete Input: GetKeyDown is better for Roguelikes than GetAxis
        //     if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) moveDir = Vector3Int.up;
        //     else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) moveDir = Vector3Int.down;
        //     else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) moveDir = Vector3Int.left;
        //     else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) moveDir = Vector3Int.right;

        //     if (moveDir != Vector3Int.zero)
        //     {
        //         TryMove(moveDir);
        //     }
        // }

        // public void OnMove(InputValue value)
        // {
        //     if (TurnManager.Instance.currentState != GameState.PLAYER_TURN) return;

        //     Vector2 input = value.Get<Vector2>();
        //     Vector3Int direction = Vector3Int.zero;

        //     if (input.x != 0) direction.x = input.x > 0 ? 1 : -1;
        //     else if (input.y != 0) direction.y = input.y > 0 ? 1 : -1;

        //     if (direction != Vector3Int.zero)
        //     {
        //         if (TryMove(direction))
        //         {
        //             TurnManager.Instance.OnPlayerActionComplete();
        //         }
        //     }
        // }

        // private void TryMove(Vector3Int direction)
        // {
        //     Vector3Int targetPos = gridPosition + direction;

        //     // 1. Check for Enemies first
        //     EnemyController targetEnemy = FindEnemyAt(targetPos);
        //     if (targetEnemy != null)
        //     {
        //         // Calculate damage based on equipment
        //         int finalDamage = equipment.GetTotalAttack(baseAttack);

        //         targetEnemy.TakeDamage(finalDamage, DamageType.Blunt);
        //         TurnManager.Instance.OnPlayerActionComplete();
        //         return;
        //     }

        //     // 2. Otherwise, check for walls
        //     if (mapManager.IsWalkable(targetPos))
        //     {
        //         MovePlayer(direction);
        //         gridPosition = targetPos;
        //         SyncPosition();

        //         // NEW: Tell the manager the player has acted
        //         TurnManager.Instance.OnPlayerActionComplete();
        //     }
        //     else
        //     {
        //         Debug.Log("Ouch! That's a wall.");
        //         // Optional: You could play a "bump" sound or animation here later
        //     }
        // }

        // Inside your movement method (e.g., MovePlayer)
        // private void MovePlayer(Vector3Int direction)
        // {
        //     InventoryManager inv = GetComponent<InventoryManager>();
        //     CharacterStats stats = GetComponent<CharacterStats>();

        //     float currentWeight = inv.GetTotalWeight();
        //     float limit = stats.EncumbranceLimit;

        //     // Calculate a speed multiplier
        //     float speedMultiplier = 1.0f;

        //     // Simple Encumbrance tiers
        //     if (currentWeight > limit)
        //     {
        //         speedMultiplier = 0.5f; // Overburdened (Crawl)
        //     }
        //     else if (currentWeight > limit * 0.75f)
        //     {
        //         speedMultiplier = 0.8f; // Heavy (Slowed)
        //     }

        //     // Apply the multiplier to your final movement speed
        //     // transform.Translate(direction * speed * speedMultiplier * Time.deltaTime);
        // }

        // private void SyncPosition()
        // {
        //     // Convert grid coordinates back to world space
        //     // We add 0.5f to center the sprite in the middle of the tile
        //     transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0);
        // }

        // private EnemyController FindEnemyAt(Vector3Int pos)
        // {
        //     foreach (var enemy in FindObjectsByType<EnemyController>())
        //     {
        //         if (enemy.GetGridPosition() == pos) return enemy;
        //     }
        //     return null;
        // }

        // private void CheckForItems()
        // {
        //     // Assuming you have a way to find objects at a grid position
        //     // This is a simplified example
        //     Collider2D hit = Physics2D.OverlapPoint(transform.position);

        //     if (hit != null && hit.TryGetComponent(out WorldItem worldItem))
        //     {
        //         // InventoryManager inv = GetComponent<InventoryManager>();
        //         worldItem.PickUp(gameObject);
        //     }
        //     /*
        //     // Logic to find an object at the grid position...
        //     // If you find a WorldItem component:
        //     if (foundObject.TryGetComponent(out WorldItem item))
        //     {
        //         item.PickUp(GetComponent<InventoryManager>());
        //     }
        //     */
        // }

        // private void OnTriggerEnter2D(Collider2D other)
        // {
        //     // Does the thing we stepped on have a WorldItem component?
        //     if (other.TryGetComponent(out WorldItem groundItem))
        //     {
        //         groundItem.PickUp(gameObject);
        //     }
        // }
    }
}