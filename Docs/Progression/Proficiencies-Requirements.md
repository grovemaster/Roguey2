# Proficiencies — Requirements

**Purpose:** Specify a **use-based proficiency system** for party members: discrete skills that **level up from practice** (not from party kill XP), grant **combat and utility bonuses** at higher levels, respect **folk aptitudes** (*Dungeon Crawl Stone Soup*–style), and enforce **character eligibility** (e.g. **Fire Magic** only for **Human Mages**). Proficiencies complement — but do not replace — [party character level](Party-Experience-And-Leveling-Requirements.md), racial subsystems (Spirit Imprint, Human class trees, Dragonian spells), and essence loadouts.

**Status:** Implemented (`ProficiencyRuntime`, `ProficiencyXpDispatcher`, `ProficiencyCombatResolver`; wired to melee, bow, Human Mage / Dragonian spell casts).

**Visual mock:** None (v0). Future: character sheet proficiency tab (see [Character equipment menu](../UI/Character-Equipment-Menu-Requirements.md) extension).

**Depends on:** `CharacterStats`, `Stat` / modifier pipeline, `WeaponType`, `DamageType`, `SkillType`, `Race`, `HumanClass`, `ItemData` (`weaponType`, `damageModules`, armor slots), combat pipelines (`PlayerController`, `BowRangedCombatService`, ability execution), [Bow & arrow](../Combat/Bow-And-Arrow-Requirements.md) (existing bow proficiency hook), [Party experience & leveling](Party-Experience-And-Leveling-Requirements.md), [Human — Class powers](../RacialSystem/Human-Class-Powers-Requirements.md), [Human Mage — Spells & spellbooks](../RacialSystem/Human-Mage-Spells-And-Spellbooks-Requirements.md), [Dragonian — Spell memory](../RacialSystem/Dragonian-Spell-Memory-Requirements.md), [Phase 0 glossary](../RacialSystem/Phase0-Glossary-And-Data-Contracts.md).

**Related:** [Subspace inventory & encumbrance](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md) (future armour penalty reduction), [Traps](../Combat/Traps-Requirements.md) (`SkillType.Perception` checks — distinct from proficiencies), [Throwing knife](../Inventory/Throwing-Knife-Requirements.md), [Status effects](../Combat/Status-Effects-Requirements.md).

**Explicitly out of scope (v0):** Proficiency **respec** or decay; cross-skill **training** (DCSS “crosstrain”); NPC trainers that sell levels; PvP; gamepad UI; full proficiency screen art; **unarmed multi-class** feat trees; renumbering `Race` enum values.

---

## Locked decisions (recommended v0)

| # | Decision |
|---|----------|
| **L1** | Proficiencies are **per party member**, persisted on the actor save blob — not party-wide. |
| **L2** | Absolute proficiency **max level = 27** (`ProficiencyRules.MaxLevel`). Stored level is **`0 … 27`**. Level **0** = untrained baseline, not “locked out” unless eligibility forbids training entirely. |
| **L3** | Level-ups come from **proficiency XP** earned by **qualifying actions**, not from [first-kill party XP](Party-Experience-And-Leveling-Requirements.md). |
| **L4** | Each folk has an **aptitude** per proficiency in **`[-4 … +4]`**. Aptitude modifies **XP required** to level (higher aptitude = faster). Same numeric scale as DCSS. |
| **L5** | Some proficiencies are **ineligible** for certain actors (`CanTrain == false`). Ineligible proficiencies stay at 0, grant no XP, and show as **N/A** in UI. |
| **L6** | **Weapon type** and **damage type** proficiencies are **separate axes**. A mace attack trains **`WeaponType.Mace`** and **`DamageType.Blunt`**. |
| **L7** | **`SkillType`** (Perception, Stealth, …) remains the pipeline for **environment / trap / dialog checks**. Proficiencies may **feed into** those checks where noted (§9), but are stored separately from today’s `CharacterStats.Skills` dictionary until a merge phase. |
| **L8** | Existing `CharacterStats.WeaponProficiencies` values migrate to **`ProficiencyKind` levels** (§14). Essences/items that added modifiers to weapon proficiencies use the **`Stat` modifier pipeline on derived combat stats**, not on raw proficiency level. |
| **L9** | v0 awards proficiency XP **only on successful, resolving actions** (hit, cast, block, detected trap disarm — not on miss whiff unless noted). |
| **L10** | Magic-school proficiencies apply only to actors who **can train** that school; Human **Fire Magic** requires `Race.Human` + `HumanClass.Mage`. |
| **L11** | A **single resolving action** may award XP to **many** proficiencies at once. Awards are driven by the **resolved strike payload** at hit/cast time (weapon type, each **active** damage module, spell tags, secondary **`Fighting`**) — not by a single “primary only” pick. |
| **L12** | **Temporary** damage (enchant, buff, brand) trains its **`Damage_*`** proficiency **only while that module is present and contributes** to the resolving hit. When the effect expires, later hits train only remaining modules (§7.4). |
| **L13** | **`Damage_*`** and **`FireMagic`** (arcane school) are **independent**. Physical or enchanted **Fire** damage trains **`Damage_Fire`** for **any eligible actor**; **`FireMagic`** trains only on **Human Mage** spell casts tagged with that school — never from a flaming sword alone. |
| **L14** | **Character-level training cap:** a proficiency’s level **cannot rise above** **`min(27, 2 × characterLevel)`** (§7.5). Higher character level unlocks room for deeper mastery. |
| **L15** | **No downward adjustment:** proficiency levels are **never reduced** because character level drops. A hypothetical de-level leaves proficiencies unchanged; training simply stays **blocked** until **`2 × characterLevel`** catches up again (§7.5.3). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Practice makes perfect** — using gear, magic, and tactics gradually improves related proficiencies. |
| **G2** | **Folk identity** — aptitudes make Dwarves better with axes, Elves with bows or air magic, etc., without hard class locks on mundane weapons. |
| **G3** | **Build expression** — a Knight who uses maces becomes a blunt specialist; a Mage who spams fireball deepens **Fire Magic** as **character level** unlocks higher mastery ceilings (§7.5). |
| **G4** | **Clear gates** — divine and arcane **school** proficiencies are impossible for the wrong class/folk; UI explains *why* (not a silent zero). |
| **G5** | **Combat integration** — higher proficiencies measurably improve damage, accuracy, mitigation, spell efficacy, or resource efficiency (§8). |
| **G6** | **Data-driven catalog** — designers add proficiencies and aptitudes in assets; code dispatches XP from action tags. |
| **G7** | **Backward compatibility** — [bow damage formula](../Combat/Bow-And-Arrow-Requirements.md) continues to work, generalized behind a shared **`ProficiencyCombatResolver`**. |

