# Ability hotbar — Requirements

Replace the legacy **Primary / Shift / Ctrl + number** ability map with a **visible, per-character action bar** on the gameplay HUD. Each party member owns a **private hotbar layout**; the bar shown always matches **`PartyManager.GetActiveMember()`**. Players **drag-and-drop** abilities and usable items onto **10 keybound slots** (`1`–`9`, `0` on the **main keyboard row**, not numpad) plus an **expandable overflow** region for everything else they know or carry but have not bound. **Overflow entries are clickable** to activate abilities the same way keybound slots are.

**Status:** **Implemented (v0)** — visible hotbar HUD, 10 keybound slots, overflow click-to-use, edit-mode drag-and-drop, tooltips, greyed disabled state, party Give transfer, legacy Shift/Ctrl ability rows removed from input routing.

**Depends on:** `PartyManager`, `PlayerCommandProcessor`, `PlayerAbilitySource`, `EssenceSlotManager`, `EquipmentManager`, `InventoryManager`, `InventoryItemUse`, `HumanMageSpellsRuntime`, racial progression runtimes (`SpiritImprintRuntime`, `DwarfCommonAbilitiesRuntime`, `ElementalSpiritContractsRuntime`, etc.), `GameControls` / `InputHandler`, [Dungeon log](Dungeon-Log-Requirements.md) (`PlayfieldLayout` — hotbar sits above message console), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md), [Evocable items](../Inventory/Evocable-Items-Requirements.md), [Subspace inventory](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md), `InventoryPolicy`, `CombatThreatCoordinator`.

**Related:** [Targeting sight range](../Combat/Targeting-Sight-Range-Requirements.md), [Friendly fire confirmation](../Combat/Friendly-Fire-Confirmation-Requirements.md), [Throwing knife](../Inventory/Throwing-Knife-Requirements.md), [Fireball scroll](../Inventory/Fireball-Scroll-Requirements.md), racial ability docs under `Docs/RacialSystem/`.

**Explicitly out of scope (v0):** Gamepad / touch layouts; user-authored macro sequences; shared party-wide hotbar; hotbar profiles saved across runs (run persistence only in v0); action bar skins marketplace; cooldown **numeric** countdown preference settings; rebinding hotbar keys away from `1`–`0`; dual-wield separate key rows per hand; assigning abilities the character has not unlocked; assigning items from another party member’s inventory without transfer (see §11).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Visible hotbar** — Player sees what each number key does without memorizing essence indices and modifier chords. |
| **G2** | **Per-character layouts** — Each roster member has an independent hotbar; switching active member swaps the displayed bar **and** which bindings fire on key press. |
| **G3** | **10 keybound slots** — Main row bound to **`1`–`9`** and **`0`** (keyboard top row; **not** numpad). |
| **G4** | **Drag-and-drop authoring** — Reorder and assign by dragging between bar slots, overflow pool, and (where allowed) inventory / ability sources. |
| **G5** | **Overflow panel** — Expandable region lists **all assignable** actives the character currently has that are **not** on the main row (or shows full pool while editing — see §7). |
| **G6** | **Unified assignables** — Same bar accepts racial actives, essence actives, equipped-item actives, and **carried** consumables / scrolls / evocables / throwables / inventory-targeted actives. |
| **G7** | **Hold-to-assign rule** — Inventory items may appear on a member’s hotbar **only** if that member **owns** the instance (carried or equipped on self). |
| **G8** | **Gameplay parity** — Pressing a bound key **or clicking** a hotbar/overflow icon invokes the **same** validation, targeting, friendly-fire, safe-zone, turn, and resource rules as today’s `PlayerCommandProcessor` paths. |
| **G9** | **Party item transfer (new)** — Document and implement moving items between party members **out of combat** so hotbar assignment is practical (§11). |
| **G10** | **Icon pipeline** — Every hotbar-visible action has a **dedicated hotbar icon** (§12); fallbacks are explicit, not silent blanks. |
| **G11** | **Disabled affordance** — Entries that **cannot** be activated right now are **greyed out** and **non-activatable** on both the main row and overflow (§8.4). |
| **G12** | **Hover tooltips** — Main row and overflow show a tooltip on hover with the action **name** and **`AbilityAction.description`** (§5.4). |
| **G13** | **Overflow click-to-use** — Player expands overflow, **clicks** an ability icon, and it fires when usable (§6.3). |
| **G14** | **Legacy input retirement** — **Primary / Shift / Ctrl + digit** ability routing is **officially replaced** by the hotbar; Shift/Ctrl ability rows are **removed** (§10.2). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Hotbar** | HUD control showing slot icons + key labels for one character. |
| **Main row** | 10 slots fixed to keys `1`–`0`. |
| **Overflow** | Secondary UI region for unbound or extra assignables; expandable/collapsible. |
| **Hotbar entry** | One slot assignment: ability reference + source metadata + optional item instance id. |
| **Assignable pool** | Everything the focused character may place on the bar right now (§8). |
| **Active member** | `PartyManager.GetActiveMember()` — only this character’s bar is shown and keybound. |
| **Edit mode** | Transient UI state where drag-and-drop is enabled (§7). |
| **Stale slot** | Entry whose item was dropped/consumed/transferred or ability revoked; shown disabled until cleared. |
| **Usable (runtime)** | Entry passes all activation checks (§8.3); icon full color, click/key fires. |
| **Disabled (runtime)** | Entry visible but **cannot** fire now; icon greyed, click/key blocked (§8.4). |
| **Hotbar tooltip** | Hover panel showing action name + description (+ optional costs). |

