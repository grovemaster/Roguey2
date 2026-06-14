# Human Knight — Auras & skill tree (requirements)

**Purpose:** Specify **Human Knight** progression after class commitment: a **data-driven skill tree** centered on **Auras** and related **active / passive techniques**. Each skill has **title, description, and icon** (same presentation contract as essences, racial actives, and Mage spells). **Active** Knight skills gain **tree rank** from **combat proficiency pxp** (use in the field) **and/or skill points**, plus **per-skill mastery** from the same combat events — distinct from party kill XP and from generic combat proficiencies like `Fighting`.

**Status:** Partially implemented (core runtime, combat pxp, auras, hotbar — see §15; racial menu UI and training events still pending).

**Visual mock:** [`Docs/RacialSystem/human-knight-racial-abilities-menu-mock.png`](human-knight-racial-abilities-menu-mock.png) — see [Human Knight — racial menu](Human-Knight-Racial-Abilities-Menu-Requirements.md).

**Depends on:** [Human — Class powers](Human-Class-Powers-Requirements.md) (Knight commitment, D2 tree runtime, Soul Power + essences), [Proficiencies — system](../Progression/Proficiencies-Requirements.md) (use-based pxp, training cap, aptitudes), [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) (character level gates), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md), [Proficiencies menu](../UI/Proficiencies-Menu-Requirements.md) (`P`), [Human Knight — racial menu](Human-Knight-Racial-Abilities-Menu-Requirements.md) (`K` body), `HumanClassSkillTreeDefinition` / `HumanClassSkillTreeRuntime`, `AbilityAction` pipeline.

**Related:** [Human Mage — Spells & spellbooks](Human-Mage-Spells-And-Spellbooks-Requirements.md) (parallel class progression model), [Sudden Strength essence](../Essence/Sudden-Strength-Essence-Requirements.md) (Soul Power actives on Knight), [STBGB tone](Human-Class-Powers-Requirements.md) §2 (training fiction).

