# Doors — Requirements (DCSS-inspired)

**Doors** are map features embedded in **walls** that separate rooms. They can be **open** or **closed**, **locked** or **unlocked**, and **intact** or **broken**. Party members interact via **bump** (DCSS “walk into door”) and **dedicated open/close commands** (DCSS `o` / `c`). **Keys** unlock a **single** designated door and are **consumed** on use. **Lever switches** (existing [interactable tiles](../Combat/Interactable-Tiles-Requirements.md)) can **unlock** doors without opening them.

**Depends on:** `MapManager` (walkability / wall layer), `GridManager`, `BaseActor.TryMove`, `PlayerCommandProcessor`, `PartyManager`, `TurnManager`, `FormationRushService`, [Interactable tiles](../Combat/Interactable-Tiles-Requirements.md) (`OpenDoorEffect` stub → full `DoorService`), [Fog of war](Fog-Of-War-Requirements.md) / [Lighting](Lighting-Requirements.md) (future: LOS through open doors).

**Related:** `DoorService` (stub today), `OpenDoorEffect`, `InventoryItemUse`, `EnemyAiBrain` / pathfinding.

**Explicitly out of scope (v0):** Runed/sealed doors (vault warden); door creak noise / Stealth; pushing items when closing; magical “wizard lock”; secret doors; save/load door state (design for it); light propagation tuning through open doors (hook only).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **DCSS bump flow** — bumping a **closed, unlocked** door **opens** it and **moves** the bumper **through** the doorway in **one** player turn (when the tile beyond is legal). |
| **G2** | **Dedicated open/close** — party member uses a **bound key** to **open** or **close** an **adjacent** door **without moving**; **turn consumed**; **formation rush** runs when formation is active. |
| **G3** | **Locked doors** — stay closed until **unlocked** (key, lever, script); locked doors **cannot** be opened by bump or open command until unlocked. |
| **G4** | **Per-door keys** — a key item unlocks **exactly one** `doorId`; on successful use the key is **removed from inventory immediately**. |
| **G5** | **Lever unlock** — flipping a lever **unlocks** a configured door (may leave it closed until opened). |
| **G6** | **Enemy doors** — some enemies **open** doors (spends **that enemy’s** turn); some **break** doors instead of opening. |
| **G7** | **Orientation** — doors support **horizontal** and **vertical** wall alignment with distinct sprites. |
| **G8** | **Extensibility** — data-driven definitions, pluggable effects, and stable `doorId` for quests / ordered-key puzzles later. |
| **G9** | **Art** — import DCSS-style **door** and **key** sprites (see §14); 32×32 pipeline consistent with hazards / interactables. |

---

## 2. DCSS reference (behavioral target)

| DCSS behavior | JRogue v0 |
|---------------|-----------|
| Walk into closed door → opens (if not locked) | **Bump-open-and-move** (§7.1) |
| `o` — open adjacent door without moving | **OpenDoor** command (§7.2) |
| `c` — close adjacent door | **CloseDoor** command (§7.2) |
| Locked / runed doors block until key or special case | **Locked** until `isUnlocked` (§6); runed = future |
| Monsters open doors when pursuing | **EnemyCanOpen** (§9) |
| Some monsters destroy doors | **EnemyCanBreak** (§9) |
| Doors in walls, 1×1 | **Single cell** doorway on wall edge (§5.2) |

---

## 3. Design decision — registry + overlay (locked)

**Recommendation:** Treat doors like [interactables](../Combat/Interactable-Tiles-Requirements.md) and [hazards](Environmental-Hazards-Requirements.md): **wall map identity** + **`DoorService` registry** + **feature overlay** (sprites per state/orientation).

| Layer | Role |
|-------|------|
| **Wall / map** | Underlying dungeon wall; doorway cell tagged at authoring time. |
| **`DoorService`** | `doorId` → `DoorInstance` (cell, state, lock, orientation, definition ref). |
| **Overlay tilemap** | Closed / open / broken sprites; horizontal vs vertical variants. |
| **`MapManager`** | `IsWalkable(cell)` consults door state (closed = blocked, open/broken = walkable). |

**Rejected:** Door state stored only in a monolithic tile enum on `wallMap` without runtime instances — hard to wire keys, levers, and save games.

---

## 4. Glossary

