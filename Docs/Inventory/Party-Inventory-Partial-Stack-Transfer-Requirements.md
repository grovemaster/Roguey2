# Party inventory — partial stack transfer — Requirements

When the player **gives** a carried item from one party member to another, the game must support transferring **part of a stack** (e.g. give 2 of 5 potions) instead of always moving the **entire** stack. A **quantity dialog** appears only when the selected stack has **quantity &gt; 1**; single-item stacks transfer immediately after the recipient is chosen, with no extra step.

**Depends on:** `PartyInventoryTransferService`, `InventoryGivePickerUI`, `InventoryUI.GiveToAlly`, `InventoryManager` (`TryConsumeCarriedQuantity`, `CanMergeCarriedStacks`, `AddItem`, `CanCarry`), `ItemInstance`, `PartyManager`, `CombatThreatCoordinator`, `EquipmentManager`, `AbilityHotbarUI`, [Ability hotbar §11](../UI/Ability-Hotbar-Requirements.md) (party Give baseline), [Subspace inventory & encumbrance](Subspace-Inventory-And-Encumbrance-Requirements.md) (encumbrance on partial weight).

**Related:** [Holy Land inventory QoL](../RacialSystem/Barbarian-Holy-Land-Requirements.md) (cross-presence Give). [Equipment stack split tests](../../Assets/Tests/UnitTests/Equipment/EquipmentStackSplitTests.cs) (split-instance pattern). Shop cart quantity UX (`ShopNpcMenuUI` `,` / `.` keys) — keyboard precedent only; this dialog is **mouse + keyboard** friendly.

**Explicitly out of scope (v0):** Drag-and-drop quantity; “swap” UI that exchanges items A↔B in one modal; giving from **inside a subspace container** without first **Take** to loose carried (follow existing container rules); partial transfer of **evocables**, **essences**, **currency**, or **mana stones**; remembering last-used quantity per item type; gamepad-specific quantity UI (keyboard + mouse v0).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Partial stacks** — Player can give any amount from **1** to **stack quantity** inclusive. |
| **G2** | **No dialog for singles** — `quantity == 1` keeps today’s two-step flow: **Give → pick recipient → done**. |
| **G3** | **Quantity dialog for stacks** — `quantity &gt; 1` adds a **How many?** step after recipient selection. |
| **G4** | **Dual input** — Quantity is adjustable via **increment / decrement controls** and by **typing a number** in a text field. |
| **G5** | **Safe validation** — Invalid, empty, or out-of-range input **disables** the transfer button; transfer never executes with quantity **≤ 0** or **&gt; available**. |
| **G6** | **Encumbrance-accurate** — `CanCarry` (and future encumbrance calculator) evaluates **only the weight of the quantity being transferred**, not the full source stack. |
| **G7** | **Stack merge on receive** — Recipient merges into an existing carried stack of the same `ItemData` when `CanMergeCarriedStacks` allows (same as `AddItem` / `TryStowCarriedItem` today). |
| **G8** | **Hotbar-safe** — Partial give **does not** clear hotbar bindings on the giver when the source `ItemInstance` remains in inventory; full transfer (all units) keeps today’s stale-slot cleanup. |
| **G9** | **Same transfer rules** — Combat block, equipped-item block, essence block, and party-membership checks from [§11 T1–T8](../UI/Ability-Hotbar-Requirements.md) still apply. |

---

## 2. Current state vs gap

| Area | Today | Gap |
|------|-------|-----|
| **`PartyInventoryTransferService.TryGiveCarriedItem`** | Removes entire `ItemInstance` from giver; adds whole instance to recipient. | No `quantity` parameter; no split. |
| **`InventoryGivePickerUI`** | Modal: “Give to whom?” → immediate transfer callback. | No quantity step. |
| **`InventoryUI.GiveToAlly`** | Opens picker, calls `TryGiveCarriedItem` with full instance. | No branch on `instance.Quantity`. |
| **Stack splitting elsewhere** | `TryConsumeCarriedQuantity`, `TrySplitCarriedForEquip`, `QuestLogic.RemoveMatchingItems` partial removal. | Not wired to party Give. |
| **Shop quantity UX** | `ShopNpcMenuUI` adjusts staging qty with `,` / `.` | Different surface; no reusable quantity modal yet. |

---

## 3. Player flow

### 3.1 — Stack size 1 (unchanged)

```
Inventory → select item (×1) → [ Give ] → “Give to whom?” → pick ally → transfer 1 → refresh UI
```

No quantity dialog. Esc on recipient picker cancels (today’s behavior).

### 3.2 — Stack size &gt; 1 (new)

```
Inventory → select item (×N, N>1) → [ Give ] → “Give to whom?” → pick ally
    → “How many?” quantity dialog → [ Give ] confirm → transfer K → refresh UI
```

