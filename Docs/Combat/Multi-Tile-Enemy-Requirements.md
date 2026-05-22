# Multi-tile enemies — Requirements

Large enemies occupy **more than one grid cell** while sharing a single HP pool, turn, and `IBattleTarget` owner. Today every actor registers **one** cell in `GridManager`, moves one cell at a time, and melee range is computed from a single **anchor** (`GridPosition`). This spec adds **footprints**, footprint-aware grid registration, movement validation, and **data-driven attack profiles**.

**Depends on:** `GridManager`, `GridMover`, `BaseActor.TryMove`, `GridAStarPathfinder`, `EnemyAiBrain`, `TargetingResolver`, party formation / bump combat.

**Examples (v0 content):** Giant Skeleton (2×2), Snake (1×3, head anchor, fixed orientation).

---

## 1. Goals

**G1 — Vertical slice (v0)**  
Designers can author enemies with rectangular footprints (minimum 1×1). The grid registers **all** occupied cells, movement is atomic, players can bump/attack any footprint cell, and enemies use **attack profiles** (single-target adjacent and side sweep). Shipped prefabs: **`GiantSkeletonEnemy`** (2×2), **`SnakeEnemy`** (1×3), both variants of **`MultiTileEnemy`** (§1.1).

**G2 — Backward compatible**  
Unspecified or 1×1 footprint behaves exactly like today’s single-tile enemies.

**G3 — Data-driven combat**  
Attack behavior is selected per enemy via assets/prefab fields, not hard-coded per species. Giant Skeleton enables **both** adjacent single-target and side sweep; **geometry** picks sweep on cardinal sides vs single on diagonal corners (§4.3).

**G4 — Future bosses**  
Data model and grid APIs must not block 3×3, 4×4, or non-rectangular footprints later (documented as out of v0).

**G5 — Physical prefab deliverables (v0)**  
Shipping content includes a **prefab variant chain** rooted at the existing 1×1 enemy. Designers place instances in scenes without hand-wiring footprint logic per enemy. See **§1.1**.

---

## 1.1 — Physical deliverables (prefabs & assets)

All paths under `Assets/Prefabs/Actor/Enemy/`. Use **Unity Prefab Variants** (inherit overrides from parent prefab; do not duplicate the full `Enemy` hierarchy).

### Prefab hierarchy (required)

```
Enemy.prefab                    ← existing 1×1 baseline (unchanged behavior)
└── MultiTileEnemy.prefab       ← NEW variant of Enemy.prefab
    ├── GiantSkeletonEnemy.prefab   ← NEW variant of MultiTileEnemy (2×2)
    └── SnakeEnemy.prefab           ← NEW variant of MultiTileEnemy (1×3)
```

| Asset | Parent | Role |
|-------|--------|------|
| `Enemy.prefab` | — | Today’s single-tile enemy; **no** footprint overrides; regression baseline. |
| `MultiTileEnemy.prefab` | `Enemy.prefab` | Shared multi-tile wiring: footprint component/fields on `EnemyController`, default attack profile list, editor footprint gizmo hook, sprite pivot rules (§5.4). Footprint size left at 1×1 on the base variant or unset until children override. |
| `GiantSkeletonEnemy.prefab` | `MultiTileEnemy.prefab` | **2×2** rectangle, bottom-left anchor, both attack profiles (§4.3), distinct sprite/scale/color as needed. |
| `SnakeEnemy.prefab` | `MultiTileEnemy.prefab` | **1×3** snake layout, head anchor, `AdjacentSingle` only (v0), distinct sprite/scale, default facing documented on prefab. |

### Per-prefab physical requirements

**MultiTileEnemy.prefab**

- Created as a **Prefab Variant** of `Enemy.prefab` (Inspector: parent = `Enemy`).
- Adds or enables whatever component/serialized fields the implementation uses for footprints (e.g. `EnemyFootprint` on the same `GameObject` as `EnemyController`, or new fields on `EnemyController`).
- Inherits: `EnemyController`, `EnemyAiBrain`, `GridMover`, `CharacterStats`, `SpriteRenderer`, tag `Enemy`, layer, turn/AI hooks — same as parent unless explicitly overridden.
- **Must not** break spawning or moving a 1×1 instance when footprint fields are default (1×1).

