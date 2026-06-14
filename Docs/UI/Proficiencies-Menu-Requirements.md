# Proficiencies menu — Requirements

**Purpose:** Specify a **party-scoped reference menu** where the player inspects **each member’s practiced proficiencies** — current level, progress toward the next level, folk **aptitude**, and **training cap** — without editing or buying levels. Mirrors the [racial abilities menu](Racial-Abilities-Menu-Requirements.md) shell (full-screen, party strip, read-only).

**Status:** Implemented (`ProficienciesUI`, `ProficiencyListBodyViewModel`; **`P`** hotkey).

**Visual mock:** [`Docs/UI/proficiencies-menu-mock.png`](proficiencies-menu-mock.png) (companion to §5).

**Depends on:** [Proficiencies — system](../Progression/Proficiencies-Requirements.md) (`ProficiencyRuntime`, `ProficiencyRules`, `ProficiencyEligibility`, `ProficiencyAptitudeService`), `PartyManager`, `BaseActor`, `CharacterStats`, `InputHandler` / `GameControls`, `GameplayModalGate`, [Racial abilities menu](Racial-Abilities-Menu-Requirements.md) (shell pattern, `RacialUiTheme`), [Character equipment menu](Character-Equipment-Menu-Requirements.md) (detail pane pattern), [Party control HUD](Party-Control-HUD-Requirements.md) (portrait catalog, F-key semantics).

**Related:** [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) (character level drives training cap), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (party strip).

**Explicitly out of scope (v0):** Granting or spending proficiency XP from this menu; respec; trainer NPC integration; comparing two party members side-by-side; full **`ProficiencyDefinition`** ScriptableObject catalog (v0 uses enum + static copy); gamepad layout; persisting last-focused member across sessions; animated level-up fanfare; editing aptitudes; showing per-action XP log history.

---

## Locked decisions (proposed — confirm before implementation)

| # | Decision |
|---|----------|
| **L1** | **Hotkey:** **`P`** toggles menu (`ToggleProficiencies`). **`Esc`** closes. |
| **L2** | **Party browse:** Same carousel as racial / character equipment — portrait strip + **F1–F5** while open; **focused member ≠ active member**. |
| **L3** | **Read-only v0:** Menu **displays** levels and progress only; training happens in combat / field use ([proficiency dispatcher](../Progression/Proficiencies-Requirements.md) §7). |
| **L4** | **Layout:** Full-screen overlay; **scrollable categorized list** in the middle band; **detail pane pinned to bottom** (~28–32% height) on row select. |
| **L5** | **Visibility:** Show **all catalog kinds** for the focused member. **Ineligible** rows appear **muted** with **`N/A`** badge and eligibility reason in detail pane — not hidden (§6.3). |
| **L6** | **Training cap UX:** Always show **`level / trainingCap`** for trainable rows; if **stored level > trainingCap**, show stored level prominently and a **“training paused”** note (character level must rise — §7.5.3). |
| **L7** | **Aesthetic:** Dark glass chrome consistent with inventory, racial menu, character equipment (`RacialUiTheme` palette). |
| **L8** | **Modal exclusivity (v0):** Opening inventory, quest journal, racial menu, or character equipment **closes** this menu and vice versa. |
| **L9** | **Default selection on open:** First **non-zero** proficiency in sort order, else first **eligible** row, else **`Fighting`**. |
| **L10** | **Display names (v0):** Format `ProficiencyKind` enum (`Weapon_Sword` → “Sword”, `Damage_Fire` → “Fire damage”, `FireMagic` → “Fire Magic”) via `ProficiencyDisplayNames` until `ProficiencyDefinition` assets ship (§13). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **At-a-glance mastery** — Player sees what each member has trained without parsing debug logs. |
| **G2** | **Per-member sheet** — Party strip swaps the entire proficiency list to that actor’s `ProficiencyRuntime`. |
| **G3** | **Cap clarity** — Training cap (`min(27, 2 × characterLevel)`) and banked pxp at cap are understandable at a glance. |
| **G4** | **Folk identity** — Aptitude (`−4 … +4`) visible per row; explains why some skills climb faster. |
| **G5** | **Gate clarity** — Ineligible schools show **why** (Human Mage only, Dragonian only, …) — not silent zeros. |
| **G6** | **Benefit preview** — Detail pane summarizes combat benefit from [Proficiencies §8](../Progression/Proficiencies-Requirements.md) (static copy keyed by kind/category). |
| **G7** | **No progression duplication** — Menu does not replace combat training or future trainer NPCs. |
| **G8** | **Modal-safe** — Open / close / change focus consumes **no** turn; respects `GameplayModalGate`. |
| **G9** | **Extensible catalog** — ViewModel reads runtime data; later **`ProficiencyDefinition`** overrides display name / description without rewriting shell. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Proficiencies menu** | Full-screen overlay toggled by **`P`**. |
| **Focused member** | Party member whose proficiencies are listed; strip / F-keys while open. |
| **Active member** | `PartyManager.GetActiveMember()` — map control; independent unless synced later. |
| **Stored level** | Persisted `ProficiencyRuntime` level (`0 … 27`); **never lowered** when character level drops. |
| **Training cap** | `ProficiencyRules.GetTrainingCap(characterLevel)` — max level that **may increase** today. |
| **Banked pxp** | Progress toward next level while **at training cap**; applies when cap rises (§7.5). |
| **Eligible row** | `ProficiencyEligibility.CanTrain(stats, kind) == true`. |
| **N/A row** | Ineligible — fixed at 0, no XP; shown muted with badge. |
| **Proficiency row** | One list entry: name, level, cap, aptitude, progress bar. |
| **Detail pane** | Bottom pinned region; content driven by selected row. |