---

## 2. Reference — external games

### 2.1 — Dungeon Crawl Stone Soup (primary model)

| DCSS idea | This project |
|-----------|----------------|
| Many independent skills (Fighting, Long Blades, Fire Magic, Armour, …) | **`ProficiencyKind`** catalog (§5) |
| Skills 0–27; XP from use | **`0–27`**; **`proficiencyXp`** per kind (§7) |
| Racial aptitudes ±4 alter XP cost | **`ProficiencyAptitudeTable`** per `Race` (+ optional `HumanClass` override) (§6) |
| Weapon skill + Fighting improve melee | **`Fighting`** + **`WeaponType.*`** + **`DamageType.*`** stack in melee formula (§8.1) |
| Armour skill reduces penalty / improves AC | **`Armour`** reduces future encumbrance tier penalties (§8.2) |
| Spell schools improve power & reduce fail | School level → spell **`power`** and optional **`cast cost`** discount (§8.4) |
| Some skills untrainable / demigod exceptions | **Eligibility** matrix instead (§6.3) |

### 2.2 — Dungeons & Dragons (secondary inspiration)

| D&D idea | This project |
|----------|----------------|
| Proficiency bonus scales with **character level** | **Not adopted** for v0 — bonus power comes from **proficiency level**, not a flat `level / 4` bonus. **Character level** caps how high each proficiency may **train** (**`2 × characterLevel`**, max **27**) — §7.5. |
| Weapon/armor proficiency = can equip without penalty | v0: **anyone can equip** legal gear; low **`Armour`** / weapon proficiencies impose **penalties** until trained (§8.2, §8.5). Future: hard gate heavy armour. |
| Expertise (double proficiency on checks) | Future: racial passives or feats — not v0. |
| Skill checks (Perception, Stealth) | Keep **`SkillType`** + d20-style checks; proficiencies add **flat bonuses** where mapped (§9). |

### 2.3 — Surviving the Game as a Barbarian (tone)

| STBGB idea | Proficiency model |
|------------|-------------------|
| Specialists grow through repeated use in the field | XP on successful combat / cast / block events |
| Party shares kill XP, not personal technique | **Party level** shared; **proficiencies personal** |
| Class commitment is permanent | Eligibility locks arcane/divine schools to committed Human class |

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Proficiency** | One trainable skill entry (e.g. **Fighting**, **Blunt**, **Fire Magic**). |
| **`ProficiencyKind`** | Stable enum / string id for a proficiency (save-safe). |
| **Proficiency level** | Integer **`0 … 27`**. Used in formulas as **`skill`** (§8). |
| **Proficiency XP (`pxp`)** | Progress toward next level for one kind. Resets or rolls over per §7.3. |
| **Aptitude** | Folk modifier **`[-4 … +4]`** on XP-to-level curve for that proficiency. |
| **Eligible / trainable** | Actor may gain XP and level this proficiency. |
| **Ineligible** | Proficiency fixed at 0; UI shows **N/A**; no XP awards. |
| **Primary proficiency** | A proficiency receiving **full base pxp** for this award (weapon type, each qualifying damage module, each spell tag). |
| **Secondary proficiency** | Proficiency receiving a **fraction** of base pxp (default **`Fighting`** at **50%** on weapon hits). |
| **Action tag** | Combat / ability metadata listing extra proficiencies (schools, **`Evocations`**, …). |
| **Strike payload** | Snapshot at resolve time: **`weaponType`**, **`damageModules[]`** actually applied, buff/enchant modules, spell **`proficiencyTags`**. Drives §7.4. |
| **Training cap** | Max level a proficiency **may increase to** today: **`min(27, 2 × characterLevel)`** (§7.5). Distinct from **stored level**. |
| **Stored proficiency level** | Persisted level used for §8 bonuses. **Never lowered** when character level drops (§7.5.3). |
| **Damage module** | One `{ type: DamageType, value }` entry contributing to the hit/cast. Each **active** module can train its matching **`Damage_*`**. |

---

## 4. Architecture overview

```
Combat / ability / trap event
        │
        ▼
ProficiencyXpDispatcher  ──►  eligibility check (folk + class + subsystem)
        │                              │
        │                              ▼
        │                     ProficiencyRuntime (per actor)
        │                       level[kind], pxp[kind]
        ▼
ProficiencyCombatResolver ◄── read levels for damage / hit / spell power
```

**Storage (v0):** `ProficiencyRuntime` `MonoBehaviour` on actors (or embedded in save snapshot), keyed by `ProficiencyKind`. Do **not** store proficiency level inside `Stat` base values — items must not permanently overwrite practiced skill.

**Migration:** `CharacterStats.WeaponProficiencies[WeaponType.Bow].GetValue()` today acts as skill level in bow math. On first load after feature ships, copy non-zero weapon proficiency stats into **`ProficiencyRuntime`** then zero the legacy stat bases (§14).

---

## 5. Proficiency catalog (v0 proposed)

### 5.1 — General combat

| `ProficiencyKind` | Trained by | Notes |
|-------------------|------------|-------|
| **`Fighting`** | Any successful melee/ranged **weapon hit**; unarmed hit | General combat aptitude (replaces Athletics-as-Fighting in [bow doc](../Combat/Bow-And-Arrow-Requirements.md)). |
| **`Throwing`** | Successful thrown weapon / item attack ([throwing knife](../Inventory/Throwing-Knife-Requirements.md)) | |
| **`Armour`** | Taking damage while wearing **Torso/Legs/Head** armour; successful bump while armoured | Trains when armour mattered, not when naked. |
| **`Dodging`** | Successfully avoiding a trap (future evasion hook); taking zero damage from avoidable melee (future) | v0 minimum: award on **trap avoided** via movement skill check pass. |
| **`Shields`** | Successful block with off-hand shield (when shields exist) | Deferred until shield content ships. |