**GiantSkeletonEnemy.prefab**

- Variant of `MultiTileEnemy.prefab`.
- `footprintLayout = Rectangle`, `footprintWidth = 2`, `footprintHeight = 2`.
- Attack profiles: `AdjacentSideSweep` + `AdjacentSingle` (§4.3).
- `SpriteRenderer`: sprite and/or `transform.localScale` so the art **reads as 2×2 tiles** in the Scene view (placeholder art acceptable for v0).
- Suggested default stats (overridable): higher `hp` than base `Enemy` (e.g. 20+); document final numbers in prefab or a linked `ScriptableObject` when balance is set.

**SnakeEnemy.prefab**

- Variant of `MultiTileEnemy.prefab`.
- `footprintLayout = SnakeHeadBody`, footprint extent **1×3** (width × height per §5.3 authoring convention: document on prefab as `1` wide × `3` long along facing, or equivalent enum fields).
- Attack profile: `AdjacentSingle` only for v0.
- Sprite/scale reads as **three tiles end-to-end** along default facing (East unless design picks otherwise).
- Default `FacingDirection` serialized so §5.3 body offsets are unambiguous in tests.

### Non-prefab physical goals (v0)

| Deliverable | Requirement |
|-------------|-------------|
| `.meta` files | Committed for each new prefab (Unity-generated). |
| Editor gizmo | Selecting a multi-tile prefab instance in Scene view draws footprint outline (anchor + occupied cells). |
| Test scene or room | At least one scene/room tilemap where **GiantSkeletonEnemy** fits in a 2×2 alcove and **cannot** enter an adjacent 1-wide corridor; **SnakeEnemy** fits in a 1-wide tunnel lengthwise. (Can be an existing dev scene.) |
| Original `Enemy.prefab` | Still spawns and fights as today; no mandatory footprint component values that change 1×1 behavior. |

### Out of scope (physical, v0)

- New 3×3 / 4×4 boss prefabs (phase 2).
- Unique animation controllers per large enemy (reuse or none for v0).
- Addressables / pooling changes beyond what `Enemy.prefab` already uses.

---

## 2. Glossary

| Term | Definition |
|------|------------|
| **Anchor** | The authoritative `GridPosition` (bottom-left of the footprint for rectangular enemies). |
| **Footprint** | Set of grid cells occupied by one enemy instance. |
| **Footprint size** | Width × height in tiles (`Vector2Int`), axis-aligned in v0. |
| **Owner** | One `GameObject` / `IBattleTarget` spanning the whole footprint. |
| **Head anchor** | Snake-specific: anchor cell is the **head**; body extends in the enemy’s **facing** direction (no rotation of the 1×3 layout in v0). |

---

## 3. Resolved design decisions

| # | Topic | Decision |
|---|--------|----------|
| 1 | Anchor convention | **Bottom-left** cell of the axis-aligned bounding box (for 2×2 and other rectangles). |
| 2 | Snake layout | **Anchor at head**; body cells extend along **facing** (see §5.3). |
| 3 | Giant Skeleton attacks | **Both** profiles on one prefab; AI picks by geometry (§4.2, §10): **side sweep** for cardinal-adjacent targets, **single-target** for diagonal-corner targets. |
| 4 | Adjacency for melee | **Manhattan** distance ≤ 1 from player to **nearest** footprint cell (see §4). Multi-tile **diagonal corner** band uses **AdjacentSingle** only (§4.2). |
| 5 | Diagonal movement | **Allowed** for multi-tile enemies (8-connected), with **footprint-aware** corner-cutting rules (§6). |
| 6 | Narrow passages | **Footprint must fit** — entire footprint must be placeable (§7). |
| 7 | Snake rotation | **No** rotation of the 1×3 footprint in v0; facing may still flip sprite / AI direction; body offset set is fixed relative to facing enum. Rotation = future phase. |

---

## 4. Adjacency: Manhattan + diagonal corner band (resolved)

### 4.1 — Manhattan (primary)

**Rule:** A cell is **Manhattan-adjacent** to a footprint iff `|dx| + |dy| ≤ 1` where `(dx, dy)` is the offset from the cell to the **nearest** footprint cell.

