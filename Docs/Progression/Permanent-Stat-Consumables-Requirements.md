# Permanent stat consumables — Requirements

**Status:** **Implemented (v0)** — permanent attribute/resistance pills, character-sheet totals, editor seed + optional bootstrap flag.

**Purpose:** Define **consumable items** that grant a **permanent** increase to a single attribute or damage resistance when used. Today effective stats come from **race/base**, **gear** (equip/unequip), and **essences** (equip/unequip or temporary buffs). Permanent consumables are a fourth track: one-shot inventory **Use** that raises the **consumer’s** lasting score (STBGB-style strength tonics, resistance elixirs, etc.).

**Depends on:** `CharacterStats`, `Stat` / `StatModifier` / `ModifierSourceLayer`, `DamageType` resistances (`CharacterStats.Resistances`), `ItemData` / `ItemInstance`, `ItemCategory.Potion`, `InventoryItemUse`, `InventoryUsability`, `InventoryConsumePolicy`, `AbilityAction` (untargeted item Use pipeline), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md), [Character equipment menu](../UI/Character-Equipment-Menu-Requirements.md) (`C` party sheet), [Stat derivation & combat scaling](Stat-Derivation-And-Combat-Scaling-Requirements.md), [Phase 0 stacking](../RacialSystem/Phase0-Glossary-And-Data-Contracts.md).

**Related:** [Party experience & leveling](Party-Experience-And-Leveling-Requirements.md) (**Potion of Experience** — party-wide XP; **not** the pattern for these items), [Healing Potion](../RacialSystem/Warrior-Willpower-Healing-Potion-And-Stun-Requirements.md) (consumable potion Use + Undead ban), [Sudden Strength essence](../Essence/Sudden-Strength-Essence-Requirements.md) (**temporary** Strength buff — must not be confused with permanent +1), [Equipment stat mods](../Equipment/Stat-And-Class-Equip-Requirements.md) / `StatModifierEffect` (equip-bound), essence `AttributeModifier` / `DamageResistanceModifier` (slot-bound).

**Explicitly out of scope (v0):** Shop stock / dungeon drop tables beyond QA seeds; identification / curse; multi-stat pills; temporary (timed) consumable buffs; party-wide permanent boosts; caps or diminishing returns; Undead-specific alternate “injectable” form (Undead still cannot consume potion-category items); save migration for mid-run characters already in play (new fields default cleanly); full attribute-sheet redesign beyond the permanent-totals readout in §10.

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Effect applies **only** to the party member who **consumes** the item (inventory owner / Use actor). **Never** the whole party. |
| **L2** | Boost is **permanent** for that character for the rest of the run/save: survives unequip, essence swap, rest, floor/town transitions, and formation changes. |
| **L3** | Consuming **removes one** stack/instance from the consumer’s carried inventory (same as Healing Potion / Experience Potion). |
| **L4** | Category is **`ItemCategory.Potion`** so existing **Undead potion ban** (`InventoryConsumePolicy`) applies. Display names use **Pill of …** (**L10**). |
| **L5** | v0 supports two target kinds on one effect type: **attribute** (`StatType`) **or** **resistance** (`DamageType`) — not both on one item. |
| **L6** | Multiple uses **stack** (two +1 Strength pills → +2). **No** per-type lifetime cap in v0. |
| **L7** | Persistence uses a dedicated **`ModifierSourceLayer.PermanentConsumable`** (new layer) so racial base packages and gear/essence mods stay distinguishable. Do **not** silently mutate authored race loadout assets. |
| **L8** | Active party member spends their turn on successful Use (same completion path as other inventory potions). |
| **L9** | v0 content: **Pill of Strength** (+1 Strength) and **Pill of Poison Resistance** (+1 Poison). |
| **L10** | Flavor naming: **`Pill of {Effect}`** (not Potion/Elixir/Tonic). |
| **L11** | Character sheet (**`C`**) shows **permanent bonus totals** for the focused member (§10). |
| **L12** | QA seeding: **dev flag** (opt-in bootstrap grant) **and** **editor menu** — easy manual test; no unconditional seed on every Play Mode. |
| **L13** | Pills set **`allowUseInSafeZone = true`** — usable in town / gameplay safe zones (utility inventory Use). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Permanent personal power** — Consumables raise lasting attributes or resistances for the consumer only. |
| **G2** | **Orthogonal to gear/essences** — Removing gear or essences does **not** remove consumable bonuses. |
| **G3** | **Clear vs temporary buffs** — Distinct from Sudden Strength and other `Temporary` modifiers. |
| **G4** | **Data-driven** — Designers author target (stat or resistance) + amount without new `MonoBehaviour` types per pill. |
| **G5** | **Inventory Use only** — Must be in carried inventory; no ground Use (same gate as other potions). |
| **G6** | **Easy playtest** — Dev flag + editor menu seed both v0 pills into party inventory. |
| **G7** | **Readable inspect** — Inventory detail states the permanent effect (e.g. “Permanently increases Strength by 1”). |
| **G8** | **Visible on sheet** — Permanent totals appear on the `C` character equipment sheet for the selected member. |

