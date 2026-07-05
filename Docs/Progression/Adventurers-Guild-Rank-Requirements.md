# Adventurer's Guild — Rank & membership — Requirements

**Purpose:** Specify **personal guild rank** (tiers **9 → 1**, where **9 is lowest** and **1 is highest**), **essence-point eligibility**, **party aggregate rank**, and the **Guild Secretary** building + dialog flow on `DimensionSquareTest`. Design the **membership rank** concept so other organizations (Temple, Mage's Tower, Dwarven Clan, …) can reuse the same architecture without storing rank on `PlayerController` or `CharacterStats`.

**Status:** **Implemented** (v0).

**Depends on:** `EssenceData`, `EssenceSlotManager`, `PartyManager`, `BaseActor`, `NpcController`, `NpcDialogBoxUI`, `NpcTalkInteraction`, [NPC dialog](../World/NPC-Dialog-Requirements.md), [Town building entry & exit](../World/Town-Building-Entry-And-Exit-Requirements.md), [Town hub multi-floor](../World/Town-Hub-Multi-Floor-Requirements.md), [Enemy essence drops](../Essence/Enemy-Essence-Drops-Requirements.md) (tier **1 = highest**, **9 = lowest**), existing **Adventure Guild Exchange** building pack (`AdventureGuildExchangeLayout`, `AdventureGuildExchangePackCreator`).

**Related:** [Dwarf — Clan & Hall of Ancestors](../RacialSystem/Dwarf-Clan-And-Hall-Of-Ancestors-Requirements.md) (organization-specific membership on actors — parallel pattern). [Shop NPCs](../World/Shop-NPC-Requirements.md) (east-side Exchange clerk). [Party experience & leveling](Party-Experience-And-Leveling-Requirements.md) (distinct progression axis).

**Explicitly out of scope (v0):** Rank **decrease**; guild rank affecting combat/stats/shop prices; save/load across game sessions; non-adventurer playable characters; equipment contributing to essence score; information-selling shop UI; party-wide rank-up in one ceremony; rank display in character sheet UI (future hook only).

---

## Locked decisions (v0)

| # | Decision |
|---|----------|
| **L1** | Guild rank is an integer **9 … 1**. **9** = novice / lowest standing; **1** = highest standing. Lower number = higher rank. |
| **L2** | A member **ranks up** one tier at a time (9→8→7→…→1). **No rank decrease** in v0. |
| **L3** | Rank-up eligibility is based on **essence points** — the member's current score must be **≥ the threshold for the target rank** (§4). |
| **L4** | Essence points (v0) = **Σ (10 − essence tier)** over all essences equipped on that actor (`EssenceSlotManager`). Formula is **pluggable** for future equipment contributors (§5). |
| **L5** | Rank thresholds are **multiples of 3**, starting at **0** for tier 9: tier **R** requires **(9 − R) × 3** essence points (§4.2). |
| **L6** | **Party guild rank** = **⌊ mean(member ranks) ⌋** over **guild-member** party members only (§6). |
| **L7** | Guild membership + rank data lives on a **dedicated actor component**, **not** `PlayerController`, **not** `CharacterStats` (§7). |
| **L8** | v0: **every party member is a guild member** starting at **rank 9**. Characters without guild membership are supported by the data model but not authored in v0 (§7.3). |
| **L9** | Rank-up happens at the **Guild Secretary** NPC in a **new west-side building** on `dimension_square` — same exterior size and interior layout as the existing **Adventure Guild Exchange** (east) (§9). |
| **L10** | Secretary dialog is **code-driven** (custom dialog session), not a static `NpcDialogProfile` graph — extensible for future services (§10). |
| **L11** | **Rank up** menu option is **disabled** when **no** party member can rank up. Eligible members only appear as choices (§10). |
| **L12** | One dialog session may perform **multiple rank-ups** for the **same** member while they remain eligible (§10.4). |
| **L13** | Rank-up is **free**, **instant**, **safe-zone only**, and costs **no turn** (align with shop / shrine NPCs). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Personal guild rank** — each guild member tracks tier **9–1** independently of character level and combat stats. |
| **G2** | **Essence-driven promotion** — collecting higher-tier essences (or more essences) increases essence points and unlocks rank-ups. |
| **G3** | **Party aggregate** — derive a single **party guild rank** from member ranks for future content gates (quests, dungeon tiers, dialog). |
| **G4** | **Physical place** — west-side guild hall on Dimension Square; secretary NPC handles rank-up. |
| **G5** | **Honest UX** — disabled rank-up when nobody qualifies; only eligible members listed; multi-step promotion in one visit. |
| **G6** | **Modular membership** — same architecture supports Temple rank, Mage's Tower standing, Dwarven Clan rank, etc., with per-org eligibility rules. |
| **G7** | **Future-ready** — essence score formula and secretary services (information sales) extend without rewriting saves or actor identity. |
| **G8** | **Separation of concerns** — guild rank is organizational standing, not a core stat or controller concern. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Organization** | A world institution that may grant **membership** and **rank** to some actors (Adventurer's Guild, Temple, Mage's Tower, …). |
| **Guild rank** | An Adventurer's Guild member's personal tier **9–1** (9 = lowest). |
| **Target rank** | The rank a member would attain on the **next** successful rank-up (current − 1, floored at 1). |
| **Essence tier** | `EssenceData.tier` in range **1–9** (1 = highest quality; matches existing essence docs). |
| **Essence points** | Non-negative integer score from equipped essences (and future contributors) used for guild rank gates. |
| **Rank threshold** | Minimum essence points required to **hold** a given guild rank tier. |
| **Guild member** | Actor with an Adventurer's Guild entry in `OrganizationMembershipRuntime`. |
| **Party guild rank** | Floor of the arithmetic mean of guild ranks across guild-member party members. |
| **Guild Secretary** | Town NPC in the new west building; rank-up authority in v0; future hub for guild services. |
| **Adventure Guild Exchange** | Existing **east** building (`town_interior_adventure_guild_exchange`) — mana-stone selling; **not** rank-up. |
| **Adventure Guild Hall** | New **west** building (`town_interior_adventure_guild_hall`) — secretary + rank-up (§9). |

**Contrast with other progression:**

| System | Axis | Stored on |
|--------|------|-----------|
| Character level | Combat XP | `CharacterStats` |
| Dwarf clan member rank | Clan tree learning | `DwarfClanMembershipRuntime` |
| **Guild rank** | Essence portfolio | `OrganizationMembershipRuntime` |
| Party guild rank | Derived aggregate | `OrganizationRankService` (computed) |

---

## 3. Conceptual model

```text
OrganizationDefinition (×N)                    ← ScriptableObject per institution
├── organizationId   e.g. "adventurers_guild"
├── displayName
├── rankMin / rankMax   (guild: 9 .. 1, lower = better)
├── rankThresholds[]    (essence points per rank tier)
└── rankUpPolicy        (eligibility rules; guild uses essence score)

Actor (party member / NPC)
└── OrganizationMembershipRuntime              ← MonoBehaviour, NOT CharacterStats
      └── memberships[]: { orgId, rank, isMember }

OrganizationRankScoreService                     ← static / DDOL helper
├── CalculateScore(orgId, actor) → int
└── contributors: essence slots (v0), equipment (future)

OrganizationRankService                        ← rank-up + party aggregate
├── CanRankUp(orgId, actor) → bool + target rank
├── TryRankUp(orgId, actor) → bool
└── GetPartyRank(orgId, party) → int

Town
├── dimension_square — west facade + portal → town_interior_adventure_guild_hall
└── Guild Secretary NPC — AdventurersGuildSecretaryDialogSession
```

**Why not `CharacterStats`?** Guild standing is **institutional**, optional per character, and will coexist with other org ranks that **not every class can hold** (e.g. a Mage has no Temple priest rank). `CharacterStats` holds intrinsic body/progression; org standing is affiliation metadata on the actor.

**Why not `PlayerController`?** Controllers are input/behaviour; membership must live on **any** `BaseActor` (party members, future recruitable allies, test NPCs) and survive controller swaps.

---

## 4. Guild rank tiers & thresholds

### 4.1 — Rank scale

| Guild rank | Standing (flavor) | Essence-point threshold |
|------------|-------------------|-------------------------|
| **9** | Unrated novice | **0** |
| **8** | | **3** |
| **7** | | **6** |
| **6** | | **9** |
| **5** | | **12** |
| **4** | | **15** |
| **3** | | **18** |
| **2** | | **21** |
| **1** | Master adventurer | **24** |

**Formula:** `threshold(rank) = (9 − rank) × 3` for rank ∈ [1, 9].

### 4.2 — Rank-up rule

A member at **currentRank** may rank up **iff**:

1. `currentRank > 1` (not already max rank), and  
2. `essencePoints(actor) ≥ threshold(currentRank − 1)` — i.e. they meet the requirement for the **target** rank.

**Example:** Member at rank **9** with essence points **0** cannot rank up (needs ≥ 3 for rank 8). With **3** points (e.g. three tier-9 essences), they may rank up to **8**. After ranking to **8**, they may rank again immediately in the same dialog if points still meet the rank **7** threshold (≥ 6).

### 4.3 — Max rank

Rank **1** is terminal for v0. Secretary dialog shows a **congratulatory / max-rank** line when a member is already rank 1 and selects them (no rank-up action).

---

## 5. Essence points (v0 formula)

### 5.1 — Calculation

```
essencePoints(actor) = Σ over equipped essences e of (10 − e.tier)
```

| Equipped essence tier | Points per essence |
|-----------------------|--------------------|
| 9 (lowest) | 1 |
| 8 | 2 |
| … | … |
| 1 (highest) | 9 |

- Empty slots contribute **0**.
- Duplicate essences in different slots each count (rare in v0; rule is explicit).
- Score is computed **at rank-up evaluation time** from live equipment — no cached score in v0.

### 5.2 — Examples

| Loadout | Points | Can reach rank (from 9) |
|---------|--------|-------------------------|
| 3 × tier-9 essences | 3 | **8** (not 7 — needs 6) |
| 1 × tier-7 essence | 3 | **8** |
| 1 × tier-5 essence | 5 | **8** |
| 1 × tier-1 essence | 9 | **6** (threshold 9) |
| 3 × tier-1 essences | 27 | **1** (all thresholds met) |

### 5.3 — Extensibility (future equipment)

Introduce `IOrganizationRankScoreContributor` (or per-org delegate on `OrganizationDefinition`):

```csharp
// Illustrative — not prescriptive API
public interface IOrganizationRankScoreContributor
{
    string OrganizationId { get; }
    int Contribute(BaseActor actor);
}
```

- v0 registers one contributor: **`EssenceSlotScoreContributor`** (`adventurers_guild`).
- Future: `EquippedItemScoreContributor` reads tagged equipment without changing rank thresholds or dialog flow.
- **Changing the formula must not require migrating guild rank** — only eligibility recalculates.

---

## 6. Party guild rank

### 6.1 — Definition

```
partyGuildRank = floor( mean( rank(m) ) ) for all party members m where m is a guild member
```

- **Guild members only** — if a future non-member is in the party, they are **excluded** from the mean (not counted as rank 9).
- If **zero** guild members are in the party, `partyGuildRank` is **undefined** (return sentinel or 0 per caller; document at call site — v0 always has ≥1 member).

### 6.2 — Examples

| Member ranks | Mean | Party guild rank |
|--------------|------|------------------|
| 9, 9, 9, 9 | 9.0 | **9** |
| 9, 8, 7, 6 | 7.5 | **7** |
| 9, 9, 8, 7 | 8.25 | **8** |
| 1, 9, 9, 9 | 7.0 | **7** |

### 6.3 — v0 usage

- **Computed on demand** via `OrganizationRankService.GetPartyRank("adventurers_guild", party)`.
- **No UI requirement** in v0; hook for quests, dialog templates (`{partyGuildRank}`), and dungeon gates later.

---

## 7. Data model — modular organization membership

### 7.1 — `OrganizationDefinition` (ScriptableObject)

One asset per institution. Example: `Assets/Data/Organizations/Organization_AdventurersGuild.asset`.

| Field | Type | Guild v0 value |
|-------|------|----------------|
| `organizationId` | string | `adventurers_guild` |
| `displayName` | string | `Adventurer's Guild` |
| `rankBest` | int | **1** (highest standing) |
| `rankWorst` | int | **9** (lowest standing) |
| `rankThresholds` | `int[]` indexed by rank | `[0,3,6,9,12,15,18,21,24]` for ranks 9→1 |
| `defaultStartingRank` | int | **9** |
| `allowsRankDecrease` | bool | **false** |

**Index convention:** `rankThresholds[k]` gives the threshold for rank tier `k`, where `k` runs from `rankWorst` down to `rankBest`. Implementation may use a dictionary or parallel arrays — must be **data-driven**, not hard-coded in dialog.

### 7.2 — `OrganizationMembershipRuntime` (MonoBehaviour on actor)

Parallel to `DwarfClanMembershipRuntime`, but **multi-org**:

```csharp
// Illustrative shape
[Serializable]
struct OrganizationMembershipRecord
{
    public string organizationId;
    public int rank;           // guild: 9..1
    public bool isActiveMember;
}

public sealed class OrganizationMembershipRuntime : MonoBehaviour
{
    [SerializeField] OrganizationMembershipRecord[] memberships;
    // GetRank(orgId), SetRank(orgId, rank), IsMember(orgId), EnsureMembership(orgId, startRank), ...
}
```

| Rule | Detail |
|------|--------|
| **Location** | On `BaseActor` prefabs (party members), **not** on `CharacterStats` or `PlayerController`. |
| **Optional membership** | Absence of a record ⇒ **not a member** of that org. |
| **Serialization** | Records serialize with actor / run persistence (exact save blob TBD). |
| **Initialization** | v0 party prefabs include `isActiveMember: true`, `rank: 9` for `adventurers_guild`. |

### 7.3 — Relationship to Dwarf clan membership

| | `DwarfClanMembershipRuntime` | `OrganizationMembershipRuntime` |
|--|------------------------------|----------------------------------|
| Scope | Dwarves only, one clan | Any org, multiple entries per actor |
| Rank meaning | Clan personal standing | Org-specific (guild 9–1, temple TBD) |
| v0 coexistence | **Separate components** — do not merge in v0 | Guild rank on all humans in party; dwarf clan unchanged |

**Future consolidation** (out of v0): a single `ActorAffiliations` facade could wrap both; not required for guild shipping.

### 7.4 — Organization IDs (reserved)

| `organizationId` | Notes |
|------------------|--------|
| `adventurers_guild` | This doc (v0) |
| `temple` | Future; priests only |
| `mages_tower` | Future; mages only |
| `dwarven_clan` | May mirror or reference clan id — TBD when unified |

---

## 8. Services & logic

### 8.1 — `OrganizationRankScoreService`

| Method | Behaviour |
|--------|-----------|
| `GetScore(organizationId, actor)` | Sum all registered contributors for that org. |
| `RegisterContributor(...)` | v0: essence contributor for guild. |

### 8.2 — `OrganizationRankService`

| Method | Behaviour |
|--------|-----------|
| `GetRank(organizationId, actor)` | Returns rank if member; else null / sentinel. |
| `CanRankUp(organizationId, actor, out int targetRank, out string denyReason)` | Checks membership, not max rank, essence threshold. |
| `TryRankUp(organizationId, actor)` | If `CanRankUp`, decrement rank by 1 (9→8), fire event, return true. |
| `GetEligibleRankUpMembers(organizationId, party)` | Party members where `CanRankUp` is true. |
| `GetPartyRank(organizationId, party)` | §6 aggregate. |

### 8.3 — Events (optional v0)

- `OrganizationRankChanged` — `(organizationId, actor, oldRank, newRank)` for log/UI hooks.

---

## 9. World placement — Adventure Guild Hall (west)

### 9.1 — District context

`dimension_square` already has the **Adventure Guild Exchange** on the **east** (`AdventureGuildExchangeLayout`: 5×5 exterior at origin **(29, 19)**, door **(31, 19)**). v0 adds a **mirror building on the west**.

### 9.2 — Exterior (locked for v0)

| Constant | Value | Notes |
|----------|-------|--------|
| `ExteriorWidth` × `ExteriorDepth` | **5 × 5** | Same as Exchange |
| `ExteriorOriginX` | **7** | Mirror of east origin 29 about center 20 |
| `ExteriorOriginY` | **19** | Same row as Exchange |
| `ExteriorDoorCell` | **(9, 19, 0)** | West door threshold |
| `EnterLinkId` | `building_adventure_guild_hall_enter` | |
| `ExitLinkId` | `building_adventure_guild_hall_exit` | |
| `InteriorFloorId` | `town_interior_adventure_guild_hall` | |

**Facade:** Reuse east building tile grammar (`AdventureGuildExchangePackCreator` patterns) with west-facing door art; new `FacadeOverlay` asset under `DistrictTest/Building/AdventureGuildHall/`.

### 9.3 — Interior (locked for v0)

**Same dimensions and layout as Exchange:**

| Constant | Value |
|----------|-------|
| `InteriorWidth` × `InteriorHeight` | **8 × 10** |
| `CounterRowY` | **5** |
| `CustomerRowY` | **4** |
| `ClerkRowY` / secretary row | **6** |
| `InteriorArrivalCell` | **(4, 4, 0)** |
| `InteriorExitCell` | **(4, 0, 0)** |
| `SecretaryNpcCell` | **(4, 6, 0)** |

Paint via scene-painted floor (`FloorLayoutMode.ScenePainted`) and `AdventureGuildHallPackCreator` editor menu (parallel to Exchange pack).

### 9.4 — Asset folder

```
Assets/Resources/Town/DistrictTest/Building/AdventureGuildHall/
├── Floor_town_interior_adventure_guild_hall.asset
├── FacadeOverlay_town_interior_adventure_guild_hall.asset
├── FacadeOverlay_dimension_square_west_guild_hall.asset   (district overlay cells)
└── PartyFormation_ShopInterior.asset   (reuse Exchange formation profile or duplicate)
```

### 9.5 — Portal wiring

Add to `Floor_dimension_square.asset` (and DistrictTest twin):

- `portalLinkId: building_adventure_guild_hall_enter`
- `targetFloorId: town_interior_adventure_guild_hall`
- `portalCell: (9, 19, 0)`
- `listLabel: Adventurer's Guild Hall`

Interior exit portal returns to `dimension_square` at west door arrival binding (reciprocal link pair per [Town building entry & exit](../World/Town-Building-Entry-And-Exit-Requirements.md)).

### 9.6 — Scene integration

- `DimensionSquareSceneCreator` — register interior floor, paint west facade, integrate like Exchange.
- `TownInteriorNpcSetupPhase` — spawn secretary at `adventure_guild_secretary` marker.
- `DungeonFloorInstanceManager` — safe-zone + camera band parity with Exchange interior.

---

## 10. Guild Secretary — NPC & dialog

### 10.1 — NPC identity

| Field | Value |
|-------|-------|
| Prefab | `TownNpc_AdventureGuildSecretary.prefab` |
| `npcId` | `adventure_guild_secretary` |
| Display name | **Guild Secretary** (name TBD in copy pass) |
| Controller | `AdventurersGuildSecretaryNpcController` extends `NpcController` |
| Dialog | **`AdventurersGuildSecretaryDialogSession`** (code-driven) |
| Counter talk | `NpcCounterTalkBinding` — same customer/counter rows as Exchange clerk |

**Extensibility:** Controller holds references to `OrganizationDefinition` (guild) and optional future service modules (`IGuildSecretaryService`). v0 implements `RankUpService` only; `InformationBrokerService` is a stub or backlog comment.

### 10.2 — Interaction model

Same as other town NPCs ([NPC dialog §3](../World/NPC-Dialog-Requirements.md)):

- **Enter** while orthogonally adjacent + facing secretary (active party leader).
- **No turn** cost.
- Safe zone only.

### 10.3 — Dialog flow (v0)

```
Start
  → Greeting line (parameterized: {partyName}, {partyGuildRank} optional)
  → Main menu [Choice]
       ├── "Rank up"     enabled iff GetEligibleRankUpMembers(guild).Count > 0
       ├── (future) "Buy information"   disabled or hidden in v0
       └── "Leave"
  → [Rank up selected]
       → "Who will register for promotion?"
       → Member choice list: ONLY eligible members (enabled rows)
            label example: "{displayName}  (rank {current} → {target}, {points}/{threshold} EP)"
       → [Member selected]
            → TryRankUp → success line: "… promoted to rank {newRank} …"
            → If CanRankUp still true for same member:
                 → Choice: "Promote again" / "Promote someone else" / "Done"
            → Else return to main menu or Done
  → Complete
```

### 10.4 — Multi rank-up in one session

After each successful `TryRankUp`, re-evaluate `CanRankUp` for that member. If still eligible, offer **"Promote again"** without closing dialog. Loop until player backs out or member reaches max rank / falls below threshold.

### 10.5 — Disabled states

| State | UX |
|-------|-----|
| No member can rank up | **Rank up** choice visible but **disabled** (greyed); hint in prompt: *"No one meets the essence requirements yet."* |
| Selected member no longer eligible (edge: essence unequipped mid-dialog — unlikely) | Show denial line; return to menu. |
| Member at rank 1 | Omit from rank-up list; if inspected, max-rank flavor line. |
| Non-guild member (future) | Omit from list; optional *"not a guild member"* if forced via debug. |

### 10.6 — Implementation pattern

Follow **`DwarfClanStewardDialogSession`** / **`HumanPriestShrineDialogSession`**:

- Build `DialogChoiceStep` lists at runtime.
- Use `DialogChoiceOptionData.payload` string tokens (`__rank_up__`, `__member_{actorId}__`, …).
- Keep static flavor lines in `NpcDialogProfile` **optional** — secretary may use zero graph assets in v0.

### 10.7 — Future secretary services

| Service | Hook |
|---------|------|
| **Information broker** | New menu row → `InformationBrokerService.ShowCatalog(party)` |
| **Quest posting** | Dialog action or separate board NPC |
| **Guild ID card** | Line showing `{rank}` + portrait |

Add rows in `ShowMainMenu()` without changing rank-up logic.

---

## 11. Initialization & content (v0)

| Item | Requirement |
|------|-------------|
| Party prefabs | `OrganizationMembershipRuntime` with guild member rank **9** |
| Organization asset | `Organization_AdventurersGuild.asset` with thresholds §4.1 |
| Dimension Square | West hall facade + portal §9 |
| Interior | Painted 8×10 hall + secretary spawn |
| Tests | Unit tests for threshold math, rank-up eligibility, party average, multi-promotion loop |

---

## 12. Acceptance criteria (v0)

| ID | Criterion |
|----|-----------|
| **A1** | Party member with essence points ≥ 3 can rank from **9 → 8** at secretary. |
| **A2** | Member with insufficient points does **not** appear in rank-up member list. |
| **A3** | **Rank up** is disabled when no member is eligible. |
| **A4** | Member with points for multiple tiers can rank **9 → 8 → 7** in one dialog session. |
| **A5** | Rank **never decreases** after equipment changes. |
| **A6** | `GetPartyRank` returns **8** for ranks [9,9,8,7]. |
| **A7** | Guild rank is **not** stored on `CharacterStats` or `PlayerController`. |
| **A8** | West building is enterable from `dimension_square`; interior matches Exchange layout dimensions. |
| **A9** | East Exchange shop remains unchanged. |

---

## 13. Test plan (unit)

| Test | Assert |
|------|--------|
| `threshold(9)==0`, `threshold(1)==24` | Table §4.1 |
| 3× tier-9 essences ⇒ 3 points | §5.1 |
| Rank 9 + 3 points ⇒ can target rank 8 | §4.2 |
| Rank 9 + 2 points ⇒ cannot rank up | §4.2 |
| After rank-up to 8, 3 points ⇒ cannot rank to 7 (needs 6) | §4.2 |
| Party ranks [9,9,8,7] ⇒ party rank 8 | §6 |
| `TryRankUp` at rank 1 fails | §4.3 |
| Non-member excluded from party average | §6.1 |

---

## 14. Implementation checklist (engineering)

1. `OrganizationDefinition` + `Organization_AdventurersGuild` asset  
2. `OrganizationMembershipRuntime` on party prefabs  
3. `EssenceSlotScoreContributor` + `OrganizationRankScoreService`  
4. `OrganizationRankService` (eligibility, rank-up, party aggregate)  
5. `AdventureGuildHallLayout` constants + `AdventureGuildHallPackCreator`  
6. West facade + portals on `dimension_square`  
7. `AdventurersGuildSecretaryNpcController` + `AdventurersGuildSecretaryDialogSession`  
8. `TownInteriorNpcSetupPhase` registration  
9. Unit tests §13  
10. `DimensionSquareSceneCreator` / Fix menu integration  

---

## 15. Open questions (post-v0)

| # | Question |
|---|----------|
| **Q1** | Should **party guild rank** display in HUD or character sheet? |
| **Q2** | Do NPC hirelings / temporary allies count toward party average? |
| **Q3** | Information broker — gold cost, story flags, or dungeon intel items? |
| **Q4** | Unify `DwarfClanMembershipRuntime` into `OrganizationMembershipRuntime`? |
| **Q5** | Rename ranks with flavor titles (Copper, Silver, Gold, …) per tier? |

---

## 16. Reference — essence tier alignment

Per [Enemy essence drops](../Essence/Enemy-Essence-Drops-Requirements.md): **`EssenceData.tier`** uses **1 = highest**, **9 = lowest**. The guild formula **`(10 − tier)`** rewards higher-tier essences linearly (tier 1 ⇒ 9 points). Changing essence tier on an asset immediately affects eligibility — no separate "guild essence register."
