# Dragonian — Spell learning (Elder quests)

**Purpose:** Specify how **Dragonian** party members **learn** new spells by completing **sequential quests** offered by **Dragonian Elder NPCs** in town. Each quest in a chain rewards **one unique `DragonianSpellDefinition`** added to that member’s **known library**. Learning is **separate** from **memorization** and **casting** — see [Dragonian — Spell memory & casting](Dragonian-Spell-Memory-Requirements.md).

**Inspiration:** *Surviving the Game as a Barbarian* — draconic techniques are taught by elders through trials, not purchased like Mage spell tiers. Closest engine analogues: [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) (race-gated town NPC + transaction) and [Quest system](../World/Quest-Requirements.md) (objectives + turn-in), but Dragonian learning requires **per-party-member quest ownership** (not party-shared progress).

**Status:** Implemented (v1 — Elder Volscale chain, per-member quest instances, `TryLearnSpell`, safe-zone gated accept/turn-in).

**Depends on:** [Dragonian — Spell memory & casting](Dragonian-Spell-Memory-Requirements.md) (`DragonianSpellDefinition`, `DragonianSpellsRuntime`, `RacialSubsystemKind.DragonianSpells`, Soul Power economy), [Quest system](../World/Quest-Requirements.md) (objective types, dialog hooks, journal — **extended** for per-member instances), [NPC dialog](../World/NPC-Dialog-Requirements.md), `PartyManager`, `CharacterStats.race` / level, `GameStoryFlagService`, `InventoryManager`, [Party experience](../Progression/Party-Experience-And-Leveling-Requirements.md) (level gates), [Safe zone](../World/Safe-Zone-Requirements.md) (town-only accept/turn-in).