| Term | Meaning |
|------|--------|
| **Doorway cell** | The single grid cell containing the door feature (usually in a wall line). |
| **Orientation** | **Horizontal** (passage east–west) or **Vertical** (north–south); drives sprite set. |
| **Closed** | Blocks movement and occupancy through the cell. |
| **Open** | Walkable; does not block movement. |
| **Broken** | Destroyed; permanently walkable; cannot close. |
| **Locked** | `isUnlocked == false`; cannot open until unlocked. |
| **Unlocked** | May open via bump or open command (still may be closed). |
| **Bump-open-and-move** | One action: open then step into doorway / beyond per §7.1. |
| **`doorId`** | Stable string or enum id for keys, levers, and scripts. |

---

## 5. Door model (extensible)

### 5.1 — `DoorState` (runtime)

```csharp
public enum DoorState
{
    Closed = 0,
    Open = 1,
    Broken = 2,
}
```

| State | Walkable | Can close? | Can lock? |
|-------|----------|------------|-----------|
| **Closed** | No (if locked or unlocked) | N/A (already closed) | Yes (when unlocked) |
| **Open** | Yes | Yes (if unlocked) | No (close first) |
| **Broken** | Yes | No | No |

### 5.2 — `DoorDefinition` (ScriptableObject)

| Field | Purpose |
|-------|---------|
| `doorId` | Unique id (`Door_NorthVault`, …) |
| `displayName` | Logs / UI |
| `orientation` | `Horizontal` \| `Vertical` |
| `startsLocked` | Initial `isUnlocked` |
| `startsOpen` | Initial `DoorState` (only if not locked) |
| `canBeBroken` | If false, enemies with break capability treat as solid |
| `breakHitPoints` | Optional HP for “break” damage (v0: single-hit break OK) |
| `enemyPolicy` | `None` \| `CanOpen` \| `CanBreak` (default for monsters interacting) |
| **Sprites** | `closedH`, `openH`, `brokenH`, `closedV`, `openV`, `brokenV` (or sprite table by orientation+state) |

Menu: **`JRogue/Doors/Door Definition`**.

### 5.3 — `DoorInstance` (runtime / serialized per scene)

| Field | Purpose |
|-------|---------|
| `definition` | `DoorDefinition` ref |
| `cell` | `Vector3Int` doorway |
| `state` | `DoorState` |
| `isUnlocked` | Lock flag |
| `facing` | Derived from orientation (for bump direction hints) |

Registered in `DoorService` at scene load (`DoorPlacement` / bootstrap asset).

### 5.4 — Placement authoring

| Approach | Detail |
|----------|--------|
| **`DoorPlacementSet`** | ScriptableObject list: `doorId`, cell, optional override locked/open. |
| **Scene bootstrap** | `DoorTileBootstrap` on scene root (mirror `InteractableTileBootstrap`). |
| **Editor menu** | Place doors in SampleScene; validate cell is wall doorway. |

---

## 6. Locking and unlock sources

### 6.1 — Locked behavior

| Rule | Detail |
|------|--------|
| **Bump** | Locked + closed → **no open**, **no move**; message “The door is locked.” **No turn spent**. |
| **Open command** | Fails; no turn spent. |
| **Close command** | N/A if closed. |
| **Key use** | If key’s `targetDoorId` matches and door locked → set `isUnlocked = true`, **consume key**, **spend turn** (§7.4). Door remains **closed** until opened. |
| **Lever** | `UnlockDoorEffect` sets `isUnlocked = true` on `doorId` (door may stay closed). |
| **Script** | `DoorService.Unlock(doorId, source)` for quests. |

### 6.2 — `DoorKeyItemData` : `ItemData`

| Field | Purpose |
|-------|---------|
| `category` | `ItemCategory` — new **`Key`** or reuse **`PlotItem`** / **`Treasure`** (prefer **`Key`** enum value) |
| `targetDoorId` | Matches exactly **one** `DoorDefinition.doorId` |
| `weight` | Light (e.g. 0.1) |
| `icon` | Key sprite (§14) |
| `consumesOnUse` | **true** (always) |

**R6.2.1** Using a key on the **wrong** door: fail message; **key not consumed**; **no turn** (v0).

**R6.2.2** Using a key when door **already unlocked**: fail or no-op; **key not consumed**; **no turn** (v0).

**R6.2.3** Key use range: bearer must be **orthogonally adjacent** to the doorway cell (v0).

### 6.3 — Lever integration

Extend interactable effects:

| Effect | Behavior |
|--------|----------|
| **`UnlockDoorEffect`** (new) | `DoorService.Unlock(doorId)` |
| **`OpenDoorEffect`** (existing) | `DoorService.TryOpen(doorId)` — opens if unlocked |
| **`UnlockAndOpenDoorEffect`** (optional) | Unlock + open in one lever pull |

Lever activation turn rules unchanged ([interactable §7](../Combat/Interactable-Tiles-Requirements.md)).

---

## 7. Player interactions

### 7.1 — Bump-open-and-move (DCSS primary)

**Preconditions:** Active member attempts **move** into doorway cell `D`; door exists; `state == Closed`; `isUnlocked == true`.

**Resolution order** (before normal enemy bump):

1. If door **locked** → fail (§6.1); return without move.
2. If door **closed + unlocked**:
   - Set `state = Open`; refresh overlay.
   - If **formation inactive**: attempt **enter** `D` (and continue move if multi-step — v0 **one step into doorway** only).
   - If **formation active**: leader moves into `D` if legal; `RecordNewLeaderPosition`; **`ProcessFollowerRush()`**; end player turn per formation rules.
3. **One player turn** consumed for the whole bump-open-move (same as DCSS).

**R7.1.1** If tile beyond door is **blocked** (enemy, wall, closed door): **open door only**, **do not move**; still **spend turn** (DCSS: door opens even if something blocks beyond).

**R7.1.2** Multi-tile actors: center / anchor cell enters doorway; footprint must fit beyond door.

**Integration:** Hook in `BaseActor.TryMove` / `PlayerCommandProcessor` **before** interactable-lever bump and enemy bump ([interactable §8](../Combat/Interactable-Tiles-Requirements.md) order — **doors first** when `dest` is doorway).

### 7.2 — Dedicated open / close (no movement)

| Command | Default binding (proposal) | Behavior |
|---------|---------------------------|----------|
| **OpenDoor** | `o` | Adjacent **closed**, **unlocked** door → **open**; actor **does not move**. |
| **CloseDoor** | `c` | Adjacent **open**, **unlocked** door → **closed** if §7.3 allows. |

**Turn:** Always **consumes** active member’s action.

| Formation | On success |
|-----------|------------|
| **Inactive** | `TurnManager.OnPlayerActionComplete(actor)` |
| **Active** | `OnPlayerActionComplete(actor)` → `ProcessFollowerRush()` → `ForceEndPlayerTurn()` when squad done (mirror [interactable §7.1](../Combat/Interactable-Tiles-Requirements.md)) |

**R7.2.1** No adjacent door / illegal state → message; **no turn spent**.

**R7.2.2** Add actions to `GameControls.inputactions`; wire in `InputHandler` → `PlayerCommandProcessor`.

### 7.3 — Close validation

| Blocker | Result |
|---------|--------|
| Actor standing **on** doorway | Cannot close (“Something is in the way.”) |
| Item pile on doorway | Cannot close (v0) or push item (future — DCSS pushes) |
| **Locked** | Cannot close open door? (v0: can close if unlocked) |
| **Broken** | Cannot close |

### 7.4 — Key use (inventory)

| Step | Action |
|------|--------|
| 1 | Player selects key in inventory → **Use** (or dedicated **Use Key** on adjacent door — v0: inventory Use with adjacency check). |
| 2 | If no matching locked door adjacent → fail; no consume. |
| 3 | `DoorService.Unlock(targetDoorId)`; remove key instance from inventory. |
| 4 | Complete player turn (+ formation rush if active). |

---

## 8. Turn consumption summary

| Action | Turn spent? | Formation rush? |
|--------|-------------|-----------------|
| Bump-open-and-move (success) | Yes | Yes if formation on |
| OpenDoor / CloseDoor (success) | Yes | Yes if formation on |
| Key unlock (success) | Yes | Yes if formation on |
| Locked bump / failed command | No | No |
| Enemy opens door | **That enemy’s turn only** | N/A |
| Enemy breaks door | **That enemy’s turn only** | N/A |

---

## 9. Enemy interaction

### 9.1 — `EnemyDoorCapability` (on species or individual)

| Capability | Behavior |
|------------|----------|
| **None** | Treat closed door as wall for movement. |
| **CanOpen** | If closed + unlocked (or enemy ignores lock — **no** in v0): open door, **do not move** through unless movement continues separately; **end enemy action**. |
| **CanBreak** | If `canBeBroken`: set `Broken`, walkable; **end enemy action**. If not breakable: blocked. |