---

## 2. Glossary

| Term | Meaning |
|------|---------|
| **Permanent stat consumable** | Pill (potion-category item) whose successful Use permanently increases one attribute or one resistance on the consumer. |
| **Pill** | Player-facing name for these items (`Pill of Strength`, …). Still `ItemCategory.Potion` in data. |
| **Consumer** | The `BaseActor` who owns the carried `ItemInstance` and executes inventory **Use** (`row.Owner`). |
| **Attribute boost** | Permanent delta to a `StatType` core score (e.g. Strength). |
| **Resistance boost** | Permanent delta to `CharacterStats.Resistances[DamageType]` (e.g. Poison). |
| **PermanentConsumable layer** | New `ModifierSourceLayer` for these bonuses (`RacialStackingContract` order — see §5). |
| **Permanent totals** | Per-member summed permanent deltas shown on the character sheet (§10). |
| **Stacking** | Each successful Use adds another permanent delta; totals accumulate with **no cap** in v0. |

---

## 3. Reference — STBGB / genre intent

| Pattern | This project |
|---------|----------------|
| Rare tonics / elixirs that raise a survivor’s lasting Strength, Con, etc. | Attribute permanent consumables (**Pill of …**) |
| Items that harden you against poison / cold / etc. | Resistance permanent consumables |
| Only the character who consumes benefits | **L1** — consumer only (contrast Potion of Experience) |
| Power persists after the item is gone | **L2** — permanent layer / runtime totals |
| Not the same as a short combat buff | Separate from Sudden Strength / Temporary layer |

---

## 4. Current baseline (as-is)

| Source | How it modifies stats | Duration |
|--------|----------------------|----------|
| Race / level / identity | Base values + racial loadout / progression layers | Permanent until respec rules say otherwise |
| Gear (`StatModifierEffect` OnEquip/OnUnequip) | `Stat.AddModifier` with equipment source | While equipped |
| Essence slot mods | Essence `AttributeModifier` / resistance lists | While essence equipped |
| Sudden Strength (etc.) | `ModifierSourceLayer.Temporary` | Timed buff |
| **Permanent consumables** | **Missing** | — |

`ModifierSourceLayer` today: `Base`, `RacialLoadout`, `RacialProgression`, `Equipment`, `Essence`, `Temporary`. No permanent-item layer yet.

---

## 5. Effect model (locked)

### 5.1 — Who benefits

```text
Use(item) by member M
  → apply authored boost to M only
  → consume 1× item from M’s inventory
  → complete M’s (active member) action
```

**Forbidden:** applying the boost to all `PartyManager.partyMembers` (that is Potion of Experience’s pattern only).

### 5.2 — What can be boosted (v0)

| Kind | Target | Example v0 item |
|------|--------|-----------------|
| **Attribute** | One `StatType` (Strength…Luck; PainTolerance allowed if authored) | Pill of Strength (+1 Strength) |
| **Resistance** | One `DamageType` | Pill of Poison Resistance (+1 Poison) |