| Metric | Rule | Effect on a 2×2 enemy |
|--------|------|------------------------|
| **Manhattan** (chosen) | `\|dx\| + \|dy\| ≤ 1` to nearest footprint cell | Only **edge** neighbors of the occupied rectangle count (north/south/east/west strips). |
| **Chebyshev** (not used for v0 melee) | `max(\|dx\|, \|dy\|) ≤ 1` | Would also include the four **corner** tiles around the 2×2 block. |

**v0 melee bump and standard adjacent checks** use **Manhattan** only (1×1 enemies unchanged).

**Examples**

- Player north of the top row of a 2×2 skeleton: **Manhattan-adjacent** → eligible for **side sweep** (§10.2).
- Player beside the snake’s **tail** cell with Manhattan ≤ 1: **in range** (single-target for Snake v0).

### 4.2 — Diagonal corner band (multi-tile only)

For axis-aligned footprints with width or height &gt; 1, the four tiles that touch only the **corner** of the footprint’s bounding box are **diagonal-corner-adjacent**:

- Chebyshev ≤ 1 to some footprint cell, **but**
- **Not** Manhattan-adjacent to any footprint cell.

On a 2×2 anchor at `(x, y)`, the diagonal-corner cells are:

`(x-1, y+height)`, `(x+width, y+height)`, `(x+width, y-1)`, `(x-1, y-1)`.

**Combat rule:** These tiles are **in extended melee range** only for **`AdjacentSingle`** (player or enemy). They do **not** qualify for **`AdjacentSideSweep`** and do **not** count as Manhattan-adjacent for generic “adjacent” checks unless an ability explicitly includes the diagonal band.

**Giant Skeleton (design intent):**

| Player position relative to 2×2 | Attack profile |
|--------------------------------|----------------|
| Cardinal **side** (Manhattan-adjacent along N/S/E/W of the block) | **AdjacentSideSweep** (all party on that side in range) |
| **Diagonal corner** tile (§4.2 band) | **AdjacentSingle** only |

**Examples**

- Player at the tile **diagonally** below the bottom-left **corner** of a 2×2 skeleton: **not** Manhattan-adjacent; **in** diagonal-corner band → skeleton uses **single-target** attack; player may melee the skeleton from that tile (one target).
- Player due **north** of the top edge: Manhattan-adjacent → **side sweep**.

**Range abilities (future):** Document per ability; v0 player bump into a footprint cell still hits the owner regardless of Manhattan vs corner band.

### 4.3 — Profile selection priority (Giant Skeleton)

When the enemy has both `AdjacentSideSweep` and `AdjacentSingle` enabled:

1. If any party member is Manhattan-adjacent on a **side** eligible for sweep → execute **AdjacentSideSweep** (§10.2).
2. Else if any party member is only in the **diagonal corner** band → execute **AdjacentSingle** (§10.1).
3. Else → no melee attack; move or idle per AI.

---

## 5. Footprint layout

### 5.1 — Rectangular enemies (e.g. Giant Skeleton 2×2)

- **Anchor** = bottom-left cell of the rectangle.
- Occupied cells: `(anchor.x + ox, anchor.y + oy)` for `ox ∈ [0, width-1]`, `oy ∈ [0, height-1]`.
- **Giant Skeleton:** `width = 2`, `height = 2` → four cells.

### 5.2 — Default 1×1

- `width = 1`, `height = 1` → footprint = `{ anchor }` only.

### 5.3 — Snake (1×3, head anchor, no footprint rotation in v0)

- **Anchor** = **head** cell.
- Body occupies two additional cells along the enemy’s `FacingDirection` (away from the head):

| Facing | Head (anchor) | Body cell 2 | Body cell 3 |
|--------|---------------|-------------|-------------|
| North | `(x, y)` | `(x, y-1)` | `(x, y-2)` |
| South | `(x, y)` | `(x, y+1)` | `(x, y+2)` |
| East | `(x, y)` | `(x+1, y)` | `(x+2, y)` |
| West | `(x, y)` | `(x-1, y)` | `(x-2, y)` |