---

## 3. Industry precedents (what we borrow)

| Game | Pattern JRogue adopts |
|------|---------------------|
| **World of Warcraft** | Horizontal action bar, icon + cooldown overlay + key hint; extra bars collapsed behind chevron; drag from spellbook / bag. |
| **Baldur’s Gate 3** | Bottom-center bar swaps with selected character; drag from spell list / inventory; greyed icons when unusable. |
| **Divinity: Original Sin 2** | Character-specific skill row; item skills mixed with spells; clear separation of “prepared” vs full list. |
| **Final Fantasy XIV** | Expandable cross-hotbar rows — maps to overflow drawer. |
| **Dragon Age: Origins** | Tactical HUD bar per party member when selected. |
| **Dungeon Crawl Stone Soup** | Letter-based ability/item slots — **not** copied literally (JRogue uses `1`–`0`), but **overflow** idea matches DCSS’s many abilities vs few finger keys. |

**Locked aesthetic direction:** CRPG **dark glass** chrome consistent with [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) and [Dungeon log](Dungeon-Log-Requirements.md) — not a sci-fi MMO neon bar.

---

## 4. Current baseline (legacy — being replaced)

| Area | Legacy (until hotbar ships) |
|------|----------------------------|
| **Input** | `PrimaryAbilities` → keys `1`–`0`; `ShiftAbilities` → Shift + same digits (secondary ability index); `CtrlAbilities` → Ctrl + same digits (equipment slot abilities). See `GameControls.inputactions`. |
| **Resolution** | `InputHandler` → `PlayerCommand.AbilitySlot(slotIndex, secondary, fromEquipment)` → `PlayerCommandProcessor.ProcessAbilityInput`. |
| **Essence** | Slot index maps to essence slot; sub-index 0 / 1 from primary vs Shift. |
| **Equipment** | Ctrl path maps index → `EquipmentSlot` via `EquipmentManager.MapIndexToSlot` (7 slots). |
| **Human Mage** | Primary/Shift on slot index ignored; uses `HumanMageSpellsRuntime.GetEquippedAbility(abilityIndex)`. |
| **Inventory use** | Scrolls, potions, evocables, throwables: **inventory UI only** (letter keys / Use action), not number row. |
| **HUD** | **No** action bar UI. |
| **Icons** | `ItemData.icon` exists; `AbilityAction` has **no** icon field; racial / essence abilities inherit item/essence art inconsistently. |
| **Party item move** | **`Give` is a stub** (`InventoryUI.GiveToStub`, `InventoryPolicy.LogCombatTransferStub`). **No** implemented transfer between member inventories. |

**Official change (locked):** The **Primary / Shift / Ctrl + number** scheme is **retired** when this feature ships. All ability and item activation moves to the **hotbar** (keybound slots + overflow clicks). See §10.2.

**Problem statement:** Players must memorize **three parallel binding schemes** plus inventory letters for items — none of it visible on screen, with no greyed-out affordance or description tooltips.

---

## 5. HUD placement & layout (locked for v0)

The hotbar sits on the **gameplay canvas**, **above** the [message console](Dungeon-Log-Requirements.md) and **below** the playfield. It does **not** shrink full-screen modals (inventory, shop, etc.) — modals cover it like the console.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│                              PLAYFIELD (map + actors)                                   │
│                         lead member centered in this region                             │
│                                                                                         │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  ABILITY HOTBAR — active: Aria Thorne                                    [ ≡ Edit ]   │
│  ┌────┬────┬────┬────┬────┬────┬────┬────┬────┬────┐   ┌──┐                           │
│  │ 1  │ 2  │ 3  │ 4  │ 5  │ 6  │ 7  │ 8  │ 9  │ 0  │   │▲ │  overflow toggle          │
│  │ 🗡 │ ✨ │ 🔥 │ 🧪 │ 📜 │    │    │    │    │    │   └──┘                           │
│  └────┴────┴────┴────┴────┴────┴────┴────┴────┴────┘                                    │
│  [ optional compact resource strip: Soul Power · MP · cooldown hint ]                   │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  MESSAGE CONSOLE (recent log lines)                                                     │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

