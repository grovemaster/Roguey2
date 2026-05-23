# Auto-pickup confirmation dialog — Requirements

Some floor items **auto-pick up when a party member enters their tile**, but only after the player **confirms** the move. If the active mover is about to step onto a tile that holds one or more such items, the game shows a **blocking confirmation dialog** naming the mover and the item(s). **No** cancels the move and **does not** spend a turn. **Yes** completes the move and auto-picks up every confirm-gated auto-pickup item on that tile that the rules allow.

Any item may opt into this behavior via data on **`ItemData`**. Items without the flag are unaffected by this dialog (they may use manual pickup, silent auto-pickup, or no walk-over pickup per other specs).

**Depends on:** `PlayerCommandProcessor`, `BaseActor.TryMove`, `PartyManager`, `TurnManager`, `FloorItemPileService` (or interim floor item query), `ItemData`, `ItemInstance`, `InventoryManager`, `PartyCurrencyLedger`, grid coordinates (`Vector3Int`).

**Related:** [Floor item piles](Floor-Item-Pile-Requirements.md) (manual `,` / `g` pickup for non-auto items). [Enemy death loot & mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md) (mana stones = **silent** auto-pickup, **no** confirmation — §4). [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md) (modal chrome, Y/N confirm pattern).

---

## 1. Goals

**G1 — Opt-in per item (v0)**  
Designers can mark any item so that **auto-pickup on tile entry requires confirmation** before the move resolves.

**G2 — Move gate**  
Confirmation appears **before** the party member enters the destination tile—not after. Cancel preserves position and **does not** consume a player turn.

**G3 — Multi-item tiles**  
One dialog covers **all** confirm-gated auto-pickup items on the destination tile, listed for the player.

**G4 — Clear attribution**  
Dialog states **which party member** would enter the tile and **which item(s)** triggered confirmation.

**G5 — Confirm = move + batch pickup**  
**Yes** executes the pending move, then attempts auto-pickup for **every** confirm-gated auto-pickup item on that tile that pickup rules allow (encumbrance, currency ledger, etc.).

**G6 — Consistent UX**  
Reuse the project’s existing **dim overlay + confirm bubble** pattern (same family as inventory drop confirm).

---

## 2. Auto-pickup taxonomy (how this fits other specs)

| Floor item policy | On tile entry | Confirmation dialog |
|-------------------|---------------|---------------------|
| **Manual pickup only** (default floor-pile v0) | No auto-pickup | No |
| **Silent auto-pickup** (e.g. mana stones) | Pickup immediately on entry | No |
| **Confirm-gated auto-pickup** (this spec) | Pickup only after **Yes** | **Yes** — this document |

An item is **confirm-gated** only when **both** are true:

1. `autoPickupOnStep == true` (or equivalent — item participates in walk-over auto-pickup), and  
2. `requiresAutoPickupConfirmation == true` (new field — §3.1).

Items with `autoPickupOnStep == false` never appear in this dialog and are picked up only via manual floor pickup (`,` / `g`).

---

## 3. Data model

### 3.1 — `ItemData` fields (new / clarified)

| Field | Type | Default | Notes |
|-------|------|---------|--------|
| `autoPickupOnStep` | bool | `false` | Item is eligible for walk-over auto-pickup when a party member enters the tile. |
| `requiresAutoPickupConfirmation` | bool | `false` | When `autoPickupOnStep` is true, entering the tile requires dialog confirmation first. Ignored if `autoPickupOnStep` is false. |

**Authoring rules:**

- `requiresAutoPickupConfirmation` without `autoPickupOnStep` → invalid; editor warning; treated as manual pickup at runtime.
- **Any category** may set these flags (potions, plot items, cursed gear, treasure, etc.).

**Example content (non-normative):**

| Item | `autoPickupOnStep` | `requiresAutoPickupConfirmation` |
|------|--------------------|----------------------------------|
| Mana stone | `true` | `false` (silent — loot doc) |
| Potion of Experience | `true` | `true` |
| Cursed ring | `true` | `true` |
| Iron sword | `false` | `false` (manual pickup) |