### 5.2 — Weapon types (`WeaponType` alignment)

Maps 1:1 to existing `WeaponType` enum (`StatTypes.cs`):

| Kind | Example gear |
|------|----------------|
| **`Weapon_Unarmed`** | Fists, claws |
| **`Weapon_Sword`** | Longsword, shortsword |
| **`Weapon_Axe`** | Hand axe, battleaxe |
| **`Weapon_Mace`** | Mace, warhammer, club |
| **`Weapon_Dagger`** | Dagger, stiletto |
| **`Weapon_Bow`** | Short bow, long bow |
| **`Weapon_Staff`** | Quarterstaff, mage staff |
| **`Weapon_Polearm`** | Spear, halberd |

**Rule:** equipped item’s **`ItemData.weaponType`** determines which **`Weapon_*`** proficiency trains on hit.

### 5.3 — Damage types (`DamageType` alignment)

| Kind | Trained by |
|------|------------|
| **`Damage_Blunt`** | Dealing **Blunt** damage (mace, warhammer, staff bash) |
| **`Damage_Slash`** | Dealing **Slash** |
| **`Damage_Pierce`** | Dealing **Pierce** (arrows, daggers) |
| **`Damage_Fire`** | Dealing **Fire** |
| **`Damage_Cold`** | Dealing **Cold** |
| **`Damage_Lightning`** | Dealing **Lightning** |
| **`Damage_Poison`** | Dealing **Poison** |
| **`Damage_Necrotic`** | Dealing **Necrotic** |
| **`Damage_Radiant`** | Dealing **Radiant** |
| **`Damage_Acid`** | Dealing **Acid** |
| **`Damage_Psychic`** | Dealing **Psychic** |
| **`Damage_Force`** | Dealing **Force** |

**Rule:** every **active** damage module on the strike payload may train its matching **`Damage_*`** (§7.4). Module index `[0]` is not special except for UI “primary type” labels.

### 5.4 — Arcane schools (Human Mage + future)

| Kind | Eligibility | Trained by |
|------|-------------|------------|
| **`Spellcasting`** | Human **Mage** | Any successful **Mage spell** cast |
| **`FireMagic`** | Human **Mage** | Fire-tagged Mage spells / Fire damage modules |
| **`IceMagic`** | Human **Mage** | Cold-tagged spells |
| **`AirMagic`** | Human **Mage** | Lightning / mobility air spells |
| **`EarthMagic`** | Human **Mage** | Earth / physical conjuration |
| **`Conjurations`** | Human **Mage** | Summoning / creation spells |
| **`Hexes`** | Human **Mage** | Debuff / curse spells |
| **`Translocations`** | Human **Mage** | Blink, teleport ([Teleport sample](../RacialSystem/Human-Class-Powers-Requirements.md)) |
| **`Alchemy`** | Human **Mage** | Transmute / poison crafting spells (when content exists) |

Each **`MageSpellDefinition`** gains optional **`proficiencyTags: List<ProficiencyKind>`** (default derived from ability damage type + designer overrides).

### 5.5 — Divine schools (Human Priest — future content)

| Kind | Eligibility |
|------|-------------|
| **`DivineMagic`** | Human **Priest** |
| **`Healing`** | Human **Priest** |
| **`Smite`** | Human **Priest** |
| **`Warding`** | Human **Priest** |

Priest skill assets reference **`proficiencyTags`** like Mage spells.

### 5.6 — Dragonian & other folk (v0 hooks)

| Kind | Eligibility | Trained by |
|------|-------------|------------|
| **`DraconicSpellcraft`** | `Race.Dragonian` | Successful **Dragonian spell** cast |
| **`Evocations`** | All folk (optional v0) | Successful **evocable item** use ([evocables](../Inventory/Evocable-Items-Requirements.md)) |
| **`Invocations`** | Folk with invocation subsystem (future) | Racial invocation abilities |

**Elf elemental contracts:** v0 does **not** duplicate Fire/Ice schools for Elves — spirit abilities train **`Evocations`** and/or **`Fighting`** unless a later doc adds **`SpiritWeaving`**.

**Barbarian Spirit Imprint:** imprint nodes may grant **starting level** or **aptitude bonus** to selected proficiencies (future cross-link); v0 Spirit Imprint does not auto-level proficiencies.

---

## 6. Folk aptitudes & eligibility

### 6.1 — Aptitude table (example v0 — tunable in data)

Aptitude **`A`** scales XP cost: **`costMultiplier = aptitudeTable[A]`**

| Aptitude **A** | XP multiplier | Feel |
|----------------|---------------|------|
| **+4** | **0.20×** | Natural gift |
| **+3** | **0.33×** | Strong affinity |
| **+2** | **0.50×** | Comfortable |
| **+1** | **0.67×** | Slight edge |
| **0** | **1.00×** | Neutral |
| **−1** | **1.50×** | Slow |
| **−2** | **2.00×** | Reluctant |
| **−3** | **3.00×** | Poor fit |
| **−4** | **5.00×** | Nearly untrainable |

**Example excerpt (illustrative — ship full table in `ProficiencyAptitudeCatalog` asset):**

| Proficiency | Human | Dwarf | Elf | Barbarian | Dragonian | Tiefling | Beastman |
|-------------|-------|-------|-----|-----------|-----------|----------|----------|
| **Fighting** | 0 | +1 | −1 | +2 | 0 | 0 | +1 |
| **Weapon_Axe** | 0 | **+2** | −1 | +1 | 0 | 0 | 0 |
| **Weapon_Mace** | 0 | **+2** | −2 | +1 | −1 | 0 | +1 |
| **Weapon_Bow** | 0 | −2 | **+2** | −1 | 0 | 0 | 0 |
| **Armour** | 0 | **+2** | −1 | 0 | +1 | +1 | 0 |
| **Spellcasting** | **Mage +2** | −4 | −2 | −4 | −4 | −2 | −4 |
| **FireMagic** | **Mage +1** | −4 | 0 | −4 | −4 | +1 | −4 |
| **DraconicSpellcraft** | −4 | −4 | −4 | −4 | **+3** | −4 | −4 |
| **Stealth** (see §9) | 0 | 0 | +1 | −1 | 0 | 0 | +1 |