### 5.1 — Slot chrome mock (single cell)

Professional CRPG bars use **frame + icon + overlays**. v0 mock:

```
     ┌──────────────┐
  1  │   ┌──────┐   │  ← key hint (top-left or bottom-left)
     │   │ ICON │   │  ← 48×48 art (64×64 @ 1080p ref, scale with canvas)
     │   └──────┘   │
     │ ▓▓▓▓░░░░░░  │  ← optional cooldown radial / horizontal wipe
     │     3        │  ← stack count (throwables / scroll stacks) when >1
     └──────────────┘
        72×72 px total slot hit target (incl. padding)
```

| State | Visual | Input |
|-------|--------|-------|
| **Empty** | Dark inset frame, faint `+` ghost, key label only. | N/A |
| **Ready (usable)** | Full-color icon. | Key / click **fires** action. |
| **Disabled (not usable)** | **Greyed out** — desaturated icon (~35–50% saturation), muted frame; **no** red “error” styling unless stale. | Key / click **ignored**; tooltip still shows on hover. |
| **Stale** | Greyed icon + warning corner notch (item gone / ability removed). | Treated as disabled; clear in edit mode (§9.4). |
| **Cooldown** | Grey base + dark sweep overlay; optional turn count when tracked. | Disabled until cooldown clears. |
| **Hover** | Bright border on usable entries; muted border on disabled. | Tooltip appears (§5.4). |

**Locked:** “Disabled” always means **greyed out** and **non-activatable**. Do not show full-color icons for actions the player cannot use right now.

### 5.4 — Tooltips (main row + overflow)

Show on **mouse hover** over any **non-empty** main-row slot or overflow icon (including **disabled** entries).

```
                    ┌─────────────────────────────────────┐
                    │  Sudden Strength                    │
                    │  ─────────────────────────────────  │
                    │  Temporarily grants +100 Strength     │
                    │  for 10 turns. Costs 1 Soul Power.  │
                    │                                     │
                    │  [1]   Soul Power: 4                │  ← key hint on main row only
                    └─────────────────────────────────────┘
                              ▲
                         ┌────┐
                         │ ⚡ │  hotbar / overflow icon
                         └────┘
```

| ID | Rule |
|----|------|
| **TT1** | **Primary text:** `AbilityAction.abilityName` (or item name for inventory-only use entries). |
| **TT2** | **Body:** `AbilityAction.description` — required for tooltip content; if empty, show “No description.” |
| **TT3** | **Optional footer:** resource costs (`soulPowerCost`, `magicPowerCost`, …), charges (`2/4`), stack qty, key binding (`[3]`) on main row. |
| **TT4** | **Disabled reason (optional v0.1):** second line when greyed — e.g. “Not your turn”, “Blocked in safe zone”, “Insufficient Soul Power”. v0 minimum: greyed icon + description still visible. |
| **TT5** | Show delay **~0.3 s**; hide on pointer exit. |
| **TT6** | Tooltips work in **play mode** (not only edit mode). |
| **TT7** | While `GameplayModalGate` blocks floor gameplay, hotbar/overflow tooltips may still show if the bar is visible; clicks do not fire. |

### 5.2 — Character swap

When the player presses **F1–F5** (existing party select) or **F** (cycle) and changes active member:

```
Before:  [Aria bar visible]     After:  [Bruenor bar visible]
         keys fire Aria's map            keys fire Bruenor's map
```

No cross-character leakage: pressing `3` always executes **active member’s** slot 3 entry, not a global slot.

### 5.3 — Party control HUD

Portrait strip, F-key labels, acted-state greying, and active-member map highlight are specified in **[Party control HUD](Party-Control-HUD-Requirements.md)**.

---

## 6. Overflow panel (expandable)

### 6.1 — Collapsed (default during combat)

Chevron / `≡` button at right of main row. Combat starts **collapsed** to reduce clutter.

### 6.2 — Expanded mock

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│ OVERFLOW — Aria · drag onto 1–0  ·  Esc or ▼ to collapse                              │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ RACIAL & CLASS                                                                          │
│  ┌────┐ ┌────┐ ┌────┐                                                                   │
│  │ 🐺 │ │ 💢 │ │    │   Ancestor Roar · Intimidate · (empty)                           │
│  └────┘ └────┘ └────┘                                                                   │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ ESSENCE ACTIVES (equipped)                                                              │
│  ┌────┐ ┌────┐                                                                          │
│  │ ⚡ │ │ 👁 │   Sudden Strength · Telekinesis                                           │
│  └────┘ └────┘                                                                          │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ EQUIPMENT ACTIVES                                                                       │
│  ┌────┐                                                                                 │
│  │ 🔆 │   Radiance (helm)                                                                │
│  └────┘                                                                                 │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ INVENTORY — carried by Aria only                                                        │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐                                                            │
│  │ 🗡 │ │ 📜 │ │ 🧪 │ │ 🎐 │   Throwing Knife ×5 · Fireball ×1 · Heal ×2 · Fan 2/2       │
│  └────┘ └────┘ └────┘ └────┘                                                            │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