**Locked:** One item → one kind → one target → one integer amount (typically **+1**).

### 5.3 — How permanence is stored

**Locked approach:**

1. Add **`ModifierSourceLayer.PermanentConsumable`** to the evaluation order **after `RacialProgression` and before `Equipment`**:

```text
Base → RacialLoadout → RacialProgression → PermanentConsumable → Equipment → Essence → Temporary
```

2. On each successful Use, add a modifier on the target `Stat` (attribute or resistance) with:
   - `value` = authored amount
   - `layer` = `PermanentConsumable`
   - `source` = a **stable per-character accumulator** (e.g. `PermanentStatBoostRuntime` on the actor, or a keyed entry inside it) — **not** the `ItemData` ScriptableObject alone (gear already uses the SO as equip source; sharing that id would risk unequip clearing consumable bonuses).

3. Keep a **serialized ledger** on the actor (list of `{ kind, target, amount }` or running totals) so save/load **and character-sheet permanent totals** can rebuild modifiers after scene load.

**Rejected for v0:** mutating `Stat.baseValue` in place without a ledger (harder to explain in UI; easier to corrupt race package assumptions).

### 5.4 — Stacking & caps

- **Stack:** yes (**L6**).
- **Cap:** **none** in v0 (**L6** / Q2 locked). Soft caps are future design only.
- **Same pill twice:** allowed; each Use consumes one and adds +amount again.

### 5.5 — Derived stats

If Strength (or other attributes) feed derived formulas (damage band, encumbrance via Constitution, etc.), those formulas already read `Stat.GetValue()` — permanent modifiers are included automatically. **Max HP** dual-track continues to use Constitution contribution + flat bonuses per [stat derivation](Stat-Derivation-And-Combat-Scaling-Requirements.md); a permanent +Con pill increases Con contribution on next recalculation (same as any other Con increase).

---

## 6. Data model

### 6.1 — Item definition

Prefer a dedicated ability or effect asset referenced from `ItemData.activeAbilities[0]` (same Use path as Healing Potion), e.g.:

**`PermanentStatBoostAbility` : `AbilityAction`** (name illustrative)

| Field | Type | Notes |
|-------|------|--------|
| `boostKind` | enum `Attribute` \| `Resistance` | Exactly one |
| `attribute` | `StatType` | Used when kind = Attribute |
| `resistance` | `DamageType` | Used when kind = Resistance |
| `amount` | int | Default **1**; must be ≠ 0 |
| `requiresTarget` | bool | **false** (self only) |

**`ItemData` (v0 pills)**

| Field | Value |
|-------|--------|
| `category` | `Potion` |
| `itemName` / display | **Pill of …** (§8) |
| `description` | Must state **permanent** and the exact boost |
| `allowUseInSafeZone` | **true** (**L13**) |
| `activeAbilities[0]` | The permanent boost ability |
| `stackable` | **true** (quantity stacks in inventory; each Use consumes 1) |

### 6.2 — Runtime on actor

| Piece | Role |
|-------|------|
| `PermanentStatBoostRuntime` (or equivalent on `CharacterStats`) | Holds ledger / re-applies `PermanentConsumable` modifiers on enable/load; exposes totals for UI |
| Debug log prefix | `[PermanentStat]` |

**UI query API (illustrative):** `IReadOnlyList` or dictionary of non-zero permanent attribute/resistance totals for the focused member — used by inventory inspect helpers and `CharacterEquipmentViewModel`.

---

## 7. Use flow

Reuse inventory potion Use (`InventoryItemUse.TryUseCarriedItem`):

1. Item in **carried** inventory (not ground).
2. `InventoryConsumePolicy.CanConsume` (Undead + Potion → deny).
3. Safe-zone / usability: **`allowUseInSafeZone`** (**L13**) so town Use is allowed; **v0: usable in and out of combat**, no Healing-Potion stun.
4. `ability.CanExecute(consumer)` — consumer alive, has `CharacterStats`, target stat/resistance exists.
5. `ability.Execute(consumer)` → apply permanent boost to **consumer only**.
6. `TryConsumeCarriedQuantity(..., 1)`.
7. `PartyPlayerActionCompletion.CompleteActiveMemberAction(activeMember)`.

