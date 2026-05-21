# Floor item piles (multi-item per tile) — Requirements

Multiple items may occupy the **same grid tile** on the ground, modeled after **Dungeon Crawl Stone Soup (DCSS)**. The player uses a dedicated **pickup input** to open a **pickup menu** (when needed), select none/some/all piles, and take them in **one party action**. **Autopickup on walk-over is out of v0** (future phase).

**Depends on:** `GridManager` / grid coordinates, `PartyManager`, `InventoryManager`, `ItemInstance`, `ItemData`, `ItemStorageLocation`, `TurnManager`, `InputHandler`, `PlayerCommandProcessor`, `GameControls.inputactions`, encumbrance on `CharacterStats`.

**Related:** [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) (potions must be in inventory before use; pickup-before-consume). [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md) (visual language). [Multi-tile enemies](../Combat/Multi-Tile-Enemy-Requirements.md) (actors and piles share tiles under different rules).

---

## 1. Goals

**G1 — Vertical slice (v0)**  
A tile may hold a **pile** of floor item entries. The active party member can press **Pickup** to take items from **their current tile** via menu (multi-select) or immediate take (single entry). One player action consumes the turn (§6). **`Esc`** closes the pickup menu **without** spending a turn. **No** autopickup when entering a tile.

**G2 — DCSS parity (core loop)**  
Same mental model as DCSS `g` / `,`: one square, one action, optional menu when multiple things are on the ground.

**G3 — Data-first piles**  
Floor items are **tile data**, not N competing `WorldItem` physics objects on one cell.

**G4 — Fair inventory rules**  
Pickup respects **encumbrance**, per-member **carried** bag, and leaves unpickable items on the tile with feedback.

**G5 — Input + UI**  
New **Input System** action, HUD button, **Floor Pickup Menu** with **list + examine pane** (§5.10), DCSS-style item detail (§5.9).

**G6 — Future**  
Autopickup on step, regex filters, partial stack quantity, examine-tile (`;` in DCSS) — documented, not v0.

---

## 2. Reference — DCSS (summary)

| DCSS behavior | This spec (v0) |
|---------------|----------------|
| Many items per floor cell | **`FloorTileItemPile`** per `Vector3Int` |
| `g` or `,` to pick up | **`PickupFloorItems`** input (§4) |
| `pickup_menu_limit` (default 1): 2+ items → menu | **`pickupMenuThreshold = 1`**: 2+ **entries** → menu (§5.2) |
| One turn per square for any pickup batch | **One player action** per confirm (§6) |
| Menu: select all / subset, confirm | **Take All**, toggles, **Take Selected** (§5) |
| Autopickup on move | **Not v0** (§12) |
| Partial stack pickup (`5` + letter) | **Future**; v0 takes **full** quantity per entry |

---

## 3. Data model

### 3.1 — `FloorTileItemPile`

- **Key:** `Vector3Int` grid cell (XY; Z from actor plane).
- **Value:** ordered list of **`FloorItemEntry`**:
  - `ItemInstance instance` (or template + quantity at drop time)
  - Stable **row id** for UI toggles
- **Service:** `FloorItemPileService` (singleton or scene manager) — add, remove, query by tile, subscribe to changes for UI refresh.

### 3.2 — Coexistence on a tile

| On same cell | Allowed? |
|--------------|----------|
| Pile + **walkable** floor | Yes |
| Pile + **party member** | Yes (pickup from underfoot) |
| Pile + **enemy footprint** | Yes (combat tiles); pickup only when rules allow player action on their tile |
| Multiple pile **entries** | Yes |
| Same `ItemData` twice | Two **entries** (stacks not merged on ground in v0 unless same entry id — **v0: separate entries**) |

### 3.3 — Deprecate walk-over auto pickup

- **`InventoryCollector.OnTriggerEnter2D`** must **not** add floor items in v0 (disable or gate off `WorldItem` collision pickup).
- Items reach the pile via **drop**, **death loot**, or **level spawn** APIs — not only `WorldItem` triggers.

### 3.4 — World presentation (visuals)

- **v0:** Optional **single** `FloorPileView` per tile (icon of top entry + `×N` badge) OR debug list; full multi-icon is **future**.
- **No** requirement for one prefab per item on the ground when multiple items share a tile (see [Party XP doc](../Progression/Party-Experience-And-Leveling-Requirements.md) §8.2).

### 3.5 — Drop from inventory

- Dropping places a new **`FloorItemEntry`** on the actor’s tile (merge stacks **future**).

---