- Changing facing **does not** rotate the 1×3 pattern in v0 beyond this fixed mapping (no “sideways snake” length-3 along X when facing changed in a way that would rotate the segment — see §12 phase 2).
- Movement slides all three cells together when the head moves one step.

### 5.4 — Visual pivot

- World position centers on the footprint bounds (e.g. 2×2 anchor bottom-left → sprite center at anchor + `(width/2, height/2)` in world units, matching current `GridMover` half-cell offset convention).

---

## 6. Grid registration and queries

### 6.1 — Multi-cell registration

- `GridManager` maps **each** footprint cell → the same `IBattleTarget` / owner.
- `GetActorAt(cell)` returns the large enemy if **any** part of its footprint occupies `cell`.
- Move registration clears **all** old cells and registers **all** new cells in one atomic operation; failure restores the previous footprint.

### 6.2 — API extensions

Extend `IBattleTarget` or add `IGridFootprint`:

- `Vector2Int FootprintSize` (or width/height).
- `IEnumerable<Vector3Int> GetOccupiedCells()` (derived from anchor + size + facing rules).
- `bool Occupies(Vector3Int cell)`.

`GridPosition` remains the **anchor** for save data, pathfinding start, and existing callers.

### 6.3 — Query rules

- **AOE / radius lists:** If multiple footprint cells fall in an area, the enemy is included **once** (dedupe by `Owner`).
- **Distance for targeting:** Use distance from attacker to the **nearest** footprint cell of the target (unless an ability specifies anchor-only).

### 6.4 — `GetAllActors`

- Enumeration returns **unique** battle targets (one entry per owner), not one per cell.

---

## 7. Narrow passages: “footprint must fit” vs “per-tile walkable” (resolved: footprint must fit)

| Rule | What it checks | 2×2 in a 1-tile-wide corridor |
|------|----------------|-------------------------------|
| **Per-tile walkable only** | Each cell of the **new** footprint is walkable and empty. | Can fail in inconsistent ways: the four cells might each be walkable while the **shape** cannot exist in a 1-wide hall (e.g. 2×2 spanning two parallel 1-wide tunnels). |
| **Footprint must fit** (chosen) | Same as above **plus** placement is rejected if the footprint would overlap tiles that are not part of a sufficiently wide **contiguous walkable region** for that shape — practically: **cannot place a 2×2** unless there is a 2×2 block of walkable, unoccupied tiles; **cannot place 1×3** unless all three cells fit in walkable space along the snake axis. |

**v0 implementation guidance:** For axis-aligned rectangles, a move is valid iff **every** footprint cell is walkable, unoccupied (except self), and the **bounding box** does not straddle a choke point narrower than `min(width, height)` where map topology requires it. Minimum practical test: all footprint cells walkable + no overlap with other actors + **corridor width ≥ max(width, height)** along the movement direction when moving through known 1-wide tiles (use map walkability only if corridor metadata is unavailable).

**Giant Skeleton:** Cannot enter a hallway that is only **one tile wide**.

**Snake:** Can traverse a 1-wide tunnel **lengthwise** (1×3 aligned with corridor) if all three cells are walkable; cannot occupy a 1×1 pocket.

---

## 8. Movement

### 8.1 — Whole-body step

- One turn action moves the **entire** footprint by one grid offset applied to the anchor (same `TryMove(direction)` entry point).
- All destination cells must pass §7 validation.

### 8.2 — Eight-direction movement

- Multi-tile enemies may use **diagonal** steps (8-connected), consistent with `GridAStarPathfinder`.
- **Corner-cutting:** A diagonal step is allowed only if the footprint’s destination cells satisfy the same corner-clear rules as single-tile actors, evaluated for **every** cell of the footprint (no cutting through corners that would leave part of the body in a wall).

### 8.3 — Pathfinding

- `GridAStarPathfinder.CanEnter` (and enemy AI) must treat the seeker as a **footprint** at each candidate anchor position.
- Goal tile may still be occupied by the chase target (player); intermediate cells must fit the seeker’s footprint.

### 8.4 — Spawn

- Spawn only if the full footprint is valid at the spawn anchor; otherwise log error and skip spawn (or shift — designer choice in tooling, default **fail**).