**Explicitly out of scope (v0):** Priest god-patron trees; **extended** Knight training event pipeline (skill point grants beyond class commit); full Knight racial menu UI; PvP; gamepad; respec of **unlocked** tree nodes; cross-class aura sharing; animated aura VFX bible.

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Knight content is **`HumanClass.Knight`** only — meaningless on `None`, Mage, or Priest. |
| **L2** | **Two-layer progression:** **(A) Tree rank** via **skill points** (training — manual spend) **and/or** **Knight skill proficiency pxp** from **active use in combat** (auto rank-up when pxp threshold met — §7.2); **(B) Skill mastery** via the **same combat events** (separate pxp sink — §7.3). |
| **L3** | **Auras** are a **tagged family** of Knight skills (`KnightSkillTag.Aura` + subtags), not a separate resource pool. |
| **L4** | Each skill node references **`AbilityAction`** (actives) and/or **stat payloads** (passives) with **displayName**, **description**, **icon** on the node or linked ability asset. |
| **L5** | **D2 tree ranks:** every Knight skill (passive **and** active) has **`maxRanks ≥ 1`**; each spent point increases **tree rank** and applies that rank’s authored property table (damage, radius, SP cost, stat mods — §8.1). |
| **L6** | **Proficiency pxp events:** toggle **activation**, successful **pulse**, successful **reactive** — **not** per-turn upkeep (§7.5). Each award feeds **rank progress** (actives, until `maxRanks`) **and** **mastery progress** (§7.3). |
| **L7** | **Mastery storage:** **`KnightSkillMasteryRuntime`** is **separate** from **`ProficiencyRuntime`** — distinct eligibility, save blob, and UI section (§7.1). |
| **L8** | **Rank from combat:** **Active** skills only — proficiency pxp from use can **automatically increase tree rank** (+1) when the per-rank pxp threshold is met; **passive** nodes still require **skill points** (no combat award). |
| **L9** | **No respec** of spent **tree points** in v0 (inherits parent doc). **Mastery never decreases** (inherits proficiency policy). Rank from proficiency pxp **never decreases** either. |
| **L10** | Knight **keeps essences + Soul Power** (parent doc §7.1). Aura actives may cost **`soulPowerCost`**, upkeep, or cooldown — per skill data. |
| **L11** | **Exclusive stance auras:** at most **one** `AuraStance` tagged skill **active** at a time per Knight (Paladin-style). |
| **L12** | v0 ships **one sample subtree** (3–5 nodes) proving multi-rank actives + proficiency rank-ups + mastery + exclusive stance — numbers tunable. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Knight fantasy** — front-line specialist whose power grows through **drill and field use**, not only level-ups. |
| **G2** | **Aura identity** — Knights are defined by **persistent or toggled battlefield presence** (buff allies, debuff foes, defensive shells) — not a clone of Mage spell lists. |
| **G3** | **Readable skills** — Every node shows **title, description, icon** in UI and hotbar tooltips. |
| **G4** | **Practice loop** — Using an **active** in combat earns **proficiency pxp** toward **next tree rank** **and** **mastery**; player sees both in [Proficiencies menu](../UI/Proficiencies-Menu-Requirements.md) and **`K`** Knight sheet. |
| **G5** | **Tree structure** — Prerequisites and branches express **build choices** (offense vs bulwark vs leadership) without hard class-multiclass. |
| **G6** | **Coexist with essences** — Tree + auras **complement** essence loadouts; neither replaces the other in v0. |
| **G7** | **Data-driven** — Designers author nodes in **`HumanClassSkillTreeDefinition`** (extend fields — §11); no per-skill `MonoBehaviour` subclasses. |
| **G8** | **STBGB tone** — Unlock fiction = **training with masters**; mastery fiction = **repetition under pressure**. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Knight skill** | One node in the Knight skill tree (`nodeId`, ranks, prerequisites). |
| **Aura** | Knight skill tagged **`Aura`** — typically a **toggle**, **stance**, or **radius buff** with optional Soul Power upkeep. |
| **Tree rank** | Integer **`0 … maxRanks`** on a node. Increased by **(1)** spending a **skill point** in town, or **(2)** accumulating **Knight skill proficiency pxp** from **active** use in combat (§7.2). |
| **Skill mastery** | Per-`nodeId` **practice level** (`0 … 27`, training cap) from the **same combat events** — scales efficacy **on top of** tree rank (§7.3). |
| **Knight skill proficiency pxp** | Per-skill progress from **active** use in combat. Feeds **rank pxp** (until rank cap) and **mastery pxp** in one dispatch (§7.4). Distinct from global **`ProficiencyKind`** pxp. |
| **Skill point** | Currency for **instant +1 rank** (manual spend in town). Also granted by **training events** (later). Does **not** replace proficiency pxp — both can raise rank on actives. |
| **Training event** | NPC / world interaction granting skill points or direct unlock (later). |
| **Exclusive stance** | Aura subtype: activating one **deactivates** other stance-tagged auras on the same actor. |
| **Resolve** | Successful combat outcome that counts for mastery pxp (hit, applied buff, blocked tick — §8.3). |

**Distinction from global proficiencies:**

| | **`ProficiencyKind` (Fighting, Sword, …)** | **Knight skill mastery** |
|--|---------------------------------------------|---------------------------|
| **Who** | Many folk / builds | **Human Knight** skills only |
| **Trained by** | Weapon hits, damage modules, etc. | **Using that Knight skill** |
| **UI** | Proficiencies menu (`P`) | Knight sheet + optional `P` section |
| **Design role** | Generic combat craft (DCSS) | **Class identity** techniques |

Both may apply on one action (e.g. sword strike under Valor aura → `Fighting` + `knight_aura_valor` mastery).

---

## 3. Reference — how other games handle similar systems

Use these as **pattern library**, not copy-paste. The recommended model for this project is **§4 Option C**.