## 4. Input — `PickupFloorItems`

### 4.1 — Input System action (required)

Add to **`Assets/Controls/GameControls.inputactions`** map **`Player`**:

| Field | Value |
|-------|--------|
| **Action name** | `PickupFloorItems` |
| **Type** | `Button` |
| **Display name** | Pick up items |

**Default bindings (v0):**

| Device | Binding | Notes |
|--------|---------|--------|
| Keyboard | **`,`** (comma) | DCSS primary |
| Keyboard | **`g`** | DCSS alias (secondary binding on same action) |
| Gamepad | **Button West** / `x` on Xbox layout | Placeholder; adjust in Unity to project standard |

**Wiring:**

- `InputHandler` subscribes to `PickupFloorItems.performed` (same pattern as `ToggleInventory`).
- Respects **`InventoryUI.BlocksGameplay`** and targeting state — pickup does not run while inventory has gameplay blocked unless menu is the active overlay (§5.7).
- Routes to `PlayerCommandProcessor` → new **`PlayerCommandKind.PickupFloorItems`**.

### 4.2 — HUD button (required)

| Field | Value |
|-------|--------|
| **Label** | `Pick up` or icon + tooltip “Pick up items (,)” |
| **Location** | Gameplay HUD cluster near formation / wait controls (exact layout = implementer; must be visible during exploration) |
| **Enabled when** | Player turn, active member can act, pile non-empty on **active member’s** `GridPosition`, not blocked by UI policy |
| **Disabled tooltip** | e.g. “Nothing here”, “Not your turn”, “Too busy” |

Button invokes the **same** code path as the Input System action (no duplicate rules).

### 4.3 — Examine before pickup — `ExamineFloorPile` (recommended v0)

| Field | Value |
|-------|--------|
| **Action name** | `ExamineFloorPile` |
| **Binding** | **`?`** (keyboard) |
| **When** | Active member’s tile has a **non-empty** pile |

Opens the **same modal** as pickup (§5.10) with row focus + inspect pane populated; **does not** take items until **Take Selected / Take All**. Complements **`,`** fast path on single-item tiles (§5.2).

### 4.4 — When pickup is attempted

1. Resolve **active party member** (`PartyManager.GetActiveMember()`).
2. Read **`GridPosition`** → `FloorItemPileService.GetPile(tile)`.
3. If **null or empty** → light feedback (log / toast “Nothing to pick up.”), **no** turn spent.
4. If **one entry** → skip menu (§5.2 fast path) unless optional “always menu” debug flag.
5. If **count > pickupMenuThreshold** (default **1**, i.e. 2+ entries) → open **Floor Pickup Menu** (§5).
6. On confirm → transfer items (§6), close menu, **consume player action**.

---

## 5. Floor Pickup Menu — UI spec & mock

### 5.1 — Presentation

- **Modal overlay** over gameplay (dimmed backdrop 40–60% alpha).
- **Not** the full Inventory screen; reuses inventory fonts/colors and **inspect pane** patterns from [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md).
- **Body layout (v0):** horizontal split — **item list ~50%** | **item examine pane ~50%** (same information architecture as Inventory §3.3). DCSS uses a separate description flow; this project **embeds** examine beside the list so details stay visible while toggling pickup checkboxes.
- Title includes **tile coordinates** or “Items at your feet”.
- Shows **picker:** active party member name + encumbrance **`current / max`** (optional post-pickup preview **future**; v0 show current only).

### 5.2 — When menu appears

| Entries on tile | Behavior |
|-----------------|----------|
| **0** | No menu; no action |
| **1** | **Fast path:** immediate attempt to take that entry (full quantity); on failure show reason; **one action** if any item moved. Optional setting later: always show menu. |
| **≥ 2** | Open menu (DCSS `pickup_menu_limit = 1`) |
| **1** + **examine-only** | Press **`?`** on tile (§5.9) → open menu with one row + inspect pane; **no** pickup until user confirms |

### 5.3 — Menu rows

Each row = one **`FloorItemEntry`**:

| Column | Content |
|--------|---------|
| Toggle | `☐` / `☑` (selected for pickup) |
| Icon | `ItemData.icon` |
| Name | `itemName` |
| Qty | `×{quantity}` if &gt; 1 |
| Weight | per-entry weight |
| Note | “Too heavy” / “Bag full” if `CanCarry` false |

Rows that cannot be carried remain visible but **unchecked and disabled** (or checked off with error on confirm).

### 5.4 — Actions (footer)