**Human class overrides:** when `humanClass == Mage`, use Mage row for arcane schools regardless of apt column “Mage +2” shorthand in docs — implement as **`HumanClass.Mage` → aptitude +2** on **`Spellcasting`** and **`+1`** on elemental schools unless designer overrides.

### 6.2 — Eligibility rules (v0)

| Condition | Effect |
|-----------|--------|
| **`Race.Human` + `HumanClass.Mage`** | May train §5.4 arcane schools + **`Spellcasting`**. |
| **`Race.Human` + `HumanClass.Priest`** | May train §5.5 divine schools; **not** arcane. |
| **`Race.Human` + `HumanClass.Knight` / `None`** | **Not** arcane/divine schools; **may** train all §5.1–5.3 physical proficiencies. |
| **`Race.Dragonian`** | **`DraconicSpellcraft`** only for Dragonian spells; **not** Human Mage schools. |
| **Other folk** | Physical proficiencies per §5.1–5.3; arcane/divine schools **ineligible** unless a future racial doc adds exceptions. |
| **Undead / Fairy NPC folk** | Use physical + **`Evocations`** in v0; schools TBD in folk docs. |

**UI copy for ineligible:** *“Only a Human Mage can train Fire Magic.”*

### 6.3 — Starting levels (v0)

- All eligible proficiencies start at **level 0**, **0 pxp**.
- **Future:** class commitment grants **+1** starting level in signature proficiencies (Knight → **`Weapon_Sword`** or **`Fighting`**; Mage → **`Spellcasting`**).

---

## 7. Earning proficiency XP

### 7.1 — Award events (v0)

| Event | Primary XP | Secondary XP (optional) |
|-------|------------|-------------------------|
| Melee bump hit with weapon | **`Weapon_*`**, **`Damage_*`** (from module) | **`Fighting`** (50%) |
| Bow shot hit | **`Weapon_Bow`** | **`Fighting`** (50%), **`Damage_Pierce`** if pierce arrow |
| Unarmed hit | **`Weapon_Unarmed`**, **`Damage_Blunt`** (default) | **`Fighting`** |
| Mage spell cast success | **`Spellcasting`**, school tags (e.g. **`FireMagic`**) | **`Damage_*`** at **50%** per resolved module (§7.4.4) |
| Dragonian spell cast success | **`DraconicSpellcraft`** | **`Fighting`** if touch spell (optional) |
| Evocable item success | **`Evocations`** | — |
| Damage taken with armour equipped | **`Armour`** | — |
| Trap avoided (movement check) | **`Dodging`** | **`Perception`** skill check bonus source unchanged |

**Miss / fizzle:** no XP in v0 (keeps grinding honest).

**Friendly fire:** still trains **`Damage_*`** / **`Weapon_*`** if the attack resolved (design choice — matches DCSS).

### 7.2 — Base XP per action (v0 constants)

| Action tier | Base **`pxp`** |
|-------------|----------------|
| Standard melee / ranged hit | **12** |
| Heavy hit (damage ≥ 2× actor base) | **18** |
| Spell cast (MP/SP cost ≥ 5) | **15** |
| Cheap cantrip (cost 0–2) | **8** |
| Armour training tick (damage taken) | **6** (once per enemy turn per actor) |
| Trap dodge | **10** |

Designers may override per **`AbilityAction`** via **`proficiencyXpAward`** field.

### 7.3 — Level curve

Let **`L`** = current level toward next **`L+1`**. Base XP required:

```
baseXpToNext(L) = floor( (L + 1)^2 * 10 + (L + 1) * 4 )
```

Examples: **0→1:** 14 pxp · **5→6:** 374 · **26→27:** 7,842.

**Effective requirement:**

```
xpToNext = floor( baseXpToNext(L) * aptitudeMultiplier[aptitude] )
```

On **`pxp >= xpToNext`**: subtract **`xpToNext`**, **`L++`**, repeat if overflow (multi-level on huge award allowed).

**Max level:** at **`L == 27`**, no further level-ups (pxp discarded at absolute max unless prestige is added later). Per-kind progress is also gated by **`trainingCap`** (§7.5) before reaching 27.

### 7.4 — Multi-proficiency resolution (one action → many awards)

**Principle:** proficiency XP mirrors **what actually happened** in the resolved action. The dispatcher builds a **`ProficiencyTrainEvent`** once per qualifying success, then emits **zero or more `(ProficiencyKind, pxp)` pairs**.

#### 7.4.1 — Algorithm (v0)

```
ProficiencyTrainEvent BuildTrainEvent(resolvedAction):
  event = new ProficiencyTrainEvent()
  event.basePxp = ResolveBasePxpTier(resolvedAction)   // §7.2

  // 1) Weapon axis (at most one)
  if resolvedAction.weaponType != None:
    event.AddFull(WeaponKind(resolvedAction.weaponType))

  // 2) Damage axis (zero or more — one per active module)
  for module in resolvedAction.damageModulesApplied:
    if module.value > 0 && module.contributedToOutcome:
      event.AddFull(DamageKind(module.type))

  // 3) Spell / ability tags (zero or more)
  for tag in resolvedAction.proficiencyTags:
    event.AddFull(tag)

  // 4) Secondary Fighting on weapon hits
  if resolvedAction.countsAsWeaponHit:
    event.AddSecondary(Fighting, fraction = 0.5)

  return event

Dispatch(event, actor):
  for (kind, pxp) in event.Awards:
    if !Eligibility.CanTrain(actor, kind): continue
    ProficiencyRuntime.AddPxp(actor, kind, pxp)  // respects trainingCap §7.5
```

**`contributedToOutcome`:** module counted toward damage, duration, or effect resolution. A **0-value** decorative module does not train. A module **suppressed** by immunity still trains if it was part of the attack (designer toggle; v0 default **train** — you attempted that element).

#### 7.4.2 — Worked example: flaming longsword