### 3.1 — Diablo 2 — Paladin auras & skill tree

| Idea | Detail | Takeaway for Knight |
|------|--------|---------------------|
| **Single active aura** | Only one aura active at a time; switching is free/instant | **Exclusive stance** rule (L9) |
| **Skill points in tree** | +1 rank per point; synergies require other skills | **Tree rank layer** (already in `HumanClassSkillTreeDefinition`) |
| **Per-rank scaling** | Damage, radius, % bonuses step with rank on **every** skill | **All** Knight nodes use **`maxRanks`** + per-rank tables (L5) |
| **No per-cast grind** | Power comes from points spent, not casts | We **add** use mastery on top — STBGB / DCSS hybrid |

### 3.2 — Path of Exile — gem levels & quality

| Idea | Detail | Takeaway |
|------|--------|----------|
| **Gem XP from use** | Skills level when used in combat (within level band) | Closest analog to **skill mastery from use** |
| **Quality / alternate currency** | Second axis (quality currency) | Optional future: **training events** grant “insight” bonus multiplier |
| **Support links** | Gems modify other gems | Future: **modifier nodes** that alter a named aura id |

### 3.3 — Dungeon Crawl Stone Soup — skills by practice

| Idea | Detail | Takeaway |
|------|--------|----------|
| **Use → XP → level** | Independent skills, aptitudes, training cap | Reuse **`ProficiencyRules`** math for mastery (L6) |
| **Many parallel skills** | Fighting coexists with spell schools | Knight mastery **alongside** `Fighting` / weapon proficiencies |

### 3.4 — Final Fantasy XIV / WoW (modern) — unlock at level

| Idea | Detail | Takeaway |
|------|--------|----------|
| **Ability unlocked once** | No per-ability grind; potency from character level + gear | Good for **UI simplicity**; weak for STBGB “training” tone |
| **Talent rows** | Periodic choice between mutually exclusive options | Useful for **branch exclusivity** (§6.3) |

**Verdict:** Use for **unlock gates** (`requiredCharacterLevel`), not for mastery.

### 3.5 — Guild Wars 1 — attribute points + skill bar

| Idea | Detail | Takeaway |
|------|--------|----------|
| **Fixed skill bar size** | Limited equipped skills | Parallels **hotbar** — Knight equips subset of **unlocked** actives |
| **Attribute investment** | Stats boost whole categories | Could map to **passive tree ranks** (+Strength branches) |

### 3.6 — Bannerlord / Mount & Blade — practice skills

| Idea | Detail | Takeaway |
|------|--------|----------|
| **Practice in field** | Using bows raises bow skill organically | Validates **use-based mastery** fiction |
| **Perk trees** | Periodic binary perks | **Small exclusive branches** in Knight tree |

### 3.7 — Elden Ring — Ashes of War / weapon arts

| Idea | Detail | Takeaway |
|------|--------|----------|
| **One “art” per weapon** | Swappable special attack | Knight **does not** tie auras to gear — auras are **intrinsic** to class |
| **FP cost per use** | Resource on activation | Maps to **`soulPowerCost`** on `AbilityAction` |

### 3.8 — Surviving the Game as a Barbarian (project tone)

| Idea | Detail | Takeaway |
|------|--------|----------|
| **Commitment to role** | Class choice is permanent | Already locked in [Human class powers](Human-Class-Powers-Requirements.md) |
| **Training with NPCs** | Learn techniques from masters | **Training events** grant **tree points**, not mastery directly |
| **Growth through use** | Specialists improve by doing | **Mastery layer** from field use |

---

## 4. Skill tree models — options for this project

### Option A — Pure D2 (tree points only)

**Flow:** Training events → skill points → ranks in tree → all power from rank tables.

| Pros | Cons |
|------|------|
| Already implemented (`HumanClassSkillTreeRuntime`) | **No per-skill use loop** — conflicts with user request |
| Simple balance | Less “practice makes perfect” |

