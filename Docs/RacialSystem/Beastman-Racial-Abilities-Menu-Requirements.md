# Beastman — Racial abilities menu (Soul Beast bond)

**Purpose:** Specify the **Beastman body** of the shared [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (`K`): a **read-only reference sheet** for the focused Beastman’s **Soul Beast contract** — empty when unbonded; when bonded, a **bond summary** plus a **scrollable ability list** (passives and actives) with **icons** and **descriptions** for every payload currently granted by cumulative levels **1 … soulBeastLevel**.

**Status:** Implemented (v0).

**Visual mock:** [`Docs/RacialSystem/beastman-racial-abilities-menu-mock.png`](beastman-racial-abilities-menu-mock.png) (bonded state — companion to §8).

**Depends on:** [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (shell, **`K`**, party strip, modal rules), [Beastman — Soul Beast](Beastman-Soul-Beast-Requirements.md) (`SoulBeastDefinition`, `SoulBeastLevelData`, `BeastmanSoulBeastRuntime`, cumulative payloads), [Beastman — contract ritual & leveling](Beastman-Soul-Beast-Contract-And-Leveling-Requirements.md) (ritual circle, Beast Blood, level cap), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (Soul Beast actives assignables — v0.1).

**Related:** [Barbarian Spirit Imprint menu](../UI/Racial-Abilities-Menu-Requirements.md) (read-only timeline reference), [Elf — racial abilities menu](Elf-Racial-Abilities-Menu-Requirements.md) (read-only roster cards), [Tiefling — racial abilities menu](Tiefling-Racial-Abilities-Menu-Requirements.md) (equipment-style slot grid + detail pane).

**Explicitly out of scope (v0):** Forming contracts, leveling with Beast Blood, ritual offerings, or assigning hotbar slots from this menu; replace / respec Soul Beast; showing ritual weights or Beast Blood shop prices; multiple Soul Beasts per Beastman; gamepad layout; persisting last-focused party member across sessions; illustrated 3D beast art; comparing unowned Soul Beasts from the registry catalog.

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Beastman body mounts when focused member is `Race.Beastman` with `RacialSubsystemKind.BeastmanSoulBeast`. |
| **L2** | **Read-only reference** — same discipline as Barbarian / Elf / Tiefling bodies. Banner points player to **Soul Beast Ritual Circle** (contract) and **Beast Blood Merchant** (leveling). |
| **L3** | **Unbonded state:** body is **blank/minimal** — centered empty emblem, title, and ritual guidance copy only. **No** ability rows, stats, or beast portrait. |
| **L4** | **Bonded state:** show **one** bond header (beast identity + level/cap) and a **vertical ability list** below — not a multi-beast roster (at most one contract). |
| **L5** | **Ability list contents:** cumulative **passives** and **actives** from level rows **1 … soulBeastLevel**; each row has **icon**, **name**, **description**, and **source level** subtitle. Stats/resistances appear in a compact **bond summary** block, not as ability rows. |
| **L6** | **Empty ability list:** when bonded but no passives/actives authored yet (v0 sample beasts), show bond summary + muted line *“No special abilities yet — stat bonuses only.”* |
| **L7** | Menu refresh on open and when focused member changes — not every frame. |
| **L8** | **Aesthetic:** `RacialUiTheme` dark glass; Beastman section accent **forest green / amber** (distinct from Barbarian gold, Elf teal, Tiefling ember). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Bond at a glance** — Player sees whether the focused Beastman has a Soul Beast, which beast, and current level/cap. |
| **G2** | **Ability reference** — Player reads every passive and active currently granted by the bond, with icons and full descriptions. |
| **G3** | **Barbarian / Elf / Tiefling parity** — Information-only; town gates own progression. |
| **G4** | **Unbonded clarity** — Empty state is intentionally sparse; ritual circle named as the path to a contract. |
| **G5** | **No duplicate systems** — Menu does not replace ritual dialog, Beast Blood Use, or hotbar assign/use. |
| **G6** | **Cumulative honesty** — List reflects **applied** payloads (levels 1…L), matching `BeastmanSoulBeastRuntime` + `RacialProgressionPayloadApplicator`. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Beastman body** | Race-specific panel in `RacialAbilitiesUI` when focused member is Beastman with Soul Beast subsystem. |
| **Unbonded state** | `BeastmanSoulBeastRuntime.IsBonded == false` — blank body (§8.2). |
| **Bonded state** | `soulBeastId` set — header + ability list (§8.3). |
| **Bond summary** | Read-only block: beast name, type, description, level, cap, cumulative stats/resistances. |
| **Ability row** | One passive or active from cumulative level payloads; icon + title + description + level tag. |
| **Level tag** | Muted subtitle e.g. `Level 2 · Passive` or `Level 3 · Active`. |

---

## 3. Screen responsibilities (locked)

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Racial menu — Beastman body** | Read bond summary; browse ability list | Contract, level, use Beast Blood, assign hotbar |
| **Soul Beast Ritual Circle** | Perform ritual → maybe bond | Replace full reference sheet |
| **Beast Blood (inventory Use)** | Raise `soulBeastLevel` | Show full ability reference |
| **Ability hotbar** | Assign and use Soul Beast actives (v0.1+) | Show passive reference |

**Read-only banner (required intent):**

> View only — form a contract at the **Soul Beast Ritual Circle**; deepen the bond with **Beast Blood** from the merchant in town.

No buttons; no teleport links.

---

## 4. Router integration

Extends parent [§5.3 router](../UI/Racial-Abilities-Menu-Requirements.md):

```
RacialAbilitiesUI.RefreshBodyForFocusedMember()
  → …
  Race.Beastman + BeastmanSoulBeast → BeastmanSoulBeastBodyView
  (else default placeholder)
```

| Condition | Body |
|-----------|------|
| `Race.Beastman` + `BeastmanSoulBeast` + `BeastmanSoulBeastRuntime` | **Soul Beast bond sheet** (this doc) |
| `Race.Beastman` but no runtime / wrong subsystem | Default placeholder with Beastman-specific copy |
| Not Beastman | Default placeholder (unchanged) |

---

## 5. Data sources

| UI region | Runtime / data |
|-----------|----------------|
| **Bond state** | `BeastmanSoulBeastRuntime.IsBonded`, `SoulBeastId`, `SoulBeastLevel` |
| **Beast definition** | `SoulBeastRegistryService.TryGetDefinition(soulBeastId, …)` |
| **Level cap** | `SoulBeastProgressionLogic.GetEffectiveLevelCap(contractorStats, beastDef)` |
| **Cumulative stats / resistances** | Sum of `SoulBeastLevelData` rows **1 … soulBeastLevel** |
| **Ability rows** | Flatten `passiveEffects` and `activeAbilities` from rows **1 … soulBeastLevel** (preserve level index for tag) |
| **Icons (v0)** | Passive: generic paw/totem emblem; Active: `AbilityAction` icon if present, else generic active glyph |
| **Future** | Optional `SoulBeastDefinition.icon` for bond header portrait |

**Do not** enumerate uncontracted beasts from `SoulBeastRegistry` — menu reflects **current bond only**.

---

## 6. Bond summary block (bonded only)

Pinned **above** the ability list (not scrollable with rows).

| Field | Source | Format |
|-------|--------|--------|
| **Beast portrait** | Future `SoulBeastDefinition.icon`; v0 fallback emblem | 64–96px left of title row |
| **Title** | `displayName` (fallback `soulBeastId`) | Bold, large |
| **Subtitle** | `{SoulBeastType} · Level {L} / Cap {C}` | Muted |
| **Description** | `SoulBeastDefinition.description` | Body text, 1–3 lines |
| **STATS** | Cumulative `statModifiers` | Compact bullets: `+N {Attribute}`; omit if empty |
| **RESISTANCES** | Cumulative `resistanceModifiers` | Bullet per type; omit if empty |
| **Progress hint** | When `L < C` | *“Use Beast Blood to deepen the bond.”* |
| **Capped** | When `L >= C` | *“Bond at maximum for your level.”* |

---

## 7. Ability list (bonded only)

### 7.1 — Row inventory

Build rows in **level order**, then **passives before actives** within each level:

| Row kind | Icon | Title | Body | Level tag |
|----------|------|-------|------|-----------|
| **Passive** | Passive emblem or `PassiveEffect` icon | `passiveName` | `effectDescription` | `Level {n} · Passive` |
| **Active** | Ability icon or active glyph | `abilityName` | ability description + SP/cooldown meta | `Level {n} · Active` |

**Active footnote (last active row or section footer):** *Assign on the ability hotbar to use in combat.*

### 7.2 — Dedup policy (v0)

| Case | Rule |
|------|------|
| Same passive name on multiple levels | **Separate rows** (level tag distinguishes) |
| Same active asset on multiple levels | **Separate rows** if listed on multiple level rows; hotbar dedup unchanged |
| No passives/actives at current level | Show §L6 muted empty-abilities line |

### 7.3 — Interaction

| Input | Behavior |
|-------|----------|
| **Scroll** | Ability list scrolls vertically; bond summary + banner pinned |
| **Click ability row** | v0: no selection / detail pane (row is self-contained). v0.1 optional: expand long descriptions |
| **Change focused party member** | Rebuild entire body; unbonded Beastman → blank state |

No drag-and-drop in v0.

---

## 8. Visual layout (mock — authoritative)

Uses same **full-screen racial shell** as parent doc (title, banner, party strip, footer). **Middle** replaces Barbarian timeline / Elf cards / Tiefling slot grid.

### 8.1 — Bonded layout (mock)

```
┌──────────────── FULL SCREEN ──────────────────────────────────────────────┐
│ RACIAL ABILITIES                                                            │
│ View only — form a contract at the Soul Beast Ritual Circle…               │  banner
│ [F1] [F2] [F3] [F4] … party strip                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│ SOUL BEAST BOND                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ [beast icon]  Ember Wolf                                                │ │
│ │               Enhancement · Level 3 / Cap 5                               │ │
│ │               A fiery wolf spirit that enhances the Beastman's body.    │ │
│ │               STATS · +3 Strength · +2 Constitution                     │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│ CURRENT ABILITIES                                              (scroll)     │
│ ┌────┐  Wolf's Endurance                                    Lv 2 · Passive │
│ │ 🐾 │  Your bond steadies your frame against blows.                         │
│ ├────┤                                                                      │
│ ┌────┐  Ember Rush                                          Lv 3 · Active  │
│ │ 🔥 │  Charge forward, scorching foes in your path. · SP 2                 │
│ │    │  Assign on the ability hotbar to use in combat.                      │
├─────────────────────────────────────────────────────────────────────────────┤
│ K — racial abilities · Esc — close · F1–F5 — focus member                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 8.2 — Unbonded layout (blank)

```
┌──────────────── FULL SCREEN ──────────────────────────────────────────────┐
│ RACIAL ABILITIES                                                            │
│ View only — form a contract at the Soul Beast Ritual Circle…               │
│ [F1] [F2] … party strip                                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│ SOUL BEAST BOND                                                             │
│                                                                             │
│                    ┌──────────────┐                                         │
│                    │  (empty      │   faint dashed circle / paw outline     │
│                    │   emblem)    │                                         │
│                    └──────────────┘                                         │
│                    No Soul Beast contract                                   │
│                                                                             │
│     Perform a ritual at the Soul Beast Ritual Circle in town                │
│     to attract a permanent Soul Beast companion.                            │
│                                                                             │
│                    (no ability list — remainder intentionally blank)        │
├─────────────────────────────────────────────────────────────────────────────┤
│ K — racial abilities · Esc — close · F1–F5 — focus member                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 8.3 — Layout tokens

| Token | Value | Notes |
|-------|-------|-------|
| Panel background | `(0.08, 0.085, 0.095, 0.96)` | Match inventory / racial |
| Outer padding | `12px` | |
| Section label | `SOUL BEAST BOND` · 20px bold | Green-amber accent optional |
| Bond summary card | Rounded rect `(0.12, 0.14, 0.11, 0.95)` | Subtle green left accent bar |
| Ability row height | min **72px** | Icon 48–56px left, text block right |
| Ability row separator | 1px `(0.25, 0.28, 0.22, 0.4)` | |
| Empty emblem | Dashed circle α ~0.35 | Centered in flex region |
| Typography | Title 28 / banner 17 / bond title 24 / ability title 19 / body 17 / footer 15 | TMP |

---

## 9. View-model builder

Suggested API (implementation hint):

```
BeastmanSoulBeastBodyViewModel.Build(BaseActor beastman)
  → runtime = beastman.GetComponent<BeastmanSoulBeastRuntime>()
  → if !runtime.IsBonded → UnbondedModel (empty copy only)
  → beast = registry lookup(runtime.SoulBeastId)
  → cap = SoulBeastProgressionLogic.GetEffectiveLevelCap(stats, beast)
  → summary = BuildBondSummary(beast, runtime.SoulBeastLevel, cap)
  → abilities = FlattenAbilities(beast, 1..runtime.SoulBeastLevel)
  → return { IsBonded, Summary, AbilityRows[], EmptyAbilitiesHint }
```

Unit tests cover: unbonded blank model, bonded stats aggregation, ability flatten order, empty passives/actives hint, cap line copy.

---

## 10. Integration

| System | Rule |
|--------|------|
| **RacialAbilitiesUI** | Router mounts `BeastmanSoulBeastBodyView`; refresh on open / focus change. |
| **BeastmanSoulBeastRuntime** | Read-only; no mutation from menu. |
| **Ritual / Beast Blood** | After bond or level change, next menu open reflects new state. |
| **HotbarAssignabilityService** | v0.1: append Soul Beast actives; menu lists same abilities for reference. |
| **AbilityHotbarUI** | Refresh after bond/level changes (existing hooks). |

---

## 11. Acceptance criteria

| ID | Test |
|----|------|
| **A1** | Focus unbonded Beastman → blank body; centered empty emblem + ritual copy; **no** ability rows. |
| **A2** | Focus bonded Beastman (Ember Wolf L3) → bond summary shows name, type, level/cap, cumulative stats. |
| **A3** | Ability list shows each passive/active from levels 1…3 with icon, name, description, level tag. |
| **A4** | Bonded beast with stats-only levels (v0 sample) → summary stats visible + *“No special abilities yet…”* line. |
| **A5** | No ritual, Beast Blood, or hotbar assign buttons; banner references ritual circle + merchant only. |
| **A6** | Non-Beastman focused member → default placeholder; Beastman body hidden. |
| **A7** | After ritual bond or Beast Blood level-up, reopen menu → updated level and abilities without scene reload. |
| **A8** | **`K` / Esc / F1–F5** behavior unchanged from parent racial menu doc. |
| **A9** | Opening menu does not consume a turn; blocked under gameplay modal gate. |

---

## 12. Implementation phases

| Phase | Scope |
|-------|-------|
| **v0 (this doc)** | **Done** — `BeastmanSoulBeastBodyViewModel`, `BeastmanSoulBeastBodyView`, router in `RacialAbilitiesUI` |
| **v0.1** | Sample passives/actives on Ember Wolf / Stone Tortoise levels 2–3; `SoulBeastDefinition.icon`; hotbar wiring |
| **v1** | Optional row selection → expanded detail; beast type color accents |

---

## 13. Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | §6 Beastman row → link here |
| [Beastman — contract ritual & leveling](Beastman-Soul-Beast-Contract-And-Leveling-Requirements.md) | Replace “future Beastman body” with link here |
| [Beastman — Soul Beast](Beastman-Soul-Beast-Requirements.md) | Player-facing inspect UI cross-link |
| [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) | Soul Beast active reference vs execution |

---

## 14. Document history

| Date | Change |
|------|--------|
| 2026-06-13 | v0 implementation — unbonded blank state, bonded summary + ability list, router in `RacialAbilitiesUI`. |
| 2026-06-13 | Initial draft — unbonded blank state, bonded ability list with icons, read-only ritual/blood parity, visual mock. |
