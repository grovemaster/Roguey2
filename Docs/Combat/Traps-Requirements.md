# Traps — Requirements (DCSS-style)

Traps are hazards inspired by **Dungeon Crawl Stone Soup**: they live on **floor** or **wall** cells, may be **hidden** until detected or triggered, can fire **finite or infinite** times, and apply **negative effects** (v0: **piercing damage**; future: status / debuffs). Floor traps use a **move confirmation** when **visible**; **invisible** floor traps spring with **no** warning. **Formation** followers **avoid visible** floor trap tiles when rushing. v0 ships three traps for **SampleScene** placement tests: **spike** (floor), **bear** (floor), **dart** (wall).

**Depends on:** `MapManager` (`floorMap`, `wallMap`, `IsWalkable`, `IsWall`), `PlayerCommandProcessor`, `FormationRushService`, `PartyManager`, `TurnManager`, `CharacterStats` / `Stat` (`SkillType.Perception`), `HealthComponent.TakeDamage`, `DamageType.Pierce`, [Auto-pickup confirmation](../Inventory/Auto-Pickup-Confirmation-Requirements.md) (move-gate UX pattern), [Essence floor pickup](../Essence/Sudden-Strength-Skeleton-Drop-And-Floor-Pickup-Requirements.md) (separate move gate ordering).

**Related:** SampleScene tile assets — floor `Scavengers_SpriteSheet_25` (sprite index 50 on `Scavengers` sheet), wall tile `guid: a770cea7164a14f4cbe48a9b5e25548f` (sprite index 64). Procedural floor gen **not** implemented — disguises reference these until generation exists.

**Explicitly out of scope (v0):** **Corner wall** traps, trap generation in procedural maps, trap disarm skill, enemy-triggered traps, poison/status implementation (hook only), save/load trap state.

---

## 1. Goals

**G1 — Floor and wall traps**  
Support **`TrapPlacement.Floor`** and **`TrapPlacement.Wall`** with different trigger geometry (§5–6).

**G2 — Visibility and detection**  
**Visible-by-default** traps always render revealed art. **Invisible** traps show a **disguise** (normal floor/wall from SampleScene) until **detected** or **triggered**; detection uses **Perception** (§4).

**G3 — Trigger limits**  
Per-trap **`TriggerLimit`**: `Once`, `Finite(n)`, `Infinite` (§7).

**G4 — Player move UX**  
**Visible** floor trap on destination → **confirmation** before enter. **Invisible** floor trap → **no** dialog; trap fires on enter. **Wall** traps fire from **trigger tiles**, not from stepping on the wall.

**G5 — Formation avoidance**  
When formation is active, followers **must not** plan moves onto tiles with **visible** floor traps (§8).

**G6 — Reveal on trigger**  
Any trap that fires becomes **visible** (revealed sprite) even if it was invisible.

**G7 — v0 content**  
Author **Spike Trap**, **Bear Trap**, **Dart Trap** assets; place instances in **SampleScene** for manual QA.

**G8 — Piercing damage**  
All three v0 traps deal authored **`piercingDamage`** via `DamageType.Pierce`.

---

## 2. Design decision — trap tile vs separate trap object

### Question

Should a grid cell **be** a trap tile, or should traps be **separate entities** registered to a cell?

### Recommendation (locked for this project)

**Register a trap instance on a cell; do not replace walkability with a special “trap tile type” on the floor tilemap.**

| Approach | Verdict |
|----------|---------|
| **Cell metadata + overlay (chosen)** | A **walkable floor cell** (or **wall cell** for wall hosts) keeps normal `MapManager` rules. A **`TrapInstance`** (runtime or prefab) registers at `Vector3Int` with `TrapDefinition` data. **Disguise** = sample scene floor/wall tile look; **revealed** = trap overlay sprite/tile on a **`TrapOverlay` tilemap** or sprite child. |
| **Trap replaces floor tile** | Rejected — couples traps to tilemap painting, fights invisible disguise, complicates formation pathfinding and floor-gen later. |
| **Trap occupies extra parallel cell** | Rejected — not DCSS-like; doubles grid confusion. |

