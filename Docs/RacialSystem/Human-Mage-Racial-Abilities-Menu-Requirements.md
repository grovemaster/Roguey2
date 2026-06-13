# Human Mage — Racial abilities menu (Arcane grimoire)

**Purpose:** Specify the **Human Mage body** of the shared [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (`K`): a **known vs prepared spell sheet** for the focused **Human Mage** — **prepared (equipped) spells** on the left, **full grimoire (known library)** on the right, **pinned detail pane** below, and **town-only loadout editing** (equip / unequip within **Magic Power budget**).

**Status:** Implemented (Human Mage body in `HumanMageSpellBodyView` / `HumanMageSpellBodyViewModel`; wired from `RacialAbilitiesUI`).

**Visual mock:** [`Docs/RacialSystem/human-mage-racial-abilities-menu-mock.png`](human-mage-racial-abilities-menu-mock.png) (town edit mode — companion to §8).

**Depends on:** [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (shell, **`K`**, party strip, modal rules), [Human Mage — Spells & spellbooks](Human-Mage-Spells-And-Spellbooks-Requirements.md) (`MageSpellDefinition`, `HumanMageSpellsRuntime`, equip budget, cast costs, spellbook learning), [Human — Class powers](Human-Class-Powers-Requirements.md) (`HumanClass.Mage`, Magic Power pools), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (`HotbarEntryKind.HumanMageSpell`, overflow assign pool), `SafeZonePolicyService`, `HumanMageSpellLoadoutService`, `MageSpellCatalogService`, `HumanMageHotbarSync`.

**Related:** [Dragonian — racial abilities menu](Dragonian-Racial-Abilities-Menu-Requirements.md) (closest structural analogue — two-column loadout sheet + detail pane; Dragonians use **Soul Power** memory budget and **memorize**, Mages use **Magic Power** budget and **equip**), [Elf — racial abilities menu](Elf-Racial-Abilities-Menu-Requirements.md) (party-scoped body), [Tiefling — racial abilities menu](Tiefling-Racial-Abilities-Menu-Requirements.md) (detail-pane action pattern).

**Explicitly out of scope (v0):** **Learning** new spells from this menu (spellbooks + [Mage Tutor](Human-Mage-Spells-And-Spellbooks-Requirements.md) only); **unlearn** / respec known library; **casting** spells from the menu; reordering prepared list (order follows equip order / stable sort by display name); gamepad layout; persisting last-focused party member across sessions; spell **tier** comparison UI; editing another party member’s loadout while a non–Human Mage is focused; Knight / Priest / unclassed Human bodies (separate future docs).

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Human Mage body mounts when focused member is `Race.Human`, `humanClass == HumanClass.Mage`, `RacialSubsystemKind.HumanSpecialization`, and `HumanMageSpellsRuntime` present. |
| **L2** | **Two-column body:** **left = prepared loadout** (“Prepared spells”); **right = known library** (“Grimoire”). |
| **L3** | **Detail pane** pinned below columns (~28–32% height); populated by **clicking any spell row** in either column. |
| **L4** | **Dungeon / non–safe zone:** menu is **view-only** — lists and detail readable; **no** equip / unequip actions. |
| **L5** | **Town safe zone + not in combat:** player may **equip** and **unequip** via detail-pane actions, subject to **Magic Power equip budget** ([spells doc §6](Human-Mage-Spells-And-Spellbooks-Requirements.md)). |
| **L6** | **Equip gate:** spell must be **known** (`KnownSpells`) and `RemainingEquipCapacity >= spell.EquipCost`. |
| **L7** | **Unequip gate:** spell must be in **equipped** set; always allowed when edit mode is active (frees capacity; does **not** remove from known library). |
| **L8** | **Hotbar:** only **equipped** spells appear in hotbar assign pool (`HotbarAssignabilityService` — existing). Detail pane provides **Add to hotbar** for equipped spells (§11). |
| **L9** | **Learn spells** only via **spellbook read** and v0 presets — banner in edit mode points to **Arcane Vendor** / spellbooks; empty grimoire state names tutor + vendor explicitly. |
| **L10** | Menu refresh on **open**, **focus change**, and after **successful equip / unequip / hotbar add** — not every frame. |
| **L11** | **Aesthetic:** `RacialUiTheme` dark glass; Human Mage accent **arcane violet / soft cyan** (distinct from Dragonian crimson, Elf teal, Barbarian totem gold). |
| **L12** | **Terminology in UI:** player-facing **“Prepared spells”** = engine **`EquippedSpells`**; **“Grimoire”** = **`KnownSpells`**. Do **not** use Dragonian “word-forms” copy on this body. |
| **L13** | **Budget resource:** Human Mage loadout uses **`MaxMagicPower`** equip capacity — **not** Dragonian Soul Power. (User-facing strip label: **Magic Power**.) |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Loadout clarity** — Player sees which arcane spells are **ready to cast** (prepared) vs merely **known**. |
| **G2** | **Town planning** — Player adjusts prepared set in **safe town** before entering the dungeon — same discipline as Dragonian memorize. |
| **G3** | **Budget honesty** — UI shows **prepared equip cost sum**, **remaining capacity**, and **current Magic Power** (cast pool) so equip vs cast costs are not confused. |
| **G4** | **Reference in dungeon** — Player can still **open `K`** underground to **read** spell descriptions and current loadout, but cannot change it. |
| **G5** | **Hotbar bridge** — Player can **add prepared spells to the hotbar** from the detail pane; full slot layout remains on the **ability hotbar**. |
| **G6** | **Spellbook loop closure** — After reading a book, player has an obvious next step: open racial menu → verify/adjust prepared set → hotbar. |
| **G7** | **Multi-Mage party** — Party strip switches body per focused member; each Human Mage has independent known + prepared lists. |
| **G8** | **Class gate clarity** — Human **None / Knight / Priest** see placeholder copy, not an empty grimoire. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Human Mage body** | Race-specific panel in `RacialAbilitiesUI` when focused member is a committed Human Mage. |
| **Grimoire (known library)** | Spells the member has **learned** (`HumanMageSpellsRuntime.KnownSpells`) — permanent until future unlearn content. |
| **Prepared loadout** | Subset of known spells **equipped for casting** (`EquippedSpells`) — UI label **“Prepared spells”**. |
| **Equip budget** | `MaxMagicPower` minus Σ `EquipCost` of prepared spells (`RemainingEquipCapacity`). |
| **Cast pool** | `CharacterStats.currentMagicPower` — spent when **casting**, not when equipping. |
| **Equip cost** | Per spell: `(10 - tier) + extraEquipCost` on `MageSpellDefinition`. |
| **Cast cost** | Per spell: `magicPowerCost` deducted from **current** Magic Power on successful cast. |
| **Edit mode** | Town safe zone **and** party **not** in combat (`SafeZonePolicyService.TryAllowHumanMageEquipChange`). |
| **View-only mode** | Any context where loadout edits are blocked — dungeon, combat, or non–safe zone. |
| **Detail pane** | Bottom region showing selected spell icon, stats, description, and action buttons. |

**Contrast with Dragonian (do not conflate in UI copy):**

| Human Mage | Dragonian |
|------------|-----------|
| Magic Power budget | Soul Power memory budget |
| Equip / Unequip | Memorize / Unmemorize |
| Prepared spells | Equipped word-forms |
| Grimoire | All word-forms |
| Learn from spellbooks / tutor quest | Learn from Elder quests |

---

## 3. Screen responsibilities (locked)

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Racial menu — Human Mage body (edit mode)** | View grimoire + prepared lists; **equip / unequip**; **add prepared spell to hotbar**; read descriptions | Learn spells, cast, reorder hotbar slots |
| **Racial menu — Human Mage body (view-only)** | View lists + descriptions | Equip, unequip, hotbar add |
| **Mage Tutor / spellbook read** | Commit class; **learn** spells | Equip or change loadout (except spellbook read may auto-prepare in town — see §11.3) |
| **Ability hotbar** | Assign **prepared** spells; cast in combat | Show full grimoire reference |
| **Quest journal** | Track tutor apprenticeship | Edit prepared loadout |

### 3.1 — Banner copy (required)

| Mode | Banner |
|------|--------|
| **Edit mode (town, peace)** | *Prepare arcane spells here. Learn new spells from **spellbooks** — buy them from the **Arcane Vendor** after training with the **Mage Tutor**.* |
| **View-only (dungeon)** | *View only — you can only adjust prepared spells in town.* |
| **View-only (combat)** | *View only — finish combat before adjusting prepared spells.* |

No teleport buttons; tutor / vendor named as progression direction only.

---

## 4. Router integration

Extends parent [§5.3 router](../UI/Racial-Abilities-Menu-Requirements.md):

```
RacialAbilitiesUI.RefreshBodyForFocusedMember()
  → …
  Race.Human + humanClass.Mage + HumanSpecialization + HumanMageSpellsRuntime
      → HumanMageSpellBodyView
  (else default placeholder)
```

| Condition | Body |
|-----------|------|
| Human + **Mage** + `HumanMageSpellsRuntime` | **Two-column spell sheet** (this doc) |
| Human + **None / Knight / Priest** | Default placeholder: *“This character has not committed to the Mage path.”* (Knight/Priest: add *“Class commitment is permanent.”*) |
| Human Mage but no runtime | Default placeholder: *“Arcane spell data is missing for this character.”* |
| Not Human | Default placeholder (unchanged) |

---

## 5. Data sources

| UI region | Runtime / data |
|-----------|----------------|
| **Grimoire list** | `HumanMageSpellsRuntime.KnownSpells` sorted **display name A→Z**, tie-break `spellId` |
| **Prepared list** | `HumanMageSpellsRuntime.EquippedSpells` — same sort |
| **Budget strip** | `CharacterStats.MaxMagicPower`, `RemainingEquipCapacity`, Σ prepared `EquipCost`, `currentMagicPower` |
| **Spell metadata** | `MageSpellDefinition`: `displayName`, `description`, `tier`, `EquipCost`, `magicPowerCost`, `ability` icon |
| **Edit eligibility** | `SafeZonePolicyService.TryAllowHumanMageEquipChange` |
| **Mutations** | `HumanMageSpellLoadoutService.TryEquip` / `TryUnequip` only — no direct runtime calls from UI |
| **Hotbar add** | `HumanMageHotbarSync.TryAssignEquippedSpellsToEmptyMainSlots` + `AbilityHotbarUI.RefreshAll` |

**Do not** list spells from `MageSpellCatalog` that are **not known** — grimoire column is the source of truth for the right column.

---

## 6. Magic Power budget strip

Pinned **above** the two columns (always visible).

| Field | Format | Example |
|-------|--------|---------|
| **Max capacity** | `Max Magic Power: {MaxMagicPower}` | Max Magic Power: 20 |
| **Prepared cost** | `Prepared: {sumEquipCost} / {MaxMagicPower}` | Prepared: 16 / 20 |
| **Remaining** | `Free: {RemainingEquipCapacity}` | Free: 4 |
| **Cast pool** | `Current Magic Power: {currentMagicPower}` | Current Magic Power: 18 |

**Muted footnote (one line):** *Preparing spells spends **capacity** only. Casting spends **current** Magic Power.*

When **edit mode** and remaining capacity is **0**, grimoire rows with `EquipCost > 0` show disabled equip affordance.

---

## 7. Two-column spell lists

### 7.1 — Column headers

| Column | Header | Subtitle |
|--------|--------|----------|
| **Left (~45%)** | `PREPARED SPELLS` | *Ready to assign on the hotbar* |
| **Right (~55%)** | `GRIMOIRE` | *Spells you have studied* |

Both columns scroll independently inside the middle band (~45–50% viewport height).

### 7.2 — Row content

Each row (either column):

| Element | Source |
|---------|--------|
| **Icon** | `spell.ability.hotbarIcon` if present; else arcane spell emblem fallback |
| **Title** | `displayName` (fallback `spellId`) |
| **Subtitle** | `Prepare {EquipCost} MP · Cast {magicPowerCost} MP` |
| **Badge (grimoire only)** | `Prepared` pill if spell is also equipped |
| **Selection** | Violet/cyan outline on selected row (either column) |

Optional tier hint (muted): `Tier {tier}` on subtitle or detail only — not required v0.

### 7.3 — Empty states

| Column | Empty copy |
|--------|------------|
| **Left (no prepared)** | *No spells prepared. Select a known spell from your grimoire and prepare it.* |
| **Right (no known)** | *Your grimoire is empty. Complete **Arcane Apprenticeship** with the **Mage Tutor**, then study **spellbooks** from the **Arcane Vendor**.* |

### 7.4 — Interaction

| Input | Behavior |
|-------|----------|
| **Click row (either column)** | Select spell; rebuild **detail pane** |
| **Default selection on open** | First prepared spell if any; else first known spell; else none |
| **Change focused party member** | Rebuild columns + detail; re-run default selection |
| **Scroll** | Independent per column; budget strip + detail pane pinned |

No drag-and-drop between columns in v0 — use detail-pane **Prepare** / **Unprepare** buttons.

---

## 8. Visual layout (mock — authoritative)

Uses same **full-screen racial shell** as parent doc (title, banner, party strip, footer).

```
┌──────────────── FULL SCREEN ──────────────────────────────────────────────┐
│ RACIAL ABILITIES                                                            │
│ Prepare arcane spells here. Learn new spells from spellbooks — buy them…   │  banner (edit)
│ [F1 Human Mage ●] [F2 …] [F3 …] … party strip                              │
├─────────────────────────────────────────────────────────────────────────────┤
│ MAGIC POWER · Max 20 · Prepared 16/20 · Free 4 · Current MP 18             │  budget strip
├──────────────────────────────┬──────────────────────────────────────────────┤
│ PREPARED SPELLS              │ GRIMOIRE                          (scroll)   │
│ ┌──────────────────────────┐ │ ┌──────────────────────────────────────────┐│
│ │ [icon] Fireball            │ │ │ [icon] Arcane Might                      ││
│ │ Prepare 7 · Cast 5       │ │ │ Prepare 3 · Cast 2                       ││
│ │ [icon] Lightning Bolt      │ │ │ [icon] Fireball              [Prepared] ││
│ │ Prepare 6 · Cast 4       │ │ │ [icon] Lightning Bolt        [Prepared] ││
│ └──────────────────────────┘ │ └──────────────────────────────────────────┘│
│ (empty hint if none)         │                                              │
├──────────────────────────────┴──────────────────────────────────────────────┤
│ DETAILS                                                                       │
│ ┌────┐  Fireball                                                              │
│ │icon│  Hurl a sphere of flame at a target tile.                             │
│      │  PREPARE COST 7 · CAST COST 5 · Fireball (ability)                     │
│      │  [ Unprepare ]  [ Add to hotbar ]              (edit, prepared)       │
│      │  Assign prepared spells on the **ability hotbar** to cast in combat.   │
├─────────────────────────────────────────────────────────────────────────────┤
│ K — racial abilities · Esc — close · F1–F5 — focus member                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Grimoire row selected (known, not prepared) — edit mode:**

```
│      │  [ Prepare ]  (disabled if insufficient Free capacity)                 │
```

**View-only variant:** Prepare / Unprepare / Add to hotbar **hidden or disabled**; banner uses §3.1 dungeon/combat copy.

### 8.1 — Layout tokens

| Token | Value | Notes |
|-------|-------|-------|
| Panel background | `(0.08, 0.085, 0.095, 0.96)` | Match inventory / racial |
| Mage accent | `(0.42, 0.34, 0.82)` selected row bar | Arcane violet |
| Secondary accent | `(0.28, 0.52, 0.88)` budget strip tint | Soft cyan |
| Column divider | 1px `(0.30, 0.25, 0.55, 0.5)` | |
| Row min height | **64px** | Icon 40–48px |
| Prepared badge | Small pill, soft cyan | Grimoire column only |
| Detail pane height | **~30%** | Scroll inside if description long |
| Typography | Title 28 / banner 17 / column header 20 / row title 19 / body 17 / footer 15 | TMP |

---

## 9. Detail pane

### 9.1 — Content (all modes)

| Block | Content |
|-------|---------|
| **Icon** | Large spell / ability icon (64–80px) |
| **Title** | `displayName` |
| **Description** | `MageSpellDefinition.description` |
| **Costs** | `Prepare cost: {EquipCost}` · `Cast cost: {magicPowerCost} Magic Power` · optional `Tier {tier}` |
| **Ability line** | Underlying `AbilityAction.abilityName` when present |
| **Hotbar footnote** | *Assign prepared spells on the **ability hotbar** to cast in combat.* |

### 9.2 — Actions (edit mode only)

| Selected spell state | Primary button(s) | Enabled when |
|----------------------|-------------------|--------------|
| **Known, not prepared** | **Prepare** | `RemainingEquipCapacity >= EquipCost` |
| **Known, insufficient capacity** | **Prepare** (disabled) | Tooltip: *Need {cost} free capacity; have {remaining}.* |
| **Prepared** | **Unprepare** | Always |
| **Prepared** | **Add to hotbar** | At least one empty main hotbar slot **or** spell not yet on hotbar; see §11 |

On success: call `HumanMageSpellLoadoutService` (equip/unequip) or `HumanMageHotbarSync` (hotbar add), refresh body, `AbilityHotbarUI.RefreshAll()`, keep selection on same spell.

On failure: show `failureReason` inline below buttons (muted red) — no modal.

### 9.3 — View-only mode

| Element | Behavior |
|---------|----------|
| **Prepare / Unprepare / Add to hotbar** | Hidden **or** visible disabled with tooltip *“Only in town.”* |
| **Detail content** | Fully readable |

---

## 10. Edit policy (implementation)

Reuse **`SafeZonePolicyService.TryAllowHumanMageEquipChange`** (already requires safe zone **and** not in combat):

| Context | Lists | Detail | Prepare / Unprepare / Hotbar add |
|---------|-------|--------|----------------------------------|
| Town, peace | ✓ | ✓ | ✓ |
| Town, in combat | ✓ | ✓ | ✗ |
| Dungeon | ✓ | ✓ | ✗ |

**Deny messages (existing):**

| Case | Message |
|------|---------|
| Not safe zone | *You can only adjust prepared spells in town.* (`MageEquipDenyMessage`) |
| In combat | *You cannot adjust equipped word-forms during combat.* (`CombatMemorizeDenyMessage` — consider Mage-specific copy in v0.1) |

---

## 11. Hotbar integration

| Rule | Detail |
|------|--------|
| **Assign pool** | Unchanged — `HotbarAssignabilityService` lists **prepared** spells under **Mage Spells** group. |
| **Add to hotbar (menu)** | Detail-pane button calls `HumanMageHotbarSync.TryAssignEquippedSpellsToEmptyMainSlots(actor)` for the **selected prepared spell’s equipped index** (assign that spell to first empty main slot among 1–0). If already on hotbar, button disabled with tooltip *“Already on hotbar.”* If all main slots full, disabled with *“Hotbar full — open ability hotbar to rearrange.”* |
| **After unprepare** | Hotbar entries pointing at removed prepared index become **stale** — existing hotbar refresh / stale handling applies. |
| **After prepare** | New spell appears in overflow pool; **Add to hotbar** offers one-click assign. |
| **Menu responsibility** | Loadout edit + **quick hotbar assign** — not full hotbar editor (drag/reorder stays on ability hotbar). |

### 11.1 — Interaction with ability hotbar Edit mode

Optional v0.1: **Add to hotbar** opens ability hotbar **Edit** mode with overflow visible. v0: silent assign to first empty slot only.

### 11.2 — Stale index handling

`HotbarEntryKind.HumanMageSpell` stores **`abilityIndex`** into `EquippedSpells` — same contract as Dragonian. After unprepare, indices shift; hotbar stale cleanup on `AbilityHotbarUI.RefreshAll` remains authoritative.

### 11.3 — Spellbook read auto-prepare (interim)

Until this menu ships, spellbook read in town may **auto-prepare** and **auto-assign hotbar** ([implementation bridge](../../Assets/Scripts/Racial/MageSpellbookReadService.cs)). When this menu ships, keep auto-prepare on read as convenience **or** remove it — product decision at implementation time; menu remains canonical loadout editor.

---

## 12. View-model builder

Suggested API (implementation hint):

```
HumanMageSpellBodyViewModel.Build(BaseActor mage, string selectedSpellId = null)
  → runtime = mage.GetComponent<HumanMageSpellsRuntime>()
  → stats = mage.GetComponent<CharacterStats>()
  → editMode = ResolveEditMode()  // mirror DragonianSpellBodyViewModel
  → budget = BuildBudgetStrip(stats, runtime)
  → preparedRows = SortSpells(runtime.EquippedSpells)
  → knownRows = SortSpells(runtime.KnownSpells)
  → selection = ResolveDefaultSelection(preparedRows, knownRows)
  → detail = BuildDetail(selection, runtime, editMode, hotbarLayout)
  → return { EditMode, BannerText, Budget, PreparedRows, KnownRows, Detail }
```

Unit tests cover: sort order, edit gating, equip capacity denial, empty states, selection default, detail button visibility, hotbar add when slots full.

---

## 13. Integration

| System | Rule |
|--------|------|
| **RacialAbilitiesUI** | Router mounts `HumanMageSpellBodyView`; refresh on open / focus / loadout change. |
| **HumanMageSpellLoadoutService** | Sole mutation path for equip / unequip from UI. |
| **HumanMageSpellsRuntime** | Read lists + budget; mutations via service only. |
| **MageSpellbookReadService** | `TryLearnSpell` only — menu reflects new known spell on next open. |
| **AbilityHotbarUI** | `RefreshAll` after loadout or hotbar add. |
| **HumanMageHotbarSync** | Shared helper for menu **Add to hotbar** and spellbook interim bridge. |

---

## 14. Acceptance criteria

| ID | Test |
|----|------|
| **A1** | Focus Human Mage with Fireball known + prepared → left column shows Fireball; grimoire shows Fireball with **Prepared** badge. |
| **A2** | Focus Mage with three prepared spells → budget strip shows correct sum / remaining / current MP. |
| **A3** | **Town, peace:** select Arcane Might in grimoire → **Prepare** succeeds when capacity allows; appears in left column; hotbar overflow lists it. |
| **A4** | **Town, peace:** select prepared spell → **Unprepare** removes from left column; grimoire unchanged. |
| **A5** | Prepare when `EquipCost > RemainingEquipCapacity` → disabled button + inline reason. |
| **A6** | **Dungeon:** open menu → lists readable; Prepare/Unprepare/Add to hotbar disabled; dungeon banner shown. |
| **A7** | **In combat (town or dungeon):** loadout edits blocked; combat banner shown. |
| **A8** | Click spell in either column → detail pane updates description + costs. |
| **A9** | Empty grimoire → grimoire empty state mentions **Mage Tutor** and **Arcane Vendor**. |
| **A10** | Human **None** focused → placeholder; Mage body hidden. |
| **A11** | **`K` / Esc / F1–F5** unchanged from parent racial menu doc. |
| **A12** | Opening menu does not consume a turn. |
| **A13** | Prepared spell selected → **Add to hotbar** fills first empty main slot; second click disabled with “Already on hotbar.” |
| **A14** | Non-Mage Human classes cannot equip from menu even if `HumanMageSpellsRuntime` present on prefab (service rejects). |

---

## 15. Implementation phases

| Phase | Scope |
|-------|--------|
| **v0 (this doc)** | Requirements + mock; `HumanMageSpellBodyView` + view-model; router; edit gating; detail pane Prepare/Unprepare/Add to hotbar |
| **v0.1** | Keyboard navigation between rows; filter (prepared only / not prepared); Mage-specific combat deny copy |
| **v1** | Drag reorder prepared list (cosmetic only); per-spell custom icons on `MageSpellDefinition` |

---

## 16. Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | §14 v1 — add Human Mage body link; mark **Done** when shipped |
| [Human Mage — Spells & spellbooks](Human-Mage-Spells-And-Spellbooks-Requirements.md) | §12 UI — replace “later / v0.1” with link here; reconcile auto-prepare on read (§11.3) |
| [Human — Class powers](Human-Class-Powers-Requirements.md) | Mage loadout UI pointer |
| [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) | Human Mage spell assign prerequisites (prepared only) |

---

## 17. Document history

| Date | Change |
|------|--------|
| 2026-06-05 | Initial draft — two-column grimoire/prepared sheet, town edit / dungeon view-only, detail pane, hotbar add, visual mock; aligned with Dragonian menu pattern and Human Mage spell economy. |