| Setup | Strike payload at hit time | Proficiencies awarded (full base pxp each) | Secondary |
|-------|----------------------------|---------------------------------------------|-----------|
| Longsword, **Slash** only | `weaponType=Sword`, modules `[Slash 8]` | **`Weapon_Sword`**, **`Damage_Slash`** | **`Fighting`** 50% |
| Same sword + **temporary Fire enchant** (+3 Fire) | `weaponType=Sword`, modules `[Slash 8, Fire 3]` | **`Weapon_Sword`**, **`Damage_Slash`**, **`Damage_Fire`** | **`Fighting`** 50% |
| Enchant **expires** before next swing | `weaponType=Sword`, modules `[Slash 8]` | **`Weapon_Sword`**, **`Damage_Slash`** only | **`Fighting`** 50% |

**Human Knight** with flaming sword: trains **`Damage_Fire`** (elemental technique) but **`FireMagic`** stays **N/A** — school proficiencies require Mage spell tags (§6.2).

**Human Mage** melee with flaming sword: same physical **`Damage_*`** awards; **`FireMagic`** still only from **spell** tags, not weapon brands.

#### 7.4.3 — Module sources (what feeds `damageModulesApplied`)

| Source | Included when | Example |
|--------|---------------|---------|
| **`ItemData.damageModules`** | Always for weapon/ammo hits | Sword Slash, arrow Pierce |
| **Equipped off-hand / ammo** | Merged per combat resolver | Bow + arrow modules ([bow doc](../Combat/Bow-And-Arrow-Requirements.md)) |
| **Temporary weapon enchant** | While buff active on weapon instance | +Fire for 10 turns |
| **Status / aura on attacker** | While status active at resolve | “Flame tongue” essence proc |
| **Ability rider** | Ability adds rider modules on hit | Sudden Strength **does not** add damage module (no **`Damage_*`** from buff alone) |
| **Spell direct damage** | On cast success | Fireball → **`Fire`** module + spell tags |

**Expiration:** enchant and timed buffs attach modules to the **strike payload builder**, not permanently to the item’s static catalog. When duration hits 0, the builder stops appending that module — the next hit trains fewer proficiencies automatically.

#### 7.4.4 — Spells: schools + damage types together

Mage **Fireball** on successful cast:

| Kind | pxp | Notes |
|------|-----|-------|
| **`Spellcasting`** | full | Always on Mage spell cast |
| **`FireMagic`** | full | From spell `proficiencyTags` |
| **`Damage_Fire`** | **50%** | From resolved **`Fire`** damage module — **secondary** to avoid double-speed school + element grinding |

**Locked (Q1 resolved):** spells train **`Damage_*`** at **50% base pxp** when a matching damage module resolves; **`FireMagic`** (school) stays **full**. Physical-only hits train **`Damage_*`** at **full** with no school award.

#### 7.4.5 — Duplicate kinds & caps

- The same **`ProficiencyKind`** must not appear twice in one event (dedupe before dispatch).
- **Maximum awards per action (v0):** **12** distinct kinds (sanity cap); overflow drops lowest-pxp secondary awards first (log in debug).
- **Minimum:** zero awards allowed (e.g. environmental damage with no trained tag — no pxp).

#### 7.4.6 — UI / log (v0 minimum)

On multi-award, debug log one line:

`[Proficiency] {Name}: +12 Weapon_Sword, +12 Damage_Slash, +12 Damage_Fire, +6 Fighting`

Character sheet (future) may collapse to “**+3 proficiencies**” with expand detail.

### 7.5 — Character level training cap

**Rationale (designer intent):** growing stronger ([party character level](Party-Experience-And-Leveling-Requirements.md)) unlocks headroom for deeper technique. A level-3 fighter may reach **`Fighting 6`**; only after reaching character level **14** can any proficiency hit the absolute maximum **27**.

#### 7.5.1 — Formula

```
characterLevel = actor CharacterStats.level   // 1 … 50 per party XP doc
trainingCap    = min(ProficiencyRules.MaxLevel, 2 * characterLevel)
               = min(27, 2 * characterLevel)
```

| Character level | Training cap per proficiency |
|-----------------|------------------------------|
| **1** | **2** |
| **5** | **10** |
| **10** | **20** |
| **13** | **26** |
| **14+** | **27** (absolute max binds) |

**Two ceilings:** a proficiency stops gaining levels when it hits **`trainingCap`** *or* **27**, whichever is lower.

#### 7.5.2 — Applying the cap (level-ups only)

When **`ProficiencyRuntime.AddPxp`** would increment level:

```
trainingCap = min(27, 2 * characterLevel)

if currentLevel >= 27:
  discard pxp for this kind
else if currentLevel >= trainingCap:
  bank pxp only; no level-up until trainingCap rises
else if currentLevel + levelsGained would exceed trainingCap:
  set level = trainingCap; bank overflow pxp toward next level
else:
  apply normal level curve (§7.3)
```

**Benefits (§8):** always use **stored level** — if stored level **exceeds** current **`trainingCap`** (after a hypothetical de-level), bonuses remain at stored level (**L15**).

#### 7.5.3 — De-level policy (edge case)

De-leveling is **not** a planned mechanic. If character level ever drops:

| Rule | Behavior |
|------|----------|
| **Stored proficiency levels** | **Unchanged** — no clamp down, no XP wipe |
| **§8 combat bonuses** | Still computed from **stored level** |
| **Further training** | Blocked while **`storedLevel ≥ trainingCap`**; resumes when **`2 × characterLevel ≥ storedLevel + 1`** |
| **UI** | Show stored level; “**Mastery capped** — raise character level to train further” when at cap |

**Example:** **`Weapon_Sword 18`** at character level **10** (cap **20**). De-level to **5** (cap **10**): sword stays **18**, still grants **`Weapon_Sword 18`** bonuses, **cannot** reach **19** until character level **≥ 10** again.

#### 7.5.4 — UI & messaging

| State | Display |
|-------|---------|
| Below cap | `{Name} 7 · 42/374 pxp · cap 20` |
| At cap, character gated | `{Name} 20 · (cap) · train to 22 at character level 11` |
| At absolute max | `{Name} 27 · Master` |

Debug log when pxp award hits cap: `[Proficiency] Weapon_Sword at training cap (20/20); pxp banked.`

---

## 8. Benefits of higher proficiencies (recommended)