**Consequences**

- `IsWalkable(trapFloorCell)` remains **true** (floor trap is stepped on).
- `TrapService.IsFloorTrapAt(tile)` / `GetTrapAt(tile)` is the query API.
- Wall trap **host** is the **wall** coordinate; **trigger** coordinates are **adjacent floor** cells (§6).
- SampleScene testing: place **`TrapInstance`** prefabs (or dev markers) that register on `Start` — no hand-editing of trap cells into the floor tilemap.

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Trap definition** | `TrapDefinition` ScriptableObject — static data (art, damage, limits, visibility). |
| **Trap instance** | Placed trap in a scene / registry with runtime state (charges left, revealed, detected). |
| **Disguise** | Sprite/tile shown while trap is **not** visible to the player. |
| **Revealed** | Trap art shown after detection or trigger. |
| **Detected** | Invisible trap is known to the player (shows revealed art; counts as **visible** for formation/confirm). |
| **Visible trap** | `initialVisibility == Visible` **or** `instance.IsDetected` **or** `instance.HasTriggered` (revealed). |
| **Floor trap** | Host cell is a **walkable floor** tile. |
| **Wall trap** | Host cell is a **wall** tile; player does not walk onto the wall. |
| **Trigger tile** | Floor cell that can fire a **wall** trap when entered. |
| **Player phase** | Same as other docs: `TurnManager.NotifyPartyTurnStart` boundary (for future turn-based trap effects). |

---

## 4. Visibility and Perception

### D4.1 — `TrapVisibility` (authoring)

| Value | Behavior |
|-------|----------|
| **Visible** | Always uses **revealed** art; always counts as visible for confirm + formation. |
| **Invisible** | Starts disguised; may become detected (§4.2). |

### D4.2 — Detection rule (invisible traps)

Each **player phase start** and whenever a party member **enters a new tile** (optional optimization: only on move), re-evaluate each **invisible, not-yet-detected** trap within **perception range** (v0: trap’s own cell + adjacent cells, or LOS to trap cell — **locked: trap cell only** for v0).

**Detected** when **any** party member on the map satisfies:

```text
PerceptionScore(member) >= trapDefinition.detectionThreshold
```

Where **`PerceptionScore`** = `CharacterStats` skill value for `SkillType.Perception` (same `Stat` pipeline as other skills). **`detectionThreshold`** is per `TrapDefinition` (suggested default **12** for SampleScene tuning).

**R4.2.1 — “All party members must have less than perception” (author intent)**  
Equivalent to: trap **stays disguised** while **no** eligible party member meets the threshold. Once **one** member with sufficient Perception **would notice** the trap (v0: party-wide omniscience for detection — no LOS), mark **`IsDetected = true`** and switch to revealed overlay.

**R4.2.2 — Visible traps**  
Skip detection; always revealed.

### D4.3 — Disguise art (v0 — SampleScene)

| Host | Disguise source |
|------|-----------------|
| **Floor** | Same tile as SampleScene floor: `Tile` **Scavengers_SpriteSheet_25** / sprite **21300050** on sheet `fbe1e7b94bb1a4a099d5bcb15a00141c`, **or** read current `floorMap.GetSprite(cell)` at bake/placement time and store reference. |
| **Wall** | SampleScene wall tile **guid `a770cea7164a14f4cbe48a9b5e25548f`** / sprite **21300064**. |

Invisible traps **do not** paint a special “trap” tile on `floorMap`/`wallMap`; overlay is hidden or shows disguise on **`TrapOverlay`** layer.

### D4.4 — Reveal on trigger

When a trap **fires** (§7): set **`HasTriggered = true`**, **`IsRevealed = true`** (show revealed sprite). Invisible traps become **visible** for confirm/formation rules **after** first trigger even if detection never ran.

---

