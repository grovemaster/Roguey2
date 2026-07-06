# Party member recruitment — Requirements

**Purpose:** Let the player **recruit adventurers into the party** at the **Adventurer's Guild Hall** (Guild Secretary NPC), gated by **party guild rank**, **party capacity**, and a **gold cost**. v0 uses a **static recruit roster**; later versions refresh recruits on a **monthly** cadence with varied loadouts.

**Status:** **Not implemented** (requirements only).

**Depends on:** `PartyManager`, `PartySpawnService`, `PartyFormationSpawnProfile`, `OrganizationRankService`, `OrganizationMembershipRuntime`, `EssenceSlotManager`, `EssenceData`, `ShopGoldUtility`, `PartyCurrencyLedger`, `AdventurersGuildSecretaryDialogSession`, `AdventurersGuildSecretaryNpcController`, [Adventurer's Guild rank & membership](../Progression/Adventurers-Guild-Rank-Requirements.md), [NPC dialog](../World/NPC-Dialog-Requirements.md), [Town building entry & exit](../World/Town-Building-Entry-And-Exit-Requirements.md), [Party member death](Party-Member-Death-Requirements.md), race player prefabs (`PartyCompositionPresets` paths).

**Related:** [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md). [Town time & calendar](../World/Town-Time-And-Calendar-Requirements.md) (future monthly recruit refresh). `PartyCompositionSwapService` / editor **Party Composition** menu (dev roster swap — **not** player recruitment). [Dungeon floor 1 production](../World/Dungeon-Floor-1-Production-Requirements.md) (Goblin / Ghoul / Dire Wolf essences).

**Explicitly out of scope (v0):** Dismissing / releasing party members; recruiting outside the guild hall; recruitable NPCs with custom dialog trees; save/load of recruit board across sessions; recruit preview character sheet; naming recruits; recruits with non-default classes beyond race prefab defaults; equipment on recruits (v0 roster has none); non-gold recruitment costs; recruiting while party is in a dungeon; **dead-member memorial** interactions.

---

## Locked decisions (v0)

| # | Decision |
|---|----------|
| **L1** | Recruitment is offered by the **Guild Secretary** as a new main-menu dialog option: **"Recruit party member"**. |
| **L2** | Recruitment is **safe-zone only**, **instant**, and costs **no turn** (same rules as rank-up and shop NPCs). |
| **L3** | **Party capacity** is a **global, adjustable integer** on a run-scoped service. **Default = 5** at new-run start. A future story event may raise it to **6** via an explicit API — v0 must not hard-code `5` at every call site. |
| **L4** | A recruit is **eligible** only when **party guild rank standing ≥ recruit guild rank standing** (§5). |
| **L5** | v0 recruit roster is a **static authored list** (§6). Once recruited, a recruit **leaves the board for the rest of the run** (no duplicates). |
| **L6** | Recruitment cost (v0) = **`1` gold + `1` gold × (count of essences on the recruit)`** (§7). Paid from the **party wallet** (`ShopGoldUtility` / `PartyCurrencyLedger`). |
| **L7** | Recruited actors join `PartyManager.partyMembers` as **non–main-character** members with default guild membership at their **authored guild rank** (§8). |
| **L8** | Recruitment is **blocked** when **living party member count ≥ `MaxPartyMembers`** (§4). |
| **L9** | Rank gate uses **party guild rank** from `OrganizationRankService.GetPartyRank` (mean of member ranks, floored) — same aggregate as the secretary greeting. |
| **L10** | Cost and eligibility formulas are **pluggable** so monthly refresh, item requirements, and alternate currencies can be added without rewriting dialog. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Player-facing recruitment** — grow the party through gameplay instead of only the dev **Party Composition** editor menu. |
| **G2** | **Capacity progression** — start with max **5** members; support a later story unlock to **6** without refactoring recruitment or formation code. |
| **G3** | **Guild rank gate** — higher-standing recruits require a party whose **aggregate guild rank** is at least as good. |
| **G4** | **Honest UX** — show rank requirement, cost, and why each recruit is disabled; confirm before spending gold. |
| **G5** | **Faithful v0 roster** — five static recruits (three rank-9, two rank-8) with correct races, essences, and costs (§6). |
| **G6** | **Future monthly board** — data model and services accept a **refreshable recruit catalog** (equipment, items, essences, varied ranks) without changing the secretary dialog shell. |
| **G7** | **Consistent party integration** — new members spawn in **formation** in town, receive guild membership, and appear on the party HUD. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Party capacity** | Maximum **living** party members allowed (`MaxPartyMembers`). |
| **Living member** | `BaseActor` in `partyMembers` with `currentHP > 0` (after death pipeline, destroyed members are not counted). |
| **Recruit** | A catalog entry describing a potential party member (race, guild rank, essences, cost inputs). |
| **Recruit board** | The set of recruits **currently available** at the guild (static list in v0; refreshed monthly later). |
| **Guild rank (personal)** | Member tier **9 … 1** on `OrganizationMembershipRuntime` (9 = lowest standing). |
| **Party guild rank** | `OrganizationRankService.GetPartyRank` — floor of mean personal guild ranks over guild-member party members. |
| **Rank standing** | Qualitative “how good” the rank is: **rank 8 standing is better than rank 9**; **rank 1 is best**. |
| **Recruitment cost** | Gold debited from the party wallet on successful recruit (v0 formula §7). |
| **Dev party preset** | Editor / dev-build `PartyCompositionSwapService` — replaces entire roster; **orthogonal** to recruitment. |

**Rank comparison (locked):**

> Party may recruit an adventurer with personal guild rank **R** iff  
> **`partyGuildRank` ≤ `R`** (numeric compare on tier integers, where **smaller = better standing**).

Examples:

| Party guild rank | Recruit rank | Eligible? |
|----------------|--------------|-----------|
| 9 | 9 | **Yes** (equal standing) |
| 9 | 8 | **No** (party too low) |
| 8 | 8 | **Yes** |
| 7 | 8 | **Yes** (party standing better than required) |

---

## 3. Conceptual model

```text
PartyCapacityService (DDOL on PartyManager)     ← run-scoped max roster size
├── MaxPartyMembers : int   (default 5)
├── GetLivingMemberCount(party) → int
├── CanAddMember(party) → bool
└── SetMaxPartyMembers(int)   ← story event / debug hook (e.g. 5 → 6)

PartyRecruitCatalog (v0: static data)           ← later: ScriptableObject + monthly refresh
├── entries[]: PartyRecruitDefinition
│     ├── recruitId
│     ├── displayName / race prefab
│     ├── guildRank (personal, at join)
│     ├── essenceLoadout[] (optional)
│     ├── equipmentLoadout[] (v0: empty)
│     └── flavor (optional)
└── GetAvailable(boardState) → filtered list

PartyRecruitmentService
├── GetEligibleRecruits(party, boardState) → list + deny reasons
├── GetRecruitCost(recruit) → int   (v0: essence-based gold)
├── TryRecruit(party, recruitId) → bool + message
│     ├── validate capacity, rank, gold, still on board
│     ├── spend gold
│     ├── instantiate actor + apply loadout + guild membership
│     ├── append to partyMembers
│     ├── mark recruitId recruited on board state
│     └── spawn in formation (town safe zone)
└── IRecruitCostCalculator (v0 impl: EssenceCountGoldCalculator)

PartyRecruitBoardState (run-scoped)
└── recruitedIds : HashSet<string>   (v0: never respawn same id in one run)

AdventurersGuildSecretaryDialogSession
└── Main menu + "Recruit party member" branch (§9)

Story / future
└── PartyCapacityService.SetMaxPartyMembers(6) on authored story beat
```

**Why separate capacity from formation `maxSlots`?** `PartyFormationSpawnProfile` already supports offsets for **1–6** members ([Dynamic dungeon generation §R6.5](../World/Dynamic-Dungeon-Floor-Generation-Requirements.md)). **Capacity** is a **gameplay limit** (when recruitment stops); formation profiles are **layout**. Raising capacity to 6 must not require new formation art — reuse existing 6-slot profiles.

---

## 4. Party capacity

### 4.1 — Service

Introduce **`PartyCapacityService`** (or equivalent name) as a **run-scoped** component on the same DDOL object as `PartyManager` (mirror `PartyCurrencyLedger` pattern).

| API | Behavior |
|-----|----------|
| `int MaxPartyMembers { get; }` | Current cap; **default 5** on new run. |
| `void SetMaxPartyMembers(int value)` | Clamps to **`[1, 6]`** for v0 (formation ceiling). Fires change event for UI. |
| `int GetLivingMemberCount(PartyManager party)` | Count non-null `partyMembers` with `HP > 0`. |
| `bool CanAddMember(PartyManager party)` | `GetLivingMemberCount < MaxPartyMembers`. |

### 4.2 — Default and story unlock

| Milestone | `MaxPartyMembers` |
|-----------|-------------------|
| New run start | **5** |
| Future story event (not authored in v0) | **6** via `SetMaxPartyMembers(6)` |

**Implementation note:** Story hook is a **single call site** (flag listener, quest completion, or cutscene command). Do **not** scatter literal `5` / `6` in recruitment UI — always read `PartyCapacityService`.

### 4.3 — Interaction with death

When a member dies and is removed from `partyMembers` ([Party member death](Party-Member-Death-Requirements.md)), **living count decreases** — recruitment may become available again without changing `MaxPartyMembers`.

### 4.4 — Main character

The **main character** counts toward capacity. Recruits never become main character unless a future system explicitly promotes them.

---

## 5. Rank eligibility

### 5.1 — Rule

```text
eligibleRank(recruit) :=
  OrganizationRankService.GetPartyRank(adventurers_guild, party) <= recruit.authoredGuildRank
```

- Uses **current** party roster at dialog evaluation time (after any rank-ups in the same visit).
- Recruit's **authored guild rank** is the rank they join with (§8) — not a separate “required rank” field unless data-driven catalogs need both later.

### 5.2 — Disabled UX

When a recruit fails the rank gate, list row is **visible but disabled** with hint:

> *Requires party guild rank {recruitRank} or better (yours: {partyRank}).*

Use the same numeric rank labels as the secretary greeting (§9).

### 5.3 — Party rank edge cases

| Case | Behavior |
|------|----------|
| Zero guild members (data error) | Treat as ineligible for rank-8 recruits; log warning. v0 rosters always include ≥1 guild member. |
| Rank-up during same dialog session | Re-filter recruit list when returning to recruit menu (party rank may have improved). |

---

## 6. v0 static recruit roster

Five entries. All use existing race **player prefabs** (`HumanPlayer`, `ElfPlayer`, `BarbarianPlayer`). **No equipment.** Display names may be generic ("Human Adventurer") or authored per entry.

### 6.1 — Rank 9 recruits (1 gold each)

No essences. Personal guild rank **9** at join. Party rank **9** required.

| `recruitId` | Race prefab | Essences | Guild rank at join | Cost |
|-------------|-------------|----------|-------------------|------|
| `guild_recruit_09_human` | `HumanPlayer.prefab` | *(none)* | **9** | **1** gold |
| `guild_recruit_09_elf` | `ElfPlayer.prefab` | *(none)* | **9** | **1** gold |
| `guild_recruit_09_barbarian` | `BarbarianPlayer.prefab` | *(none)* | **9** | **1** gold |

### 6.2 — Rank 8 recruits (4 gold each)

Three **tier-9** essences (floor-1 monster drops). Essence points = **3** (= rank-8 threshold per [guild rank doc §4](../Progression/Adventurers-Guild-Rank-Requirements.md)). Personal guild rank **8** at join (experienced recruits — **not** rank 9 with essences only for display).

| `recruitId` | Race prefab | Essences (equipped on join) | Guild rank at join | Cost |
|-------------|-------------|----------------------------|-------------------|------|
| `guild_recruit_08_human` | `HumanPlayer.prefab` | Goblin, Ghoul, Dire Wolf | **8** | **4** gold |
| `guild_recruit_08_elf` | `ElfPlayer.prefab` | Goblin, Ghoul, Dire Wolf | **8** | **4** gold |

**Essence asset paths (production):**

- `Resources/Item/Essence/Production/GoblinEssence`
- `Resources/Item/Essence/Production/GhoulEssence`
- `Resources/Item/Essence/Production/DireWolfEssence`

Equip into `EssenceSlotManager` slots **0, 1, 2** at instantiation (before `Apply` / stat hooks run, or via same path as prefab-serialized essences).

### 6.3 — Board persistence (v0)

- **`PartyRecruitBoardState`** (run-scoped) tracks `recruitedIds`.
- After successful recruit, id is added; entry **never reappears** until a future monthly refresh clears or replaces the board.
- v0: **no** monthly refresh — board only shrinks.

### 6.4 — Future monthly refresh (design hook only)

Not implemented in v0. Document intended behavior for implementers:

| Aspect | Future behavior |
|--------|-----------------|
| **Trigger** | First town morning of each **calendar month** (requires month index on `TownTimeService` — see [Town time](../World/Town-Time-And-Calendar-Requirements.md) backlog). |
| **Catalog** | `PartyRecruitCatalog` ScriptableObject or procedural generator — varied races, ranks, essences, equipment, items. |
| **State** | Replace available entries; clear `recruitedIds` **or** merge per design (TBD — prefer fresh board with new ids). |
| **Cost** | Still via `IRecruitCostCalculator`; v0 gold+essence formula remains default fallback. |

v0 code should load recruits through **`PartyRecruitCatalog`** (hard-coded static list inside the class is acceptable) so monthly refresh swaps the catalog provider only.

---

## 7. Recruitment cost (v0)

### 7.1 — Formula

```text
cost(recruit) = baseGold + (perEssenceGold × essenceCountOnRecruit)

v0 constants:
  baseGold = 1
  perEssenceGold = 1
  essenceCountOnRecruit = number of essences in recruit definition (equipped at join, not party essences)
```

| Recruit tier | Essence count | Cost |
|--------------|---------------|------|
| Rank 9 (v0) | 0 | **1** |
| Rank 8 (v0) | 3 | **4** |

### 7.2 — Payment

- Debit via **`ShopGoldUtility.TrySpendPartyGold(cost)`** (same wallet as shops, inn, quests).
- If spend fails after validation, show error and **do not** spawn actor (transactional recruit).

### 7.3 — Extensibility

```csharp
// Illustrative — not prescriptive API
public interface IRecruitCostCalculator
{
    bool TryCalculate(PartyRecruitDefinition recruit, out int goldCost, out string summary);
}
```

- v0: single implementation (`EssenceCountGoldCalculator`).
- Future: composite calculator (gold + required items + mana stones); dialog shows multi-line cost summary.

### 7.4 — Insufficient gold UX

Disabled recruit row or confirmation denial:

> *Not enough gold. Need {cost}; you have {partyGold}.*

---

## 8. Actor creation on recruit

### 8.1 — Instantiation pipeline

On successful `TryRecruit`:

1. **Instantiate** race prefab under party container (same parent as `DungeonRunBootstrap` / `PartyCompositionSwapService`).
2. **`OrganizationMembershipRuntime.EnsureOn`** → `EnsureMembership(guild, startingRank: recruit.authoredGuildRank)`.
3. **Apply essence loadout** via `EssenceSlotManager.EquipEssence` (rank-8 recruits only in v0).
4. **Apply equipment loadout** — no-op in v0 (hook for future catalog).
5. **Set display name** from recruit definition (optional override of prefab default).
6. **Append** to `PartyManager.partyMembers`.
7. **Do not** assign `PartyMainCharacterMarker` / main character.
8. **Spawn in formation** at party's current anchor (`PartySpawnService.TrySpawnFormationAtAnchor` with living members + new member) — **town safe zone only** in v0.
9. **Wire services:** `InitializeRosterAfterDeferredSpawn`, mana stone auto-pickup subscribe, portal subscribe (mirror existing spawn paths).
10. **Mark** `recruitId` on board state.

### 8.2 — Guild rank vs essences

Rank-8 recruits join at **personal rank 8** with essences equipped. They are **not** rank 9 members who still need secretary rank-up to reach 8 — the essences explain **flavor and cost**, and match guild progression math.

### 8.3 — Class and stats

Use prefab defaults (`CharacterStats`, race, class). v0 does not offer class selection at recruit time.

---

## 9. Guild Secretary dialog (v0)

Extend **`AdventurersGuildSecretaryDialogSession`** (code-driven — same pattern as rank-up).

### 9.1 — Main menu

```
Start
  → Greeting (existing: party name, party guild rank)
  → Main menu [Choice]
       ├── "Rank up"              (existing)
       ├── "Recruit party member" (NEW)
       └── "Leave"
```

| State | **Recruit party member** |
|-------|--------------------------|
| `CanAddMember` false (at capacity) | **Disabled** — *"Your party is full ({count}/{max})."* |
| No recruits left on board | **Disabled** — *"No adventurers are seeking a party right now."* |
| Recruits exist but all rank-gated | **Enabled** — recruit submenu shows all rows disabled with rank hints |
| At least one eligible recruit | **Enabled** |

### 9.2 — Recruit submenu

```
→ "Who would you like to recruit?"
→ List [Choice] — one row per board entry not yet recruited
     label example:
       "{displayName}  (rank {recruitRank}, {cost} gold)"
     enabled per-row: rank eligible AND gold >= cost AND CanAddMember
     disabled hint appended in prompt or grey subtext:
       rank fail / gold fail / full party
→ [Recruit selected]
     → Confirm [Choice]: "Recruit {name} for {cost} gold?"  Yes / No
→ [Yes]
     → TryRecruit
     → Success: "{name} has joined your party."
     → Failure: denial line (gold, capacity, already recruited)
→ Post-success [Choice]: "Recruit another" / "Done"
     "Recruit another" only if CanAddMember AND eligible recruits remain
→ Done → Main menu or Complete (match rank-up back navigation)
```

### 9.3 — Interaction rules

- **Enter** adjacent + facing secretary (active party leader).
- **No turn** cost.
- **Safe zone only** — same guard as rank-up (town interior / safe floors).
- Dialog uses existing `NpcDialogBoxUI` / `DialogChoiceOptionData` payload tokens (`__recruit__`, `__recruit_{id}__`, …).

### 9.4 — Coexistence with rank-up

Player may rank up and recruit in **one visit**. After rank-up, party guild rank may increase — recruit list should **re-query** eligibility when re-entering recruit menu.

---

## 10. Party integration & HUD

| System | Behavior on recruit |
|--------|----------------------|
| `PartyControlHudUI` | New portrait appears when member added (existing party list binding). |
| Formation | `positionHistory` / `SnapHistoryToCurrentPositions` after spawn. |
| Active member index | Unchanged unless implementation shifts active leader (v0: **do not** change active index). |
| Inventory | New member has prefab-default inventory; no party inventory merge in v0. |
| XP | Per [party XP doc](../Progression/Party-Experience-And-Leveling-Requirements.md) — new member follows living-member XP rules from join onward. |

---

## 11. Dev tools & testing

| Tool | Purpose |
|------|---------|
| Editor/debug: `SetMaxPartyMembers(6)` | Verify story unlock path without story content. |
| Editor/debug: reset `PartyRecruitBoardState` | Re-test recruit flow in play mode. |
| Unit tests | `PartyRecruitmentService`: cost formula, rank gate (`9` vs `8`), capacity block, gold spend, board dedup. |
| Unit tests | `PartyCapacityService`: default 5, clamp, living count with null/dead members. |

**Regression:** `PartyCompositionSwapService` editor menu continues to **replace** the full roster for dev — it does not need to clear recruit board state unless testers want a clean slate (document as optional debug).

---

## 12. Acceptance criteria (v0)

| # | Criterion |
|---|-----------|
| **A1** | New run: `MaxPartyMembers == 5`; sixth living member cannot be recruited until capacity raised. |
| **A2** | `SetMaxPartyMembers(6)` allows a sixth recruit; formation spawn succeeds with six living members. |
| **A3** | Secretary main menu shows **Recruit party member**; flow matches §9. |
| **A4** | All five v0 recruits appear with correct race, rank, essences (rank-8 only), and costs **1** / **4** gold. |
| **A5** | Party guild rank **9** cannot recruit rank-8 entries; rank **8** or better can. |
| **A6** | Gold is deducted on success; recruit removed from board; cannot recruit same id twice in one run. |
| **A7** | Recruited actor is in `partyMembers`, has correct `OrganizationMembershipRuntime` rank, essences equipped (rank-8), and is placed in formation in town. |
| **A8** | Recruitment blocked at party capacity with clear message. |
| **A9** | Insufficient gold blocks confirm / shows denial without spawning actor. |

---

## 13. Open questions (non-blocking for v0)

| # | Question | Default for v0 |
|---|----------|----------------|
| **Q1** | Display names for generic recruits? | `"Human Adventurer"`, `"Elf Adventurer"`, `"Barbarian Adventurer"` |
| **Q2** | Should recruit menu show essence names for rank-8 rows? | Optional flavor subtext; not required for A4 |
| **Q3** | Monthly refresh: clear `recruitedIds` or only add new ids? | Defer to monthly feature; static v0 unaffected |

---

## 14. File & type checklist (implementation reference)

| Artifact | Suggested location |
|----------|------------------|
| `PartyCapacityService` | `Assets/Scripts/Manager/Party/` |
| `PartyRecruitDefinition` | `Assets/Data/Party/` or `Assets/Scripts/Party/Recruitment/` |
| `PartyRecruitCatalog` | Static v0 list; later ScriptableObject under `Assets/Data/Party/` |
| `PartyRecruitBoardState` | Run-scoped on `PartyManager` DDOL object |
| `PartyRecruitmentService` | `Assets/Scripts/Party/Recruitment/` |
| `IRecruitCostCalculator` | Same folder |
| Dialog changes | `AdventurersGuildSecretaryDialogSession.cs` |
| Tests | `Assets/Tests/UnitTests/Party/PartyRecruitmentTests.cs` |