**Fit:** Baseline only; **insufficient alone**.

---

### Option B — Pure use-based (no tree points)

**Flow:** Commit Knight → all nodes visible by level → mastery **only** from use; rank 0 = locked until character level + optional quest flag.

| Pros | Cons |
|------|------|
| Strong practice fantasy | **Ignores existing D2 runtime** and training-event fiction |
| Like DCSS | Hard gating for **build identity** (everything grinds from zero) |

**Fit:** Possible for a spin-off mode; **not recommended** as v1 Knight model.

---

### Option C — Hybrid (recommended) ★

**Two layers:**

```
Training event / level gate
        │
        ▼
  Tree rank (skill points)  ──►  Rank 1..N: per-rank stat / aura property tables (D2 — passives + actives)
        │
        ▼
  Field use (successful resolves)
        │
        ▼
  Skill mastery (0–27)      ──►  Scales aura potency, duration, SP efficiency
```

| Layer | Source | Player-facing |
|-------|--------|---------------|
| **Tree rank (actives)** | Skill points **or** proficiency pxp from combat | “Trained at the drill yard **or** refined in battle” |
| **Tree rank (passives)** | Skill points only | “Learned at the drill yard” |
| **Mastery** | Same combat events as rank pxp | “Mastered through repeated use” |

| Pros | Cons |
|------|------|
| Matches **existing code** + **user request** | Two numbers per skill in UI |
| STBGB training + DCSS practice | Balance tuning for both axes |
| Synergizes with **`ProficiencyRules`** | |

**Recommendation:** Ship **Option C** (L2).

---

### Option D — Tree unlocks, essences modify auras (future)

**Flow:** Tree provides **aura slots** or **aura templates**; essences **imbue** or **enhance** specific auras (extra module, reduced SP cost).

| Pros | Cons |
|------|------|
| Deep buildcraft with existing essence system | High design / UI cost |
| Unique to this game | Defer past v0 |

**Fit:** §14 future hook — not v0.

---

## 5. Recommended Knight tree shape

### 5.1 — Top-level branches (authoring template)

Three **themes** — each a column or wedge in the tree UI (layout TBD):

| Branch | Theme | Example skills |
|--------|-------|----------------|
| **Bulwark** | Defense, `Shields`, damage reduction | *Steadfast Aura*, *Shield Brother*, passive +Armour synergy |
| **Valor** | Offense, party damage, pressure | *Valor Aura*, *Mark of Challenge*, reactive on crit |
| **Command** | Party utility, formation, recovery | *Rally Aura*, *Hold the Line*, small heal / morale |

**Depth:** 3–4 tiers per branch; **cross-branch prerequisites** discouraged in v0 (pure columns first).

### 5.2 — Node kinds (extend §T6.2 parent doc)

| Kind | Tag | Behavior |
|------|-----|----------|
| **Passive technique** | — | Rank ≥ 1 applies stat modifiers per rank table; **no** mastery pxp from passives alone |
| **Toggle aura** | `Aura`, `AuraToggle` | Rank ≥ 1 improves effect / SP cost per rank table; on/off; **exclusive** if also `AuraStance` |
| **Pulse aura** | `Aura`, `AuraPulse` | Rank ≥ 1 improves burst per rank table; cooldown ability; mastery on successful resolve |
| **Reactive** | `Aura`, `AuraReactive` | Rank ≥ 1 improves proc per rank table; mastery on successful proc resolve |

### 5.3 — Mutual exclusivity (optional v0.1)

| Group | Rule |
|-------|------|
| **`aura_stance`** | At most one **`AuraStance`** active |
| **`bulwark_capstone`** | Pick one of two capstones (D2-style branch end) |

Implement with **`siblingExclusivityGroup`** on nodes (same pattern as [Barbarian Spirit Imprint](Phase3-Requirements.md)) **or** runtime tag rule for stances only (L9 — start with stance tag rule).