**Rules:**

| ID | Rule |
|----|------|
| **R6.1** | Overflow lists **assignable pool** grouped by category (§8.2). |
| **R6.2** | Entries **already** on the main row show a **linked** badge or appear dimmed in overflow (pick one in impl; mock shows dimmed duplicate). |
| **R6.3** | Drag from overflow → main row slot **assigns**; drag from main row → overflow **unbinds** (returns to pool only, does not destroy item). |
| **R6.4** | Drag between two main-row slots **swaps** assignments. |
| **R6.5** | **Click-to-use:** left-click on an overflow icon **activates** that entry using the same resolver as a key press (§6.3). |
| **R6.6** | Expand state is **HUD preference** per session (not saved per character). |

### 6.3 — Overflow click-to-use (locked)

Player flow:

1. Click **overflow toggle** (`▲` / chevron) to expand the panel.
2. Click an **ability icon** in any overflow group.
3. If the entry is **usable** (§8.4), dispatch through `HotbarResolver` → `PlayerCommandProcessor` (targeting, friendly-fire, turn spend — same as key press).
4. If **disabled** (greyed), click does **nothing** (no turn spent, no targeting opened).
5. Collapse overflow with **▼**, **Esc**, or second toggle click; collapsing does not cancel in-progress targeting.

**Usability examples:**

| Entry | Usable when | Disabled (greyed) when |
|-------|-------------|-------------------------|
| Essence active | Active member’s turn, `CanExecute`, can afford SP/MP, not safe-zone denied | Wrong turn, insufficient resources, safe zone, cooldown |
| Inventory scroll | Item in owner’s inventory, `InventoryUsability` allows | Item gone, out of combat restriction, ally-only rules |
| Racial active | Unlocked + same checks as essence | Not unlocked, same runtime blocks |

Overflow clicks respect `GameplayModalGate` — no activation through inventory/shop/dialog overlays.

---

## 7. Edit mode & drag-and-drop

| ID | Rule |
|----|------|
| **R7.1** | **Edit** button (or hold `Alt` — pick one) toggles edit mode: slot borders highlight, tooltips show “drop here”. |
| **R7.2** | Edit mode allowed **out of combat** freely; **in combat** allowed for **reordering only** if entry types unchanged — **no** new inventory item assignments mid-fight (locked). |
| **R7.3** | Drag **inventory row** onto hotbar (from open inventory) assigns if owner matches focused member and item is usable. |
| **R7.4** | Invalid drop targets show red X overlay; no-op on release. |
| **R7.5** | Right-click slot in edit mode **clears** binding. |
| **R7.6** | Changes persist to character hotbar state immediately (§9). |

---

## 8. Assignable content & eligibility

### 8.1 — Entry kinds (`HotbarEntryKind`)

| Kind | Source | Invocation path (existing) |
|------|--------|----------------------------|
| `EssenceActive` | `EssenceSlotManager` slot + ability index | `PlayerAbilitySource.Essence` |
| `EquipmentActive` | `EquipmentSlot` + ability index on equipped item | `PlayerAbilitySource.EquipmentItem` |
| `HumanMageSpell` | Spell index in `HumanMageSpellsRuntime` | `PlayerAbilitySource.HumanMageSpell` |
| `RacialActive` | Racial runtime id + ability index (Spirit Imprint node, Dwarf common, **Elf spirit active**, Undead tree, Tiefling implant, etc.) | Unified resolver → `AbilityAction.Execute` |
| `ElementalSpiritSummon` | Elf **`contractInstanceId`** on `ElementalSpiritContractsRuntime` | `ElementalSpiritContractsRuntime.TrySummon` / `TryDismiss` — **no** `AbilityAction`; see [Elf §5.10](../RacialSystem/Elf-ElementalSpirit-Contracts-Requirements.md) |
| `InventoryActive` | `ItemInstance` id + ability index on `ItemData.activeAbilities` | `PlayerAbilitySource.InventoryItem` |
| `InventoryUse` | `ItemInstance` id for single-action consumable (no separate `AbilityAction`) | `InventoryItemUse` / processor |

### 8.2 — Eligibility matrix

