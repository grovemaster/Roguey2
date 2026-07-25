# Stat derivation & combat scaling — Requirements (draft)

**Status:** **Implemented (v0)** — dual-track Max HP, armor interaction, band-aware attack contribution, race base HP packages, enemy migration. Magnitudes remain tunable placeholders.

**Purpose:** Replace the current tight coupling **`MaxHP = Constitution × 10`** with a **race/class base + soft Constitution contribution** model, keep numbers **small and readable** (DCSS-inspired) while retaining **D&D-style attribute names**, and define a single **power band** so Max HP, weapon damage, monster HP, and resistances all scale together instead of one attribute driving the whole economy.

**Depends on:** `CharacterStats` (`Assets/Scripts/Stats/CharacterStats.cs` — derived-stat formulas), `Stat` / `StatModifier` (base + layered modifiers), [Party experience & leveling](Party-Experience-And-Leveling-Requirements.md) (per-level growth), `RacialLoadoutDefinition` / `RacialLoadoutApplier` (race stat packages), `HealthComponent` (`Assets/Scripts/Actors/Components/HealthComponent.cs` — damage → resistance → AC application), [Rest](Rest-Requirements.md) (`% MaxHP` heal budget), [Proficiencies](Proficiencies-Requirements.md) (future AC-on-armor, weapon skill).

**Related:** [Equipment stat & class equip](../Equipment/Stat-And-Class-Equip-Requirements.md) (stat minimums on gear), [Blacksmith shop](../World/Blacksmith-Shop-Requirements.md) (+Constitution armor), racial subsystems that add stat/resist modifiers (Spirit Imprint, Soul Beast, Elemental Spirits, Tiefling implants).

**Explicitly out of scope (this doc):** Full class base tables for every Human class (only the framework + a couple of examples); final level-50 tuning numbers; XP curve changes; rewriting the modifier/`Stat` layering system; adding new `DamageType` enum values; critical hits; status-effect scaling beyond how status ticks choose delivery (§8.2); save format changes beyond what new fields require.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Decouple HP from a single attribute** — Remove `MaxHP = Constitution × 10`. Max HP comes from **race + class + level + a soft Constitution contribution**. |
| **G2** | **Race identity in base stats** — Each **`Race`** has a **starting attribute package** and a **base HP** (e.g. Barbarian high HP/Strength, low Dexterity; Human average). |
| **G3** | **Small, readable numbers** — Default to low integers (attributes ~6–20 early; Max HP tens, not hundreds), DCSS-style. |
| **G4** | **Keep D&D attribute names** — Strength, Dexterity, Agility, Constitution, Intelligence, Wisdom, Charisma, Luck stay; flat integers, no forced modifier table (modifier layer optional/internal). |
| **G5** | **Constitution is dual-purpose** — Contributes to **Max HP** (secondary source) **and** stays the driver of **encumbrance** and (optionally) resilience — never the sole HP source. |
| **G6** | **One power band** — Max HP, weapon damage, monster HP, and resistances share a documented scaling band per game stage so damage keeps pace with HP. |
| **G7** | **Data-driven & scalable** — Bases, per-level growth, and damage live in **authored tables/curves** so late-game ceilings can rise later without code rewrites. |
| **G8** | **No linear attribute→pool coupling** — No derived combat pool may equal `attribute × N` for large N (the mistake being corrected). |
| **G9** | **Backward-safe migration** — Existing enemies/party assets keep working after the formula change (documented conversion so a Con-10 actor doesn’t silently drop from 100 HP to 10). |
| **G10** | **Separate damage type from armor interaction** — Typed resistance answers *what* hits you; armor interaction answers *whether / how much* AC applies. A Fireball can be Fire **and** partially blocked by armor; a poison tick can be Poison **and** ignore armor. |

---

## 2. Glossary