Esc on quantity dialog **returns to inventory** without transferring (recipient picker already closed). Esc on recipient picker still cancels before quantity dialog opens.

### 3.3 — When quantity dialog is skipped

| Condition | Dialog |
|-----------|--------|
| `instance.Quantity == 1` | **Skip** |
| `instance.Quantity &gt; 1` | **Show** |
| Item category blocked from Give (essence, etc.) | **N/A** — Give disabled / message before picker |
| Evocable (`ItemCategory.Evocable`) | **Skip** at qty 1 only (evocables are always qty 1) |

---

## 4. Quantity dialog — requirements

### 4.1 — When shown

After the player picks a **valid recipient** in `InventoryGivePickerUI`, if `sourceInstance.Quantity &gt; 1`, open **`InventoryGiveQuantityDialogUI`** (new) before calling the transfer service.

Pass context: `ItemInstance source`, `BaseActor giver`, `BaseActor recipient`, `int maxQuantity` (= `source.Quantity` at open time).

### 4.2 — Content

| Element | Requirement |
|---------|-------------|
| **Title** | `How many?` or `Give how many?` |
| **Item summary** | Item name, icon (if available), and **available count**: e.g. `Healing Potion  ·  you have 5` |
| **Recipient line** | `To: {recipient.DisplayName}` |
| **Quantity control** | **Decrement** button, **numeric text field**, **increment** button (horizontal row) |
| **Range hint** | Subtitle: `1 – {maxQuantity}` |
| **Primary action** | `[ Give ]` — executes transfer with validated quantity |
| **Cancel** | `[ Cancel ]` and **Esc** — close dialog, no transfer |
| **Chrome** | Same family as `InventoryGivePickerUI`: dim overlay, centered panel, `sortingOrder` above inventory |

### 4.3 — Increment / decrement behavior

| Control | Behavior |
|---------|----------|
| **− (decrement)** | Subtract 1 from current value; **floor at 1** (button disabled or no-op at 1). |
| **+ (increment)** | Add 1; **ceiling at `maxQuantity`** (button disabled or no-op at max). |
| **Hold repeat (optional v0)** | Not required v0; single click per change is enough. |

### 4.4 — Give button enablement (locked)

The **`[ Give ]`** button is **interactable only** when the current quantity is a valid integer **K** where **`1 ≤ K ≤ maxQuantity`**.

Recompute on every change: `+` / `−` click, text field edit (`onValueChanged`), and focus loss.

| Current value | `[ Give ]` |
|---------------|------------|
| Empty field | **Disabled** |
| Non-numeric (e.g. `abc`) | **Disabled** |
| `0`, negative, or `≤ 0` | **Disabled** |
| `1` … `maxQuantity` | **Enabled** |
| Greater than `maxQuantity` (e.g. `10` when max is `5`) | **Disabled** |

When disabled, **Enter** does not confirm. Optional inline hint below the field: `Enter a number from 1 to {max}.` (styling only — not a second confirm path).

### 4.5 — Manual text entry

| Rule | Detail |
|------|--------|
| **Field type** | Integer only — no decimals. Player may type a leading `-` briefly; value stays invalid until removed. |
| **On focus** | Select all text so typing replaces the value (standard TMP input behavior). |
| **While editing** | Allow empty or partial input temporarily (e.g. user cleared field to type `3`). **Do not** auto-clamp the displayed text to max while typing — show what they typed; disable `[ Give ]` until valid. |
| **On blur** | **Do not** auto-correct invalid text to `1`. Leave the field as typed; `[ Give ]` stays disabled until the player fixes it or uses `+` / `−`. |
| **On confirm** | Only reachable when `[ Give ]` is enabled — use parsed **K** as transfer quantity. |
| **Max bound** | `maxQuantity` is the stack size **at dialog open**; service re-validates at execute time as a safety net. |

### 4.6 — Keyboard (v0)

| Key | Action |
|-----|--------|
| **Esc** | Cancel |
| **Enter** | Confirm Give **only if** `[ Give ]` is enabled |
| **↑ / ↓** or **+ / −** | Optional convenience: increment / decrement focused quantity (nice-to-have; not blocking v0) |

### 4.7 — Mock — quantity dialog (valid)

