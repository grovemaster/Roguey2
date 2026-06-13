# Tiefling — Racial abilities menu (Cyborg implants)

**Purpose:** Specify the **Tiefling body** of the shared [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (`K`): a **read-only reference sheet** for every **implant slot** on the focused Tiefling, using an **equipment-menu-style layout** — implant **slot grid** plus a **pinned bottom detail pane** that fills when the player selects a slot.

**Status:** Implemented (v0).

**Visual mock:** [`Docs/RacialSystem/tiefling-racial-abilities-menu-mock.png`](tiefling-racial-abilities-menu-mock.png) (companion to §8).

**Depends on:** [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (shell, **`K`**, party strip, modal rules), [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) (`ImplantSlot`, `CyborgImplantDefinition`, `TieflingImplantsRuntime`, `IRacialProgressionPayload`), [Tiefling — Fleshmetal Forgemaster NPC](Tiefling-Fleshmetal-Forgemaster-NPC-Requirements.md) (town install/replace/remove), [Character equipment menu](../UI/Character-Equipment-Menu-Requirements.md) (slot grid + bottom detail pane pattern), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (implant actives assignables).

**Related:** [Barbarian Spirit Imprint menu](../UI/Racial-Abilities-Menu-Requirements.md) (read-only timeline reference), [Elf — racial abilities menu](Elf-Racial-Abilities-Menu-Requirements.md) (read-only roster cards + nickname edit).

**Explicitly out of scope (v0):** Install, replace, remove, or pay costs from this menu; showing **Forgemaster prices**; respec shortcuts; drag implant actives to hotbar; editing implants; gamepad layout; persisting last-focused party member across sessions; illustrated 3D body art; comparing implants side-by-side; catalog of **unowned** grafts (only **installed** state).

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Tiefling body mounts when focused member is `Race.Tiefling` with `RacialSubsystemKind.TieflingImplants`. |
| **L2** | **Read-only reference** — same discipline as Barbarian / Elf bodies. Banner points player to **Fleshmetal Forgemaster** for changes. |
| **L3** | **Layout mirrors equipment menu (§8):** middle band = **implant slot grid** around a silhouette; **bottom ~30%** = **DETAILS** pane driven by selection. |
| **L4** | **All seven** `ImplantSlot` values always visible. Empty slots use dashed ghost cells (not hidden). |
| **L5** | **Selection:** click (later: keyboard focus) on any slot cell updates detail pane. **Default on open:** `LeftArm` if occupied, else first occupied slot in fixed slot order, else empty `LeftArm`. |
| **L6** | **Folk baseline** (Fire resist, horns) shown in a compact **read-only** block above the grid — not selectable as an implant slot. |
| **L7** | Menu refresh on open and when focused member changes — not every frame. |
| **L8** | **Aesthetic:** `RacialUiTheme` dark glass; Tiefling section accent warm amber / ember (distinct from Barbarian totem gold and equipment blue-grey). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Loadout at a glance** — Player sees which grafts are installed in which body locations without opening Forgemaster dialog. |
| **G2** | **Equipment parity** — Selecting a slot feels like the **`C`** character sheet: icon, title, stats, passives, actives in the bottom pane. |
| **G3** | **Barbarian / Elf parity** — Information-only; town NPC owns progression. |
| **G4** | **Empty slot clarity** — Unfilled locations are visible; detail pane explains the slot is empty and where to get grafts. |
| **G5** | **No duplicate systems** — Menu does not replace Forgemaster dialog, hotbar assign/use, or dev swap tools. |
| **G6** | **Payload completeness** — Detail pane surfaces the same progression facts as `CyborgImplantDefinition` / `IRacialProgressionPayload` (stats, resistances, passives, actives, benefits, restrictions). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Tiefling body** | Race-specific panel in `RacialAbilitiesUI` when focused member is Tiefling with implant subsystem. |
| **Implant cell** | One UI cell bound to an `ImplantSlot` enum value. |
| **Occupied cell** | Slot with an installed `CyborgImplantDefinition` from `TieflingImplantsRuntime`. |
| **Empty cell** | Slot with no implant — dashed border, slot label only. |
| **Detail pane** | Bottom pinned explanation region; content driven by selected implant cell (or empty-slot copy). |
| **Folk baseline block** | Read-only summary of `RacialLoadoutApplier` effects (not an implant slot). |

---

## 3. Screen responsibilities (locked)

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Racial menu — Tiefling body** | Browse slots; read installed graft details | Install, replace, remove, pay, or assign hotbar |
| **Fleshmetal Forgemaster** | Install / replace / remove (with costs) | Replace full reference sheet (compact summary in dialog only) |
| **Ability hotbar** | Assign and use implant actives in combat | Show full implant passive reference |

**Read-only banner (required intent):**

> View only — visit the **Fleshmetal Forgemaster** in town to install or change grafts.

No buttons; no teleport links.

---

## 4. Router integration

Extends parent [§5.3 router](../UI/Racial-Abilities-Menu-Requirements.md):

```
RacialAbilitiesUI.RefreshBodyForFocusedMember()
  → …
  Race.Tiefling + TieflingImplants → TieflingImplantBodyView
  (else default placeholder)
```

| Condition | Body |
|-----------|------|
| `Race.Tiefling` + `TieflingImplants` + `TieflingImplantsRuntime` | **Implant slot grid + detail pane** (this doc) |
| `Race.Tiefling` but no runtime / wrong subsystem | Default placeholder with Tiefling-specific copy |
| Not Tiefling | Default placeholder (unchanged) |

---

## 5. Data sources

| UI region | Runtime / data |
|-----------|----------------|
| **Slot occupancy** | `TieflingImplantsRuntime.InstalledSnapshot` (`ImplantSlot` → `CyborgImplantDefinition`) |
| **Implant payload** | Selected `CyborgImplantDefinition` via `IRacialProgressionPayload` fields |
| **Folk baseline** | `RacialLoadoutApplier.Loadout` on same actor (`DefaultTieflingRacialLoadout`: Fire resist, etc.) |
| **Icons (v0)** | Optional future `CyborgImplantDefinition.icon`; fallback generic graft emblem per slot |
| **Actives on hotbar** | Independent — menu lists definition; footnote points to ability hotbar |

**Do not** read Forgemaster catalog for uninstalled offers — menu reflects **installed state only**.

---

## 6. Folk baseline block (read-only)

Pinned **above** the implant grid (not a selectable cell).

| Content | Source |
|---------|--------|
| **Section label** | `FOLK BASELINE` |
| **Fire resistance** | `RacialLoadoutDefinition.resistanceModifiers` (e.g. Fire +10) |
| **Horns** | `CharacterStats.bodyCapabilities` includes `Horns` → muted line: *Cannot equip horn-excluding helmets.* |
| **Other loadout passives** | `RacialLoadoutDefinition.passiveEffects` names + descriptions (if any) |

**Rule:** Folk baseline is **never** mixed into implant detail pane unless product later adds a dedicated “Baseline” pseudo-selection (out of v0).

---

## 7. Implant slot grid

### 7.1 — Slot inventory (all seven)

| `ImplantSlot` | Grid position (mock) | Cell label |
|---------------|----------------------|------------|
| `Head` | Top center | HEAD |
| `LeftArm` | Upper left | LEFT ARM |
| `RightArm` | Upper right | RIGHT ARM |
| `Torso` | Center | TORSO |
| `Heart` | Upper center-left (near torso) | HEART |
| `LeftLeg` | Lower left | LEFT LEG |
| `RightLeg` | Lower right | RIGHT LEG |

Faint **silhouette** behind cells (same role as equipment menu paper-doll — positioning aid, not illustrated art in v0).

### 7.2 — Cell content

| State | Visual | Subtitle under icon |
|-------|--------|-------------------|
| **Occupied** | Graft icon (or slot emblem fallback) + gold thin border | Truncated `displayName` (fallback `implantId`) |
| **Empty** | Dashed border, muted | Slot label only |
| **Selected** | Thick gold / silver selection outline (same token as equipment menu) | unchanged |

**Slot order for “first occupied” default (§L5):**  
`LeftArm`, `RightArm`, `Torso`, `Heart`, `Head`, `LeftLeg`, `RightLeg`.

### 7.3 — Interaction

| Input | Behavior |
|-------|----------|
| **Click implant cell** | Set selection; rebuild detail pane |
| **Click empty cell** | Set selection; detail pane shows empty-slot copy (§9.3) |
| **Change focused party member** | Rebuild grid + detail; re-run default selection rule |

No drag-and-drop in v0.

---

## 8. Visual layout (mock — authoritative)

Uses same **full-screen racial shell** as parent doc (title, banner, party strip, footer). **Middle + bottom** replace Barbarian timeline / Elf scroll cards.

```
┌──────────────── FULL SCREEN ──────────────────────────────────────────────┐
│ RACIAL ABILITIES                                                            │
│ View only — visit the Fleshmetal Forgemaster in town…                       │  banner
│ [F1] [F2] [F3] [F4] … party strip                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│ FOLK BASELINE · Fire resist +10 · Horns (no horn-blocking helmets)          │  compact strip
├─────────────────────────────────────────────────────────────────────────────┤
│ CYBORG IMPLANTS                          (flexible height ~45–50%)          │
│           ┌─────────┐                                                         │
│           │  HEAD   │  (empty)                                               │
│           └─────────┘                                                         │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐                                     │
│  │LEFT ARM │  │  TORSO  │  │RIGHT ARM│                                     │
│  │ Iron    │  │ Thoracic│  │ (empty) │                                     │
│  │ Sleeve ●│  │  Plate  │  │         │                                     │
│  └─────────┘  └─────────┘  └─────────┘                                     │
│      ┌───┐                    ┌───┐                                         │
│      │HEART│   (silhouette)    │   │                                         │
│      └───┘                    └───┘                                         │
│  ┌─────────┐              ┌─────────┐                                       │
│  │LEFT LEG │              │RIGHT LEG│                                       │
│  └─────────┘              └─────────┘                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│ DETAILS                                    (~28–32% height, scroll inside)  │
│ ┌────┐  Iron Sleeve — Left Arm                                               │
│ │icon│  Reinforced left-arm cybernetic sleeve.                                │
│      │  STATS · PASSIVES · ACTIVES · (benefits/restrictions if any)         │
├─────────────────────────────────────────────────────────────────────────────┤
│ K — racial abilities · Esc — close · F1–F5 — focus member                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 8.1 — Layout tokens

| Token | Value | Notes |
|-------|-------|-------|
| Panel background | `(0.08, 0.085, 0.095, 0.96)` | Match inventory / racial / equipment |
| Outer padding | `12px` | |
| Folk baseline strip height | `~40–48px` | Single TMP row + optional wrap |
| Middle band | Flexible; min ~320px | Slot grid centered |
| Implant cell | **72×88px** icon area + label | Match essence cell scale |
| Empty cell | Dashed border α ~0.45 | |
| Selected cell | Gold border `(0.91, 0.77, 0.28)` | Same as equipment menu |
| Detail pane min height | **~28–32%** of panel | Pinned; scroll body |
| Section label | `CYBORG IMPLANTS` · 20px bold | Ember accent optional |
| Typography | Title 28 / banner 17 / detail title 21 / body 17 / footer 15 | TMP + default font asset |

---

## 9. Detail pane (bottom box)

### 9.1 — Structure

| Region | Content |
|--------|---------|
| **Header row** | `DETAILS` divider (optional) + large icon (64–96px) + title + slot subtitle |
| **Body** | Scrollable rich text or structured sections (`TextMeshProUGUI`) |

### 9.2 — Occupied implant selected

| Block | Source | Format |
|-------|--------|--------|
| **Title** | `{displayName}` (fallback `implantId`) | Bold |
| **Subtitle** | `{slot display name}` — e.g. `Left Arm` | Muted |
| **Description** | `CyborgImplantDefinition.description` | Full `[TextArea]` text |
| **STATS** | `statModifiers` | Bullet: `+N {Attribute}`; omit if empty |
| **RESISTANCES** | `resistanceModifiers` | Bullet per type; omit if empty |
| **PASSIVES** | `passiveEffects` | Header `PASSIVES (n)`; each: name + `effectDescription`; `— none —` if empty |
| **ACTIVES** | `activeAbilities` | Header `ACTIVES (n)`; each: `abilityName`, description, SP/cooldown meta; footnote: *Assign on the ability hotbar to use in combat.* |
| **BENEFITS** | `racialBenefits` | Only if list non-empty; formatted like Barbarian benefit rows |
| **RESTRICTIONS** | `racialRestrictions` | Only if list non-empty |

**Do not show:** `installCost`, `removeCost`, `allowedSlots` validation, internal `implantId` (except debug), Forgemaster catalog rows for unowned grafts.

### 9.3 — Empty slot selected

```
{Slot display name} — Empty

No graft installed in this location.

Visit the Fleshmetal Forgemaster in town to install a cyborg implant here.
```

Icon: generic empty-slot glyph or slot emblem at reduced opacity.

### 9.4 — Missing runtime

| Condition | Body |
|-----------|------|
| Tiefling without `TieflingImplantsRuntime` | *“This character cannot host fleshmetal grafts.”* |
| Wrong subsystem | Default placeholder |

---

## 10. View-model builder

Suggested API (implementation hint):

```
TieflingImplantBodyViewModel.Build(BaseActor tiefling)
  → runtime = tiefling.GetComponent<TieflingImplantsRuntime>()
  → loadout = tiefling.GetComponent<RacialLoadoutApplier>()?.Loadout
  → slots = all ImplantSlot values in UI order
  → cells = slots.Select(slot → ImplantSlotCellModel {
        Slot, IsOccupied, Implant, Icon, Label, IsSelected })
  → detail = BuildDetail(selectedSlot, runtime, loadout)
  → folkBaseline = BuildFolkBaselineSummary(loadout, stats)
```

Unit tests cover: default selection order, occupied vs empty detail copy, payload sections populated from sample `IronSleeveArm` / `ThoracicPlate` assets.

---

## 11. Integration

| System | Rule |
|--------|------|
| **RacialAbilitiesUI** | Router mounts `TieflingImplantBodyView`; refresh on open / focus change. |
| **TieflingImplantsRuntime** | Read-only `InstalledSnapshot`; no mutation from menu. |
| **Forgemaster transactions** | After install/replace/remove, next menu open reflects new state (same as Shaman → Barbarian menu). |
| **HotbarAssignabilityService** | Unchanged; implant actives listed when installed (see hotbar doc). |
| **AbilityHotbarUI** | No change required for menu v0; Forgemaster already calls `RefreshAll()` after transactions. |

---

## 12. Acceptance criteria

| ID | Test |
|----|------|
| **A1** | Focus Tiefling with `LeftArm` + `Torso` implants → both cells show names/icons; five other slots dashed empty. |
| **A2** | Select **Iron Sleeve** cell → detail pane shows +10 STR, passives, **Sudden Strength** active, description. |
| **A3** | Select empty **Heart** cell → empty-slot copy; no stats/actives from other slots. |
| **A4** | Folk baseline strip shows Fire resist + horns note; not duplicated inside implant detail. |
| **A5** | No install/remove buttons; banner references Forgemaster only. |
| **A6** | Non-Tiefling focused member → default placeholder; Tiefling body hidden. |
| **A7** | After Forgemaster install, reopen menu → new graft appears without scene reload. |
| **A8** | **`K` / Esc / F1–F5** behavior unchanged from parent racial menu doc. |
| **A9** | Opening menu does not consume a turn; blocked under gameplay modal gate. |

---

## 13. Implementation phases

| Phase | Scope |
|-------|-------|
| **v0 (this doc)** | Requirements + mock; `TieflingImplantBodyView` with slot grid, selection, detail pane, folk baseline strip |
| **v0.1** | Per-implant icons on `CyborgImplantDefinition`; keyboard slot focus |
| **v1** | Optional “compare with Forgemaster catalog” teaser silhouettes (read-only, no costs) — defer unless design requests |

---

## 14. Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | §6 Tiefling row → link here; §14 v1 Tiefling body **Done** |
| [Tiefling — Fleshmetal Forgemaster NPC](Tiefling-Fleshmetal-Forgemaster-NPC-Requirements.md) | §14 / checklist — menu doc created |
| [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) | Player-facing inspect UI cross-link |
| [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) | Implant active reference vs execution |

---

## 15. Document history

| Date | Change |
|------|--------|
| 2026-06-13 | v0 implementation — `TieflingImplantBodyViewModel`, slot grid, detail pane, router in `RacialAbilitiesUI`. |
| 2026-06-09 | Initial draft — equipment-style slot grid + detail pane, read-only Forgemaster parity, visual mock. |