### 5.4 — Prerequisites (D2)

Per node (existing fields on `HumanClassSkillTreeNodeData`):

- `requiredCharacterLevel`
- `requiredParentNodeId` + `requiredParentMinRank`
- Optional: **`requiredPointsInBranch`** (sum of ranks in listed node ids) — add field if needed

---

## 6. Sample content (v0 proof)

Minimum **`KnightSkillTree_Sample`** extension (replace / augment current Might/Finesse-only sample):

| nodeId | displayName | Kind | maxRanks | Parent | Notes |
|--------|-------------|------|----------|--------|-------|
| `knight_passive_might` | Iron Posture | Passive | 5 | — | +2 STR / rank (existing pattern) |
| `knight_passive_finesse` | Quick Feet | Passive | 5 | might ≥1 | +2 DEX / rank |
| `knight_aura_valor` | Valor Aura | Toggle `AuraStance` | **5** | might ≥1, lvl 3 | Party damage buff scales per rank; SP upkeep; mastery on **activation** |
| `knight_aura_bulwark` | Bulwark Aura | Toggle `AuraStance` | **5** | might ≥2, lvl 5 | Damage reduction per rank; exclusive with Valor |
| `knight_pulse_rally` | Rallying Cry | Pulse `AuraPulse` | **3** | finesse ≥1, lvl 4 | Cooldown shout; per-rank potency; mastery on **successful pulse** |

Each active links an **`AbilityAction`** with **icon**, **title**, **description** (node display fields mirror ability for UI fallback).

---

## 7. Knight skill proficiency & mastery (combat use)

### 7.1 — Storage

**`KnightSkillMasteryRuntime`** — **separate component and save blob** from **`ProficiencyRuntime`** (L7):

```text
perSkill: {
  skillId → {
    rank: int,              // tree rank (mirrors HumanClassSkillTreeRuntime; authoritative on runtime)
    rankPxp: int,           // progress toward next tree rank (actives only while rank < maxRanks)
    masteryLevel: int,      // 0..27
    masteryPxp: int         // progress toward next mastery level
  }
}
```

- **`skillId`** = stable `nodeId`.
- **Tree rank** on `HumanClassSkillTreeRuntime` remains authoritative for stat application; proficiency dispatch **increments rank** through a shared service that updates both runtimes.
- **Not** stored as `ProficiencyKind` entries — Knight sheet / **`P`** menu uses a dedicated **KNIGHT SKILLS** section (§12).

### 7.2 — Rank progression from proficiency pxp (actives)

| Rule | Detail |
|------|--------|
| **Eligible skills** | **Active** nodes only (`activeAbilities` non-empty). **Passives** do **not** gain rank from combat pxp. |
| **Eligible actor** | `Race.Human`, `HumanClass.Knight`, current rank ≥ 1 (skill unlocked) and rank &lt; `maxRanks` |
| **Award timing** | Same events as §7.5 (activation, pulse, reactive) |
| **Base pxp** | Default **12** per event; `proficiencyXpOverride` on ability optional |
| **Threshold** | `GetXpToNextRank(currentRank)` — may reuse `ProficiencyRules` curve or per-node authored table |
| **On threshold** | **Rank +1** if prerequisites still satisfied; excess pxp carries over; re-apply rank payloads |
| **Manual skill point** | **`TrySpendPoint`** still valid — instant +1 rank without requiring pxp (town menu) |
| **Both paths** | Spending a skill point **and** combat pxp may both raise rank on the same active — same `maxRanks` cap |

**Player-facing copy:** *Using this skill in combat earns **proficiency experience** toward the **next rank** on your skill tree.*

### 7.3 — Mastery progression (same combat events)