---

## 3. Screen responsibilities (locked)

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Proficiencies menu (`P`)** | Browse party proficiencies; read level, cap, pxp, aptitude, benefit summary | Add levels, spend pxp, respec, or train at NPCs |
| **Combat / abilities** | Earn pxp via qualifying actions | Replace this reference sheet |
| **Character equipment (`C`)** | Inspect gear and essences | Show full proficiency catalog (by design — separate menu) |
| **Racial abilities (`K`)** | Inspect folk-specific progression | Show mundane / DCSS-style proficiencies |

---

## 4. Input & hotkey

### 4.1 — Toggle

| Action | Binding | Notes |
|--------|---------|-------|
| **ToggleProficiencies** | **`P`** | Add to `GameControls.inputactions`; wire in `InputHandler` like `ToggleRacialAbilities`. |
| **Close** | **`P`** (toggle) or **`Esc`** | |

**Footer copy (v0):** `P — proficiencies · Esc — close · F1–F5 — focus member`

### 4.2 — While menu open

| Input | Behavior |
|-------|----------|
| **F1–F5** | Focus party member at index. |
| **Click portrait** | Focus that member. |
| **Click proficiency row** | Select row; refresh detail pane. |
| **Scroll wheel / scrollbar** | Scroll list only (header, strip, detail pane pinned). |
| **Esc** | Close menu. |

**Does not** change `PartyManager` active member or issue floor commands.

### 4.3 — Turn & modal semantics

| Rule | Detail |
|------|--------|
| **Turn cost** | Open / close / change focused member → **no** `TurnManager.OnPlayerActionComplete`. |
| **Modal gate** | Do not open when `GameplayModalGate.BlocksFloorGameplay`. |
| **Coexistence** | **v0: mutually exclusive** with inventory, quest journal, racial menu, character equipment. |

---

## 5. Visual layout (mock — authoritative)

Full-screen shell (anchor stretch 0→1, panel α ≈ 0.96). Typography via TMP + `TMP_Settings.defaultFontAsset` (same as racial v0.1).

```
┌──────────────── FULL SCREEN ──────────────────────────────────────────────┐
│ PROFICIENCIES                                                               │  title 28
│ Practice in the field — levels rise from use, not party kill XP.            │  banner 17 gold
│ [F1 portrait●] [F2 portrait] [F3 portrait] …                                │  strip 108
│ Aldric · Human Knight · Character level 10 · Training cap 20 · Max 27       │  subtitle 17
├─────────────────────────────────────────────────────────────────────────────┤
│ [All] [Combat] [Weapons] [Damage] [Magic] [Divine] [Other]                  │  filter chips
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌ scroll list ───────────────────────────────────────────────────────────┐ │
│ │ GENERAL COMBAT                                                          │ │
│ │  Fighting          14 / 20   [███████░░░]  +0 apt    412 / 580 pxp      │ │
│ │  Armour             8 / 20   [████░░░░░░]  +2 apt    120 / 340 pxp      │ │
│ │  Dodging            3 / 20   [██░░░░░░░░]  +0 apt     45 /  98 pxp      │ │
│ │ WEAPONS                                                                 │ │
│ │  Sword             12 / 20   [██████░░░░]  +0 apt    …                  │ │
│ │  Mace               6 / 20   [███░░░░░░░]  +2 apt    …                  │  ← Dwarf would show +2
│ │  Bow                0 / 20   [░░░░░░░░░░]  −2 apt    —                  │ │
│ │ DAMAGE TYPES                                                            │ │
│ │  Slash             11 / 20   …                                          │ │
│ │  Fire               4 / 20   …        (trains from flaming sword)       │ │
│ │ ARCANE                                                                  │ │
│ │  Fire Magic         N/A      —         Only a Human Mage can train…       │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│ DETAILS · Fighting                                                          │  ~30% height
│ Level 14 (stored) · Trainable to 20 · Absolute max 27                       │
│ Progress: 412 / 580 pxp to level 15 · Aptitude +0 (normal learning speed)   │
│ Benefits: +47% melee/ranged damage modifier from Fighting (§8.1).           │
│ Trained by: successful weapon hits (50% pxp on secondary award).            │
├─────────────────────────────────────────────────────────────────────────────┤
│ P — proficiencies · Esc — close · F1–F5 — focus member                        │
└─────────────────────────────────────────────────────────────────────────────┘
```

