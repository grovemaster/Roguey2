# Human — Class powers (requirements)

Humans are a playable **folk** (`Race.Human`) who begin with **no class** and may later commit to **exactly one** of three specializations: **Knight**, **Mage**, or **Priest**. Class choice is **permanent**. Progression after commitment differs by class: **Knight** and **Priest** use **Diablo 2–style** skill trees (prerequisites, per-skill rank caps, per-rank property scaling); **Mage** uses a **spell library** with **tiered equip costs** against a **Magic Power** pool instead of essences and Soul Power.

Design inspiration: *Surviving the Game as a Barbarian* (**STBGB**) — civilians become specialists through story gates and training, not respec-friendly class hopping.

**Subsystem kind (existing code):** `RacialSubsystemKind.HumanSpecialization` (see `RacialSubsystemKind.cs`).

**Commitment policy:** **`Permanent`** for **class choice** (Knight / Mage / Priest). Skill **point spending** within a class tree is **permanent per point** in v0 unless a later doc adds Knight-only retraining rules.

**Depends on:** Phase 1–2 (`RacialLoadoutDefinition` / `RacialLoadoutApplier`, stacking-by-source, `RacialPassiveHooks`), `HumanClass` on `CharacterStats`, `EssenceSlotManager`, `AbilityAction` / targeting pipeline, [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) (character level gates skill nodes).

