# Interactable floor tiles — Requirements (lever switches)

**Interactable tiles** are **floor cells** the player **cannot enter** but can **bump** from an adjacent cell to trigger behavior. v0 focuses on **lever switches** (DCSS-style): **off** = handle points **right**, **on** = handle points **left**, **latching** (once on, never off). Activation runs **data-driven effects** (XP, chain other levers, open doors, future buffs/status). **Preconditions** gate whether a bump can flip the lever.

**Depends on:** `MapManager`, `BaseActor.TryMove` / `OnBump`, `PlayerController`, `PlayerCommandProcessor`, `PartyManager`, `TurnManager`, `PartyExperienceService`, [Environmental hazards](Environmental-Hazards-Requirements.md) (same “registry on cell” pattern), [Status effects](Status-Effects-Requirements.md) (future buff/status effects), [Traps](Traps-Requirements.md) (distinct — sprung one-shot vs interactable).

**Related:** `GrantPartyExperienceAbility`, `FormationRushService`.

**Explicitly out of scope (v0):** Toggle-off levers, walk-on pressure plates, multiplayer sync, save/load interactable state (nice-to-have later), full door system implementation (stub only for “open door” effect).

---

## 1. Goals

**G1 — Extensible interactable framework**  
One pipeline for **levers now** and **other floor features later** (altars, shrines, runes, etc.) via `InteractableTileDefinition` + pluggable **preconditions** and **effects**.

**G2 — Lever switch (v0)**  
Four test levers in **SampleScene** with chained logic per §11.

**G3 — Bump-only interaction**  
Player **cannot occupy** the lever cell; **bump** from orthogonally adjacent cell attempts activation.

**G4 — Turn cost on successful activation**  
When a bump **turns the lever on**, consume the bumper’s player action per §7.

**G5 — Preconditions**  
Any lever may require stats, other levers, flags, or **script-only** activation (no player bump).

**G6 — Composable effects**  
On activation, run zero or more **effects** (grant XP, chain lever, open door, future status).

**G7 — Art**  
Off/right and on/left sprites; import after product approval (§12).

---

## 2. Design decision — tile type vs registry

### Question

Should the lever be a **special floor tile type**, or **metadata on a normal floor cell**?

### Recommendation (locked)

**Floor cell + interactable registry + feature overlay** (same discipline as [environmental hazards](Environmental-Hazards-Requirements.md) and [traps](Traps-Requirements.md)).

| Layer | Role |
|-------|------|
| **Base floor** | `floorMap` keeps underlying walkable floor art (or uniform stone). |
| **Interactable registry** | `InteractableTileService` maps `Vector3Int` → instance + definition id. |
| **Feature overlay** | `Interactable_Overlay` tilemap/sprite: lever off (right) / on (left). |

| Approach | Verdict |
|----------|---------|
| **Registry + overlay (chosen)** | Extensible; lever does not replace proc-gen floor identity. |
| **Monolithic “lever tile” on floorMap only** | Rejected — couples art to `floorMap` paint; hard to query preconditions/effects. |

**Occupancy:** `InteractableTileService.BlocksOccupancy(cell) == true` → actors cannot `ApplyPositionChange` onto cell; movement into cell is treated as **bump** (§6).

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Interactable tile** | Feature on a floor cell; blocks occupancy; may respond to bump. |
| **Lever switch** | Latching interactable; off → on only. |
| **Bump** | Move attempt into the interactable cell from an **adjacent** cell (orthogonal v0). |
| **Activation** | Off → on transition; runs effects once. |
| **Precondition** | Predicate evaluated before activation; failure = no state change. |
| **Effect** | Action executed on successful activation (ordered list). |
| **Scripted activation** | Effect or service turns lever on **without** player bump (Lever 3). |

---

## 4. Extensibility — framework

### D4.1 — `InteractableTileId`

```csharp
public enum InteractableTileId
{
    None = 0,
    LeverSwitchFirst = 1,
    LeverSwitchSecond = 2,
    LeverSwitchThird = 3,
    LeverSwitchFourth = 4,
    // Future: PressurePlate, RuneAltar, ...
}
```