| Assignable | On bar when | Blocked when |
|------------|-------------|--------------|
| Essence active | Essence equipped in that slot on **this** character | Class cannot use essences; slot empty |
| Equipment active | Item equipped in slot | Ability index out of range |
| Racial active | Progression unlock present on **this** character | Dev-only invoker flags off in shipping |
| **Elemental spirit summon** | Elf has **contract instance** in roster (`ElementalSpiritContractsRuntime`) | Not an Elf; instance no longer in roster (stale) |
| **Elemental spirit active** | Parent instance **summoned** + active index valid | Instance not summoned; insufficient SP for active |
| Inventory item / scroll / evocable / throwable | `ItemInstance` **carried or equipped** on **this** character | Item on ally; item on ground; essence category |
| Mage spell | `HumanClass.Mage` | Wrong class |

**Locked:** Cannot bind ally inventory to your bar — use §12 transfer first.

### 8.2.1 — Elf elemental spirits (summon / dismiss + actives)

| Entry kind | Assignable pool | Hotbar press behavior | Turn cost |
|------------|-----------------|----------------------|-----------|
| **`ElementalSpiritSummon`** | **Every contract instance** on this Elf (summoned or not) | **Toggle:** not summoned → summon; summoned → dismiss | **None** |
| **`RacialActive`** (Elf spirit active) | **Deduped** union of actives from **all summoned instances** (one entry per unique `AbilityAction` asset) | Execute via first eligible summoned instance | **Per active** (`consumesTurn`) |

| Usability (summon entry) | Enabled | Greyed |
|--------------------------|---------|--------|
| Instance not summoned | Soul Power ≥ `summonSoulPowerCost` | Insufficient SP; not active member’s turn (combat) |
| Instance summoned | Always (dismiss) | Not active member’s turn (combat) |

**Active deduplication (locked):**

| Rule | Detail |
|------|--------|
| **Same ability asset** | Multiple summoned instances exposing e.g. **Sudden Strength** → **one** hotbar assignable + **one** bound main-row slot. |
| **Different ability assets** | Separate entries (Sudden Strength vs Ember Imbue vs Tide Mend). |
| **Binding key** | `ElementalSpiritActive:{abilityAssetId}` — **not** per `contractInstanceId`. |
| **Execution** | `HotbarResolver` picks a summoned instance on this Elf that provides the ability and passes `CanExecuteSpiritActive`. |
| **Summon toggles** | **Not** deduped — N instances → N `ElementalSpiritSummon` entries. |

- **Labels (summon):** *“{SpiritName} — Summon”* / *“… — Dismiss”*; duplicate spirit types disambiguate with instance suffix (*“Ember Warden (2)”*).
- **Labels (active):** ability name only when deduped (*“Sudden Strength”*).
- **Icons:** spirit icon for summon entries; ability icon for deduped actives.
- **Cross-ref:** [Elf §5.11–§5.12](../RacialSystem/Elf-ElementalSpirit-Contracts-Requirements.md).

---

### 8.3 — Runtime validation (key press & click)

Activation attempts ( **key** on main row **or left-click** on main row / overflow ) fire only when existing rules pass:

- Active member is the **controlled** party member
- Actor’s turn (`TurnManager`) when an action is required
- `CanExecute` / `HumanClassAbilityResources.CanAfford`
- `SafeZonePolicyService` denials
- Targeting / friendly-fire gates
- Inventory charges / quantity
- Combat item-use restrictions (`InventoryUsability`, `InventoryPolicy`)

**Failed activation** (player pressed key or clicked while usable check fails — should not happen if greyed correctly):

- Console message (dungeon log) + brief icon flash optional
- **No** silent no-op on press when icon was full-color ( indicates UI bug )

### 8.4 — Disabled & greyed-out UI (locked)

| ID | Rule |
|----|------|
| **D1** | `HotbarUsabilityService` (or equivalent) evaluates each visible entry **every frame or on relevant events** (turn change, SP change, safe zone, inventory change). |
| **D2** | **Not usable → disabled presentation:** greyed icon + non-interactive activation (keys and clicks ignored). |
| **D3** | Applies identically to **main row** and **overflow**. |
| **D4** | Disabled entries remain **hoverable**; tooltip still shows name + description (§5.4). |
| **D5** | **Stale** entries are always disabled until cleared. |
| **D6** | **Empty** slots are not “disabled abilities” — no greyed ghost except edit-mode `+`. |
| **D7** | When an entry transitions usable → disabled mid-targeting, existing targeting flow is unchanged; only new activations blocked. |

**Typical disable reasons (non-exhaustive):**

- Not active member’s turn
- Insufficient Soul Power / MP / divine power
- `AbilityAction.CanExecute` false
- Safe zone policy deny
- Item consumed / missing / on wrong character
- Cooldown active (when tracked)
- Combat restriction on inventory item use

