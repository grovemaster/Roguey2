# Racial abilities menu — Requirements

**Purpose:** Specify a **party-scoped reference menu** opened by hotkey where the player browses **folk / racial ability information** for each party member. Content **depends on the focused member’s race** (and subsystem where applicable). v0 ships a **read-only Barbarian Spirit Imprint** reference; all other races use a **placeholder default screen**.

**Status:** Implemented (v0.1 — full-screen visual redesign).

**Depends on:** `PartyManager`, `CharacterStats.race`, `CharacterStats.racialSubsystem`, `BaseActor`, `InputHandler` / `GameControls`, `GameplayModalGate`, [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (`InventoryPartyStripView` party carousel pattern), [Party control HUD](Party-Control-HUD-Requirements.md) (portrait catalog, F-key semantics), [Ability hotbar](Ability-Hotbar-Requirements.md) (active ability icons / tooltips), [Phase 3 — Barbarian Spirit Imprint](../RacialSystem/Phase3-Requirements.md) (`SpiritImprintGraph`, `SpiritImprintNodeData`, `SpiritImprintRuntime`), [Barbarian Spirit Imprint — Shaman NPC](../RacialSystem/Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) (`SpiritImprintUpgradeLogic.GetNextNodeOffers` — **not** for purchases in this menu).

**Related:** [Dwarf — Patron Ancestor](../RacialSystem/Dwarf-Ancestor-And-Common-Abilities-Requirements.md), [Elf — Elemental Spirit contracts](../RacialSystem/Elf-ElementalSpirit-Contracts-Requirements.md), [Tiefling — Cyborg implants](../RacialSystem/Tiefling-Cyborg-Implants-Requirements.md), [Human class powers](../RacialSystem/Human-Class-Powers-Requirements.md) (future race-specific bodies).

**Explicitly out of scope (v0):** Editing imprint path or any racial progression from this menu; showing **unlock costs** (gold, items, flags); respec; assigning actives to the hotbar (hotbar remains the binding UI); gamepad layout; persisting last-focused member across sessions; full imprint **skill tree** with pan/zoom; **next-mark silhouettes** at menu bottom (deferred v0.1 — §8.4); Dwarf / Elf / Tiefling / Human **reference bodies** beyond placeholder copy; node-specific art per imprint mark (v0 may reuse one Spirit Imprint emblem).

**Locked decisions (user, 2026-06-07):**

| # | Decision |
|---|----------|
| **L1** | **Hotkey:** **`K`** toggles menu (`ToggleRacialAbilities`). |
| **L2** | **Party browse:** Same carousel pattern as inventory — click portrait (or F1–F5 while open) changes **focused member**; does **not** swap map control unless explicitly synced later. |
| **L3** | **Barbarian body:** **Read-only** Spirit Imprint reference; banner states changes only at **Shaman Barbarian**. |
| **L4** | **Node visibility (hybrid):** **Committed** nodes full detail; **foreclosed exclusive siblings** as one-line ghosts; **all other unreached nodes hidden** (§8). |
| **L5** | **Scroll:** Main body is vertically scrollable; header + party strip pinned. |
| **L6** | **Aesthetic:** **Dark glass** chrome consistent with inventory / party HUD / ability hotbar. **Full-screen** panel (same anchor stretch as inventory), not a floating card. |

---

## 5.1 — Visual layout (v0.1 — implemented)

Full-screen shell matching inventory / quest journal chrome. Implemented in `RacialAbilitiesUI`, `RacialUiTheme`, `RacialAbilitiesPartyStripView`, `SpiritImprintTimelineView`.

```
┌──────────────── FULL SCREEN (opaque 96%) ────────────────────────────┐
│ RACIAL ABILITIES                                                     │  32px title (TMP)
│ View only — visit the Shaman Barbarian in town…                      │  22px gold banner
│ [F1 portrait] [F2 portrait] [F3 portrait] …                          │  92px party strip
├──────────────────────────────────────────────────────────────────────┤
│ SPIRIT IMPRINT PATH                                                  │  20px section label
│  ●─┬ [icon] Title                                    [ACTIVE]        │  committed node card
│    │      description · STATS · PASSIVES · ACTIVES                  │
│  ○─┬ [icon] ○ Name — Not chosen (exclusive with …).                 │  ghost card (~42% opacity)
│    │      New marks only at the Shaman Barbarian.                    │
│  (scroll)                                                            │
├──────────────────────────────────────────────────────────────────────┤
│ K — racial abilities · Esc — close · F1–F5 — focus member            │  22px footer
└──────────────────────────────────────────────────────────────────────┘
```

| Token | Value | Notes |
|-------|-------|-------|
| Panel background | `(0.08, 0.085, 0.095, 0.96)` | Same as inventory |
| Section spacing | `6px` | Outer `VerticalLayoutGroup` |
| Panel padding | `12px` | Matches inventory |
| Party chip | `96×108px`, portrait `56px` | Horizontal row; `VerticalLayoutGroup` per chip (portrait above name) |
| Focus border | Gold `(0.91, 0.77, 0.28)` | Party HUD `ActiveBorderColor` |
| Typography | Title `28`, banner `17`, card title `21`, body `17`, footer `15` | TMP with `TMP_Settings.defaultFontAsset` |
| Node card fill | `(0.14, 0.15, 0.165, 0.95)` | Inventory row tint |
| Node left accent | `3px` gold bar | Committed cards only |
| Timeline rail | `24px` dot + vertical line | Filled dot = committed; muted dot = ghost |
| Ghost card | `CanvasGroup` α ≈ `0.42`, muted outline | §8.2 content only — no stats/passives/actives |
| Imprint icon v0 | Shared procedural emblem | `RacialUiTheme.ImprintEmblemSprite`; per-node icons deferred |

**§8 hybrid visibility unchanged:** foreclosed exclusive siblings remain visible as ghost timeline rows; deep unreached nodes stay hidden.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **At-a-glance reference** — Player can read what racial abilities a party member **currently has** without visiting town NPCs. |
| **G2** | **Per-member content** — Switching focused member swaps the body to that actor’s race/subsystem view. |
| **G3** | **Barbarian clarity** — Spirit Imprint path reads as a **timeline** (root → deepest mark) with passives and actives spelled out. |
| **G4** | **No progression duplication** — Menu does **not** replace Shaman dialog for buying the next mark; no costs, no confirm buttons. |
| **G5** | **Exclusivity memory** — Foreclosed sibling marks remain visible as **ghosts** so the player sees forks they closed (§8.2). |
| **G6** | **Spoiler control** — Deep / unreached branches stay **hidden** until committed or foreclosed at a known tier. |
| **G7** | **Hotbar separation** — Active abilities listed here for **description**; footer note points player to ability hotbar for combat use. |
| **G8** | **Modal-safe** — Opening/closing does not consume a turn; blocked when floor gameplay is modal-gated (same class as inventory). |
| **G9** | **Extensible router** — One shell + race-specific body views so Dwarf / Elf / etc. can plug in later without rewriting chrome. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Racial abilities menu** | Full-screen overlay UI toggled by **`K`**. |
| **Focused member** | Party member whose racial data the body displays; selected via party strip or F1–F5 while menu is open. |
| **Active member** | `PartyManager.GetActiveMember()` — who the player controls on the map; **independent** of focused member unless a future doc says otherwise. |
| **Race body** | Race-specific panel implementation (Barbarian Spirit Imprint, default placeholder, …). |
| **Committed node** | Id present on `SpiritImprintRuntime.chosenPathNodeIds`. |
| **Foreclosed sibling** | Graph node **not** on path, sharing the same `parentNodeId` and **non-zero** `siblingExclusivityGroup` as a **committed** sibling under that parent. |
| **Hidden node** | Any node that is neither committed nor a foreclosed sibling ghost. |
| **Ghost row** | Non-interactive, muted one-line entry for a foreclosed sibling (no payload, no costs). |
| **Node card** | Expanded block for a committed node: icon, title, description, stats, passive list, active list. |

---

## 3. Screen responsibilities (locked)

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Racial abilities menu (`K`)** | Read committed imprint marks, passives, actives, stats | Change path, pay costs, pick next mark |
| **Shaman dialog** | Buy **one** next mark (with costs) | Browse full party roster imprint sheet |
| **Ability hotbar** | Bind and **use** actives in combat | Show full racial passive reference |

---

## 4. Input & hotkey

### 4.1 — Toggle

| Action | Binding | Notes |
|--------|---------|-------|
| **ToggleRacialAbilities** | **`K`** | Add to `GameControls.inputactions`; mirror `ToggleInventory` / `ToggleQuestJournal` wiring in `InputHandler`. |
| **Close** | **`K`** (toggle) or **`Esc`** | `Esc` closes without toggling other menus. |

**Footer copy (v0):** `K — racial abilities · Esc — close · F1–F5 — focus member`

### 4.2 — While menu open

| Input | Behavior |
|-------|----------|
| **F1–F5** | Set focused member to `partyMembers[index]` (same index map as party HUD). |
| **Click portrait** | Same as F-key for that strip index. |
| **Scroll wheel / scrollbar** | Scroll body content only. |

### 4.3 — Turn & modal semantics

| Rule | Detail |
|------|--------|
| **Turn cost** | Open / close / change focused member → **no** `TurnManager.OnPlayerActionComplete`. |
| **Modal gate** | Do not open when `GameplayModalGate.BlocksFloorGameplay` (unless product later exempts reference menus — **not** in v0). |
| **Coexistence** | Opening racial menu while inventory open: **close inventory first** OR stack modals — **v0: mutually exclusive** (opening one closes the other). |

---

## 5. Shell layout (all races)

### 5.1 — Structure

```
┌──────────────────────────────────────────────────────────────────┐
│  RACIAL ABILITIES                                         [K]    │
├──────────────────────────────────────────────────────────────────┤
│  Party strip: [F1 portrait●] [F2] …   Name · Race · (subsystem)  │
├──────────────────────────────────────────────────────────────────┤
│  ┌ ScrollRect (vertical) ─────────────────────────────────────┐ │
│  │  << RaceBodyView >>                                          │ │
│  └──────────────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────────────┤
│  Footer: K toggle · Esc close · F1–F5 focus member               │
└──────────────────────────────────────────────────────────────────┘
```

| Region | Behavior |
|--------|----------|
| **Title bar** | `"RACIAL ABILITIES"` + subtle `[K]` hint. |
| **Party strip** | Reuse or extract shared component from `InventoryPartyStripView` (portraits, crown, F-labels, selection border). **No** party aggregate / gold / mana mode toggle (inventory-only). |
| **Subtitle** | `{DisplayName} · {Race}`; Barbarian adds `Spirit Imprint` or rank when applicable. |
| **Body** | `ScrollRect` + vertical layout; race router mounts one child view. |
| **Footer** | Collapsed hotkey help (optional `?` expand — match inventory pattern if cheap). |

### 5.2 — Visual style

| Element | Spec |
|---------|------|
| **Panel** | Full-viewport dark glass (~same `panelBackgroundColor` family as inventory). |
| **Typography** | TMP; title 18–20 px bold; section headers 14 px; body 12–13 px muted `#cfd6dd`. |
| **Barbarian accent** | Optional warm amber / totem accent for section headers (distinct from inventory blue-grey). |

### 5.3 — Router

```
RacialAbilitiesUI.Open(focusedIndex)
  → member = party[focusedIndex]
  → switch (member.stats.race, member.stats.racialSubsystem)
      Barbarian + SpiritImprint → BarbarianSpiritImprintBodyView
      Elf + ElfElementalContracts → ElfElementalSpiritBodyView ([Elf menu doc](../RacialSystem/Elf-Racial-Abilities-Menu-Requirements.md))
      Human + Mage + HumanSpecialization → HumanMageSpellBodyView ([Human Mage menu doc](../RacialSystem/Human-Mage-Racial-Abilities-Menu-Requirements.md))
      Human + Knight + HumanSpecialization → HumanKnightSkillBodyView ([Human Knight menu doc](../RacialSystem/Human-Knight-Racial-Abilities-Menu-Requirements.md))
      default → DefaultRacialAbilitiesBodyView
```

---

## 6. Default race body (non-Barbarian v0)

Placeholder only — no mechanical lists until each race’s reference doc ships.

```
┌ scroll ──────────────────────────────────────────┐
│  {RACE} · {DisplayName}                          │
│  ─────────────────────────────────────────────   │
│        [ faint race emblem / silhouette ]        │
│                                                  │
│   Racial ability reference is not available      │
│   for {Race} yet.                                │
│                                                  │
│   Future updates will show {race-specific        │
│   one-liner from table below}.                   │
└──────────────────────────────────────────────────┘
```

| Race | Placeholder subtitle (v0 copy) |
|------|--------------------------------|
| Human + **Mage** | [Human Mage — grimoire & prepared spells](../RacialSystem/Human-Mage-Racial-Abilities-Menu-Requirements.md) when `HumanClass.Mage`; otherwise class placeholder. |
| Human + **Knight** | [Human Knight — skill tree & auras](../RacialSystem/Human-Knight-Racial-Abilities-Menu-Requirements.md) when `HumanClass.Knight`. |
| Dwarf | Ancestor path and common abilities — coming soon. |
| Elf | [Elemental spirit contracts](../RacialSystem/Elf-Racial-Abilities-Menu-Requirements.md) — roster reference + nicknames. |
| Tiefling | [Cyborg implants — racial menu body](Tiefling-Racial-Abilities-Menu-Requirements.md) — slot grid + detail pane; change at [Fleshmetal Forgemaster](../RacialSystem/Tiefling-Fleshmetal-Forgemaster-NPC-Requirements.md). |
| Undead / Beastman / other | Racial abilities — coming soon. |

**Rules:** No fake data; no links to hotbar; no Shaman / NPC callouts unless race-specific doc requires it later.

---

## 7. Barbarian body — Spirit Imprint (read-only)

### 7.1 — Pinned summary (inside scroll, first block)

| Field | Source |
|-------|--------|
| **Title** | `SPIRIT IMPRINT` |
| **Rank** | `SpiritImprintRuntime` derived rank (`chosenPathNodeIds.Count - 1` when path starts at root — Phase 3 invariant). |
| **Path depth** | `{Count} marks committed` (include root in count if shown as first card). |

**Read-only banner (required, exact intent):**

> View only — visit the **Shaman Barbarian** in town to extend your imprint by one mark.

No button; no link teleport. Name matches [Shaman NPC doc](../RacialSystem/Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) display name.

### 7.2 — Section: Path timeline

Heading: **`SPIRIT IMPRINT PATH`** (root → current tail).

Render **committed nodes in path order** as **expanded node cards** in `SpiritImprintTimelineView` (icon, gold left accent, ACTIVE badge, body sections). Connect visually with a vertical timeline rail (dot + line per row — not an interactive graph).

After each committed tier that has **foreclosed siblings**, render **ghost cards** immediately below that tier (§8.2) before continuing to deeper committed nodes. Ghost cards use reduced opacity and exclusivity copy only.

**Do not** render children of unchosen branches (hidden per §8.3).

### 7.3 — Missing runtime

| Condition | Body |
|-----------|------|
| Not `Race.Barbarian` | Router should not mount this view (default body). |
| No `SpiritImprintRuntime` or null graph | Single message: *“Spirit imprint is not awakened on this character.”* |
| Invalid path (runtime fallback) | Show whatever path runtime normalized to; log warning in dev. |

---

## 8. Node visibility (locked hybrid)

### 8.1 — Tiers

| Tier | ID | Show? | Interaction | Content |
|------|-----|-------|-------------|---------|
| **1 — Committed** | On `chosenPathNodeIds` | **Yes — full card** | Read-only expand (always expanded v0) | Icon, title, description, stats, passives, actives |
| **2 — Foreclosed** | Exclusive sibling not taken | **Yes — ghost row** | Non-interactive | Title + exclusivity line only |
| **3 — Hidden** | All other unreached nodes | **No** | — | — |

### 8.2 — Foreclosed ghost rules

A node **N** is a **foreclosed ghost** when **all** of:

1. **N** is **not** in `chosenPathNodeIds`.
2. **N** has `siblingExclusivityGroup != 0`.
3. Some **committed** node **C** shares the same `parentNodeId` as **N** and the same `siblingExclusivityGroup`.
4. **C** is on the path (player took the other branch).

**Placement:** Ghost rows appear **immediately after** the committed parent tier’s card block (siblings of the chosen child), not at the bottom of the scroll.

**Ghost row copy (v0 template):**

```
○ {displayName} — Not chosen (exclusive with {chosenSiblingDisplayName}).
  New marks only at the Shaman Barbarian.
```

- **No** description, stats, passives, actives, or costs on ghosts.
- **Opacity ~40%**, dashed or hollow timeline node; **no** expand chevron.

**Example:** Path `root → tier1_str → tier2_constitution`. Ghost: `First Mark — Dexterity` after tier 1 block. `tier2_constitution` children not yet taken remain **hidden**, not ghosts, until one is committed or foreclosed at that tier.

### 8.3 — Hidden rules

Hide any node that is:

- Not on `chosenPathNodeIds`, **and**
- Not a foreclosed ghost per §8.2.

Includes: deep unreached descendants, non-exclusive siblings never offered alongside a pick, entire subtrees below foreclosed branches.

### 8.4 — Deferred: next-mark teasers (v0.1)

Optional future block **`NEXT AT SHAMAN`**: silhouettes from `SpiritImprintUpgradeLogic.GetNextNodeOffers` — **title + one-line description only**, **no costs**. **Out of v0** to avoid duplicating Shaman offer UI.

---

## 9. Node card content (committed only)

### 9.1 — Header row

| Element | Source |
|---------|--------|
| **Icon** | v0: shared `Spirit Imprint` emblem; v0.1+: optional per-node icon field on `SpiritImprintNodeData`. |
| **Title** | `displayName` (fallback `nodeId`). |
| **Badge** | `ACTIVE` for all committed cards; root may add `DORMANT` sublabel when `!HasGameplayPayload()`. |

### 9.2 — Description

`SpiritImprintNodeData.description` — full `[TextArea]` text.

### 9.3 — Stats & resistances (when present)

| Section | Source | Format |
|---------|--------|--------|
| **STATS** | `statModifiers` | Bullet list: `+N {Attribute}` |
| **RESISTANCES** | `resistanceModifiers` | Bullet list per modifier type |

Omit empty sections entirely.

### 9.4 — Passives

Section header: **`PASSIVES (n)`** — if `n == 0`, show `— none —` in muted text.

Each passive row:

| Column | Source |
|--------|--------|
| **Icon** | v0: generic passive glyph; future: field on `PassiveEffect` if added. |
| **Title** | `PassiveEffect` asset **name** (ScriptableObject name) until a `displayName` field exists. |
| **Description** | `PassiveEffect.effectDescription` |

### 9.5 — Actives

Section header: **`ACTIVES (n)`** — if `n == 0`, show `— none —`.

Each active row:

| Column | Source |
|--------|--------|
| **Icon** | `AbilityAction.hotbarIcon` (fallback: generic active glyph). |
| **Title** | `AbilityAction.abilityName` |
| **Description** | `AbilityAction.description` |
| **Meta line** | Soul / magic / divine costs + cooldown when non-zero (same facts as hotbar tooltip). |
| **Footnote** | Muted: *Assign on the ability hotbar to use in combat.* |

**No** click-to-fire; **no** drag to hotbar in v0.

---

## 10. View-model builder (Barbarian)

Suggested static builder (implementation hint, not API lock):

```
BarbarianSpiritImprintViewModel.Build(BaseActor member)
  → runtime = member.GetComponent<SpiritImprintRuntime>()
  → path = runtime.ChosenPathNodeIds in order
  → committedCards = map path ids → NodeCardModel
  → ghosts = foreach committed node C:
        foreach sibling in graph.GetDirectChildren(C.parentNodeId):
          if sibling is foreclosed per §8.2 → GhostRowModel
  → orderedSections = interleave cards + ghosts by tree walk from root
```

**Validation:** Builder must use the same graph instance as runtime (`runtime.Graph`).

---

## 11. Integration

| System | Rule |
|--------|------|
| **PartyManager** | `partyMembers` order for strip; subscribe to roster changes if members can die mid-session. |
| **PartyRacePortraitCatalog** | Same portrait resolution as inventory / party HUD. |
| **SpiritImprintRuntime** | Read-only; refresh body when menu opens (not every frame). |
| **Ability hotbar** | Independent; racial actives may appear on hotbar when assigned — menu lists definitions from node data regardless of hotbar binding. |
| **Shaman** | After successful upgrade, next open of menu reflects new path without scene reload. |

---

## 12. Backend gaps (v0 — resolved)

| Gap | Status |
|-----|--------|
| **Input action** | `ToggleRacialAbilities` on **`K`** in `GameControls.inputactions` + `InputHandler`. |
| **UI shell** | `RacialAbilitiesUI` full-screen overlay (v0.1 visual pass). |
| **Party strip** | `RacialAbilitiesPartyStripView` — portrait chips + F-key badges via `PartyRacePortraitCatalog`. |
| **Timeline cards** | `SpiritImprintTimelineView` — committed + ghost node cards with timeline rail. |
| **Race router** | Inline routing in `RacialAbilitiesUI` (interface deferred). |
| **View model** | `BarbarianSpiritImprintViewModel` + unit tests. |
| **Modal exclusivity** | Inventory / quest journal / racial menus close peers on open. |

---

## 13. Acceptance criteria (v0)

| ID | Test |
|----|------|
| **A1** | **`K`** opens/closes menu; **`Esc`** closes when open. |
| **A2** | Party strip shows all living members; clicking portrait or **F2** changes body to that member without closing menu. |
| **A3** | Focused member defaults to **`GetActiveMember()`** index on first open. |
| **A4** | Non-Barbarian focused member shows default placeholder; no imprint cards. |
| **A5** | Barbarian with sample path shows **committed** nodes in order with title, description, passives, actives per node data. |
| **A6** | Barbarian with Strength-first path shows **Dexterity first mark** as **ghost** after tier 1; no payload on ghost. |
| **A7** | Unreached deep nodes (e.g. tier-3 not yet bought) are **not** visible anywhere in scroll. |
| **A8** | No buttons to purchase, respec, or append nodes; banner references Shaman only. |
| **A9** | Menu open does not mark turn acted; blocked under dialog modal gate. |
| **A10** | Opening inventory closes racial menu and vice versa (v0 mutual exclusivity). |

---

## 14. Implementation phases

| Phase | Scope |
|-------|-------|
| **v0** | Shell, **`K`**, party strip, default placeholder, Barbarian read-only path, scroll + text cards |
| **v0.1** | **Done** — full-screen layout, TMP chrome, portrait party strip, timeline node cards + ghost styling (§5.1) |
| **v0.2** | Optional **next-at-Shaman** silhouettes (§8.4); passive/active row icons from assets; per-node imprint icons |
| **v1** | Dwarf reference bodies; **Human Mage body:** [Human Mage — racial abilities menu](../RacialSystem/Human-Mage-Racial-Abilities-Menu-Requirements.md); **Elf body:** [Elf — racial abilities menu](../RacialSystem/Elf-Racial-Abilities-Menu-Requirements.md); **Tiefling body:** [Tiefling — racial abilities menu](../RacialSystem/Tiefling-Racial-Abilities-Menu-Requirements.md) |
| **v1.1** | Persist last focused member; hover tooltips on truncated descriptions |

---

## 15. Resolved decisions

| # | Question | Resolution |
|---|----------|------------|
| **Q1** | Hide all unreached nodes? | **Hybrid:** hide except **foreclosed exclusive siblings** (§8). |
| **Q2** | Show full imprint tree? | **No** — reference sheet, not planner; Shaman owns next mark. |
| **Q3** | Show unlock costs in menu? | **No** — Shaman dialog only. |
| **Q4** | Hotkey? | **`K`** (locked §4.1). |
| **Q5** | Change imprint here? | **No** — read-only (locked §7.1). |
| **Q6** | Focused member vs active member? | **Independent** in v0 (inventory-style browse). |
| **Q7** | Next-mark preview at bottom? | **Deferred v0.1** (§8.4). |

---

## 16. Cross-references to update when implemented

- [Phase 3 — Barbarian Spirit Imprint](../RacialSystem/Phase3-Requirements.md) §G3 — link this menu as player-facing inspect UI.
- [Barbarian Spirit Imprint — Shaman NPC](../RacialSystem/Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) — clarify menu vs dialog responsibilities (§3 above).
- [Ability hotbar](Ability-Hotbar-Requirements.md) — racial actives reference vs execution.
- [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) — shared party strip pattern.

---

## 17. Document history

| Date | Change |
|------|--------|
| 2026-06-07 | Initial draft — shell, **`K`**, default body, Barbarian read-only Spirit Imprint, hybrid node visibility (committed + foreclosed ghosts + hide rest). |