| Rule | Detail |
|------|--------|
| **Eligible** | Actives with rank ≥ 1 (same events as §7.5) |
| **Mastery cap** | `min(27, 2 × characterLevel)` ([Proficiencies §7.5](../Progression/Proficiencies-Requirements.md)) |
| **Base pxp** | Same dispatch as rank pxp (§7.4) — **one award**, two sinks |
| **Never decrease** | Same as proficiencies |

Mastery **does not** replace rank baselines; it **scales on top** (§8).

### 7.4 — Dispatcher

```text
KnightSkillProficiencyDispatcher.Dispatch(actor, resolvedKnightAction):
  skillId = action.knightSkillId
  if !IsActiveSkill(skillId) || rank < 1: return  // rank pxp
  basePxp = ResolveBasePxp(action)

  AddRankPxp(skillId, basePxp)      // may auto +1 rank when threshold met
  AddMasteryPxp(skillId, basePxp)   // parallel mastery track
```

Hook from **`PlayerCommandProcessor`** / aura tick service when Knight ability completes.

### 7.5 — Proficiency pxp events (locked)

| Event | Awards rank + mastery pxp? |
|-------|----------------------------|
| **Toggle activation** (press to turn **on**) | **Yes** — once per successful activation (L6) |
| **Toggle deactivation** (turn off) | **No** |
| **Pulse resolve** | **Yes** — when effect applies (§8.3) |
| **Reactive proc resolve** | **Yes** — when proc applies (§8.3) |
| **Per-turn aura upkeep** | **No** — prevents AFK farming |
| **Passive nodes** | **No** combat pxp — rank from **skill points** only |

**Re-toggle same stance:** each successful **on** press may award pxp (v0 allows every activation).

### 7.6 — Aptitude

| Source | Value |
|--------|-------|
| Default Human Knight | **+0** all skills |
| Future folk | Optional **`KnightSkillAptitudeTable`** (mirror `ProficiencyAptitudeService`) |
| Spirit / essence | Essences do **not** modify mastery aptitude in v0 |

---

## 8. Combat integration — what mastery changes

Mastery **does not** replace tree rank baselines; it **multiplies or adds on top**. **Tree rank** rises from **skill points** and/or **proficiency pxp from active use**; **mastery** rises from the **same combat events**.

### 8.1 — Suggested formulas (tunable)

Each node defines **per-rank curves** in data (D2). Resolver combines **current tree rank** + **mastery level**:

| Property | Tree rank | Mastery (field use) |
|----------|-----------|---------------------|
| **Aura effect %** | `baseBonus + rank * rankStep` (authored) | `+ masteryLevel * 2%` |
| **Radius (tiles)** | `baseRadius + rank / 2` (floor authored min) | `+ masteryLevel / 9` (cap +2) |
| **Duration (turns)** | Scales with rank table | `+ masteryLevel / 6` |
| **`soulPowerCost` / upkeep** | May **decrease** at higher ranks (authored) | `- masteryLevel / 5` (floor 1) |
| **Cooldown** | May decrease per rank (pulse nodes) | `- masteryLevel / 4` turns (floor 0) |
| **Passive stats** | e.g. +2 STR per rank (existing applicator) | Optional small bonus at high mastery (v0.1+) |

**Example — Valor Aura at tree rank 3, mastery 12:**

```text
partyDamageBonus = rankTable[3] * (1 + 0.12 * 0.02)   // rank baseline + mastery multiplier
soulPowerUpkeep    = max(1, rankTableUpkeep[3] - 2)   // mastery efficiency
```

### 8.2 — Link to global proficiencies

| Knight action | Also trains (existing dispatcher) |
|---------------|-----------------------------------|
| Melee attack under aura | `Fighting`, weapon, damage types |
| Block with shield reactive | `Shields` (when shields exist) |
| Aura pulse with fire module | `Damage_Fire` at 50% if spell-like module |

Knight mastery is **additional**, not a replacement.

### 8.3 — Successful resolve (mastery pxp)