This section answers *“what does leveling get me?”* Benefits should be **noticeable by level 5–8**, strong by **15+**, without breaking v0 balance.

### 8.1 — Melee & ranged damage (physical)

**Pattern (extends [bow §8.2](../Combat/Bow-And-Arrow-Requirements.md)):**

```
weaponMod   = 1 + weaponLevel / 25
fightMod    = 1 + fightingLevel / 30
damageMod   = 1 + damageTypeLevel / 35   // optional v0.1; v0 may fold into weaponMod

physicalDamage = Round(baseDamage * weaponMod * fightMod * damageMod)
```

| Proficiency | Primary benefit | Secondary benefit (v0.1+) |
|-------------|-----------------|-----------------------------|
| **`Fighting`** | Global melee/ranged **`fightMod`** | +1 **`SkillType.Athletics`** effective bonus per 6 levels (display-only merge) |
| **`Weapon_*`** | Accuracy: **`hitBonus = weaponLevel / 3`** (future to-hit roll) | **`weaponMod`** damage |
| **`Damage_*`** | **`damageMod`** when primary type matches | Resistance penetration **`level / 50`** (future) |
| **`Throwing`** | Range +0.5 tile per 5 levels (cap +2); damage uses **`Throwing`** as weapon level |

**Minimum damage** remains **1** on hit.

### 8.2 — Armour & mitigation

| **`Armour` level** | Benefit |
|--------------------|---------|
| **0–5** | Remove “untrained armour” penalty: **−0 AC** (baseline) |
| **Every 3 levels** | **+1 effective AC** while wearing **Torso** armour (stacks with item AC when AC system ships) |
| **Level ≥ 10** | Reduce future **encumbrance tier** penalty by **1 step** ([encumbrance doc](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md) hook) |
| **Level ≥ 15** | **−5%** physical damage taken (multiplicative, cap one instance) |

**Untrained penalty (v0 optional):** wearing **Torso** armour with **`Armour < 3`** applies **`−5%`** evasion / movement until trained — soft gate before hard D&D-style “cannot equip plate.”

### 8.3 — Dodging & shields

| Proficiency | Benefit |
|-------------|---------|
| **`Dodging`** | **`+1`** effective **`SkillType.Acrobatics`** per 4 levels on trap dodge checks; future: marginal melee miss chance |
| **`Shields`** | **`blockChance = shieldLevel * 2%`** (cap 40%) when shield equipped |

### 8.4 — Spellcasting & magic schools

Applies to **`AbilityAction`** executions tagged with schools.

| Proficiency | Benefit |
|-------------|---------|
| **`Spellcasting`** | **`spellPower = 100% + spellcastingLevel * 3%`** on all eligible spells |
| **`FireMagic`** (etc.) | Additional **`+ schoolLevel * 4%`** power on tagged spells |
| **Combined** | Multiplicative with **`Spellcasting`**: `totalPower = base * spellPower * (1 + schoolLevel * 0.04)` |
| **High school (≥ 10)** | **−1 Magic Power** cast cost (floor 1) on tagged spells |
| **High spellcasting (≥ 14)** | **+1** tile range on ranged spells (cap +2) |

**Human Mage:** school bonuses apply to **`HumanMageSpellsRuntime`** casts only.

**Dragonian:** **`DraconicSpellcraft`** replaces **`Spellcasting`** + schools for Dragonian spells — same power formula.

**Priest (future):** **`DivineMagic`** + **`Healing` / `Smite`** mirror arcane pattern on **`currentDivinePower`** skills.

**Design note:** spell power affects **damage, duration, heal amount** via existing ability scaling hooks — abilities must read **`ProficiencyCombatResolver.GetSpellPowerMultiplier(actor, tags)`**.

### 8.5 — Evocations & items

| **`Evocations`** | **`+5%`** evocable effect per 3 levels (damage, radius, charges recovered) |

### 8.6 — Utility cross-links (`SkillType`)

Proficiencies **do not replace** skill checks; they add **flat bonuses** where thematically aligned:

| Proficiency | Adds to skill check |
|-------------|---------------------|
| **`Fighting`** | **`Athletics`** (+1 per 6 levels) |
| **`Stealth`** (future proficiency) | **`Stealth`** (+1 per 4 levels) |
| **`Armour`** | **`Athletics`** climb/swim while armoured (+1 per 8 levels) |
| **`Spellcasting`** | **`Arcana`**-style checks (future **`SkillType.Insight`**) (+1 per 5 levels) |

---

## 9. Relationship to existing `SkillType`

Today `CharacterStats.Skills` holds **`Stealth`**, **`Athletics`**, **`Perception`**, etc., used in [traps](../Combat/Traps-Requirements.md) and debug skill checks.

| System | Role |
|--------|------|
| **`SkillType`** | **Checks** vs DC (Perception vs trap, …). Improved by **level-ups**, items, essences — **not** by use-based pxp in v0. |
| **Proficiencies** | **Use-based** combat craft (DCSS skills). |

**Convergence (future):** allow **level-up skill points** to boost **`SkillType`** OR **grant pxp burst** to one proficiency — out of v0.

**Bow doc migration:** replace **`Skills[Athletics]`** in `BowRangedCombatService` with **`Fighting`** proficiency level (§8.1).

---

## 10. UI & feedback (v0 minimum)

| Surface | Behavior |
|---------|----------|
| **Character equipment menu** (future tab) | List eligible proficiencies: **name, level, pxp / next**, aptitude badge (**+2**, **−1**). |
| **Inspect / detail** | Weapon inspect shows **`Bow 7`**, **`Fighting 4`** contributing to formula breakdown. |
| **Level-up toast** | `"Fire Magic increased to 5!"` (log + optional floating text). |
| **Ineligible** | Grey **N/A** with reason string (§6.2). |
| **Racial menu** | No proficiency editing — view-only link **“See proficiencies (C)”** when character sheet ships. |

Hotkey **`C`** for character sheet is **future**; v0 may use debug overlay only.

---

## 11. Saves & multiplayer

- Serialize **`ProficiencyRuntime`** as `{ kindId, level, pxp }[]` on actor snapshot.
- On load, clamp unknown kinds; drop ineligible kinds with warning.
- Party members retain **individual** proficiency state across scene loads.