See [`proficiencies-menu-mock.png`](proficiencies-menu-mock.png) for color / spacing reference.

### 5.1 — Layout tokens

| Token | Value | Notes |
|-------|-------|-------|
| Panel background | `(0.08, 0.085, 0.095, 0.96)` | Match inventory / racial |
| Outer padding | `12px` | |
| Section spacing | `6px` | |
| Party chip | `96×108px`, portrait `56px` | Reuse `RacialAbilitiesPartyStripView` or shared strip |
| Focus border | Gold `(0.91, 0.77, 0.28)` | Party HUD `ActiveBorderColor` |
| Title / banner / subtitle / footer | **28 / 17 / 17 / 15** | Match racial v0.1 |
| Filter chip height | `28px` | Toggle category filter; **All** default |
| List row height | `36–40px` | Name left; level + bar center; aptitude right |
| Section header | **18px** bold, muted gold | `GENERAL COMBAT`, `WEAPONS`, … |
| Progress bar | `8px` tall, fill gold / track dark grey | Filled fraction = `pxp / xpToNext` when below cap |
| N/A row | α ≈ `0.45`, italic name | Badge **`N/A`** right-aligned |
| At-cap row | Gold outline on level text | Footnote “training paused until level N” |
| Detail pane min height | **~28–32%** | Scroll inside if benefit text long |

### 5.2 — Summary strip (under party portraits)

Single line built from focused member:

| Field | Source |
|-------|--------|
| **Name · race · class** | `CharacterStats` + display helpers |
| **Character level** | `stats.level` |
| **Training cap** | `ProficiencyRules.GetTrainingCap(stats.level)` |
| **Absolute max** | `ProficiencyRules.MaxLevel` (27) |

Optional second line when **any** stored level exceeds training cap:

> Some proficiencies are above today’s training cap — bonuses remain; new levels unlock when character level rises.

---

## 6. Proficiency list body

### 6.1 — Categories (v0 static map)

| Section header | `ProficiencyKind` members |
|----------------|---------------------------|
| **GENERAL COMBAT** | `Fighting`, `Throwing`, `Armour`, `Dodging`, `Shields` |
| **WEAPONS** | `Weapon_Unarmed` … `Weapon_Polearm` (weapon enum order) |
| **DAMAGE TYPES** | `Damage_Blunt` … `Damage_Force` |
| **ARCANE** | `Spellcasting`, `FireMagic`, `IceMagic`, `AirMagic`, `EarthMagic`, `Conjurations`, `Hexes`, `Translocations`, `Alchemy` |
| **DIVINE** | `DivineMagic`, `Healing`, `Smite`, `Warding` |
| **OTHER** | `DraconicSpellcraft`, `Evocations`, `Invocations` |

Filter chips map 1:1 to sections; **All** shows every section header + rows.

### 6.2 — Row content

| Column | Eligible | Ineligible (`N/A`) |
|--------|----------|---------------------|
| **Name** | `ProficiencyDisplayNames.Get(kind)` | Same, muted |
| **Level** | `{storedLevel} / {trainingCap}` | **`N/A`** |
| **Progress bar** | Hidden if level ≥ trainingCap **or** level ≥ 27; else `pxp / GetXpToNextLevel(level, aptitude)` | Hidden |
| **Aptitude** | `{+N}` / `{−N}` / `+0` with color (green ≥ +1, red ≤ −1) | Hidden |
| **Pxp hint** | `{pxp} / {needed} pxp` when bar visible | — |

