# Human Priest — Divine covenant, piety & vows (requirements)

**Purpose:** Specify how **Human Priests** (`Race.Human`, `HumanClass.Priest`) differ from **Knights** and **Mages**. A Priest serves a **patron deity**, earns **piety** through **conduct** and optional **vows**, spends **Divine Power** on **invocations**, and suffers **divine retribution** when they break covenant. This doc is the **player-facing progression layer** on top of the mechanical baseline in [Human — Class powers §9](Human-Class-Powers-Requirements.md).

**Status:** **Draft — partially locked** (see §2, §6, §9, §10, §15). **Implementation scope:** **Slim v0** → **Full covenant v0** (both manually playable — §16); all later work deferred to §16.3.

**Explicitly out of scope (before Full covenant v0 ships):** Second patron god; Divine Mark invocation family; quest-linked Seals beyond vow rewards; piety decay; `Proficiencies (P)` Priest section; gamepad; cross-class invocation theft; stigmata cosmetics.

**Explicitly out of scope (always / post roadmap):** Multi-deity worship; respec patron; PvP theology; full pantheon art bible.

**Depends on:** [Human — Class powers](Human-Class-Powers-Requirements.md) (Priest commitment, essence/Soul Power prohibition, Divine Power pool), [Human Mage — Spells & spellbooks](Human-Mage-Spells-And-Spellbooks-Requirements.md) (contrast — library + equip; tutor quest pattern), [Human Mage — racial menu](Human-Mage-Racial-Abilities-Menu-Requirements.md) (devotion loadout UI pattern — Full covenant v0), [Human Knight — Auras & skill tree](Human-Knight-Auras-And-Skill-Tree-Requirements.md) (Drill Master pack pattern), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (`HotbarEntryKind.HumanPriestInvocation`), [Quest system](../World/Quest-Requirements.md), [NPC dialog](../World/NPC-Dialog-Requirements.md), [Safe zone](../World/Safe-Zone-Requirements.md), [Dungeon time](../World/Dungeon-Time-Requirements.md) (vow floor-night minimums), [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (`K` body — Full covenant v0), `AbilityAction` / hotbar pipeline.

**Related:** [Sudden Strength essence](../Essence/Sudden-Strength-Essence-Requirements.md) (Knight uses essences; Priest does **not**), [Phase 0 — Glossary](Phase0-Glossary-And-Data-Contracts.md), STBGB tone in [Human class powers §2](Human-Class-Powers-Requirements.md).

---

## Executive summary — how Priest differs

| | **Knight** | **Mage** | **Priest (proposed)** |
|--|------------|----------|------------------------|
| **Identity** | Trained warrior | Arcane scholar | Covenant servant |
| **Forbidden** | — | Essences, Soul Power | Essences, Soul Power |
| **Class resource** | Soul Power (tactical) | Magic Power (cast + equip budget) | **Divine Power** (tactical) + **Piety** (standing) |
| **Progression axis** | Skill tree ranks + field mastery | Known spell library | **Patron god** + piety bands + optional **vows** |
| **Loadout model** | Unlocked tree actives on hotbar | Prepare spells from grimoire | **Invoke** unlocked **devotions** (see §6) |
| **Town loop** | Drill / training events | Tutor quest + spellbooks | **Shrine** — choose patron, vows, devotion loadout |
| **Dungeon loop** | Practice skills in combat | Cast prepared spells | **Adhere to conduct** → maintain piety → invoke powers |
| **Failure mode** | Out of Soul Power | Out of Magic Power | Out of Divine Power **or** penance / retribution |

**Recommendation:** Priest should **not** be a third copy of the Knight D2 skill-point tree. The existing sample `PriestSkillTree_Sample` (+STR / +DEX passives) remains valid as **engine proof** only; shipping Priest fantasy should pivot to **piety-gated invocations** (DCSS god model) with vows as the STBGB “hard mode training” layer.

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Priest **cannot consume essences** (equip ≡ consume, same as Mage). May carry unconsumed essence items in inventory. |
| **L2** | Priest **cannot use Soul Power** (`MaxSoulPower == 0`). **Divine Power** is the tactical pool. |
| **L3** | Class commitment is **one patron god**, **permanent** (like class itself). |
| **L4** | **Piety** gates **which** invocations exist; **Divine Power** pays **most** invocation costs. |
| **L5** | **Piety scale** is **0 … 100** for v0 (authored `maxPiety = 100`; formula and band thresholds remain **tunable** without redesigning the covenant model). |
| **L6** | **Conduct** is mostly **automatic** (kill tags, exploration, ally protection). |
| **L7** | **Vows** are **opt-in per dungeon run**, taken at shrine before descent. |
| **L8** | Breaking a vow affects **only that vow** — other active vows continue. Retribution lasts for the **remainder of the run** (until town return). |
| **L9** | **Party vows** exist: party members must obey the rule, but **only the Priest** earns piety / Seals on success or suffers retribution on failure. |
| **L10** | Progression tokens: **Divine Mark** (combat debuff on enemies) and **Covenant Seal** (persistent Priest unlock). Do **not** use “Divine Brand” as an engine term. |
| **L11** | **Devotion loadout** slots scale with piety (see §6). **Absolute cap** for v0 content authoring: **8** equipped devotions max at peak piety. New priests start with **2** slots. |
| **L12** | Equipped devotions go on the **ability hotbar**. **Slim v0:** loadout via shrine stub; **Full covenant v0:** primary editor is **`K` menu** (mirror Mage prepared-spell flow). |
| **L13** | Priest should **not** ship as a Knight-style skill-point grind. Piety + invocations are the primary progression (see §4.4 for passive handling). |
| **L14** | **Two implementation milestones**, both **manually playable** from town test scene: **Slim v0** then **Full covenant v0** (§16). No further Priest phases until both are done. |

---

## Locked recommendations (supporting rationale)

| # | Recommendation | Rationale |
|---|----------------|-----------|
| **R1** | Commit gate requires **no consumed essences** (Mage parity). | STBGB “chosen path” beat. |
| **R2** | Knight remains the **only** Human class stacking essences + class techniques. | Class clarity. |
| **R3** | Equipping devotions does **not** consume piety (equip ≠ invoke). | Mage equip-budget parallel. |
| **R4** | Avoid DCSS hard excommunication at piety 0 in v0. | Party RPG fairness — use penance first (§8). |

---

## 1. Design question — essence prohibition for Priest?

**Yes — recommended.**

| Argument | Detail |
|----------|--------|
| **Class clarity** | Three Humans, three contracts: Knight borrows essence power; Mage internalizes arcane power; Priest channels divine power. |
| **STBGB alignment** | Mage commitment already requires relinquishing essence consumption; Priest is the same “I chose a higher path” beat. |
| **Balance knob** | Essences are strong generic power; Priest gets piety + invocations instead. |
| **Knight contrast** | Knight remains the **only** Human class that stacks essences + class techniques — intentional. |

**Optional nuance (later):** A specific **chaos god** patron might *require* carrying a cursed essence as a **taboo item** without *consuming* it — vow fiction, not essence slots.

**Commit gate (mirror Mage):** Shrine commitment requires **no consumed essences** equipped; unconsumed inventory OK.

---

## 2. Reference — pattern library

Use as **inspiration**, not copy-paste. Recommended synthesis is **§4 Option C**.

### 2.1 — Surviving the Game as a Barbarian (project tone)

| Idea | Priest mapping |
|------|----------------|
| Permanent class commitment | One patron, one class — no respec |
| Training with masters | Shrine NPC + vow trials |
| Growth through disciplined action | Conduct + vow fulfillment, not passive XP |
| Risk when breaking rules | Divine retribution, not arbitrary GM fiat |

### 2.2 — Dungeon Crawl Stone Soup — gods & piety

| Idea | Detail | Takeaway |
|------|--------|----------|
| **Single god** | One patron; abandon → wrath | Permanent patron choice (R4) |
| **Piety 1–200** | Standing meter; stars at thresholds | **Piety bands** unlock invocation tiers |
| **Conduct** | Kills, exploration, donations, abstentions | **Automatic piety events** (§7) |
| **Penance** | Negative piety debt after violation | **Retribution state** blocks gains until cleared |
| **Active abilities** | `a` invocations; some cost piety | **Devotions** on hotbar; dual costs possible |
| **Passive gifts** | High piety passives (regen, resist) | Patron **passive boons** at band thresholds |
| **Piety decay** | Some gods decay over time | Optional per-god **upkeep conduct** (explore, pray at altar) |

**Do not import wholesale:** DCSS excommunication at piety 0 is harsh for a party RPG — prefer **penance + ability lock** before hard excommunication (§8).

### 2.3 — D&D 5e / Pathfinder — clerics, domains, oaths

| Idea | Detail | Takeaway |
|------|--------|----------|
| **Domain / deity** | Spell lists + channel divinity | Patron defines **invocation palette** + conducts |
| **Prepared spells** | Daily prep from full list | **Different from our Priest** — we already have Mage for prep |
| **Channel Divinity** | Short-rest limited burst | Model as **invocations** with Divine Power + cooldown |
| **Oath / anathema** | Paladin tenets; break → lose features | **Conduct + vow** system |
| **Branding Smite** | Mark enemy; radiant rider | **Divine Mark** combat debuff (§10.1) |
| **Pathfinder anathema** | Explicit list of forbidden acts | Author per-god **`DivineConductDefinition`** rows |

### 2.4 — Diablo 2 — Paladin auras

| Idea | Takeaway |
|------|----------|
| Exclusive auras | Optional: **one channeled devotion** at a time for aura-like passives |
| Skill points | **Not** the primary Priest progression — piety replaces points |

### 2.5 — Guild Wars — attribute + skill bar

| Idea | Takeaway |
|------|----------|
| Limited equipped skills | **Devotion loadout** cap (e.g. 4–6 slots) separate from full unlocked library |

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Patron god** | `PatronGodDefinition` — deity id, domains, conduct rules, invocation list, vow catalog. |
| **Piety** | Integer **0 … 100** per Priest character (`maxPiety` tunable later). Not party-shared. |
| **Piety band** | Threshold range unlocking invocation tiers / passives / devotion slot count (e.g. 0–19, 20–39, …). |
| **Conduct** | Rule that **adds or removes piety** when world events match (kill undead, explore tile, ally injured). |
| **Taboo** | Standing prohibition from patron (always on) — e.g. “never use poison,” “never butcher corpses.” Violation → piety loss + penance. |
| **Vow** | **Opt-in**, **run-scoped** promise taken at shrine before dungeon descent. Success → piety bonus + possible **Seal** unlock. Failure → retribution. |
| **Divine Power** | Tactical pool (`maxDivinePower`, `currentDivinePower`) — spent on most invocations; regen per rest / turn rules TBD. |
| **Invocation / devotion** | Priest active or channeled ability from patron list (`PriestInvocationDefinition`). |
| **Devotion loadout** | Subset of **unlocked** invocations **equipped** for the run (town-editable in safe zone). Slot count scales with piety (§6.2). |
| **Devotion slot** | One position in the devotion loadout. Equipped devotions are eligible for **ability hotbar** assignment. |
| **Penance** | Debt state after serious violation; incoming piety pays debt first; abilities may be suppressed. |
| **Divine retribution** | Misfortunes while in penance or after broken vow — damage, status, blocked invocations, hostile spawns (authored per god). |
| **Divine Mark** | **Enemy-facing** debuff applied by invocation (track undead, smite bonus). |
| **Covenant Seal** | **Priest-facing** permanent record of a **fulfilled vow** — unlocks nodes or passives (§10.2). |
| **Essence vow** | Vow framed around essence / item / behavior taboo (extends vow system to item categories). |

---

## 4. Priest progression models — options

### Option A — D2 skill tree only (current code baseline)

**Flow:** Skill points → ranks in `PriestSkillTree` → actives cost Divine Power.

| Pros | Cons |
|------|------|
| Already implemented | **Too similar to Knight** — fails design goal |
| Simple | No god fantasy |

**Fit:** **Retire as shipping model**; keep runtime for shared Human class infrastructure.

---

### Option B — Pure DCSS piety (no skill tree)

**Flow:** Pick god → earn piety from conduct → invocations unlock by band → some cost piety directly.

| Pros | Cons |
|------|------|
| Strong identity | Ignores existing `HumanClassSkillTreeRuntime` |
| Conduct-driven | Harder to gate “character level 10” nodes without a second axis |

**Fit:** Good **core loop**; needs a **level gate** on invocations for party leveling doc.

---

### Option C — Hybrid covenant model (recommended) ★

**Three layers:**

```
Shrine: choose Patron (once) + optional Vows (per run)
        │
        ▼
Conduct loop (dungeon) ──► Piety ──► Unlock invocation tiers + passives
        │
        ▼
Town: assign Devotion loadout (safe zone)
        │
        ▼
Combat: Invoke (Divine Power cost; some also cost Piety)
        │
        ▼
Vow success ──► Covenant Seal (persistent) ──► Gates high-tier invocations
```

| Layer | Source | Player-facing |
|-------|--------|---------------|
| **Patron** | One-time commitment | “I serve The Shining Lance” |
| **Piety** | Conduct + vow bonuses | Stars / meter on `K` menu |
| **Invocations** | Piety bands + level + optional Seal | Holy fire, heal, turn undead |
| **Vows** | Opt-in per run | “No bladed weapons this delve” |
| **Seals** | Vow fulfilled under floor/time rules | Permanent unlock badge |

**Fit:** Matches user request + STBGB + DCSS + distinct from Mage/Knight.

### 4.4 — Passive bonuses: D2 skill tree vs piety bands (decision guide)

The codebase already has `HumanClassSkillTreeRuntime` and a sample `PriestSkillTree_Sample` (+STR / +DEX passives). The question is whether Priest **shipping progression** should use that tree, piety bands, or both.

#### Model A — D2 passive tree (Knight-like)

**Flow:** Skill points (level / quests) → spend ranks on `PriestSkillTree` passives → +stats / small rules per rank.

| Pros | Cons |
|------|------|
| Reuses existing runtime | **Third Human with the same UI metaphor as Knight** — weak class identity |
| Familiar to players who know Knight | Two progression currencies (piety **and** skill points) — confusing |
| Easy to author +2 STR nodes | Passives ignore **conduct** — “I served my god” fiction breaks |

**When to use:** Internal engine QA only; **not** recommended as the player-facing Priest loop.

#### Model B — Piety band passives only (recommended) ★

**Flow:** Conduct + vows raise piety → crossing band thresholds **auto-grants** patron passives (no skill points).

| Pros | Cons |
|------|------|
| **One progression axis** — piety is the Priest’s “XP with morality” | Requires new `PietyBandDefinition` tables (not hard) |
| Passives **feel earned** by behavior | Less granular than 20-rank D2 nodes |
| Distinct from Knight (practice) and Mage (library) | Must author band tables per god |
| Scales naturally with devotion slot unlocks (same meter) | |

**Example band table (global v0, tunable):**

| Piety | Band | Passive boon (example) | Devotion slots |
|-------|------|------------------------|----------------|
| 0–19 | ★☆☆☆☆ | — | **2** |
| 20–39 | ★★☆☆☆ | +1 Wisdom | **3** |
| 40–59 | ★★★☆☆ | +5 max HP | **4** |
| 60–79 | ★★★★☆ | +1 Divine Power regen on rest | **5** |
| 80–89 | ★★★★★ | Resistance row (patron-themed) | **6** |
| 90–99 | ★★★★★+ | Patron minor gift | **7** |
| 100 | ★★★★★++ | Patron capstone passive | **8** |

Patron-specific passives **replace or stack** on the generic row per `PatronGodDefinition`.

#### Model C — Hybrid (minimal tree + piety)

**Flow:** 2–3 **fixed** passive nodes unlocked by **character level** only; everything else from piety.

| Pros | Cons |
|------|------|
| Reuses tree asset for a tiny “ordination basics” branch | Still two systems to explain |
| Level gates feel like “ordination ranks” | Risk of scope creep back toward Knight |

**When to use:** Only if playtests show players need **early-game stats** before piety matters.

#### Recommendation

| Layer | Source | Ship? |
|-------|--------|-------|
| **Actives (invocations)** | Piety band + level + Covenant Seal | **Yes** |
| **Passives** | Piety band tables per patron | **Yes** (Model B) |
| **D2 skill tree** | `PriestSkillTree_Sample` | **Engine legacy / QA preset only** — do not expose skill-point spend in Priest UI |
| **Channeled auras** | Invocation subtype (optional) | **Later** — one active channeled devotion at a time |

**Migration note:** `HumanClassSkillTreeRuntime` can remain on the Priest prefab for shared Human infrastructure tests; `HumanPriestCovenantRuntime` becomes the **authoritative** progression owner for shipping content.

---

## 5. Power economy — Divine Power vs Piety

### 5.1 — Roles

| Resource | Role | Analog |
|----------|------|--------|
| **Piety** | Long-term **favor**; unlocks abilities; may be spent by rare invocations | DCSS piety stars |
| **Divine Power** | Short-term **channeling stamina** per rest/turn | Mage `currentMagicPower` |

**Invariant:** Equipping devotions in loadout does **not** consume piety (parallel Mage: equip ≠ cast).

### 5.2 — Proposed formulas (tunable)

| Field | v0 proposal |
|-------|-------------|
| `maxDivinePower` | `Wisdom × 5 + levelDivinePowerBonus` (mirror Mage Int formula) |
| `currentDivinePower` | Spent on invoke; refilled on rest / town return ([dungeon time doc](../World/Dungeon-Time-Requirements.md)) |
| `maxPiety` | **100** (tunable constant — bands and slot table rescale if changed) |
| Starting piety on commit | **10** (≈10% of scale; tunable) |

### 5.2.1 — Piety bands (v0 draft, scale 100)

Five-star display maps to **seven** mechanical steps (capstone at 100):

| Stars (UI) | Piety range | Typical unlocks |
|------------|-------------|-----------------|
| ★☆☆☆☆ | 0–19 | Starter invocations; **2** devotion slots |
| ★★☆☆☆ | 20–39 | Tier-2 invocations; patron passive I; **3** slots |
| ★★★☆☆ | 40–59 | Tier-3 invocations; **4** slots |
| ★★★★☆ | 60–79 | Tier-4 invocations; patron passive II; **5** slots |
| ★★★★★ | 80–89 | Tier-5 invocations; **6** slots |
| ★★★★★+ | 90–99 | Rare invocations; **7** slots |
| ★★★★★++ | 100 | Capstone invocation or passive; **8** slots |

Exact thresholds live in `PriestPietyProgressionDefinition` (global) with per-god overrides optional.

### 5.3 — Invocation cost types

| Cost type | Example |
|-----------|---------|
| **Divine Power only** | Lay on Hands — 8 DP |
| **Divine Power + cooldown** | Sanctuary — 12 DP, 3-turn CD |
| **Piety + Divine Power** | Major miracle — 20 DP + 5 piety |
| **Piety only (rare)** | Emergency intervention — 15 piety, 0 DP |

Insufficient resource → same UX as Soul Power / Magic Power failures.

---

## 6. Invocations, devotion loadout & hotbar

### 6.1 — Invocations vs Mage spells vs Knight skills

| | **Mage spell** | **Knight skill** | **Priest invocation** |
|--|----------------|------------------|------------------------|
| **Unlock** | Learn from spellbook | Tree rank + training | **Piety band** + level (+ Seal) |
| **Loadout** | Equip to Magic Power budget | Hotbar from unlocked tree | **Devotion loadout** (slot count scales with piety) |
| **Improve** | — (v0) | Mastery from use | **Piety band** upgrades rank table |
| **Spend** | Magic Power on cast | Soul Power on active | **Divine Power** on invoke |
| **Flavor** | Arcane craft | Martial training | **Divine favor** |

**No spellbooks for Priest** — new invocations come from **piety thresholds**, **quests**, and **Covenant Seal** milestones.

### 6.2 — Devotion loadout slots (locked)

| Rule | Detail |
|------|--------|
| **Starting slots** | **2** on class commit (piety band ★☆☆☆☆) |
| **Maximum slots (v0)** | **8** at peak piety (band ★★★★★++ / piety 100) |
| **Growth driver** | **Piety band** (primary). Character level may gate *which* invocations fit in a slot, but **slot count** is piety-driven unless playtests say otherwise. |
| **Scaling** | Authored in `PriestPietyProgressionDefinition.devotionSlotsByBand[]` — must support future retuning when `maxPiety` changes. |
| **Overfill on piety loss** | If piety drops below a band, **slot cap shrinks** but existing equipped list is **not** auto-trimmed until player opens town loadout editor — then they must unequip down to legal count (mirror Mage over-budget edge cases). |

**Player fantasy:** A novice priest prepares **two** sacred rites before descending; a saint prepares **eight**.

### 6.3 — Hotbar pipeline (locked)

Yes — **equipped devotions go on the ability hotbar**, same as Mage equipped spells and Knight tree actives.

```
Unlocked library (all invocations piety allows)
        │
        ▼
Devotion loadout (≤ current slot cap)  ◄── edited in Priest K menu (town only)
        │
        ▼
Ability hotbar (HotbarEntryKind.HumanPriestInvocation)
        │
        ▼
PlayerCommandProcessor invoke → spends Divine Power (+ piety if authored)
```

| Check | Rule |
|-------|------|
| Hotbar assign pool | Only invocations in **current devotion loadout** |
| Invoke in dungeon | Hotbar only — not from `K` menu |
| Edit loadout | Town safe zone + not in combat (`SafeZonePolicyService` — parallel Mage equip) |
| Slot overflow | Cannot equip 9th devotion while cap is 8; UI shows “Requires higher piety (N slots at ★★★☆☆).” |

### 6.4 — Racial menu (`K`) — Priest body (loadout editor)

Parallel [Human Mage menu](Human-Mage-Racial-Abilities-Menu-Requirements.md):

| Region | Content |
|--------|---------|
| Banner (town) | *Choose which invocations to prepare — {equipped}/{maxSlots} devotion slots.* |
| Banner (dungeon) | *View only — adjust devotions at the shrine in town.* |
| **Left column** | **Prepared devotions** (current loadout — maps to hotbar pool) |
| **Right column** | **Covenant library** (all unlocked invocations; grayed if above slot cap or below piety) |
| Detail pane | DP cost, piety invoke cost, cooldown, conduct tags; **Equip** / **Unequip** in town |
| Footer | Piety meter (0–100), star band, patron emblem, penance warning, **slot cap** |

**Terminology:** UI says **“Prepared devotions”** (not “skill tree”). Engine field: `equippedInvocationIds` on `HumanPriestDevotionRuntime`.

**Out of scope v0 menu:** invoke from menu; take vows (shrine only).

---

## 7. Conduct system

### 7.1 — Structure

Each `PatronGodDefinition` owns a list of **`DivineConductRule`** entries:

| Field | Purpose |
|-------|---------|
| `conductId` | Stable key |
| `kind` | `PietyGain`, `PietyLoss`, `Taboo` |
| `trigger` | Event tag (see §7.2) |
| `pietyDelta` | Amount (scaled optional) |
| `cooldownTurns` | Anti-farm (e.g. exploration piety once per floor) |
| `description` | Player-facing on `K` conduct ledger |

### 7.2 — Example triggers (data-driven)

| Trigger | Example god | Delta |
|---------|-------------|-------|
| `Kill.Undead` | Sun god | +2 |
| `Kill.Innocent` | Sun god | −8 + penance |
| `Explore.NewTile` | Wanderer god | +1 (cooldown) |
| `Ally.DamagedWhileAdjacent` | Protection god | +1 |
| `Corpse.Butchered` | Death god | −3 |
| `Corpse.LeftIntact` | Death god | +1 |
| `Item.Use.Poison` | Healing god | taboo |
| `Rest.AtShrine` | All | +3 (town only) |

### 7.3 — Conduct UI

**`K` menu (Priest body):** read-only **conduct ledger** — patron tenets, recent piety changes (last 5 events), current band, penance flag.

**Not on `K`:** taking vows (shrine only); invoking (hotbar).

---

## 8. Penance & divine retribution

### 8.1 — Penance (DCSS-inspired, softened)

When piety would drop below **1** or a **taboo** fires:

1. Set **`penanceDebt`** (positive integer).
2. Future piety gains pay debt first.
3. While `penanceDebt > 0`:
   - Optional: **suppress high-tier invocations**
   - Apply **retribution** table rolls on rest, floor enter, or every N turns

**Avoid v0:** instant excommunication and unwinnable state.

### 8.2 — Retribution examples (authored per god)

| Severity | Effect |
|----------|--------|
| Mild | −1 random stat for 20 turns |
| Medium | `Silenced` on Priest 1 turn; party hears thunder |
| Severe | Spawn **divine test** enemy; block healing invocations until cleared |
| Vow break | Above + **no piety gain** for rest of run |

### 8.3 — Repentance (town)

Returning to town with penance:

- Shrine interaction: pay gold / donate / quest → clear **partial** debt
- Full clear required before next vow selection

---

## 9. Essence vow & taboo system

### 9.1 — Taboo vs vow

| | **Taboo** | **Vow** |
|--|-----------|---------|
| **Scope** | Patron-defined; always active | Player opt-in per run |
| **Source** | `PatronGodDefinition` | Shrine UI before dungeon |
| **Failure** | Penance + piety loss | Retribution + vow failure flag |
| **Success** | — | Piety bonus + Seal progress |

### 9.2 — Vow structure

```text
PriestVowDefinition
  vowId
  displayName / description
  patronGodIds[]          // empty = any god
  rule                    // scriptable or enum + params
  minFloorIndex           // dungeon depth gate (anti-cheese)
  minDayNightInDungeon    // time-on-floor gate
  pietyRewardOnSuccess
  requiredSealId          // optional — unlock invocation
  stacksWithSelf          // usually false
```

### 9.3 — Example vows

| Vow | Rule | Min floor | Reward |
|-----|------|-----------|--------|
| **Peacebound** | No bladed weapons equipped or used | 2 | +15 piety; Seal `peacebound` |
| **Full vigor** | No invocation unless at 100% HP | 3 | +20 piety |
| **Essence abstinence** | No party member consumes essence this run | 2 | +10 piety (party coordination) |
| **Unburdened** | Inventory ≤ 20 items at all times | 2 | +12 piety |
| **Lantern silence** | No light sources | 4 | +25 piety; rare invocation |

**Floor/time gate rationale:** Prevents farming vow completion on floor 1 repeatedly.

### 9.4 — Multi-vow stacking

| Rule | Detail |
|------|--------|
| Max vows per run | **3** |
| Piety multiplier | `1.0 + 0.25 × (successfulVowCount − 1)` on turn-in (optional tuning) |
| Failure | **Only the broken vow** fails and triggers its retribution. Other active vows **continue**. |
| Re-offer | Broken vow cannot be re-taken until town return + repentance (shrine) |

### 9.5 — Personal vows vs party vows

| | **Personal vow** | **Party vow** |
|--|------------------|---------------|
| **Who must obey** | Priest only | **Whole party** |
| **Who is rewarded** | Priest | **Priest only** |
| **Who is punished** | Priest | **Priest only** |
| **Design goal** | Self-challenge | Coordination challenge — “talk to your Knight before taking this” |
| **UI** | Vow card shows “Personal” | Vow card shows **“Party — all members”** + rule summary |

**Party vow examples:**

| Vow | Party rule | Priest reward |
|-----|------------|---------------|
| **Essence abstinence** | No party member may **consume (equip)** an essence this run | +10 piety; Seal progress |
| **Bloodless delve** | No party member may deal killing blow with blade | +15 piety |
| **Shared burden** | No party member may rest until all living members are below 50% HP | +12 piety |

**Failure handling:** If a **Knight equips an essence** during an active party vow, the **Priest** suffers vow break retribution (and loses that vow’s success reward). Party members see a log line: *{Priest}’s covenant was broken.*

**Player choice:** Shrine vow picker offers both personal and party vows so the player can weigh solo risk vs party coordination.

### 9.6 — Essence-specific vows

Gods may offer:

- **Personal:** “I will not **carry** essence items” (inventory check).
- **Party:** “No essence consumption this delve” (coordinates with Knight players).

These are **vows**, not essence slots — Priest still has 0 slots.

---

## 10. Divine Mark & Covenant Seal

Locked terminology — do **not** use “Divine Brand” in code or UI.

### 10.1 — Divine Mark (combat)

**Target:** enemy or tile.  
**Role:** Short-to-medium duration debuff enabling Priest synergies.

| Example | Effect |
|---------|--------|
| `mark_of_judgment` | Target takes +25% damage from invocations |
| `mark_of_revelation` | Reveals stealth; undead flagged |
| `mark_of_binding` | Reduces enemy move speed |

**Not progression** — applied in combat, costs Divine Power.

### 10.2 — Covenant Seal (progression)

**Target:** Priest character (persistent).  
**Role:** Proof of fulfilled vow or major quest; unlocks invocation or passive.

| Example | Unlock |
|---------|--------|
| Seal `peacebound` | Invocation `aura_of_truce` |
| Seal `undead_slayer` | +conduct piety vs undead |
| Seal `martyr` | Once-per-run death ward |

Displayed on `K` menu as small icons / “seals earned.”

### 10.3 — Stigmata (optional flavor, low priority)

Visible body mark from high piety or patron — NPC reactions, no hide in town. **Cosmetic + ±1 dialogue** unless we want social mechanics later.

| Term | Use |
|------|-----|
| **Divine Mark** | Combat debuff invocation family |
| **Covenant Seal** | Persistent vow / quest unlock |

---

## 11. Patron gods (sample sketch)

v0 vertical slice: **two gods** proving exclusivity + different conducts.

### 11.1 — **Argent Vigil** (sun / law / protection)

| Conduct gain | Conduct loss / taboo |
|--------------|----------------------|
| Kill undead | Kill humanoid non-hostile |
| Protect ally below 30% HP | Use poison |
| Explore new tiles | Leave ally dead without rites (flavor flag) |

**Invocation themes:** heal, ward, smite undead, **Divine Mark: judgment**  
**Vows offered:** Peacebound (personal), Full vigor (personal), Essence abstinence (**party**)

### 11.2 — **Marrow Keeper** (cycle / death / endurance)

| Conduct gain | Conduct loss / taboo |
|--------------|----------------------|
| Leave corpses unbutchered | Destroy corpses |
| Endure damage without fleeing tile | Use fire invocations (taboo) |
| Rest with party injured | Full-heal before invoking |

**Invocation themes:** drain, bone armor, raise fallen ally as brief ally, corpse explosion  
**Vows offered:** Unburdened, Lantern silence

Each god references a **`PatronGodDefinition`** asset; Priest save stores `patronGodId`.

---

## 12. Class commitment (shrine gate)

Mirror [Mage tutor quest](Human-Mage-Spells-And-Spellbooks-Requirements.md) and [Knight Drill Master](Human-Knight-Auras-And-Skill-Tree-Requirements.md):

| Step | Rule |
|------|------|
| Location | Town shrine NPC (**Argent Vigil** altar steward in v0) |
| Quest id | `quest_priest_shrine_initiation` (stable) |
| Cost | **5 gold** on turn-in (v0 default — tunable) |
| Requires | `HumanClass.None`, **no consumed essences** equipped |
| Choice | Player confirms **Argent Vigil** as patron (only god in Slim v0) |
| Effect | `humanClass = Priest`, `patronGodId = argent_vigil`, piety **10**, **2** devotion slots, starter invocations unlocked per §16.1.4 |

**Editor bootstrap:** `JRogue/Racial/Create Human Priest Shrine Pack` — creates quest asset, patron god, invocations, shrine NPC prefab, plaza marker; run **`JRogue/Town/Fix Town Test Scene`** to wire markers (same discipline as Knight/Mage packs).

**Invariant:** Cannot commit Mage or Knight afterward.

---

## 13. Racial menu (`K`) — Priest body

Full layout in **§6.4**. Summary:

- **Town:** two-column loadout editor (prepared devotions | covenant library) + detail-pane Equip/Unequip.
- **Dungeon:** read-only reference + conduct ledger.
- **Hotbar:** prepared devotions only (`HotbarAssignabilityService`).

Future doc: `Human-Priest-Racial-Abilities-Menu-Requirements.md` (mirror Mage/Knight menu specs) when UI ships.

---

## 14. Data contracts (draft)

| Asset / type | Role |
|--------------|------|
| `PatronGodDefinition` | God identity, conduct list, vow catalog, invocation ids, band passive overrides |
| `PriestPietyProgressionDefinition` | Global piety bands, devotion slot counts, star UI thresholds |
| `PriestInvocationDefinition` | Ability payload, costs, required piety band, required Seal |
| `PriestVowDefinition` | Vow rules, `scope` (Personal / Party), floor/time gates, rewards |
| `HumanPriestCovenantRuntime` | `patronGodId`, piety, penance, active vows, seal set |
| `HumanPriestDevotionRuntime` | Unlocked + equipped invocation ids |
| `DivineConductDispatcher` | Subscribes to combat/world events → piety deltas |

**Passive progression:** `HumanPriestCovenantRuntime` + `PriestPietyProgressionDefinition` own passives (**Model B**, §4.4). `HumanClassSkillTreeRuntime` remains for shared Human class tests only — not Priest player UI.

---

## 15. Open questions (remaining)

### 15.1 — Resolved (design)

| Topic | Decision |
|-------|----------|
| Piety scale | **100** (tunable later) |
| Vow failure scope | **Broken vow only** |
| Party vows | **Yes** — party obeys, **Priest** rewarded/punished |
| Divine Mark / Covenant Seal | **Locked** — no “Brand” term |
| Devotion slots | **2 → 8** by piety band; **8** absolute cap |
| Hotbar + `K` menu | **Yes** — `K` ships in **Full covenant v0**; Slim uses shrine stub + hotbar |
| D2 passive tree for shipping | **No** — piety band passives (Model B) in **Full covenant v0** |
| Implementation roadmap | **Slim v0** → **Full covenant v0** → defer §16.3 |
| Shrine commit | **5 gold**, `quest_priest_shrine_initiation`, patron **argent_vigil** only until post-roadmap |
| Starting piety | **10** on commit |
| Piety bands (v0) | §5.2.1 table — authoritative until playtest retune |
| God switching | **Never** |
| Piety decay | **Off** until post-roadmap |
| Dual-Priest party | **Allowed** (post-roadmap content) |
| Invocation scaling | **Level** gates eligibility; **piety band** gates potency (Full covenant v0) |
| Model B vs C | **Model B**; Model C only if Slim playtest feels starved |

### 15.2 — Tune in playtest (defaults OK for implementation)

| Topic | v0 default |
|-------|------------|
| Conduct piety deltas | Per §7.2 examples; ±1–3 for common events |
| Penance debt amounts | **5–15** for taboo; vow break **10** |
| Vow `minFloorIndex` | **2** |
| Vow `minDayNightInDungeon` | **2** |
| DP regen | **Full** on rest / town return (dungeon-time doc parity) |

---

## 16. Implementation roadmap

Two milestones, each ending in a **playable manual test** from the town test scene. Do **not** start §16.3 until both are complete.

### 16.0 — Shared manual-test contract (both milestones)

Every milestone must satisfy:

| Requirement | Detail |
|-------------|--------|
| **Editor pack** | Menu item creates/refreshes assets + NPC prefab + quest |
| **Town wiring** | Plaza marker placed; `Fix Town Test Scene` picks it up |
| **Fresh Human** | `HumanPlayer` defaults to `HumanClass.None` (no preset Priest) |
| **Commit in play** | Talk to shrine NPC in town → become Priest without debug cheats |
| **Dungeon loop** | Enter dungeon → use at least one invocation from **hotbar** |
| **Feedback** | Piety changes visible (game log minimum; `K` or shrine UI when available) |
| **Regression** | Essence equip blocked; Mage/Knight cannot take Priest quest |

---

### 16.1 — Slim v0 (covenant core — manually playable)

**Goal:** Prove patron + piety + conduct + invocations + devotion loadout + hotbar. **No vows, no Seals, no penance, no slot scaling, no `K` menu.**

#### 16.1.1 — Runtime & data

| Deliverable | Detail |
|-------------|--------|
| `PatronGodDefinition` | `argent_vigil` — conduct list, invocation ids |
| `PriestInvocationDefinition` | **4** actives (see §16.1.4) |
| `PriestPietyProgressionDefinition` | Bands + slot table authored; **Slim uses fixed 2 slots** (scaling dormant) |
| `HumanPriestCovenantRuntime` | `patronGodId`, piety, conduct log (last N events) |
| `HumanPriestDevotionRuntime` | Unlocked + equipped invocation ids |
| `DivineConductDispatcher` | **3** rules minimum (§16.1.3) |
| `HumanClassCommitment` | Priest path: zero essences, zero Soul Power, init DP |
| `HotbarEntryKind.HumanPriestInvocation` | Assign + execute pipeline |
| Quest + NPC | `quest_priest_shrine_initiation`, shrine steward NPC |

#### 16.1.2 — UI (minimal)

| Surface | Slim v0 |
|---------|---------|
| **Shrine dialog** | Commit quest + turn-in; show piety after commit |
| **Devotion equip** | **Shrine “Prepare devotions” stub** after commit — forced-choice pick **2** of unlocked invocations (replaces `K` until Full covenant v0) |
| **`K` menu** | Placeholder banner for Human Priest (“Shrine prepares devotions in town”) |
| **Hotbar** | Player assigns prepared devotions to hotbar slots |
| **Game log** | `[Piety]` lines on conduct events |

#### 16.1.3 — Argent Vigil conduct (Slim v0 minimum)

| Rule | Trigger | Delta |
|------|---------|-------|
| `argent_slay_undead` | `Kill.Undead` | +2 |
| `argent_explore` | `Explore.NewTile` | +1 (once per floor cooldown) |
| `argent_no_poison` | `Item.Use.Poison` | taboo → −5 piety, log warning (no penance system yet) |

#### 16.1.4 — Argent Vigil invocations (Slim v0)

| id | Piety to unlock | Level | Role |
|----|-----------------|-------|------|
| `priest_lay_on_hands` | 0 (starter) | 1 | Single-target heal; DP cost |
| `priest_ward` | 0 (starter) | 1 | Short buff / damage reduction |
| `priest_smites_undead` | 10 | 1 | Bonus vs undead; DP cost |
| `priest_sanctuary` | 20 | 3 | Defensive cooldown active |

On commit: piety **10** → first two always unlocked; **smite** unlocked; **sanctuary** locked until ★★☆☆☆.

#### 16.1.5 — Slim v0 manual test script

1. Run **`JRogue/Racial/Create Human Priest Shrine Pack`**, then **`JRogue/Town/Fix Town Test Scene`**.
2. Play town scene as default **Human** (`HumanClass.None`).
3. Talk to **Argent Vigil shrine steward** → accept quest → pay **5 gold** → become Priest.
4. Use shrine **Prepare devotions** → equip **Lay on Hands** + **Smite Undead** (2 slots).
5. Assign both to **ability hotbar**.
6. Enter dungeon; kill an undead → log shows **+2 piety**.
7. Invoke **Smite Undead** on target → spends **Divine Power**.
8. Confirm essence equip **fails** on Priest.

**Slim v0 done when:** all steps pass without Inspector edits.

---

### 16.2 — Full covenant v0 (manually playable)

**Goal:** Complete covenant fantasy on top of Slim — vows, party vows, Seals, penance, retribution, piety band passives, devotion slot scaling **2→8**, **`K` loadout editor**. Still **one god** (Argent Vigil).

#### 16.2.1 — Adds on top of Slim

| Deliverable | Detail |
|-------------|--------|
| **Vow shrine UI** | Before dungeon descent: pick up to **3** personal and/or party vows |
| **Vow tracking** | Per-vow state machine; **only broken vow** fails |
| **Party vow enforcement** | Party actions checked; Priest rewarded/punished only |
| **Covenant Seal** | Persist fulfilled vow; unlock e.g. `priest_aura_of_truce` |
| **Penance + retribution** | Taboo/vow break → debt + mild/medium effects (§8) |
| **Slot scaling** | `PriestPietyProgressionDefinition` live — **2→8** slots by band |
| **Piety band passives** | Model B — auto-apply on band crossing |
| **`K` Priest body** | Two-column loadout editor (§6.4); replaces shrine devotion stub as primary editor |
| **Conduct ledger** | Last 5 piety events on `K` |
| **Repentance** | Shrine clears penance before new vows |

#### 16.2.2 — Vows shipping in Full covenant v0

| Vow | Scope | Min floor | Min day/nights |
|-----|-------|-----------|----------------|
| **Peacebound** | Personal | 2 | 2 |
| **Full vigor** | Personal | 2 | 2 |
| **Essence abstinence** | **Party** | 2 | 2 |

Seal example: **Peacebound** success → Seal `peacebound` → unlocks `priest_aura_of_truce`.

#### 16.2.3 — Full covenant v0 manual test script

1. Complete **Slim v0** regression (§16.1.5).
2. At shrine, take **Essence abstinence** (party vow) + **Peacebound** (personal).
3. Open **`K`** → verify piety **10**, **2/2** devotion slots, conduct ledger empty.
4. Prepare devotions in **`K`** (not shrine stub).
5. Enter dungeon; explore until vow time gates met; finish delve without breaking vows.
6. Return to town → shrine **Report vows** → gain piety + **Peacebound** Seal.
7. Raise piety to **20** (conduct) → **`K`** shows **3** slots; passive **+1 Wisdom** applied.
8. Break test: start new run with party vow → have Knight equip essence → Priest vow breaks; other vow **still active**; Priest sees retribution log.
9. Repent at shrine → penance cleared → take new vows.

**Full covenant v0 done when:** all steps pass; shrine devotion stub removed or demoted to vow-only flow.

---

### 16.3 — Deferred (after Full covenant v0)

Do not implement until Slim + Full covenant v0 both pass manual tests.

| Item | Notes |
|------|-------|
| **Second patron god** (Marrow Keeper) | Proves exclusivity + opposing conducts |
| **Divine Mark** invocation family | Combat debuff layer |
| **Quest-linked Seals** | Beyond vow fulfillment |
| **Piety decay** per god | DCSS upkeep |
| **`Human-Priest-Racial-Abilities-Menu-Requirements.md`** | Formal UI spec if `K` body outpaces shell doc |
| **Channeled aura devotions** | One active channeled devotion |
| **Stigmata** cosmetics | High-piety flavor |
| **Proficiencies (`P`)** Priest section | |
| **Invocation mastery** | Use-based grind — intentionally absent |
| **Update `Human-Class-Powers-Requirements.md` §9** | Point to this doc; retire D2 Priest tree as shipping model |

---

## 17. Acceptance criteria

### 17.1 — Slim v0

- Given `HumanClass.None` with equipped essence, shrine quest **rejects**.
- Given commit, `humanClass == Priest`, `patronGodId == argent_vigil`, piety **10**.
- Given Priest, essence equip **fails**.
- Given undead kill, piety increases and game log records conduct.
- Given 2-slot cap, 3rd devotion equip **fails** (shrine stub or `K`).
- Given prepared devotion on hotbar, invoke spends **Divine Power**.

### 17.2 — Full covenant v0

- All Slim v0 criteria (regression).
- Given party vow active, Knight essence equip breaks **only** that vow; Priest penalized.
- Given two vows, breaking one leaves the other **trackable**.
- Given vow success after floor/time gates, **Covenant Seal** persists after town return.
- Given piety band crossing, devotion slot cap and passive boon update.
- Given penance, high-tier invocation **blocked** until repentance.
- Given `K` in dungeon, loadout is **view-only**.

### 17.3 — Post-roadmap (deferred)

- Second god commit at separate altar.
- Divine Mark applied to enemy.

---

## 18. Related documents

- [Human — Class powers](Human-Class-Powers-Requirements.md) — update §9 when this doc locks
- [Human Mage — Spells & spellbooks](Human-Mage-Spells-And-Spellbooks-Requirements.md)
- [Human Knight — Auras & skill tree](Human-Knight-Auras-And-Skill-Tree-Requirements.md)
- [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md)
- [Phase 0 — Glossary](Phase0-Glossary-And-Data-Contracts.md)

---

## Appendix A — Three-class player fantasy (one paragraph each)

**Knight:** “I trained my body and borrowed essence tools; my auras and skills **grow through practice**.”

**Mage:** “I studied arcana and **curate a spellbook**; power is knowledge and preparation.”

**Priest:** “I swore a covenant; my god watches **how I behave**. I **choose harder vows** for deeper favor, and I **invoke** blessings when my faith and focus allow.”
