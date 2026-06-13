# Dragonian — Racial abilities menu (Draconic word-forms)

**Purpose:** Specify the **Dragonian body** of the shared [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (`K`): a **learned vs memorized spell sheet** for the focused Dragonian — **memorized word-forms** on the left, **full known library** on the right, **pinned detail pane** below, and **town-only loadout editing** (memorize / unmemorize within Soul Power memory budget).

**Status:** Implemented (v0 — requirements + mock + `DragonianSpellBodyView` / view-model + router).

**Visual mock:** [`Docs/RacialSystem/dragonian-racial-abilities-menu-mock.png`](dragonian-racial-abilities-menu-mock.png) (town edit mode — companion to §8).

**Depends on:** [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (shell, **`K`**, party strip, modal rules), [Dragonian — Spell memory & casting](Dragonian-Spell-Memory-Requirements.md) (`DragonianSpellDefinition`, `DragonianSpellsRuntime`, memory budget, cast costs), [Dragonian — Spell learning (Elder quests)](Dragonian-Spell-Learning-Elder-Quests-Requirements.md) (`TryLearnSpell`, Elder chains), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (`HotbarEntryKind.DragonianSpell`, overflow assign pool), `SafeZonePolicyService`, `DragonianSpellLoadoutService`, `DragonianSpellCatalogService`.

**Related:** [Tiefling — racial abilities menu](Tiefling-Racial-Abilities-Menu-Requirements.md) (slot grid + detail pane layout pattern), [Elf — racial abilities menu](Elf-Racial-Abilities-Menu-Requirements.md) (party-scoped racial body), [Human — Class powers](Human-Class-Powers-Requirements.md) (Mage **known vs equipped** — closest analogue, but Dragonians use **Soul Power** budget not Magic Power).

**Explicitly out of scope (v0):** **Learning** new spells from this menu (Elder quests only); **unlearn** / respec known library; casting spells from the menu; drag-and-drop to hotbar (use existing hotbar overflow UI); reordering memorized list (order follows add order / stable sort by display name); gamepad layout; persisting last-focused party member across sessions; spell **tier** UI; comparing spells side-by-side; editing another party member’s loadout while a non-Dragonian is focused.

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Dragonian body mounts when focused member is `Race.Dragonian` with `RacialSubsystemKind.DragonianSpells` and `DragonianSpellsRuntime` present. |
| **L2** | **Two-column body:** **left = memorized loadout** (“Equipped word-forms”); **right = known library** (“All word-forms”). |
| **L3** | **Detail pane** pinned below columns (~28–32% height); populated by **clicking any spell row** in either column. |
| **L4** | **Dungeon / non–safe zone:** menu is **view-only** — lists and detail readable; **no** memorize / unmemorize actions. |
| **L5** | **Town safe zone + not in combat:** player may **memorize** and **unmemorize** via detail-pane actions, subject to memory budget ([spell memory §5](Dragonian-Spell-Memory-Requirements.md)). |
| **L6** | **Memorize gate:** spell must be **learned** (`KnownSpells`) and `remainingMemory >= memorizeCost`. |
| **L7** | **Unmemorize gate:** spell must be in **memorized** set; always allowed when edit mode is active (frees capacity; does **not** remove from known library). |
| **L8** | **Hotbar:** only **memorized** spells appear in hotbar assign pool (`HotbarAssignabilityService` — existing). Detail pane footnote reminds player to assign on the **ability hotbar**. |
| **L9** | **Learn spells** only via **Dragonian Elder** quest turn-in — banner in edit mode points to Elders; empty known library state names Elders explicitly. |
| **L10** | Menu refresh on **open**, **focus change**, and after **successful memorize / unmemorize** — not every frame. |
| **L11** | **Aesthetic:** `RacialUiTheme` dark glass; Dragonian accent **deep crimson / ember gold** (distinct from Tiefling amber, Barbarian totem gold, Elf teal). |
| **L12** | **Terminology in UI:** player-facing label **“Equipped word-forms”** = engine **`MemorizedSpells`**; **“All word-forms”** = **`KnownSpells`**. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Loadout clarity** — Player sees at a glance which draconic spells are **ready to cast** (memorized) vs merely **learned**. |
| **G2** | **Town planning** — Player adjusts memorized set in **safe town** before entering the dungeon — same discipline as Human Mage equip. |
| **G3** | **Budget honesty** — UI shows **memorized cost sum**, **remaining memory**, and **current Soul Power** (cast pool) so memorize vs cast costs are not confused. |
| **G4** | **Reference in dungeon** — Player can still **open `K`** underground to **read** spell descriptions and current loadout, but cannot change it. |
| **G5** | **Hotbar bridge** — Menu explains memorized spells must be **assigned on the ability hotbar**; does not duplicate hotbar assign UI. |
| **G6** | **Elder loop closure** — After quest learn, player has an obvious next step: open racial menu → memorize → hotbar. |
| **G7** | **Multi-Dragonian** — Party strip switches body per focused member; each Dragonian has independent known + memorized lists. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Dragonian body** | Race-specific panel in `RacialAbilitiesUI` when focused member is Dragonian with spell subsystem. |
| **Known library** | Spells the member has **learned** (`DragonianSpellsRuntime.KnownSpells`) — permanent until future unlearn content. |
| **Memorized loadout** | Subset of known spells **equipped for casting** (`MemorizedSpells`) — UI label **“Equipped word-forms”**. |
| **Memory budget** | `MaxSoulPower` minus Σ `memorizeCost` of memorized spells (`RemainingMemoryCapacity`). |
| **Cast pool** | `CharacterStats.currentSoulPower` — spent when **casting**, not when memorizing. |
| **Edit mode** | Town safe zone **and** party **not** in combat (`SafeZonePolicyService` + `CombatThreatCoordinator`). |
| **View-only mode** | Any context where loadout edits are blocked — dungeon, combat, or non–safe zone. |
| **Detail pane** | Bottom region showing selected spell icon, stats, description, and action buttons. |

---

## 3. Screen responsibilities (locked)

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Racial menu — Dragonian body (edit mode)** | View known + memorized lists; **memorize / unmemorize**; read descriptions | Learn spells, cast, assign hotbar slots |
| **Racial menu — Dragonian body (view-only)** | View lists + descriptions | Memorize, unmemorize |
| **Dragonian Elder** | Accept / turn-in quests → **learn** spell | Memorize or change loadout |
| **Ability hotbar** | Assign **memorized** spells; cast in combat | Show full known library reference |
| **Quest journal** | Track Elder trials | Edit memorized loadout |

### 3.1 — Banner copy (required)

| Mode | Banner |
|------|--------|
| **Edit mode (town, peace)** | *Adjust equipped word-forms here. Learn new word-forms from **Dragonian Elders** in town.* |
| **View-only (dungeon)** | *View only — you can only adjust equipped word-forms in town.* |
| **View-only (combat)** | *View only — finish combat before adjusting equipped word-forms.* |

No teleport buttons; Elders named as progression direction only.

---

## 4. Router integration

Extends parent [§5.3 router](../UI/Racial-Abilities-Menu-Requirements.md):

```
RacialAbilitiesUI.RefreshBodyForFocusedMember()
  → …
  Race.Dragonian + DragonianSpells → DragonianSpellBodyView
  (else default placeholder)
```

| Condition | Body |
|-----------|------|
| `Race.Dragonian` + `DragonianSpells` + `DragonianSpellsRuntime` | **Two-column spell sheet** (this doc) |
| `Race.Dragonian` but no runtime / wrong subsystem | Default placeholder: *“This character cannot use draconic word-forms.”* |
| Not Dragonian | Default placeholder (unchanged) |

---

## 5. Data sources

| UI region | Runtime / data |
|-----------|----------------|
| **Known list** | `DragonianSpellsRuntime.KnownSpells` sorted **display name A→Z** (case-insensitive), tie-break `spellId` |
| **Memorized list** | `DragonianSpellsRuntime.MemorizedSpells` — same sort |
| **Budget strip** | `CharacterStats.MaxSoulPower`, `RemainingMemoryCapacity`, Σ memorized `memorizeCost`, `currentSoulPower` |
| **Spell metadata** | `DragonianSpellDefinition`: `displayName`, `description`, `memorizeCost`, `soulPowerCastCost`, `ability` icon |
| **Edit eligibility** | `SafeZonePolicyService.TryAllowDragonianMemorizeChange` **and** `!CombatThreatCoordinator.IsInCombat` |
| **Mutations** | `DragonianSpellLoadoutService.TryMemorize` / `TryUnmemorize` only — no direct runtime calls from UI |

**Do not** list spells from `DragonianSpellCatalog` that are **not learned** — known library is the source of truth for the right column.

---

## 6. Soul Power budget strip

Pinned **above** the two columns (always visible).

| Field | Format | Example |
|-------|--------|---------|
| **Max memory** | `Max Soul Power: {MaxSoulPower}` | Max Soul Power: 50 |
| **Memorized cost** | `Equipped: {sumMemorizeCost} / {MaxSoulPower}` | Equipped: 10 / 50 |
| **Remaining** | `Free: {RemainingMemoryCapacity}` | Free: 40 |
| **Cast pool** | `Current Soul Power: {currentSoulPower}` | Current Soul Power: 42 |

**Muted footnote (one line):** *Memorizing spends **capacity** only. Casting spends **current** Soul Power.*

When **edit mode** and remaining capacity is **0**, known-list rows with `memorizeCost > 0` show disabled memorize affordance.

---

## 7. Two-column spell lists

### 7.1 — Column headers

| Column | Header | Subtitle |
|--------|--------|----------|
| **Left (~45%)** | `EQUIPPED WORD-FORMS` | *Ready to assign on the hotbar* |
| **Right (~55%)** | `ALL WORD-FORMS` | *Learned draconic techniques* |

Both columns scroll independently inside the middle band (~45–50% viewport height).

### 7.2 — Row content

Each row (either column):

| Element | Source |
|---------|--------|
| **Icon** | `spell.ability` icon if present; else Dragonian spell emblem fallback |
| **Title** | `displayName` (fallback `spellId`) |
| **Subtitle** | `Memorize {memorizeCost} SP · Cast {soulPowerCastCost} SP` |
| **Badge (right column only)** | `Equipped` pill if spell is also memorized |
| **Selection** | Gold outline on selected row (either column) |

### 7.3 — Empty states

| Column | Empty copy |
|--------|------------|
| **Left (no memorized)** | *No word-forms equipped. Select a learned spell and equip it.* |
| **Right (no known)** | *No word-forms learned yet. Complete trials with a **Dragonian Elder** in town.* |

### 7.4 — Interaction

| Input | Behavior |
|-------|----------|
| **Click row (either column)** | Select spell; rebuild **detail pane** |
| **Default selection on open** | First memorized spell if any; else first known spell; else none |
| **Change focused party member** | Rebuild columns + detail; re-run default selection |
| **Scroll** | Independent per column; budget strip + detail pane pinned |

No drag-and-drop between columns in v0 — use detail-pane **Equip** / **Unequip** buttons.

---

## 8. Visual layout (mock — authoritative)

Uses same **full-screen racial shell** as parent doc (title, banner, party strip, footer).

```
┌──────────────── FULL SCREEN ──────────────────────────────────────────────┐
│ RACIAL ABILITIES                                                            │
│ Adjust equipped word-forms here. Learn new word-forms from Dragonian Elders…│  banner (edit)
│ [F1 Dragonian ●] [F2 …] [F3 …] … party strip                               │
├─────────────────────────────────────────────────────────────────────────────┤
│ SOUL POWER · Max 50 · Equipped 10/50 · Free 40 · Current SP 42            │  budget strip
├──────────────────────────────┬──────────────────────────────────────────────┤
│ EQUIPPED WORD-FORMS          │ ALL WORD-FORMS                    (scroll)   │
│ ┌──────────────────────────┐ │ ┌──────────────────────────────────────────┐│
│ │ [icon] Draconic Surge      │ │ │ [icon] Draconic Surge        [Equipped] ││
│ │ Memorize 3 · Cast 1      │ │ │ Memorize 3 · Cast 1                     ││
│ └──────────────────────────┘ │ │ [icon] Dragon Flame                     ││
│ (empty hint if none)         │ └──────────────────────────────────────────┘│
├──────────────────────────────┴──────────────────────────────────────────────┤
│ DETAILS                                                                       │
│ ┌────┐  Draconic Surge                                                        │
│ │icon│  Internalize draconic might — a sudden burst of strength.              │
│      │  MEMORIZE COST 3 · CAST COST 1 · Sudden Strength (ability)             │
│      │  [ Unequip ]                                    (edit mode, memorized) │
│      │  Assign equipped word-forms on the **ability hotbar** to cast.         │
├─────────────────────────────────────────────────────────────────────────────┤
│ K — racial abilities · Esc — close · F1–F5 — focus member                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

**View-only variant:** detail pane shows **Equip** / **Unequip** buttons **hidden or disabled**; banner uses §3.1 dungeon/combat copy.

### 8.1 — Layout tokens

| Token | Value | Notes |
|-------|-------|-------|
| Panel background | `(0.08, 0.085, 0.095, 0.96)` | Match inventory / racial |
| Dragonian accent | `(0.72, 0.22, 0.14)` left bar on selected row | Distinct crimson |
| Budget strip fill | `(0.11, 0.09, 0.10, 0.95)` | Subtle red tint |
| Column divider | 1px `(0.35, 0.20, 0.15, 0.5)` | |
| Row min height | **64px** | Icon 40–48px |
| Equipped badge | Small pill, ember gold | Right column only |
| Detail pane height | **~30%** | Scroll inside if description long |
| Typography | Title 28 / banner 17 / column header 20 / row title 19 / body 17 / footer 15 | TMP |

---

## 9. Detail pane

### 9.1 — Content (all modes)

| Block | Content |
|-------|---------|
| **Icon** | Large spell / ability icon (64–80px) |
| **Title** | `displayName` |
| **Description** | `DragonianSpellDefinition.description` |
| **Costs** | `Memorize cost: {memorizeCost}` · `Cast cost: {soulPowerCastCost} Soul Power` |
| **Ability line** | Underlying `AbilityAction` name if useful for players who know essences |
| **Hotbar footnote** | *Assign equipped word-forms on the **ability hotbar** to cast in combat.* |

### 9.2 — Actions (edit mode only)

| Selected spell state | Primary button | Enabled when |
|----------------------|----------------|--------------|
| **Known, not memorized** | **Equip** | `RemainingMemoryCapacity >= memorizeCost` |
| **Known, not memorized, insufficient capacity** | **Equip** (disabled) | Tooltip: *Need {cost} free capacity; have {remaining}.* |
| **Memorized** | **Unequip** | Always |

On success: call `DragonianSpellLoadoutService`, refresh body, `AbilityHotbarUI.RefreshAll()`, keep selection on same spell.

On failure: show `failureReason` inline below buttons (muted red) — no modal.

### 9.3 — View-only mode

| Element | Behavior |
|---------|----------|
| **Equip / Unequip** | Hidden **or** visible disabled with tooltip *“Only in town.”* |
| **Detail content** | Fully readable |

---

## 10. Edit policy (implementation)

Extend **`SafeZonePolicyService.TryAllowDragonianMemorizeChange`** (or wrap in `DragonianSpellLoadoutService`) to require **both**:

1. **`IsSafeZoneForActiveParty()`** — existing town gate.
2. **`!CombatThreatCoordinator.Instance.IsInCombat`** — block loadout edits during combat even on safe tiles.

| Context | Lists | Detail | Equip / Unequip |
|---------|-------|--------|-----------------|
| Town, peace | ✓ | ✓ | ✓ |
| Town, in combat | ✓ | ✓ | ✗ |
| Dungeon | ✓ | ✓ | ✗ |

**Deny messages (reuse / extend):**

| Case | Message |
|------|---------|
| Not safe zone | *You can only adjust memorized spells in town.* (existing) |
| In combat | *You cannot adjust equipped word-forms during combat.* |

---

## 11. Hotbar integration

| Rule | Detail |
|------|--------|
| **Assign pool** | Unchanged — `HotbarAssignabilityService` lists **memorized** spells under **Dragonian Spells** group. |
| **After unmemorize** | Hotbar entries pointing at removed memorized index become **stale** — existing hotbar refresh / stale handling applies. |
| **After memorize** | New spell appears in overflow pool; menu does **not** auto-assign to a slot. |
| **Menu responsibility** | Reference + loadout edit only — **not** hotbar slot picker. |

---

## 12. View-model builder

Suggested API (implementation hint):

```
DragonianSpellBodyViewModel.Build(BaseActor dragonian)
  → runtime = dragonian.GetComponent<DragonianSpellsRuntime>()
  → stats = dragonian.GetComponent<CharacterStats>()
  → editMode = CanEditLoadout()
  → budget = BuildBudgetStrip(stats, runtime)
  → memorizedRows = SortSpells(runtime.MemorizedSpells)
  → knownRows = SortSpells(runtime.KnownSpells)
  → selection = ResolveDefaultSelection(memorizedRows, knownRows)
  → detail = BuildDetail(selection, editMode, runtime)
  → return { EditMode, BannerText, Budget, MemorizedRows, KnownRows, Detail }
```

Unit tests cover: sort order, edit gating, equip capacity denial, empty states, selection default, detail button visibility.

---

## 13. Integration

| System | Rule |
|--------|------|
| **RacialAbilitiesUI** | Router mounts `DragonianSpellBodyView`; refresh on open / focus / loadout change. |
| **DragonianSpellLoadoutService** | Sole mutation path for memorize / unmemorize from UI. |
| **DragonianSpellsRuntime** | Read lists + budget; mutations via service only. |
| **Elder quest turn-in** | `TryLearnSpell` only — menu reflects new known spell on next open. |
| **AbilityHotbarUI** | `RefreshAll` after loadout change. |
| **Quest journal** | May show learn-spell reward line using catalog display names (unchanged). |

---

## 14. Acceptance criteria

| ID | Test |
|----|------|
| **A1** | Focus Dragonian with Draconic Surge learned + memorized → left column shows Surge; right shows Surge with **Equipped** badge. |
| **A2** | Focus Dragonian with two learned spells, one memorized → budget strip shows correct sum / remaining. |
| **A3** | **Town, peace:** select Dragon Flame → **Equip** succeeds when capacity allows; appears in left column; hotbar overflow lists it. |
| **A4** | **Town, peace:** select memorized spell → **Unequip** removes from left column; known library unchanged. |
| **A5** | Equip when `memorizeCost > RemainingMemoryCapacity` → disabled button + inline reason. |
| **A6** | **Dungeon:** open menu → lists readable; Equip/Unequip disabled; dungeon banner shown. |
| **A7** | **In combat (town or dungeon):** loadout edits blocked; combat banner shown. |
| **A8** | Click spell in either column → detail pane updates description + costs. |
| **A9** | Empty known library → right-column empty state mentions **Dragonian Elder**. |
| **A10** | Non-Dragonian focused member → default placeholder; Dragonian body hidden. |
| **A11** | **`K` / Esc / F1–F5** unchanged from parent racial menu doc. |
| **A12** | Opening menu does not consume a turn. |

---

## 15. Implementation phases

| Phase | Scope |
|-------|--------|
| **v0 (this doc)** | Requirements + mock; `DragonianSpellBodyView` + view-model; router; edit gating; detail pane actions |
| **v0.1** | Keyboard navigation between rows; optional filter (equipped only / not equipped) |
| **v1** | Drag reorder memorized list (cosmetic hotbar order hint only); spell icons per `DragonianSpellDefinition` |

---

## 16. Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | §14 v1 — add Dragonian body link; mark **Done** when shipped |
| [Dragonian — Spell memory](Dragonian-Spell-Memory-Requirements.md) | §5.4 — replace “read-only v0” with link here |
| [Dragonian — Elder quests](Dragonian-Spell-Learning-Elder-Quests-Requirements.md) | Post-learn player flow: racial menu → equip → hotbar |
| [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) | Dragonian spell assign prerequisites (memorized only) |

---

## 17. Document history

| Date | Change |
|------|--------|
| 2026-06-05 | Implemented v0 — `DragonianSpellBodyView`, view-model, router, theme tokens, unit tests. |
| 2026-06-05 | Initial draft — two-column learned/memorized sheet, town edit / dungeon view-only, detail pane, hotbar footnote, visual mock. |