Authored on `EnemySpeciesDefinition` with per-enemy override optional.

### 9.2 — AI hook

During enemy move resolution, when path wants to enter doorway cell:

1. If **open** → walk through.
2. If **closed + unlocked + CanOpen** → `TryOpen`; consume turn; re-plan next turn.
3. If **closed + locked** → repath or wait.
4. If **CanBreak** → `TryBreak`; consume turn.

**R9.2.1** Opening a door **alerts** party — optional `CombatThreatCoordinator` ping (future); v0 log only.

---

## 10. Pathfinding and map queries

| Query | Rule |
|-------|------|
| `MapManager.IsWalkable` | `false` if door `Closed`; `true` if `Open` or `Broken`. |
| `DoorService.BlocksMovement(cell)` | Convenience mirror of above. |
| **A\*** (player & enemy) | Uses `IsWalkable`; closed doors block until opened/broken. |
| **Interactable occupancy** | Door cell is **not** an interactable; levers remain separate. |

**Future:** `ShadowCaster` / fog — open door cells participate in LOS (see [Lighting](Lighting-Requirements.md) backlog).

---

## 11. Extensibility architecture

```
DoorPlacementSet / scene markers
    → DoorService.Register(instance)
        → DoorDefinition (data)
        → DoorInstance (runtime state)

PlayerCommandProcessor / BaseActor.TryMove
    → IDoorPlayerInteraction.TryBumpOpenAndMove(...)
    → IDoorPlayerInteraction.TryOpenAdjacent / TryCloseAdjacent

InventoryItemUse
    → DoorKeyUseHandler.TryUseKey(...)

InteractableEffect
    → UnlockDoorEffect / OpenDoorEffect

EnemyAiBrain / EnemyController
    → EnemyDoorInteraction.TryInteract(...)
```

| Extension point | Future use |
|-----------------|------------|
| **`IDoorLockSource`** | Quest flag, floor card, spell |
| **`IDoorOpenListener`** | Spawn enemies when door opens |
| **`DoorTrapOnOpen`** | Link to [Traps](Traps-Requirements.md) |
| **`SealedDoorPolicy`** | Vault warden / timed seal |
| **Save snapshot** | `doorId` + state + isUnlocked |

Keep **orchestration** in `DoorService`; listeners **subscribe** to `DoorStateChanged` event.

---

## 12. v0 sample content (SampleScene)

| Asset | Purpose |
|-------|---------|
| `Door_Test_Horizontal` | Unlocked; bump-open-and-move QA |
| `Door_Test_Vertical` | Unlocked; open/close command QA |
| `Door_Test_Locked` | Locked until `Key_Test_A` used |
| `Key_Test_A` | `DoorKeyItemData` → `Door_Test_Locked` only |
| `LeverSwitch_UnlockDoor` | Existing lever pattern + `UnlockDoorEffect` → `Door_Test_Locked` |

**Editor menus (implementation):**

| Menu | Purpose |
|------|---------|
| `JRogue/Doors/Create Door v0 Assets` | Definitions + key + placements |
| `JRogue/Doors/Seed Test Key on Party Barbarian Warrior` | Carried key for lock QA |
| `JRogue/Doors/Place Door Test Layout in SampleScene` | Horizontal + vertical + locked door |

---

## 13. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Bump into **unlocked closed** door → door **open**, actor **moves** through if legal; **one turn** spent. |
| **AC2** | **OpenDoor** on adjacent closed unlocked door → **open**, actor **does not move**; turn spent; **formation rush** when formation on. |
| **AC3** | **CloseDoor** on adjacent open door → **closed** if nothing blocking; turn spent. |
| **AC4** | **Locked** door: bump and open command **fail** without spending turn. |
| **AC5** | **Key** on correct adjacent door → **unlocked**, key **removed** from inventory; door still closed until opened. |
| **AC6** | **Key** on wrong door → key **remains**; no turn. |
| **AC7** | **Lever** `UnlockDoorEffect` → door becomes openable; bump works after pull. |
| **AC8** | Enemy with **CanOpen** opens door; **does not** take a second move that turn (action consumed). |
| **AC9** | Enemy with **CanBreak** leaves **broken** walkable tile. |
| **AC10** | Horizontal and vertical doors show **correct** sprites per state. |

---

## 14. Art — doors and keys (approval required)

**Do not import until product confirms option.** Target **32×32**, **PPU 32**, point filter (match interactables / hazards).