**Log (success):**  
`[PermanentStat] {DisplayName} permanently gained +{amount} {TargetLabel}.`

**Combat log / dungeon log (player-facing, recommended):**  
`{DisplayName}'s Strength permanently increased by 1.` / `… Poison resistance permanently increased by 1.`

---

## 8. v0 content pack

| Asset (suggested path) | Display name | Effect |
|------------------------|--------------|--------|
| `Assets/Resources/Item/Potion/Pill_Strength.asset` | **Pill of Strength** | **+1 Strength** (attribute) |
| `Assets/Resources/Item/Potion/Pill_PoisonResistance.asset` | **Pill of Poison Resistance** | **+1 Poison** resistance |

Icons: reuse potion art placeholders or dedicated sprites under `Assets/Art/UI/Items/`.

Editor pack creator: **JRogue → Inventory → Create Permanent Stat Pill Pack** (mirror other pack creators).

---

## 9. QA seed (dev flag + editor menu)

**Goal:** Easily put both pills in party inventory for testing — without forcing them into every Play Mode session.

### 9.1 — Editor menu (required)

**JRogue → Inventory → Seed Permanent Stat Pills on Party**

- Grants **1× Pill of Strength** and **1× Pill of Poison Resistance** into living party carried inventory (prefer first / active member; document if split).
- Idempotent enough for repeated clicks (either always add one more of each, or skip if already present — prefer **always add** so stacking can be tested quickly; log what was granted).
- Works in Play Mode (and Editor with a valid party if supported by similar seed menus).

### 9.2 — Dev flag bootstrap (required)

- Inspector / script flag on bootstrap (e.g. `DungeonRunBootstrap.seedPermanentStatPills` or a small `PermanentStatPillTestGrants` helper), default **`false`**.
- When **`true`**, on fresh party / run start grant the same two pills once (same placement rules as §9.1).
- Production / normal play keeps the flag off.

**Locked:** Do **not** unconditionally seed every Play Mode. Use the menu for ad-hoc tests; flip the flag when you want automatic seeds for a session.

### 9.3 — Verify after Use

| Check | Expected |
|-------|----------|
| Pill of Strength used by member A | A’s Strength +1; other members unchanged |
| Pill of Poison Resistance used by member B (or A) | That member’s Poison resistance +1; others unchanged |
| Unequip all gear / swap essences | Permanent bonuses remain |
| Inventory counts | Pills removed after Use |
| Open **`C`** on consumer | Permanent totals section shows the new bonuses (§10) |

---

## 10. UI

| Surface | Requirement |
|---------|-------------|
| Inventory list | Normal potion-category row; stack quantity if > 1; display **Pill of …** |
| Inspect / detail | Explicit **Permanent** wording + target + amount |
| Use action | Enabled when `InventoryUsability` / consume policy pass |
| Character sheet (`C`) | **Permanent bonuses** readout for the **focused party member** (**L11**) |

### 10.1 — Character sheet permanent totals (locked)

Extend [Character equipment menu](../UI/Character-Equipment-Menu-Requirements.md) (`CharacterEquipmentUI` / `CharacterEquipmentViewModel`) so the focused member shows a compact **Permanent** section when any ledger total ≠ 0 (and still show an empty/placeholder line or hide when all zero — prefer **hide section when empty**, show when any boost exists).

**Content (v0):**

```text
PERMANENT
  Strength +1
  Poison resistance +1
```

| Rule | Detail |
|------|--------|
| **Source** | Ledger / `PermanentStatBoostRuntime` totals (not recomputed from Temporary/Equipment) |
| **Attributes** | One line per boosted `StatType` with non-zero total |
| **Resistances** | One line per boosted `DamageType` (label e.g. `Poison resistance +N`) |
| **Refresh** | Rebuild when party strip focus changes and when the sheet opens; after Use, next open/`C` refresh must show updated totals |
| **Read-only** | No remove/refund UI in v0 |

