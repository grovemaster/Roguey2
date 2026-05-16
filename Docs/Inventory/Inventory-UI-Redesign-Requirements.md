# Inventory UI — Requirements: CRPG-style layout & polish

Redesign the **Inventory screen** for full-viewport presentation, readable column layout, and inspect-pane density inspired by **Baldur’s Gate 3**, **Pathfinder: Wrath / Kingmaker**, **Divinity: Original Sin 2**, and **Skyrim / Fallout**-style list inventories.

**Depends on:** existing `InventoryUI`, `InventoryPresentationModel`, `InventoryDetailFormatter`, `ItemCategoryRegistry`, party browse modes, search/filter/sort, user marks, inscriptions, encumbrance, `PartyCurrencyLedger`.

**Explicitly separate (later phase):** **Character screen** with paper doll / equipment silhouette — **not** part of this work.

---

## 1. Goals

**G1.1 — Full-viewport feel**  
Inventory reads as a **full-screen overlay** (minimal outer margin, ~8–16 px), not a small floating panel inside the canvas.

**G1.2 — Scannable list**  
Item rows use **fixed columns** (icon, name, qty, weight, value) with consistent alignment; metadata is not one long wrapped sentence.

**G1.3 — Rich inspect pane**  
Selected item shows **grouped stat blocks** (damage, modifiers, passives/actives, compare-to-equipped, marks, inscription) in a pane occupying **50%** of the body width.

**G1.4 — CRPG affordances**  
Party scope, encumbrance, currency strip, horizontal category tabs, contextual action bar, and **collapsible** hotkey help preserve existing power-user flows without cluttering the default view.

**G1.5 — Value & appraisal**  
A **Value** column shows known gold (or stack value) or **`?`** until the item/stack is appraised.

---

## 2. Locked layout decisions

| # | Decision |
|---|----------|
| L1 | **No paper doll** on Inventory; equipment silhouette → **Character screen (later)**. |
| L2 | **Value column** in list + inspect; **`?`** when unappraised; numeric when known. |
| L3 | Body split: **item list 50%** \| **inspect pane 50%** (horizontal). |
| L4 | **Hotkey / help footer**: **collapsed by default**; **`?`** toggles expand/collapse. |
| L5 | **Categories**: **horizontal tabs** (not a left vertical rail). |

---

## 3. Layout specification

Visual mocks below are the **authoritative layout reference** for review and implementation. §3.4–§3.5 are column/section tables for engineers.

### 3.1 Full viewport mock (top → bottom)

Outer chrome: **≤16 px** margin; panel is full-rect anchored (not a floating card).

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│ INVENTORY                                                                               │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ PARTY / SCOPE (BG3 / DOS2)                                                              │
│   ● Aria    ○ Bruenor    ○ Imoen          Mode: [ Member ▾ ]                            │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ ENCUMBRANCE (PF — always visible)                                                       │
│   ████████████░░░░  142 / 180        Gold 1,240    ·    (other currencies from ledger) │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ SEARCH + FILTERS                                                                        │
│   [ Search __________________________ ]     Usable only ☐    Marks: Fav · Prot · Junk   │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ CATEGORY TABS (horizontal — §2 L5)                                                      │
│   [ All ] [ Weapons ] [ Armor ] [ Consumables ] [ … ]  ← ItemCategoryRegistry labels  │
├───────────────────────────────────────┬─────────────────────────────────────────────────┤
│ ITEM LIST — 50% width                 │ INSPECT — 50% width (§3.3)                      │
│                                       │                                                 │
│  Qty    Wt     Value   (headers)      │  ┌─────────────────────────────────────────┐  │
│ ┌──┬──┬──────────────┬────┬─────┬────┐│  │ [icon 96–128]   ITEM NAME        ★ fav  │  │
│ │a │📷│ Iron Sword   │ ×1 │ 3.2 │ 45 ││  │ Slashing · MainHand · Rare              │  │
│ │b │📷│ Mystery Gem  │ ×1 │ 0.1 │  ? ││  └─────────────────────────────────────────┘  │
│ │c │📷│ …            │    │     │    ││  Value · Weight · Location · Owner            │
│ └──┴──┴──────────────┴────┴─────┴────┘│  ── Damage / Modifiers / Passives / Actives ──  │
│   ↑ rows per §3.2                     │  ── Compare vs equipped (same slot) ─────────  │
│                                       │  Inscription · Marks · Guards (scroll)          │
├───────────────────────────────────────┴─────────────────────────────────────────────────┤
│ ACTIONS (contextual, BG3-style)                                                         │
│   [ Equip ]  [ Use ]  [ Drop ]  [ Give ]  … only when applicable                        │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ [ ? ]  Hotkeys & help (collapsed by default — §3.5)                                      │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