Stable ids for preconditions (`OtherInteractableOn`), scene wiring, and saves.

### D4.2 — `InteractableTileDefinition` (ScriptableObject)

| Field | Purpose |
|-------|---------|
| `interactableId` | Enum / string |
| `displayName` | UI / logs |
| `kind` | `Lever` (v0); future kinds |
| `blocksOccupancy` | **true** for levers |
| `bumpEnabled` | If false, player bump never activates (Lever 3) |
| `preconditions` | Ordered list of `InteractablePrecondition` assets |
| `onActivateEffects` | Ordered list of `InteractableEffect` assets |
| `spriteOff` / `spriteOn` | Lever: right / left |

Menu: **`JRogue/Interactables/Interactable Tile Definition`**.

### D4.3 — `InteractablePrecondition` (abstract ScriptableObject)

| Implementation | v0 use |
|----------------|--------|
| **`AlwaysTruePrecondition`** | Lever 1 |
| **`OtherInteractableOnPrecondition`** | Requires another lever’s instance **on** (`interactableId` + optional scene instance id) |
| **`StatMinimumPrecondition`** | e.g. `Strength >= N` (extensible; not required for test levers) |
| **`FlagPrecondition`** | Global/map quest flag (stub for future) |
| **`ScriptOnlyPrecondition`** | Always fails for player bump; passes for `InteractableTileService.ActivateById(..., source: Scripted)` |

**Evaluation:** **All** preconditions in the list must pass (AND). Empty list = always allowed.

### D4.4 — `InteractableEffect` (abstract ScriptableObject)

| Implementation | v0 use |
|----------------|--------|
| **`ActivateInteractableEffect`** | Sets another lever **on** (Lever 2 → 3) |
| **`GrantPartyExperienceEffect`** | `PartyExperienceService.AwardPartyExperience(amount, source)` |
| **`OpenDoorEffect`** | Calls `DoorService.Open(doorId)` (stub v0) |
| **`ApplyStatusEffect`** | Future → [status spec](Status-Effects-Requirements.md) |
| **`GrantStatBuffEffect`** | Future timed stat modifiers |

**Execution:** Run effects in **list order** on the activating lever’s definition. Chained activations (Lever 3) run **that lever’s** effect list in the same activation pass.

### D4.5 — Runtime

| Type | Role |
|------|------|
| **`InteractableTileInstance`** | Per cell: `definition`, `isOn`, `hasActivated` (latch) |
| **`InteractableTileService`** | Registry, queries, `TryBumpActivate`, `ActivateById` (scripted) |
| **`InteractableTileView`** | Overlay sprite swap off/on |

### D4.6 — Future tile kinds (not levers)

| Kind | Interaction (future) |
|------|----------------------|
| **Altar** | Bump or pray command |
| **Shrine** | Stand on / bump |
| **Rune door** | Item precondition |

Same service; `kind` drives bump rules and visuals.

---

## 5. Lever switch — behavior (v0)

### F5.1 — Visual states

| State | Sprite | Facing (design) |
|-------|--------|-----------------|
| **Off** | `spriteOff` | Handle points **right** |
| **On** | `spriteOn` | Handle points **left** |

**R5.1.1 — Latching:** After first activation, `isOn == true` permanently for the run (v0). No toggle off.

### F5.2 — Occupancy and walkability

| Rule | Detail |
|------|--------|
| **Enter cell** | **Forbidden** — `TryMove` must not place actor on lever cell. |
| **Map query** | `InteractableTileService.BlocksOccupancy(cell)` → movement resolver treats cell as blocked for **entry**. |
| **Floor** | Cell may still have `floorMap` tile underneath overlay. |

### F5.3 — Bump detection

When party member attempts move with destination `dest`:

1. If `dest` has interactable with `blocksOccupancy` and actor is **orthogonally adjacent** to `dest` (v0), treat as **bump** into interactable (not ally/enemy combat bump).
2. Call `InteractableTileService.TryBumpActivate(dest, bumper)`.