### 3.2 — Floor query API

**`FloorItemPileService`** (or loot query helper) exposes:

```text
GetConfirmGatedAutoPickupEntries(Vector3Int tile) → IReadOnlyList<FloorItemEntry>
```

Returns pile entries on `tile` whose definition has **both** auto-pickup flags set (§3.1). Used by move interception and dialog copy.

Optional companion:

```text
GetSilentAutoPickupEntries(Vector3Int tile) → …
```

For mana stones and other silent auto-pickup (processed **after** move resolves, without dialog — §5.4).

---

## 4. When the dialog appears

### 4.1 — Trigger

Intercept **player-initiated movement** in `PlayerCommandProcessor` (or shared move resolver) **after** walkability / bump checks pass and **before** `TryMove` mutates position:

1. Compute **destination tile** `dest` = active mover’s `GridPosition + direction` (or formation leader destination — §4.3).
2. Query `GetConfirmGatedAutoPickupEntries(dest)`.
3. If the list is **non-empty**, **do not move yet**; open **`AutoPickupConfirmDialogUI`** (§6) with pending move context.
4. If empty, proceed with normal move; after move, run silent auto-pickup pass (§5.4) if applicable.

### 4.2 — When dialog does **not** appear

| Situation | Behavior |
|-----------|----------|
| Destination tile has no confirm-gated auto-pickup items | Normal move; no dialog |
| Item is manual-pickup only | Normal move; item stays on tile |
| Enemy bump / attack instead of move | No dialog (no entry attempt) |
| Move illegal (wall, blocked) | No dialog; move fails as today |
| Silent auto-pickup items only on tile | Normal move; silent pickup after entry |
| Player already standing on tile | N/A — dialog is **entry-only**, not for items spawned underfoot mid-turn (§11) |

### 4.3 — Which party member

- Dialog names the **`BaseActor`** who **would perform the move** — the **active party member** for solo moves, or the **formation leader** when formation movement moves the leader onto `dest`.
- Pickup on **Yes** uses the **same actor** as the picker for encumbrance / bag rules (`InventoryManager` on that member), consistent with [floor pickup](Floor-Item-Pile-Requirements.md) “picking up as: active member”.
- **Future:** follower stepping onto a tile independently — same rule with that follower as mover.

### 4.4 — Formation / multi-cell footprints

- v0: evaluate **destination anchor tile** only (the cell the mover’s anchor would occupy).
- Footprint overlap with confirm items on adjacent cells **does not** trigger dialog unless the **anchor** enters that tile.

---

## 5. Dialog outcomes

### 5.1 — No / Cancel / Esc

| Result | Detail |
|--------|--------|
| Position | Unchanged — mover stays on current tile |
| Turn | **Not consumed** |
| Floor items | Unchanged on destination tile |
| Pending move | Discarded |

### 5.2 — Yes / Confirm

| Step | Detail |
|------|--------|
| 1 | Execute the **pending move** (`TryMove` / formation move) onto `dest` |
| 2 | Run **confirm-gated auto-pickup batch** on `dest` for the mover (§5.3) |
| 3 | Run **silent auto-pickup** on `dest` for any remaining silent auto-pickup entries (§5.4) |
| 4 | Close dialog |
| Turn | **Consumed** — same as a normal successful move that spends the player action |

Failed move after Yes (race / validation edge) should not occur in v0 if pre-validated; if move fails, do not run pickup; log error; **v0:** still consume action if validation re-run fails (document as bug to fix).

### 5.3 — Batch pickup (confirm-gated items)

On **Yes**, for **each** floor entry on `dest` with `autoPickupOnStep && requiresAutoPickupConfirmation`:

1. Attempt pickup via shared **`FloorPickupService.TryAutoPickup(entry, picker)`** (encumbrance, currency → ledger, etc.).
2. **Partial success allowed:** pick up what fits; leave the rest on the tile; show summary (toast or log): e.g. `"Picked up 2; left 1 (too heavy)."`
3. Items that fail pickup **remain on the tile**; move still happened.