## 5. Floor traps

### F5.1 — Placement

- Host = **one walkable floor** `Vector3Int`.
- **At most one** floor trap per cell.
- Walkable check: `MapManager.IsWalkable(host)`.

### F5.2 — Trigger

- Party member **enters** host cell (anchor tile after successful move).
- **Invisible:** no confirm dialog; resolve trap **after** move lands (§7).
- **Visible (or detected):** **TrapMoveGate** blocks move until dialog (§9).

### F5.3 — Move confirmation (visible only)

**Template (recommended):**

```text
{actorName} is about to enter a tile with a visible {trapDisplayName}. Move onto this tile anyway?
```

**No / Esc:** no move, no turn. **Yes:** move, then trap resolves (§7); turn consumed by move.

**R5.3.1** Invisible floor traps **never** use this dialog.

### F5.4 — v0 floor trap specs

| Trap | `TriggerLimit` | `piercingDamage` (default) | Notes |
|------|----------------|----------------------------|--------|
| **Spike Trap** | **Infinite** | **8** | Classic recurring floor spikes. |
| **Bear Trap** | **Once** | **15** | Single heavy hit; after fire, remains revealed, no more damage. |

---

## 6. Wall traps

### F6.1 — Placement rules

- Host = **wall** cell: `MapManager.IsWall(host)`.
- **Never** on **corner** wall cells (§6.2).
- **Dart Trap** v0 wall host only.

### F6.2 — Corner wall (forbidden)

**Corner wall cell (v0 topology rule):** a wall tile whose **orthogonal floor neighbors** form an **L-shape** (two floor neighbors on **perpendicular** sides, e.g. N+E). **Straight** wall segment for dart host: wall has floor neighbors on **opposite** sides (N+S **or** E+W) **or** exactly **one** floor neighbor (dead-end wall face). **Corner Wall traps** = future work (§12).

### F6.3 — Trigger tiles (not the wall cell)

- **`triggerTiles`** = orthogonally adjacent **walkable floor** cells to the wall host (Manhattan distance 1 on floor).
- **`triggerRange`** on definition (Dart: **1**) — v0 only adjacent floors count.
- When a party member **enters** a trigger tile, the **wall trap** on the adjacent wall fires toward/over the actor per definition (v0: apply damage to **entering actor** only).

### F6.4 — Visibility

Same detection/disguise/reveal rules as floor traps (disguise = wall sprite from SampleScene).

**Note:** Author text mentioned wall traps “always visible” in one sentence; **locked:** wall traps use the **same** Visible / Invisible + detection rules as floor traps. **After trigger**, always revealed.

### F6.5 — v0 wall trap spec

| Trap | `TriggerLimit` | `triggerRange` | `piercingDamage` (default) |
|------|----------------|----------------|----------------------------|
| **Dart Trap** | **Finite (3)** | **1** (adjacent floor triggers) | **10** |

Example: player steps on floor tile **east** of dart wall host → dart fires, 10 pierce damage, charges 3→2; trap becomes visible.

---

## 7. Trigger resolution

### F7.1 — Order on enter

1. Complete movement onto **floor** cell (or **trigger** cell for wall).
2. If **floor trap** on entered cell → resolve (respect charges).
3. If entered cell is **trigger** for adjacent wall trap(s) → resolve each (v0: one dart per wall; multiple walls rare).

### F7.2 — Charge consumption

| `TriggerLimit` | Behavior |
|----------------|----------|
| **Once** | First fire applies effect; subsequent enters do nothing. |
| **Finite(n)** | Decrement `chargesRemaining`; at 0, no more fires. |
| **Infinite** | Always fires on enter. |

### F7.3 — Damage (v0)

```csharp
health.TakeDamage(trapDefinition.piercingDamage, DamageType.Pierce, trapSource);
```

Future: `TrapEffect` list (stat debuff, poison status when that system exists).

### F7.4 — Noise / logging

Debug log: `[Trap] {trapName} triggered by {actor} at {tile} for {damage} Pierce.`