---

## 12. Future extensions (explicitly not v0)

| Idea | Notes |
|------|-------|
| **Character level cap on proficiency** | **`min(27, 2 × characterLevel)`** — implemented §7.5 |
| **Crosstrain** | Using long blades gives **25%** XP to **`Short Blades`** |
| **Skill decay** | DCSS removed decay; only add if roguelike mode wants it |
| **Trainer NPCs** | Spend gold for **`+500 pxp`** in one school |
| **Feats / Spirit Imprint nodes** | **`+aptitude`**, starting level, or **`Expertise`** |
| **Hard equip gates** | Cannot equip plate without **`Armour ≥ 10`** |
| **Party-wide proficiencies** | Rejected — personal only |

---

## 13. Content authoring

### 13.1 — `ProficiencyDefinition` (ScriptableObject)

| Field | Purpose |
|-------|---------|
| **`kind`** | `ProficiencyKind` |
| **`displayName`** | UI |
| **`description`** | UI / codex |
| **`category`** | Combat / Weapon / Damage / Arcane / Divine / Utility |
| **`defaultEligibleRaces`** | Optional filter before class rules |

### 13.2 — `ProficiencyAptitudeCatalog`

- Rows: **`(Race, HumanClass?, ProficiencyKind) → aptitude`**
- Loaded at boot; tests lock sample cell values.

### 13.3 — Ability / item tags

Extend **`AbilityAction`** (and **`MageSpellDefinition`**, **`DragonianSpellDefinition`**, **`ItemData`**) with:

```
List<ProficiencyKind> trainsOnSuccess;
int proficiencyXpOverride;  // 0 = use default tier table
```

**Example — Fireball (Human Mage):**

- **`proficiencyTags`:** `Spellcasting`, `FireMagic`
- **Resolved modules:** `Fire` damage → dispatcher adds **`Damage_Fire`** at **50%** via §7.4.4 (not duplicated in tags)

**Example — Mace bump:**

- Item: **`weaponType = Mace`**, damage module **`Blunt`**
- Dispatcher awards **`Weapon_Mace`**, **`Damage_Blunt`** (full each), **`Fighting`** (secondary 50%)

**Example — Flaming sword (Knight):**

- Static: **`Weapon_Sword`**, **`Damage_Slash`**
- Active enchant: **`Damage_Fire`** module appended until expiry
- **`FireMagic`:** not in event (ineligible + not tagged)

### 13.4 — Authoring checklist

Use this when shipping or reviewing content. **Rule of thumb:** if the combat resolver already knows **`weaponType`** and **`damageModules`** on the strike payload, you usually **do not** list proficiencies by hand.

#### Automatic — set gear / payload fields only

| Content | Author sets | System derives on success |
|---------|-------------|---------------------------|
| **Weapon** (`ItemData`) | **`weaponType`**, **`damageModules`** | **`Weapon_*`**, each **`Damage_*`**, **`Fighting`** (50%) |
| **Arrow / ammo** | **`damageModules`** (and bow legality) | **`Damage_*`** on shot; bow adds **`Weapon_Bow`** |
| **Melee bump attack** | *(nothing extra)* | From equipped MainHand (+ merged ammo rules for bow) |
| **Bow shot** | Bow + ammo as above | **`Weapon_Bow`**, ammo **`Damage_*`**, **`Fighting`** (50%) |
| **Unarmed bump** | *(nothing)* | **`Weapon_Unarmed`**, default **`Damage_Blunt`**, **`Fighting`** (50%) |
| **Temporary fire on sword** | Buff/enchant adds **Fire module to strike payload** while active | Extra **`Damage_Fire`** only while module present (§7.4.3) — **no** spell-school tags |
| **Mage spell element XP** | Spell’s damage module + tags below | **`Damage_*`** at **50%** from module — **do not** tag **`Damage_Fire`** on the spell asset |

Default **`pxp`** comes from §7.2 action tier unless **`proficiencyXpOverride`** is set.

#### Explicit tags required — add `proficiencyTags` / `trainsOnSuccess`

| Content | Tag on asset | Do **not** tag manually |
|---------|--------------|-------------------------|
| **Human Mage spell** (`MageSpellDefinition`) | **`Spellcasting`** + school(s) (`FireMagic`, …) | **`Damage_*`** (inferred from ability module) |
| **Dragonian spell** | **`DraconicSpellcraft`** (or auto from subsystem if code adds it) | Human arcane schools |
| **Human Priest skill** (future) | **`DivineMagic`**, **`Healing`**, **`Smite`**, … | — |
| **Evocable item / wand** | **`Evocations`** | Weapon/damage unless item also strikes as a weapon |
| **Non-damage ability** (buff, heal, utility) | Only proficiencies that match effect (designer) | **`Fighting`** unless it resolves as a weapon hit |
| **Trap dodge / armour tick** | *(none — event type drives award)* | — |

#### Optional override — `proficiencyXpOverride` on `AbilityAction`

Set only when this action should **not** use the default tier (§7.2):

| Use override when | Example |
|-------------------|---------|
| Channeled / expensive spell | **`15`** or **`18`** pxp base |
| Weak cantrip | **`8`** |
| Signature boss ability | Custom value for balance |

Override sets **`basePxp`** for **every** proficiency awarded by that action (each kind still gets full vs secondary fractions from §7.4).

#### Quick review before merge

1. **Weapon item:** `weaponType` + at least one **`damageModules`** entry with correct **`DamageType`**?
2. **Spell:** **`proficiencyTags`** include **`Spellcasting`** + schools, but **not** redundant **`Damage_*`**?
3. **Flaming enchant:** implemented as **payload module**, not a static item-only tag?
4. **School proficiencies:** only on assets the eligible class actually uses?
5. **Override:** only where default **12 / 15 / 8** tier is wrong?

---

## 14. Migration from current code

| Today | After proficiencies |
|-------|---------------------|
| `CharacterStats.WeaponProficiencies[WeaponType]` as **`Stat(0)`** | **`ProficiencyRuntime`** holds level; **`Stat`** reserved for **temporary** buffs only |
| Bow formula uses **`WeaponProficiencies[Bow]`** + **`Skills[Athletics]`** | **`Weapon_Bow`** + **`Fighting`** via **`ProficiencyCombatResolver`** |
| Melee **`GetTotalAttack(baseAttack)`** without skills | Add **`ProficiencyCombatResolver`** for equipped weapon |

