# Conditional enemy spawn — Requirements

**Dynamic enemy spawning** places hostile actors at runtime when **spawn triggers** fire (lever activation in v0; map flags, quests, timers, and scripted events later). Placement is **data-driven** and **footprint-aware** so multi-tile species reuse the same pipeline.

**Depends on:** [Interactable tiles](Interactable-Tiles-Requirements.md) (`InteractableEffect`, lever activation), `MapManager.IsWalkable`, `GridManager` occupancy, `GridMover` / `EnemyController`, [Multi-tile enemies](Multi-Tile-Enemy-Requirements.md).

**Related:** `EnemySpeciesDefinition`, `CombatThreatCoordinator` (future: re-evaluate tension after spawn). [Offering altars](../World/Altar-And-Map-Interact-Requirements.md) (v0 mana-stone altar uses same placement policy).

**Out of scope (v0):** Save/load spawn ledger, spawn caps per room, patrol routes for spawned enemies, networked replication.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Extensible triggers** — v0: interactable `onActivateEffects`; future: quest flags, zone entry, turn count, custom `IEnemySpawnTrigger`. |
| **G2** | **Extensible placement** — v0: north-of-origin then nearest walkable floor; future: fixed cell, random in radius, spawn points. |
| **G3** | **Species via prefab** — `EnemySpawnDefinition` references an `EnemyController` prefab (Skeleton v0 uses `Assets/Prefabs/Actor/Enemy/Enemy.prefab` + `SkeletonSpecies`). |
| **G4** | **Lever → Skeleton (v0)** | Activating a configured lever spawns one Skeleton using placement rules in §4. |
| **G5** | **Safe placement** | Never spawn overlapping party/enemies, on walls, or on interactable-occupied cells (lever tile). |

---

## 2. Architecture

```
Trigger (v0: SpawnEnemyInteractableEffect)
    → EnemySpawnService.TrySpawn(EnemySpawnDefinition, originCell)
        → EnemySpawnPlacementResolver.TryResolveAnchor(...)
        → Instantiate(prefab) + GridMover.InitializeAtGridAnchor(anchor)
```

| Type | Role |
|------|------|
| **`EnemySpawnDefinition`** | ScriptableObject: prefab, placement policy, optional primary offset. |
| **`EnemySpawnPlacementPolicy`** | How to pick anchor from `originCell` (lever cell in v0). |
| **`EnemySpawnPlacementResolver`** | Pure placement: walkability, occupancy, footprint fit, interactable blocks. |
| **`EnemySpawnService`** | Orchestrates resolve + instantiate + grid registration. |
| **`SpawnEnemyInteractableEffect`** | `InteractableEffect` that spawns using lever `instance.Cell` as origin. |

### 2.1 — Future triggers (not implemented)

| Trigger | Notes |
|---------|--------|
| **`QuestFlagSpawnTrigger`** | When `MapFlag` set, spawn once. |
| **`ZoneEntrySpawnTrigger`** | Party enters tile region. |
| **`TurnWaveSpawnTrigger`** | Nth enemy phase. |
| **`IEnemySpawnTrigger`** | Interface: `bool TryFire(EnemySpawnContext ctx)` — unify above. |

Keep **placement + instantiate** in `EnemySpawnService`; triggers only supply `originCell` + `EnemySpawnDefinition`.

### 2.2 — Future placement policies

| Policy | Use |
|--------|-----|
| **`NorthOfOriginThenNearestUnoccupiedFloor`** | v0 lever skeleton |
| **`NearestUnoccupiedFloorFromOrigin`** | Generic fallback |
| **`FixedCell`** | Boss intro cinematic tile |
| **`RandomInChebyshevRadius`** | Ambush packs |

---

## 3. Placement rules (v0)

**Origin cell** = interactable anchor (lever tile).

**Primary candidate** = `origin + primaryOffset`. Default offset **`(0, 1, 0)`** = **north** (+Y, same as movement north).

**Valid anchor** for a prefab footprint:

1. Every cell in the footprint at that anchor is **walkable** (`MapManager.IsWalkable`).
2. No cell is blocked by an **interactable** with `blocksOccupancy` (lever, etc.).
3. No cell is occupied by an existing **battle target** on `GridManager`.
4. Multi-tile enemies: all footprint cells must pass (see `GridFootprintUtility.GetOccupiedCells`).

**Fallback:** If primary fails, **breadth-first search** from `origin` over **8-connected** walkable cells, increasing distance, first anchor that fits (Manhattan distance tie-break to prefer closer tiles).

**Failure:** Log warning; no spawn; lever activation still succeeds (effects are independent).

---

## 4. v0 content — Skeleton on lever

| Item | Value |
|------|--------|
| Prefab | `Assets/Prefabs/Actor/Enemy/Enemy.prefab` (species: Skeleton) |
| Definition asset | `Assets/Data/Spawn/Spawn_Skeleton_NorthOfLever.asset` |
| Effect asset | `Assets/Data/Interactables/Effects/SpawnSkeletonOnLeverActivate.asset` |
| Sample wiring | `LeverSwitch_First` → `onActivateEffects` includes spawn effect |

**Test (SampleScene):**

1. Bump **Lever 1** from the south (or any adjacent cell).
2. Skeleton appears on the tile **north** of the lever if clear.
3. Block north tile (party member or wall); bump lever → skeleton at **nearest** open floor tile.

---

## 5. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Potions/scrolls unrelated; only `SpawnEnemyInteractableEffect` on lever triggers spawn. |
| **AC2** | North tile preferred when walkable and unoccupied. |
| **AC3** | Wall or actor on north → spawn at nearest valid floor anchor. |
| **AC4** | 2×2 enemy prefab (future) uses same resolver; all four cells checked. |
| **AC5** | Spawned enemy registered on `GridManager`; AI/turns work same as scene-placed enemies. |
| **AC6** | Second bump on latched lever does not re-spawn (lever already on; no second activation). |

---

## 6. Implementation checklist

| Item | Status |
|------|--------|
| `EnemySpawnDefinition` + `EnemySpawnPlacementPolicy` | Done |
| `EnemySpawnPlacementResolver` | Done |
| `EnemySpawnService` | Done |
| `SpawnEnemyInteractableEffect` | Done |
| `GridMover.InitializeAtGridAnchor` | Done |
| Unit tests: placement resolver | Done |
| Editor: create spawn assets + wire Lever 1 | Done in repo; optional refresh via **JRogue → Interactables → Create Skeleton Lever Spawn Assets** |
| SampleScene QA | Pending play-mode |

---

## 7. Wiring a new spawn (designer)

1. **Create** `EnemySpawnDefinition` (*Assets → Create → JRogue → Spawn → Enemy Spawn Definition*).
2. Assign **Enemy Prefab** (e.g. Giant Skeleton prefab for 2×2).
3. Choose **Placement Policy** (default: north then nearest).
4. **Create** `SpawnEnemyInteractableEffect` (*JRogue → Interactables → Effects → Spawn Enemy*).
5. Assign the spawn definition to the effect.
6. Add effect to lever `onActivateEffects` list (order matters if combined with XP/chain).

Menu: **JRogue → Interactables → Create Skeleton Lever Spawn Assets** (refreshes definition + effect; patches Lever 1).  
Assets are also committed under `Assets/Data/Spawn/` and wired on `LeverSwitch_First` — no menu required for QA.