**Not on this screen:** paper doll / equipment silhouette (Character screen, later phase — §2 L1).

### 3.2 Item row mock (column layout)

Example rows (Focused Member mode — no owner subtitle; Party Aggregate adds owner on subtitle line).

```
┌────┬────┬────────────────────────────┬─────┬──────┬────────┐
│ Ltr│Icon│ Name                       │ Qty │ Wt   │ Value  │
│    │    │ optional subtitle line     │     │ (kg) │        │
├────┼────┼────────────────────────────┼─────┼──────┼────────┤
│ a  │ 📷 │ [F] Iron Sword             │ ×1  │ 3.2  │   45   │
│    │    │ #a1b2 · MainHand           │     │      │        │
│    │    │ [E MainHand]  (if equipped)│     │      │        │
├────┼────┼────────────────────────────┼─────┼──────┼────────┤
│ b  │ 📷 │ Mystery Gem                │ ×1  │ 0.1  │    ?   │
│    │    │ Unappraised                │     │      │        │
├────┼────┼────────────────────────────┼─────┼──────┼────────┤
│ c  │ 📷 │ Healing Potion             │ ×3  │ 0.9  │   12   │
│    │    │ #c4d5 · Potion             │     │      │        │
└────┴────┴────────────────────────────┴─────┴──────┴────────┘
  ↑    ↑              ↑                     ↑     ↑        ↑
marks icon      title + subtitle          qty   weight   value
```

**Party Aggregate** — add owner to subtitle, e.g. `· Aria` (not a separate column in v1).

| Column | Content | Notes |
|--------|---------|--------|
| **Letter** | Hotkey letter (`a`–`z`) | Pill or fixed narrow column; existing letter binding preserved. |
| **Icon** | `ItemData.icon` or placeholder | ~40–48 px; preserve aspect. |
| **Name** | Colored name + optional subtitle | Subtitle: slot type, short id, equip badge; see §4.2. |
| **Qty** | `×N` if stack > 1 | Right-aligned. |
| **Wt** | Stacked weight (`kg`) | Right-aligned. |
| **Value** | Known value or `?` | Right-aligned; see §5. |

**Marks** (`[F]`, `[P]`, `[J]`) appear as chips/icons before the name (existing semantics).

**Equipped** indicator: compact badge on subtitle row, e.g. `[E MainHand]` — not one long wrapped TMP line.

### 3.3 Inspect pane mock (50% width)

```
┌──────────────────────────────────────────────────────────────┐
│  [96×128 icon]     IRON SWORD                    ★ Favorite   │
│                    Slashing · MainHand · Rare                 │
├──────────────────────────────────────────────────────────────┤
│  Value (stack)      45          (or  ?  if unappraised)       │
│  Weight (stack)     3.2 kg                                      │
│  Location           Carried · Aria                              │
├──────────────────────────────────────────────────────────────┤
│  ── Damage ──                                                   │
│  ── Stat modifiers ──                                           │
│  ── Passive / Active ──                                         │
├──────────────────────────────────────────────────────────────┤
│  ── Compared to equipped (Main Hand) ──                         │
│  (InventoryDetailFormatter compare block)                       │
├──────────────────────────────────────────────────────────────┤
│  Inscription · Marks · Guards                                   │
└──────────────────────────────────────────────────────────────┘
```

### 3.4 Inspect pane sections (content order)

Order (top → bottom, scroll as needed):

1. **Hero** — large icon (~96–128 px), title, favorite/mark chips, category / slot / risk hints.
2. **Summary** — value (or `?`), stack weight, location, owner (when relevant).
3. **Damage** — if any.
4. **Stat modifiers** — if any.
5. **Passive / Active** — if any.
6. **Compare vs equipped** — reuse / extend `InventoryDetailFormatter.FormatCompareEquippedSameSlot`.
7. **Inscription, marks, guards** — existing formatter behavior.

### 3.5 Collapsed footer mock

| State | UI |
|-------|-----|
| **Collapsed (default)** | `[ ? ]` control + one-line hint, e.g. `Press ? for controls`. |
| **Expanded** | Full hotkey copy currently in `ApplyFooterCopy()` (mode, scope, category, search, sort, nav, actions, Phase 3 notes). |