---

## 9. Data model & persistence

### 9.1 — Per-character hotbar state

Attach to each `BaseActor` (component or run-save slice keyed by stable actor id):

```
HotbarLayout
  MainSlots[10] : HotbarEntry?   // index 0 → key "1", … index 9 → key "0"
  
HotbarEntry
  Kind : HotbarEntryKind
  AbilityAssetId : string?       // AbilityAction GUID/name
  EssenceSlotIndex / AbilityIndex / EquipmentSlot / ItemInstanceId / RacialBindingId
  // enough fields to resolve PlayerCommandProcessor dispatch
```

### 9.2 — Default seeding (first run / new recruit)

On first spawn, auto-fill main row from **legacy mapping** so veterans are not reset:

| Key | Suggested default |
|-----|-------------------|
| `1`–`3` | Essence slot 0–2 primary actives (if any) |
| `4`–`0` | Empty until player assigns |

Do **not** auto-seed Shift/Ctrl layers — those bindings are **removed** (§10.2).

### 9.3 — Persistence scope

| Data | v0 |
|------|-----|
| Layout per character | **Yes** — `RunPartyPersistence` / dungeon run save |
| Across app restarts | Follow existing run save rules |
| Across meta progression | Out of scope |

### 9.4 — Stale entry hygiene

When item consumed, transferred away, or essence unequipped:

- Slot becomes **stale** (visible, disabled)
- Auto-clear on next **out-of-combat** rest or inventory close **or** manual clear in edit mode (pick auto-clear at impl)

---

## 10. Input & command routing

### 10.1 — Key bindings (locked)

| Key | Action |
|-----|--------|
| `1`–`9`, `0` | Fire main-row slot 0–9 for **active member** |
| Numpad digits | **Unbound** — remain movement (`GameControls` numpad move) |

### 10.2 — Legacy input retirement (official — locked)

The **Primary / Shift / Ctrl + digit** ability system is **replaced**, not extended.

| Legacy binding | Fate when hotbar ships |
|----------------|-------------------------|
| **`1`–`0` PrimaryAbilities** | **Repurposed:** keys fire **hotbar main-row slots 0–9** via `HotbarResolver` — **not** implicit essence slot index. |
| **Shift + digit (`ShiftAbilities`)** | **Removed** — unmap from `GameControls`; secondary essence actives go on another hotbar slot or overflow. |
| **Ctrl + digit (`CtrlAbilities`)** | **Removed** — unmap from `GameControls`; equipment actives dragged to hotbar slots or overflow. |
| **`AbilitySlot(…, secondary, fromEquipment)`** | **Retired** — replace with `HotbarSlot(index)` or `HotbarActivate(entryId)`. |

**Migration notes:**

- Remove player-facing docs referencing Shift/Ctrl ability rows.
- Update playtests and QA scripts to use hotbar layout only.
- `ProcessAbilityInput` slot-index / modifier routing **deleted** after hotbar resolver lands.

### 10.3 — Processor integration

Replace index-implicit routing in `ProcessAbilityInput` with:

```
HotbarActivate(activeMember, HotbarEntry | slotIndex 0..9)
  → resolve HotbarEntry
  → dispatch to existing PlayerAbilitySource handlers
```

Same entry point for **keyboard** (main row) and **pointer** (main row + overflow click).

Record **`PlayerCommandKind.HotbarSlot`** (or `HotbarActivate`) for replay/tests.

### 10.4 — Pointer input

| Gesture | Action |
|---------|--------|
| Left-click main-row icon | `HotbarActivate` for that slot if **usable** |
| Left-click overflow icon | `HotbarActivate` for that pool entry if **usable** |
| Drag (edit mode) | Assign / reorder only — no accidental fire on drop |

### 10.5 — Modal blocking

Hotbar keys and clicks respect `GameplayModalGate` / `BlocksFloorGameplay` same as current abilities — no firing through inventory, dialog, rest, etc.

---

## 11. Party inventory transfer (new feature — prerequisite)

### 11.1 — Current state

**Not implemented.** Inventory UI shows **[ Give ]** but calls `GiveToStub()`. `InventoryPolicy` defines combat turn cost stub only.

### 11.2 — Requirements (v0 for hotbar unblock)