| Action type | Counts when |
|-------------|-------------|
| **Toggle on** | Player activates aura and activation **succeeds** (SP paid, not silenced) — **once per press** (L6) |
| **Pulse** | Ability completes and **effect applied** (buff/debuff/damage &gt; 0 or status applied) |
| **Reactive** | Proc fires and **effect applied** |
| **Toggle off / stance swap** | **No** pxp (swap off is not training) |
| **Per-turn while aura active** | **No** pxp |
| **Whiff / fizzle** | **No** pxp (SP insufficient, silenced, target immune) |

---

## 9. Presentation — title, description, icon

### 9.1 — Authoring contract

Each **`HumanClassSkillTreeNodeData`** (extended) **must** expose:

| Field | Source priority |
|-------|-----------------|
| **Title** | `displayName` on node; fallback `AbilityAction` display name |
| **Description** | Node `description`; fallback ability inspect text |
| **Icon** | `AbilityAction.icon` for actives; procedural **Knight emblem** for passives without ability (v0) |

Same fields power:

- [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) Knight body (future)
- [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) tooltips
- [Proficiencies menu](../UI/Proficiencies-Menu-Requirements.md) optional **Knight skills** section

### 9.2 — Hotbar

| Rule | Detail |
|------|--------|
| **Assignable** | Unlocked actives (`tree rank ≥ 1`) with valid `AbilityAction` |
| **Source enum** | Extend `PlayerAbilitySource` with **`HumanKnightSkill`** (parallel Mage / Dragonian) |
| **Costs** | `HumanClassAbilityResources` — **`soulPowerCost`** on Knight actives |

---

## 10. Class commitment & training (Knight gate)

Inherits [Human class powers §5.3](Human-Class-Powers-Requirements.md):

| Gate | v0 |
|------|-----|
| **Become Knight** | **Drill Master** NPC in town — quest **`quest_knight_drill_apprenticeship`**: accept apprenticeship, pay **5 gold** on turn-in → `HumanClass.Knight` (mirrors Mage tutor). Run **`JRogue/Racial/Create Human Knight Drill Master Pack`** once in editor for NPC prefab + plaza marker. |
| **Skill points** | Preset `skillPointsTotal` on runtime; **later:** training events |
| **Mastery** | **Never** granted by NPC purchase — **use only** |

**Fiction copy (training event later):**

> The drill master teaches the **form** (tree point). Only the battlefield teaches ** mastery** (use).

---

## 11. Data model extensions (proposed)

### 11.1 — `HumanClassSkillTreeNodeData` additions

| Field | Purpose |
|-------|---------|
| **`tags`** | `List<KnightSkillTag>` — `Aura`, `AuraStance`, `AuraPulse`, … |
| **`masteryId`** | Optional override; default `nodeId` |
| **`activeAbilityIndex`** | Which `activeAbilities[]` entry is the hotbar/mastery target |
| **`iconOverride`** | Optional sprite if ability icon empty |
| **`proficiencyXpOverride`** | 0 = default 12 |
| **`perRankAuraProperties`** | Optional list parallel to rank index: effect %, radius, SP cost, cooldown — for **active** nodes (L5) |

### 11.2 — `AbilityAction` additions

| Field | Purpose |
|-------|---------|
| **`knightSkillId`** | Links resolve → mastery dispatcher |
| **`auraTags`** | Runtime: stance exclusivity, radius family |

### 11.3 — Runtime components

| Component | Role |
|-----------|------|
| **`HumanClassSkillTreeRuntime`** | Tree ranks, stat apply (existing) |
| **`KnightSkillMasteryRuntime`** | Per-skill level/pxp (new) |
| **`KnightAuraStateRuntime`** | Active stance id, upkeep timers (new) |
| **`KnightSkillProficiencyDispatcher`** | Award rank + mastery pxp; auto rank-up (new) |
| **`KnightSkillCombatResolver`** | Read rank + mastery for scaling (new) |

---

## 12. UI

