# Evocable Items — Requirements (DCSS-inspired)

**Evocable** items are inventory-held gadgets the player **invokes** (v0: **Use** from inventory, like scrolls). Each physical copy tracks its own **current charges** and **maximum charges**. They do **not** merge into quantity stacks when they share the same `ItemData` name, because two fans can hold different charge counts. Some evocables are **removed** when charges hit zero; others **remain** and **recharge** over time.

**Depends on:** `ItemCategory.Evocable`, `ItemInstance` (per-instance identity), `InventoryViewModel` (one row per instance), `InventoryItemUse`, `InventoryUsability`, `PlayerCommandProcessor` (targeted invoke — same pipeline as [Fireball scroll](Fireball-Scroll-Requirements.md)), [Area ability splash targeting](../Combat/Area-Ability-Splash-Targeting-Requirements.md), `Fireball_Standard`, `SuddenStrength_Standard`, [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md).

**Explicitly out of scope (v0):** Dedicated **V** evoke hotkey outside inventory; DCSS-style **Evocations** skill scaling; merging duplicate evocables into “+enchantment” stacks; XP-based recharge; identification / curse; equip slot for fans; ally-using evocables in combat beyond existing `InventoryUsability`; shops and drop tables (except SampleScene QA seeds); save/load migration for charge fields (implement with normal serialization).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | Every evocable `ItemInstance` has **`currentCharges`** and **`maxCharges`** with **`currentCharges ≤ maxCharges`** always. |
| **G2** | **No stacking** by definition for evocables: pickup and loot always create or keep **separate instances** (`quantity` is always **1** for this category). |
| **G3** | **Invoke** only when **`currentCharges > 0`**; successful invoke **decrements** current by **1** (never below 0). |
| **G4** | **Consumable-at-zero** vs **rechargeable** is authored per item; rechargeable items use a per-definition **recharge interval** (v0 default **10 player phases** per +1 charge). |
| **G5** | Inventory list shows **current/max charges** for evocable rows (see §6 mock). |
| **G6** | v0 content: **Fan of Fireball** (2/2, targeted fireball, vanishes at 0) and **Fan of Might** (4/4, Sudden Strength, recharges). |
| **G7** | v0 invocation entry point: **inventory Use only** (reuse scroll/knife targeting and instant-use paths). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Charge** | One use of the evocable’s effect. |
| **Max charges** | Upper cap for this instance (`maxCharges`). |
| **Current charges** | Uses remaining before recharge or removal (`currentCharges`). |
| **Invoke / Evoke** | Player activates the item; v0 = inventory **Use**. |
| **Consumable evocable** | When `currentCharges` reaches **0** after a successful invoke, the **`ItemInstance` is removed** from inventory. |
| **Rechargeable evocable** | At **0** charges the instance **stays**; charges increase over time up to `maxCharges`. |
| **Recharge tick** | One step of the recharge timer (v0: **1 player phase** elapsed on the **carrying party member** or **party** — see §5.3). |
| **Recharge interval** | Player phases required to gain **+1** charge (v0 default **10**). |

---

## 3. DCSS reference and recommendations

### 3.1 How DCSS handles evocables (summary)

