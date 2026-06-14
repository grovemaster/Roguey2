# Human Knight — Racial abilities menu (Skill tree & auras)

**Purpose:** Specify the **Human Knight body** of the shared [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (`K`): a **branch-grouped skill tree sheet** for the focused **Human Knight** — each skill shows **tree rank**, **proficiency progress toward the next rank** (actives, from combat use), **field mastery**, **icon**, and **active aura state** — with a **pinned detail pane** below and **town-only skill point spending** (instant D2-style rank-ups). **Proficiency pxp** and **mastery** are **view-only** everywhere (earned by use in combat — [Knight auras doc](Human-Knight-Auras-And-Skill-Tree-Requirements.md)).

**Status:** Implemented (v0 — skill tree sheet, town spend, view-only dungeon/combat).

**Visual mock:** [`Docs/RacialSystem/human-knight-racial-abilities-menu-mock.png`](human-knight-racial-abilities-menu-mock.png) (town edit mode — companion to §8).

**Depends on:** [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (shell, **`K`**, party strip, modal rules), [Human Knight — Auras & skill tree](Human-Knight-Auras-And-Skill-Tree-Requirements.md) (tree ranks, mastery, auras, stance exclusivity), [Human — Class powers](Human-Class-Powers-Requirements.md) (`HumanClass.Knight`, Soul Power + essences, `HumanClassSkillTreeRuntime`), [Proficiencies menu](../UI/Proficiencies-Menu-Requirements.md) (`P` — optional cross-link for mastery-only list), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md), `SafeZonePolicyService`, `HumanClassSkillTreeDefinition`.

**Related:** [Human Mage — racial abilities menu](Human-Mage-Racial-Abilities-Menu-Requirements.md) (two-column loadout analogue — Mage prepares spells, Knight **spends skill points**), [Barbarian Spirit Imprint](../UI/Racial-Abilities-Menu-Requirements.md) §7 (vertical timeline + node cards — visual reference for tree rows), [Dragonian — racial abilities menu](Dragonian-Racial-Abilities-Menu-Requirements.md) (town edit / dungeon view-only pattern).

**Explicitly out of scope (v0):** **Training events** that grant skill points (NPC pipeline later — menu only **spends** existing points); **respec** of spent ranks; **mastery editing** or purchase; **casting** skills from the menu; interactive pan/zoom skill tree graph; gamepad layout; persisting last-focused party member; Priest body; Human **None / Mage** Knight tree view; drag-and-drop hotbar editor; comparing two Knights side-by-side.

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Knight body mounts when focused member is `Race.Human`, `humanClass == HumanClass.Knight`, `RacialSubsystemKind.HumanSpecialization`, and `HumanClassSkillTreeRuntime` (Knight tree) present. |
| **L2** | **Single scrollable tree list** grouped by **branch** (Bulwark / Valor / Command + General passives) — not a free-pan graph in v0. |
| **L3** | Each skill row shows **icon**, **title**, **tree rank** (`current / maxRanks`), **rank proficiency bar** (actives only, when rank &lt; max), **mastery** (`level / trainingCap`), and status badges (**ACTIVE** stance, **LOCKED**, **MAX**). |
| **L4** | **Detail pane** pinned below (~28–32% height); populated by **clicking any skill row**. |
| **L5** | **Dungeon / non–safe zone / combat:** menu is **view-only** — read ranks, mastery, descriptions; **no** skill point spend. |
| **L6** | **Town safe zone + not in combat:** player may **spend 1 skill point** on selected node via detail pane when gates pass (`TrySpendPoint`). |
| **L7** | **Proficiency pxp** and **mastery** are **never** edited from this menu — display + progress bars only; training happens in combat ([Knight doc §7.2–§7.5](Human-Knight-Auras-And-Skill-Tree-Requirements.md)). |
| **L8** | **Summary strip** above tree: **Soul Power** (current / max), **unspent skill points**, **active stance aura** name (or *None*). |
| **L9** | **Hotbar:** detail pane **Add to hotbar** for **active** skills with **tree rank ≥ 1** (same pattern as Mage prepared spells). |
| **L10** | Menu refresh on **open**, **focus change**, and after **successful point spend / hotbar add** — not every frame. |
| **L11** | **Aesthetic:** `RacialUiTheme` dark glass; Knight accent **steel gold / warm amber** (distinct from Mage violet, Dragonian crimson, Barbarian totem gold). |
| **L12** | **Terminology:** **“Rank”** = tree rank; **“Proficiency”** (or **rank pxp**) = combat progress toward **next rank** on actives; **“Mastery”** = field practice level (`KnightSkillMasteryRuntime`). Do not conflate the three. |
| **L13** | Link footnote in detail pane: *Full mastery list also on **`P`** proficiencies menu* (when Knight skills section ships). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Build clarity** — Player sees every Knight skill, its **invested ranks**, and **practice mastery** in one place. |
| **G2** | **Town planning** — Player spends **skill points** at a drill master / in town before delving — **or** earns **rank proficiency pxp** from active use underground (auto rank-up). |
| **G3** | **Three-layer honesty** — UI distinguishes **Rank** (current tier), **Proficiency** (combat progress toward next rank on actives), and **Mastery** (field practice). |
| **G4** | **Aura state** — Player sees which **stance aura** is **ACTIVE** without opening the hotbar. |
| **G5** | **Reference in dungeon** — Open **`K`** underground to read skill descriptions and current ranks; cannot spend points. |
| **G6** | **Hotbar bridge** — **Add to hotbar** for unlocked actives from detail pane. |
| **G7** | **Multi-Knight party** — Party strip switches body per focused Human Knight. |
| **G8** | **Gate clarity** — Locked nodes show **why** (level, parent rank, exclusivity) in detail pane. |
| **G9** | **STBGB tone** — Banner points to **training NPCs** (later) for new points; menu spends points only. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Human Knight body** | Race-specific panel in `RacialAbilitiesUI` when focused member is a committed Human Knight. |
| **Tree rank** | Current tier on a node (`0 … maxRanks`). Raised by **(1)** **`TrySpendPoint`** (skill point in town) or **(2)** **proficiency pxp** from **active** use in combat (auto +1 at threshold — [Knight doc §7.2](Human-Knight-Auras-And-Skill-Tree-Requirements.md)). |
| **Rank proficiency pxp** | Per-skill progress toward **next tree rank** on **actives** (`rankPxp` on `KnightSkillMasteryRuntime`). Not shown for passives. |
| **Mastery** | Practice level on `KnightSkillMasteryRuntime` (`0 … 27`, training cap). |
| **Training cap (mastery)** | `min(27, 2 × characterLevel)` — max mastery that **may increase** today. |
| **Skill point** | Unspent currency shown in summary strip; decrements on successful spend. |
| **Active stance** | At most one `AuraStance` skill currently toggled on (`KnightAuraStateRuntime`). |
| **Edit mode** | Town safe zone **and** not in combat — allows **Spend skill point**. |
| **View-only mode** | Dungeon, combat, or non–safe zone — lists + detail readable only. |
| **Detail pane** | Bottom region: icon, stats, rank/mastery bars, action buttons. |
| **Branch** | Designer grouping: **General**, **Bulwark**, **Valor**, **Command** (from [Knight doc §5.1](Human-Knight-Auras-And-Skill-Tree-Requirements.md)). |

**Contrast with Human Mage (do not conflate copy):**

| Human Knight | Human Mage |
|--------------|------------|
| Skill **points** **or combat proficiency pxp** → tree **rank** | **Magic Power budget** → prepare spells |
| **Mastery** + **rank proficiency** from combat use | No spell mastery track |
| **Soul Power** (cast + aura upkeep) | **Magic Power** (prepare + cast) |
| Branch-grouped **skill tree** | Two columns: prepared + grimoire |
| Spend points in town | Equip / unequip in town |

---

## 3. Screen responsibilities (locked)

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Racial menu — Knight body (edit mode)** | View full tree; read rank + proficiency + mastery; **spend skill point** on eligible node; **add active to hotbar** | Grant pxp; respec ranks; cast skills; earn skill points |
| **Racial menu — Knight body (view-only)** | View tree + detail | Spend points, hotbar add (optional: hotbar add disabled in dungeon) |
| **Training NPC (later)** | Grant skill points or unlock nodes | Replace this menu’s reference role |
| **Proficiencies menu (`P`)** | Read rank + mastery pxp list (Knight section) | Spend tree points |
| **Ability hotbar** | Assign + use actives in combat | Show full tree prerequisites |

### 3.1 — Banner copy (required)

| Mode | Banner |
|------|--------|
| **Edit mode (town, peace)** | *Spend skill points on your Knight techniques here. New points come from **training** with masters in the field — visit a **drill instructor** when available.* |
| **View-only (dungeon)** | *View only — you can only spend skill points in town.* |
| **View-only (combat)** | *View only — finish combat before adjusting your skill tree.* |

No teleport to NPCs; names are progression direction only.

---

## 4. Router integration

Extends parent [§5.3 router](../UI/Racial-Abilities-Menu-Requirements.md):

```
RacialAbilitiesUI.RefreshBodyForFocusedMember()
  → …
  Race.Human + humanClass.Knight + HumanSpecialization
      + HumanClassSkillTreeRuntime (Knight tree)
      → HumanKnightSkillBodyView
  Human + None / Mage / Priest → existing placeholders
```

| Condition | Body |
|-----------|------|
| Human + **Knight** + Knight `HumanClassSkillTreeRuntime` | **Skill tree sheet** (this doc) |
| Human + **Knight** but no runtime / wrong tree class | *“Knight skill data is missing for this character.”* |
| Human + **Mage** | `HumanMageSpellBodyView` (existing) |
| Human + **None / Priest** | Default placeholder + class message |
| Not Human | Default placeholder (unchanged) |

---

## 5. Data sources

| UI region | Runtime / data |
|-----------|----------------|
| **Skill rows** | `HumanClassSkillTreeDefinition.nodes` + `HumanClassSkillTreeRuntime` rank map |
| **Mastery column** | `KnightSkillMasteryRuntime` per `nodeId` (when component present; else 0) |
| **Summary strip** | `CharacterStats.currentSoulPower`, `MaxSoulPower`; unspent = `skillPointsTotal - GetTotalSpentPoints`; active stance = `KnightAuraStateRuntime` |
| **Skill metadata** | `HumanClassSkillTreeNodeData`: `displayName`, `description`, `maxRanks`, tags, `activeAbilities[]` icons |
| **Gate checks** | `TrySpendPoint` failure reasons for detail pane |
| **Edit eligibility** | New: `SafeZonePolicyService.TryAllowHumanKnightSkillSpend` (mirror Mage equip — safe zone + not in combat) |
| **Mutations** | `HumanKnightSkillTreeService.TrySpendPoint` (wrapper) — no direct rank mutation from UI |
| **Hotbar add** | `HumanKnightHotbarSync` (new, parallel `HumanMageHotbarSync`) for active abilities |

**Sort within branch:** unlocked (rank ≥ 1) first by descending rank, then locked nodes by `requiredCharacterLevel`, then name A→Z.

---

## 6. Summary strip (Knight resources)

Pinned **above** the scrollable tree (always visible).

| Field | Format | Example |
|-------|--------|---------|
| **Soul Power** | `Soul Power: {current} / {max}` | Soul Power: 18 / 24 |
| **Skill points** | `Points: {unspent} unspent ({spent} spent)` | Points: 2 unspent (8 spent) |
| **Active stance** | `Stance: {displayName}` or `Stance: —` | Stance: Valor Aura |
| **Character level** | `Level {level} · Mastery cap {trainingCap}` | Level 10 · Mastery cap 20 |

**Muted footnote:** *Rank rises from **skill points** in town **or** **proficiency experience** from using **actives** in combat. Mastery grows from the same combat use.*

When **unspent points = 0**, detail **Spend skill point** disabled with inline hint.

---

## 7. Skill tree list

### 7.1 — Branch headers

| Header | Typical nodes |
|--------|----------------|
| **GENERAL TECHNIQUES** | Passives (Iron Posture, Quick Feet) |
| **BULWARK** | Defensive auras, shield synergies |
| **VALOR** | Offensive auras, challenge marks |
| **COMMAND** | Rally pulses, party utility |

Optional **filter chips** (v0.1): `All` | `Passives` | `Auras` | `Actives` — filter by `KnightSkillTag`.

### 7.2 — Row content

Each row in the scroll list:

| Element | Source / rule |
|---------|----------------|
| **Icon** | `AbilityAction` icon for actives; Knight emblem fallback for passives |
| **Title** | `displayName` |
| **Rank** | `{rank} / {maxRanks}` — **`0 / 5`** when locked |
| **Rank proficiency bar** | Actives only, rank ≥ 1 and rank &lt; max: thin bar `rankPxp / xpToNextRank` |
| **Mastery** | `{masteryLevel} / {trainingCap}` — hidden as **`—`** when rank = 0 (not yet unlocked) |
| **Mastery bar** | Thin bar: `masteryPxp / xpToNextMastery` when rank ≥ 1 and below caps |
| **Badge: ACTIVE** | Gold pill when node is current **stance aura** |
| **Badge: LOCKED** | Muted when rank = 0 and gates fail |
| **Badge: MAX** | When `rank == maxRanks` |
| **Selection** | Amber left accent on selected row |

**Row height:** ~56–64px (icon 40–48px).

### 7.3 — Locked row display

| State | Row appearance |
|-------|----------------|
| **rank = 0**, gates fail | Muted row; rank `0 / max`; mastery hidden; **LOCKED** badge |
| **rank = 0**, gates pass, unspent points | Normal brightness; detail pane offers **Spend skill point** → rank becomes 1 |
| **rank ≥ 1** | Full brightness; mastery visible |

### 7.4 — Empty / error states

| Condition | Copy |
|-----------|------|
| No Knight tree asset | *Knight skill tree is not configured.* |
| Zero nodes authored | *No Knight skills are defined.* |
| `KnightSkillMasteryRuntime` missing | Mastery column shows `—`; log dev warning |

### 7.5 — Interaction

| Input | Behavior |
|-------|----------|
| **Click row** | Select skill; rebuild **detail pane** |
| **Default selection on open** | First skill with **rank ≥ 1** and **ACTIVE** stance, else highest rank skill, else first unlockable, else first node |
| **Change focused party member** | Rebuild tree + detail |
| **Scroll** | Tree only; summary + detail pinned |

No drag-and-drop in v0.

---

## 8. Visual layout (mock — authoritative)

Uses same **full-screen racial shell** as parent doc (title, banner, party strip, footer).

```
┌──────────────── FULL SCREEN ──────────────────────────────────────────────┐
│ RACIAL ABILITIES                                                            │
│ Spend skill points on your Knight techniques here. New points come from…    │  banner (edit)
│ [F1 Human Knight ●] [F2 …] [F3 …] … party strip                             │
├─────────────────────────────────────────────────────────────────────────────┤
│ SOUL POWER 18/24 · Points 2 unspent (8 spent) · Stance: Valor Aura          │  summary strip
│ Level 10 · Mastery cap 20                                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│ [All] [Passives] [Auras] [Actives]                          filter (v0.1) │
│ ┌ scroll tree ────────────────────────────────────────────────────────────┐ │
│ │ GENERAL TECHNIQUES                                                       │ │
│ │  [icon] Iron Posture      Rank 3/5   Mastery 8/20   [████░░░░]           │ │
│ │  [icon] Quick Feet        Rank 2/5   Mastery 4/20   [██░░░░░░]           │ │
│ │ VALOR                                                                    │ │
│ │  [icon] Valor Aura        Rank 3/5   [Rank ████░░]  Mastery 12/20  [ACTIVE]│ │
│ │  [icon] Mark of Challenge Rank 0/3   —              [LOCKED]             │ │
│ │ BULWARK                                                                  │ │
│ │  [icon] Bulwark Aura      Rank 1/5   Mastery 2/20   [█░░░░░░░]           │ │
│ │ COMMAND                                                                  │ │
│ │  [icon] Rallying Cry      Rank 2/3   Mastery 6/20   [███░░░░░]           │ │
│ └───────────────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│ DETAILS · Valor Aura                                                        │
│ ┌────┐  Rank 3/5 · Mastery 12/20 (cap 20) · +24% party damage (resolved)    │
│ │icon│  While active, allies in radius deal increased damage. Stance aura.  │
│      │  Rank proficiency: 148 / 220 pxp to rank 4 — earned by using this active in combat │
│      │  Next rank: +2% damage · −1 Soul Power upkeep                                        │
│      │  Mastery: 412 / 580 pxp to level 13 — same combat events                           │
│      │  [ Spend skill point ]  [ Add to hotbar ]         (edit mode)       │
│      │  Assign actives on the **ability hotbar** to use in combat.           │
├─────────────────────────────────────────────────────────────────────────────┤
│ K — racial abilities · Esc — close · F1–F5 — focus member                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

**View-only variant:** **Spend skill point** / **Add to hotbar** hidden or disabled; banner uses §3.1 dungeon/combat copy.

### 8.1 — Layout tokens

| Token | Value | Notes |
|-------|-------|-------|
| Panel background | `(0.08, 0.085, 0.095, 0.96)` | Match racial shell |
| Knight accent | `(0.82, 0.68, 0.32)` | Selected row bar, ACTIVE badge |
| Secondary accent | `(0.55, 0.58, 0.65)` | Steel grey section labels |
| LOCKED row alpha | ~`0.45` | |
| Rank text | Gold when &lt; max; white at MAX | |
| Mastery bar | 6px, amber fill | Hidden when rank = 0 |
| Detail pane height | **~30%** | Scroll inside |
| Typography | Title 28 / banner 17 / section 18 / row 17 / footer 15 | TMP |

---

## 9. Detail pane

### 9.1 — Content (all modes)

| Block | Content |
|-------|---------|
| **Icon** | 64–80px skill / ability icon |
| **Title** | `displayName` |
| **Rank line** | `Rank {rank} / {maxRanks}` · tags (`Aura`, `Stance`, …) |
| **Rank proficiency line** | Actives, rank ≥ 1 and rank &lt; max: `{rankPxp} / {xpToNextRank} pxp to rank {rank+1}`; passives: *Rank increases only from skill points.* |
| **Mastery line** | If rank ≥ 1: `Mastery {level} / {trainingCap}` + mastery pxp progress; if rank = 0: *Unlock this technique to begin proficiency and mastery.* |
| **Description** | Node `description` |
| **Effect summary** | Resolved combat text from rank + mastery ([Knight doc §8.1](Human-Knight-Auras-And-Skill-Tree-Requirements.md)) — e.g. current party damage bonus |
| **Next rank preview** | If rank &lt; max: authored delta from rank `{rank+1}` table — *Next rank: …* |
| **Training hint** | *Using this **active** in combat earns **proficiency experience** toward the **next rank** and **mastery** (activation and successful use — not passive upkeep).* |
| **Hotbar footnote** | *Assign actives on the **ability hotbar** to use in combat.* |
| **Proficiencies link** | *See also **`P`** proficiencies menu → Knight skills.* |

### 9.2 — Actions (edit mode only)

| Selected skill state | Button(s) | Enabled when |
|----------------------|-----------|--------------|
| **Unlockable (rank 0)** | **Spend skill point** | Unspent points ≥ 1 **and** `TrySpendPoint` passes |
| **Rank &lt; max** | **Spend skill point** | Same |
| **Rank = max** | **Spend skill point** (disabled) | Label *Max rank* |
| **Active skill, rank ≥ 1** | **Add to hotbar** | Ability present; not already on hotbar / empty slot exists |
| **Passive only** | *(no hotbar button)* | — |

On successful spend: call `HumanKnightSkillTreeService.TrySpendPoint`, refresh tree + detail, re-apply stat payloads via runtime.

On failure: show `failureReason` inline (muted red).

### 9.3 — View-only mode

| Element | Behavior |
|---------|----------|
| **Spend skill point / Add to hotbar** | Hidden or disabled — tooltip *Only in town.* |
| **Detail content** | Fully readable including next-rank preview |

---

## 10. Edit policy

New helper (mirror Mage):

```csharp
SafeZonePolicyService.TryAllowHumanKnightSkillSpend(out denyReason)
  => IsSafeZoneForActiveParty() && !partyInCombat
```

| Context | Tree + detail | Spend point | Hotbar add |
|---------|---------------|-------------|------------|
| Town, peace | ✓ | ✓ | ✓ |
| Town, in combat | ✓ | ✗ | ✗ |
| Dungeon | ✓ | ✗ | ✗ (optional: allow hotbar add — **default deny** in v0) |

**Deny messages:**

| Case | Message |
|------|---------|
| Not safe zone | *You can only spend skill points in town.* |
| In combat | *You cannot adjust your skill tree during combat.* |

---

## 11. Hotbar integration

| Rule | Detail |
|------|--------|
| **Assign pool** | `HotbarAssignabilityService` lists Knight actives with **tree rank ≥ 1** under **Knight Skills** group (new). |
| **Add to hotbar (menu)** | Assign selected active’s `AbilityAction` to first empty main slot; disabled if already assigned. |
| **Source enum** | Extend `PlayerAbilitySource` with **`HumanKnightSkill`** + stable `nodeId` / ability index. |
| **After rank spend on active** | Skill appears in hotbar pool; **Add to hotbar** enabled. |

---

## 12. View-model builder

Suggested API:

```
HumanKnightSkillBodyViewModel.Build(BaseActor knight, string selectedNodeId = null)
  → treeRuntime = HumanClassSkillTreeRuntime
  → masteryRuntime = KnightSkillMasteryRuntime
  → auraState = KnightAuraStateRuntime
  → editMode = ResolveEditMode()
  → summary = BuildSummaryStrip(stats, treeRuntime, auraState)
  → rows = BuildBranchGroupedRows(tree, mastery, auraState)
  → selection = ResolveDefaultSelection(rows, auraState)
  → detail = BuildDetail(selection, editMode, hotbarLayout)
  → return { EditMode, BannerText, Summary, Rows, Detail }
```

Unit tests: branch grouping, locked vs unlockable, spend gating, mastery hidden at rank 0, ACTIVE badge, default selection, detail button states.

---

## 13. Integration

| System | Rule |
|--------|------|
| **RacialAbilitiesUI** | Router mounts `HumanKnightSkillBodyView`; hide Mage body when Knight focused. |
| **HumanKnightSkillTreeService** | Sole mutation path for spending points from UI. |
| **HumanClassSkillTreeRuntime** | Read ranks; refresh after spend. |
| **KnightSkillMasteryRuntime** | Read-only in this menu. |
| **AbilityHotbarUI** | `RefreshAll` after hotbar add. |

---

## 14. Acceptance criteria

| ID | Test |
|----|------|
| **A1** | Focus Human Knight → tree lists all authored nodes grouped by branch with correct rank, rank proficiency bar (actives), and mastery. |
| **A2** | Valor Aura rank 3 → row shows `3/5`, rank proficiency bar, mastery `12/20`; **ACTIVE** badge when stance on. |
| **A3** | Rank-0 locked node → mastery hidden; detail shows gate requirements. |
| **A4** | **Town, peace:** select Iron Posture with unspent point → **Spend skill point** succeeds; rank increments. |
| **A5** | **Town, peace:** rank = max → spend button disabled. |
| **A6** | **Dungeon:** spend disabled; dungeon banner; tree still readable. |
| **A7** | **In combat:** spend disabled. |
| **A8** | Active skill rank ≥ 1 → **Add to hotbar** assigns to empty slot. |
| **A9** | Human **Mage** focused → Mage body; Knight tree not shown. |
| **A10** | Summary strip shows Soul Power, unspent points, active stance name. |
| **A11** | **`K` / Esc / F1–F5** unchanged from parent racial menu. |
| **A12** | Opening menu does not consume a turn. |
| **A13** | Detail pane distinguishes **Rank**, **Proficiency** (rank pxp), and **Mastery** in all copy. |
| **A14** | Passive node (Iron Posture) → no rank proficiency bar; detail states rank from skill points only. |

---

## 15. Implementation phases

| Phase | Scope |
|-------|--------|
| **v0 (this doc)** | Requirements + mock; `HumanKnightSkillBodyView` + view-model; router; edit gating; detail spend + hotbar add |
| **v0.1** | Filter chips; keyboard row navigation; foreclosed sibling ghosts (exclusive branches) |
| **v1** | Visual tree graph with connector lines; training NPC grants points in-world with menu refresh |

---

## 16. Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | §5.3 router — add Knight body |
| [Human Knight — Auras & skill tree](Human-Knight-Auras-And-Skill-Tree-Requirements.md) | §12 UI — link here; mark mock done |
| [Human Mage — racial menu](Human-Mage-Racial-Abilities-Menu-Requirements.md) | §14 out of scope — Knight doc exists |
| [Human — Class powers](Human-Class-Powers-Requirements.md) | Knight UI pointer |

---

## 17. Document history

| Date | Change |
|------|--------|
| 2026-06-13 | Initial draft — branch-grouped skill tree sheet, rank + mastery rows, summary strip, town spend / dungeon view-only, detail pane, mock; aligned with Knight hybrid progression and Mage menu patterns. |
| 2026-06-05 | **Rank proficiency pxp** — actives show combat progress toward next tree rank; detail pane + row bars; terminology Rank / Proficiency / Mastery. |
