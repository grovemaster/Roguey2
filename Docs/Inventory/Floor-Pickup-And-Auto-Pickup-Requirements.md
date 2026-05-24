# Floor pickup & per-item auto-pickup — Requirements

Players interact with items on the ground in two ways: **automatic pickup when entering a tile** (opt-in per item) and **manual pickup** via a dedicated input key (with a multi-select menu when several items share a tile). Designers control auto-pickup per `ItemData` asset and may change flags at any time without code changes.

This document **consolidates and supersedes** conflicting notes in [Floor item piles](Floor-Item-Pile-Requirements.md) §1 G1 / §8 #3 (“no autopickup v0”) and the **Potion of Experience** example in [Auto-pickup confirmation](Auto-Pickup-Confirmation-Requirements.md) §3.1 (which listed the potion as confirm-gated auto-pickup). **Authoritative examples for this feature set** are in §4 below.

**Depends on:** `ItemData`, `ItemInstance`, `FloorItemPileService`, `FloorItemEntry`, `FloorPickupService`, `InventoryManager`, `PartyManager`, `TurnManager`, `InputHandler`, `PlayerCommandProcessor`, `GameControls.inputactions`, `GridManager` / `Vector3Int` tiles.

**Related:** [Floor item piles](Floor-Item-Pile-Requirements.md) (manual menu UI mock §5.8–5.10, input bindings §4). [Auto-pickup confirmation](Auto-Pickup-Confirmation-Requirements.md) (move gate dialog — **implemented**). [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md) (fonts, inspect pane). [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) (potions must be in bag before use). [Enemy death loot & mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md) (mana stones = silent auto-pickup).

---

## 1. Goals

**G1 — Per-item auto-pickup policy (data-driven)**  
Each item declares whether it auto-picks up on tile entry, and if so whether that requires a confirmation dialog before the move. New items and flag changes are **authoring-only** (`ItemData` in Unity).

**G2 — Manual pickup for everything else**  
Items that do not auto-pick up on entry (e.g. Potion of Experience) remain on the tile until the player uses a **manual pickup** action from their current tile.

**G3 — Manual pickup key**  
A new gameplay input attempts to pick up items on the **active party member’s tile**. Default bindings: **`,`** (comma) and **`g`** (DCSS parity), same as [Floor item piles](Floor-Item-Pile-Requirements.md) §4.

**G4 — Multi-item menu**  
If the tile has **more than one** pickable floor entry, open a **Floor Pickup Menu** where the player selects **any subset** (including all or none) before confirming. UI layout and mocks are in §7 and [Floor item piles](Floor-Item-Pile-Requirements.md) §5.8–5.10 — **implemented** in `FloorPickupMenuUI`.

**G5 — One action per manual batch**  
A successful manual pickup that moves ≥1 item into inventory or currency ledger consumes **one** player action. Closing the menu with **Esc** / **Cancel** does **not** consume a turn (per floor-pile spec §6).

**G6 — Coexistence**  
Auto-pickup on entry and manual pickup use the same floor pile / `FloorPickupService` transfer rules (encumbrance, ledgers). An item is never picked up twice.

---

## 2. Pickup policy taxonomy

Every floor item is in exactly one of **four** policies, determined only by `ItemData` flags (§3).

| Policy | `autoPickupOnStep` | `requiresAutoPickupConfirmation` | On party member **entering** tile | Manual **`,` / `g`** |
|--------|--------------------|------------------------------------|-----------------------------------|----------------------|
| **Manual only** | `false` | `false` (ignored) | No pickup | Yes — only way to take item |
| **Silent auto-pickup** | `true` | `false` | Pickup immediately after move resolves | Yes (redundant if still on tile) |
| **Confirm-gated auto-pickup** | `true` | `true` | Move blocked until [Auto-pickup confirmation](Auto-Pickup-Confirmation-Requirements.md) **Yes**; then batch pickup | Yes (if left on tile, e.g. too heavy) |
| **Invalid authoring** | `false` | `true` | Treated as **manual only**; editor warning | Yes |