**R5.3.1** Diagonal bump: **no** (v0).

**R5.3.2** Bump into already-on lever: log “already activated”; **no** activation, **no** turn cost (§7).

---

## 6. Activation flow

### F6.1 — `TryBumpActivate(cell, bumper)`

| Step | Action |
|------|--------|
| 1 | Load instance at `cell`; if none or not `bumpEnabled`, return `Failed`. |
| 2 | If already **on**, return `AlreadyOn`. |
| 3 | Evaluate **preconditions** for `source = PlayerBump` and `bumper`. |
| 4 | If any fail, return `PreconditionFailed` (log reason). |
| 5 | Set **on**, swap sprite, run **onActivateEffects** in order. |
| 6 | Return `Activated`. |

### F6.2 — `ActivateById(id, source: Scripted)` (Lever 3)

| Step | Action |
|------|--------|
| 1 | Resolve instance by `interactableId` (scene registration). |
| 2 | Skip `bumpEnabled` check; use preconditions with `source = Scripted` (`ScriptOnlyPrecondition` **passes**). |
| 3 | Same on transition + effects. |

**R6.2.1** Player bump on Lever 3: `bumpEnabled == false` **or** `ScriptOnlyPrecondition` → **PreconditionFailed** / no bump handler.

### F6.3 — Chain example (Lever 2 → 3)

`LeverSwitchSecond` effects include **`ActivateInteractableEffect`** targeting **LeverSwitchThird**.

When Lever 2 activates, effect calls `ActivateById(LeverSwitchThird, Scripted)` → Lever 3 turns on and runs **its** effects (none required for test).

---

## 7. Turn consumption

| Result | Formation **inactive** | Formation **active** |
|--------|------------------------|----------------------|
| **`Activated`** | `TurnManager.OnPlayerActionComplete(bumper)` only | `OnPlayerActionComplete(bumper)`; then formation bookkeeping (§7.1) |
| **`AlreadyOn`**, **`PreconditionFailed`**, **`Failed`** | **No** turn spent | **No** turn spent |

**R7.1 — Formation active (mirror attack bump)**  
After `OnPlayerActionComplete(bumper)`: `SnapHistoryToCurrentPositions` (position unchanged), `ProcessFollowerRush()`, and if squad finished acting, `ForceEndPlayerTurn()` — same family as leader bump-attack in `PlayerCommandProcessor` when leader does not move.

**R7.2 — Soul Power / targeting** | Unaffected.

---

## 8. Integration — `TryMove` / `PlayerCommandProcessor`

### F8.1 — Order of resolution (destination `dest`)

1. Interactable bump (if `dest` is blocked interactable and bump valid)
2. Enemy / ally bump (`BaseActor` on `dest`)
3. Passage hazards (lava STR, etc.)
4. Hazard / essence / item move gates
5. Normal move

### F8.2 — `BaseActor.TryMove` changes

Before `IsWalkable(dest)` success path:

- If `InteractableTileService.BlocksOccupancy(dest)` and mover adjacent → `TryBumpActivate`; return **true** without changing position if activated (turn handled in §7); return **false** if precondition failed without spending turn.

**R8.2.1** Do not call `OnBump(BaseActor)` for interactable cells (no combat).

---

## 9. SampleScene test content — four levers (§11)

Author four assets + instances at assigned cells (designer-placed).

| Lever | Id | Preconditions | On activate effects | Bump? |
|-------|-----|---------------|---------------------|-------|
| **First** | `LeverSwitchFirst` | `AlwaysTrue` | *(none)* | Yes |
| **Second** | `LeverSwitchSecond` | `OtherInteractableOn(LeverSwitchFirst)` | `ActivateInteractable(LeverSwitchThird)` | Yes |
| **Third** | `LeverSwitchThird` | `ScriptOnly` (player bump always fails) | *(none)* | **No** |
| **Fourth** | `LeverSwitchFourth` | `OtherInteractableOn(LeverSwitchThird)` | `GrantPartyExperience(25)` | Yes |

