# Character equipment menu — Requirements

**Purpose:** Specify a **party-scoped character sheet** opened by hotkey where the player inspects **what each party member currently has equipped** — weapons, armor, accessories, and **essence slots** — with **icon-backed slot cells** and a **pinned bottom detail pane** for explanations. v0 is **read-only** (equip/unequip remains in [Inventory UI](../Inventory/Inventory-UI-Redesign-Requirements.md)).

**Status:** Implemented (v0).

**Visual mock:** [`Docs/UI/character-equipment-menu-mock.png`](character-equipment-menu-mock.png) (companion to §5).

**Depends on:** `PartyManager`, `BaseActor`, `CharacterStats`, `EquipmentManager`, `EssenceSlotManager`, `ItemData` / `ItemInstance`, `EssenceData`, `InventoryDetailFormatter`, `PortraitResolver` / `PartyRacePortraitCatalog`, `InputHandler` / `GameControls`, `GameplayModalGate`, [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (detail formatting, party strip pattern), [Racial abilities menu](Racial-Abilities-Menu-Requirements.md) (full-screen shell, party browse), [Party control HUD](Party-Control-HUD-Requirements.md) (portrait catalog, F-key semantics), [Ability hotbar](Ability-Hotbar-Requirements.md) (item/essence actives listed for reference only).

**Related:** [Subspace inventory & encumbrance](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md) (inventory doc deferred **Character screen** to this menu), [Human class powers](../RacialSystem/Human-Class-Powers-Requirements.md) (essence slot count by class).

**Explicitly out of scope (v0):** Equip, unequip, swap, or drop from this menu; drag-and-drop; inventory list embedded in the sheet; full **aggregated stat block** (STR/DEX totals); animated paper-doll character art; gamepad layout; persisting last-focused member across sessions; compare-to-bag-item; appraisal actions; shop integration; binding actives to hotbar (hotbar remains binding UI).

---

## Locked decisions (proposed — confirm before implementation)

| # | Decision |
|---|----------|
| **L1** | **Hotkey:** **`C`** toggles menu (`ToggleCharacterEquipment`). **`Esc`** closes. |
| **L2** | **Party browse:** Same pattern as racial / inventory — portrait strip + **F1–F5** while open; **focused member ≠ active member** (browse only). |
| **L3** | **Read-only v0:** Menu **shows** equipment and essences; **changing** loadout stays in inventory (`Equip` / essence pickup flows). |
| **L4** | **Layout:** Full-screen overlay; **equipment slot grid** (paper-doll layout) + **essence panel** in the middle; **detail pane pinned to bottom** (~30% height). |
| **L5** | **Selection:** Click (or keyboard focus later) on an **occupied or empty slot** or **essence cell** updates the bottom detail pane. Default selection on open: **Main Hand** if occupied, else first occupied slot, else Main Hand empty. |
| **L6** | **Aesthetic:** Dark glass chrome consistent with inventory, racial menu, party HUD (`RacialUiTheme` palette). |
| **L7** | **Modal exclusivity (v0):** Opening inventory, quest journal, or racial menu closes this menu and vice versa. |
| **L8** | **Icons:** Items use `ItemData.icon`; essences use `EssenceData.mapIcon` (fallback: generic essence glyph). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **At-a-glance loadout** — Player sees worn gear and essences without opening inventory filters. |
| **G2** | **Per-member sheet** — Party strip switches the entire body to that actor’s equipment + essences. |
| **G3** | **Slot clarity** — Every `EquipmentSlot` has a labeled cell; empty slots are visible ghosts, not hidden. |
| **G4** | **Essence clarity** — Show **all** slots from `EssenceSlotManager.totalSlots` (class-dependent); empty essence slots visible. |
| **G5** | **Explain on select** — Bottom pane spells out stats, passives, actives, inscription, and essence description for the selected cell. |
| **G6** | **Icon fidelity** — Item and essence cells show real sprites when authored. |
| **G7** | **No duplicate equip UX** — Inventory remains the single place to change loadout in v0. |
| **G8** | **Modal-safe** — Does not consume a turn; respects `GameplayModalGate`. |
| **G9** | **Reuse formatters** — Item detail reuses `InventoryDetailFormatter`; essences get a parallel formatter (§9). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Character equipment menu** | Full-screen overlay toggled by **`C`**. |
| **Focused member** | Party member whose sheet is displayed; strip / F-keys while open. |
| **Active member** | `PartyManager.GetActiveMember()` — map control; independent unless synced later. |
| **Paper-doll layout** | **Slot grid** arranged like a character silhouette (BG3 / Diablo sheet), not necessarily illustrated body art in v0. |
| **Equipment cell** | One UI cell bound to an `EquipmentSlot` value. |
| **Essence cell** | One UI cell bound to an essence slot index (`0 … totalSlots-1`). |
| **Detail pane** | Bottom **pinned** explanation region; content driven by current selection. |
| **Empty cell** | Slot with no equipped item / essence — dashed border, slot label, short hint in detail pane. |

---

## 3. Screen responsibilities (locked)

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Character equipment menu (`C`)** | Browse party loadouts; read item/essence details | Equip, unequip, use, drop, or rebind hotbar |
| **Inventory (`I`)** | Equip/unequip from bag; full item management | See paper-doll layout (by design — §2 inventory doc L1) |
| **Ability hotbar** | Bind and use actives in combat | Replace this menu’s reference role |

---

## 4. Input & hotkey

### 4.1 — Toggle

| Action | Binding | Notes |
|--------|---------|-------|
| **ToggleCharacterEquipment** | **`C`** | Add to `GameControls.inputactions`; wire in `InputHandler` like `ToggleRacialAbilities`. |
| **Close** | **`C`** (toggle) or **`Esc`** | |

**Footer copy (v0):** `C — character · Esc — close · F1–F5 — focus member`

### 4.2 — While menu open

| Input | Behavior |
|-------|----------|
| **F1–F5** | Focus party member at index (same as racial menu). |
| **Click portrait** | Focus that member. |
| **Click equipment cell** | Select slot; refresh detail pane. |
| **Click essence cell** | Select essence slot; refresh detail pane. |
| **Esc** | Close menu. |

**Does not** change `PartyManager` active member or issue floor commands.

---

## 5. Visual layout (mock — authoritative)

Full-screen shell (anchor stretch 0→1, panel α ≈ 0.96). Typography via TMP + `TMP_Settings.defaultFontAsset` (same fix as racial v0.1).

```
┌──────────────── FULL SCREEN ──────────────────────────────────────────────┐
│ CHARACTER · EQUIPMENT                                                       │  title 28
│ [F1 portrait] [F2 portrait] [F3 portrait] …                               │  strip 108
├────────────────────────────────────┬──────────────────────────────────────┤
│ EQUIPMENT                          │ ESSENCES                             │
│                                    │                                      │
│           ┌─────────┐              │  ┌────┐ ┌────┐ ┌────┐               │
│           │  HEAD   │              │  │ E1 │ │ E2 │ │ E3 │  (class count) │
│           └─────────┘              │  └────┘ └────┘ └────┘               │
│    ┌───┐  ┌─────────┐  ┌───┐       │  name + tier under icon              │
│    │Acc│  │  TORSO  │  │Acc│       │                                      │
│    │ H │  │         │  │ H │       │                                      │
│    └───┘  └─────────┘  └───┘       │                                      │
│ ┌──────┐            ┌──────┐     │                                      │
│ │ MAIN │   (silhou)  │ OFF  │     │                                      │
│ │ HAND │              │ HAND │     │                                      │
│ └──────┘            └──────┘     │                                      │
│    ┌───┐              ┌───┐       │                                      │
│    │Acc│              │Acc│       │                                      │
│    │MH │              │OH │       │                                      │
│    └───┘              └───┘       │                                      │
│           ┌─────────┐              │                                      │
│           │  LEGS   │              │                                      │
│           └─────────┘              │                                      │
│           ┌─────────┐              │                                      │
│           │  FEET   │              │                                      │
│           └─────────┘              │                                      │
├────────────────────────────────────┴──────────────────────────────────────┤
│ DETAILS                                                                     │  ~30% height
│ ┌────┐  Iron Longsword — Main Hand                                          │
│ │icon│  Slashing 12 · +1 Strength · Passive: … · Active: …                 │
│ └────┘  (scroll if long — inscription, compare note, essence passives)      │
├─────────────────────────────────────────────────────────────────────────────┤
│ C — character · Esc — close · F1–F5 — focus member                          │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5.1 — Layout tokens

| Token | Value | Notes |
|-------|-------|-------|
| Panel background | `(0.08, 0.085, 0.095, 0.96)` | Match inventory / racial |
| Outer padding | `12px` | |
| Section spacing | `6–8px` | |
| Body split | Equipment **~55%** \| Essences **~45%** | Horizontal split in middle band |
| Middle band height | Flexible (fills space above detail pane) | |
| Detail pane min height | **~28–32%** of panel | Pinned; scroll inside |
| Equipment cell | **64×64px** icon area + label | Selected: gold border |
| Empty cell | Dashed border, muted label | |
| Essence cell | **72×88px** | Icon + name + `T{n}` tier |
| Title / body / footer fonts | **28 / 17 / 15** | Match racial v0.1 scale |

### 5.2 — Equipment slot grid (v0)

All values of `EquipmentSlot` get a cell:

| Slot | Grid position (mock) | Label |
|------|----------------------|-------|
| `Head` | Top center | HEAD |
| `Torso` | Center | TORSO |
| `MainHand` | Left mid | MAIN HAND |
| `OffHand` | Right mid | OFF HAND |
| `Legs` | Lower center | LEGS |
| `Feet` | Bottom center | FEET |
| `Accessory_Head` | Top-right of head | ACC (head) |
| `Accessory_MainHand` | Below main hand | ACC (main) |
| `Accessory_OffHand` | Below off hand | ACC (off) |

**Data source:** `EquipmentManager.EquippedSnapshot` / `GetEquippedInstance(slot)`.

**Cell content (occupied):** `ItemData.icon`, truncated `itemName`, optional `[×qty]` for stacked ammo in off-hand.

**Cell content (empty):** Slot label only; detail pane shows *“Nothing equipped in {slot}.”*

### 5.3 — Essence panel (v0)

| Element | Source |
|---------|--------|
| Slot count | `EssenceSlotManager.totalSlots` (after `ApplyMaxSlotsFromClass`) |
| Occupied | `GetEssenceInSlot(i)` |
| Icon | `EssenceData.mapIcon` |
| Subtitle | `{essenceName}` + `Tier {tier}` |
| Empty | Dashed cell + `ESSENCE {i+1}` label |

**Human class with zero essence slots:** Essence panel shows muted message: *“This class cannot equip essences.”* (from `HumanClassRules.CanGainEssences`).

---

## 6. Detail pane (bottom box)

### 6.1 — Structure

| Region | Content |
|--------|---------|
| **Header row** | Large icon (64–96px) + title + subtitle |
| **Body** | Scrollable rich text (`TextMeshProUGUI`) |

### 6.2 — Item selected (equipment cell)

Reuse existing formatters where possible:

| Section | Source |
|---------|--------|
| Title | `InventoryDetailFormatter.FormatHeroTitle` |
| Subtitle | `FormatHeroSubtitle` (slot, category, rarity hints) |
| Body | `FormatInspectBody` for the equipped `ItemInstance` |

**Do not** show compare-to-equipped block (item **is** equipped). Optional one-line: *“Equipped on {slot}.”*

Include inscription and user marks when present on `ItemInstance`.

### 6.3 — Essence selected

New **`EssenceDetailFormatter`** (or static section in inventory formatters):

| Section | Source |
|---------|--------|
| Title | `essenceName` |
| Subtitle | `Tier {tier}` · slot index |
| Body | `description` + stat/resistance lists + passive names + active ability names/meta |
| Footer note | *“Assign actives on the ability hotbar to use in combat.”* (same intent as racial menu) |

### 6.4 — Empty selection

| Selection | Copy |
|-----------|------|
| Empty equipment slot | *“Nothing equipped in {slot label}. Use Inventory to equip items.”* |
| Empty essence slot | *“No essence in slot {n}. Acquire essences in the dungeon or from events.”* |
| No selection (edge) | *“Select a slot above to view details.”* |

---

## 7. Party strip

Reuse **`RacialAbilitiesPartyStripView`** pattern (or extract shared `PartyMemberStripView`):

- Portraits from `PortraitResolver` / `PartyRacePortraitCatalog`
- Gold focus border on focused member
- F-key badges on portraits
- Truncate names with ellipsis (`DisplayName`, not `GameObject` name)

On open, focused index = `partyMembers.IndexOf(GetActiveMember())` (fallback 0).

---

## 8. Integration

| System | Rule |
|--------|------|
| **PartyManager** | `partyMembers` order for strip |
| **EquipmentManager** | Read-only snapshot on refresh (open + focus change) |
| **EssenceSlotManager** | Read `totalSlots` + per-slot essence on refresh |
| **GameplayModalGate** | `CharacterEquipmentUI.BlocksGameplay` when open |
| **GameOverService** | Force-close on game over |
| **Inventory / racial / quest** | Mutual close on open (v0) |

Refresh body when menu opens and when focused member changes — not every frame.

---

## 9. Backend gaps (implementation prerequisites)

| Gap | Required change |
|-----|-----------------|
| **Input action** | `ToggleCharacterEquipment` on **`C`** |
| **UI shell** | `CharacterEquipmentUI` + bootstrap |
| **Slot grid view** | `EquipmentSlotGridView` — layout + cell factory |
| **Essence panel view** | `EssenceSlotPanelView` |
| **Detail pane view** | `EquipmentDetailPaneView` (hero row + scroll body) |
| **Essence formatter** | `EssenceDetailFormatter` |
| **View model** | `CharacterEquipmentViewModel.Build(BaseActor)` — slot cells + essence cells + default selection |
| **Modal gate / exclusivity** | Wire like racial menu |

---

## 10. Acceptance criteria (v0)

| ID | Test |
|----|------|
| **A1** | **`C`** opens/closes menu; **`Esc`** closes when open. |
| **A2** | Full-screen panel; not a floating card. |
| **A3** | Party strip shows living members; F2 / click changes sheet without closing menu. |
| **A4** | All nine `EquipmentSlot` cells visible; occupied show item icon + name. |
| **A5** | Essence slots match class count; occupied show `mapIcon` + name + tier. |
| **A6** | Clicking Main Hand item populates bottom pane with damage/stats/passives from formatters. |
| **A7** | Clicking empty Legs slot shows empty-slot copy; no equip button. |
| **A8** | Clicking essence shows description and modifiers. |
| **A9** | Menu blocked under dialog modal gate; does not consume turn. |
| **A10** | Opening inventory closes character menu and vice versa. |
| **A11** | Text readable at 1080p (title ≥28, body ≥17, TMP default font assigned). |

---

## 11. Implementation phases

| Phase | Scope |
|-------|-------|
| **v0** | Shell, **`C`**, party strip, slot grid, essence panel, bottom detail pane, read-only |
| **v0.1** | Keyboard slot navigation (arrow keys / tab); selected cell outline without mouse |
| **v1** | Quick **Unequip** action per slot (still no equip-from-sheet); optional aggregate stat sidebar |
| **v1.1** | Illustrated paper-doll silhouette art; drag-from-inventory equip |

---

## 12. Industry reference (what we borrow)

| Game | Pattern |
|------|---------|
| **Baldur’s Gate 3** | Character panel: doll slot grid + item tooltip region |
| **Diablo / POE** | Equipment cells around character; click item for stat block |
| **Pathfinder: Kingmaker** | Separate equipment sheet from bag inventory |
| **Final Fantasy / FF14** | Gear slots + materia/essence-like socket row |

JRogue keeps **inventory = bag management**, **character menu = worn loadout reference** (v0).

---

## 13. Open questions (for user confirmation)

| # | Question | Default if silent |
|---|----------|-------------------|
| **Q1** | Hotkey **`C`** acceptable? | Yes (proposed L1) |
| **Q2** | v0 strictly read-only? | Yes (proposed L3) |
| **Q3** | Include **Legs** and **Feet** slots even if often empty in early content? | Yes — show all enum values |
| **Q4** | Essence panel hidden for non-essence classes or show disabled message? | Show disabled message |

---

*Document version: 2026-06-07 — draft for review before implementation.*