**One-time migration:** if legacy stat **`GetValue() > 0`**, set proficiency level to that value (cap 27), then reset stat base to 0.

---

## 15. Acceptance criteria (v0)

| ID | Test |
|----|------|
| **A1** | Human Knight gains **`Weapon_Mace`** + **`Damage_Blunt`** XP on mace hit; **`FireMagic`** unchanged at 0 N/A. |
| **A2** | Human Mage gains **`FireMagic`** on fireball cast; Knight on same party does not. |
| **A3** | Dwarf **`Weapon_Axe`** aptitude **+2** levels faster than **`Weapon_Bow`** at **−2** for same pxp awards. |
| **A4** | Bow damage at **`Weapon_Bow 10`**, **`Fighting 10`** matches §8.1 formula (regression vs old bow doc numbers when migrated). |
| **A5** | **`pxp`** overflow levels multiple times in one award when threshold crossed. |
| **A6** | Ineligible school shows N/A in debug UI; dispatcher ignores awards. |
| **A7** | Save/load restores levels; ineligible kinds stripped on load for wrong class. |
| **A8** | Dragonian **`DraconicSpellcraft`** trains on Dragonian spell; not **`Spellcasting`**. |
| **A9** | Flaming sword hit while enchant active: **`Damage_Slash`** + **`Damage_Fire`** + **`Weapon_Sword`**; after enchant ends, **`Damage_Fire`** not awarded. |
| **A10** | Knight flaming sword does **not** award **`FireMagic`**; Mage Fireball awards **`FireMagic`** full + **`Damage_Fire`** at 50%. |
| **A11** | Character level **5**: **`Fighting`** cannot exceed **10**; at level **14**, can reach **27**. |
| **A12** | At training cap, pxp **banks**; level-up applies when character level raises **`trainingCap`**. |
| **A13** | Hypothetical de-level: stored proficiency **unchanged**; bonuses use stored level; training blocked until cap catches up. |

---

## 16. Design decisions — elaborated

### 16.1 — Q1: Spell **`Damage_*`** vs arcane school (resolved)

See **§7.4.4**. Summary:

| Path | Who | Trains |
|------|-----|--------|
| Flaming **sword** | Anyone eligible for physical proficiencies | **`Damage_Fire`** full (while enchant active) |
| **Fireball** spell | Human Mage | **`FireMagic`** + **`Spellcasting`** full; **`Damage_Fire`** **50%** |
| Fireball | Knight (scroll / future) | **`Damage_Fire`** only if a future evocation path exists; **`FireMagic`** **N/A** |

**Why 50% on spell element:** cast already awards two full school tags; triple full awards would outpace melee. Physical dual-type (slash + fire) stays **full both** because neither is a “school” — you earned both by wielding a hybrid weapon.

---

### 16.2 — Q2: Max proficiency level **27** (locked)

**Decision:** **`ProficiencyRules.MaxLevel = 27`** for v0 and until a deliberate rebalance changes it.

- Matches DCSS expectations and leaves room for a long mastery tail (§7.3 cumulative grind).
- Combined with §7.5, **character level 14+** is required to train any proficiency to **27**.
- If endgame feels slow, tune §7.3 curve constants before lowering max level.

---

### 16.3 — Q3: Character level vs proficiency training (locked)

**Decision:** **`trainingCap = min(27, 2 × characterLevel)`** per proficiency (**L14**).

| Principle | Rule |
|-----------|------|
| **Growth unlocks mastery** | Higher character level raises the ceiling on every proficiency |
| **No retroactive penalty** | De-level (if ever introduced) does **not** lower stored proficiency levels (**L15**) |
| **Banked progress** | pxp earned while at cap is kept; levels apply when cap rises (§7.5.2) |
| **Benefits follow stored level** | §8 uses stored level even when it exceeds current cap |

**Rejected alternatives:** no cap (Model A), **`characterLevel + K`**, tier gates only for 21–27, diminishing pxp, or “effective level” split — see git history / prior draft for comparison.

**Party XP note:** because [first-kill XP](Party-Experience-And-Leveling-Requirements.md) is sparse, players who grind combat without new species may hit proficiency caps **before** character level catches up — intentional; exploring new monsters unlocks character level, which unlocks mastery headroom.

---

### 16.4 — Remaining open questions

| # | Question | Recommendation |
|---|----------|----------------|
| **Q4** | Merge **`SkillType.Perception`** into a proficiency? | **No for v0** — keep traps on **`SkillType`**; add **`Stealth` proficiency** later that adds to checks (§9). |
| **Q5** | Award XP on **miss** at reduced rate? | **No for v0** — simpler; revisit for bruising weapons. |
| **Q6** | **`Fighting`** from **essence actives**? | **No** — essence actives train **`Evocations`** or racial subsystem only. |
| **Q7** | Untrained **armour** soft penalty (§8.2)? | **Optional v0** — enable if early plate feels wrong without training. |

---

## 17. Implementation order (suggested)

1. **`ProficiencyKind` enum + `ProficiencyRuntime` + save blob**
2. **`ProficiencyAptitudeCatalog` + eligibility service**
3. **`ProficiencyXpDispatcher` hooked to melee, bow, spell cast**
4. **`ProficiencyCombatResolver`** — bow first, then melee, then spell power
5. Debug UI list + migration from **`WeaponProficiencies`**
6. Character sheet tab + toasts

---

## 18. Cross-links to update when implemented

- [Bow & arrow](../Combat/Bow-And-Arrow-Requirements.md) §8 — reference **`ProficiencyCombatResolver`**
- [Party experience & leveling](Party-Experience-And-Leveling-Requirements.md) — character **`level`** drives proficiency **`trainingCap`** (§7.5); separate XP pools
- [Human Mage spells](../RacialSystem/Human-Mage-Spells-And-Spellbooks-Requirements.md) — add **`proficiencyTags`** on spells
- [Character equipment menu](../UI/Character-Equipment-Menu-Requirements.md) — proficiency tab