Stack of 5 potions; player chose to give to **Bruenor**:

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ ░░  ┌────────────────────────────────────────────────────────────────┐  ░░ │
│ ░░  │  How many?                                                     │  ░░ │
│ ░░  │                                                                │  ░░ │
│ ░░  │  [icon]  Healing Potion          you have 5                    │  ░░ │
│ ░░  │          To: Bruenor                                           │  ░░ │
│ ░░  │                                                                │  ░░ │
│ ░░  │          Give quantity                                         │  ░░ │
│ ░░  │                                                                │  ░░ │
│ ░░  │              [ − ]    ┌─────────┐    [ + ]                     │  ░░ │
│ ░░  │                       │    2    │                              │  ░░ │
│ ░░  │                       └─────────┘                              │  ░░ │
│ ░░  │                       1 – 5                                    │  ░░ │
│ ░░  │                                                                │  ░░ │
│ ░░  │              [ Give ]              [ Cancel ]                  │  ░░ │
│ ░░  └────────────────────────────────────────────────────────────────┘  ░░ │
│ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
└──────────────────────────────────────────────────────────────────────────────┘
```

**Default quantity on open:** `1` (conservative; player raises if they want more). *Open decision D1 — see §10.*

**After success:** Log / feedback e.g. `Gave 2 × Healing Potion to Bruenor.` (include count when `K &gt; 1` or always when from a multi-stack).

### 4.8 — Mock — invalid quantity (`[ Give ]` disabled)

Player typed `10` into a stack of 5:

```
│ ░░  │              [ − ]    ┌─────────┐    [ + ]                     │  ░░ │
│ ░░  │                       │   10    │                              │  ░░ │
│ ░░  │                       └─────────┘                              │  ░░ │
│ ░░  │                       1 – 5                                    │  ░░ │
│ ░░  │                       Enter a number from 1 to 5.             │  ░░ │
│ ░░  │                                                                │  ░░ │
│ ░░  │              [ Give ]              [ Cancel ]                  │  ░░ │
│ ░░  │               ^^^^^^^                                            │  ░░ │
│ ░░  │              greyed / non-interactable                           │  ░░ │
```

Same treatment for `0`, `-1`, or an empty field — `[ Give ]` disabled; `[ Cancel ]` and Esc always work.

### 4.9 — Full flow mock (stack &gt; 1)

```
┌─ INVENTORY ─────────────────────────────────────────────────────────────────┐
│  Selected: Healing Potion ×5  (carried · Aria)                              │
│  [ Use ]  [ Drop ]  [ Give ]  [ Equip ]                                     │
└─────────────────────────────────────────────────────────────────────────────┘
        │
        ▼  [ Give ]
┌─ Give to whom? ─────────────────────────────────────────────────────────────┐
│  [ Bruenor ]  [ Imoen ]  [ Minsc ]                          Esc = cancel    │
└─────────────────────────────────────────────────────────────────────────────┘
        │
        ▼  pick Bruenor  (qty > 1 → quantity dialog)
┌─ How many? ─────────────────────────────────────────────────────────────────┐
│  (mock above)                                                                │
└─────────────────────────────────────────────────────────────────────────────┘
        │
        ▼  [ Give ] with 2
  Aria: Healing Potion ×3  |  Bruenor: Healing Potion ×2 (merged if already had)
```

---

## 5. Service & inventory logic

### 5.1 — API change

Extend transfer entry point (name illustrative):

```csharp
bool TryGiveCarriedItem(
    ItemInstance instance,
    BaseActor from,
    BaseActor to,
    int quantity,
    out string message);
