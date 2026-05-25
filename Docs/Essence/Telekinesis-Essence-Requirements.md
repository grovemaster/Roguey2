# Telekinesis Essence — Requirements

An essence active ability that lets the **active party member** remotely pull a **floor item** into inventory using the existing **targeting reticle** flow. Costs **1 Soul Power** on successful use. Invalid confirms do **not** spend Soul Power or the player’s action.

**Depends on:** `EssenceData`, `EssenceSlotManager`, `AbilityAction`, `CharacterStats.currentSoulPower`, `PlayerCommandProcessor`, `InputHandler`, `TargetingReticleView`, `InputState.Targeting`, `FloorItemPileService`, `FloorItemEntry`, `FloorPickupQuery` / `WorldItem`, `InventoryManager` (`CanCarry`, `AddItem`), `TurnManager` (`CanActorTakeAction`, `OnPlayerActionComplete`), [Floor pickup & auto-pickup](../Inventory/Floor-Pickup-And-Auto-Pickup-Requirements.md), [Subspace inventory & encumbrance](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md) (when implemented — until then, baseline `CanCarry` / `GetTotalWeight`).

**Related:** `IronSkin` essence (`Assets/Resources/Item/Essence/IronSkin.asset`) as an authoring example; `Teleport_Standard` / `Fireball` as targeted `AbilityAction` patterns.

**Explicitly out of scope (v0):** Pulling equipped items off enemies, moving items between tiles without pickup, pulling party members, destructibles, or items inside containers on the floor. No new UI beyond the existing reticle.

---

## 1. Goals

**G1 — Targeted essence active**  
Telekinesis is an **`AbilityAction`** on a **`EssenceData`** asset, invoked from an essence slot like other actives (`requiresTarget = true`, `soulPowerCost = 1`).

**G2 — Familiar targeting UX**  
Activation enters **`InputState.Targeting`**, shows **`TargetingReticleView`**, moves the reticle with grid directions, confirms with the same binding as other targeted abilities, cancels without spending resources.

**G3 — Remote pickup**  
Confirm on a **valid floor item** removes it from the target tile and adds it to the **active member’s** inventory when encumbrance allows.

**G4 — Encumbrance fallback**  
If adding the item would cause **over-encumbrance**, the item is placed on the **active member’s current tile** (floor pile), removed from the target tile, and the action still **consumes the turn** and **Soul Power**.

**G5 — Invalid confirm is free**  
Confirm on an **invalid** target (e.g. empty tile) logs a **Debug** message with tile coordinates, **keeps** targeting mode and the reticle, and does **not** consume Soul Power or the player’s action.

**G6 — Data-first**  
Designers author one essence asset + one ability asset; no per-item code for Telekinesis.

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Active member** | `PartyManager.GetActiveMember()` — the actor whose essence slot was used. |
| **Reticle tile** | `TargetingReticleView.Position` at confirm time. |
| **Pickable floor item** | A single `FloorItemEntry` or legacy `WorldItem` on the reticle tile that manual pickup would consider a physical item (not currency / mana stone ledger pickup). |
| **Over-encumbrance** | `InventoryManager.CanCarry(instance)` is **false** for the active member after the item would be acquired (same rule as floor manual pickup today). |
| **Successful ability execution** | `AbilityAction.ExecuteCore(user, targetTile)` returns **true** → `EssenceSlotManager` deducts Soul Power; `PlayerCommandProcessor` ends targeting and completes the player action. |

---

## 3. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Essence actives** | Listed on `EssenceData.activeAbilities`; executed via `EssenceSlotManager.TryExecuteAbility` (deducts SP on success). |
| **Targeting** | `requiresTarget` → `PlayerCommandProcessor.EnterTargetingMode`; confirm calls `TryExecuteAbility(..., reticleView.Position)`. |
| **Turn on confirm** | Turn completes only when `TryExecuteAbility` returns **true** (`ApplyConfirmTarget`). |
| **Turn on failed execute** | Returns **false**; targeting state **unchanged** (reticle stays). |
| **Floor items** | `FloorItemPileService` per-tile lists; legacy `WorldItem` via `FloorPickupQuery`. |
| **Pickup encumbrance** | `InventoryManager.AddItem` fails when `!CanCarry`; manual pickup UI blocks heavy items. |
| **Telekinesis** | **Not implemented** — no assets or `AbilityAction` subclass. |