Order: pile list order (stable).

### 5.4 — Silent auto-pickup (same tile, same entry)

After move + §5.3, process entries with `autoPickupOnStep && !requiresAutoPickupConfirmation` (e.g. mana stones) **without** a second dialog.

If a tile has **both** confirm-gated and silent items:

- Dialog lists **only** confirm-gated items.
- **Yes** picks up confirm-gated (§5.3) **then** silent (§5.4) in one entry resolution.

### 5.5 — Mixed with manual-only items

Items with no auto-pickup remain on the tile after **Yes**; player uses manual floor pickup (`,` / `g`) later.

---

## 6. Dialog UI specification

### 6.1 — Presentation

| Property | Value |
|----------|--------|
| Style | Modal overlay (dim gameplay) + centered bubble — match inventory destructive confirm (`InventoryUI` modal from §6.2) |
| Blocks input | **Yes** — movement, abilities, inventory blocked until resolved |
| Title | `Enter tile?` or `Pick up and move?` (implementer picks one; consistent across game) |

### 6.2 — Required copy elements

| Element | Content |
|---------|---------|
| **Mover** | Display name of party member about to enter (e.g. `Bruenor`) |
| **Destination** | Tile coordinates `(x, y)` optional but recommended |
| **Item list** | One row per confirm-gated entry: icon + name + stack qty (`×N`) |
| **Prompt** | Short explanation that **Yes** moves onto the tile and auto-picks up listed items |
| **Actions** | **Yes** / **No** with keyboard hints |

**Footer bindings (v0):**

| Key | Action |
|-----|--------|
| **Y** / **Enter** | Yes |
| **N** / **Esc** | No |

Gamepad: **South** = Yes, **East** = No (or project standard).

### 6.3 — Multi-item list rules

| Case | Display |
|------|---------|
| 1 confirm item | Single line under mover |
| 2+ confirm items | Bulleted or stacked list; scroll if > ~6 rows |
| Same `ItemData` twice | Two lines (or `×2` if merged for display — **v0: separate lines** per pile entry) |
| Unidentified items | **Future** — v0 show `ItemData.itemName` |

### 6.4 — Authoritative UI mock

Single-item example:

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░┌────────────────────────────────────────────┐░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░│  Enter tile and pick up?                     │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░├────────────────────────────────────────────┤░░░░░░░░░░░░░░ │
│ ░░░░░app░░░░░░░│                                              │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░│  ● Bruenor  would move to  (4, 2)           │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░│                                              │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░│  The following will be picked up:           │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░│    [icon]  Potion of Experience  ×1         │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░│                                              │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░│  Move onto this tile and take these items?  │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░│                                              │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░│     [  Yes (Y)  ]     [  No (N)  ]          │░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░└────────────────────────────────────────────┘░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
└──────────────────────────────────────────────────────────────────────────────┘
  ░ = semi-transparent dim (gameplay visible but inactive)
```

Multi-item example (same dialog pattern):

```
┌────────────────────────────────────────────┐
│  Enter tile and pick up?                   │
├────────────────────────────────────────────┤
│                                            │
│  ● Aria  would move to  (12, 7)            │
│                                            │
│  The following will be picked up:          │
│    [icon]  Potion of Experience  ×1        │
│    [icon]  Cursed Iron Ring      ×1        │
│    [icon]  Plot Artifact         ×1        │
│                                            │
│  Move onto this tile and take these items? │
│                                            │
│     [  Yes (Y)  ]     [  No (N)  ]         │
└────────────────────────────────────────────┘
```

### 6.5 — Optional enrichments (future)

| Feature | Notes |
|---------|--------|
| Encumbrance preview | “+0.9 kg” if all picked up |
| Per-row “too heavy” warning | Gray out rows picker cannot carry |
| **`?` examine** | Focus row → inspect pane (reuse floor pickup examine) |
| “Always pick up without asking” | Per-item or global ignore flag |

---

## 7. Input & wiring

### 7.1 — Move pipeline hook

```text
PlayerCommandProcessor.TryMove(direction)
  → if dest has confirm-gated auto-pickup entries
       → AutoPickupConfirmDialogUI.Show(pendingMove, entries)
       → return (wait for UI)
  → else TryMove as today
       → OnEnteredTile(dest) → silent auto-pickup pass