| Term | Meaning |
|------|---------|
| **Attribute** | One of the eight `StatType` core scores (Strength…Luck). Small integers. |
| **Derived stat** | A value computed from attributes/level/race (Max HP, AC, encumbrance, MoveSpeed, resource pools). |
| **Base HP** | The race/class portion of Max HP before Constitution and level. |
| **Con contribution** | The (soft) portion of Max HP from Constitution (e.g. `+Con` or `+floor(Con/2)`). |
| **Power band** | The intended numeric range for HP / damage / monster HP at a given game stage (§7). |
| **Race package** | Starting attribute values + base HP for a `Race`, authored as data. |
| **Class package** | Additional base HP / attribute biases applied on Human class commit (future; framework now). |
| **Attribute modifier (optional)** | A D&D-style `floor((score−10)/2)` value. **Internal/optional**; not shown to players in v0 (§6). |
| **Damage type** | The resistance channel (`DamageType`: Blunt, Fire, Poison, …). Answers *what elemental / kinetic flavor* this is. |
| **Armor interaction** | How AC applies to this hit: **Full**, **Partial**, or **None** (§8.2). Orthogonal to damage type. |
| **Delivery** | Design shorthand for why a hit has a given armor interaction (weapon contact, physical projectile/explosion, pure magic, status tick). |

---

## 3. Current baseline (as-is)

From `CharacterStats.cs`:

| Derived stat | Current formula | Problem |
|--------------|-----------------|---------|
| **Max HP** | `Constitution.GetValue() * 10` | One attribute = whole HP pool; explodes with Con growth (**G1/G8**) |
| **Encumbrance** | `Constitution.GetValue() * 5` | Fine to keep; Con dual-purpose (**G5**) |
| **Armor Class** | `10 + Dexterity/4` | Keep shape; revisit magnitude in band |
| **Move Speed** | `1.0 - Agility*0.01` | Keep; unrelated to HP band |
| Resource pools | `HumanClassRules` / `DragonianRules` | Out of scope except HP parity |

**Damage application** (`HealthComponent.TakeDamage`): `damage = max(1, raw − resistance)`, then for **Blunt/Slash/Pierce only** `damage = max(1, damage − ArmorClass/5)`. Minimum **1** damage always applies. Elemental types currently **ignore AC** entirely — this is the Fireball gap addressed in §8.2.

**Starting attributes:** all default to `new Stat(10)`; race packages currently applied (if any) via `RacialLoadoutDefinition.statModifiers`. There is **no** per-race **base HP** concept yet.

---

## 4. Attribute roles (locked intent)

Define what each attribute governs so scaling is intentional. Magnitudes tunable; **roles** locked.

| Attribute | Primary role | Secondary | Notes |
|-----------|--------------|-----------|-------|
| **Strength** | Melee damage contribution; carrying/heavy-gear thresholds | Some athletics | Should influence damage **band**, not raw HP |
| **Dexterity** | **Accuracy / Armor Class**; finesse weapons | Ranged to-hit | Keep `AC = base + Dex/k` shape |
| **Agility** | **Move speed**; evasion (future) | Turn order (future) | Distinct from Dex (aim vs footwork) |
| **Constitution** | **Base HP contribution + encumbrance** | Resilience / poison-stun resist (future) | **Dual track** (§5); never sole HP source |
| **Intelligence** | Mage magic power / spell scaling | Some skills | Class-gated resource |
| **Wisdom** | Priest divine power; perception/insight | Willpower (future) | Class-gated resource |
| **Charisma** | Social / prices / recruitment (future) | Renown interplay | Minimal combat role |
| **Luck** | Skill-check bonus (`Luck/10`), crit/loot nudges (future) | — | Keep small influence |

**Locked:** Damage does **not** read Max HP; HP does **not** read Strength. Cross-wiring stays through explicit, band-aware formulas only.

---

## 5. Max HP model (dual track) — the core change

### 5.1 — Formula (proposed default)

```text
MaxHP =
    RaceBaseHP                 // per Race package (§5.3)
  + ClassBaseHP               // 0 until Human class commit; then per class (future)
  + LevelHPGain(level, class) // from curve/table (§5.4)
  + ConContribution(Con)      // soft (§5.2)
  + external modifiers        // gear / essence / racial (already supported)
```

**Track A — Constitution (attribute):** durability identity, encumbrance, soft HP bump.
**Track B — Max HP (pool):** the survivability number driven mostly by race/class/level.

### 5.2 — Constitution contribution options

Pick one default (all satisfy G5/G8):

| Option | Formula | +1 Con gives | Feel |
|--------|---------|--------------|------|
| **C1 (recommended)** | `ConContribution = Constitution` | +1 HP | Simple, honest, small |
| **C2** | `ConContribution = floor(Constitution / 2)` | +0–1 HP | Softer; good if Con grows fast per level |
| **C3 (D&D-style)** | `ConContribution = floor((Con−10)/2) × HpPerConMod` | scales in steps | Familiar to D&D; two-layer cognition |