**Expected QA sequence**

1. Bump **First** → on; turn ends.
2. Bump **Second** (before First on) → fails precondition.
3. Bump **Second** (after First on) → on; **Third** auto-on; turn ends.
4. Bump **Third** → no player activation.
5. Bump **Fourth** (before Third on) → fails.
6. Bump **Fourth** (after Third on) → on; party +25 XP each via `PartyExperienceService`; turn ends.

---

## 10. Functional acceptance (F10.x)

**F10.1** Cannot walk onto lever cell.  
**F10.2** Bump from adjacent activates First when off.  
**F10.3** Second blocked until First on.  
**F10.4** Second on chains Third without player bumping Third.  
**F10.5** Third never activates from player bump.  
**F10.6** Fourth grants 25 XP to all party members when Third on.  
**F10.7** Activated lever stays on; second bump does not spend turn.  
**F10.8** Formation off: only bumper marked acted; formation on: squad flow per §7.1.

---

## 11. Data assets (paths)

| Asset | Path |
|-------|------|
| Definitions | `Assets/Data/Interactables/LeverSwitch_*.asset` |
| Preconditions | `Assets/Data/Interactables/Preconditions/` |
| Effects | `Assets/Data/Interactables/Effects/` |
| Scene markers | `InteractableTileMarker` prefab or painted overlay + id |

---

## 12. Art — options (approval required)

**Do not import until you confirm.** Lever needs **off (right)** and **on (left)** at **32×32** to match SampleScene / DCSS hazard scale.

### Option A — **32×32 Dungeon Tileset** (2024, CC0) — recommended

| | |
|--|--|
| **License** | CC0 ([OGA page](https://opengameart.org/content/32x32-dungeon-tileset-0)) |
| **Download** | `dungoen_tileset_png.png` (~38 KB) |
| **Fit** | Pack lists **“Leavers”** explicitly; orthogonal dungeon style. |
| **Work** | Slice right/off and left/on frames from sheet after import. |

### Option B — **Dungeon Crawl 32×32** (already used for lava/gas)

| | |
|--|--|
| **License** | Free use ([OGA](https://opengameart.org/content/dungeon-crawl-32x32-tiles)) |
| **Fit** | No dedicated lever in subset imported; possible **pedestal** / rune press tiles as placeholder — **not ideal** for left/right handle. |
| **Use** | Only if you want zero new packs. |

### Option C — **pechvogel “Switch”** (CC-BY 3.0)

| | |
|--|--|
| **License** | [CC-BY 3.0](https://opengameart.org/content/switch-0) — attribution required |
| **Download** | `Switch.zip` — up / down / middle states |
| **Fit** | Clear switch art; may need rotate to match right/left spec. |

### Recommended

**Option A** for CC0 + explicit levers; **Option C** if you prefer classic up/down switch look and accept CC-BY.

**Import path (after yes):** `Assets/Art/Interactables/ThirdParty/...` + `Assets/Art/Interactables/Sprites/LeverSwitch_Off.png`, `LeverSwitch_On.png` + `LICENSE.txt`.

**Please reply** with Option A, B, or C (or mix). On approval, assets will be downloaded and sliced.

---

## 13. Implementation status

| Deliverable | Status |
|-------------|--------|
| `InteractableTileService` + framework | **Not created** |
| Precondition / effect SO types | **Not created** |
| Lever 1–4 definitions + SampleScene | **Not created** |
| `TryMove` bump integration | **Not created** |
| Lever sprites | **Awaiting approval** (§12) |
| Door open effect | **Stub** |
| Status / stat buff effects | **Future** |

---

## 14. Traceability

| Request | Section |
|---------|---------|
| Lever off=right, on=left, never off | §5.1 |
| Cannot walk on; can bump | §5.2–5.3, §8 |
| Turn ends on activation | §7 |
| Multiple effect types | §4.4 |
| Preconditions | §4.3, §6 |
| Extensible for other tiles | §4 |
| Four test levers | §9, §11 |
| Sprites — ask before download | §12 |