---

## 8. Formation — avoid visible floor traps

### F8.1 — Leader

Leader manual move uses **TrapMoveGate** for **visible** floor traps (§5.3) — may still choose Yes.

### F8.2 — Followers (`FormationRushService`)

Extend **`IsValidMove`** (or trap-aware wrapper):

```text
if (TrapService.IsVisibleFloorTrapAt(tile)) return false;
```

Followers pick alternate neighbors (existing rush search). **Invisible** (undetected) floor traps are **not** avoided — followers may step on them unknowingly.

### F8.3 — After leader reveals a trap

Leader triggers or detects trap → tile becomes **visible** → followers treat it as blocked for subsequent rushes in the same player phase and future phases.

---

## 9. Move gate ordering

When player moves to `dest`, intercept **before** `TryMove` (same hook as `AutoPickupMoveGate`):

| Priority | Gate | Condition |
|----------|------|-----------|
| 1 | **TrapMoveGate** (floor, visible) | Visible floor trap on `dest` |
| 2 | **EssenceMoveGate** | Floor essence on `dest` |
| 3 | **AutoPickupMoveGate** | Confirm-gated items on `dest` |

**Invisible** floor trap on `dest`: **no** TrapMoveGate; move then trap fires.

**Wall trap:** no move gate (trigger fires post-move on trigger tile).

---

## 10. Data model

### D10.1 — `TrapDefinition` : ScriptableObject

| Field | Purpose |
|-------|---------|
| `trapId` | Stable id |
| `displayName` | UI / dialog (e.g. `Spike Trap`) |
| `placement` | `Floor` \| `Wall` |
| `initialVisibility` | `Visible` \| `Invisible` |
| `detectionThreshold` | Perception check (invisible) |
| `triggerLimit` | `Once` \| `Finite` \| `Infinite` |
| `finiteCharges` | When `Finite` (Dart: **3**) |
| `triggerRange` | Wall only (Dart: **1**) |
| `piercingDamage` | int |
| `disguiseFloorTile` / `disguiseWallTile` | Optional Tile refs; default SampleScene tiles §4.3 |
| `revealedSprite` | Shown when detected/triggered |
| `futureEffects` | List placeholder for debuffs/status |

Menu: `JRogue/Traps/Trap Definition`.

### D10.2 — `TrapInstance` (scene / runtime)

| State | Purpose |
|-------|---------|
| `definition` | Reference |
| `hostCell` | `Vector3Int` |
| `chargesRemaining` | For finite |
| `IsDetected` | Perception revealed |
| `HasTriggered` | Ever fired |
| `IsRevealed` | UI overlay state |

Prefab: `TrapInstance_Floor` / `TrapInstance_Wall` registers with **`TrapService`** on play.

### D10.3 — `TrapService`

- Registry: floor traps by cell; wall traps by host + index trigger tiles.
- Queries: `IsFloorTrapAt`, `IsVisibleFloorTrapAt`, `GetWallTrapsTriggeredBy(entryCell)`.
- `EvaluateDetection()` on player phase start.
- `TryTriggerFloorTrap(actor, cell)`, `TryTriggerWallTrap(actor, triggerCell)`.

---

## 11. v0 trap assets and SampleScene

### D11.1 — ScriptableObject assets (create)

| Asset | Path (suggested) |
|-------|------------------|
| `TrapDefinition_Spike` | `Assets/Data/Traps/TrapDefinition_Spike.asset` |
| `TrapDefinition_Bear` | `Assets/Data/Traps/TrapDefinition_Bear.asset` |
| `TrapDefinition_Dart` | `Assets/Data/Traps/TrapDefinition_Dart.asset` |

### D11.2 — SampleScene test layout

- Add **`TrapOverlay`** tilemap (or sprite root) under scene grid.
- Place at least one of each trap prefab in **`SampleScene`** with clear walking paths:
  - Spike: invisible + visible variant for QA.
  - Bear: once-only underfoot.
  - Dart: straight wall with trigger tile beside corridor.