**Locked direction:** ship **C1** unless playtest shows Con growth is too swingy, then switch to **C2** (single-line change). C3 only if we later adopt modifiers globally (§6).

### 5.3 — Race base packages (data-driven)

Each `Race` authors a starting package: **base HP** + **starting attributes**. Illustrative (not final):

| Race | RaceBaseHP | Str | Dex | Agi | Con | Int | Wis | Cha | Luck |
|------|-----------|-----|-----|-----|-----|-----|-----|-----|------|
| **Human** | 12 | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 10 |
| **Barbarian** | 18 | 13 | 8 | 9 | 12 | 8 | 9 | 9 | 10 |
| **Elf** | 10 | 9 | 12 | 11 | 8 | 11 | 11 | 10 | 10 |
| **Dwarf** | 14 | 11 | 9 | 8 | 12 | 9 | 11 | 9 | 10 |
| … | … | … | … | … | … | … | … | … | … |

**Locked:** numbers above are **placeholders**; the **mechanism** (per-race base HP + attribute package as authored data) is the requirement. Existing `RacialLoadoutDefinition` may host the attribute package; **base HP** needs a new authored field (race package or `CharacterStats`).

### 5.4 — Level HP growth

- `LevelHPGain` from an authored **curve/table** (per class where classes exist; race/default otherwise).
- Default placeholder: **+3 to +6 Max HP per level** for a frontliner-ish curve; casters lower.
- Replaces the current “level Constitution → Max HP rises via ×10” side effect. Level-up **may still** grant +Con (per [leveling §6.3](Party-Experience-And-Leveling-Requirements.md)) but HP growth is **primarily** the level table, not Con×10.

### 5.5 — Worked examples (with C1 + placeholders)

| Actor | RaceBase | Class | Level gain (L5) | Con | ConContrib | MaxHP |
|-------|----------|-------|-----------------|-----|-----------|-------|
| Human fighter L1 | 12 | +4 | 0 | 10 | +10 | **26** |
| Human fighter L5 | 12 | +4 | +20 | 12 | +12 | **48** |
| Barbarian L5 | 18 | +6 | +24 | 14 | +14 | **62** |
| Human mage L5 | 12 | +0 | +8 | 10 | +10 | **30** |

All land in a readable band (§7), not the 100–500 the old formula produced.

---

## 6. D&D modifiers — decision (locked for v0)

- **v0 uses flat integers.** Players see `Con 12`, `MaxHP 48`, `Strength 14`. No hidden `+3`.
- An **optional internal** `AttributeModifier(score) = floor((score−10)/2)` helper **may** exist for formulas that want soft scaling (e.g. C3, future to-hit), but is **not** surfaced in UI in v0.
- If we later adopt modifiers game-wide, add a **hybrid display** (`Con 16 (+3)`) — tracked as future, not v0.

**Why:** flat integers match the “small numbers” goal and are easy to remove-free later; modifiers are additive to adopt but hard to retract once taught.

---

## 7. The power band (one economy)

**Locked principle:** Max HP, weapon damage, monster HP, and effective resistances must share a **band per stage** so damage keeps pace with HP (**G6**).

Working band (tunable; treat late-game as a content dial, not a hard cap):

| Stage | Frontliner MaxHP | Typical player hit | Trash monster HP | Notable monster HP |
|-------|------------------|--------------------|------------------|--------------------|
| **Tutorial / Floor 1** | 18–35 | 3–8 | 8–20 | 20–40 |
| **Mid dungeon** | 40–75 | 8–16 | 25–50 | 60–110 |
| **Deep / Lords** | 80–140 | 14–28 | 60–120 | 150–300 (boss multiplier) |

Rules:
- **Move columns together.** If we raise late-game HP later, weapon/monster tables rise in the **same** edit.
- **Bosses scale via multipliers**, not by inflating every trash mob (a Lord of the Floor can be 3× a giant without skeletons hitting 200 HP).
- **Minimum 1 damage** stays (chip damage guaranteed).
- **Headroom:** because everything is table-driven (G7), the ceiling can move past this band for later content without touching `CharacterStats` formulas.

---

## 8. Damage scaling

### 8.1 — Weapon / attack damage

- Base damage from **weapon data** (authored), within the §7 band.
- **Strength contribution** to melee: small, band-aware — default proposal `+floor(Strength/4)` or a table, **not** `+Strength`. Prevents Str growth from doubling damage.
- Ranged/finesse may read **Dexterity** for to-hit and a smaller damage share.
- Keep all contributions in a single `AttackDamageLogic` so the band is enforced in one place.