---

## 4. Content authoring

### D4.1 — `EssenceData` — `Telekinesis` (or project naming convention)

| Field | Requirement |
|-------|-------------|
| **`essenceName`** | e.g. `Telekinesis` |
| **`description`** | Player-facing: pull items from a distance using Soul Power. |
| **`statModifiers` / `resistanceModifiers` / `complexPassives`** | Empty for v0 unless design adds passive flavor later. |
| **`activeAbilities`** | Exactly one reference: **`TelekinesisAbility`** asset (§4.2). |

**Suggested paths (implementation):**

- `Assets/Resources/Item/Essence/Telekinesis.asset`
- `Assets/Resources/Item/Ability/Telekinesis_Standard.asset`

### D4.2 — `TelekinesisAbility` : `AbilityAction`

| Field | Value / rule |
|-------|----------------|
| **`abilityName`** | `Telekinesis` |
| **`soulPowerCost`** | **1** |
| **`requiresTarget`** | **true** |
| **`range`** | Designer-authored max **Manhattan** distance from **active member** `GridPosition` to **reticle tile** (v0 default: **7** if unset in spec — implementers may expose on asset). |
| **`splashRadius`** | **0** (single tile) |
| **`isMovementAbility`** | **false** |
| **`cooldownTurns`** | **0** (v0) |
| **`noiseVolume`** | **0** (v0 silent) unless design requests audible pull |

**Script:** `TelekinesisAbility` in `Assets/Scripts/Abilities/` (or `JRogue.Ability` namespace matching `FireballAbility` / `HealAbility`).

---

## 5. Player flow

### F5.1 — Activation

1. Player presses essence ability hotkey (same as existing essence slot **0** / **1** with Shift for second active).
2. Preconditions (existing pipeline):
   - Player turn active (`GameState.PLAYER_TURN`).
   - Active member `TurnManager.CanActorTakeAction`.
   - `EssenceSlotManager.CanAfford(slot, index)` for **1** Soul Power — if false, log **Not enough Soul Power!** and **do not** enter targeting (current behavior).
3. Enter targeting: reticle shown at active member tile (or current reticle policy); log entered targeting mode (existing debug).

### F5.2 — Targeting mode

| Input | Behavior |
|-------|----------|
| **Direction** | `TargetingReticleView.Move(direction)` — same as today. |
| **Cancel** | `ExitTargetingMode`; no SP cost; no turn cost. |
| **Confirm** | Run §6 resolution at `reticleView.Position`. |

**Range (R5.2.1):** If reticle tile is farther than `range` (Manhattan), treat as **invalid target** (§6.3) — same UX as empty tile.

**Line of sight (v0):** **Not required** unless a global targeting rule is added later; document as **open** in §12.

### F5.3 — Turn and Soul Power accounting

| Outcome | Soul Power | Player action (turn) | Targeting UI |
|---------|------------|----------------------|--------------|
| **Valid item, inventory add OK** | −1 | Consumed | Exit targeting |
| **Valid item, over-encumbered → drop on player tile** | −1 | Consumed | Exit targeting |
| **Invalid target** | Unchanged | **Not** consumed | **Stay** in targeting; reticle unchanged |
| **Cancel** | Unchanged | Not consumed | Exit targeting |

Alignment with code: `EssenceSlotManager.TryExecuteInternal` deducts SP only when `Execute` returns true; `ApplyConfirmTarget` calls `OnPlayerActionComplete` only when `TryExecuteAbility` returns true.

---

## 6. Confirm resolution

### F6.1 — Resolve target tile

