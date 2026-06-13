# Beastman — Soul Beast contract ritual & leveling (requirements)

**Purpose:** Specify how a **Beastman** **forms a permanent contract** with at most **one** **Soul Beast** (ritual gate), and how that bonded Soul Beast **levels up** using **Beast Blood** (progression gate). Implements the acquisition and progression gates deferred from [Beastman — Soul Beast](Beastman-Soul-Beast-Requirements.md) §11.4.

**Inspiration:** *Surviving the Game as a Barbarian* — Beastmen perform **rituals** to attract a Soul Beast companion; they feed **beast blood** to deepen the bond. Barbarian = deterministic imprint node at Shaman; Beastman contract = **weighted ritual appearance**; Beastman leveling = **consumable blood** + **contractor level cap** (parallel to Elf meditation + character level cap).

**Status:** Implemented (v0).

**Beast Blood shop price (locked):** **2 gold** each.

**Depends on:** [Beastman — Soul Beast](Beastman-Soul-Beast-Requirements.md) (`SoulBeastDefinition`, `SoulBeastType`, linear chain / level payloads, `BeastmanSoulBeastRuntime`, one-beast-forever), [Shop NPCs](../World/Shop-NPC-Requirements.md) (party gold, sell-only stock), [NPC dialog](../World/NPC-Dialog-Requirements.md) (`NpcDialogBoxUI`), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (Use action, greyed unusable rows), [Safe zone](../World/Safe-Zone-Requirements.md), `InventoryItemUse`, `PartyManager`, `CharacterStats.race` / `CharacterStats.level`, `GameplayModalGate`, `RacialProgressionPayloadApplicator` discipline.

**Related:** [Elf — Fairy Stone spirit contracts](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) (probabilistic contract item — **different** fiction; Beastman ritual is **typed + weighted pool**, not uniform 50%). [Elf — Elemental Spirit meditation & leveling](Elf-ElementalSpirit-Meditation-Leveling-Requirements.md) (level cap policy pattern). [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) (town safe-zone progression NPC). [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md), [Beastman — racial abilities menu](Beastman-Racial-Abilities-Menu-Requirements.md) (read-only bond + ability list).

**Explicitly out of scope (v0):** Replace / respec Soul Beast after contract; **multiple** Soul Beasts on one Beastman; ritual or blood use in **combat** or **dungeon** (town / safe zone only for v0 gates); bespoke full-screen ritual cinematic UI; Beast Blood as dungeon loot (shop-only v0); save/load beyond existing party persistence; gamepad layout; **Race** other than Beastman performing rituals.

---

## FAQ — “Did we already cover level-up payloads?”

**Partially yes** — the parent [Beastman — Soul Beast](Beastman-Soul-Beast-Requirements.md) doc defines **chain nodes** (B6.2) where each node may carry:

- `statModifiers`
- `resistanceModifiers`
- `passiveEffects`
- `activeAbilities`

Those effects apply to the **bonded Beastman** (always-on while bonded — not summon-gated like Elf spirits).

**What was not covered until this doc:**

| Topic | Parent doc | This doc |
|-------|------------|----------|
| **How to acquire** the bond | Inspector preset only; “special event TBD” | **Ritual** flow (typed pool, item weights, appearance roll, accept dialog) |
| **How to advance** | Append **one chain node** per progression event (TBD) | **`soulBeastLevel`** via **Beast Blood** Use |
| **Integer level** | Derived **rank** = path depth | Persisted **`soulBeastLevel`** with **cumulative** rows 1…L |
| **Contractor cap** | Not specified | **`soulBeastLevel ≤ effectiveCap`** (v0: contractor `CharacterStats.level`) |
| **v0 sample content** | Stats only on nodes | Unchanged for samples; framework supports passives + actives per level |

**Canonical authoring (this doc):** each `SoulBeastDefinition` exposes **`levels[1 … maxLevel]`** rows (mirror [Elf per-level rows](Elf-ElementalSpirit-Contracts-Requirements.md) D4.2). Existing **linear chain** assets may **map 1:1** to level rows (level *L* = chain node at depth *L*); new content should prefer **level rows** directly.