### 8.2 — Damage type vs armor interaction (reconciles Fireball vs poison)

**Problem with as-is:** `HealthComponent` applies AC only when `DamageType` is Blunt / Slash / Pierce. That treats *type* as a stand-in for *physicality*. A Fireball is Fire, but it is also a physical ball of fire — armor should blunt some of it; Fire resist should blunt it more. A poison status tick is also typed (Poison) but should ignore armor.

**Locked reconciliation:** split the hit into two orthogonal fields.

| Field | Answers | Examples |
|-------|---------|----------|
| **`DamageType`** | Which resistance applies? | Fireball → `Fire`; sword → `Slash`; poison tick → `Poison` |
| **`ArmorInteraction`** | Does AC reduce this hit? | Fireball → **Partial**; sword → **Full**; poison tick → **None** |

```text
ArmorInteraction:
  Full    — AC mitigation at full strength (AC / k), current physical weapons
  Partial — AC mitigation at reduced strength (AC / k_partial, or fraction of Full)
  None    — AC ignored; only typed resistance applies
```

**Default content mapping (authorable per attack / ability / status / trap):**

| Source | DamageType | ArmorInteraction | Why |
|--------|------------|------------------|-----|
| Melee / most weapons | Blunt / Slash / Pierce | **Full** | Contact with armor |
| Bow / thrown physical | Pierce (etc.) | **Full** | Physical projectile |
| Fireball, acid splash, similar | Fire / Acid / … | **Partial** | Physical mass/heat hits the body; armor helps some |
| Lightning bolt (arcing energy) | Lightning | **Partial** or **None** | Author per fantasy; default **Partial** if it arcs through armor |
| Poison / disease status ticks | Poison | **None** | Internal; armor irrelevant |
| Psychic / pure Force blasts | Psychic / Force | **None** | No physical contact |
| Environmental puddles (optional) | Acid / Fire / … | **Partial** or **None** | Author; standing in fire may be Partial |

**Effectiveness hierarchy (design intent):** for a Fireball, **Fire resist > Partial AC > nothing**. Armor helps; typed resist helps more. That matches the Fireball intuition without splitting the hit into fake Blunt+Fire packets.

**Rejected alternatives (documented so we don’t re-litigate):**

| Alternative | Why not (for v0) |
|-------------|------------------|
| Split Fireball into Blunt + Fire packets | Messy UI, double combat-log lines, resist/AC double-count edge cases |
| Give every elemental type Full AC always | Over-values AC vs Fire resist; poison would wrongly get AC |
| Keep type→AC hardcode (Blunt/Slash/Pierce only) | Fails the Fireball case |

### 8.3 — Damage application order

```text
raw            = weaponBase + attributeContribution + buffs
afterResist    = max(1, raw − resistance[type])              // always; typed
armorMit       = ArmorMitigation(AC, interaction)            // Full / Partial / None
afterArmor     = max(1, afterResist − armorMit)
finalDamage    = afterArmor                                  // always ≥ 1
```

```text
ArmorMitigation(AC, Full)    = AC / k              // current k ≈ 5
ArmorMitigation(AC, Partial) = AC / k_partial      // e.g. k_partial ≈ 10, or floor(Full/2)
ArmorMitigation(AC, None)    = 0
```

- `k` / `k_partial` tuned so AC is meaningful but not immunity within the band.
- Call sites pass `(raw, DamageType, ArmorInteraction)` into `HealthComponent` (or a thin combat resolver). Legacy untyped `TakeDamage(amount, source)` → `(amount, Blunt, Full)`.
- Existing Blunt/Slash/Pierce callers that omit interaction default to **Full**.
- Elemental abilities that omit interaction should not silently get Full — prefer an explicit authored default of **Partial** for projectile/explosion elemental, **None** for status ticks (**O8**).
- Consider a **cap on total flat mitigation** (resist + armor) as a fraction of `raw` (e.g. ≤ 80%). **Open question O3.**

### 8.4 — Worked examples

Assume AC 14 → Full mit = `14/5 = 2`, Partial mit = `14/10 = 1`. Fire resist 4. Poison resist 0.