| ID | Requirement |
|----|-------------|
| **T1** | **Give** transfers a **carried** `ItemInstance` from owner A to owner B (party members only). |
| **T2** | Allowed **only out of combat** (`CombatThreatCoordinator.IsInCombat == false`). In combat: button disabled + log `[Inventory] Cannot transfer items during combat.` |
| **T3** | Recipient must pass `CanCarry` / encumbrance rules. |
| **T4** | Equipped items **cannot** transfer — must unequip first. |
| **T5** | UI: **Give** opens ally picker (party strip) or drag item onto portrait in **Party Aggregate** inventory mode. |
| **T6** | Transfer is a **free action** out of combat (no turn spend). |
| **T7** | On success, remove from A, add to B, refresh both hotbars if item was hotbar-bound (clear stale on A). |
| **T8** | Essences, currency, mana stones follow existing ledger rules — **not** transferable via Give unless already supported elsewhere. |

### 11.3 — Mock — Give flow

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│ INVENTORY — Party Aggregate                                                             │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  Selected: Healing Potion ×2  (carried · Aria)                                          │
│                                                                                         │
│  [ Use ]  [ Drop ]  [ Give ▾ ]  [ Equip ]                                               │
│                                                                                         │
│  Give to:   (●) Bruenor   ( ) Imoen   ( ) …                                             │
│                                                                                         │
│            [ Confirm Give ]     Esc cancel                                              │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

**Doc cross-update:** [Subspace inventory §6.3](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md) — replace “Give future” with link here.

---

## 12. Art & asset direction

### 12.1 — Hotbar chrome assets (UI frames)

Use **reusable 9-slice** frames — do not hand-draw one PNG per slot.