**Cumulative rule (locked):** when bonded at **`soulBeastLevel = L`**, apply payloads from **all rows 1 … L** to the Beastman with distinct sources (Pattern B), same cumulative discipline as Elf summoned spirits and Barbarian path nodes.

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | **At most one** Soul Beast contract per Beastman; **no replacement** after bond (parent B5.1). |
| **L2** | **Ritual performer** = the Beastman party member who **takes** the ritual (not “active leader only”). |
| **L3** | Ritual **eligibility:** performer must be `Race.Beastman`, `RacialSubsystemKind.BeastmanSoulBeast`, and **unbonded** (`soulBeastId` empty). |
| **L4** | **Ritual type** (player choice) **filters** the Soul Beast candidate pool (§5.2). |
| **L5** | **Contributed ritual items** (optional, player choice) **further filter** the pool and **add weight** to matching beasts (§5.3). |
| **L6** | **Appearance roll:** from the final weighted pool, **at most one** Soul Beast may appear; **zero** appearances = **ritual failure** (§5.4). |
| **L7** | On appearance, **contract dialog** asks whether the performer accepts bond with **that** beast; **No** = ritual fails (beast leaves); **Yes** = permanent contract at **`soulBeastLevel = 1`** (§5.5). |
| **L8** | **Beast Blood** is the v0 gate for **raising `soulBeastLevel`** on an **existing** bond (§7). |
| **L9** | **`soulBeastLevel` cap (v0):** `effectiveCap = min(soulBeast.maxLevel, contractor.level)` — pluggable policy (§7.3). |
| **L10** | **Beast Blood Use** requires a bonded Beastman target; otherwise **greyed out** in inventory (§7.4). |
| **L11** | Ritual and Beast Blood Use **do not consume a combat turn** (town / inventory management). |
| **L12** | **v0 delivery — ritual (repeatable):** town **Soul Beast Ritual Circle** interactable in **safe zone** (§6.1). |
| **L13** | **v0 delivery — Beast Blood:** sold by a **town shop NPC** (§7.2); consumed via inventory **Use**. |
| **L14** | **Future story events** call the same **`SoulBeastRitualService`** / **`SoulBeastLevelService`** APIs as v0 gates — no duplicate logic. |

---

## Part A — Soul Beast contract ritual

### A1. Goals

| ID | Goal |
|----|------|
| **G1** | **Repeatable v0 testing** — designers and players can run rituals **many times** without story flags (§6). |
| **G2** | **Typed rituals** — player chooses ritual flavor that **limits** which Soul Beasts can appear. |
| **G3** | **Item offerings** — optional contributions **narrow** the pool and **bias** weights toward specific beasts. |
| **G4** | **Weighted random** — transparent pool + weights; **failure** (no appearance) is a valid outcome. |
| **G5** | **Informed consent** — appearance alone does **not** bond; player must **accept** in dialog. |
| **G6** | **One beast forever** — success sets bond; performer cannot ritual again until save edited / future respec (out of scope). |
| **G7** | **Service discipline** — all ritual mutations through **`SoulBeastRitualService`** (mirror `ElementalSpiritContractService`). |

### A2. Glossary (ritual)

| Term | Meaning |
|------|--------|
| **Ritual event** | One player-initiated session: pick ritual type → optional offerings → roll → maybe appearance dialog → maybe bond. |
| **Ritual type** | Data-defined category (e.g. **Summoning**, **Enhancement**, **Special Ability**, **Specialist**) aligned with `SoulBeastType` — limits candidate pool. |
| **Ritual performer** | The Beastman who performs the ritual and would receive the bond. |
| **Candidate pool** | Soul Beasts still eligible after type + item filters. |
| **Weight** | Integer ≥ 0 per candidate; **0 weight excludes** from roll. |
| **Appearance roll** | One weighted draw; **empty pool** or **“none” outcome** → failure. |
| **Ritual failure** | No bond formed: no appearance, declined dialog, or invalid eligibility. |
| **Contract dialog** | Shown when a beast **appears**; Yes = bond; No = failure. |

### A3. Ritual eligibility