**Level display rules:**

1. **`storedLevel ≤ trainingCap`:** show `{storedLevel} / {trainingCap}`; bar toward next level if `storedLevel < 27`.
2. **`storedLevel > trainingCap`:** show **`{storedLevel}`** with `(cap {trainingCap})` suffix; bar hidden; tooltip / detail: *“Training paused — raise character level to train further.”*
3. **`storedLevel == 0` and never trained:** show `0 / {trainingCap}`; empty bar; no pxp hint.

**Sort within section:** descending stored level, then name.

### 6.3 — Ineligible rows

| Rule | Detail |
|------|--------|
| **Always listed** | Do not hide arcane/divine schools on Knights — teaches eligibility gates. |
| **Badge** | **`N/A`** on the level column. |
| **Detail pane** | `ProficiencyEligibility.GetIneligibilityReason(stats, kind)` |
| **Level source** | Always **0**; ignore stale runtime entries if any (dev assert). |

### 6.4 — Missing runtime

| Condition | Body |
|-----------|------|
| No `ProficiencyRuntime` on actor | `ProficiencyRuntime.EnsureOn` on first open (same as gameplay); list all zeros. |
| Actor not in party | Should not happen via strip; guard with empty state. |

---

## 7. Detail pane

Updates when player selects a row (keyboard focus deferred v0.1).

### 7.1 — Fields (eligible, training)

| Block | Content |
|-------|---------|
| **Title** | Display name |
| **Level line** | `Level {stored} (stored) · Trainable to {cap} · Absolute max 27` |
| **Progress** | `Progress: {pxp} / {needed} pxp to level {stored+1}` — omit if at cap or max |
| **Aptitude** | `Aptitude {±N} ({ProficiencyAptitudeFormatter.GetBlurb(aptitude)})` — e.g. “learns 2× faster” |
| **Benefits** | Static blurb from `ProficiencyBenefitFormatter.GetSummary(kind, storedLevel)` — cites §8 formulas in plain language |
| **Trained by** | One-line hint from catalog (e.g. “Successful sword hits while wielding a sword.”) |

### 7.2 — Fields (eligible, above training cap)

Same as §7.1 plus pinned callout:

> **Training paused** — character level {L} caps trainable proficiencies at {cap}. Stored level {stored} still applies to combat bonuses. Banked progress will apply when the cap rises.

Show **banked pxp** if `storedLevel == trainingCap` and `pxp > 0`.

### 7.3 — Fields (N/A)

| Block | Content |
|-------|---------|
| **Title** | Display name |
| **Status** | **Not available** for this character |
| **Reason** | Eligibility string |
| **Lore** | Optional static description (future `ProficiencyDefinition.description`) |

---

## 8. Data & architecture

### 8.1 — ViewModel

```
ProficiencyListBodyViewModel.Build(BaseActor actor):
  stats = actor.stats
  runtime = ProficiencyRuntime.EnsureOn(actor.gameObject)
  cap = ProficiencyRules.GetTrainingCap(stats.level)
  for each kind in CatalogOrder:
    row = new ProficiencyRowViewModel {
      kind,
      displayName = ProficiencyDisplayNames.Get(kind),
      category = ProficiencyCategories.Get(kind),
      eligible = ProficiencyEligibility.CanTrain(stats, kind),
      storedLevel = eligible ? runtime.GetLevel(kind) : 0,
      pxp = eligible ? runtime.GetPxp(kind) : 0,
      trainingCap = cap,
      aptitude = ProficiencyAptitudeService.GetAptitude(stats, kind),
      xpToNext = ProficiencyRules.GetXpToNextLevel(storedLevel, aptitude),
      ineligibilityReason = ProficiencyEligibility.GetIneligibilityReason(stats, kind),
      ...
    }
  return rows
```

**No mutation** in ViewModel — read-only projection.

### 8.2 — Components (proposed)

| Type | Responsibility |
|------|----------------|
| **`ProficienciesUI`** | Shell: bootstrap, toggle, party strip, filter chips, modal gate, mutual close |
| **`ProficienciesPartyStripView`** | Reuse racial strip or thin wrapper |
| **`ProficiencyListBodyView`** | Scroll list + section headers + row widgets |
| **`ProficiencyListBodyViewModel`** | Build row models from actor |
| **`ProficiencyDisplayNames`** | Enum → display string (v0) |
| **`ProficiencyCategories`** | Enum → section + filter chip |
| **`ProficiencyBenefitFormatter`** | Static benefit blurbs keyed by kind + level |
| **`ProficiencyAptitudeFormatter`** | “+2 aptitude → learns twice as fast” copy |