- Document coordinates in scene README comment or `Docs/Combat/Traps-SampleScene-Layout.md` (optional).

### D11.3 — Acceptance (F11.x)

**F11.1** Visible spike → confirm → Yes → damage + revealed.  
**F11.2** Invisible spike → no confirm → damage on enter.  
**F11.3** Bear → damages once, second enter safe.  
**F11.4** Dart → stepping adjacent floor fires dart; 3 charges then silent.  
**F11.5** Formation follower paths around **visible** spike, not through it.  
**F11.6** High Perception party member reveals invisible trap before step (overlay swap).

---

## 12. Future

| Item | Notes |
|------|--------|
| **Corner wall traps** | Separate spec; corner topology validator |
| Status / debuff on trap | `TrapEffect` when status system exists |
| LOS for detection | Per-member cone instead of party-wide |
| Procedural disguise | Sample from floor-gen tile at placement |
| Trap disarm / reveal by Scan spell | — |

---

## 13. Art — Stealthix Option A (approved)

**Status:** **Imported** (product approval 2026-05-25). Option B (Foozle) not used.

| | |
|--|--|
| **License** | CC0 — `Assets/Art/Traps/ThirdParty/Stealthix/LICENSE.txt` |
| **Source** | [OpenGameArt — Animated Traps](https://opengameart.org/content/animated-traps) (`Traps.zip`) |
| **Original sheets** | `Assets/Art/Traps/ThirdParty/Stealthix/*.png` |
| **Unity-ready slices** | `Assets/Art/Traps/Sprites/` (32×32, Point filter in Unity) |

### Locked sprite mapping

| Trap | Source | Sliced sprites |
|------|--------|----------------|
| **Spike Trap** | `Pit_Trap_Spikes.png` | `SpikeTrap_Revealed.png` |
| **Bear Trap** | `Bear_Trap.png` | `BearTrap_Revealed_Idle.png`, `BearTrap_Revealed_Triggered.png` |
| **Dart Trap** | `Push_Trap_Front.png` (wall emitter) | `DartTrap_Revealed_Idle.png`, `DartTrap_Revealed_Fire.png` |

See `Assets/Art/Traps/ThirdParty/Stealthix/README.md` for unused sheets.

### Locked v0 damage (piercing)

| Trap | `piercingDamage` |
|------|------------------|
| Spike | **8** |
| Bear | **15** |
| Dart | **10** |

Wire these on `TrapDefinition_*` assets when trap code is implemented (§11).

---

## 14. Implementation status

| Deliverable | Status |
|-------------|--------|
| `TrapDefinition` / `TrapService` / `TrapInstance` | **Not created** |
| Move gate + confirm UI | **Not created** |
| Formation `IsValidMove` trap check | **Not created** |
| Perception detection pass | **Not created** |
| Spike / Bear / Dart assets | **Not created** |
| SampleScene placements | **Not created** |
| Third-party sprites (Stealthix CC0) | **Imported** — §13 |
| Sliced sprites in `Assets/Art/Traps/Sprites/` | **Done** |

---

## 15. Traceability

| Request | Section |
|---------|---------|
| Floor vs wall traps | §5–6 |
| Visible / invisible + Perception | §4 |
| Disguise vs revealed sprites | §4.3, §10 |
| All traps visible after trigger | §4.4 |
| Finite / once / infinite triggers | §7, §5.4, §6.5 |
| Visible floor trap move confirm | §5.3, §9 |
| Formation avoids visible traps | §8 |
| Negative effects (damage; future status) | §7, §10 |
| No confirm on invisible floor trap | §5.2 |
| Wall trigger tiles, range, no corners | §6 |
| Spike / Bear / Dart piercing damage | §5.4, §6.5, §7 |
| Tile vs trap architecture question | §2 |
| SampleScene test placement | §11 |
| Sprite search + ask before download | §13 |