| Need | Suggested source | Notes |
|------|------------------|-------|
| Slot frame (normal / hover / disabled) | [Kenney UI Pack](https://kenney.nl/assets/ui-pack) (CC0) or [Kenney Game Icons](https://kenney.nl/assets/game-icons) companion borders | Recolor to JRogue dark glass palette |
| Cooldown radial mask | Unity UI Image filled radial | Procedural; no external asset required |
| Expand chevron / edit icons | Kenney UI Pack | Match inventory modal icons |
| Key hint font | Existing TMP in project | Consistent with message console density |

**Reference shots to mimic (layout only, not art theft):** BG3 bottom bar proportions; WoW default bar spacing; DOS2 skill row grouping.

### 12.2 — Ability & action icons (content pipeline)

**New requirement:** every assignable active exposes a **hotbar icon**.

| Asset type | Field (proposed) | Fallback chain |
|------------|------------------|----------------|
| `AbilityAction` | `Sprite hotbarIcon` | `icon` → parent `ItemData.icon` → parent `EssenceData.icon` → category placeholder |
| `EssenceData` | `Sprite icon` (existing?) + override | Generic essence glyph |
| `ItemData` | `icon` (existing) | Category placeholder from `ItemCategoryRegistry` |
| Racial actives | icon on ability asset | Race-colored placeholder |

**Placeholder set (v0 minimum):** 16 icons — melee, ranged, heal, buff, debuff, summon, scroll, potion, evocable, throwable, essence, racial, equipment, unknown, stale, empty slot.

### 12.3 — Content audit (follow-up task)

When implementing, audit and add `hotbarIcon` to:

- All `EssenceData.activeAbilities` entries ([Telekinesis](../Essence/Telekinesis-Essence-Requirements.md), [Sudden Strength](../Essence/Sudden-Strength-Essence-Requirements.md), …)
- Item actives ([Throwing knife](../Inventory/Throwing-Knife-Requirements.md), [Fireball scroll](../Inventory/Fireball-Scroll-Requirements.md), evocables, light-source radiance, warrior potion ability, …)
- Racial actives (Dwarf common, Elf spirits, Spirit Imprint nodes, Undead tree, Tiefling implants, Human mage spells, …)

**Icon spec:** 64×64 source art, authored on **64×64** canvas, **256×256** export optional for crisp scaling; transparent PNG; readable at 48×48 displayed.

### 12.4 — Recommended third-party icon packs (ability glyphs)

| Pack | Use case | License |
|------|----------|---------|
| [Kenney Game Icons](https://kenney.nl/assets/game-icons) | Generic spells / items | CC0 |
| [Game-Icons.net](https://game-icons.net/) | Large variety, consistent SVG→PNG | CC BY 3.0 |
| [Lucid Icons — Fantasy Skills](https://assetstore.unity.com/) (Unity Asset Store) | Skill bar polish | Commercial — verify license before ship |

Pick **one** style family project-wide; recolor to JRogue palette (#c8d0e0 icons on #141820 glass).

---

## 13. Implementation sketch

| Component | Responsibility |
|-----------|----------------|
| `HotbarLayout` / `HotbarEntry` | Serializable per-actor layout |
| `HotbarAssignabilityService` | Builds grouped assignable pool for UI |
| `HotbarResolver` | Maps entry → `AbilityAction` + `PlayerAbilitySource` + context |
| `HotbarUsabilityService` | Usable vs disabled; drives greyed presentation (§8.4) |
| `HotbarTooltipUI` | Hover tooltips — name + description (§5.4) |
| `AbilityHotbarUI` | Main row HUD, edit mode, drag-drop, click-to-activate |
| `AbilityHotbarOverflowUI` | Expandable pool panel, click-to-activate |
| `HotbarInputBridge` | `InputHandler` → `HotbarSlot` command |
| `HotbarIconResolver` | Fallback icon chain |
| `PartyInventoryTransferService` | §11 Give implementation |
| `PlayerCommandProcessor` | Dispatch refactored to resolver |

**Bootstrap:** `AbilityHotbarUI.EnsureInstance()` alongside `GameLogService` on gameplay scenes (`TownTest`, `DungeonFloorTest`).

---

## 14. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Active member’s hotbar visible in town and dungeon; hidden under full-screen modals. |
| **AC2** | Pressing `1`–`0` fires that member’s slot binding, not another member’s. |
| **AC3** | Swapping party member (F-key) swaps visible bar and key routing within one frame. |
| **AC4** | Drag essence active to slot `2`; pressing `2` executes that essence ability with existing SP/turn rules. |
| **AC5** | Drag carried fireball scroll to slot `5`; pressing `5` starts targeting same as inventory Use. |
| **AC5b** | Expand overflow, **click** unbound essence active; executes same as if it were on the main row (when usable). |
| **AC6** | Cannot assign ally’s potion to your bar; after §11 Give, assigned item works on recipient’s bar only. |
| **AC7** | Overflow shows unbound assignables; expand/collapse persists for session. |
| **AC8** | Stale slot visible after item consumed; clears per §9.4. |
| **AC9** | Every bound slot shows an icon (content or placeholder); no empty pink missing sprite. |
| **AC10** | **Shift/Ctrl + digit ability bindings removed**; only hotbar keys + clicks activate abilities/items. |
| **AC11** | Give between party members works out of combat; blocked in combat with message. |
| **AC12** | Out-of-turn or safe-zone-blocked ability appears **greyed** on main row and overflow; click/key does not fire. |
| **AC13** | Hover any non-empty slot or overflow icon → tooltip with **name + description**. |
| **AC14** | Usable ability turns grey immediately when last Soul Power spent (or other gate triggers) without requiring scene reload. |
| **AC15** | Elf with contracted instance: hotbar **summon** entry toggles summon/dismiss **without** consuming turn; spirit **active** entries appear only while summoned. |
| **AC16** | Three summoned spirit instances all expose **Sudden Strength** | Open assignable pool / main row | **One** Sudden Strength slot; **three** summon/dismiss slots. |

---

## 15. Phasing

| Phase | Deliverable |
|-------|-------------|
| **P0 — Data & icons** | `HotbarEntry` model, `AbilityAction.hotbarIcon`, placeholder set, icon audit list |
| **P1 — HUD read-only** | Main row displays assignments + key labels + tooltips; keys fire via resolver; disabled greyed state |
| **P2 — Edit & overflow** | Drag-drop, expandable pool, **overflow click-to-use**, edit mode |
| **P3 — Party Give** | §11 transfer + inventory UI wiring |
| **P4 — Migration** | Default seeds, **remove Shift/Ctrl ability actions**, delete legacy `ProcessAbilityInput` routing |

---

## 16. Open questions (defaults locked for v0)

| Question | v0 default |
|----------|------------|
| Edit mode toggle | **`Edit` button** on bar (not chord) |
| Overflow activation | **Left-click** icon when usable (§6.3) — **required v0** |
| Disabled presentation | **Greyed out** + non-activatable (§8.4) — **required v0** |
| Tooltip content | **Name + `AbilityAction.description`** on hover (§5.4) — **required v0** |
| Duplicate same ability on two keys | **Allowed** (player choice) |
| Mage spells fill overflow only? | **Allowed** on main row like any assignable |
| Bar width on ultrawide | Center cluster max **720px**; slots scale down before wrapping |
| Save hotbar in editor test scenes | **Yes** via run persistence; scene defaults from seed §9.2 |

---

## 17. Related doc updates (when implemented)

| Doc | Update |
|-----|--------|
| [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) | Drag-to-hotbar from list; wire **Give** to §11 |
| [Evocable items](../Inventory/Evocable-Items-Requirements.md) | Hotbar as v0 invoke entry (optional fast path) |
| [Dungeon log](Dungeon-Log-Requirements.md) | Note hotbar reserved strip above console |
| Racial / essence item docs | `hotbarIcon` on each active ability |
| `Docs/Controls/` (if added) | Replace modifier ability tables with hotbar doc link |

---

*Last updated: overflow click-to-use, disabled/greyed state, hover tooltips (name + description), official Primary/Shift/Ctrl retirement.*