**Related:** [Human — Class powers](Human-Class-Powers-Requirements.md) (Mage spell **learning** sources deferred — **different folk, Magic Power**), [Dragonian — racial abilities menu](Dragonian-Racial-Abilities-Menu-Requirements.md) (memorize loadout UI), [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (shared `K` shell), [Tiefling — Fleshmetal Forgemaster](Tiefling-Fleshmetal-Forgemaster-NPC-Requirements.md) (multi-NPC extensibility pattern).

**Explicitly out of scope (v1):** Spell **memorize UI** (defer to spell-memory v0.1); **unlearn** / respec spells; **scroll** or **loot** spell learning; cross-race quest boosting; **shared** party quest progress for Elder chains; casting spells during quest dialog; PvP spell theft; **Magic Power** costs anywhere in this pipeline; procedural quest generation; save/load quests across game sessions beyond existing run scope (follow quest doc until meta-save exists).

---

## Locked decisions (user)

| # | Decision |
|---|----------|
| **L1** | Dragonians **learn** spells from **Dragonian Elder NPCs** via **quests**, not from Human Mage trainers or generic spell vendors. |
| **L2** | Each Elder NPC owns a **sequential quest chain** — quest **N+1** is offered only after quest **N** is **completed for that same party member**. |
| **L3** | Each quest in a chain rewards **exactly one** **`DragonianSpellDefinition`** (unique spell per quest). |
| **L4** | **Multiple Elder NPCs** will exist over time; each may have its **own chain** (and optional cross-Elder story gates). |
| **L5** | **Only Dragonians** may **accept** Elder spell quests and **turn them in** — the **active speaker must be Dragonian** at accept time; non-Dragonian speakers are rejected. |
| **L6** | Quests may require **story progression** (`GameStoryFlagService`) and/or the **accepting Dragonian’s minimum level**. |
| **L7** | **Per-party-member quest instances** — if the roster has two Dragonians, **each must accept and turn in separately**; progress is **not** shared between them. |
| **L8** | **Fetch / deliver objectives** count items from the **quest owner’s inventory only** — each Dragonian must gather **their own** required stacks (not party-pooled for turn-in). |
| **L9** | Dragonian spells use **Soul Power** (`MaxSoulPower`, `currentSoulPower`, `memorizeCost`, `soulPowerCastCost`) — **never Magic Power**. |
| **L10** | **Speaker = active party leader** at talk time (`PartyManager.GetActiveMember()`); player switches focus (F-key strip) to each Dragonian to advance their chain. |
| **L11** | **Accept, turn-in, and spell learning** occur **only in town / safe zone** (non-combat) — blocked in dungeon and combat, same discipline as [memorize loadout](Dragonian-Spell-Memory-Requirements.md) §L10. |
| **L12** | A single Dragonian may hold **multiple Active Elder quests simultaneously** when they come from **different Elders** (e.g. one quest each from three elders). **Within one Elder’s chain**, still **one Active quest at a time** per member. |
| **L13** | **Production:** new Dragonians start with **no known spells** on `DragonianPlayer` — **`knownSpells`** and **`presetMemorizedSpellIds`** are **empty**; all spells enter the library via **Elder quest turn-in** (`TryLearnSpell`). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Learn vs cast clarity** — Quest reward adds to **known library** only; player still **memorizes** in town per spell-memory doc. |
| **G2** | **Race-exclusive progression** — Elder quests are a Dragonian identity pillar; engine blocks accept/turn-in for other folk. |
| **G3** | **Multi-Dragonian fairness** — Two Dragonians in one roster both earn the **same spell catalog** through **parallel** personal chains. |
| **G4** | **Sequential teaching** — Elders expose one lesson at a time; designers author ordered chains without code forks. |
| **G5** | **Extensible elders** — Data model supports **several NPCs** and **several chains** without rewriting turn-in logic. |
| **G6** | **Quest reuse** — Leverage existing objective types (kill, fetch, talk, visit, flag) where possible; add **LearnSpell** reward type. |
| **G7** | **Journal clarity** — Active Elder quests show **which Dragonian** owns the instance. |
| **G8** | **Safe degradation** — Duplicate learn attempts, invalid saves, or dead quest owners fail with clear feedback — no silent spell loss. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Elder** | Town `NpcController` with a **`DragonianElderDefinition`** — offers one spell-learning **chain**. |
| **Spell-learning chain** | Ordered list of **`QuestDefinition`** ids authored on the Elder; sequential per member. |
| **Quest owner** | The **specific party member** (`partyMemberId`) who accepted the instance; only they may progress objectives (where bound) and turn in. |
| **Per-member quest instance** | Runtime state keyed by **`(questId, partyMemberId)`** — **not** party-shared (contrast [Quest §R5.6](../World/Quest-Requirements.md)). |
| **Learn spell** | Add `spellId` to owner’s `DragonianSpellsRuntime` known list — **permanent** for that member (v1). |
| **Memorize** | Separate loadout step — **not** granted by quest reward unless explicitly authored (default **no**). |
| **Speaker** | Active party leader at **`Enter`** talk time. |
| **Turn-in** | Speaker talks to **designated giver** Elder with **Complete quest** branch; validates owner + objectives + race. |

---

## 3. Relationship to spell memory & casting

| Layer | Doc | Elder quest role |
|-------|-----|------------------|
| **Learn** | This doc | Quest reward → **`KnownSpells`** |
| **Memorize** | [Spell memory §5](Dragonian-Spell-Memory-Requirements.md) | Player edits loadout in **safe zone** (`DragonianSpellLoadoutService`) |
| **Cast** | [Spell memory §6](Dragonian-Spell-Memory-Requirements.md) | Hotbar → **`soulPowerCastCost`** from **`currentSoulPower`** |

**Locked:**

| Rule | Detail |
|------|--------|
| **R3.1** | Quest completion calls **`DragonianSpellsRuntime.TryLearnSpell(spellId)`** (new API) — idempotent if already known. |
| **R3.2** | Learning **does not** consume Soul Power and **does not** change memorized loadout by default. |
| **R3.3** | Reward UI shows spell **`displayName`**, **`memorizeCost`**, and **`soulPowerCastCost`** so players understand post-quest steps. |
| **R3.4** | **`magicPowerCost`**, **`MaxMagicPower`**, and **`HumanMageSpell`** pipeline are **never** involved. |

**Player-facing copy (turn-in success template):**

> *“{speakerName} has internalized {spellDisplayName}. Visit a safe haven to memorize draconic word-forms before battle.”*

---

## 4. Relationship to the quest system

General quests are **party-shared** ([Quest §R5.6](../World/Quest-Requirements.md)). Elder spell quests require a **new ownership mode**.

### 4.1 — Quest ownership extension (locked direction)

Add to **`QuestDefinition`** (or parallel **`DragonianSpellQuestDefinition`** wrapper):

| Field | Value for Elder chains |
|-------|------------------------|
| **`ownership`** | **`PerPartyMember`** (new enum value) |
| **`requiredRace`** | **`Race.Dragonian`** (accept + turn-in + objective credit where applicable) |
| **`requiredSubsystem`** | **`RacialSubsystemKind.DragonianSpells`** (optional hard gate) |

**Runtime key:** `QuestInstanceKey = (questId, partyMemberId)`.

| Party-shared quest | Elder spell quest |
|--------------------|-------------------|
| One instance per `questId` | One instance per **`(questId, memberId)`** |
| Any member may satisfy `actorRequirement: None` objectives | Objectives default to **quest owner** unless explicitly party-wide |
| Fetch sums **all** inventories (§R7.6.1) | Fetch counts **owner inventory only** (§7.4) |
| Turn-in: any leader at giver | Turn-in: **speaker must be quest owner** + Dragonian |

### 4.2 — Chain sequencing

Each **`DragonianElderDefinition`** references:

```text
chainQuestIds[] = [ quest_dragonian_elder_a_01, quest_dragonian_elder_a_02, ... ]
```

| Rule | Detail |
|------|--------|
| **R4.2.1** | Quest at index **0** is offerable when **accept prerequisites** pass for **speaker** (level, flags, Elder unlocked). |
| **R4.2.2** | Quest at index **k > 0** is offerable only when quest **`chainQuestIds[k-1]`** is **Completed** for the **same `partyMemberId`**. |
| **R4.2.3** | A member may have **at most one Active** quest **per Elder chain** at a time. |
| **R4.2.4** | A member may have **Active quests from multiple Elders in parallel** (e.g. Volscale + Mireth + a third elder simultaneously). |
| **R4.2.5** | Completing the **last** quest in a chain does not block other Elders’ chains unless authored via flags. |

### 4.3 — Reward type: learn spell

Extend quest rewards:

| Reward field | Type | Effect |
|--------------|------|--------|
| **`learnDragonianSpellId`** | string | Grants spell to **quest owner** via `TryLearnSpell` |
| **`gold` / `items` / `flags`** | existing | Optional secondary rewards (unusual for teaching quests) |

**Locked:** **`learnDragonianSpellId`** is **mandatory** for every quest in an Elder spell chain.

---

## 5. Dragonian Elder NPCs

### 5.1 — NPC model (v1 placeholder)

| Field | v1 placeholder | Notes |
|-------|----------------|-------|
| **Display name** | **Dragonian Elder** (suffix by id, e.g. “Elder Volscale”) | Narrative pass later |
| **Stable id** | `dragonian_elder_{theme}` | Dialog profile + stamp marker |
| **Race (folk)** | Dragonian appearance | Distinct from party field sprites |
| **Prefab** | `DragonianNpc` variant | Strip player-only components; add `NpcController` |
| **Location** | Town safe zone | Near other racial trainers |
| **Narrative role** | Teaches one **thematic** spell chain | e.g. flame elder, warding elder |

### 5.2 — Multiple elders (extensibility)

| Concept | Rule |
|---------|------|
| **`DragonianElderDefinition`** | ScriptableObject: `elderId`, display name, portrait, **`chainQuestIds[]`**, optional **`unlockStoryFlags[]`** |
| **Catalog** | Optional registry listing all elders for tools / racial menu banner |
| **Cross-elder gates** | Elder B’s chain quest 0 may require flag set by Elder A’s final quest |
| **Spell uniqueness** | Each `spellId` appears as reward **at most once** across all Elder quests (design guardrail) |

**Future content target (not v1 scope):** **3–5 elders**, **3–6 quests each**, **unique spell per quest**.

---

## 6. Interaction model

| Rule | Detail |
|------|--------|
| **Open talk** | **`Enter`** adjacent + facing — [NPC dialog §3](../World/NPC-Dialog-Requirements.md) |
| **Speaker** | `PartyManager.GetActiveMember()` |
| **Turn cost** | **No turn** for talk, accept, or turn-in |
| **Safe zone** | Accept, turn-in, and **`TryLearnSpell`** **town / safe zone only** — non-combat (L11); gate via `SafeZonePolicyService` |
| **Blocks gameplay** | Standard dialog stack |

```
Enter (adjacent + facing Elder)
  → Resolve speaker race + partyMemberId
  → Branch: non-Dragonian | no subsystem | chain state
  → Offer next quest in chain (Accept?) OR turn-in ready OR flavor / complete chain
  → On accept: create PerPartyMember instance for speaker
  → On turn-in: validate owner + objectives → LearnSpell reward → completed
```

---

## 7. Race gates & speaker rules

### 7.1 — Accept gate

| Check | Failure feedback (template) |
|-------|----------------------------|
| Not in safe zone (town) | **“You can only study with the elders in town.”** |
| `speaker.race != Dragonian` | **“The elders share their word-forms only with Dragonian kin.”** |
| Missing `DragonianSpellsRuntime` | **“Your draconic spirit is not awakened.”** (fallback) |
| `requiredMinLevel` not met | **“You are not yet ready for this lesson. (Requires level {n}.)”** |
| Story flag missing | Elder-specific line referencing the blocked story beat |
| Prior chain quest incomplete **for this member** | **“Complete your prior lesson first.”** |
| Spell already known (edge case) | Skip to next chain quest offer or **“You already know this word-form.”** |

### 7.2 — Turn-in gate

| Check | Failure |
|-------|---------|
| Speaker not **quest owner** | **“This trial was sworn by another. Let them speak.”** |
| Speaker not Dragonian | Same as §7.1 rejection |
| Objectives incomplete | Standard quest incomplete branch |
| Not at **giver** Elder (`giverNpcId` / `elderId` match) | Cannot complete from wrong NPC |

### 7.3 — Non-Dragonian party leader at Elder

- Show **§7.1 rejection** only — **no** quest journal preview for other members’ Dragonian quests on that dialog page (avoid spoilers).
- Hint (optional v1.1): **“Switch to a Dragonian party member to speak with this elder.”**

---

## 8. Per-party-member progress

### 8.1 — Accept

| Rule | Detail |
|------|--------|
| **R8.1.1** | Accept binds instance to **`speaker.partyMemberId`** (stable roster key — same id used in save blobs). |
| **R8.1.2** | Second Dragonian accepting the **same `questId`** creates a **second instance** — independent progress. |
| **R8.1.3** | Same Dragonian **cannot** accept the same quest twice per run (unless **`repeatable`** — **no** for spell chains v1). |
| **R8.1.4** | Accept is **explicit** (dialog Yes) — [Quest §R5.1–R5.2](../World/Quest-Requirements.md). |

### 8.2 — Objective credit

Default **`actorRequirement`** for Elder spell quests:

| Objective kind | Credits to |
|----------------|------------|
| Kill / visit / talk (default) | **Quest owner** only |
| Fetch / deliver | **Quest owner inventory** only (§8.3) |
| Party-wide kill (authored exception) | Any member may count — **discouraged** for teaching quests |

### 8.3 — Fetch / deliver — per-member items (locked)

Overrides [Quest §R7.6.1](../World/Quest-Requirements.md) for **`ownership = PerPartyMember`**:

| Rule | Detail |
|------|--------|
| **R8.3.1** | **Collect** progress = items in **`questOwner`’s** carried (+ subspace when enabled) inventory **only**. |
| **R8.3.2** | **Turn-in** removes required items from **owner’s** inventory only — not from other members’ bags. |
| **R8.3.3** | Example: quest requires **Dragon Scale ×3** — Dragonian A and Dragonian B each need **3 scales in their own inventory** at turn-in. |
| **R8.3.4** | Party cannot “pool” scales onto one Dragonian to complete two turn-ins in one transaction — second turn-in validates owner inventory again. |
| **R8.3.5** | Quest items (`ItemCategory.QuestItem`) bind to **`(questId, partyMemberId)`** when dropped-on-floor rules apply. |

### 8.4 — Journal presentation

| Rule | Detail |
|------|--------|
| **R8.4.1** | List title format: **`{memberDisplayName}: {questTitle}`** (e.g. *“Volscale: Gather Ember Scales”*). |
| **R8.4.2** | Detail pane repeats owner name in header accent. |
| **R8.4.3** | Filter toggle **post-v1:** “Show all members” vs “Active leader only”. |
| **R8.4.4** | Reward line includes **“Learn: {spellDisplayName}”** and Soul Power costs — not Magic Power. |

---

## 9. Quest prerequisites (authoring)

### 9.1 — Accept prerequisites (evaluated per speaker / owner)

| Type | Example | Evaluated against |
|------|---------|-------------------|
| **Minimum level** | `requiredMinLevel = 5` | **`speaker.level`** (or party XP level on `CharacterStats`) |
| **Story flag** | `met_dragonian_elder_volscale` | `GameStoryFlagService` |
| **Prior quest complete** | Implicit via **`chainQuestIds`** order | Same **`partyMemberId`** |
| **Prior spell known** | Optional gate for advanced lesson | Owner’s `KnownSpells` |
| **Elder unlocked** | Flag set by main story or other Elder | Global or per-elder |

### 9.2 — Objective templates (v1 set)

| Type | Use in spell chains | Notes |
|------|---------------------|-------|
| **`KillSpeciesObjective`** | Hunt trial | Owner gets kill credit |
| **`CollectItemObjective`** | Gather reagents | Owner inventory only |
| **`TalkToNpcObjective`** | Report to another NPC | Owner as speaker |
| **`VisitMarkerObjective`** | Meditation shrine / ritual tile | Owner position |
| **`StoryFlagObjective`** | Dungeon story beat | Set by dungeon script |
| **`Composite`** | Multi-step lesson | AND required |

**Defer v1:** escort, timed, PvP, dungeon-floor-only auto-grant without Elder turn-in.

---

## 10. Dialog flows (Elder)

### 10.1 — Non-Dragonian speaker

Single line → close (see §7.1).

### 10.2 — Dragonian — no quest available

| Condition | Line (template) |
|-----------|-----------------|
| Chain complete for this member | **“You have learned all word-forms I can teach.”** |
| Gates not met (level / story) | Elder-specific blocked line with hint |
| Active quest **from this Elder’s chain** | **“Finish your current lesson with me before the next.”** |

### 10.3 — Dragonian — offer next quest

**Body template:**

> **{elderDisplayName}:** The next word-form is **{nextQuestSpellName}**. Will you undertake **{questTitle}**?
>
> **Requirements:** {formattedPrerequisites}  
> **Trial:** {objectiveSummary}  
> **Reward:** Learn **{spellDisplayName}** (memorize {memorizeCost} SP · cast {soulPowerCastCost} SP)

**Choices:** **Accept** | **Not now**

### 10.4 — Dragonian — turn-in ready

> **{elderDisplayName}:** You have fulfilled the trial. Shall I seal **{spellDisplayName}** into your spirit?

**Choices:** **Complete quest** | **Not yet**

**On success:** run **`TryLearnSpell`** → success line → close → journal **Completed** for that member.

### 10.5 — Wrong member at turn-in

> **“This trial was sworn by {ownerDisplayName}. Let them speak.”**

---

## 11. Data model (sketch)

### 11.1 — `DragonianElderDefinition` (ScriptableObject)

| Field | Notes |
|-------|-------|
| **`elderId`** | Stable key |
| **`displayName` / `description`** | UI + dialog |
| **`npcId`** | Links to `NpcDialogProfile` |
| **`chainQuestIds[]`** | Ordered quest ids |
| **`unlockStoryFlags[]`** | Elder visible when all true |
| **`portrait` / `sprite`** | Art refs |

### 11.2 — Quest definition extensions

| Field | Notes |
|-------|-------|
| **`ownership`** | `PerPartyMember` |
| **`requiredRace`** | `Dragonian` |
| **`requiredMinLevel`** | On **accepting member** |
| **`learnDragonianSpellId`** | Reward spell id |
| **`giverNpcId`** | Must match offering Elder |

### 11.3 — `DragonianSpellsRuntime` API addition

| Method | Behavior |
|--------|----------|
| **`TryLearnSpell(spellId, out reason)`** | Add to known if valid definition + Dragonian actor; idempotent if known |
| **`HasLearned(spellId)`** | Query for dialog / journal |

Persist **`knownSpellIds[]`** on party member save (may already serialize via asset refs — normalize to ids for migration).

### 11.4 — Services

| Service | Role |
|---------|------|
| **`DragonianElderQuestService`** | Offer / accept / turn-in orchestration, chain index resolution |
| **`QuestService`** | Extended instance map for `PerPartyMember` |
| **`DragonianSpellLoadoutService`** | Unchanged — memorize still safe-zone gated |

---

## 12. v1 sample content (proof spec)

### 12.1 — Elder Volscale (placeholder)

| Field | Value |
|-------|-------|
| **`elderId`** | `dragonian_elder_volscale` |
| **Chain length** | **2** quests |
| **Town marker** | Near plaza (stamp TBD) |

| Order | Quest id | Objective (sample) | Min level | Reward spell |
|-------|----------|-------------------|-----------|--------------|
| 1 | `quest_dragonian_volscale_01` | Collect **Ember Scale ×2** (owner inventory) | 1 | **`dragonian_spell_sudden_strength`** → display **Draconic Surge** |
| 2 | `quest_dragonian_volscale_02` | Kill **5** fire-themed species (owner kills) | 3 | **`dragonian_spell_fireball`** → display **Dragon Flame** |

**Production prefab (locked L13):** `DragonianPlayer` ships with **empty** `knownSpells` / `presetMemorizedSpellIds`. Sample spells above are **Elder rewards**, not Inspector presets.

**QA / unit tests:** grant spells via **`TryLearnSpell`**, Elder quest dev completion, or **`SetKnownAndMemorized`** in tests — not production prefab data.

### 12.2 — Second elder (stub — content later)

| Field | Value |
|-------|-------|
| **`elderId`** | `dragonian_elder_mireth` |
| **Unlock** | Requires **`quest_dragonian_volscale_02`** completed by **any** member **or** story flag |
| **Chain** | TBD — defensive / utility spells |

---

## 13. Power economy reminder

| Resource | Used for learning? | Used for casting? |
|----------|------------------|-------------------|
| **Soul Power (`current`)** | **No** | **Yes** — `soulPowerCastCost` |
| **Soul Power (`Max`)** | **No** | **Yes** — memory budget via `memorizeCost` |
| **Magic Power** | **Never** | **Never** |

Quest copy and journal must **never** reference Magic Power for Dragonian spells.

---

## 14. Acceptance criteria

| ID | Test |
|----|------|
| **A1** | Human speaker at Elder gets rejection; **no** quest instance created. |
| **A2** | Dragonian A accepts chain quest 1; Dragonian B **does not** inherit progress — B must accept separately. |
| **A3** | Fetch quest requiring **3× Item X** — turn-in succeeds only when **owner** carries 3; party mate carrying 3 for them **does not** count. |
| **A4** | Two Dragonians both on same fetch quest each turn in with **their own** item stacks. |
| **A5** | Quest 2 not offered until quest 1 **Completed** for **same member**. |
| **A6** | Turn-in grants **`TryLearnSpell`**; spell appears in known library; **not** auto-memorized unless authored. |
| **A7** | Learned spell uses **`soulPowerCastCost`** when cast from hotbar — **not** `magicPowerCost`. |
| **A8** | Non-owner Dragonian at turn-in dialog gets **wrong member** rejection. |
| **A9** | Level 2 Dragonian cannot accept quest with **`requiredMinLevel = 5`**. |
| **A10** | Journal shows **`{memberName}: {questTitle}`** for per-member instances. |
| **A11** | Completing Elder A’s final quest sets unlock for Elder B when authored. |
| **A12** | Accept / turn-in / **`TryLearnSpell`** blocked outside safe zone (dungeon / combat). |
| **A13** | One Dragonian with Active quests from **three different Elders** — each progresses independently; completing one does not cancel the others. |
| **A14** | Fresh **`DragonianPlayer`** spawn has **zero** known and **zero** memorized spells until an Elder quest grants **`TryLearnSpell`**. |

---

## 15. Implementation phases

| Phase | Scope |
|-------|--------|
| **v1 (this doc)** | `PerPartyMember` quest ownership, `TryLearnSpell`, one placeholder Elder + 2-quest chain sample, dialog offer/turn-in, journal owner labels, per-owner fetch rules |
| **v1.1** | Racial menu banner linking active Elder / next lesson; second Elder stub |
| **v2** | Full elder roster; cross-elder story; quest item binding per member; repeat-run meta persistence |

**Update [Spell memory §12](Dragonian-Spell-Memory-Requirements.md):** move “Learn spells from NPC” from v1 bullet to **this doc** as v1 implementation target.

---

## 16. Resolved & open questions

| # | Question | Resolution |
|---|----------|------------|
| **Q1** | Party-shared or per-member quests? | **Locked: per-member instances** (L7, §4.1). |
| **Q2** | Pool fetch items across party? | **Locked: no** — quest owner inventory only (L8, §8.3). |
| **Q3** | Where may spells be learned? | **Locked: town / safe zone only** — accept, turn-in, and `TryLearnSpell` blocked in dungeon/combat (L11). |
| **Q4** | Who must speak to accept? | **Locked: active speaker must be Dragonian** (L5, L10). |
| **Q5** | Who must speak to turn in? | **Quest owner** must be speaker (§7.2). |
| **Q6** | Magic Power anywhere? | **No** — Soul Power only (L9). |
| **Q7** | Same spell reward twice on one member? | **No** — `TryLearnSpell` idempotent; skip or show “already known”. |
| **Q8** | Multiple active Elder quests per member? | **Locked: yes across Elders** — e.g. one Active quest each from three elders; **one Active per Elder chain** (L12, R4.2.3–R4.2.4). |
| **Q9** | Dead quest owner mid-quest? | Follow [Party member death](../Party/Party-Member-Death-Requirements.md) — instance **paused** until revive or **fail** (authored, post-v1). |
| **Q10** | Dev preset known spells vs quests? | **Locked (L13):** production prefab **empty**; spells from Elders only. Dev tools / tests may grant spells without changing prefab (§16.1). |
| **Q11** | Auto-memorize on learn? | **No** — learning adds to known library only; memorize remains separate safe-zone step ([spell memory §5](Dragonian-Spell-Memory-Requirements.md)). |

### 16.1 — Q10 explained: empty prefab vs Elder progression

**Locked:** **`DragonianPlayer`** production prefab has **no** `knownSpells` and **no** `presetMemorizedSpellIds`. A new Dragonian in the default roster begins with an **empty spell library** and earns each spell by **completing an Elder quest** in town.

| Context | Known spells source |
|---------|---------------------|
| **Production gameplay** | Elder quest turn-in → **`TryLearnSpell`** only |
| **Unit tests** | `SetKnownAndMemorized` / test helpers in code |
| **Manual QA** | Dev menu, context menu, or temporary test scene overrides (log warning) |

Spell definition assets (**Draconic Surge**, **Dragon Flame**, etc.) remain in **`Assets/Data/Racial/Dragonian/`** for Elder rewards and tools — they are **not** pre-wired on the player prefab.

**Cross-ref:** [Spell memory §8.3](Dragonian-Spell-Memory-Requirements.md) updated to match.

---

## 17. Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Dragonian — Spell memory](Dragonian-Spell-Memory-Requirements.md) | §9.3 racial menu banner; §12 phases; §13 Q2 learning cost |
| [Quest system](../World/Quest-Requirements.md) | `PerPartyMember` ownership; per-owner fetch override |
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | Dragonian body — learned source (Elder name) |
| [Phase 0 glossary](Phase0-Glossary-And-Data-Contracts.md) | Elder quest ownership enum |

---

## 18. Document history

| Date | Change |
|------|--------|
| 2026-06-13 | Locked L13: production `DragonianPlayer` empty known/memorized; spells from Elders only. Prefab cleared. |
| 2026-06-13 | Locked L11–L12, Q3/Q8: safe-zone-only learning; parallel Active quests across multiple Elders. Clarified Q10 (prefab preset vs quest progression). |
| 2026-06-13 | Initial draft — Elder NPC sequential chains, per-member accept/turn-in, per-owner fetch items, spell reward → known library, Soul Power only, multi-elder extensibility. |
