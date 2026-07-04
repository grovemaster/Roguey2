# Stat & class equipment requirements — Requirements

**Purpose:** Specify **selective** equip gates for weapons and armor: some gear is **unrestricted** (iron knife, cloth, handheld torch), while **heavy martial gear** and **oversized weapons** require a **martial calling** (Human **Knight** or unclassed **None** — not **Mage** or **Priest**), and **high-tier endgame gear** additionally requires **minimum effective stats** and/or **character level**. A level-1 Human with baseline stats who has **not** committed to Mage/Priest may equip plate; a level-1 Human **Mage** with similar attributes may **not**, even though their STR/DEX look the same on paper.

**Status:** Proposed — not implemented. Today `EquipmentLegalityEvaluator` enforces slot/category, bow pairing, and **anatomy** (`BodyCapabilityFlags`); **`Giants_Blade`**, **`Armor_HelmetOfLight`**, and iron/steel chestplates have **no** class or stat gates authored.

**Depends on:** `ItemData`, `EquipmentManager`, `EquipmentLegalityEvaluator`, `CharacterStats` / `Stat.GetValue()`, `HumanClass`, `HumanClassRules`, [Human — Class powers](../RacialSystem/Human-Class-Powers-Requirements.md), [Proficiencies](../Progression/Proficiencies-Requirements.md) (soft penalties vs hard gates), [Character equipment menu](../UI/Character-Equipment-Menu-Requirements.md), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md), [Blacksmith shop](../World/Blacksmith-Shop-Requirements.md), [Light-emitting items](../World/Light-Emitting-Items-Requirements.md) (Helmet of Light).

**Related:** [Phase 4 / Phase 5 equip pipeline](../RacialSystem/Phase5-Requirements.md) (`BodyCapabilityFlags`, exclusion bypass), [Tiefling — Cyborg implants](../RacialSystem/Tiefling-Cyborg-Implants-Requirements.md) (horns vs helmets — **anatomy**, not class), [Warrior Willpower](../RacialSystem/Warrior-Willpower-Healing-Potion-And-Stun-Requirements.md) (`RacialTraitFlags` vs body capabilities — class gates stay on **equip requirements**, not trait flags).

**Explicitly out of scope (v0 of this feature):** Respec or temporary buffs that **fake** class commitment; NPC-only gear with special bypass rules; **proficiency hard gates** as the primary plate gate (optional v1+ — §12); encumbrance tier changes; two-handed / off-hand **size** enforcement beyond existing `handsRequired`; retroactive re-validation of already-equipped illegal gear on class commit (handled in §8.4); gamepad-specific requirement UI.

---

## Locked decisions (proposed — confirm before implementation)