### 14.1 — Doors — Option A (recommended): DCSS crawl-tiles

| | |
|--|--|
| **Source** | [Dungeon Crawl 32×32 tiles](https://opengameart.org/content/dungeon-crawl-32x32-tiles) (`crawl-tiles Oct-5-2010.zip`) |
| **License** | Free use; credit Crawl / contributors ([OGA page](https://opengameart.org/content/dungeon-crawl-32x32-tiles)) |
| **Paths in pack** | `dungeon/` — closed wooden/metal doors; open door variants; broken/grate tiles (verify sheet after download) |
| **Fit** | Same family as scroll/knife/hazard art; authentic DCSS look |
| **Work** | Slice **closed / open / broken** × **horizontal / vertical** (rotate or pick oriented frames from sheet) |

**Import target:**

- `Assets/Art/Doors/ThirdParty/crawl-tiles-LICENSE.txt`
- `Assets/Art/Doors/Sprites/Door_Closed_H.png`, `Door_Open_H.png`, … (vertical set `*_V.png`)

### 14.2 — Doors — Option B: 32×32 Dungeon Tileset (CC0)

| | |
|--|--|
| **Source** | [32×32 Dungeon Tileset](https://opengameart.org/content/32x32-dungeon-tileset-0) |
| **License** | CC0 |
| **Fit** | Generic doors; less DCSS-specific |

### 14.3 — Keys — Option A (recommended): DCSS crawl-tiles

| | |
|--|--|
| **Source** | Same `crawl-tiles` pack — `item/misc/` or `item/key/` (golden key, skeleton key, etc.) |
| **License** | Same as §14.1 |
| **Import target** | `Assets/Art/Items/Sprites/Key_Generic.png` → `DoorKey_Test.asset` icon |

### 14.4 — Keys — Option B: OpenGameArt “Key” CC0 packs

Search OGA for **32×32 key** CC0 if crawl-tiles key read is unclear at small size.

### 14.5 — Recommended

**Option A (DCSS crawl-tiles)** for doors **and** keys for visual consistency with existing JRogue DCSS imports.

**Please reply** with door/key art option before implementation import milestone.

---

## 15. Debug logging

Prefix: **`[Door]`**

| Event | Level |
|-------|--------|
| State change (open/close/break/unlock) | Log |
| Player bump-open-and-move | Log |
| Command open/close | Log |
| Key consumed | Log |
| Blocked (locked, in way) | Log |
| Enemy open/break | Log |

---

## 16. Implementation checklist

- [x] `DoorDefinition`, `DoorInstance`, `DoorPlacementSet`, `DoorService` (replace stub)
- [x] Map walkability + overlay tilemap (`MapManager` + `Door_Overlay`)
- [x] Bump-open-and-move in `TryMove` / `PlayerCommandProcessor`
- [x] `OpenDoor` / `CloseDoor` input + commands (`o` / `c`)
- [x] `DoorKeyItemData` + inventory use + consume
- [x] `UnlockDoorEffect` + editor asset hook
- [x] Enemy open/break hooks (`EnemySpeciesDefinition.doorCapability`)
- [x] Art import — DCSS crawl-tiles subset in `Assets/Art/Doors/Sprites/` (see ThirdParty README)
- [x] Editor menus (§12)
- [x] Unit tests: `DoorServiceTests`
- [x] Update [Interactable-Tiles-Requirements.md](../Combat/Interactable-Tiles-Requirements.md) §13

### SampleScene setup (editor)

1. `JRogue → Doors → Create Door v0 Assets`
2. `JRogue → Doors → Wire Door Service in SampleScene`
3. `JRogue → Doors → Seed Test Key on Party Barbarian Warrior`
4. Door cells are on existing SampleScene floor: `(1,-2)` horizontal, `(0,1)` vertical (open), `(2,-2)` locked
5. Assign `UnlockDoor_TestLockedDoor` effect to a test lever targeting `Door_Test_Locked`

---

## 17. Traceability

| Request | Section |
|---------|---------|
| Unlocked open/close via dedicated key, no move, turn + rush | §7.2, §8 |
| Enemy open (turn consumed) | §9 |
| Enemy break | §9 |
| Horizontal / vertical sprites | §5.1, §14 |
| Bump open + move one turn | §7.1 |
| Locked until unlock | §6 |
| Lever unlocks door | §6.3 |
| Key → one door, removed on use | §6.2 |
| Extensible | §11 |
| DCSS door/key art | §14 |