### 8.3 — Integration points

| System | Hook |
|--------|------|
| **`InputHandler`** | `OnToggleProficienciesPerformed` → `ProficienciesUI.TogglePanelFromGameplayInput()` |
| **`GameplayModalGate`** | Register `ProficienciesUI.BlocksGameplay` |
| **Other modals** | On open proficiencies: close inventory / journal / racial / character; reverse on their open |
| **Save / load** | No UI state; runtime already on actor blob |

### 8.4 — Future: `ProficiencyDefinition` catalog

When §13.1 assets ship:

- Replace / override `ProficiencyDisplayNames` and benefit blurbs from ScriptableObjects.
- Optional icon per kind in list + detail pane.
- Menu code unchanged — ViewModel reads definition registry first, falls back to enum formatters.

---

## 9. Copy & formatting helpers (v0)

### 9.1 — Display name samples

| `ProficiencyKind` | Display name |
|-------------------|--------------|
| `Fighting` | Fighting |
| `Weapon_Sword` | Sword |
| `Damage_Fire` | Fire damage |
| `FireMagic` | Fire Magic |
| `DraconicSpellcraft` | Draconic Spellcraft |
| `Spellcasting` | Spellcasting |

### 9.2 — Aptitude blurbs

| Aptitude | Player-facing blurb |
|----------|---------------------|
| **+4 … +1** | learns faster (XP cost × multiplier from §6) |
| **0** | normal learning speed |
| **−1 … −4** | learns slower |

Use exact multiplier in tooltip only if `?` expanded help added later.

### 9.3 — Benefit summary (examples)

| Kind | Template (level L) |
|------|---------------------|
| `Fighting` | `+{pct}% melee and ranged damage (Fighting modifier).` where `pct ≈ Round(L/30*100)` |
| `Weapon_Bow` | `+{pct}% bow damage; +{hit} accuracy (future).` |
| `FireMagic` | `+{pct}% power on Fire Magic spells.` |
| `Armour` | `Armour training reduces penalties and improves mitigation (see §8.2).` |

Keep numbers **derived from live level** in detail pane; list rows stay compact.

---

## 10. Acceptance criteria (v0)

1. **`P`** opens/closes full-screen proficiencies menu during non-modal floor play.
2. Party strip + **F1–F5** switches focused member; list and summary strip refresh.
3. Every **`ProficiencyKind`** (except `None`) appears in exactly one section.
4. Eligible rows show stored level, training cap, pxp progress, and aptitude.
5. Ineligible rows show **`N/A`** and correct reason in detail pane (Knight + Fire Magic case).
6. Row with **stored level > training cap** shows training-paused messaging.
7. Selecting a row updates bottom detail pane with benefits + trained-by hint.
8. Menu is read-only — no buttons that mutate `ProficiencyRuntime`.
9. Opening inventory / **`K`** / **`C`** / **`J`** closes proficiencies menu.
10. Unit tests: `ProficiencyListBodyViewModelTests` for Knight vs Mage row eligibility, cap display, above-cap pause state.

---

## 11. Implementation order

1. **`ProficiencyDisplayNames` + `ProficiencyCategories` + formatters** (pure C#, testable)
2. **`ProficiencyListBodyViewModel` + tests**
3. **`ProficienciesUI` shell** (clone racial bootstrap pattern)
4. **`ProficiencyListBodyView` + detail pane**
5. **`GameControls` + `InputHandler` + modal mutual close**
6. Link from [Proficiencies — Requirements](../Progression/Proficiencies-Requirements.md) § header (replace “Future: character sheet tab”)

---

## 12. Open questions

| # | Question | Default if unanswered |
|---|----------|------------------------|
| **Q1** | Hotkey **`P`** vs tab on **`C`** character sheet? | Standalone **`P`** (this doc) |
| **Q2** | Hide **level-0** rows behind “Show untrained” toggle? | **Show all** in v0 |
| **Q3** | Show **numeric combat preview** (exact damage mod) vs qualitative blurbs? | **Derived numbers** in detail only |
| **Q4** | Per-kind icons in v0? | **No** — text only until catalog assets |

---

## 13. Cross-links

- [Proficiencies — system](../Progression/Proficiencies-Requirements.md)
- [Racial abilities menu](Racial-Abilities-Menu-Requirements.md)
- [Character equipment menu](Character-Equipment-Menu-Requirements.md)