| # | Decision |
|---|----------|
| **L1** | Equip gates apply to **some** items only. Default for new gear: **no** stat or class requirements unless authored. |
| **L2** | **`EquipmentLegalityEvaluator.CanEquip`** remains the **single runtime gate** for inventory equip, floor pickup auto-equip (if any), and shop “buy and equip” flows. |
| **L3** | **Martial calling gate** blocks **`HumanClass.Mage`** and **`HumanClass.Priest`** from equipping tagged gear. **`HumanClass.None`** and **`HumanClass.Knight`** pass. Non-Human folk **pass** martial calling unless a future folk-specific rule says otherwise. |
| **L4** | Martial calling is **independent of stat totals**. A level-1 Mage with STR 18 still **cannot** equip plate; a level-1 unclassed Human with STR 8 **can**. |
| **L5** | **Stat minimums** use **effective** stat values (`Stat.GetValue()` after modifiers), evaluated at equip attempt time. |
| **L6** | **Character level** minimums (when authored) use `CharacterStats.level`. |
| **L7** | Failure reasons are **player-facing strings** naming the first failed rule (class, stat, level) — not internal enum dumps. |
| **L8** | **Anatomy rules unchanged.** Horns, reduced stature, etc. stay on `BodyCapabilityFlags` / `equipExcludesActorFlags` — do not overload body flags for Mage/Priest. |
| **L9** | **Iron / steel chestplate** (medium shop tier) stays **unrestricted** in v0 unless explicitly tagged later. **Plate** (future heavy tier) uses **martial calling only** at baseline — no stat floor at introduction. |
| **L10** | **Giant's Sword** (`Giants_Blade`, display **Giant's Sword**) requires **martial calling**; no stat minimum in v0. |
| **L11** | **Helmet of Light** requires **martial calling** (full martial helm — same class rule as plate); no stat minimum in v0. |
| **L12** | Inventory / shop UI **shows requirements** on inspect and greys unusable equip actions with the same reason string as runtime. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Class fantasy** — Arcane and divine specialists do not wear full plate or wield oversized martial weapons meant for front-line fighters. |
| **G2** | **Stat progression fantasy** — Legendary armor and weapons can demand **high STR/CON/DEX** (etc.) so only developed martial characters qualify — after passing the class gate. |
| **G3** | **Selective authoring** — Designers opt in per item; mundane gear stays frictionless. |
| **G4** | **Same stats, different rules** — Equip eligibility reflects **calling/commitment**, not only attribute numbers. |
| **G5** | **One evaluator** — No duplicate gate logic in UI, shop, or combat. |
| **G6** | **Composable with proficiencies** — Hard class/stat gates coexist with soft **Armour** proficiency penalties ([Proficiencies §8.2](../Progression/Proficiencies-Requirements.md)). |
| **G7** | **Clear UX** — Player understands *why* equip failed (“Mages cannot wear plate armor” vs “Requires Strength 16 (you have 12)”). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Equip requirement** | Optional authored rules on an item: martial calling, stat floors, level floor, (future) proficiency floor. |
| **Martial calling** | Actor is allowed **heavy martial** gear. Human: `None` or `Knight`. **Not** Mage or Priest. |
| **Martial-capable** (code-facing) | Passes the martial calling gate. Player copy may say **warrior**, **fighter**, or **martial class**. |
| **Heavy martial gear** | Items tagged **`requiresMartialCalling`** — e.g. plate armor, Giant's Sword, Helmet of Light. |
| **Stat floor** | Minimum **effective** value for one or more `StatType` entries. |
| **Unrestricted gear** | No equip requirement fields set — anyone who passes slot/anatomy rules may equip. |
| **Effective stat** | `CharacterStats.GetStatByType(...).GetValue()` including equipment/essence/passive modifiers at check time. |
| **Anatomy gate** | Existing Phase 4 pipeline: `equipRequiresAllFlags`, `equipExcludesActorFlags`, essence bypass masks. |

---

## 3. Reference — design intent

### 3.1 — Surviving the Game as a Barbarian (primary tone)

| STBGB idea | This system |
|------------|-------------|
| Mages study books; they are not plate-clad bruisers | Human **Mage** / **Priest** blocked from **heavy martial** tags |
| Warriors and civilians pick up big weapons and armor | Human **None** / **Knight** (and other folk) may |
| Late-game gear rewards built characters | **Stat floors** on authored endgame pieces |
| Not every item has a gate | **Opt-in** per `ItemData` |

### 3.2 — Dungeons & Dragons (secondary)

| D&D idea | This project |
|----------|--------------|
| Proficiency with heavy armor | [Proficiencies](../Progression/Proficiencies-Requirements.md) — **soft** penalty / AC bonus; **class gate** is separate and **hard** for tagged gear |
| STR minimum for heavy armor (table variant) | **Stat floor** on specific legendary items, not all plate |
| Class armor proficiency | **Martial calling** for Human Mage/Priest only in v0 |

### 3.3 — Dungeon Crawl Stone Soup (tertiary)

| DCSS idea | This project |
|-----------|--------------|
| Some races/classes cannot use certain armour | **Martial calling** + optional future **proficiency hard gate** (§12) |
| Stat-dependent artefact equip | **Stat floors** on selected artifacts |

---

## 4. Problem statement (authoritative example)

**Scenario A — level 1 unclassed Human (`HumanClass.None`):** STR 10, DEX 10, CON 10. **May equip** plate armor (when authored) and **Giant's Sword**.

**Scenario B — level 1 Human Mage (`HumanClass.Mage`):** STR 10, DEX 10, CON 10 (same numbers). **May not equip** plate or Giant's Sword — failure reason cites **class**, not stats.