### 8.5 — Party / formation

- No swapping into a footprint tile.
- Formation rush and follower pathing treat **any** footprint cell as blocked by that owner.

---

## 9. Player → multi-tile enemy

- Moving into **any** hostile footprint cell triggers **bump** against that enemy (one action, one owner).
- Melee from an adjacent tile: **Manhattan** ≤ 1 to **any** defender footprint cell, **or** player stands in the defender’s **diagonal corner** band (§4.2) for multi-tile footprints only.
- Player cannot share a cell with a hostile footprint (no co-occupancy).

---

## 10. Attack profiles (data-driven)

Enemies may enable one or both profiles. **Giant Skeleton** enables **both**; runtime picks by §4.3 (sides → sweep, diagonal corners → single).

### 10.1 — `AdjacentSingle`

- **Trigger:** Some party member is in the **diagonal corner band** (§4.2), **or** Manhattan-adjacent when sweep is not selected / not enabled.
- **Effect:** Deal melee damage to **one** target.
- **Target selection (v0):** Active party leader if in range; else closest party member to anchor (Manhattan to anchor); tie-break deterministic (instance id).

### 10.2 — `AdjacentSideSweep`

- **Trigger:** Some party member is **Manhattan-adjacent** to **any** footprint cell **and** lies on the **attack side** relative to enemy facing (not in the diagonal corner band only).
- **Sides:** Cardinal half-planes from anchor-based footprint geometry:
  - **North side:** target cell `y` ≥ top row of footprint (for 2×2, `y ≥ anchor.y + 1`).
  - **South / East / West:** analogous.
- **Effect:** Melee damage to **all** party members in range on that side (one action, multiple `TakeDamage` calls).
- **Side choice (v0):** If members on multiple sides, pick the side with the **most** in-range targets; tie → side of active party leader if in range; else clockwise priority N, E, S, W.

### 10.3 — Future profiles (documented, not v0)

| Profile | Purpose |
|---------|---------|
| `ReachTile` | Strike one tile within range (Snake lunge). |
| `ReachLine` | Hit tiles along a line (Snake breath). |
| `StompRadius` | Boss AOE around footprint |

### 10.4 — AI integration

- Replace `EnemyAiBrain` check `cheb(anchor, player) ≤ 1` with footprint-aware range: Manhattan-adjacent and/or diagonal corner band (§4) → evaluate profiles per §4.3.
- If attack triggers, consume turn action (no move that turn) unless design later allows move+attack.

---

## 11. Sensing, threat, and visibility

- **LOS / cone sight (v0):** **Multi-origin union** — shadow LOS is cast from **every** footprint cell and unioned; cone/range to a target uses the **nearest** occupied cell to that target. Body does **not** block LOS for other actors in v0.
- **Combat threat:** Party tension uses nearest footprint cell vs party for distance/LOS buckets.
- **Fog / revealed tiles:** When an enemy is seen, reveal **all** footprint cells.

---

## 12. Damage, death, and effects

- **Single HP pool** per enemy; damage to any footprint tile damages the owner.
- **Death:** Unregister all footprint cells; destroy `GameObject`.
- **Knockback / push (v0):** Large enemies are **not knockbackable** unless a future ability explicitly moves the whole footprint.

---

## 13. Authoring

### 13.1 — Prefab / definition fields

| Field | Type | Notes |
|-------|------|--------|
| `footprintWidth` | int ≥ 1 | |
| `footprintHeight` | int ≥ 1 | |
| `footprintLayout` | enum | `Rectangle`, `SnakeHeadBody` (v0) |
| `primaryAttackProfile` | enum | Default `AdjacentSideSweep` for Giant Skeleton |
| `secondaryAttackProfiles` | list | Optional `AdjacentSingle` on Giant Skeleton |
| `allowDiagonalMovement` | bool | Default true for multi-tile |

### 13.2 — Sample content (v0)

See **§1.1** for variant hierarchy and paths.