| Control | Behavior |
|---------|----------|
| **Take All** | Select every **carryable** row; run confirm |
| **Take Selected** | Confirm with checked rows only |
| **Select All** | Toggle all carryable on |
| **Clear** | Deselect all |
| **Cancel** | Same as **Esc** — close menu; **no** turn consumed |

**Keyboard (in menu) — required v0:**

| Key | Action |
|-----|--------|
| **`Esc`** | **Close pickup menu entirely**; **does not** consume a player turn (§6) |
| `Enter` | Take Selected |
| `A` or `*` | Select All (carryable) |
| `↑`/`↓` or `j`/`k` | Move row **focus** (updates examine pane §5.9) |
| `?` | Toggle **expanded examine** (optional) or focus inspect pane; **does not** pick up |

### 5.5 — Confirm logic

- Iterate selected entries in list order.
- For each: `InventoryManager.AddItem(instance)` on **active member**; on success remove from pile.
- If pile empty → remove tile from pile map; refresh/despawn view.
- **Partial success** allowed: carry what fits; leave rest on tile; summary message “Picked up 2; left 1 (too heavy).”
- **Currency** entries use existing ledger path (§3.6).

### 5.6 — Currency on tile

- Gold/currency entries follow **`PartyCurrencyLedger`** (same as `InventoryManager.AddItem` today); removed from pile on success.

### 5.7 — Interaction with Inventory UI

- Opening **Floor Pickup Menu** sets gameplay block flag so movement does not fire.
- **`Esc`** while pickup menu is open is handled by **`FloorPickupMenuUI`** first (close modal, no turn) — must not fall through to other UI cancel handlers that leave the modal open.
- **`PickupFloorItems`** while full Inventory open: **ignored** or closes inventory first — **v0: ignored** if `InventoryUI.BlocksGameplay`.

---

### 5.8 — UI mock A — pickup list + footer (left column detail)

List-only column (pairs with **§5.10** examine pane on the right):

```
│  ┌────┬──────────────────────────────┬──────┬──────────┐  │
│  │ ☐  │ [icon]  Rusty Sword          │  ×1  │   3.2 kg │  │
│  ├────┼──────────────────────────────┼──────┼──────────┤  │
│  │ ☑  │ [icon]  Potion of Experience │  ×1  │   0.4 kg │  │  ← focused row
│  ├────┼──────────────────────────────┼──────┼──────────┤  │     (highlights)
│  │ ☑  │ [icon]  Gold                 │ ×25  │      —   │  │
│  ├────┼──────────────────────────────┼──────┼──────────┤  │
│  │ ☐  │ [icon]  Giants_Blade (heavy) │  ×1  │  48.0 kg │  │
│  └────┴──────────────────────────────┴──────┴──────────┘  │
│  2 selected · +0.4 kg (+ currency)                         │
```

### 5.9 — Item examine (DCSS + implementation)

#### DCSS behavior (reference)

| Context | How examine works |
|---------|-------------------|
| **Pickup / multidrop menu** | Press **`?`** then the item’s **menu letter** (or focus item) to open a **full item description** — stats, curse status, art props, etc. Does **not** pick up the item and does **not** spend a turn. |
| **Inventory `(i)`** | Select a slot to view description; unidentified items show generic text until identified. |
| **Shops** | Examine before purchase (`!` + letter in shop UI). |
| **Stash search** | `?` + letter on search results to read remote pile contents. |

#### Roguey2 approach (v0 — recommended)

**Reuse existing inspect pipeline** instead of a one-off floor UI:

| Piece | Reuse |
|-------|--------|
| Formatter | `InventoryDetailFormatter.Format` / `FormatInspectBody` |
| View | `InventoryInspectPaneView` (scrollable rich text + hero icon) |
| Row adapter | Map `FloorItemEntry` → `InventoryViewModel.Row` with `StorageLocation = OnGround`, owner = active picker, **tile** in location string |

**Examine pane updates when:**

- User **focuses** a list row (↑/↓, click row), or
- User presses **`?`** while a row is focused (same as inventory **`x`** inspect — pickup menu standardizes on **`?`** for DCSS parity).

**Examine pane content** (top → bottom, same order as [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md) §3.4):

1. Hero — icon, name, category / slot / risk hints  
2. Summary — value (`?` if unappraised), stack weight, **location: `On ground · (12, 7)`**  
3. Damage, stat modifiers, passive / active lists  
4. **Compare vs equipped** — `FormatCompareEquippedSameSlot` for active picker (useful before picking up a weapon)  
5. Inscription / marks (usually empty on fresh drops)  
6. **Pickup hint** — e.g. “Too heavy for Bruenor” when `CanCarry` is false  