**Scenario C — level 18 Human Knight:** STR 16, CON 14. **May equip** baseline plate and Giant's Sword. **May not equip** (example) **Adamant Warplate** if authored with `Strength ≥ 18` — failure cites **Strength**.

**Scenario D — level 18 Human Mage:** Even with STR 18 from gear buffs, **still may not equip** plate — martial calling fails before stat checks.

---

## 5. Requirement layers (evaluation order)

All checks run in **`EquipmentLegalityEvaluator.CanEquip`** after existing slot/category/bow rules.

| Order | Layer | Source | v0 |
|-------|--------|--------|-----|
| 1 | Slot / category / bow pairing | Existing | Yes |
| 2 | Anatomy — required flags | `item.equipRequiresAllFlags` | Yes |
| 3 | Anatomy — excluded flags | `item.equipExcludesActorFlags` + bypass | Yes |
| 4 | **Martial calling** | `item.requiresMartialCalling` | **New** |
| 5 | **Character level** | `item.minimumCharacterLevel` | **New** |
| 6 | **Stat floors** | `item.statMinimums[]` | **New** |
| 7 | Proficiency floor | `item.minimumProficiency` + level | Future (§12) |

**Short-circuit:** Return on **first** failure with one clear `reason` string.

---

## 6. Data model (proposed)

### 6.1 — Fields on `ItemData`

Add to **`ItemData`** (names stable for assets; adjust in code review if needed):

| Field | Type | Default | Meaning |
|-------|------|---------|---------|
| **`requiresMartialCalling`** | `bool` | `false` | When true, Human Mage/Priest cannot equip. |
| **`minimumCharacterLevel`** | `int` | `0` | `0` = no level gate; else `stats.level >= value`. |
| **`statMinimums`** | `StatMinimumRequirement[]` | empty | All entries must pass (AND). |

```csharp
[System.Serializable]
public struct StatMinimumRequirement
{
    public StatType stat;
    [Min(1)] public int minimumEffectiveValue;
}
```

**Future (not serialized in v0):** `minimumProficiency` + `minimumProficiencyLevel` — see §12.

### 6.2 — Designer tags (documentation only)

Authors think in **tags**; implementation is the three fields above.

| Designer tag | Typical authoring |
|--------------|-------------------|
| *(none)* | Default — unrestricted beyond anatomy |
| **Heavy martial armor** | `requiresMartialCalling = true` |
| **Oversized martial weapon** | `requiresMartialCalling = true` |
| **Martial artifact helm** | `requiresMartialCalling = true` |
| **Legendary war gear** | `requiresMartialCalling = true` + stat floors + optional level |

No separate **`ArmorWeightClass`** enum is required for v0; weight class is a **content convention** documented in §7.

### 6.3 — What does *not* move onto `ItemData`

| Concern | Keep on |
|---------|---------|
| Horns block narrow helmets | `equipExcludesActorFlags` |
| Fairy stature / anatomy | `BodyCapabilityFlags` |
| Warrior Willpower potion gate | `RacialTraitFlags` |
| Bow requires arrows | Existing bow rules |

---

## 7. Content tiers & example items

### 7.1 — Armor weight (convention)

| Tier | Examples (current / planned) | Martial calling | Stat floor (v0) |
|------|------------------------------|-----------------|-----------------|
| **Light** | Cloth, robes, handheld torch accessory | No | No |
| **Medium** | Iron Chestplate, Steel Chestplate | No | No |
| **Heavy / plate** | *Plate armor* (future asset) | **Yes** | No at intro |
| **Legendary plate** | *Adamant Warplate* (future) | **Yes** | Yes (e.g. STR 18, CON 16, level 15) |

Medium tier stays unrestricted so early blacksmith gear remains usable by all classes; **plate** is the teaching example for class gates.

### 7.2 — Weapons

| Item | Asset | Martial calling | Stat floor (v0) | Notes |
|------|-------|-----------------|-----------------|-------|
| Iron / Steel Knife, Sword | `Weapon_*` shop line | No | No | Mundane |
| **Giant's Sword** | `Giants_Blade` | **Yes** | No | Oversized martial weapon; display name **Giant's Sword** |
| Future **colossal** weapons | TBD | **Yes** | Optional high STR | Same pattern |

