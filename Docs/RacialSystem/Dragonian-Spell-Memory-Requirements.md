# Dragonian — Spell memory & casting (requirements)

**Purpose:** Specify how **Dragonian** folk (`Race.Dragonian`) combine **essence slots** with a **Dragonian-only spell library**. Spells must be **learned**, then **memorized** into a Soul Power **memory budget** before they can be cast. Casting spends **current Soul Power** (0 or more). Dragonians **cannot** cast Human Mage spells or any non-Dragonian spell type.

**Inspiration:** *Surviving the Game as a Barbarian* — Dragonians internalize draconic techniques (memorized “word-forms”) while still binding **essences** like other Soul Power folk. Closest engine analogue: Human **Mage known vs equipped** spells ([Human — Class powers](Human-Class-Powers-Requirements.md) §8), but Dragonians use **`MaxSoulPower` as the memory budget** instead of Magic Power, and keep **essence slots**.

**Status:** Implemented (v0).

**Depends on:** Phase 0–2 (`CharacterStats`, `Race`, `RacialSubsystemKind`, modifier stacking), [Sudden Strength essence](../Essence/Sudden-Strength-Essence-Requirements.md) (`SuddenStrength_Standard`), [Fireball scroll / ability](../Inventory/Fireball-Scroll-Requirements.md) (`Fireball_Standard`), [Soul Power regeneration](../Progression/Soul-Power-Regeneration-Requirements.md), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md), `EssenceSlotManager`, `AbilityAction` / targeting pipeline, [Party composition presets](../../Assets/Scripts/World/Generation/PartyCompositionPresets.cs) (`DragonianPlayer` in Tiefling/Beastman/Dragonian/Dwarf roster).