| Hit | Raw | After resist | Armor | Final |
|-----|-----|--------------|-------|-------|
| Sword Slash, Full | 12 | 12 (Slash resist 0) | −2 | **10** |
| Fireball Fire, Partial | 12 | 8 (Fire −4) | −1 | **7** |
| Fireball, no Fire resist, Partial | 12 | 12 | −1 | **11** |
| Fireball, Fire resist 4, **if** wrongly None | 12 | 8 | 0 | **8** (armor wasted — rejected) |
| Poison tick Poison, None | 5 | 5 | 0 | **5** |
| Poison tick, if wrongly Full | 5 | 5 | −2 | **3** (armor heals poison — rejected) |

### 8.5 — Strength/level must not outrun HP

- Damage growth per level (via weapons/tables) must track the **same band** as `LevelHPGain`. Document target: a same-tier fight should last a **similar number of hits** across stages (e.g. ~4–8 hits to down a peer), not trend toward one-shots or infinite chip.

---

## 9. Resistance scaling

### 9.1 — Model (locked: flat, typed, band-aware)

- Keep **per-`DamageType` flat resistance** (subtracted **before** armor), as today.
- Resistances are **small integers** sized to the band: early resist values ~1–4, deep ~6–12. A resist should blunt a hit, not zero it.
- **Minimum 1 damage** already guarantees no full immunity from stacked flat resist.
- **Typed resist is independent of armor interaction.** Fire resist always applies to Fire hits whether armor is Full, Partial, or None. Armor never substitutes for a missing resist.

### 9.2 — Optional percentage layer (future)

- If flat resist becomes swingy at high tiers, add an **optional percentage resist** applied after flat (e.g. `afterPct = afterResist × (1 − pctResist)`), capped (e.g. ≤ 75%). **Out of v0**; note as extension so content authors know the ceiling.

### 9.3 — Sources unchanged

- Racial loadouts, essences, Spirit Imprint, Soul Beast, implants already push `DamageResistanceModifier`s through `Stat` layers — **no change** to that pipeline; only the **magnitude guidance** (band) is new.

---

## 10. Other derived stats

| Derived | v0 rule | Notes |
|---------|---------|-------|
| **Encumbrance** | Keep `Constitution × 5` (or soften to `Con × 4` later) | Con dual-purpose (G5) |
| **Armor Class** | Keep `10 + Dexterity/4` | Magnitude fits band via `AC/k` in damage step |
| **Move Speed** | Keep `1.0 − Agility×0.01` | Unrelated to HP band |
| **Resource pools** | Unchanged (class/race rules) | Ensure HP-parity behaviors (heal-on-levelup) still work |
| **Skill checks** | Keep `d20 + skill + Luck/10` | D&D-style check stays; small numbers |

---

## 11. Migration from `Con × 10`

**Locked requirement (G9):** changing the formula must not silently gut existing actors.

| Step | Action |
|------|--------|
| **M1** | Introduce `RaceBaseHP` (+ optional `ClassBaseHP`) fields and `LevelHPGain` table with defaults chosen so a **default Human (Con 10, L1)** lands in-band (~22–26), not 100. |
| **M2** | Audit enemy `CharacterStats` (Skeleton, Giant Skeleton, Giant Skeleton King, Goblin, Ghoul, Dire Wolf, etc.) — set explicit **base HP** so their effective HP matches intended §7 band (giants tanky, trash low). |
| **M3** | Re-tune weapon damage + monster HP to the band in the **same** pass (G6). |
| **M4** | Verify [Rest](Rest-Requirements.md) `% MaxHP` and level-up heal-by-delta still behave (smaller MaxHP → smaller but proportional heals). |
| **M5** | Keep `EncumbranceLimit` behavior stable so inventory carrying doesn’t change unexpectedly. |
| **M6** | Update [leveling §6.3](Party-Experience-And-Leveling-Requirements.md) to state Max HP comes from the level table + Con contribution, not Con×10. |

**Do not** ship the formula swap without M2–M3, or existing content HP/damage will desync.

---

## 12. Data & code touch-points (target)