**Derived helpers (already in code):**

- `ParticipatesInSilentAutoPickupOnStep` → silent row  
- `RequiresConfirmBeforeAutoPickupOnStep` → confirm-gated row  

**Manual pickup query (to implement):** entries on a tile whose definition has `autoPickupOnStep == false` **or** any entry still on the tile after failed auto-pickup. For the menu, use **all entries on the active member’s tile** that are not yet removed (practical v0: `FloorItemPileService.GetEntries(tile)`).

---

## 3. Data model — `ItemData` (authoring)

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `autoPickupOnStep` | `bool` | `false` | If true, item is considered when a party member **enters** the tile (silent or confirm-gated). |
| `requiresAutoPickupConfirmation` | `bool` | `false` | If true **and** `autoPickupOnStep`, show move confirmation before enter + pickup. |

**Rules:**

- Designers may toggle flags on existing assets at any time; no migration script required.
- `requiresAutoPickupConfirmation` without `autoPickupOnStep` → `OnValidate` warning; runtime = manual only.
- Category-agnostic: weapons, potions, currency, plot items all use the same fields.

**Floor storage:** `FloorItemPileService` keyed by `Vector3Int`; each entry is a `FloorItemEntry` (`entryId`, `ItemInstance`). Legacy scene `WorldItem` objects should migrate to pile entries over time; until then, `FloorPickupQuery` may still find `WorldItem` for auto-pickup only.

---

## 4. Authoritative content examples

These examples match **product intent** for this document. Update assets to match during implementation / QA.

| Item | `autoPickupOnStep` | `requiresAutoPickupConfirmation` | Player experience |
|------|--------------------|----------------------------------|-------------------|
| **Mana stone** | `true` | `false` | Silent auto-pickup on enter (see mana stone loot doc). |
| **Giant's Blade** (`Giants_Blade`) | `true` | `true` | Entering tile prompts “move and pick up?”; **Yes** moves + picks up. |
| **Potion of Experience** | `false` | `false` | **No** auto-pickup on enter; player must press **`,` / `g`** (or HUD **Pick up**). |

**Note:** `PotionOfExperience.asset` may not yet serialize floor-pickup fields; default is manual-only. **Giant's Blade** is already authored confirm-gated in `Assets/Resources/Item/Weapon/Giants_Blade.asset`.

---

## 5. Auto-pickup on tile entry

### 5.1 — Behavior summary

| Policy | When | Implementation status |
|--------|------|------------------------|
| Silent | After move onto tile (or `GridMover.Moved`) | **Done** — `ManaStoneAutoPickupService` on all party `GridMover.Moved`; `PlayerCommandProcessor` silent pass after move; confirm path in `CompleteConfirmedMove` |
| Confirm-gated | Before move; dialog lists items | **Done** — `AutoPickupMoveGate`, `AutoPickupConfirmDialogUI` |
| Manual-only | Never on enter | **By default** when flags false |

### 5.2 — Implementation notes

| Topic | Status |
|-------|--------|
| Silent pickup on move | `ManaStoneAutoPickupService` + move completion in `PlayerCommandProcessor` |
| `InventoryCollector` trigger | Gated by `autoPickupOnStep`; manual pickup uses `manualPickup: true` |
| Mixed tile | Confirm dialog lists confirm-gated only; **Yes** runs confirm then silent batch |
| Drop from inventory | `InventoryUI` drop places `FloorItemEntry` on actor tile via `FloorItemPileService` |

Full acceptance criteria: [Auto-pickup confirmation](Auto-Pickup-Confirmation-Requirements.md) §9.

---

## 6. Manual pickup — input & command flow

### 6.1 — Input System

Add to `Assets/Controls/GameControls.inputactions` map **Player**:

| Field | Value |
|-------|--------|
| **Action name** | `PickupFloorItems` |
| **Type** | `Button` |
| **Display name** | Pick up items |

**Default bindings:**

| Device | Binding |
|--------|---------|
| Keyboard | **`,`** (comma) |
| Keyboard | **`g`** (secondary, same action) |
| Gamepad | Button West / project standard |

**Wiring (required):**

- `InputHandler` subscribes to `PickupFloorItems.performed` (same pattern as `ToggleInventory`).
- Block when `InventoryUI.BlocksGameplay`, `AutoPickupConfirmDialogUI.BlocksGameplay`, or targeting (unless pickup menu is the active overlay).
- Route to `PlayerCommandProcessor` via new `PlayerCommandKind.PickupFloorItems`.

**Status:** **Implemented** — `PickupFloorItems` on `GameControls.inputactions` (`,` and `g`); wired in `InputHandler` and `PlayerCommandProcessor`.

### 6.2 — HUD button

| Field | Value |
|-------|--------|
| **Label** | `Pick up` or icon + tooltip “Pick up items (,)” |
| **Location** | Gameplay HUD near wait / formation / inventory |
| **Enabled when** | Player turn, active member can act, tile has ≥1 floor entry, not blocked by UI |

Invokes the **same** handler as `PickupFloorItems`. **Status:** **Implemented** (`FloorPickupHudButton`).

### 6.3 — When manual pickup is attempted

1. Resolve **active party member** (`PartyManager.GetActiveMember()`).
2. `tile = activeMember.GridPosition`.
3. `entries = FloorItemPileService.GetEntries(tile)` (+ legacy `WorldItem` on tile if still used — **v0 prefer pile only**).
4. If **empty** → feedback (“Nothing to pick up.”), **no** turn.
5. If **one** entry → **fast path:** attempt full pickup of that entry; **one** turn if pickup attempted (per [Floor item piles](Floor-Item-Pile-Requirements.md) §6).
6. If **count ≥ 2** (threshold: `pickupMenuThreshold = 1`, i.e. menu when 2+ entries) → open **Floor Pickup Menu** (§7).
7. On confirm → transfer selected entries via `FloorPickupService` / `InventoryManager.AddItem`; remove from pile; consume player action if ≥1 success.

**Important:** Manual pickup collects **all** pile entries the player selects, regardless of auto-pickup flags — including manual-only potions and leftover confirm-gated items.

---

## 7. Floor Pickup Menu — UI spec & mock

### 7.1 — Authoritative full-modal mock

Implement **`FloorPickupMenuUI`** to match this layout (also summarized in [Floor item piles](Floor-Item-Pile-Requirements.md) §5.10). Colors and fonts follow [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md) (dark panel, inventory inspect pane).