**Related:** [Human — Class powers](Human-Class-Powers-Requirements.md) (Mage spell equip budget — **different resource**), [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) (Soul Power spend, race-exclusive actives), [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (future Dragonian body — read-only spell sheet).

**Explicitly out of scope (v0):** Dragonian **learning** gates (NPC trainer, scrolls, level unlocks) beyond Inspector preset / dev menu; spell **tiers** derived from a formula (use explicit **`memorizeCost`** per spell); respec **refund** of learning costs; Dragonian racial menu UI (defer v0.1); gamepad layout; PvP spell steal; casting Dragonian spells from items scrolls owned by non-Dragonians.

---

## Locked decisions (user)

| # | Decision |
|---|----------|
| **L1** | Dragonians may **equip essences** (existing `EssenceSlotManager` — v0: **3** slots when `humanClass == None`, same as unclassed Human). |
| **L2** | Dragonians may **learn Dragonian spells** — spells **unique to Dragonians**; no other folk learns or casts them. |
| **L3** | Dragonians may **only** cast **memorized Dragonian spells** (+ essence actives). They **cannot** cast Human Mage spells, Priest skills-as-spells, or other folk racial actives presented as “spells.” |
| **L4** | Each Dragonian spell has an integer **`memorizeCost` ≥ 0**. |
| **L5** | A spell may be **memorized** iff `memorizeCost(spell) + Σ memorizeCost(m) for all currently memorized spells m ≤ MaxSoulPower`. |
| **L6** | Each Dragonian spell has a **`soulPowerCastCost` ≥ 0** paid from **`currentSoulPower`** on successful cast (may be **0**). |
| **L7** | **Memorization** controls **access** (hotbar / cast list). **Cast cost** is separate and paid at execution time. |
| **L8** | v0 sample spells: one spell behaving like **Sudden Strength**, one like **Fireball** (reuse existing `AbilityAction` assets). |
| **L9** | v0 testing: at least one party member is **`DragonianPlayer`** (existing **`JRogue → Party → Use Roster → Tiefling, Beastman, Dragonian, Dwarf`** preset). |
| **L10** | **Memorize / unmemorize only in safe zone (town).** Blocked in dungeon / combat — same discipline as Human Mage equip changes. |
| **L11** | v0 **`Dragon Flame`** cast cost **`soulPowerCastCost = 5`** (locked for implementation; balance may revise later). |
| **L12** | v0 sample spells use **distinct Dragonian display names:** **`Draconic Surge`**, **`Dragon Flame`** — not parity names of the underlying abilities. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Dual power paths** — Dragonian uses **essences** (slot-limited) **and** **memorized spells** (Soul Power budget-limited) without merging the two systems. |
| **G2** | **Race-exclusive spell identity** — “Dragonian spell” is a first-class content type; engine rejects cross-race casting at validation time. |
| **G3** | **Mage-like loadout clarity** — Players understand **learned** (library) vs **memorized** (active loadout) vs **cast cost** (combat spend). |
| **G4** | **Max Soul Power matters twice** — Sets **memory capacity** (sum of memorize costs) and **fuel pool** (current SP for essence actives + spell casts + Elf-style costs if ever combined on same actor — N/A for Dragonian v0). |
| **G5** | **Reuse abilities** — Spell definitions reference existing `AbilityAction` assets (`SuddenStrength_Standard`, `Fireball_Standard`) so behavior matches proven content. |
| **G6** | **Hotbar parity** — Memorized Dragonian spells appear in ability hotbar assign pool under **Racial** or dedicated **Dragonian Spells** group; execution goes through Dragonian runtime, not Mage runtime. |
| **G7** | **Safe degradation** — Invalid saves (memorized spell not learned, over-budget loadout) clamp with warnings on load. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Dragonian spell** | A `DragonianSpellDefinition` asset: ability payload + **`memorizeCost`** + **`soulPowerCastCost`**. |
| **Learned spell** | Spell id present in the actor’s **known library** (permanent until future unlearn content). |
| **Memorized spell** | Subset of learned spells currently **active for casting**; subject to §5 memory budget. |
| **Memory budget** | `MaxSoulPower` — upper bound on **Σ memorizeCost** of memorized spells. |
| **Memorize / unmemorize** | Toggle spell into/out of memorized set (loadout edit). Does **not** spend current Soul Power. |
| **Cast** | Execute memorized spell’s `AbilityAction`; on success deduct **`soulPowerCastCost`** from `currentSoulPower`. |
| **Essence active** | Unchanged — ability from equipped `EssenceData`; spends `AbilityAction.soulPowerCost` via existing essence pipeline. |

---

## 3. Dragonian power economy (v0)

| Source | Limit | Spend resource | Notes |
|--------|-------|----------------|-------|
| **Essences** | **3** slots (v0) | **`currentSoulPower`** via `AbilityAction.soulPowerCost` | Same as Human `None` / Knight today (`humanClass == None` on Dragonian prefab). |
| **Memorized spells** | **Σ memorizeCost ≤ MaxSoulPower** | **`currentSoulPower`** via **`soulPowerCastCost`** on cast | Independent of essence slots. |
| **Memory budget** | **`MaxSoulPower`** | *(none — capacity only)* | Not reduced by casting. |

### 3.1 — `MaxSoulPower` for Dragonians

**v0 policy (locked):** use the **same formula as unclassed Human** until a dedicated balance pass:

```text
MaxSoulPower = Intelligence × 5 + Wisdom × 5 + levelSoulPowerBonus
```

**Engine note:** Today `CharacterStats.MaxSoulPower` delegates to `HumanClassRules.ComputeMaxSoulPower`, which returns a non-zero value when `humanClass == None` (Dragonian prefab default). Implementation should add an explicit **`DragonianRules.UsesSoulPower(Race)`** branch so Dragonian max SP does **not** depend accidentally on Human class enum semantics long-term.

**Regeneration:** Dragonian uses existing [Soul Power regeneration](../Progression/Soul-Power-Regeneration-Requirements.md) when eligible (same as other Soul Power folk).

---

## 4. Exclusivity & validation

### 4.1 — Who may use Dragonian spells

| Check | Rule |
|-------|------|
| Race | `CharacterStats.race == Race.Dragonian` |
| Subsystem | `RacialSubsystemKind.DragonianSpells` on actor (v0: set on `DragonianPlayer` prefab) |
| Runtime | `DragonianSpellsRuntime` present |

### 4.2 — What Dragonians cannot cast

| Blocked | Reason |
|---------|--------|
| `HotbarEntryKind.HumanMageSpell` | Human Mage subsystem |
| Human Priest tree actives | Human specialization |
| Other folk racial actives | Wrong race/subsystem |
| Raw `AbilityAction` assets not bound to a **learned Dragonian spell** | Spells must be memorized through Dragonian runtime |

### 4.3 — What other folk cannot do

- Non-Dragonians **cannot** learn, memorize, or execute `DragonianSpellDefinition` entries (dev tools may override with warning).

### 4.4 — Essence + spell stacking

- Essence passives and spell effects use normal modifier stacking rules.
- A Dragonian may have **Sudden Strength essence** **and** **Sudden Strength Dragonian spell** memorized simultaneously — they are separate systems; designer should avoid duplicate authoring in production content (v0 test **may** duplicate intentionally to compare pipelines).

---

## 5. Memorization rules (locked)

### 5.1 — Capacity formula

```text
remainingMemory = MaxSoulPower - Σ memorizeCost(s) for s in memorizedSpells
```

**Memorize validation:** spell `S` may be added to memorized set iff:

```text
remainingMemory >= memorizeCost(S)
```

**Unmemorize:** always allowed; frees capacity immediately.

### 5.2 — Learned gate

- Only **learned** spells may be memorized.
- v0: learning = preset list on `DragonianSpellsRuntime` / prefab (see §9).

### 5.3 — Persistence

- **Learned** spell ids and **memorized** spell ids persist on the party member save blob (mirror `HumanMageSpellsRuntime` known + equipped ids).

### 5.4 — When loadout may change (locked)

| Context | Memorize / unmemorize |
|---------|------------------------|
| **Town / safe zone** | **Yes** |
| **Dungeon / combat** | **No** — reject with clear feedback (e.g. *“You can only adjust memorized spells in town.”*) |
| Racial menu (`K`) | **Read-only v0**; editing deferred to v0.1 UI or dedicated spell sheet in safe zone |

**Implementation:** gate via `SafeZonePolicyService` (or equivalent) on `TryMemorize` / `TryUnmemorize`, matching Beast Blood / ritual safe-zone discipline.

---

## 6. Casting rules

### 6.1 — Preconditions

1. `GameState.PLAYER_TURN` (same as essences).
2. `TurnManager.CanActorTakeAction(caster)`.
3. Spell is **memorized** and **learned**.
4. `currentSoulPower >= soulPowerCastCost(spell)`.
5. `AbilityAction.CanExecute(caster)` (targeting, buff exclusivity, etc.).

### 6.2 — Execution

1. Resolve `AbilityAction` from spell definition.
2. Execute targeted or untargeted path (same as essence / Mage spell).
3. On **success only:** `currentSoulPower -= soulPowerCastCost(spell)`.
4. Consume player action / formation rules (same as existing actives).

### 6.3 — Cast cost vs ability `soulPowerCost`

**Locked:** Dragonian spell runtime uses **`DragonianSpellDefinition.soulPowerCastCost`** for deduction, **not** `ability.soulPowerCost`, so designers can decouple display ability asset from Dragonian pricing. v0 sample spells should set **`soulPowerCastCost`** to match the intended gameplay (see §8).

**Authoring rule:** when wrapping an existing ability, set `ability.soulPowerCost` to **0** on the Dragonian spell path **or** ignore ability cost in Dragonian executor — prevent **double charge**.

---

## 7. Data model

### 7.1 — `RacialSubsystemKind`

Add:

```csharp
DragonianSpells = 8
```

`RacialSubsystemCatalog`: `Race.Dragonian` ↔ `DragonianSpells`, commitment policy **`Permanent`** for **learned** spells; **memorized loadout** freely changeable (policy similar to Mage **equip**, not Barbarian imprint).

### 7.2 — `DragonianSpellDefinition` (ScriptableObject)

| Field | Type | Notes |
|-------|------|-------|
| **`spellId`** | string | Stable id (e.g. `dragonian_spell_sudden_strength`) |
| **`displayName`** | string | UI / hotbar |
| **`description`** | string | `[TextArea]` |
| **`memorizeCost`** | int ≥ 0 | Memory budget cost (§5) |
| **`soulPowerCastCost`** | int ≥ 0 | Cast spend (§6) |
| **`ability`** | `AbilityAction` | Behavior (targeting, damage, buff, …) |

**No tier enum in v0** — memorize cost is **explicit** per spell (user request differs from Mage `10 - tier`).

Optional v0.1: `icon`, `school`, `prerequisiteSpellIds`.

### 7.3 — `DragonianSpellRegistry` (optional v0)

- Central list of all Dragonian spells for content tools (mirror `SoulBeastRegistry` pattern).
- v0 may inline known list on runtime component instead.

### 7.4 — `DragonianSpellsRuntime` (MonoBehaviour)

| Responsibility | API sketch |
|----------------|------------|
| Known library | `IReadOnlyList<DragonianSpellDefinition> KnownSpells` |
| Memorized loadout | `IReadOnlyList<DragonianSpellDefinition> MemorizedSpells` |
| Budget | `int RemainingMemoryCapacity` |
| Mutations | `TryMemorize(spellId, out reason)`, `TryUnmemorize(spellId, out reason)` |
| Cast | `TryExecuteMemorized(index, user, targetTile?)` |
| Preset bootstrap | `SetKnownAndMemorized(known, memorizedIds)` for prefab / dev |

Validate `race` + subsystem on all mutations (mirror `HumanMageSpellsRuntime.ValidateMageActor`).

---

## 8. v0 sample content (required for spec proof)

### 8.1 — Draconic Surge (Sudden Strength behavior)

| Field | Value |
|-------|-------|
| **`spellId`** | `dragonian_spell_sudden_strength` |
| **`displayName`** | **`Draconic Surge`** |
| **`ability`** | [`SuddenStrength_Standard`](../../Assets/Resources/Item/Ability/SuddenStrength_Standard.asset) |
| **`memorizeCost`** | **3** |
| **`soulPowerCastCost`** | **1** |
| **Targeting** | Self, untargeted (inherited from ability) |

### 8.2 — Dragon Flame (Fireball behavior)

| Field | Value |
|-------|-------|
| **`spellId`** | `dragonian_spell_fireball` |
| **`displayName`** | **`Dragon Flame`** |
| **`ability`** | [`Fireball_Standard`](../../Assets/Resources/Item/Ability/Fireball_Standard.asset) |
| **`memorizeCost`** | **7** |
| **`soulPowerCastCost`** | **5** (locked v0; subject to future balance pass) |
| **Targeting** | Targeted tile + splash (inherited) |

### 8.3 — v0 preset loadout (Dragonian test prefab)

Suggested **`DragonianPlayer`** bootstrap:

| Set | Content |
|-----|---------|
| **Known** | Both sample spells |
| **Memorized** | **`dragonian_spell_sudden_strength` only** (leaves room to test memorizing Fireball in UI) |
| **Essences** | Empty **or** one non-conflicting essence for dual-path QA |

**Budget check example:** if `MaxSoulPower = 100`, memorizing both (3 + 7 = **10**) is legal; casting Fireball five times in one fight requires **25** current SP total.

---

## 9. Integration

### 9.1 — Hotbar

| Item | Rule |
|------|------|
| **Assign pool** | `HotbarAssignabilityService` appends memorized Dragonian spells (new `HotbarEntryKind.DragonianSpell` or reuse `RacialActive` with binding key) |
| **Execute** | `HotbarResolver` → `DragonianSpellsRuntime.TryExecuteMemorized` |
| **Label** | `displayName`; tooltip shows memorize cost + cast cost |

### 9.2 — Player command processor

- Targeted Dragonian spells use same reticle / splash pipeline as Fireball essence scroll.
- Untargeted spells skip reticle (Sudden Strength pattern).

### 9.3 — Racial abilities menu (`K`)

**v0.1 (defer):** read-only sheet — learned spells, memorized flag, memorize/cast costs, remaining memory capacity. Banner: *“Visit … to learn new draconic spells”* (NPC TBD).

### 9.4 — Party testing

| Action | Detail |
|--------|--------|
| **Roster** | Menu: **`JRogue → Party → Use Roster → Tiefling, Beastman, Dragonian, Dwarf`** |
| **Focus** | Select **Dragonian** party member (F-key strip) for hotbar + casting tests |
| **Soul Power** | Confirm HUD shows non-zero max/current SP on Dragonian |

Replace a single party slot with Dragonian only if using a custom roster; default test preset already includes **`DragonianPlayer`**.

### 9.5 — Editor / dev tooling (v0)

| Tool | Purpose |
|------|---------|
| **`JRogue/Racial/Create Dragonian Spell Pack`** | Author sample spell assets + wire prefab runtime |
| **Context menu on `DragonianSpellsRuntime`** | Memorize / unmemorize / list budget (dev) |

---

## 10. Comparison — Human Mage vs Dragonian

| | **Human Mage** | **Dragonian** |
|---|----------------|---------------|
| **Resource pool** | Magic Power | Soul Power (`Max` + `current`) |
| **Loadout budget** | Σ equipCost ≤ **MaxMagicPower** | Σ memorizeCost ≤ **MaxSoulPower** |
| **Loadout verb** | Equip / unequip | Memorize / unmemorize |
| **Cast spend** | `magicPowerCost` on spell | `soulPowerCastCost` on spell |
| **Essences** | **Disabled** | **Enabled** (3 slots v0) |
| **Exclusive content** | `MageSpellDefinition` | `DragonianSpellDefinition` |
| **Cost authoring** | Tier → `10 - tier` | Explicit **`memorizeCost`** |

---

## 11. Acceptance criteria

| ID | Test |
|----|------|
| **A1** | Dragonian with `MaxSoulPower = 100` can memorize **Draconic Surge** (3) + **Dragon Flame** (7); total **10 ≤ 100**. |
| **A2** | Adding a third spell with memorize cost **91** fails while the first two are memorized. |
| **A3** | Unmemorizing **Dragon Flame** frees **7** capacity; re-memorize succeeds **in town only**. |
| **A4** | Cast **Draconic Surge** deducts **1** current SP on success; fails at **0** SP. |
| **A5** | Cast **Dragon Flame** deducts **5** current SP once (no double charge from ability asset). |
| **A6** | Human Mage cannot cast Dragonian spell hotbar entry; Dragonian cannot cast Human Mage spell entry. |
| **A7** | Dragonian with essence equipped can still cast memorized spells if SP budgets allow. |
| **A8** | Non-memorized learned spell cannot execute from hotbar. |
| **A9** | Party preset with **DragonianPlayer** spawns actor with subsystem + sample known spells. |
| **A10** | `TryMemorize` / `TryUnmemorize` in dungeon returns failure; same operations succeed in town. |

---

## 12. Implementation phases

| Phase | Scope |
|-------|-------|
| **v0 (this doc)** | `DragonianSpellDefinition`, `DragonianSpellsRuntime`, memorize budget validation, cast pipeline, hotbar wiring, sample Sudden Strength + Fireball spells, `DragonianPlayer` preset |
| **v0.1** | Racial menu read-only spell sheet; safe-zone memorize UI |
| **v1** | Learn spells from NPC / loot; spell registry; content pack creator |

---

## 13. Resolved & remaining questions

| # | Question | Resolution |
|---|----------|------------|
| **Q1** | Can the same spell be memorized twice? | **No** — at most one entry per `spellId`. |
| **Q2** | Does learning a spell cost resources? | **Not in v0** — preset known list only. |
| **Q3** | Memorize changes in dungeon? | **Locked no** — safe zone only (§L10). |
| **Q4** | `MaxSoulPower` formula for Dragonian long-term | **Same as Human None v0**; revisit in balance pass. |
| **Q5** | Display name parity vs unique names? | **Locked distinct names** — `Draconic Surge`, `Dragon Flame` (§L12). |
| **Q6** | Dragon Flame cast cost? | **Locked 5 SP v0** (§L11); may change in balance pass. |

---

## 14. Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Phase 0 glossary](Phase0-Glossary-And-Data-Contracts.md) | Add `DragonianSpells` subsystem |
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | §6 Dragonian row → link here |
| [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) | Dragonian spell assign + execute |
| [Soul Power regeneration](../Progression/Soul-Power-Regeneration-Requirements.md) | Confirm Dragonian eligibility |

---

## 15. Document history

| Date | Change |
|------|--------|
| 2026-06-05 | Implemented v0 — DragonianSpellsRuntime, hotbar wiring, sample spells, DragonianPlayer preset. |
| 2026-06-13 | Locked L10–L12: safe-zone-only memorize, Dragon Flame cast 5 SP, distinct spell names. |
| 2026-06-13 | Initial draft — spell memory budget on MaxSoulPower, cast spend, essence coexistence, sample Sudden Strength + Fireball, party roster testing. |