Effective equipped-item / essence detail panes stay as today; permanent lines are **member-level**, not slot-level.

---

## 11. Explicit non-goals / contrasts

| Item / system | Difference |
|---------------|------------|
| **Potion of Experience** | Grants **XP to whole party**; does not change Strength/resistances |
| **Healing Potion** | Temporary HP restore + possible Stun; no permanent stats |
| **Sudden Strength** | +100 Strength for **10 phases**, Temporary layer |
| **Gear Con +1** | Lost on unequip (`Equipment` layer) |
| **Essence Strength** | Lost when essence unequipped |

---

## 12. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Using Pill of Strength on member A increases A’s effective Strength by 1 permanently; B unchanged. |
| **AC2** | Using Pill of Poison Resistance on a member increases that member’s Poison resistance by 1 permanently. |
| **AC3** | After Use, item quantity decreases by 1 (removed at 0). |
| **AC4** | Unequipping gear and essences does **not** remove the permanent bonuses. |
| **AC5** | A second Strength pill on the same member stacks to +2 (no cap). |
| **AC6** | Undead cannot Use these items (`InventoryConsumePolicy.UndeadPotionBanMessage`). |
| **AC7** | Ground instances cannot be Used until picked up. |
| **AC8** | Editor menu seeds both pills; with **dev flag on**, bootstrap also seeds both once. Flag **off** → no automatic seed. |
| **AC9** | After Use, `C` sheet for that member lists the matching permanent total line(s). |
| **AC10** | Unit tests cover: consumer-only apply; stack; persist across simulated unequip; Undead deny; ledger totals for UI. |

---

## 13. Implementation sketch

| Piece | Suggested location |
|-------|-------------------|
| `ModifierSourceLayer.PermanentConsumable` | `RacialCommitmentPolicy.cs` + Phase 0 contract tests |
| `PermanentStatBoostAbility` | `Assets/Scripts/Abilities/…` |
| `PermanentStatBoostRuntime` | `Assets/Scripts/Stats/` or `Actors/Components/` |
| Item assets | `Assets/Resources/Item/Potion/Pill_*.asset` |
| Pack + seed menu | `Assets/Editor/Inventory/…` |
| Dev-flag grant | `PermanentStatPillTestGrants` (`JRogue.Progression`) + bootstrap flag |
| Character sheet lines | `CharacterEquipmentViewModel` / `CharacterEquipmentUI` |
| Tests | `Assets/Tests/UnitTests/Progression/PermanentStatConsumableTests.cs` |

---

## 14. Resolved questions

| # | Question | Locked answer |
|---|----------|---------------|
| **Q1** | Flavor names? | **`Pill of …`** (**L10**) |
| **Q2** | Soft cap per attribute? | **No cap** in v0 (**L6**) |
| **Q3** | Show permanent totals on character sheet in v0? | **Yes** — `C` sheet §10.1 (**L11**) |
| **Q4** | Auto-seed every Play Mode vs editor menu? | **Dev flag + editor menu** (**L12**); not unconditional |

---

## 15. Checklist

- [x] Requirements reviewed / decisions locked
- [x] `PermanentConsumable` layer + contract test
- [x] Ability + runtime ledger (+ UI totals API)
- [x] Two pill assets (+ icons optional)
- [x] Inventory Use path verified (standard potion Use)
- [x] Editor seed menu + dev-flag bootstrap
- [x] Character sheet permanent section
- [x] Unit tests AC1–AC6, AC10
- [ ] Play-mode AC7–AC9

---

| Date | Note |
|------|------|
| 2026-08-01 | Initial draft — permanent attribute/resistance consumables; consumer-only; Strength + Poison Resistance v0; scene inventory seed |
| 2026-08-01 | Locked Q1–Q4: Pill naming; no cap; `C` sheet permanent totals; dev flag + editor menu seeds |
| 2026-08-01 | Implemented v0: layer, runtime, abilities, pills, UI, seeds, unit tests |