```
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│  PICK UP — Items at your feet (12, 7)                                                [×] │
├──────────────────────────────────────────────────────────────────────────────────────────┤
│  Picking up as:  ● Bruenor          Encumbrance:  142 / 180                              │
├────────────────────────────────────────────┬─────────────────────────────────────────────┤
│  PICKUP LIST (~50%)                        │  EXAMINE (~50%)                             │
│  ┌────┬────────────────────┬────┬───────┐  │  ┌─────────────────────────────────────┐  │
│  │ ☐  │ Rusty Sword        │ ×1 │ 3.2kg │  │  │ [96px]  POTION OF EXPERIENCE        │  │
│  ├────┼────────────────────┼────┼───────┤  │  │         Potion · Consumable         │  │
│  │ ☑  │ Potion of Experience│×1 │ 0.4kg │◀─│  ├─────────────────────────────────────┤  │
│  ├────┼────────────────────┼────┼───────┤  │  │ Value (stack)     ?  (unappraised)  │  │
│  │ ☑  │ Gold               │×25 │   —   │  │  │ Weight (stack)    0.4 kg            │  │
│  ├────┼────────────────────┼────┼───────┤  │  │ Location          On ground · (12,7)│  │
│  │ ☐  │ Giant's Blade      │ ×1 │ 20kg  │  │  ├─────────────────────────────────────┤  │
│  │    │ (too heavy)        │    │       │  │  │ ── Active (on use) ──               │  │
│  └────┴────────────────────┴────┴───────┘  │  │ • Grant 50 XP to party (all)        │  │
│  2 selected · +0.4 kg (+ currency)           │  ├─────────────────────────────────────┤  │
│                                              │  │ Compare vs equipped: —              │  │
│                                              │  │ ⚠ Too heavy for Bruenor (162/180)   │  │
│                                              │  └─────────────────────────────────────┘  │
├────────────────────────────────────────────┴─────────────────────────────────────────────┤
│  [ Take All * ]  [ Take Selected Enter ]  [ Select All A ]  [ Cancel Esc ]               │
├──────────────────────────────────────────────────────────────────────────────────────────┤
│  Controls                                                                                │
│  ↑ ↓  or  j / k — move focus (updates examine pane)                                      │
│  Space — toggle check on focused row                                                     │
│  Enter — pick up checked items (Take Selected)                                           │
│  * — pick up all carryable items on this tile (Take All)                                 │
│  A — check all carryable rows (Select All); does not pick up until Enter or *            │
│  Esc — close menu without spending a turn                                                │
│  Browsing and examining do not consume a turn until you Take All or Take Selected        │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

**List column (left) — row spec**

| Column | Width / align | Content |
|--------|---------------|---------|
| Toggle | 32px | `☐` / `☑`; **Space** toggles focused or clicked row |
| Icon | 28px | `ItemData.icon` or placeholder |
| Name | flex | `itemName`; suffix `(too heavy)` in muted text when `!CanCarry` |
| Qty | 48px | `×{quantity}` if &gt; 1, else `×1` |
| Weight | 64px | `{stack} kg` or `—` for currency |

**Summary line (below list):** `{n} selected · +{kg} kg` (carryable selected only).

**Examine pane (right):** `InventoryInspectPaneView` + `InventoryDetailFormatter.FormatInspectBody` / `FormatCompareEquippedSameSlot` for **active picker**; location line must read `On ground · (x, y)`.

**Footer buttons**

| Button | Shortcut | Action |
|--------|----------|--------|
| **Take All** | **`*`** | Pick up **every carryable** row on the tile in one action (no need to check rows first) |
| Take Selected | **Enter** | Pick up **checked** carryable rows in list order |
| Select All | **A** | Check all carryable rows only; does not pick up until Enter or `*` |
| Cancel | **Esc** | Close modal; **no** turn |

**Controls hint (required):** A dedicated multi-line footer below the buttons (see mock) lists every binding in plain language. Muted text (~12–13px); always visible while the menu is open.

**Title bar `[×]`:** same as Cancel.

### 7.2 — List-only column mock (reference)

When implementing row layout without the full modal frame:

```
│  ┌────┬──────────────────────────────┬──────┬──────────┐  │
│  │ ☐  │ [icon]  Rusty Sword          │  ×1  │   3.2 kg │  │
│  ├────┼──────────────────────────────┼──────┼──────────┤  │
│  │ ☑  │ [icon]  Potion of Experience │  ×1  │   0.4 kg │  │  ← focused row
│  ├────┼──────────────────────────────┼──────┼──────────┤  │
│  │ ☑  │ [icon]  Gold                 │ ×25  │      —   │  │
│  ├────┼──────────────────────────────┼──────┼──────────┤  │
│  │ ☐  │ [icon]  Giant's Blade (heavy) │  ×1  │  20.0 kg │  │
│  └────┴──────────────────────────────┴──────┴──────────┘  │
```

### 7.3 — Gameplay HUD mock

```
┌───────────────────────────── Gameplay HUD (corner) ─────────────────────────────┐
│  [ Wait ]   [ Formation ]   [ Pick up , ]   [ Inventory i ]                     │
└─────────────────────────────────────────────────────────────────────────────────┘
```

`Pick up` invokes the same handler as **`PickupFloorItems`** (`,` / `g`). Disabled when pile empty or not player turn.

### 7.4 — Key UX rules

- **Esc** / **Cancel** / **`[×]`** → close menu, **no** turn.
- **Enter** → Take Selected; multi-select any carryable rows.
- **`*`** → Take All carryable items on the tile (same as **Take All** button).
- Row **focus** updates examine pane (**↑**/**↓**, **j**/**k**, or click row).
- Rows that fail `CanCarry` stay visible; toggle disabled or ignored on confirm.
- **Partial success:** pick up what fits; leave rest on tile; log/toast summary.
- **Fast path** (one item on tile): no modal; immediate pickup attempt; **one** turn on attempt.

### 7.5 — Implementation status

| Component | Status |
|-----------|--------|
| `FloorPickupMenuUI` | **Implemented** — `Assets/Scripts/UI/Gameplay/FloorPickupMenuUI.cs` (§7.1) |
| `FloorPickupHudButton` | **Implemented** — `Assets/Scripts/UI/Gameplay/FloorPickupHudButton.cs` |
| `FloorPickupCoordinator` | **Implemented** — fast path + menu orchestration |
| `PickupFloorItems` input + `PlayerCommandKind.PickupFloorItems` | **Implemented** — `GameControls.inputactions`, `InputHandler`, `PlayerCommandProcessor` |
| `FloorItemPileService` | **Implemented** |
| `FloorPickupService.TryManualPickup` | **Implemented** |
| Menu threshold / fast path | **Implemented** — threshold = 1 (menu when 2+ entries) |

When implementing, create `FloorPickupMenuUI` per §5.10 in the floor-pile doc; set `FloorPickupMenuUI.BlocksGameplay` analogous to `InventoryUI` / `AutoPickupConfirmDialogUI`.

### 7.6 — Optional: examine-only entry (recommended)

| Action | Binding | Behavior |
|--------|---------|----------|
| `ExamineFloorPile` | **`?`** when tile non-empty | Open same modal as pickup, focus first row, no take until confirm |

Defer if needed; not required for first manual-pickup vertical slice.

---

## 8. Turn & action economy

| Event | Consumes player action? |
|-------|-------------------------|
| Manual pickup fast path (1 item), pickup attempted | **Yes** (v0 default) |
| Manual pickup menu → Take Selected / Take All with ≥1 success | **Yes** |
| Manual pickup menu → Esc / Cancel | **No** |
| Empty tile / mis-press manual pickup | **No** |
| Confirm-gated auto **Yes** (move + pickup) | **Yes** (same as move) |
| Confirm-gated auto **No** / Esc | **No** |
| Silent auto-pickup on enter | **No** extra action beyond the move that entered the tile |

Align with `TurnManager.OnPlayerActionComplete` for the active member.

---

## 9. Party & inventory rules

| Rule | Detail |
|------|--------|
| Receiver | **Active party member** only |
| Encumbrance | `InventoryManager.CanCarry` per entry |
| Currency / mana stones | Existing ledger paths in `FloorPickupService` |
| Use after pickup | Potions must be **Carried** before use (party XP doc) |
| Drop from inventory → floor | `InventoryUI` drop adds `FloorItemEntry` on actor tile |

---

## 10. Implementation status summary

| Area | Status | Notes |
|------|--------|-------|
| `ItemData.autoPickupOnStep` / `requiresAutoPickupConfirmation` | **Done** | `Assets/Data/Item/ItemData.cs` |
| `FloorItemPileService` | **Done** | Pile map + query helpers |
| `FloorPickupService` | **Done** | Silent + confirm-gated auto paths |
| `AutoPickupConfirmDialogUI` | **Done** | Move gate before enter |
| `AutoPickupMoveGate` + `PlayerCommandProcessor` hook | **Done** | |
| `ManaStoneAutoPickupService` | **Done** | Silent pickup on `GridMover.Moved` |
| Silent pickup on **all** moves | **Done** | `GridMover.Moved` + move completion paths |
| `PickupFloorItems` / `InputHandler` | **Done** | |
| `FloorPickupMenuUI` | **Done** | §7.1 mock |
| HUD **Pick up** button | **Done** | `FloorPickupHudButton` |
| Silent pickup on all moves | **Done** | Party `GridMover.Moved` + player move completion |
| Inventory drop → floor pile | **Done** | `InventoryUI` → `FloorItemPileService.AddEntry` |
| `InventoryCollector` / `WorldItem` | **Legacy** | Auto via flags; manual via `TryManualPickup(WorldItem)` |
| Unit tests | **Done** | `FloorPickupManualTests`, `AutoPickupConfirmationTests` |

---

## 11. Physical deliverables

| Deliverable | Path / notes |
|-------------|----------------|
| `GameControls.inputactions` | Add `PickupFloorItems` + bindings |
| `PlayerCommand.cs` / `PlayerCommandProcessor` | `PickupFloorItems` kind + apply |
| `InputHandler.cs` | Subscribe + gameplay block checks |
| `FloorPickupMenuUI.cs` + prefab | Per floor-pile doc §5.10 |
| `FloorPickupHudButton` | Gameplay HUD |
| `FloorItemPileService` | Optional `GetManualPickupEntries` if filtering needed |
| `FloorPickupService` | `TryManualPickup(entry, picker)` or reuse `TryAutoPickup` with shared rules |
| Content | Align `PotionOfExperience` (manual), `Giants_Blade` (confirm), mana stone assets (silent) |
| Tests | Policy query, menu threshold, Esc no-turn, mixed tile auto + manual |
| Docs | Update floor-pile doc §1 G1 / §8 #3 to point here for auto-pickup policy |

---

## 12. Acceptance criteria

### Per-item policy

- Given **mana stone** on a tile, when a party member enters, then it is picked up **without** a dialog (silent).
- Given **Giant's Blade** on a tile, when the active member attempts to **enter** that tile, then the auto-pickup confirmation dialog appears **before** the move.
- Given **Potion of Experience** on a tile, when a party member **enters**, then the potion **remains** on the tile (no auto-pickup).
- Given a designer sets `autoPickupOnStep` from false → true on an item, when a player enters a tile with that item, then behavior updates without code deploy.

### Manual pickup

- Given **one** manual-only item underfoot, when the player presses **`,`**, then the item is picked up (or failure feedback) and **one** turn is consumed if pickup was attempted.
- Given **three** items underfoot, when the player presses **`,`**, then the **Floor Pickup Menu** opens with **three** rows (per §5.10 mock).
- Given the menu is open, when the player checks two rows and presses **Take Selected**, then only those two move to inventory; **one** turn consumed.
- Given the menu is open, when the player presses **Esc**, then the menu closes and **no** turn is consumed.
- Given HUD **Pick up** with items underfoot, then behavior matches **`,`**.

### Regression

- Given confirm-gated + silent items on the same destination tile, when the player confirms enter, then both types process per auto-pickup doc; manual-only items remain for **`,`**.

---

## 13. Related documents

| Document | Relationship |
|----------|----------------|
| [Floor item piles](Floor-Item-Pile-Requirements.md) | Menu mock §5.8–5.10, input §4, turn rules §6 — **implement manual pickup from this doc + that UI spec** |
| [Auto-pickup confirmation](Auto-Pickup-Confirmation-Requirements.md) | Enter-tile dialog for confirm-gated items — **implemented** |
| [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md) | Visual language for pickup menu |
| [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) | Potion use requires inventory pickup first |

---

*Document version: v1 — per-item auto-pickup + manual pickup key/menu; consolidates floor-pile autopickup conflict and potion authoring example.*