**Contrast:** [Undead — Race requirements](Undead-Race-Requirements.md) (D4-style tree, **respec** allowed, folk always has the subsystem). [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (forward-only path, not point ranks). [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) (Soul Power upkeep). Essences remain the default Human-**None** power source until class commitment.

**Explicitly later:** NPC / ritual **class-change** requirements; Knight **training events**; Mage **spell learning** sources; Priest **god patronage** implementation; additional Human classes beyond the initial three.

---

## 1. Goals

**G1 — Class lifecycle**  
A Human actor starts as `HumanClass.None` (no specialization). They may transition to **Knight**, **Mage**, or **Priest** **once**, via a gated ceremony (content later; v0 may use Inspector / debug preset). After commitment, `HumanClass` **cannot** change.

**G2 — Three distinct power economies**  
| Class | Essences | Soul Power | Class resource |
|-------|----------|------------|----------------|
| **None** | Yes (default slots) | Yes (`MaxSoulPower` / `currentSoulPower`) | — |
| **Knight** | Yes | Yes | Skills cost **Soul Power** when authored on actives |
| **Mage** | **No** (`maxEssenceSlots = 0`) | **0** max / current | **Magic Power** (cast + equip budget) |
| **Priest** | **No** | **0** max / current | **Divine Power** (skill costs) |

**G3 — Data-driven class content**  
Designers author Knight/Priest skill trees and Mage spells as assets—no per-skill `MonoBehaviour` subclasses.

**G4 — Sample content (required for spec proof)**  
Shipping sample data includes:
- Knight: **two** passive skills (+Strength, +Dexterity) in a small D2-style tree.
- Priest: **two** passive skills (+Strength, +Dexterity) in a small D2-style tree.
- Mage: **`Fireball`** and **`Teleport`** spells with tiers and equip costs; only **equipped** spells are castable.

**G5 — Safe degradation**  
Invalid saves (class without subsystem, Mage with essences equipped, etc.) clamp or strip illegal state with **warnings**, not silent corruption.

**G6 — Playable Human prefab parity**  
`HumanPlayer` (and party humans) continue to default to `HumanClass.None` + canonical default human loadout until content assigns a class preset.

---

## 2. Reference — STBGB (design intent)

| STBGB idea | Human class model |
|------------|-------------------|
| Ordinary people enter the game without a “build” | `HumanClass.None` — essences / Soul Power like today’s generic hero |
| Commitment to a role through story | One-way class change at a **special NPC** (later) |
| Knights train techniques over time | Knight skills unlocked via **training events** (later); tree structure is D2-like |
| Priests serve a deity | **Patron god** (later); **Divine Power** instead of Soul Power |
| Mages memorize / prepare spells | **Known** spell library vs **equipped** subset limited by Magic Power budget |

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Human class** | `HumanClass` on `CharacterStats`: `None`, `Knight`, `Mage`, or `Priest` (extensible enum). Meaningless unless `race == Human`. |
| **Class commitment** | Irreversible assignment from `None` → one of the three classes. |
| **Class-change gate** | Story/NPC requirement authorizing commitment (not v0). |
| **Skill (Knight / Priest)** | A node in that class’s **skill tree**; may be passive, active, or stat-only; supports **multiple ranks** up to a per-skill cap. |
| **Skill point** | Currency spent to increase a skill’s rank (sources: training events for Knight—later; Priest/Knight point earn rate TBD—likely level). |
| **Skill rank** | Integer `0 … maxRanks` on a node. Rank 0 = inactive. |
| **Magic Power** | Mage-only resource pool (max + current). Replaces Soul Power for mages. Used for **casting** and as the **equip budget** (§8). |
| **Divine Power** | Priest-only resource pool (max + current). Replaces Soul Power for priests. |
| **Spell** | Mage-only `SpellDefinition` (or successor): ability payload + **tier** + equip cost. |
| **Known spells** | Spells the mage has learned (library). Learning pipeline is **later**; v0 may preset known list. |
| **Equipped spells** | Subset of known spells active for casting; subject to equip budget (§8). |
| **Spell tier** | Integer **1** (highest, most expensive to equip) through **9** (lowest, cheapest). |
| **Equip cost** | `10 - spellTier` — deducted from the mage’s **remaining equip capacity** (§8). |
| **Training event** | World/NPC interaction that grants Knight skill points or unlocks a specific training (later). |

---

## 4. Human with no class (`HumanClass.None`)

### H4.1 — Baseline behavior

- `humanClass == HumanClass.None` is the default for new Humans, random civilians, and `HumanPlayer` until a class is committed.
- **Essences:** `EssenceSlotManager` uses the normal slot count (v0: **3** unless folk loadout overrides).
- **Soul Power:** `MaxSoulPower` and `currentSoulPower` use existing formulas (`Intelligence`, `Wisdom`, `levelSoulPowerBonus`) — see `CharacterStats`.
- **Racial subsystem:** `racialSubsystem` may remain `None` or `HumanSpecialization` with **empty** class tree / spell loadouts until commitment.
- **Folk loadout:** Continue to use the single canonical **`DefaultHumanRacialLoadout`** (Phase 5 guardrail GR5.2).

### H4.2 — What None does *not* grant

- No Knight skill tree ranks, no Priest tree, no Mage known/equipped spells.
- No Divine Power or Magic Power pools (or they remain **0** / hidden in UI).

---

## 5. Class commitment (one-way)

### C5.1 — Allowed transitions

| From | To | Allowed |
|------|-----|---------|
| `None` | `Knight`, `Mage`, `Priest` | **Yes**, once, when gate satisfied |
| Any class | `None` or another class | **No** |

### C5.2 — Runtime enforcement

- On commit: set `CharacterStats.humanClass`, set `racialSubsystem = HumanSpecialization`, apply class-specific subsystem bootstrap (disable essences for Mage/Priest, zero Soul Power caps, initialize Magic/Divine pools, attach tree/spell runtime).
- **Save / identity:** persist `humanClass` in `RacialIdentitySnapshot` (or party member save). Loads must not downgrade or swap class without migration tooling.
- **Validation:** if save says `Mage` but actor still has equipped essences, **strip** essences and log warning (v0 policy).

### C5.3 — Class-change requirements (later, not v0)

- Commitment requires interaction with a **special NPC** (or equivalent story gate): faction standing, quest flag, item sacrifice, etc.—**content-defined per gate**, not hard-coded in engine.
- v0: **preset** class on test prefabs or debug command; document gate ids as **stable string keys** for future quests.

### C5.4 — Extensibility

- `HumanClass` enum reserves numeric values; new classes (e.g. Summoner mentioned in Phase 0) add new enum entries and **separate** tree/spell definitions without reusing Knight/Mage/Priest assets.

---

## 6. Shared — Diablo 2–style skill trees (Knight & Priest)

This section defines the **mechanical contract** for both classes. Undead uses a **D4** cluster model with respec; Human class trees use **D2** prerequisites and **per-node rank caps** without requiring identical UI layout.

### T6.1 — Tree shape

- One **`HumanClassSkillTreeDefinition`** (or equivalent) per class: `KnightSkillTree`, `PriestSkillTree`.
- Nodes are **vertices**; **edges** encode prerequisites (“must have N points in skill X before skill Y is visible/spendable”).
- Additional gates per node:
  - **`requiredCharacterLevel`** — party member level ≥ value.
  - **`requiredPointsInPrerequisiteSkills`** — sum of ranks in listed prerequisite node ids ≥ threshold (D2 “synergy” gate pattern).
- Nodes may be **mutually exclusive** (optional groups) if design needs branch choices; v0 sample may omit exclusivity.

### T6.2 — Node kinds

| Kind | Role | Typical `maxRanks` |
|------|------|--------------------|
| **Passive** | Always-on stat/rule while rank ≥ 1 | 1–20 (per node) |
| **Active** | `AbilityAction` executed from hotbar / command processor | 1–20 |
| **Modifier** | Alters a named active (by id) per rank | Often 1 |

Exact caps are **per-node data** (`maxRanks`), not global.

### T6.3 — Per-rank scaling

Each skill node defines **rank curves** for one or more **properties**, e.g.:

- Active: `damage`, `splashRadius`, `range`, **`soulPowerCost`** (Knight) or **`divinePowerCost`** (Priest), `cooldownTurns`.
- Passive: `+Strength`, `+Dexterity`, resistance rows.

**Requirement:** spending rank *r* applies the payload for rank *r* and **removes** rank *r−1* modifiers from the same **source id** (Pattern B stacking). Re-spending after refund (if ever allowed) must not double-stack.

**Example (authoring intent, numbers tunable):**

| Skill id | Kind | maxRanks | Per rank (example) |
|----------|------|----------|---------------------|
| `knight_passive_might` | Passive | 5 | +2 Strength per rank |
| `knight_passive_finesse` | Passive | 5 | +2 Dexterity per rank |
| `priest_passive_might` | Passive | 5 | +2 Strength per rank |
| `priest_passive_finesse` | Passive | 5 | +2 Dexterity per rank |

Sample trees should wire **at least** these four nodes with a simple prerequisite chain (e.g. Might tier 1 requires character level 1; Finesse requires 1 point in Might) to prove gates in tests.

### T6.4 — Skill points

| Class | Point source (v0 / later) |
|-------|---------------------------|
| **Knight** | **Training events** (later) grant points or direct rank unlocks; v0 may **preset** ranks |
| **Priest** | **Later:** level curve, quests, or shrine offerings (TBD); v0 **preset** ranks |

**Invariant:** spending a point increments **one** node’s rank if all gates pass; cannot exceed `maxRanks`.

### T6.5 — Respec within tree

- **Class choice:** never respec-able.
- **Knight / Priest skill ranks:** **no respec in v0** unless a later doc adds Knight retraining fiction. Undead-style free respec **does not** apply here.

### T6.6 — Persistence

- Versioned save: `humanClass`, per-node **ranks** (sparse map: `nodeId → rank`), spent/available skill points.
- Old saves: missing class fields ⇒ `None`.

---

## 7. Knight

### K7.1 — Essences and Soul Power

- Knight **may** equip and use **essences** (same slot rules as `HumanClass.None` unless loadout overrides).
- Active Knight skills use **`soulPowerCost`** on `AbilityAction` (or successor field) when &gt; 0; insufficient Soul Power ⇒ existing **Not enough Soul Power!** behavior.

### K7.2 — Training events (later)

- **Training events** are the primary way to earn **skill points** or authorized rank-ups (fiction: drill master, mercenary captain, etc.).
- Each event references: `trainingEventId`, optional `skillPointGrant`, optional `unlockNodeId`, prerequisite flags.
- v0: no event pipeline; preset ranks on `KnightPlayer` test prefab acceptable.

### K7.3 — Sample tree (minimum)

- Assets: `KnightSkillTree_Sample` with ≥ 2 passive nodes (§T6.3 table).
- Runtime: `HumanKnightSkillTreeRuntime` (name flexible) eligible when `humanClass == Knight`.

---

## 8. Mage

### M8.1 — Essence and Soul Power prohibition

| Rule | Requirement |
|------|-------------|
| **Max essence slots** | **0** — `EssenceSlotManager.totalSlots = 0`; cannot equip or pick up essences into essence slots |
| **Gain essences** | Blocked at pickup/equip UI and pipeline |
| **Max Soul Power** | **0** — `MaxSoulPower` returns 0 regardless of Int/Wis while Mage |
| **Current Soul Power** | Clamped to **0** on class commit and each turn boundary |
| **Use essences** | Any attempt fails with clear feedback |

Regression: `HumanClass.None` and **Knight** still use essences normally.

### M8.2 — Magic Power

- **Magic Power** replaces Soul Power for Mages:
  - `maxMagicPower` (design formula TBD; may derive from Intelligence, level, gear—document in implementation).
  - `currentMagicPower` — spent on **cast**, not on equip.
- **Cast cost** is per spell per rank (if spells have ranks) or flat on `SpellDefinition`.
- v0: mirror Soul Power simplicity (e.g. `maxMagicPower = Intelligence * 5 + levelBonus`) until a dedicated balance pass.

### M8.3 — Spells: known vs equipped

| Set | Meaning |
|-----|--------|
| **Known** | All spells the mage has learned (library). Populated by **learning** (later) or v0 preset. |
| **Equipped** | Spells currently on the action bar / cast list. |

**Cast rule:** A mage may **only** cast spells in the **equipped** set. Known-but-unequipped spells are visible in UI (later) but not executable.

### M8.4 — Spell tiers and equip cost

**Tier:** integer **1 … 9** where **1 = highest tier** (strongest / most prestigious) and **9 = lowest**.

**Equip cost per spell:**

```text
equipCost(spell) = 10 - spellTier
```

| Tier | Equip cost |
|------|------------|
| 1 | 9 |
| 2 | 8 |
| … | … |
| 9 | 1 |

**Remaining equip capacity:**

```text
remainingEquip = maxMagicPower - Σ equipCost(s) for all currently equipped spells s
```

**Equip validation:** A spell may be added to **equipped** only if:

```text
remainingEquip >= equipCost(spell)
```

After equipping, recompute `remainingEquip`. **Unequip** frees capacity immediately.

**Note:** The user-facing “Player's remaining equip number” **is** `remainingEquip` above. Casting does **not** consume equip capacity—only the equip loadout does.

### M8.5 — Sample spells (required)

#### M8.5.1 — Fireball

| Field | Requirement |
|-------|-------------|
| **id** | `mage_spell_fireball` (stable) |
| **tier** | **3** (equip cost **7**) — strong but not top-tier; tunable |
| **Ability** | Reuse `FireballAbility` behavior aligned with [`Fireball_Standard`](../../Assets/Resources/Item/Ability/Fireball_Standard.asset): targeted tile, splash, fire damage, noise |
| **Cast cost** | Paid from **`currentMagicPower`**, not Soul Power |
| **requiresTarget** | **true** |

#### M8.5.2 — Teleport

| Field | Requirement |
|-------|-------------|
| **id** | `mage_spell_teleport` |
| **tier** | **6** (equip cost **4**) — mid-low slot cost |
| **Ability** | Reuse `TeleportAbility` / [`Teleport_Standard`](../../Assets/Resources/Item/Ability/Teleport_Standard.asset): targeted empty tile, party leader history snap rules unchanged |
| **Cast cost** | Magic Power &gt; 0 (exact value in data) |
| **requiresTarget** | **true** |

**v0 equip example:** If `maxMagicPower = 20`, equipping Fireball (7) + Teleport (4) uses **11**, `remainingEquip = 9`. A tier-1 spell (cost 9) could still be equipped alone but not with both unless max is raised.

### M8.6 — Spell learning (later)

- Sources: scroll transcription, trainer NPC, level-up grimoire, quest rewards.
- Learning adds to **known** only; does not auto-equip.
- v0: preset `knownSpellIds` on test Mage prefab.

### M8.7 — Runtime

- `HumanMageSpellsRuntime` (name flexible): known list, equipped list, equip validation, cast routing through `PlayerCommandProcessor` with source **`HumanMageSpell`** (new enum value parallel to Essence / Equipment).
- On class commit: clear essence slots, strip Soul Power modifiers tied to essences.

---

## 9. Priest

### P9.1 — Essence and Soul Power prohibition

Same hard rules as Mage (§M8.1): **zero** essence slots, **zero** Soul Power, cannot gain or use essences.

### P9.2 — Divine Power

- **Divine Power** replaces Soul Power entirely for Priests.
- Priest skill actives use **`divinePowerCost`** (new field or parallel to `soulPowerCost`) on ability assets.
- Insufficient Divine Power ⇒ same UX pattern as Soul Power failures (message + no action spent).

### P9.3 — Patron god (later)

- Each Priest has **patronage of a god** (deity id, domains, flavor restrictions).
- Patron may gate subtrees, modify costs, or unlock exclusivity groups—**data hooks only in v0** (`patronGodId` string, may be empty).
- Sample Priest can use `patronGodId: "unspecified"` until god system ships.

### P9.4 — Skill tree

- Same mechanical contract as Knight (§6) but costs reference **Divine Power**, not Soul Power.
- Required sample passives: `priest_passive_might`, `priest_passive_finesse` (§T6.3).

### P9.5 — Runtime

- `HumanPriestSkillTreeRuntime` when `humanClass == Priest`.
- Optional: passive “channel” FX keyed off `patronGodId` later.

---

## 10. Cross-class comparison

| | **None** | **Knight** | **Mage** | **Priest** |
|--|----------|------------|----------|------------|
| **Essences** | Yes | Yes | **No** | **No** |
| **Soul Power** | Yes | Yes | **No** | **No** |
| **Class resource** | — | Soul Power (actives) | Magic Power | Divine Power |
| **Progression UI** | Essence slots | D2 skill tree | Known + equipped spells | D2 skill tree |
| **Point / unlock source** | — | Training events (later) | Learn spells (later) | TBD (later) |
| **Respec class** | — | **Never** | **Never** | **Never** |

---

## 11. Out of scope (v0)

- Special NPC class-change quests and UI.
- Knight training event content pipeline.
- Mage spell learning drops, shops, scrolls (distinct from [Fireball scroll](../Inventory/Fireball-Scroll-Requirements.md) consumable for non-mages).
- God definitions, patron quests, deity-specific Priest subtrees.
- Character sheet layout for three classes.
- Balance pass on `maxMagicPower` / Divine formulas.
- Additional Human classes beyond Knight / Mage / Priest.

---

## 12. Content assets (minimum for proof)

| Asset | Purpose |
|-------|---------|
| `HumanClass` enum | Add `Knight`, `Mage`, `Priest` (keep `None = 0`) |
| `DefaultHumanRacialLoadout` | Unchanged for `None`; optional class-specific loadout refs later |
| `KnightSkillTree_Sample` | Might + Finesse passives, D2 gates |
| `PriestSkillTree_Sample` | Might + Finesse passives, D2 gates |
| `Spell_Fireball_Mage` / `Spell_Teleport_Mage` | Tier, equip cost, cast cost, ability refs |
| `HumanPlayer` prefab | Default `None`; optional `HumanKnightPlayer` etc. for QA |
| Runtime components | Knight tree, Priest tree, Mage spells under `Assets/Data/Racial/Human/` |
| Tests | Commit blocks re-class; Mage cannot equip essence; equip budget math; passive ranks stack once |

---

## 13. Acceptance criteria (examples)

- Given `HumanClass.None`, essences and Soul Power behave as today (regression).
- Given commit to **Mage**, `EssenceSlotManager.totalSlots == 0` and equipping an essence **fails**.
- Given **Mage** with `maxMagicPower = 20`, Fireball (tier 3, cost 7) + Teleport (tier 6, cost 4) equipped, `remainingEquip == 9`.
- Given same loadout, attempting to equip a tier-1 spell (cost 9) **fails** without unequipping another spell.
- Given **Mage** with only Fireball equipped, casting Fireball reduces `currentMagicPower` and does **not** use Soul Power.
- Given commit to **Priest**, `MaxSoulPower == 0` and an active with `divinePowerCost` uses Divine Power.
- Given **Knight** with 1 rank in `knight_passive_might`, Strength includes +2 (example) from a distinct modifier source.
- Given `HumanClass.Knight`, attempting to set class to **Priest** via save or debug **rejects**.
- Given class **None**, Priest/Mage runtimes do not apply tree or spell effects.

---

## 14. Code touchpoints (implementation checklist)

| Area | Action |
|------|--------|
| `HumanClass.cs` | Add `Knight`, `Mage`, `Priest` |
| `RacialSubsystemKind.HumanSpecialization` | Wire runtimes for committed classes |
| `CharacterStats` | Magic Power / Divine Power fields; conditional `MaxSoulPower` |
| `EssenceSlotManager` | Respect `maxEssenceSlots` by class |
| `AbilityAction` | `divinePowerCost`, `magicPowerCost` (or resource enum) |
| `PlayerCommandProcessor` / `PlayerAbilitySource` | Mage equipped spell source |
| Data | `Assets/Data/Racial/Human/` trees + spells |
| Saves | `humanClass`, node ranks, known/equipped spell ids |
| UI | Class panel, equip capacity display — **later** |

---

## 15. Design decisions (resolved + open)

### 15.1 — Resolved

| Topic | Decision |
|-------|----------|
| Start state | `HumanClass.None` |
| Class count (initial) | Knight, Mage, Priest |
| Class change count | **Once**, permanent |
| Knight / Priest tree shape | **Diablo 2** prerequisites + per-skill max ranks + per-rank property scaling |
| Mage essences / Soul Power | **Forbidden** |
| Mage casting | **Equipped spells only** |
| Equip cost formula | `10 - tier`; budget = `maxMagicPower - Σ equipped costs` |
| Sample skills | Knight & Priest: +STR / +DEX passives; Mage: Fireball + Teleport |

### 15.2 — Open (not blocking this requirements doc)

- Exact `maxMagicPower` / `maxDivinePower` formulas and level scaling.
- Priest skill point earn rate (parity with level vs shrine).
- Whether Knight passives from the tree stack with essence passives (default **yes**, Pattern B distinct sources).
- Mutual exclusivity groups on Priest/Knight trees (optional).
- Mage spell ranks (single-rank spells in v0 vs tiered spell mastery later).

---

## 16. Related documents

- [Phase 0 — Glossary and data contracts](Phase0-Glossary-And-Data-Contracts.md)
- [Phase 5 — Additional folk & subsystem shapes](Phase5-Requirements.md) (§R5.4 Human class stretch)
- [Undead — Race requirements](Undead-Race-Requirements.md) (contrasting tree policy)
- [Sudden Strength essence](../Essence/Sudden-Strength-Essence-Requirements.md) (Soul Power pattern for Knight)
- [Fireball scroll](../Inventory/Fireball-Scroll-Requirements.md) (non-mage consumable; Mage uses spell equip)
- [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) (level gates for skill nodes)