**Rules:**

| Rule | v0 |
|------|-----|
| Costs a turn? | **No** — browsing the menu and examine pane is free |
| Close menu without a turn? | **`Esc`** or **Cancel** — closes modal immediately; **no** player action consumed (§6) |
| Unidentified items | **Future** — v0 shows full `ItemData` (identification system later) |
| Fast path (1 item, `,`) | Skips menu; **`?`** still opens menu in **examine mode** with one row |

#### Optional input — `ExamineFloorPile` (v0 recommended)

| Field | Value |
|-------|--------|
| Action | `ExamineFloorPile` in `GameControls` (or `PickupFloorItems` + hold **Shift**) |
| Binding | **`?`** when pile under active member is non-empty |
| Behavior | Open pickup modal with focus on first row; **no** pickup until Take |

Same **`?`** works **inside** the open menu to scroll inspect help text (footer one-liner).

### 5.10 — UI mock B — full modal with examine pane (authoritative layout)

Combines §5.8 list column + §5.9 inspect pane — **this is the implementable target**:

```
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│  PICK UP — Items at your feet (12, 7)                                                [×] │
├──────────────────────────────────────────────────────────────────────────────────────────┤
│  Picking up as:  ● Bruenor          Encumbrance:  142 / 180                              │
├────────────────────────────────────────────┬─────────────────────────────────────────────┤
│  PICKUP LIST (~50%)                        │  EXAMINE (~50%) — §5.9                    │
│  ┌────┬────────────────────┬────┬───────┐  │  ┌─────────────────────────────────────┐  │
│  │ ☐  │ Rusty Sword        │ ×1 │ 3.2kg │  │  │ [96px]  POTION OF EXPERIENCE        │  │
│  ├────┼────────────────────┼────┼───────┤  │  │         Potion · Consumable         │  │
│  │ ☑  │ Potion of Experience│×1 │ 0.4kg │◀─│  ├─────────────────────────────────────┤  │
│  ├────┼────────────────────┼────┼───────┤  │  │ Value (stack)     ?  (unappraised)  │  │
│  │ ☑  │ Gold               │×25 │   —   │  │  │ Weight (stack)    0.4 kg            │  │
│  ├────┼────────────────────┼────┼───────┤  │  │ Location          On ground · (12,7)│  │
│  │ ☐  │ Giants_Blade       │ ×1 │ 48kg  │  │  ├─────────────────────────────────────┤  │
│  │    │ (too heavy)        │    │       │  │  │ ── Active (on use) ──               │  │
│  └────┴────────────────────┴────┴───────┘  │  │ • Grant 50 XP to party (all)        │  │
│  2 selected · +0.4 kg (+ currency)           │  ├─────────────────────────────────────┤  │
│                                              │  │ Compare vs equipped: —              │  │
│                                              │  │ ⚠ Too heavy for Bruenor (148/180)   │  │
│                                              │  └─────────────────────────────────────┘  │
├────────────────────────────────────────────┴─────────────────────────────────────────────┤
│  [ Take All ]  [ Take Selected ]  [ Select All ]  [ Cancel ]                             │
├──────────────────────────────────────────────────────────────────────────────────────────┤
│  ↑↓ focus row (updates examine) ·  ?  help / inspect ·  , / g  take (when confirmed)      │
│  **Esc — close menu, no turn** ·  Examine / browsing does not consume a turn              │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

**HUD button (in gameplay, not modal):**

```
┌───────────────────────────── Gameplay HUD (corner) ─────────────────────────────┐
│  [ Wait ]   [ Formation ]   [ Pick up , ]   [ Inventory i ]                     │
└─────────────────────────────────────────────────────────────────────────────────┘
```

**Alternative (future):** full-screen examine overlay on top of list-only mock (§5.8) — closer to classic DCSS text mode; defer unless split pane feels cramped on small resolutions.

---

## 6. Turn & action economy

- Successful pickup that moves **≥1** item into inventory or currency ledger consumes **one** player action (same as move/attack/wait for active member).
- **`Esc` closes pickup menu** → **no** player action consumed; active member may move/act normally on the same turn (required v0).
- **Cancel** button → same as **`Esc`** (no turn).
- **Empty** pile / mis-press → **no** action.
- **Fast path** single item: **one** action whether or not pickup succeeded (DCSS spends turn on attempt — document: failed full attempt still costs action if any validation ran on pile **future** tuning; **v0:** cost action if at least one pick attempted OR always on fast path confirm — **v0 default: spend action on fast path only when `AddItem` attempted**).

Align with `TurnManager.CanActorTakeAction` for active member.

---

## 7. Party & inventory rules

| Rule | v0 |
|------|-----|
| Who receives items | **Active party member** only |
| Encumbrance | `InventoryManager.CanCarry` per item |
| Give from pile to ally | **No** — use inventory Give later |
| Undead potion ban | Applies **after** pickup, on Use — not at pickup |
| Stacking in bag | Existing `AddItem` / instance rules |

---

## 8. Resolved design decisions

| # | Topic | Decision |
|---|--------|----------|
| 1 | Pile per cell | **Yes** — `FloorTileItemPileService` |
| 2 | Menu + input button | **Yes** — §4, §5 |
| 3 | Autopickup | **No** v0; disable trigger pickup |
| 4 | One action per batch | **Yes** — §6 |
| 5 | Encumbrance + member bag | **Yes** — §7 |
| 6 | `pickupMenuThreshold` | **1** (menu when 2+ entries) |
| 7 | DCSS `,` / `g` | Both bound to **`PickupFloorItems`** |
| 8 | **Esc** closes menu | **Yes** — never consumes a turn (§5.4, §6) |

---

## 9. Physical deliverables (v0)

| Deliverable | Path / notes |
|-------------|----------------|
| `FloorTileItemPileService.cs` | Runtime pile map |
| `FloorPickupMenuUI.cs` + prefab | Modal per §5.10; embed `InventoryInspectPaneView` |
| `InventoryDetailFormatter` | Extend location line for floor tile coords (optional helper) |
| `FloorPickupHudButton` | Wired to same handler |
| `GameControls.inputactions` | `PickupFloorItems` + bindings |
| `InputHandler` + `PlayerCommand` | `PickupFloorItems` kind |
| Disable `InventoryCollector` auto pickup | Or limit to non-item triggers |
| Test scene pile | One tile with 3+ entries |
| Sample spawn API | `FloorItemPileService.AddEntry(tile, instance)` |

**Not required v0:** per-item ground prefabs for every entry in a pile.

---

## 10. Acceptance criteria

- Given **three** items on the active member’s tile, pressing **`,`** opens the pickup menu with **three** rows.
- Given **Take All**, all **carryable** rows move to active member inventory; pile empty or only heavy items remain.
- Given **Take Selected** with one row checked, only that item moves; **one** player action consumed.
- Given pickup menu open, pressing **`Esc`** closes the menu and **does not** consume the active member’s player action; they may still move or act that turn.
- Given **Cancel** button clicked, same behavior as **`Esc`** (no turn).
- Given walking onto a tile with items, **no** automatic pickup occurs.
- Given HUD **Pick up** click with items underfoot, same behavior as **`,`**.
- Given item too heavy, row shows disabled; remains on tile after confirm.
- Given **one** item on tile, **`,`** picks it up without opening menu (fast path).
- Given potion on pile, after pickup it is **Carried**; Use still requires inventory (per XP doc).
- Given pickup menu open, focusing a row updates **examine pane** with that item’s stats (§5.10).
- Given **`?`** on a tile with one item, menu opens with **examine visible**; **no** turn until Take.
- Given pickup menu open with a row focused and examine pane visible, **`Esc`** still **closes the whole menu** (not a separate “back out of examine” step in v0).

---

## 11. Code touchpoints

| Area | Action |
|------|--------|
| `FloorTileItemPileService` | New |
| `FloorPickupMenuUI` | New; **`Esc`** → close without `TurnManager` action complete |
| `GameControls.inputactions` | `PickupFloorItems` |
| `PlayerCommandKind` / `PlayerCommandProcessor` | Pickup command |
| `InputHandler` | Subscribe + HUD hook |
| `InventoryCollector` | Disable item autopickup |
| `WorldItem` | Migrate spawns to pile API; optional view only |
| `TurnManager` | Action completion on pickup |
| Tests | Menu threshold, take all/selected, no autopickup, encumbrance |

---

## 12. Future phases

| Phase | Scope |
|-------|--------|
| **v1** | Autopickup on step; category filters; examine tile overlay |
| **v2** | Partial quantity pickup; regex select (DCSS Ctrl-F); merge stacks on ground |
| **v3** | Party “pick up for ally”; loot all adjacent |

---

## 13. Related documents

- [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md)
- [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md)
- [Multi-tile enemies](../Combat/Multi-Tile-Enemy-Requirements.md)