### 7.3 — Helmet of Light

| Field | Value |
|-------|-------|
| **Asset** | `Armor_HelmetOfLight` |
| **Slot** | Head |
| **Martial calling** | **Yes** — treated as **martial war helm**, not caster headgear |
| **Stat floor (v0)** | No |
| **Rationale** | Same class rule as plate: priests and mages rely on light spells / torches, not a heavy radiant helm |

Light-emitting **accessory** torch remains unrestricted (§7.1 Light).

### 7.4 — Authoring checklist (new heavy item)

1. Set slot, stats, passives as today.
2. Decide tier (§7.1 / §7.2).
3. If heavy martial or oversized weapon → `requiresMartialCalling = true`.
4. If endgame power spike → add `statMinimums` and/or `minimumCharacterLevel`.
5. Verify anatomy flags (horns, etc.) separately.
6. Add inspect string / shop copy for requirements (§9).

---

## 8. Runtime behavior

### 8.1 — Martial calling resolver

Pseudocode:

```
bool PassesMartialCalling(CharacterStats stats):
    if !item.requiresMartialCalling: return true
    if stats.race != Human: return true   // v0: other folk pass
    return stats.humanClass is None or Knight
```

**Human Mage / Priest failure copy (locked v0):**

- Armor: *“{Class}s cannot wear this armor.”* (e.g. *Mages cannot wear this armor.*)
- Weapon: *“{Class}s cannot wield this weapon.”*
- Generic: *“Your class cannot equip this item.”*

Use **`HumanClass`** display names: **Mage**, **Priest**, **Knight**, **Unclassed** (for None in UI if ever shown).

### 8.2 — Stat and level checks

- **`minimumCharacterLevel`:** fail if `stats.level < minimumCharacterLevel`.  
  Copy: *“Requires character level {N} (you are level {current}).”*
- **`statMinimums`:** for each entry, fail if `GetValue() < minimumEffectiveValue`.  
  Copy: *“Requires {StatName} {N} (yours: {current}).”*  
  If multiple fail, report the **first authored entry** in v0 (keep simple).

### 8.3 — Equip attempt surfaces

| Surface | Behavior |
|---------|----------|
| Inventory **Equip** | Block + toast/log with `reason` |
| Character equipment menu | Read-only v0 — show requirements in detail pane when implemented |
| Shop buy → equip | Block equip; item still purchased to bag if shop allows |
| Debug / editor force equip | Should still call evaluator (no silent bypass) |

### 8.4 — Class commitment while gear equipped

When a Human commits **None → Mage/Priest** ([Human class powers](../RacialSystem/Human-Class-Powers-Requirements.md)):

1. Scan equipped slots for **`requiresMartialCalling`** items.
2. **Unequip to bag** each illegal piece (same path as manual unequip).
3. Log: *“{Name} can no longer wear {item} as a {class}.”*

**v0:** No player choice to “keep wearing” illegal gear.

### 8.5 — Give / party inventory transfer

[Holy Land inventory Give](../RacialSystem/Barbarian-Holy-Land-Requirements.md) and bag transfers are unaffected — requirements apply only on **equip**, not ownership.

---

## 9. UI & copy

### 9.1 — Inspect / detail pane

When an item has any requirement field set, append a **Requirements** block:

```
Requirements:
  • Martial class (not Mage or Priest)
  • Strength 18
  • Character level 15
```

Use ✓ / ✗ against **focused party member** when inventory is open (same member as equip target).

### 9.2 — Equip button state

- **Enabled** only if `CanEquip` would pass for the target member.
- **Disabled tooltip** = evaluator `reason`.

### 9.3 — Shop

- Show requirements in buy-column inspect.
- Do **not** hide items — players may buy for a martial ally or for later.

---

## 10. Integration with proficiencies

| Mechanism | Role |
|-----------|------|
| **Martial calling + stat floors (this doc)** | **Hard** — cannot equip |
| **`Armour` proficiency** ([Proficiencies §8.2](../Progression/Proficiencies-Requirements.md)) | **Soft** — penalties / mitigation while wearing torso armor |
| **Future proficiency hard gate** (§12) | Optional AND on top of class gate for plate |