| Check | Rule |
|-------|------|
| Performer race | `Race.Beastman` |
| Subsystem | `RacialSubsystemKind.BeastmanSoulBeast` |
| Bond state | **`soulBeastId` empty** — already contracted Beastmen **cannot** take the ritual |
| Location (v0) | **Safe zone** (town) — same discipline as [Elf meditation](Elf-ElementalSpirit-Meditation-Leveling-Requirements.md) L12 |
| Party | Performer must be a **live party member** |

**UI copy when blocked (already bonded):** *“{Name} is already bound to a Soul Beast.”*

### A4. Ritual flow (player steps)

```
1. Player starts ritual at gate (§6)
2. If multiple Beastmen in party → pick performer (Cancel/Esc aborts — no costs)
3. If performer ineligible (bonded) → error message; abort
4. Player picks **ritual type** (required)
5. Player optionally adds **offering items** from inventory (0…N slots — cap TBD in content, suggest 3)
6. Player confirms **Perform ritual**
7. Service builds weighted pool → rolls appearance (§5.4)
8a. Failure → feedback line; session ends
8b. Success → show **contract dialog** with beast name, type, short description
9. Player **Yes** → bond `soulBeastId`, `soulBeastLevel = 1`, apply level-1 cumulative payloads
   Player **No** → ritual fails; no bond
```

**Costs (v0):** ritual gate may charge **party gold** and/or **consume offering items** on confirm (before roll). Exact costs authored per `SoulBeastRitualTypeDefinition` — suggest **offerings consumed on confirm**, base gold **optional** (0 for dev-friendly v0).

### A5. Pool construction & weights

#### A5.1 — Base pool

Start from **`SoulBeastRegistry`** (all `SoulBeastDefinition` assets) or a ritual-type-specific subset list.

#### A5.2 — Ritual type filter

Each **`SoulBeastRitualTypeDefinition`** specifies:

| Field | Purpose |
|-------|---------|
| **`ritualTypeId`** | Stable id |
| **`displayName`** / **`description`** | UI |
| **`allowedSoulBeastTypes`** | Subset of `SoulBeastType` — beast must match **one** |
| **`baseWeights`** | Optional per-`soulBeastId` overrides; default weight **1** for allowed types |

**Example:** “Enhancement rite” → pool = all beasts where `soulBeastType == Enhancement`.

#### A5.3 — Offering item contributions

Each **`SoulBeastRitualOfferingDefinition`** (on `ItemData` or sidecar asset) specifies:

| Field | Purpose |
|-------|---------|
| **`requiredRitualTypes`** | Empty = any ritual; else item only valid for listed types |
| **`poolFilterTags`** | Beast must have **all** tags (or **any** — pick one rule; **locked: any tag match** keeps pool wider) |
| **`weightBonuses`** | `soulBeastId → +weight` or `tag → +weight` for beasts matching tag |
| **`poolExcludes`** | Optional beast ids **removed** from pool when this item is offered |

**Filter order (locked):**

1. Start registry
2. Apply **ritual type** filter (`allowedSoulBeastTypes`)
3. For each offered item, apply **tag filter** (intersection across items — beast must satisfy **every** offered item’s filter, or item skipped if incompatible with ritual type)
4. Compute **final weight** = `baseWeight + sum(bonuses)` per beast; clamp at 0

#### A5.4 — Appearance roll

| Case | Result |
|------|--------|
| **Empty pool** after filters | **Failure** — show *“The ritual finds no answering soul.”* |
| **Non-empty pool** | Weighted random **one** entry **or** explicit **“none”** outcome |

**Locked v0 roll model:**

- Total weight `W = sum(beast weights)`
- Roll integer `r` in `[0, W]` inclusive interpretation:
  - **`r == 0`** OR dedicated **`failureWeight`** slice → **no appearance** (failure)
  - Else map `r` to one beast

**Suggested default:** `failureWeight = W / 2` (50% failure chance when pool non-empty) **OR** author `failureWeight` per ritual type. **Recommend v0:** each `SoulBeastRitualTypeDefinition` sets **`noneOutcomeWeight`** (default **50%** of total alongside beast weights) so designers tune difficulty without code changes.

**At most one** beast per ritual — single roll, no multi-spawn.

#### A5.5 — Contract dialog