Modern [Dungeon Crawl Stone Soup evocables](http://crawl.chaosforge.org/Evocable) are a **side option**: limited charges, often comparable to spells but **no spell failure**, usable under **Silence**, usually **no MP**. Miscellaneous evocables (e.g. tin of tremorstones — **2 charges**) **recharge as you gain XP**, not on a fixed turn clock. Duplicate misc evocables **merge** and increase **enchantment**, which **lowers XP needed** to recharge; **Evocations** skill and **Gadgeteer** also speed recharge.

Historical **fans** (e.g. fan of gales, removed 0.25) depleted on use, went **inert**, and recharged with **XP**; carrying multiple copies once shared inert state — later designs moved away from that.

**JRogue differs by design (locked for v0):**

| DCSS pattern | JRogue v0 |
|--------------|-----------|
| XP-based recharge | **Player-phase** timer (default **10** phases per charge) |
| Merge duplicates → +enchant, faster recharge | **No merge** — separate rows per instance, distinct charge counts |
| Evoke via **V** anywhere | **Inventory Use** only |
| Evocations skill scales power | **No** skill gate in v0 (use authored ability as-is) |

### 3.2 Recommendations informed by DCSS (future phases)

These are **guidance**, not v0 requirements:

1. **Recharge pacing** — DCSS ties recharge to **progress** (XP), so evocables stay **emergency tools**, not spammable nukes. When replacing the flat **10-turn** default, consider:
   - **Player phases** (current v0 clock) for predictability in turn-based combat.
   - Optional **XP or kill-based** progress (closer to DCSS) for overworld pacing.
   - **Cap recharge** so items at full charges do not “bank” extra progress.

2. **Charge caps** — DCSS tins often hold **2** charges; wands hold more but are a different item class. Keep **low caps** (2–4) for offensive evocables; **4** for buffs (Fan of Might) is reasonable.

3. **Consumable vs rechargeable** — DCSS offensive/misc evocables are **precious** (slow recharge or finite). **Fan of Fireball** as **consumable-at-zero** matches “found loot, burn it” fantasy; **Fan of Might** as **rechargeable** matches buff fans / rods you keep.

4. **Per-instance recharge** — Prefer **each `ItemInstance`** tracking its own timer (JRogue already splits instances). Avoid DCSS-era “all fans inert together” unless deliberately designing set cooldowns.

5. **Future formula hooks** (when leaving fixed `10`):
   - `effectiveInterval = baseInterval × f(level, evocationsAnalog, enchantment)`
   - **Gadgeteer-like** feat: multiply recharge rate (e.g. interval × 0.75).
   - **Scroll of recharging** analogue: instant +N charges or full refill (consumable scroll, not evocable).

6. **Power scaling** — DCSS scales effect with Evocations; JRogue can later scale `AbilityAction` potencies or duration from a stat without changing charge rules.

7. **UI** — DCSS shows charges in item description; JRogue adds an explicit **Chg** column (§6) for scanability.

---

## 4. Data model

### 4.1 Category and instance fields

| Layer | Requirement |
|-------|-------------|
| **`ItemData.category`** | `ItemCategory.Evocable` |
| **`ItemInstance.quantity`** | **Always 1** for evocables (enforced on add, pickup, and invoke). |
| **`ItemInstance` charge fields** | `currentCharges`, `maxCharges` (serialized on instance). |
| **Definition authoring** | Prefer `EvocableItemData : ItemData` (or equivalent ScriptableObject) with: |

**`EvocableItemData` (authored)**

| Field | Type | Notes |
|-------|------|--------|
| `maxCharges` | int | Default cap when instance created (≥ 1). |
| `startingCharges` | int | Initial `currentCharges` on new instance; clamp to `maxCharges`. |
| `consumesWhenEmpty` | bool | **true** → remove instance at 0 after invoke; **false** → keep and recharge. |
| `rechargeIntervalPlayerPhases` | int | Only if `!consumesWhenEmpty`; v0 default **10**. |
| `invokeAbility` | `AbilityAction` | Effect when invoked (shared assets, e.g. `Fireball_Standard`). |

**Invariants (runtime, all code paths):**

```
0 ≤ currentCharges ≤ maxCharges
maxCharges ≥ 1
quantity == 1
```

Clamp on load, pickup, invoke, recharge, and debug seed.

### 4.2 Stacking and pickup

| Rule | Detail |
|------|--------|
| **No merge on pickup** | If the player already carries `Fan_of_Fireball` with 1 charge and picks up another, inventory has **two rows** (two `ItemInstance` ids). |
| **No quantity stacks** | `InventoryManager.AddItem` must **not** increment `quantity` for evocables; always `new ItemInstance(def)` with authored starting charges. |
| **Sort/display** | `InventoryViewModel` remains **one row per instance** (already true); evocable rows may sort adjacent by name but **never** merge. |

### 4.3 Charge lifecycle

```
[Created] startingCharges (clamped)
    → Invoke success → currentCharges--
        → if currentCharges > 0: keep instance
        → if currentCharges == 0 && consumesWhenEmpty: Remove instance
        → if currentCharges == 0 && rechargeable: start/continue recharge timer
[Recharging] each rechargeIntervalPlayerPhases → currentCharges++ (cap at maxCharges)
```

**Failed invoke** (invalid target, silenced stub, etc.): **no** charge spent (match [Fireball scroll](Fireball-Scroll-Requirements.md) invalid-confirm rules).

**Cancel targeting**: **no** charge spent; inventory reopens with same row selected.

---

## 5. Recharge (rechargeable evocables)

### 5.1 v0 behavior

- **`rechargeIntervalPlayerPhases`**: default **10** on `EvocableItemData` when not consumable-at-empty.
- When `currentCharges == 0`, item **remains** in inventory (greyed or subtitle “Recharging” optional).
- After **10** eligible **player phases** (see 5.3), `currentCharges` becomes **1**; timer resets for next charge until `currentCharges == maxCharges`.
- When `currentCharges == maxCharges`, recharge timer **does not run** (or is cleared).

### 5.2 Future formula (placeholder)

Document only; **not implemented in v0**:

```
phasesPerCharge = baseInterval
    / (1 + evocationsBonus)
    / (1 + enchantmentBonus)
    / gadgeteerMultiplier
```

`baseInterval` defaults from `rechargeIntervalPlayerPhases` (10). Designers can override per asset.

### 5.3 What counts as a “player phase” (locked v0)

**One** recharge tick advances when a **player turn cycle ends** (enemy phase begins), or during **rest** on each `ExecuteRestPlayerPhaseStep`. Implemented in `TurnManager` via `IsPartyDone`, `ForceEndPlayerTurn`, and rest steps — **formation** leader moves + follower rush count the same as solo moves and inventory invokes.

**Open choice for implementation** (pick one and test):

- **A (recommended):** Timer runs on **party** player-phase boundary (any member’s completed action counts once per party turn cycle).
- **B:** Timer runs only while the **carrier** is the actor who just acted.

Requirements doc default: **A**, so off-hand members carrying a fan still recharge during dungeon play.

---

## 6. Inventory UI — charges column (mock)

Evocables use the **Qty** column for **charges**, not stack count. Non-evocable rows keep today’s `×quantity` behavior.

### 6.1 List mock (evocable section highlighted)

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│ INVENTORY — Member: Aria                                                                │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ … category tabs … [ Evocables ]                                                         │
├───────────────────────────────────────┬─────────────────────────────────────────────────┤
│  Chg    Wt     Value                  │ INSPECT — Fan of Fireball                       │
│ ┌──┬──┬──────────────┬────┬─────┬────┐│  ┌─────────────────────────────────────────┐  │
│ │a │📷│ Fan of Fireball │ 2/2 │ 0.5 │ 80││  │ [icon]   Fan of Fireball          ★     │  │
│ │b │📷│ Fan of Fireball │ 1/2 │ 0.5 │ 80││  │ Evocable · Charges 2 / 2 · Consumable   │  │
│ │c │📷│ Fan of Might      │ 4/4 │ 0.5 │120││  │ Invoke: Fireball (targeted)             │  │
│ │d │📷│ Fan of Might      │ 0/4 │ 0.5 │120││  │ On last charge: item is destroyed       │  │
│ └──┴──┴──────────────┴────┴─────┴────┘│  └─────────────────────────────────────────┘  │
│       ↑ Chg = current/max              │  [ Use ]  [ Drop ]  [ Give ]                   │
└───────────────────────────────────────┴─────────────────────────────────────────────────┘
```

**Column rules**

| Column | Evocable row | Other categories |
|--------|----------------|------------------|
| **Chg** (header rename from Qty when any evocable visible, or always show “Chg” with `—` for non-evocables) | `{current}/{max}` e.g. `2/2`, `0/4` | `×1` or `×N` as today |
| **Subtitle** | Optional: `Recharging` when `0/max` and rechargeable | unchanged |
| **Inspect pane** | Lines: **Charges**, **Recharge** (interval or “Consumable”), **Invoke effect** (ability name) | unchanged |

**Use button**

- Enabled iff `currentCharges > 0` and `InventoryUsability` allows invoke.
- Disabled tooltip when `currentCharges == 0`: “No charges remaining.”

### 6.2 Implementation notes (UI)

- Extend `InventoryItemRowView.Bind`: if `row.Item.category == Evocable`, set charge text instead of `×qty`.
- `InventoryDetailFormatter`: append charge block for inspect pane.
- No change to per-instance row model.

---

## 7. Invocation (v0)

### 7.1 Entry

- Player opens inventory → selects evocable row → **Use** (same action bar as scroll).
- **Not** equippable; **not** invoked from equipment hotkeys in v0.

### 7.2 Preconditions

1. `currentCharges > 0`
2. `InventoryUsability.AppearsUsableNow` (owner present, combat policy, turn gate — mirror scroll: block if member cannot act **only when** invoke would spend a turn on success)
3. `invokeAbility` non-null

### 7.3 Targeted vs instant

| Ability | Flow |
|---------|------|
| `requiresTarget == true` (Fan of Fireball) | Close inventory → targeting reticle → confirm spends charge + turn; cancel free |
| `requiresTarget == false` (Fan of Might / Sudden Strength) | Execute immediately on Use; on success spend charge + turn |

Reuse `InventoryItemUse` → `InventoryUseResult.StartedTargeting` and `PlayerCommandProcessor` pending inventory ability state ([Fireball scroll](Fireball-Scroll-Requirements.md) §6).

### 7.4 On successful invoke

1. Run `invokeAbility.Execute(user, target?)` (must return **true**).
2. `currentCharges--` (clamp).
3. If `currentCharges == 0` && `consumesWhenEmpty` → `TryRemoveCarried(instance)`.
4. If `currentCharges == 0` && rechargeable → start recharge timer.
5. End player action / formation (same as scroll confirm).

### 7.5 Usability when empty

- Row **visible**; **Use** disabled.
- Rechargeable at 0/4: show **0/4** in Chg column; optional subtitle **Recharging (N turns)** when timer exposed to UI.

---

## 8. v0 content

### 8.1 Fan of Fireball

| Field | Value |
|-------|--------|
| **itemName** | `Fan of Fireball` |
| **category** | `Evocable` |
| **maxCharges** | 2 |
| **startingCharges** | 2 |
| **consumesWhenEmpty** | **true** (remove at 0) |
| **invokeAbility** | `Fireball_Standard` (`requiresTarget`, splash per [splash doc](../Combat/Area-Ability-Splash-Targeting-Requirements.md)) |
| **weight** | 0.5 (tune) |

**Suggested path:** `Assets/Resources/Item/Evocable/Fan_of_Fireball.asset`

### 8.2 Fan of Might

| Field | Value |
|-------|--------|
| **itemName** | `Fan of Might` |
| **category** | `Evocable` |
| **maxCharges** | 4 |
| **startingCharges** | 4 |
| **consumesWhenEmpty** | **false** |
| **rechargeIntervalPlayerPhases** | **10** (default) |
| **invokeAbility** | `SuddenStrength_Standard` (untargeted; +100 STR, 10 player phases) |
| **weight** | 0.5 (tune) |

**Suggested path:** `Assets/Resources/Item/Evocable/Fan_of_Might.asset`

### 8.3 QA assets

- `WorldItem_Fan_of_Fireball`, `WorldItem_Fan_of_Might` prefabs (mirror scroll world prefab).
- Editor seed: two Fireball fans at **2/2** and **1/2** on test character to prove **no stacking**.
- SampleScene floor placement optional.

---

## 9. System integration map

| System | Change |
|--------|--------|
| **`EvocableItemData`** | New ScriptableObject type + create menu |
| **`ItemInstance`** | `currentCharges`, `maxCharges`, factory helper `CreateEvocable(EvocableItemData)` |
| **`InventoryManager.AddItem`** | Reject merge; enforce qty 1; init charges |
| **`InventoryItemUse`** | Branch evocable: charge check, targeted vs instant, decrement on success only |
| **`InventoryUsability`** | Evocable category usable rules (like Scroll; not equippable) |
| **`InventoryConsumePolicy`** | Evocable: do not use generic “remove whole stack”; use charge rules |
| **Recharge service** | New small service or hook on player-phase end (party scope §5.3) |
| **`InventoryItemRowView` / formatter** | Chg column + inspect text |
| **`PlayerCommandProcessor`** | Pending state includes evocable instance id + charge spend on confirm |
| **Tests** | Charge invariants, no merge pickup, consumable removal, recharge tick, Use disabled at 0 |

---

## 10. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Two `Fan of Fireball` pickups appear as **two rows** with independent `current/max`. |
| **AC2** | Fan of Fireball at **1/2** → Use → targeted fireball → **0/2** → Use → item **removed** from inventory. |
| **AC3** | Fan of Fireball cancel targeting → charge **unchanged** → inventory reopens. |
| **AC4** | Fan of Might **4/4** → Use → Sudden Strength applies → **3/4**; at **0/4** item **still listed**, Use disabled. |
| **AC5** | After **10** player phases with Fan of Might at 0/4, charges become **1/4** (then continues toward 4/4 every 10 phases). |
| **AC6** | List shows **2/2**, **1/2**, **4/4**, **0/4** in Chg column per §6 mock. |
| **AC7** | `currentCharges` never exceeds `maxCharges` after invoke, recharge, or load. |
| **AC8** | Invalid fireball confirm does **not** decrement charges. |

---

## 11. Debug logging

Prefix: **`[Evocable]`**

| Event | Level |
|-------|--------|
| Invoke start (ability, instance id, charges before) | Log |
| Invoke success (charges after, removed?) | Log |
| Invoke blocked (0 charges) | Log |
| Recharge +1 | Log |
| Invariant clamp | Warning |

---

## 12. Implementation checklist

- [x] `EvocableItemData` + charge fields on `ItemInstance`
- [x] Pickup/add: no stacking; init charges
- [x] `InventoryItemUse` + targeting pending state
- [x] Recharge tick on player phase (party scope)
- [x] UI: Chg column + inspect lines
- [x] Assets: Fan of Fireball, Fan of Might + world prefabs
- [x] Editor seed + unit tests (§10)
- [x] SampleScene QA: editor menus below

### SampleScene QA (editor)

| Menu | Purpose |
|------|---------|
| `JRogue/Inventory/Create Evocable v0 Assets` | (Re)create item assets + world prefabs |
| `JRogue/Inventory/Seed Evocables on Party Barbarian Warrior` | Barbarian gets Fireball fans **2/2** + **1/2**, Might fans **4/4** + **0/4** (creates assets if needed) |
| `JRogue/Inventory/Place Evocable Pickups in SampleScene` | Floor pickups for pickup testing |

---

## 13. Related docs

- [Fireball scroll](Fireball-Scroll-Requirements.md) — targeted inventory consume flow
- [Throwing knife](Throwing-Knife-Requirements.md) — quantity stack decrement pattern (contrast: evocables **do not** use quantity for charges)
- [Area ability splash targeting](../Combat/Area-Ability-Splash-Targeting-Requirements.md) — Fan of Fireball preview
- [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md) — column layout baseline