Let `tile = reticleView.Position`, `user = activeMember`.

### F6.2 — Valid target definition

A confirm is **valid** iff **all** of the following hold:

| # | Rule |
|---|------|
| V1 | Manhattan distance `user.GridPosition` → `tile` ≤ `range`. |
| V2 | Tile has **exactly one** pickable floor item (see V3). Zero items → invalid. **More than one** pickable entry on the tile → **invalid** (v0 — avoids ambiguous pull; manual `,` / `g` menu still handles multi-pile tiles). |
| V3 | That item is a **physical** loot item: normal `ItemInstance` on pile or `WorldItem` with non-null `ItemData`, **not** currency-only or mana-stone-only ledger pickup. |
| V4 | Item is not already owned in inventory (still on floor). |

**Pickable resolution order (implementation):**

1. If `FloorItemPileService` has entries on `tile`, count entries that are **not** currency / mana stone auto-ledger types.
2. Else if legacy `WorldItem`(s) exist on `tile` via `FloorPickupQuery`, count those.
3. If total pickable count ≠ 1 → invalid.

### F6.3 — Invalid target

When confirm fails V1–V4:

- **`ExecuteCore` returns `false`.**
- **`Debug.Log`** (exact intent from design):

  ```text
  [Telekinesis] Invalid target at tile ({x}, {y}, {z}).
  ```

  Use the reticle tile coordinates (`tile.x`, `tile.y`, `tile.z`).

- **Do not** remove floor items, **do not** modify inventory, **do not** deduct Soul Power, **do not** call `OnPlayerActionComplete`.
- **Do not** call `ExitTargetingMode` — player may move reticle and confirm again or cancel.

### F6.4 — Valid target — acquire instance

1. Remove the item from the **source tile** (`tile`):
   - Pile: `FloorItemPileService.RemoveEntry(entryId)` after copying `ItemInstance` reference.
   - `WorldItem`: collect via `CollectInstance()` (or equivalent) and destroy / disable the world object.
2. Set `instance.StorageLocation` appropriately before transfer.

### F6.5 — Valid target — inventory vs player tile

Let `inv = user.GetComponent<InventoryManager>()`.

| Condition | Behavior |
|-----------|----------|
| `inv != null` && `inv.CanCarry(instance)` | `inv.AddItem(instance)` — must succeed; remove from floor already done in F6.4. |
| `inv == null` OR `!inv.CanCarry(instance)` | **Over-encumbrance path:** do **not** add to inventory. Place item on **`user.GridPosition`** via `FloorItemPileService.AddEntry(user.GridPosition, instance)` (create pile entry + world view). Log optional warning, e.g. `[Telekinesis] Too encumbered; dropped {itemName} at feet.` |

In **both** success paths, **`ExecuteCore` returns `true`** (ability succeeded — turn and Soul Power spent).

**Note:** Subspace auto-routing (§6.4 of subspace spec) is **not** required for v0 Telekinesis unless `AddItem` already performs it; if subspace is implemented later, Telekinesis pickup should use the same `AddItem` / `CanCarry` entry points as manual floor pickup.

### F6.6 — Currency and mana stones

Tiles with **only** currency or mana stones are **invalid** for Telekinesis (V3). Those types use party ledgers and silent auto-pickup rules — out of scope for this ability in v0.

---

## 7. Integration points (code)

| Component | Responsibility |
|-----------|----------------|
| **`TelekinesisAbility`** | `CanExecute`, `ExecuteCore(user, targetTile)` — all rules in §6. |
| **`EssenceData` + prefab/slot** | Wire ability on essence asset; equip essence on test actor. |
| **`EssenceSlotManager`** | No change required if ability follows `AbilityAction` contract. |
| **`PlayerCommandProcessor`** | No change required if invalid → `Execute` false; valid → true. Verify reticle stays on false. |
| **`FloorItemPileService`** | `GetEntries`, `RemoveEntry`, `AddEntry` for pull and feet-drop. |
| **`InventoryManager`** | `CanCarry`, `AddItem`. |