Toggle: **`?`** key and clickable `?` control. Persist expanded state in session optional (default **collapsed** on open).

---

## 4. Functional requirements

### 4.1 Chrome & layout

**R4.1.1** Root `InventoryPanel` remains full-rect anchored; reduce perceived inset via **minimal padding** and **50/50 body split** (replace fixed ~270 px detail width).

**R4.1.2** `InventoryBodyColumns` (or successor) uses horizontal layout: list **flex 1**, detail **flex 1** (equal width ≈ 50% each at runtime).

**R4.1.3** Encumbrance + currency strip stays **always visible** above search (existing `BuildWeightAndCurrencyLine` behavior, restyled).

**R4.1.4** Search line and filters remain above category tabs; search focus / inscription modes unchanged in behavior.

### 4.2 Party & browse mode

**R4.2.1** **Party strip** shows one control per active party member; selection sets **Focused Member** browse index (existing `_memberCarouselIndex` / `BrowseMode.FocusedMember`).

**R4.2.2** **Mode** control toggles **Focused Member** vs **Party Aggregate** (existing `BrowseMode`).

**R4.2.3** In **Focused Member** mode, **omit owner** from row subtitle (carrier is implicit).

**R4.2.4** In **Party Aggregate** mode, show **owner** on row subtitle or dedicated narrow column (subtitle preferred for v1).

### 4.3 Category tabs

**R4.3.1** Horizontal tab bar: **All** + one tab per category from `ItemCategoryRegistry.CategoriesForFilterCycle()` (labels = `HeaderLabel`; **Currency** omitted from tabs — still in encumbrance strip).

**R4.3.2** Active tab drives the same filter as today’s category cycle (`_categoryCycleIndex` / `CurrentCategoryFilter()`).

**R4.3.3** Keyboard `[` / `]` category cycle **remains**; tabs stay in sync.

**R4.3.4** Section headers inside the list may remain for grouped sort modes or be simplified when tabs already filter — **implementation choice**: either keep section headers for sort grouping only, or hide redundant headers when a single category tab is active (document in PR).

### 4.4 Item list & selection

**R4.4.1** Replace single-line row TMP with **column layout** (`InventoryItemRowView` refactor or new prefab).

**R4.4.2** Column headers row: **Qty**, **Wt**, **Value** (and optional **Name** label) — sticky or static above scroll content.

**R4.4.3** Selection highlight, scroll-into-view, letter hotkeys, and click-to-select behavior **unchanged**.

**R4.4.4** Sort mode cycling (`0` / profile) **unchanged**; optional **Sort ▾** in header is **nice-to-have**, not required for v1.

### 4.5 Inspect pane

**R4.5.1** Detail pane uses **50%** width; content uses grouped sections per §3.3–§3.4.

**R4.5.2** **Value** line in inspect mirrors list rules (§5).

**R4.5.3** Reuse `InventoryDetailFormatter` where possible; extend for value/appraisal display only as needed.

### 4.6 Actions bar

**R4.6.1** Contextual action bar below the 50/50 body (above footer).

**R4.6.2** Actions map to existing behaviors: Equip, Unequip, Drop (+ confirm), Use stub, Give stub, Inspect (log) — enable/disable from selection + policy (combat, protected, etc.).

**R4.6.3** Keyboard shortcuts for actions **unchanged**; expanded footer documents them.

### 4.7 Help footer

**R4.7.1** Footer **collapsed** on every open unless user expanded in-session (optional `PlayerPrefs` for expanded state — default **false**).

**R4.7.2** **`?`** toggles expand/collapse when inventory is open and not in search/inscription text focus (or `?` only when not typing — match search focus rules).

**R4.7.3** Expanded content equals or supersedes current `footerText` / `ApplyFooterCopy()` strings.

### 4.8 Accessibility & theme

**R4.8.1** `InventoryAccessibilitySettings` scales (list font, detail font, high-contrast rows) **apply** to new column text and inspect sections.

**R4.8.2** Dark panel theme, row tints, selected tint **preserved** (may tune colors for column readability).

---

## 5. Value & appraisal

### 5.1 Display rules

| Condition | List column | Inspect |
|-----------|-------------|---------|
| Appraised (known) | Formatted integer, e.g. `45` or `1,240` | Same + label `Value` |
| Not appraised | `?` (muted) | `?` + optional hint “Unappraised” |
| No monetary value (data) | `—` | Omit or `—` per `ItemData` flag |