```

| `quantity` | Behavior |
|------------|----------|
| `&lt; 1` | Fail — `Invalid quantity.` |
| `&gt; instance.Quantity` | Fail — `Not enough items in stack.` |
| `== instance.Quantity` | **Full transfer** — same as today (move instance reference; clear hotbar on from). |
| `&lt; instance.Quantity` | **Partial transfer** — see §5.2. |

All §11 validation (combat, party, equipped, essence, carried membership) runs **before** mutating inventories.

### 5.2 — Partial transfer algorithm

1. Build a **prospective** `ItemInstance` for encumbrance check: same `ItemData`, `quantity` units, copy appraisal / marks / inscription from source as appropriate (match `CreateSplitInstance` / equip-split semantics).
2. `toInventory.CanCarry(prospective)` — if false, fail with today’s carry message; **no mutation**.
3. `fromInventory.TryConsumeCarriedQuantity(sourceInstance, quantity)` — if false, fail; **no mutation**.
4. Add to recipient:
   - If `CanMergeCarriedStacks`: merge `quantity` into existing stack, **discard** prospective instance id.
   - Else: add new `ItemInstance` with new id and `quantity` (evocables never reach this branch).
5. **Hotbar:** call `ClearHotbarReferences(from, instance.Id)` **only** when `quantity == instance.Quantity` **before** step 3 (full give). Partial leave source instance in bag — **do not** clear hotbar.
6. Success message includes quantity when `quantity &gt; 1`.

**Atomicity:** If step 4 fails after step 3, roll back by restoring quantity to source (or refuse step 4 paths that can fail after split — prefer pre-check `CanCarry` + merge room so Add cannot fail).

### 5.3 — Item types

| Type | Partial give |
|------|----------------|
| Normal stackables (potions, ammo not equipped, etc.) | **Yes** |
| `ItemCategory.Evocable` | **No** — always qty 1; existing rules |
| `ItemCategory.Essence` | **No** — not transferable |
| Currency / mana stones | **No** — ledger rules |
| Quest items | **Yes** if Give is otherwise allowed |
| Items with charges (if qty 1) | **No partial** — whole instance only |

### 5.4 — Equipped items

Unchanged: **cannot Give** equipped items. Partial stack rules apply only to **carried** stacks.

---

## 6. UI integration

| Component | Change |
|-----------|--------|
| `InventoryUI.GiveToAlly` | After recipient pick: if `row.Instance.Quantity &gt; 1`, open quantity dialog; else call service with `quantity: 1`. |
| `InventoryGivePickerUI` | Unchanged recipient list; callback passes recipient to quantity dialog or direct transfer. |
| `InventoryGiveQuantityDialogUI` | **New** — §4 layout and validation. |
| `PartyInventoryTransferService` | Accept `quantity`; implement §5. |
| `AbilityHotbarUI` | Refresh after successful transfer (unchanged). |

**Give button enablement:** Unchanged — enabled for carried, non-equipped, transferable categories.

---

## 7. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Give **1** potion from a ×1 stack: recipient picker only, no quantity dialog, recipient receives 1. |
| **AC2** | Give from a ×5 stack: after picking recipient, quantity dialog opens with max **5**. |
| **AC3** | Confirming **2** leaves giver with **3** and recipient gains **2** (merged if same `ItemData`). |
| **AC4** | Confirming **5** (all) removes source instance entirely — equivalent to today’s full transfer. |
| **AC5** | `+` / `−` respect bounds 1 and max; typed `10`, `0`, or empty when max is 5 leaves **`[ Give ]` disabled** per §4.4. |
| **AC6** | Transfer blocked in combat with existing message. |
| **AC7** | Partial give leaves hotbar slot bound to source instance **intact** on giver. |
| **AC8** | Full give clears hotbar references on giver for that instance id. |
| **AC9** | Recipient over encumbrance limit for **partial** weight fails with no inventory change. |
| **AC10** | Esc on quantity dialog cancels with no transfer. |

---

## 8. Tests (recommended)

| Test | Assert |
|------|--------|
| `TryGiveCarriedItem_partial_reduces_source` | ×5 → give 2 → source ×3 |
| `TryGiveCarriedItem_partial_merges_recipient` | Recipient already has ×3 same potion → give 2 → recipient ×5 |
| `TryGiveCarriedItem_full_clears_hotbar` | quantity == stack size → hotbar entry cleared |
| `TryGiveCarriedItem_partial_keeps_hotbar` | quantity &lt; stack size → hotbar unchanged |
| `TryGiveCarriedItem_over_quantity_fails` | quantity 6 from ×5 → false |
| `TryGiveCarriedItem_encumbrance_partial` | Recipient can carry 2× weight but not 5× |

---

## 9. Traceability — code touchpoints

| File | Role |
|------|------|
| `PartyInventoryTransferService.cs` | Quantity-aware transfer |
| `InventoryGivePickerUI.cs` | Recipient selection (unchanged UX) |
| `InventoryGiveQuantityDialogUI.cs` | **New** quantity modal |
| `InventoryUI.cs` | Orchestrate picker → quantity → service |
| `InventoryManager.cs` | `TryConsumeCarriedQuantity`, `CanCarry`, merge via `AddItem` |
| `ItemInstance.cs` | `Quantity` setter rules (evocable lock) |

---

## 10. Open decisions

| # | Question | Default recommendation |
|---|----------|------------------------|
| **D1** | Default quantity when dialog opens? | **1** — avoids accidental full-stack gives |
| **D2** | Invalid typed value UX? | **Locked:** disable `[ Give ]`; optional hint text; no silent clamp |
| **D3** | Show weight delta in dialog? | **Nice-to-have** — `+0.4 kg` for selected qty; not required v0 |
| **D4** | Quantity dialog before or after recipient? | **After recipient** (this doc) — keeps “Give to whom?” modal unchanged |
| **D5** | “Give all” shortcut button? | **Optional** — sets field to max; not required v0 |

---

## 11. Doc cross-updates (when implemented)

- [Ability hotbar §11](../UI/Ability-Hotbar-Requirements.md) — add **T9**: partial stack quantity; update §11.3 mock to reference this doc for stacks &gt; 1.
- [Subspace inventory §12](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md) — link partial **party Give** (distinct from future container stack split).

---

*Last updated: partial stack Give quantity dialog spec, mock §4.6–4.7, service API §5.*