When a beast **appears**:

| Element | Content |
|---------|---------|
| **Title** | *“A Soul Beast appears”* |
| **Body** | Beast **`displayName`**, **`soulBeastType`**, **`description`** excerpt |
| **Choices** | **Form contract** (Yes) · **Send it away** (No) |

| Choice | Result |
|--------|--------|
| **Yes** | Set `soulBeastId`, `soulBeastLevel = 1`, `contractExperience = 0` if used; apply cumulative level-1 payloads; success log |
| **No** | Close dialog; **ritual failure**; no bond |

**No** does **not** grant a “second chance” roll in the same session.

### A6. v0 delivery — easily repeatable ritual

**Problem:** final game wants rituals as **rare special events**; implementation needs a gate that is **easy to trigger repeatedly** for testing and iteration.

**Locked v0 approach (two hooks, same service):**

| Hook | Purpose | Repeatable? |
|------|---------|-------------|
| **A6.1 — Soul Beast Ritual Circle** | Town interactable (safe zone), visible on plaza or near beast-temple stamp marker | **Yes** — unlimited attempts |
| **A6.2 — Editor dev menu** | `JRogue → Racial → Test Soul Beast Ritual` — opens same dialog flow in Edit/Play mode | **Yes** — for rapid QA |

**Future (not v0):** story quests, one-shot world events, and cutscenes call:

```text
SoulBeastRitualService.TryBeginRitual(
    ritualGateDefinition,
    performer,
    ritualTypeId,
    offerings,
    out result);
```

…with optional **`maxAttempts`**, **`storyFlag`**, or **location lock** on the gate definition — without forking roll logic.

**Why not inventory consumable for v0 ritual?** Offerings are **ingredients**, not the gate itself — avoids conflating “buy ritual scroll” with Beast Blood progression and keeps failure loops cheap (walk to circle again).

**Town placement:** add **`StampMarkerKind.SoulBeastRitualCircle`** (or reuse generic interactable spawn) in [town pack creator](../World/Town-Test-Scene-Requirements.md) beside other racial services (Shaman, Forgemaster, Fairy Merchant).

### A7. Ritual acceptance criteria

| ID | Test |
|----|------|
| **R1** | Unbonded Beastman completes ritual → can bond; **`soulBeastLevel == 1`**. |
| **R2** | Bonded Beastman → ritual gate shows ineligible; no bond change. |
| **R3** | Ritual type **Enhancement** → no **Summoning**-type beast in pool. |
| **R4** | Offering item with tag filter → pool excludes non-matching beasts. |
| **R5** | Offering item with weight bonus → targeted beast appears more often (statistical or seeded test). |
| **R6** | Roll **none** → failure message; no bond. |
| **R7** | Appearance + dialog **No** → no bond. |
| **R8** | Appearance + dialog **Yes** → bond; second ritual on same actor **blocked**. |
| **R9** | Non-Beastman cannot be ritual performer. |

---

## Part B — Soul Beast leveling (Beast Blood)

### B1. Goals

| ID | Goal |
|----|------|
| **G8** | **Shop access** — player buys **Beast Blood** from a town NPC. |
| **G9** | **Inventory Use** — bonded Beastman consumes blood to gain **one Soul Beast level** (when below cap). |
| **G10** | **Contractor coupling** — Soul Beast level bounded by performer’s **character level** in v0. |
| **G11** | **Extensible cap** — policy swappable later (e.g. **2× contractor level**) without rewriting Use flow. |
| **G12** | **Payload refresh** — level-up re-applies cumulative rows 1…L on the Beastman (passives, actives, stats). |
| **G13** | **Clear UX** — greyed Use when no bonded Beastman; message when at cap. |

### B2. Glossary (leveling)

| Term | Meaning |
|------|--------|
| **Beast Blood** | Consumable `ItemData` (`beast_blood` id) used to raise Soul Beast level. |
| **Soul Beast level** | Integer **`soulBeastLevel`** on bonded runtime — **1 … effectiveCap**. |
| **Effective level cap** | Max level **right now** (§B3). |
| **Level cap policy** | Pluggable rule (v0: contractor character level). |
| **Level row** | Payload block for level **L** on `SoulBeastDefinition.levels[L]`. |