```

Dialog callbacks:

- `OnConfirm` → resume pending move + §5.2–5.4 + `TurnManager.OnPlayerActionComplete` if applicable  
- `OnCancel` → clear pending move, no turn  

While dialog open: set **`GameplayInputBlockers.AutoPickupConfirm`** (or reuse `InventoryUI.BlocksGameplay` pattern).

### 7.2 — No new Input System action required (v0)

Dialog consumes **Y / N / Esc / Enter** locally; does not register a new `GameControls` action unless needed for gamepad focus.

---

## 8. Physical deliverables

| Deliverable | Path / notes |
|-------------|----------------|
| `ItemData` fields | `autoPickupOnStep`, `requiresAutoPickupConfirmation` |
| `AutoPickupConfirmDialogUI` | `Assets/Scripts/UI/Floor/` or `Assets/Scripts/UI/Gameplay/` |
| Prefab (optional) | `Assets/Prefabs/UI/AutoPickupConfirmDialog.prefab` |
| `FloorPickupService.TryAutoPickup` | Shared with silent auto-pickup + future pile menu |
| Sample content | At least one `ItemData` with both flags `true` on a floor tile in `SampleScene` for QA |
| Tests | Unit tests for query + cancel/no-turn + confirm/move/pickup |

---

## 9. Acceptance criteria

### Dialog trigger

- Given a tile with **one** confirm-gated auto-pickup item, when the active member attempts to **enter** that tile, then the confirmation dialog opens **before** position changes.
- Given a tile with **three** confirm-gated items, when the player attempts to enter, then the dialog lists **all three** by name (and qty).
- Given a tile with only manual-pickup items, when the player attempts to enter, then **no** dialog appears and items remain after move.

### Cancel

- Given the dialog is open, when the player chooses **No** or **Esc**, then the mover does **not** change tile and **no** turn is consumed.
- Given **No**, when the player moves again, then the dialog may appear again (not suppressed).

### Confirm

- Given the dialog is open, when the player chooses **Yes**, then the mover enters the tile, confirm-gated items are auto-picked up per rules, and the turn is consumed as for a normal move.
- Given two confirm-gated items and picker can carry only one, when **Yes**, then one is picked up, one remains, move still completes.

### Mover attribution

- Given **Bruenor** is the active member, when the dialog opens, then body text includes **Bruenor** (display name).

### Silent auto-pickup coexistence

- Given a tile with one confirm-gated potion and one silent mana stone, when the dialog opens, then **only the potion** is listed; when **Yes**, then both are picked up (if rules allow).
- Given only a mana stone on the tile, when the player enters, then **no** dialog and silent pickup runs.

### Regression

- Given inventory drop confirm is open, when auto-pickup confirm would trigger, then only one modal at a time (**v0:** block move before either opens; priority = move confirm first).

---

## 10. Future (out of v0)

| Feature | Notes |
|---------|--------|
| “Don’t ask again this run” | Per `ItemData` or category |
| Examine pane in dialog | §6.5 |
| Items landing underfoot mid-combat | Pickup prompt without move |
| Multi-tile footprint entering multiple cells with items | Per-cell or merged dialog |

---

## 11. Open decisions

| # | Question | v0 default |
|---|----------|------------|
| 1 | Confirm + silent on same tile — one turn or two pickup phases? | **One move, one turn**, two pickup phases (§5.2) |
| 2 | Failed encumbrance on all items — still consume turn? | **Yes** (move happened) |
| 3 | Formation followers stepping on confirm tile without leader | **Future** — leader-only v0 |

---

*Document version: v0 — confirmation gate before enter-tile auto-pickup.*
