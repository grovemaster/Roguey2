# Dwarf — Clan, Hall of Ancestors & racial progression (requirements)

**Purpose:** Specify how **Dwarf clans** gate racial ability progression: **clan membership**, **personal clan rank**, **clan prestige**, and **learning techniques** at each clan’s **Hall of Ancestors** altar. Each clan has its own **patron Ancestor** and **branching skill tree**. This doc is the **player-facing progression layer** on top of the data/runtime contracts in [Dwarf — Patron Ancestor & common abilities](Dwarf-Ancestor-And-Common-Abilities-Requirements.md).

**Status:** Partially implemented (v0 — Forge Brothers clan join, plaza Hall altar, frontier learn dialog).

**Depends on:** [Dwarf — Patron Ancestor & common abilities](Dwarf-Ancestor-And-Common-Abilities-Requirements.md) (`AncestorDefinition`, `SpiritImprintGraph`, `DwarfAncestorPathRuntime`, `DwarfCommonAbilitiesRuntime`), [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (tree graph shape, sibling exclusivity), [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) (town NPC upgrade dialog pattern), [Town building entry & exit](../World/Town-Building-Entry-And-Exit-Requirements.md) (interior floor instances), [NPC dialog](../World/NPC-Dialog-Requirements.md), [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md), [Safe zones](../World/Safe-Zone-Requirements.md), `PartyManager`, `CharacterStats.level`, `Race.Dwarf`.

**Related:** [Quest system](../World/Quest-Requirements.md) (future clan prestige + rank quests), [Shop NPCs](../World/Shop-NPC-Requirements.md) (donation UX patterns).

**Explicitly out of scope (v0):** Switching clans; respec / unlearn clan nodes; raising **clan prestige** in play (gates exist in data but prestige is **authored baseline** only); multi-clan party politics; PvP clan warfare; clan creation by the player; interior art for every clan (one sample clan vertical slice); hotbar execution polish for new actives; **Proficiencies menu (`P`)** Dwarf section.

---

## Locked decisions (v0)

| # | Decision |
|---|----------|
| **L1** | A Dwarf belongs to **at most one clan** at a time. Membership is **permanent** in v0 (clan switch is a **later** feature — §12). |
| **L2** | **Clan ⇒ patron Ancestor.** Joining clan **X** sets the Dwarf’s patron to clan **X**’s `AncestorDefinition` and initializes the Ancestor path to **root only**. There is no “patron without clan” for player-facing progression (anonymous NPCs may still have no clan). |
| **L3** | Each clan owns a **distinct town building** (exterior facade + interior floor instance). The interior includes a **Hall of Ancestors** room with an **altar** interactable. |
| **L4** | **Only Dwarves** may join a clan or use a Hall of Ancestors altar. Non-Dwarf speaker gets a single rejection line; no UI. |
| **L5** | **Learning** a new clan technique happens **only** at the altar (**Pay respects**). The racial menu **`K`** is **read-only reference** — it does not grant nodes (§10). |
| **L6** | One successful altar ceremony learns **exactly one** new node from the **frontier**: unlearned nodes that are **direct children of any already-learned node** on the patron tree (§7). |
| **L7** | When the frontier has **multiple** eligible nodes, v0 opens a **forced-choice dialog** listing every option (title, description, icon). The player **must pick one**; Cancel is **not** offered when ≥1 eligible node exists. |
| **L8** | When the frontier is **empty** (maxed, gated out, or exclusivity closed all branches), altar shows an appropriate **complete / blocked** message — no choice dialog. |
| **L9** | Node eligibility uses **three independent gates** (all must pass): **character level**, **clan member rank**, **clan prestige** (§6). Unmet gates appear in choice rows as disabled reasons (greyed out, not selectable). |
| **L10** | **Clan member rank** (personal) increases by **+1** each time the Dwarf learns a **new non-root** node at the altar in v0. Rank **0** = newly joined (root only). |
| **L11** | **Clan prestige** (clan-wide) is **serialized per clan** but **not raised in v0** — designers author a starting value per clan; future systems consume it (§9). |
| **L12** | **Common racial abilities** (0–3 slots, no clan required) remain a **separate track** unlocked by **character level** (see [ancestor doc §5](Dwarf-Ancestor-And-Common-Abilities-Requirements.md)); clan membership does not replace them. |
| **L13** | Altars and join dialogs work in **town safe zone** only; no combat side effects; no turn cost. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Clan identity** — Dwarves progress through **membership in a folk institution**, not a solo pick from a global Ancestor catalog. |
| **G2** | **Physical place** — Each clan has a **building** the player can enter; the **Hall of Ancestors** is the sacred progression space. |
| **G3** | **Three-axis gates** — Ability access reflects **personal level**, **standing in the clan**, and **the clan’s reputation** — not gold alone. |
| **G4** | **Branching honesty** — Trees may branch; the player chooses **which adjacent technique** to learn next when multiple frontier nodes qualify. |
| **G5** | **Barbarian parity** — Town **place + dialog** owns progression; **`K`** menu owns **reference**. |
| **G6** | **Data-driven** — Clans, trees, gates, and altar copy are assets; no per-clan C# subclasses. |
| **G7** | **Party-safe** — Any Dwarf party member can join **their** clan and use **that** clan’s altar when they are the **speaker** (active leader at interact time). |
| **G8** | **Future-ready** — Prestige raising, clan quests, and donations hook into the same **clan prestige** field without rewriting saves. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Dwarf clan** | A persistent world entity: identity, patron Ancestor, starting prestige, town building, Hall layout, join rules. |
| **Clan membership** | A Dwarf actor’s **clan id** (or empty). At most one clan. |
| **Patron Ancestor** | The `AncestorDefinition` bound to a clan; defines the **clan skill tree** (`SpiritImprintGraph`). |
| **Clan skill tree** | Same graph shape as Barbarian Spirit Imprint / existing Ancestor trees: root + branching children, optional `siblingExclusivityGroup`. |
| **Learned nodes** | Set of node ids the Dwarf has acquired (superset of `chosenPathNodeIds` — see §7.1). |
| **Frontier node** | An **unlearned** node whose **parent is learned**, passing all gates, and not foreclosed by exclusivity. |
| **Pay respects** | Altar interaction that runs the learn ceremony (choice dialog → append one node). |
| **Clan member rank** | Personal standing **within** the clan (integer tier on the Dwarf). Distinct from `CharacterStats.level`. |
| **Clan prestige** | **Clan-wide** reputation score (integer on the clan record). Gates **clan** content; not per-Dwarf. |
| **Hall of Ancestors** | Named room zone inside a clan building containing the **altar**. |
| **Join ceremony** | One-time dialog flow that sets clan membership (before first altar use). |

**Contrast with Human Knight:**

| Human Knight | Dwarf clan |
|--------------|------------|
| Class commit (Mage / Knight) | **Clan join** (which patron tree) |
| Skill points + combat pxp | **Altar learn** (one adjacent node per ceremony) |
| Drill instructor / tutor NPC | **Hall of Ancestors altar** |
| `K` menu spends points in town | **`K` menu read-only**; altar learns |

---

## 3. Conceptual model

```text
World
└── DwarfClanDefinition (×N)
      ├── clanId, displayName, description
      ├── patronAncestor → AncestorDefinition → SpiritImprintGraph
      ├── startingPrestige
      ├── townBuilding (exterior marker + interior floorId)
      └── hallAltarMarkerId

Dwarf actor (party member)
├── DwarfClanMembershipRuntime   ← clanId, clanMemberRank
├── DwarfAncestorPathRuntime     ← learned nodes / path (patron from clan)
└── DwarfCommonAbilitiesRuntime  ← 0–3 level-gated folk abilities (unchanged)

Clan save state (run/world)
└── DwarfClanProgressRuntime or GameStoryFlagService-backed record
      └── prestige (int), optional future quest flags
```

**Resolved tension with [ancestor doc §D6.0](Dwarf-Ancestor-And-Common-Abilities-Requirements.md):** v0 player Dwarves get patron **through clan join**, not a free-floating patron pick. NPC Dwarves may still be authored with preset clan + path for tests.

---

## 4. Screen & service responsibilities

| UI / place | Player can… | Cannot… |
|------------|-------------|---------|
| **Clan join dialog** (steward / elder) | Join **one** clan (Dwarf speaker only) | Join without being Dwarf; join second clan |
| **Hall altar — Pay respects** | Learn **one** frontier node (forced choice if many) | Learn non-adjacent nodes; learn when gated out |
| **Racial menu `K` — Dwarf body** | Read clan, rank, prestige, learned + ghost nodes | Learn nodes; join clan; raise prestige |
| **Future clan quest board / treasury** | Raise prestige or rank (post-v0) | Replace altar for basic tree learning |

---

## 5. Clan membership & joining

### 5.1 — Eligibility

| Rule | Detail |
|------|--------|
| Race | `CharacterStats.race == Race.Dwarf` |
| Subsystem | `RacialSubsystemKind.DwarfAncestry` on player prefab |
| Not already joined | `clanId` empty |
| Location | Join NPC or dialog trigger at **clan building** (exterior steward or interior elder) in **safe zone** |

### 5.2 — Join flow (v0)

```text
Enter (adjacent + facing Clan Steward) OR interact Join plaque
  → Resolve speaker = active party leader
  → If not Dwarf: rejection line
  → If already in a clan: "You already owe allegiance to {clanName}."
  → If unaffiliated: confirm join copy + Accept / Decline
  → On Accept:
       set speaker.clanId
       set patronAncestor from clan.patronAncestor
       initialize learned path = [root]
       clanMemberRank = 0
       re-apply DwarfAncestorPathRuntime
  → Success line + close
```

**Commitment:** `Permanent` in v0 (matches Human class commit policy).

### 5.3 — Unaffiliated Dwarf

- May still use **folk baseline** (`RacialLoadoutApplier`) and **common abilities** when level gates pass.
- **Cannot** use any Hall of Ancestors altar until joined.
- **`K` menu** shows unaffiliated empty state with banner: *Find a clan hall in town and swear allegiance to begin the Ancestor path.*

---

## 6. Three progression gates (per node)

Each **non-root** graph node may specify zero or more requirements:

| Gate | Field (proposed) | Checked against | v0 behavior |
|------|------------------|-----------------|-------------|
| **Character level** | `requiredCharacterLevel` | `CharacterStats.level` | Enforced |
| **Clan member rank** | `requiredClanMemberRank` | Speaker’s `clanMemberRank` | Enforced |
| **Clan prestige** | `requiredClanPrestige` | Speaker’s clan **`prestige`** | Enforced (value static in v0) |

**All specified gates must pass** for a frontier node to appear as **selectable** in the altar dialog. Failed gates still **list** the node (optional v0.1: hide instead) with greyed row + reason, e.g. *Requires clan prestige 20 (clan has 5).*

**Root node:** Auto-learned on join; no gates; empty payload (same as Barbarian root).

**Clan member rank (v0 formula):** `clanMemberRank = max(0, learnedNonRootNodeCount)`. Each altar learn increments rank. **Later:** rank titles (*Initiate*, *Hammer-Brother*, …) may map from thresholds without changing save shape.

---

## 7. Hall of Ancestors & altar — learning rules

### 7.1 — Learned set vs path list

**v0 save shape:** Keep `chosenPathNodeIds` as the **ordered learn history** (append-only). **Learned set** = all ids in that list (validated against graph). Parent checks use the **set**, not only the path tail — this supports **branching** where the player learned node B before returning to learn sibling C under the same parent.

**Invariant:** Every id in the list must form a valid **root-connected** set (each non-root’s parent is also in the set). Validator rejects corrupt saves → **root only** + warning (same policy as imprint).

### 7.2 — Frontier (adjacent unlearned)

```text
frontier = { node N |
  N not in learnedSet
  AND parent(N) in learnedSet
  AND exclusivity OK (no learned sibling in same exclusivity group)
  AND all gates pass (§6)
}
```

This matches the design intent: *“unlearned skills adjacent to learned skills.”*

### 7.3 — Pay respects ceremony

| Step | Behavior |
|------|----------|
| 1 | Interact altar (`Enter` adjacent + facing) in Hall of Ancestors |
| 2 | Speaker must be **Dwarf**, **member of this clan**, in **safe zone** |
| 3 | Compute `frontier` for speaker on **this clan’s** patron tree |
| 4 | If `frontier` empty → flavor line (*Ancestors have no new secrets…* / *Prove yourself further…*) |
| 5 | If `frontier` non-empty → **Learn choice dialog** (§8) |
| 6 | On pick → append node id, `clanMemberRank++` (if non-root), re-apply runtime, success line |

**Frequency:** No cooldown in v0 (designers may add **once per town visit** later).

**Cost:** No gold/item cost in v0 at the altar itself (gates carry the progression cost). **Donations** are a **separate** future prestige channel (§9).

### 7.4 — Sibling exclusivity

When the player learns one child of a parent, **other children sharing the same non-zero `siblingExclusivityGroup`** leave the frontier permanently (foreclosed). **`K` menu** shows foreclosed siblings as **ghost rows** (Barbarian hybrid visibility pattern).

---

## 8. Altar learn dialog (v0 — forced choice)

### 8.1 — Layout

Modal dialog (reuse `NpcDialogBoxUI` choice pattern or dedicated `DwarfAltarChoiceUI`):

| Element | Source |
|---------|--------|
| **Title** | *The ancestors await your offering.* (clan-flavored override per clan asset) |
| **Body** | Short clan / patron blurb (1–2 sentences) |
| **Choice rows** | One per **selectable** frontier node: **icon**, **displayName**, **description** (truncated), optional gate hint |
| **Disabled rows** | Frontier nodes failing gates (if shown): greyed, not confirmable, show **why** |
| **Cancel** | **Only when zero selectable nodes** — actually close without learning. When ≥1 selectable: **no Cancel** — player **must** choose (per user request). **Esc** behavior: if forced choice, Esc picks nothing and **closes without learning** (document explicitly — or block Esc; **recommend:** Esc closes without learn, same as declining when no valid pick). |

**User requirement:** *The player must choose one of them* when multiple **selectable** options exist — implement as **no Cancel button** on that dialog; only node buttons.

### 8.2 — Single option

When exactly **one** selectable frontier node: still show full row (icon + text); one confirm button (*Accept the {name}*). No other selectable choices.

### 8.3 — Icons

Use `SpiritImprintNodeData` active ability hotbar icon when present; else clan **patron emblem** fallback (procedural gold/steel ring in `RacialUiTheme`, distinct from Barbarian gold).

---

## 9. Clan prestige (clan-wide)

### 9.1 — What it is

**Clan prestige** is a **single integer per clan** representing the clan’s standing in the world. It gates **deeper tree nodes** (`requiredClanPrestige`) so individual Dwarves cannot outpace their clan’s reputation.

### 9.2 — v0

| Rule | v0 |
|------|-----|
| Storage | `DwarfClanDefinition.startingPrestige` copied into run save on first touch |
| Raising | **Not implemented** — value stays at start unless debug/cheat |
| UI | **`K` menu** displays current prestige read-only |
| Gates | Nodes with `requiredClanPrestige > startingPrestige` appear **blocked** at altar with reason |

### 9.3 — Future raising (recommended — post-v0)

Use **multiple channels** (not either/or):

| Channel | Role | Notes |
|---------|------|-------|
| **Clan quests** | **Primary** — story beats, dungeon objectives, escort, retrieve relic | Quest turn-in increments prestige; repeatable dailies optional later |
| **Treasury donations** | **Secondary** — gold sink in clan building | Diminishing returns; cap per week; never sole gate for top-tier nodes |
| **World events** | **Spike rewards** — defend hall, festival win | Optional live-ops / narrative |

**Recommendation:** Quests carry **narrative weight** for large prestige jumps; donations tune **economy sink** between quest beats. Document exact formulas when implementing — out of v0 scope.

**Not recommended as v0 default:** Prestige **only** from donations ( feels pay-to-win ) or **only** from combat kills ( duplicates proficiency systems ).

---

## 10. Racial abilities menu (`K`) — Dwarf body

### 10.1 — Role (locked)

The Dwarf **`K` body is primarily read-only reference**, same discipline as Barbarian Spirit Imprint and Beastman Soul Beast:

| In scope | Out of scope |
|----------|--------------|
| Clan name, patron portrait/name | Join clan |
| Clan member rank, clan prestige | Pay respects / learn nodes |
| Learned nodes (full detail) | Raise prestige |
| Foreclosed sibling **ghosts** | Respec |
| Common ability slots (filled + locked empty) | Assign hotbar (footer points to ability hotbar) |

**Banner (unaffiliated):** *Swear allegiance at a **clan hall** to walk your patron’s path.*

**Banner (member):** *View only — learn new clan techniques at the **Hall of Ancestors** altar in **{clanShortName}**.*

### 10.2 — Why not learn from `K`?

| Reason | Detail |
|--------|--------|
| **Place fantasy** | Learning is a **ritual at the altar**, not a spreadsheet |
| **Consistency** | Barbarian uses Shaman; Elf uses meditation circle; Dwarf uses Hall |
| **Branch choice** | Altar dialog is the natural **forced pick** UX; cluttering `K` duplicates it |

### 10.3 — Optional v0.1 enhancement (not required for first ship)

- **Highlight** frontier nodes as *Available at altar* (spoiler-safe if gates pass) — still **not clickable** to learn.
- Link footnote: *Clan prestige rises through **clan quests** and **hall offerings** (coming soon).*

---

## 11. Clan buildings (town)

### 11.1 — One building per clan

Each `DwarfClanDefinition` references:

| Field | Purpose |
|-------|---------|
| `exteriorDoorMarkerId` | Plaza door → interior portal pair ([town building doc](../World/Town-Building-Entry-And-Exit-Requirements.md)) |
| `interiorFloorId` | e.g. `town_interior_clan_forgefather` |
| `hallAltarMarkerId` | Interior cell for altar interactable |
| `joinNpcMarkerId` | Steward spawn (exterior or interior) |

### 11.2 — Hall of Ancestors room

- Authored sub-region of interior stamp (labeled tiles or marker bounds).
- Contains: **altar** (required), optional **patron statue**, optional **treasury** prop (future donations), optional **quest board** (future).
- v0 vertical slice: **one** sample clan (*Forge-Father* / smith clan) wired in `TownTest`.

### 11.3 — Cross-clan access

- Any player may **enter** any clan building ( tourism / lore ).
- **Altar** checks speaker **`clanId` matches this hall’s clan** — other clans get: *This altar belongs to {otherClan}. Your ancestors wait elsewhere.*

---

## 12. Switching clans (explicitly later)

| Rule | Detail |
|------|--------|
| v0 | **Not allowed** |
| Future | Rare story beat (exile + adoption, grave crime, epic quest) — wipes or freezes old tree progress |
| Design guard | Saves **`formerClanId`** + **foreclosed path snapshot** for narrative callbacks |

Do not implement hooks beyond **single `clanId`** in v0.

---

## 13. Common abilities (unchanged track)

Per [ancestor doc §5](Dwarf-Ancestor-And-Common-Abilities-Requirements.md):

- Up to **three** folk packages (`DwarfCommonAbilityDefinition`) **without** clan membership.
- Unlocked by **character level** thresholds (separate from clan rank).
- **`K` menu** shows slots; unlocking happens automatically on level-up (or future level-up UI), **not** at altar.

**Independence:** A joined Dwarf has **both** clan tree progress **and** common slots.

---

## 14. Data model (proposed extensions)

### 14.1 — `DwarfClanDefinition` (new ScriptableObject)

| Field | Notes |
|-------|-------|
| `clanId` | Stable save key |
| `displayName` / `shortName` / `description` | UI + dialog |
| `patronAncestor` | `AncestorDefinition` reference |
| `startingPrestige` | v0 static prestige |
| `townBuilding` | Marker ids + interior floor id (§11) |
| `altarFlavorTitle` | Optional override for learn dialog |

### 14.2 — `SpiritImprintNodeData` extensions

| Field | Default | Notes |
|-------|---------|-------|
| `requiredCharacterLevel` | 1 | Existing on imprint nodes if not already present |
| `requiredClanMemberRank` | 0 | Personal standing |
| `requiredClanPrestige` | 0 | Clan-wide |

### 14.3 — `DwarfClanMembershipRuntime` (new component)

| Field | Notes |
|-------|-------|
| `clanId` | Empty = unaffiliated |
| `clanMemberRank` | Personal tier (§6) |

Patron + path remain on `DwarfAncestorPathRuntime` (patron derived from clan on join).

### 14.4 — Clan prestige save

Minimal v0: `Dictionary<clanId, int>` on a DDOL `DwarfClanWorldState` service, seeded from `startingPrestige`.

---

## 15. Router integration (`K` menu)

Extends [Racial abilities menu §5.3](../UI/Racial-Abilities-Menu-Requirements.md):

```text
Race.Dwarf + DwarfAncestry
  → DwarfClanBodyView (new)
      unaffiliated → placeholder + join banner
      member → clan summary + tree list + common slots
```

**Status:** Dwarf body **not implemented** — placeholder only today. Implement alongside or after first clan vertical slice.

---

## 16. Sample content (v0 proof)

| Asset | Purpose |
|-------|---------|
| `DwarfClan_ForgeBrothers` | First clan; patron `ForgeFather` |
| `town_interior_clan_forgefather` | Interior with Hall + altar marker |
| Plaza door marker | Enter clan hall from town |
| `ForgeFatherTree` (extend) | Add **branch** with exclusivity + prestige/rank gates for tests |
| Join steward NPC | `clan_forgefather_steward` |
| Altar interactable | `hall_ancestor_altar_forgefather` |

**Test gates example:**

| Node | Level | Member rank | Prestige |
|------|-------|-------------|----------|
| `forge_blessing` | 1 | 0 | 0 |
| `stone_endurance` | 3 | 1 | 0 |
| Branch B sibling | 5 | 2 | 10 (blocked in v0 until prestige implemented) |

---

## 17. Acceptance criteria (examples)

- Given an **unaffiliated** Dwarf, interacting with clan A’s altar shows **rejection** (not a member).
- Given a Dwarf **joins** clan A, patron is A’s Ancestor, path is **root only**, rank **0**.
- Given learned `{root, forge_blessing}` and two eligible children, altar dialog shows **two** icon rows and **requires** picking one.
- Given a frontier node failing **prestige** gate, row is **visible but disabled** with reason (or hidden — pick one behavior and test).
- Given learning a node with exclusivity siblings, siblings **never** appear on frontier afterward; **`K`** shows them as **ghosts**.
- Given a **Human** speaker at altar, single rejection line.
- Given **`K`** open, no button learns a node or raises prestige.
- Given **Barbarian** imprint, Dwarf clan systems do not mutate Barbarian runtime.

---

## 18. Implementation phases

| Phase | Deliverable |
|-------|-------------|
| **P0** | `DwarfClanDefinition`, membership runtime, join dialog, extend node gate fields |
| **P1** | One clan building interior + altar interactable + learn dialog (forced choice) |
| **P2** | **`K` Dwarf body** (read-only) |
| **P3** | Second clan + exclusivity branch proof |
| **P4** | Prestige raising (quests + donations) + blocked node unlock in play |

---

## 19. Open questions (recommendations)

| Question | Recommendation |
|----------|----------------|
| Should quests raise prestige? | **Yes — primary channel** (§9.3) |
| Should donating gold raise prestige? | **Yes — secondary sink**, post-v0; not required for first tree nodes |
| Should certain **actions** (boss kills, crafting) raise prestige? | **Optional tertiary** via flagged world events; avoid always-on grinds |
| Should **`K` menu** do more than static display? | **v0: no.** Optional v0.1: highlight altar-available nodes read-only |
| Patron pick without clan? | **Player Dwarves: no.** NPCs / debug presets only |
| One learn per visit? | **Defer** — unlimited in v0 unless playtest says otherwise |
| Hide vs show gated-out frontier nodes? | **Show disabled** with reason (teaches prestige/rank goals) |

---

## 20. Related documents

- [Dwarf — Patron Ancestor & common abilities](Dwarf-Ancestor-And-Common-Abilities-Requirements.md) — runtime, common slots, graph shape
- [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) — dialog upgrade pattern
- [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) — shell + read-only discipline
- [Town building entry & exit](../World/Town-Building-Entry-And-Exit-Requirements.md) — interior instances
- [Phase 0 — Glossary](Phase0-Glossary-And-Data-Contracts.md) — update when shipped

**Supersedes for player progression:** [ancestor doc §11.4](Dwarf-Ancestor-And-Common-Abilities-Requirements.md) table row “Ancestor path — Inspector preset / Special event NPC” → **Hall of Ancestors altar** for player Dwarves; Inspector preset remains for NPCs/tests.