### B3. Level cap policy (extensible)

**`ISoulBeastLevelCapPolicy`:**

```text
int ResolveEffectiveCap(
    CharacterStats contractorStats,
    SoulBeastDefinition beastDef);
```

| Policy | v0? | Rule |
|--------|-----|------|
| **`CharacterLevelSoulBeastCapPolicy`** | **Yes (default)** | `effectiveCap = min(beastDef.maxLevel, contractorStats.level)` |
| **`DoubleCharacterLevelCapPolicy`** | Future | `effectiveCap = min(beastDef.maxLevel, contractorStats.level * 2)` |
| *(custom)* | Future | Quest flags, Wisdom, party level, etc. |

**Locked:** policy is **data-selected** on `BeastmanSoulBeastRuntime` or global config — swap without changing Beast Blood item logic.

**At cap:** Use is **disabled / greyed** with tooltip: *“Soul Beast level cannot exceed {Name}'s level ({cap}).”*

**No de-level:** if contractor level drops (not planned), **`soulBeastLevel` is never reduced** — only future gains blocked (mirror [Elf meditation O10](Elf-ElementalSpirit-Meditation-Leveling-Requirements.md)).

### B4. Level-up payloads (per level)

Each **`SoulBeastLevelData`** row (level **L**) may include **zero or more** of:

| Field | Applies to Beastman when bonded |
|-------|-----------------------------------|
| **`statModifiers`** | Yes — cumulative |
| **`resistanceModifiers`** | Yes — cumulative |
| **`passiveEffects`** | Yes — cumulative; `RefreshPassives` / turn hooks |
| **`activeAbilities`** | Yes — cumulative; hotbar assignability when implemented |

**Cumulative application:** at level **L**, apply rows **1…L** with source keys e.g. `SoulBeast:{soulBeastId}:L`.

**Level-up transaction:**

1. Validate bonded + below cap
2. Consume **one Beast Blood**
3. `soulBeastLevel++`
4. Remove prior level-sourced modifiers from old **L−1** set if incremental apply; **or** full rebuild 1…L (prefer **full rebuild** for simplicity — mirror Elf L7)
5. Refresh hotbar assignables + passives

**One blood = one level** (no XP pool in v0). Future: XP + multiple level-ups per item can plug same service.

### B5. Beast Blood item & shop

#### B5.1 — Item data

| Field | Value |
|-------|-------|
| **`itemId`** | `beast_blood` |
| **Display name** | **Beast Blood** |
| **Category** | Consumable |
| **Use** | Triggers **`SoulBeastLevelService.TryUseBeastBlood`** |

#### B5.2 — Shop NPC (v0)

| Field | Suggestion |
|-------|------------|
| **NPC** | **Beast Blood Merchant** (or shared apothecary) in town |
| **Mode** | Sell-only |
| **Price** | **2 gold** (locked) |
| **Stock** | Unlimited Beast Blood |

Parallel: [Fairy Merchant](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) sells Fairy Stones; this NPC sells Beast Blood.

#### B5.3 — Inventory Use flow

| Step | Behavior |
|------|----------|
| 1 | If **no** `Race.Beastman` in party with **active bond** → row **greyed**; tooltip *“Requires a Beastman bonded to a Soul Beast.”* |
| 2 | If **multiple** bonded Beastmen (edge case — normally impossible) → picker; v0: **at most one bond per party** assumed |
| 3 | If **one** eligible Beastman → confirm dialog optional (recommend **instant** Use like potion — or confirm if design prefers) |
| 4 | If **`soulBeastLevel >= effectiveCap`** → greyed; tooltip cap reason |
| 5 | Else consume blood + level up + feedback |

**Picker when multiple Beastmen, only one bonded:** auto-target bonded member.

**Picker when multiple Beastmen, none bonded:** greyed (same as step 1).

### B6. Leveling acceptance criteria