A Human **Knight** may equip plate at level 1 with **`Armour` 0** and suffer untrained penalties if enabled — but a Human **Mage** cannot equip at all.

---

## 11. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC-EQ1** | Unrestricted item (e.g. Iron Chestplate) equips on Human Mage, Human None, Dwarf, etc. |
| **AC-EQ2** | Item with `requiresMartialCalling` **fails** for Human Mage and Priest with class-specific message. |
| **AC-EQ3** | Same item **succeeds** for Human None and Knight regardless of level-1 baseline stats. |
| **AC-EQ4** | Non-Human folk equip martial-tagged gear in v0 (no accidental Mage analogue). |
| **AC-EQ5** | Stat floor blocks equip when effective stat below minimum; passes at or above. |
| **AC-EQ6** | Level floor blocks equip when `stats.level` too low. |
| **AC-EQ7** | **`Giants_Blade`** and **`Armor_HelmetOfLight`** authored with martial calling per **L10–L11**. |
| **AC-EQ8** | Class commitment to Mage/Priest strips illegal equipped martial gear to bag with log line. |
| **AC-EQ9** | Inventory inspect shows requirement block; disabled equip shows evaluator reason. |
| **AC-EQ10** | Unit tests cover evaluator: anatomy unchanged; martial; stat; level; message strings. |
| **AC-EQ11** | Future **plate armor** asset only needs `requiresMartialCalling` for baseline behavior (no code change). |

---

## 12. Future extensions (not v0)

| Idea | Notes |
|------|-------|
| **Proficiency hard gate** | e.g. plate also requires **`Armour ≥ 10`** ([Proficiencies §12](../Progression/Proficiencies-Requirements.md)); evaluated after martial calling |
| **Folk-specific martial rules** | e.g. Dragonian shapeshift form cannot wear plate |
| **Bypass sources** | Quest blessing or essence: “may wear one heavy piece despite class” — stable source key on stats |
| **Two-hand enforcement** | Oversized weapons require empty off-hand |
| **Cursed “sticky” illegal gear** | Class change blocked until removed — rejected for v0 |
| **Compare stats in shop** | Highlight which party member can equip |

---

## 13. Implementation touchpoints (for engineering)

| Area | Change |
|------|--------|
| **`ItemData`** | Add §6.1 fields |
| **`EquipmentLegalityEvaluator`** | Add §5 layers 4–6; human-readable reasons |
| **`HumanClassCommitment`** (or class commit service) | §8.4 strip illegal gear |
| **`InventoryDetailFormatter`** | §9.1 requirements block |
| **Inventory equip UI** | §9.2 disabled state + tooltip |
| **Editor** | Odin / custom drawer for `statMinimums`; preset buttons for “Heavy martial” |
| **Assets** | `Giants_Blade`, `Armor_HelmetOfLight`; future plate |
| **Tests** | `EquipmentLegalityEvaluatorTests` — martial, stat, level, commit strip |

---

## 14. Open questions

| # | Question | Recommendation |
|---|----------|----------------|
| **Q1** | Should **Human Priest** copy say “Priests” or “ divine servants”? | **“Priests”** — matches class enum display |
| **Q2** | Block **medium** armor (iron chestplate) for casters later? | **No v0** — only **heavy martial** tag; revisit if rogues/cloth casters feel wrong in medium |
| **Q3** | **Giant's Sword** for **Barbarian** with low STR — allowed? | **Yes v0** — martial calling only; STR floor only if authored on a variant |
| **Q4** | Show requirements on **character equipment menu (`C`)** read-only sheet? | **Yes** when that formatter gains a pass — same strings as inventory |
| **Q5** | **`minimumProficiency`** in same milestone or follow-up? | **Follow-up** — class + stat gates ship first |

---

## 15. Document history

| Version | Date | Notes |
|---------|------|-------|
| **0.1** | 2026-07-03 | Initial proposal — martial calling vs stat floors; plate / Giant's Sword / Helmet of Light examples |