| Area | Change |
|------|--------|
| `CharacterStats.MaxHP` | Replace `Con*10` with §5.1 sum (reads race/class base + level table + Con contribution + modifiers) |
| New: `RaceBaseHP` / attribute package data | Author per `Race` (extend `RacialLoadoutDefinition` or a new `RaceStatPackage` asset) |
| New: `LevelHpCurve` (per class/default) | Data-driven §5.4 |
| New: `HpDerivationLogic` (pure) | Testable formula; keeps `CharacterStats` thin |
| `AttackDamageLogic` (new/collect) | Band-aware weapon + attribute damage (§8.1) |
| `HealthComponent` | Accept `ArmorInteraction`; apply Full / Partial / None AC (§8.2–8.3); optionally mitigation cap (O3) |
| Ability / trap / status / ranged callers | Author `(DamageType, ArmorInteraction)` per hit — Fireball Partial Fire, poison ticks None Poison, etc. |
| Enemy species/prefabs | Explicit base HP per M2 |
| Tests | HP derivation, band examples, migration parity, damage/resist math |

---

## 13. Acceptance criteria (design targets)

| ID | Criterion |
|----|-----------|
| **AC1** | `MaxHP` no longer equals `Constitution × N`; it sums race + class + level + soft Con + modifiers. |
| **AC2** | A default **Human L1** has Max HP in the tutorial band (§7), not ~100. |
| **AC3** | A **Barbarian L1** has higher base HP and Strength, lower Dexterity than Human L1, from **data**. |
| **AC4** | +1 Constitution raises Max HP by a **small** amount (C1: +1) and still raises encumbrance. |
| **AC5** | Level-up raises Max HP primarily via the **level table**, not via Con×10. |
| **AC6** | Weapon damage, monster HP, and Max HP sit in the same **stage band**; a same-tier fight takes several hits, not one. |
| **AC7** | Resistances are small flat values that blunt (not negate) typed damage; minimum 1 damage holds. |
| **AC8** | Attributes display as **flat integers**; no modifier shown in v0. |
| **AC9** | After migration, existing enemies retain sensible, in-band HP (no silent 10× drop). |
| **AC10** | Bases, level curve, weapon damage, and resist values are all **authored data**, editable without code changes. |
| **AC11** | Armor interaction is **not** inferred from `DamageType` alone: a Fireball-like hit uses Fire + **Partial** AC; a poison tick uses Poison + **None**; melee uses physical type + **Full**. Fire resist reduces Fireball more than Partial AC alone. |

---

## 14. Open questions

| ID | Question | Default if unresolved |
|----|----------|------------------------|
| **O1** | Late-game frontliner Max HP ceiling? | Design toward ~**80–140**; keep table-driven so it can rise later |
| **O2** | Con contribution C1 (`+Con`) vs C2 (`+Con/2`)? | **C1** now; switch to C2 if per-level Con growth feels swingy |
| **O3** | Cap total flat mitigation (resist + armor) as % of raw? | Add an **80% cap** if high-tier defenses trivialize the band |
| **O4** | Store race base HP on `RacialLoadoutDefinition` or a new `RaceStatPackage`? | New lightweight field/asset if loadout coupling is awkward |
| **O5** | Do non-Con attributes grow on level-up? | Keep v0 (Con-focused) unless class tables add more |
| **O6** | Add optional **percentage** resistance layer now or later? | **Later** (§9.2); flat-only for v0 |
| **O7** | Strength→damage: `+Str/4` vs authored table? | `+floor(Str/4)` placeholder; move to table if weapons need per-tier control |
| **O8** | Partial AC formula: `AC/10` vs `floor((AC/5)/2)`? | **`AC/10`** (half the Full mitigation at current k=5) |
| **O9** | Default for Lightning / Force / Radiant projectile spells? | **Partial** for “physical-feeling” energy; **None** for pure mental/Force/status — author per ability |
| **O10** | Show armor interaction in combat log / tooltips? | v0: optional debug; player-facing later (“armor softens the blast”) |

---

## 15. Revision history

| Date | Change |
|------|--------|
| 2026-07-25 | Initial draft — dual-track Max HP (race/class/level + soft Con), attribute roles, single power band, damage & resistance scaling, migration from `Con×10` |
| 2026-07-25 | §8.2–8.4 — split **DamageType** vs **ArmorInteraction** (Full / Partial / None); Fireball Partial Fire vs poison None; worked examples; G10 / AC11 / O8–O10 |
| 2026-07-25 | **Implemented v0** — `HpDerivationLogic`, `DamageApplicationLogic`, `AttackDamageLogic`, `ArmorInteraction`; `CharacterStats.MaxHP` dual-track; level `hpPerLevel`; race loadout `raceBaseHp`; HealthComponent armor interaction; call-site wiring; enemy/race migration; unit tests |