**Optional helper (implementation):** `TelekinesisFloorQuery.TryGetSinglePickable(tile, out pickable)` shared with tests — not required in spec if logic lives in ability.

---

## 8. Edge cases

| Case | Expected behavior |
|------|-------------------|
| **Item on same tile as player** | Valid if alone on tile; adds to inventory or feet per encumbrance. |
| **Formation active** | On success, same post-confirm behavior as other targeted abilities (`RecordNewLeaderPosition`, follower rush, `ForceEndPlayerTurn` if applicable). |
| **Encumbered but feet tile already has items** | `AddEntry` stacks another entry on player tile (multi-item pile allowed). |
| **Partial implementation subspace** | Use current `CanCarry` until subspace encumbrance ships. |
| **Null `InventoryManager`** | Treat as cannot carry → feet-drop path; still success (turn spent). |

---

## 9. Functional acceptance (F9.x)

**F9.1 — Activation and cost gate**  
Given Telekinesis equipped and `currentSoulPower >= 1`, activating opens targeting. Given `currentSoulPower < 1`, activation fails with existing insufficient-Soul-Power messaging and targeting does not open.

**F9.2 — Pull into inventory**  
Given a single sword on tile T within range and member can carry it, confirm removes sword from T, adds to active member inventory, deducts 1 Soul Power, consumes action, exits targeting.

**F9.3 — Over-encumbrance feet drop**  
Given member cannot `CanCarry` the item, confirm removes item from T, creates pile entry on member tile, does not add to inventory, deducts 1 Soul Power, consumes action.

**F9.4 — Invalid empty tile**  
Given empty tile within range, confirm logs `[Telekinesis] Invalid target at tile (…)`, Soul Power unchanged, action available, reticle still visible, targeting mode active.

**F9.5 — Invalid multi-item tile**  
Given two pickable items on tile, confirm is invalid per F9.4 (same free retry behavior).

**F9.6 — Cancel**  
Cancel exits targeting without SP or turn cost.

---

## 10. Tests (recommended)

| Test | Type | Notes |
|------|------|-------|
| Valid pull adds item | Edit Mode / Play Mode | Mock pile + `InventoryManager` |
| Encumbered → `AddEntry` at player tile | Edit Mode | `CanCarry` false |
| Invalid → `Execute` false, no SP deduct | Edit Mode | `EssenceSlotManager.TryExecuteAbility` |
| `PlayerCommandProcessor` invalid confirm keeps `InputState.Targeting` | Edit Mode | Extend `PlayerCommandProcessorTest` pattern |
| Range exceeded → invalid | Edit Mode | `range = 3`, reticle 4 away |

---

## 11. Implementation status

| Asset / type | Purpose | Status |
|--------------|---------|--------|
| `Telekinesis.asset` | `EssenceData` | **Not created** |
| `Telekinesis_Standard.asset` | `TelekinesisAbility` | **Not created** |
| `TelekinesisAbility.cs` | Execute logic | **Not created** |

---

## 12. Open / later

| Topic | v0 choice |
|-------|-----------|
| **Line of sight** | Ignored |
| **Multi-item tile** | Invalid (use manual pickup) |
| **Pull priority** when pile + `WorldItem` coexist | Prefer pile entries; if both exist, invalid until pile-only |
| **Range default** | **7** tiles Manhattan unless playtest changes |
| **Noise / VFX** | Silent |
| **Enemy-held items** | Out of scope |

---

## 13. Traceability to product request

| Request | Section |
|---------|---------|
| Soul power cost **1** | §4.2, §5.3 |
| Targeting reticule | §5.1–5.2, §3 |
| Confirm on item → inventory if not over-encumbered | §6.4–6.5 |
| Over-encumbered → item on **player tile**, turn consumed | §6.5, §5.3 |
| Invalid (e.g. blank tile) → **Debug.Log** + coordinates, reticle stays, **no** turn | §6.3, §5.3 |