| ID | Test |
|----|------|
| **L1** | Bonded Beastman at level 1, contractor level 5, maxLevel 10 → Use raises to 2. |
| **L2** | At **`soulBeastLevel == contractor.level`** → Use blocked. |
| **L3** | Unbonded Beastman in party → Use greyed. |
| **L4** | Level-up applies **new row’s** stats + passives + actives (data-driven test asset). |
| **L5** | **`DoubleCharacterLevelCapPolicy`** (test hook) → cap = 2× level without changing item code. |
| **L6** | Non-Beastman cannot consume Beast Blood for Soul Beast (greyed). |

---

## Part C — Data model extensions

### C1 — `SoulBeastDefinition` (extend parent B6.1)

| Field | Notes |
|-------|-------|
| **`maxLevel`** | ≥ 1 |
| **`levels[]`** | **`SoulBeastLevelData`** per level 1…maxLevel |
| **`tags[]`** | Optional strings for ritual offering filters (e.g. `wolf`, `fire`, `feral`) |
| *(existing)* **`soulBeastType`** | Ritual type filter |
| *(existing)* **`abilityChain`** | **Optional legacy** — editor tool may **import chain → levels** |

### C2 — `SoulBeastLevelData`

| Field | Requirement |
|-------|-------------|
| **`statModifiers`** | Optional list |
| **`resistanceModifiers`** | Optional list |
| **`passiveEffects`** | Optional list |
| **`activeAbilities`** | Optional list |

### C3 — `BeastmanSoulBeastRuntime` (extend parent B6.4)

| Field | Notes |
|-------|-------|
| **`soulBeastId`** | Empty = unbonded |
| **`soulBeastLevel`** | ≥ 1 when bonded |
| *(optional legacy)* **`chosenPathNodeIds`** | Deprecated when level rows canonical; migrate to level |

### C4 — Ritual authoring assets

| Asset | Purpose |
|-------|---------|
| **`SoulBeastRitualTypeDefinition`** | Ritual type id, allowed beast types, default weights, `noneOutcomeWeight` |
| **`SoulBeastRitualOfferingDefinition`** | On items — filters + weight bonuses |
| **`SoulBeastRitualGateDefinition`** | Interactable / event hook — costs, allowed types, safe-zone flag |

---

## Part D — Integration matrix

| System | Ritual | Leveling |
|--------|--------|----------|
| **`BeastmanSoulBeastRuntime`** | Sets bond + level 1 | Increments level |
| **`RacialLoadoutApplier`** | Unchanged (folk baseline) | Unchanged |
| **`RacialPassiveHooks`** | After bond | After level-up |
| **Ability hotbar** | After bond (actives from level 1) | Refresh assignables |
| **Racial abilities menu** | Future — show bond + level | Future — show level + cap |
| **Dungeon log** | Success / failure lines | Level-up line |
| **`GameplayModalGate`** | Ritual dialog blocks movement | Use via inventory overlay rules |

---

## Part E — Implementation phases

| Phase | Scope |
|-------|-------|
| **v0 — ritual** | `SoulBeastRitualService`, ritual circle interactable, type picker, offering slots, weighted roll, contract dialog, dev menu |
| **v0 — leveling** | `Beast Blood` item, shop NPC, `SoulBeastLevelService`, cap policy v0, cumulative payload rebuild |
| **v0.1** | Sample beasts with passives + actives on levels 2–3; hotbar wiring |
| **v1** | Story-gated ritual events reusing service; optional XP curve instead of 1 blood = 1 level |
| **v1** | Beastman racial menu body (read-only bond sheet) |

---

## Part F — Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Beastman — Soul Beast](Beastman-Soul-Beast-Requirements.md) | §11.4 acquisition/progression gates → link here; level rows |
| [Phase0-Glossary](Phase0-Glossary-And-Data-Contracts.md) | Beast Blood, ritual type |
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | Beastman row |
| [Shop NPCs](../World/Shop-NPC-Requirements.md) | Beast Blood merchant entry |

---

## Part G — Document history

| Date | Change |
|------|--------|
| 2026-06-13 | v0 implementation — ritual circle, Beast Blood merchant (2 gold), runtime, services, tests. |
| 2026-06-13 | Initial draft — ritual (typed pool, offerings, weighted appearance, contract dialog, repeatable v0 gate), Beast Blood leveling, contractor level cap, cumulative level payload FAQ. |