Stack value: **per-unit value × quantity** when appraised (document formula in implementation).

### 5.2 Data contract (new / extended)

**D5.1 — Base value on definition**  
Add **`ItemData` monetary value** field (e.g. `int baseValue` or `int goldValue`) for designer-authored worth. Items with `0` and no appraisal requirement may show `—`.

**D5.2 — Appraisal on instance**  
Add **`ItemInstance` appraisal state**, e.g. `bool isAppraised` (or `AppraisalState` enum). **Default:** unappraised for loot that requires appraisal; **known** items may spawn appraised from shop/quest rewards (content rules).

**D5.3 — Appraisal scope**  
**Per instance** (recommended): appraising one stack member does not auto-appraise all items of that definition globally unless a future “identify item type” ability says otherwise.

**D5.4 — Appraisal gameplay (stub OK for UI phase)**  
Appraisal **action** (shop, skill, use item) can be a **stub** that sets `isAppraised = true` for testing; full economy integration is a separate systems task.

**D5.5 — Persistence**  
Appraisal flag **saved** with inventory / instance persistence (same path as quantity, marks, inscription).

---

## 6. Out of scope

| Item | Notes |
|------|--------|
| Paper doll / equipment silhouette | **Character screen**, later phase. |
| Equip slots UI on inventory | Equip via list + actions only. |
| New item categories | Use `ItemCategoryRegistry`; new enum members get fallback tab labels. |
| Grid inventory / drag-drop | List remains primary. |
| Full shop / merchant UI | Value column supports future merchant flows. |
| Replacing `InventoryDetailFormatter` compare logic | Extend, don’t rewrite unless required. |

---

## 7. Implementation notes (existing code)

| Area | Current | Target |
|------|---------|--------|
| `InventoryUI.EnsureInventoryBodySplitAndDetails` | Detail `preferredWidth = 270` | 50% flex split |
| `InventoryItemRowView.Bind` | Single `detailsText` line | Column row prefab |
| `ItemRowPrefab` | One TMP child | Letter, icon, name stack, qty, wt, value |
| Category filter | `[` / `]` cycle + section headers | Horizontal tabs + cycle |
| Footer | Always visible multi-line | Collapsed + `?` |
| `ItemData` | No gold field | Add `baseValue` (name TBD) |
| `ItemInstance` | No appraisal | Add appraised flag |

---

## 8. Open decisions (defaults for v1)

Resolve during implementation if not overridden:

| Topic | v1 default |
|-------|------------|
| Sort UI in header | **Hotkey only** (`0` cycle); no Sort ▾ dropdown required. |
| Appraisal scope | **Per `ItemInstance`**. |
| Owner in aggregate | **Subtitle** under name, not extra column. |
| Tab labels | **`ItemCategoryRegistry.HeaderLabel`** + **All** tab. |
| Section headers in list | Keep when sort groups by category; hide when redundant with active tab (optional polish). |

---

## 9. Acceptance checklist

- [ ] Inventory fills viewport with ≤16 px outer padding; body is **50% list / 50% inspect**.
- [ ] No paper doll or equip-slot silhouette on this screen.
- [ ] Horizontal **category tabs** (+ **All**); keyboard category cycle still works.
- [ ] Rows show **letter, icon, name, qty, weight, value** columns with aligned numerics.
- [ ] Value shows number or **`?`**; inspect pane repeats value rules.
- [ ] Party strip + member vs aggregate mode; owner hidden in member-only mode.
- [ ] Encumbrance + currency strip always visible.
- [ ] Contextual **actions bar** present; existing key bindings still work.
- [ ] Footer **collapsed** by default; **`?`** expands full hotkey help.
- [ ] Accessibility font/contrast settings apply to new UI.
- [ ] Appraisal state persists on save/load (when persistence layer exists for instances).
- [ ] No regression: search, marks, inscription, drop confirm, sort, selection scroll, combat policy stubs.

---

## 10. Relation to roadmap

| Track | Relationship |
|-------|----------------|
| **Inventory UI (this doc)** | Presentation & layout; appraisal **data** + display. |
| **Character screen (later)** | Paper doll, worn equipment layout, possibly deeper equip UX. |
| **Economy / shops (later)** | Appraisal actions, buy/sell using `baseValue`. |

Recommended sequencing: **layout shell** (50/50, tabs, footer collapse) → **row prefab columns** → **value/appraisal data** → **actions bar polish** → acceptance pass.