| Prefab asset | Footprint | Attack profiles | Notes |
|--------------|-----------|-----------------|--------|
| `MultiTileEnemy.prefab` | 1×1 default (inherits) | Configurable | Shared multi-tile base; variant of `Enemy.prefab` |
| `GiantSkeletonEnemy.prefab` | 2×2, bottom-left anchor | Sweep + single | §4.3 geometry selects profile per turn |
| `SnakeEnemy.prefab` | 1×3, head anchor | `AdjacentSingle` only | No footprint rotation v0 |

---

## 14. Phased delivery

| Phase | Scope |
|-------|--------|
| **v0** | Rect + snake layout, grid registration, movement, corridor fit, Manhattan adjacency + diagonal corner band, diagonal move, `AdjacentSingle` + `AdjacentSideSweep`, samples, tests |
| **v1** | `ReachTile` / `ReachLine` for Snake; corridor metadata on maps |
| **v2** | 3×3 / 4×4 bosses, custom shapes, footprint rotation for snake, body blocks LOS |

---

## 15. Acceptance criteria

- Given a **2×2** enemy in a 2×2 room, all four cells return the same owner from `GetActorAt`.
- Given a **1-tile-wide** corridor, a 2×2 enemy **cannot** move into a position overlapping that corridor.
- Given a player on a **diagonal corner** tile of a 2×2 (§4.2), player melee **succeeds**; skeleton counter-attack uses **AdjacentSingle** only.
- Given a player **Manhattan-adjacent** on the **east** side only, skeleton **does not** use sweep from a diagonal-corner-only position.
- Given a **Giant Skeleton** and two party members Manhattan-adjacent on its **east** side, one **AdjacentSideSweep** hits **both**.
- Given one member on the east side only, side sweep hits **one**.
- Given members only on diagonal corner tiles (no Manhattan-adjacent targets), skeleton uses **AdjacentSingle** once per action.
- Given members on both an east **side** and a diagonal corner, **AdjacentSideSweep** runs (§4.3 priority).
- Given a **Snake** facing East, footprint = head + two cells east; moving east moves all three cells atomically.
- Given **Snake** facing changed North ↔ South in v0, body cells follow §5.3 table (fixed mapping), not rotated segment.
- Given a **fireball** overlapping two footprint cells, the enemy takes damage **once**.
- Given existing **`Enemy.prefab`**, no new fields required; behavior unchanged.
- Given **`MultiTileEnemy.prefab`** is a **Prefab Variant** of `Enemy.prefab`, overriding a child field does not strip inherited `EnemyController` / `GridMover` / `EnemyAiBrain` components.
- Given **`GiantSkeletonEnemy.prefab`** placed in a 2×2 alcove, all four footprint cells register on play; sprite bounds visually cover 2×2 tiles.
- Given **`SnakeEnemy.prefab`** with default facing, three footprint cells register in a line per §5.3.
- Given **`GiantSkeletonEnemy`** in scene next to a 1-wide corridor, the enemy cannot move into the corridor.
- Given **`Enemy.prefab`** (not a variant child), spawning still uses single-cell registration only.

---

## 16. Code touchpoints (implementation checklist)

| Area | Action |
|------|--------|
| `IBattleTarget` / `IGridFootprint` | Footprint size + occupied cells |
| `GridManager` | Multi-cell register, move, unregister, deduped `GetAllActors` |
| `GridMover` | Atomic footprint move; sync visual center from bounds |
| `BaseActor.TryMove` | Validate full footprint; bump on any overlapping hostile cell |
| `GridAStarPathfinder` | Footprint-aware `CanEnter` + diagonal corner rules |
| `EnemyAiBrain` | Adjacency + attack profile execution |
| `TargetingResolver` | Dedupe by owner; nearest-cell distance |
| `CombatThreatCoordinator` | Nearest footprint cell vs party |
| Data | `EnemyFootprintDefinition` or fields on `EnemyController` |
| Prefabs | `MultiTileEnemy.prefab`, `GiantSkeletonEnemy.prefab`, `SnakeEnemy.prefab` under `Assets/Prefabs/Actor/Enemy/` (§1.1) |
| Tests | Registration, 2×2 hallway block, Manhattan + diagonal-corner melee, AOE dedupe, side sweep vs single |

---

## 17. Related documents

- [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (orthogonal)
- Future: boss encounter spec when 3×3 / 4×4 content is scheduled