| Surface | Behavior |
|---------|----------|
| **Racial menu (`K`) — Knight body** | Full spec: [Human Knight — racial menu](Human-Knight-Racial-Abilities-Menu-Requirements.md) — branch-grouped tree, rank + mastery rows, town point spend, detail pane |
| **Proficiencies (`P`)** | Section **KNIGHT SKILLS** when `HumanClass.Knight` — **rank pxp** + mastery level / pxp per unlocked active (read-only) |
| **Hotbar** | Icon + mastery level badge on hover (optional v0.1) |

---

## 13. Open questions

| # | Question | Notes |
|---|----------|-------|
| **Q1** | Can **two non-stance** auras stack? | Default **yes** unless tagged exclusive |
| **Q2** | Priest mirrors this doc later? | Default **yes** — shared tree machinery, `Divine Power` |

### 13.1 — Resolved (user, 2026-06-13 / 2026-06-05)

| Topic | Decision |
|-------|----------|
| **Multi-rank actives** | **Yes** — passives **and** actives use **`maxRanks ≥ 1`** with D2 per-rank property tables (L5). |
| **Rank from combat use** | **Active** skills earn **proficiency pxp** in combat; at threshold, **tree rank +1** automatically (L8). **Passives:** skill points only. |
| **Mastery pxp triggers** | **Activation** + **successful pulse/reactive** only — **not** per-turn upkeep (L6). Same events feed rank pxp. |
| **Mastery storage** | **`KnightSkillMasteryRuntime`** separate from **`ProficiencyRuntime`** (L7). |

---

## 14. Acceptance criteria (v0)

1. Committed **Human Knight** with tree rank ≥ 1 on `knight_aura_valor` can activate Valor from hotbar using **`soulPowerCost`**.
2. **Valor rank 3** produces a **stronger baseline** than rank 1 per authored rank table (multi-rank actives — L5).
3. Activating **Bulwark** while **Valor** stance active **deactivates Valor** (exclusive stance).
4. **Toggle-on** and successful **Rallying Cry** pulse each award proficiency pxp → **rank progress** and **mastery progress**; at rank threshold, **tree rank +1** without manual skill point spend.
5. **Passive** node (Iron Posture) gains rank **only** from skill points — combat use does **not** award rank pxp.
6. Mastery respects **training cap** and banks pxp at cap ([Proficiencies §7.5](../Progression/Proficiencies-Requirements.md)).
7. Skill displays **title, description, icon** in hotbar inspect.
8. **`HumanClass.None`** and **Mage** do not award Knight proficiency pxp; storage is **not** in **`ProficiencyRuntime`**.
9. Unit tests: exclusivity, dispatcher eligibility, auto rank-up from pxp, passive excluded, mastery cap, rank-3 vs rank-1 baseline.

## 15. Implementation order (suggested)

1. Extend **`HumanClassSkillTreeNodeData`** + sample Knight aura nodes + ability assets.
2. **`KnightSkillMasteryRuntime`** + save blob + **`KnightSkillProficiencyDispatcher`** (rank + mastery pxp, auto rank-up) hooked to command processor.
3. **`KnightAuraStateRuntime`** + stance exclusivity in aura tick / activation path.
4. **`KnightSkillCombatResolver`** — mastery scaling on aura abilities.
5. Tests + **`Proficiencies` menu** Knight section (read-only mastery list).
6. Knight **racial menu body** (separate doc) — tree + mastery display, town point spend.

---

## 16. Cross-links

- [Human — Class powers](Human-Class-Powers-Requirements.md)
- [Proficiencies — system](../Progression/Proficiencies-Requirements.md)
- [Proficiencies menu](../UI/Proficiencies-Menu-Requirements.md)
- [Human Mage — Spells & spellbooks](Human-Mage-Spells-And-Spellbooks-Requirements.md) (contrasting Mage model)
- [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md)
- [Human Knight — racial menu](Human-Knight-Racial-Abilities-Menu-Requirements.md)
