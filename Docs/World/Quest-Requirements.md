# Quest system — Requirements (draft)

**Purpose:** Define a **data-driven quest system** for JRogue’s **town hub + dungeon run** loop: NPCs and systems offer tasks; the player tracks **active** and **completed** quests in a **journal**; objectives cover **kill**, **fetch**, **talk**, **visit**, and **flag** goals; **rewards** and **quest items** integrate with existing inventory, shops, and story flags.

**Status:** v0 implemented — core quest service, journal, dialog hooks, and P0/P1 objectives.

**Depends on:** `GameStoryFlagService`, `FlagPrecondition`, `NpcDialogProfile` / `DialogGraphEvaluator`, `NpcController`, `NpcTalkInteraction`, `PartyManager`, `InventoryManager`, `ItemData` / `ItemInstance`, `ItemCategory.QuestItem`, `EnemySpeciesDefinition`, `DungeonRunState`, `TownTimeService`, [NPC dialog](NPC-Dialog-Requirements.md), [Shop NPCs](Shop-NPC-Requirements.md), [Safe zone](Safe-Zone-Requirements.md), [Dungeon time](Dungeon-Time-Requirements.md), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md), [Conditional enemy spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md).

**Related:** [Altar & map interact](Altar-And-Map-Interact-Requirements.md), [Party experience](../Progression/Party-Experience-And-Leveling-Requirements.md), [Main character game over](../Party/Main-Character-Game-Over-Requirements.md).

**Explicitly out of scope (v0):** multiplayer quest sync; quest editor inside Unity Play Mode; procedural quest generation; faction reputation matrix; escort AI with pathing; timed real-time deadlines; save/load quests across game sessions (run-scoped only in v0); full quest map markers / compass (text journal only v0).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Journal UI** — scrollable **Active**, **Completed**, and **Failed** lists; **status borders**; optional **pin-to-top**. |
| **G2** | **Quest types** — support common CRPG patterns (kill, fetch, talk, explore, flag) via **composable objectives**, not one-off code per quest. |
| **G3** | **Offer flow** — quests offered through **NPC dialog** (primary v0), extensible to signboards and scripted triggers. |
| **G4** | **Conditions** — **accept prerequisites** (when quest can be offered) and **clear conditions** (when quest completes) are data-authored. |
| **G5** | **Quest items** — fetch quests use **`ItemCategory.QuestItem`**; protected from shop sell; clear turn-in rules. |
| **G6** | **Rewards** — gold, items, XP, story flags, unlock follow-up quests — granted atomically on turn-in. |
| **G7** | **Run persistence** — quest state survives **town ↔ dungeon** travel within a run (DDOL service). |
| **G8** | **Reuse story flags** — integrate with `GameStoryFlagService` without replacing it (flags remain low-level; quests are structured layers above). |
| **G9** | **Designer-friendly** — one **`QuestDefinition`** asset per quest; objectives and rewards in one place. |

---

## 2. Reference — how other games handle quests

Useful patterns when choosing JRogue defaults:

| Game / genre | Journal | Objectives | Turn-in | Takeaway for JRogue |
|--------------|---------|------------|---------|---------------------|
| **WoW / FFXIV** | Quest log + tracker on HUD | Kill X, collect Y, talk to Z | Return to quest giver | **Journal tabs** (active / complete); **pin** one tracked quest |
| **Baldur’s Gate / Divinity** | Journal with entries | Multi-step; party-shared | NPC or container | **Rich text** + **sub-objectives**; quest items in inventory |
| **Octopath** | Story + side paths | Hunt species, bring item | Tavern / NPC | **Species kill** + **item delivery** map well to dungeon runs |
| **Persona** | Calendar-linked requests | Date/phase gates | Fixed NPC hours | Inspires **town phase** prerequisites (future hook to [Town time](Town-Time-And-Calendar-Requirements.md)) |
| **DCSS / roguelikes** | Minimal or branch-specific | Short fetch/kill | Temple / shop | Keep **v0 quests short**; avoid overwhelming journal in a run-based game |
| **Skyrim** | Active + completed list | World objectives | NPC | **Completed** tab read-only; optional **failed/abandoned** later |

**Recommendation (locked direction):** JRogue follows **CRPG journal + WoW clarity** for UI, but **scope v0 to 3–5 hand-authored town/dungeon quests** per run — not an MMO quest hub.

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Quest** | Authored **`QuestDefinition`** plus runtime **`QuestInstance`** state. |
| **Quest id** | Stable string key (`quest_greta_blade_sample`) — used in dialog, saves, logs. |
| **Quest giver** | NPC (or interactable) that **offers** and often **turns in** the quest. |
| **Offer** | Player **accepts** quest → moves to **Active**. |
| **Turn-in** | Player returns to **designated giver** (Enter talk → complete branch) → rewards. |
| **Party-shared quest** | One journal entry for the **whole party**; progress is shared unless an objective specifies a **required actor**. |
| **Character-bound objective** | Objective that only completes when a **specific party member** performs the action (equip, talk as, kill as killer, etc.). |
| **Objective** | Atomic progress unit (kill 3 skeletons, possess item X, talk to NPC Y). |
| **Prerequisite** | Condition that must pass **before offer** or **before accept**. |
| **Clear condition** | All **required objectives** satisfied (AND) or optional OR-groups (see §7). |
| **Quest item** | Item with **`ItemCategory.QuestItem`** (or **`PlotItem`**) tied to an objective. |
| **Run scope** | Quest progress tied to current **`DungeonRunState`** run — resets on new run (v0). |
| **Story flag** | Boolean in **`GameStoryFlagService`** — quests may set/read flags but are not flags themselves. |
| **Journal** | Full-screen overlay (inventory chrome) listing **Active**, **Completed**, and **Failed** quests. |

---

## 4. Quest taxonomy (brainstorm)

### 4.1 — By narrative role

| Type | Description | Example |
|------|-------------|---------|
| **Main story** | Gates major progression (portal, class, floor) | “Prove yourself in the dungeon before the gate opens.” |
| **Side quest** | Optional gear, gold, lore | “Bring Greta 2 iron ingots.” |
| **Tutorial** | Teaches UI/system | “Barbarian: equip Giant's Sword.” |
| **Repeatable** | Can accept again after cooldown / new run | “Daily hunt: 5 skeletons” (post-v0) |
| **Chain** | Quest B unlocks after Quest A completes | Mira → Edda follow-up |

### 4.2 — By gameplay pattern (objective templates)

| Pattern | Player experience | v0 priority |
|---------|-------------------|-------------|
| **Kill** | Slay N of species / unique enemy id | **High** |
| **Kill boss** | Slay specific spawned or map enemy once | Medium |
| **Collect / fetch** | Bring N × `ItemData` or quest item to giver | **High** |
| **Obtain** | Have item in party inventory (no turn-in yet) | Medium |
| **Talk** | Speak to NPC (Enter dialog once or specific line) | **High** |
| **Visit** | Reach tile, floor, or stamp marker | Medium |
| **Interact** | Bump lever, use altar, unlock door | Medium |
| **Survive** | Return from dungeon alive / before time expires | Low (hook [Dungeon time](Dungeon-Time-Requirements.md)) |
| **Flag** | `GameStoryFlagService` set (script or prior quest) | **High** (glue) |
| **Equip (character)** | Specific member equips item in slot | **High** |
| **Composite** | Multiple objectives in parallel or sequence | **High** |

### 4.3 — By location

| Scope | Notes |
|-------|-------|
| **Town-only** | Talk, fetch from inventory, shop-related |
| **Dungeon-only** | Kill, find item on floor |
| **Cross-scene** | Accept in town → complete in dungeon → turn in town (primary JRogue pattern) |

### 4.4 — By time (future)

| Type | Hook |
|------|------|
| **Town phase** | Only offered during Morning / Day / Night |
| **Calendar day** | Portal-window quests on days 1, 4, 7… |
| **Dungeon deadline** | Must finish before forced exit |

---

## 5. Quest lifecycle

```text
[Hidden] ──prerequisites met──► [Offered] ──accept──► [Active]
                                                      │
                    ┌─────────────────────────────────┤
                    │                                 │
                    ▼                                 ▼
              [Turn-in ready]                  [Failed] (future)
                    │
              turn-in + rewards
                    ▼
              [Completed]
```

| State | Meaning | Journal tab |
|-------|---------|-------------|
| **Hidden** | Player unaware; giver shows normal dialog | — |
| **Offered** | Dialog choice “Accept quest?” visible | — (not active until accept) |
| **Active** | Objectives tracked | **Active** |
| **ReadyToTurnIn** | All objectives done; return to giver (optional sub-state) | **Active** (highlight) |
| **Completed** | Rewards granted; read-only history | **Completed** |
| **Failed** | Quest failed (run rule or authored fail) | **Failed** |

**Locked (v0):**

| Rule | Detail |
|------|--------|
| **R5.1** | Accept is **explicit** (dialog Yes or journal accept if auto-offered). |
| **R5.2** | **No silent accept** — player must confirm. |
| **R5.3** | **Turn-in requires the designated quest giver** — player must talk to **`giverNpcId`** (adjacent + **Enter**) and choose the **Complete quest** dialog branch. Turn-in from **other NPCs is not allowed**. |
| **R5.4** | **Auto-complete** without visiting the giver allowed **only** when **`QuestDefinition.autoCompleteOnObjectives`** is true. |
| **R5.5** | **One accept per quest id per run** unless **`repeatable`** (post-v0). |
| **R5.6** | Quest **ownership** is **party-shared** — one active instance per quest id for the whole roster. |

---

## 6. How quests are given (offer sources)

### 6.1 — Design decision — dialog-first

#### Question

How should quests enter the game?

#### Recommendation (locked for v0)

| Source | v0 | Notes |
|--------|----|-------|
| **NPC dialog node** | **Yes** | Extend dialog graph with **`OfferQuest`** / **`CompleteQuest`** node actions |
| **Quest board interactable** | Defer | Town sign / board tile — same underlying `QuestService.TryOffer` |
| **Auto-grant on flag** | Defer | e.g. enter dungeon first time |
| **Item pickup** | Defer | Scroll starts quest |
| **Dungeon floor enter** | Defer | `OnFloorActivated` hook |

**Verdict:** v0 offers and turn-ins happen inside **`NpcDialogProfile`** graphs (same Enter-adjacency flow as [NPC dialog §3](NPC-Dialog-Requirements.md)).

### 6.2 — Dialog integration (v0)

| Dialog action | Effect |
|---------------|--------|
| **`OfferQuest(questId)`** | If prerequisites pass → show accept choice → `QuestService.TryAccept` |
| **`CompleteQuest(questId)`** | If active + objectives done → grant rewards → completed dialog line |
| **`SetQuestStage(questId, stage)`** | Optional multi-stage quests (post-v0) |
| **Conditional branch** | `QuestState(questId) == Active` / `Completed` in dialog conditions |

Extend **`DialogConditionKind`** (future enum values):

| Condition | Use |
|-----------|-----|
| **`QuestState`** | Active / Completed / NotStarted |
| **`QuestObjectiveProgress`** | e.g. kill count ≥ N for branch |
| **`StoryFlag`** | Already exists |

### 6.3 — Accept prerequisites (when offer appears)

Evaluated **before** showing Accept button:

| Prerequisite type | Example |
|-------------------|---------|
| **Story flag** | `portal_opened` |
| **Quest completed** | Finished `quest_intro` |
| **Quest not started** | Not active or completed |
| **Town phase / day** | Morning only (future) |
| **Party level** | ≥ 5 (future) |
| **Has item** | Shows offer only if carrying key |
| **Npc talk count** | Mira talked ≥ 1 (reuse dialog counters) |

**Implementation:** reuse **`FlagPrecondition`** pattern — new **`QuestPrecondition`** ScriptableObject list on `QuestDefinition`.

---

## 7. Objectives & clear conditions

### 7.1 — Objective model (recommended)

Each **`QuestDefinition`** contains an ordered or parallel list of **`QuestObjectiveDefinition`** (ScriptableObject or serializable structs):

```csharp
public abstract class QuestObjectiveDefinition : ScriptableObject
{
    public string objectiveId;       // stable within quest
    public string journalText;       // "Slay 3 skeletons."
    public bool optional;            // v1+ OR-groups
    public bool hiddenUntilActive;   // spoiler control
    public QuestActorRequirement actorRequirement; // None = any party member / party-wide
}
```

**`QuestActorRequirement`** (locked):

| Value | Meaning |
|-------|---------|
| **`None`** | Party-wide (any member's action counts) |
| **`PartyMemberId`** | Match stable member id / prefab roster key (e.g. `BarbarianWarrior`) |
| **`ActiveLeader`** | Only while that member is party leader |
| **`HumanClass` / `Race`** | Match `CharacterStats` (future fine-grained gates) |

Example: quest **`quest_barbarian_blade`** is **party-shared**, but objective **`equip_giants_sword`** sets **`actorRequirement = PartyMemberId("BarbarianWarrior")`** — only that member equipping **`Giants_Blade`** completes it. Journal shows: *“Barbarian: Equip Giant's Sword (0/1)”*.

**Runtime progress** stored per quest instance:

```csharp
public struct QuestObjectiveProgress
{
    public string objectiveId;
    public int current;
    public int required;
    public bool completed;
}
```

### 7.2 — Objective types (v0 set)

| Type | Tracks | Completion | Event source |
|------|--------|------------|--------------|
| **`KillSpeciesObjective`** | `EnemySpeciesDefinition.speciesId` | `current >= count` | `OnEnemyDeath` |
| **`KillUniqueObjective`** | Spawn id / `enemyInstanceId` | once | `OnEnemyDeath` (match id) |
| **`CollectItemObjective`** | `ItemData` + qty | party total ≥ qty | inventory scan on change |
| **`DeliverItemObjective`** | quest item + qty | turn-in removes items | `QuestService.TryTurnIn` |
| **`TalkToNpcObjective`** | `npcId` | dialog started or completed | `NpcDialogSession` |
| **`VisitCellObjective`** | floor id + cell | leader on cell | move hook |
| **`VisitFloorObjective`** | `floorId` | first visit | `DungeonFloorInstanceManager` |
| **`StoryFlagObjective`** | flag id | flag set | `GameStoryFlagService` |
| **`InteractObjective`** | interactable id | bump activated | `InteractableTileService` |

| **`InteractObjective`** | interactable id | bump activated | `InteractableTileService` |
| **`EquipItemObjective`** | `ItemData` + `EquipmentSlot` | required member has item equipped | `EquipmentManager` equip hook |

**Locked (v0 clear rule):** All **non-optional** objectives must be **complete** (logical **AND**). Optional objectives grant **bonus rewards** only (post-v0).

### 7.3 — Party-shared quests, character-bound objectives (locked)

| Rule | Detail |
|------|--------|
| **R7.3.0** | **All quests are party-shared** — one **`QuestInstance`** per quest id; all members see the same journal entry and objective list. |
| **R7.3.0a** | Individual objectives may set **`actorRequirement`** so only a **specific party member** can satisfy that step. |
| **R7.3.0b** | Journal text should name the required member when bound (e.g. *“Barbarian: Equip Giant's Sword”*). |
| **R7.3.0c** | Switching active party leader **does not** reset shared quest progress. |
| **R7.3.0d** | If required member **dies** ([Party member death](../Party/Party-Member-Death-Requirements.md)), character-bound objectives remain **incomplete** until revived (no revive v0) or quest **fails** (authored fail rule, post-v0). |

### 7.4 — Kill quest specifics

| Rule | Detail |
|------|--------|
| **R7.4.1** | **Species kill** counts when **`actorRequirement`** passes for the **killer** (or any party member if `None`). Default killer = party member who dealt killing blow. |
| **R7.4.2** | **Unique kill** uses **`enemyInstanceId`** on spawn or authored boss flag — avoids counting wrong skeleton. |
| **R7.4.3** | Journal shows **`Slain: 2 / 3`** updated on kill event. |

### 7.5 — Equip objectives (character-bound example)

| Rule | Detail |
|------|--------|
| **R7.5.1** | **`EquipItemObjective`**: completes when **`actorRequirement`** member has **`ItemData`** equipped in authored slot (usually `MainHand`). |
| **R7.5.2** | Unequip after complete **does not** un-complete the objective (latched on first valid equip). |
| **R7.5.3** | Example: party accepts **`quest_barbarian_blade`**; only **`BarbarianWarrior`** equipping **`Giants_Blade`** ticks progress. |

### 7.6 — Fetch / deliver item specifics

| Rule | Detail |
|------|--------|
| **R7.6.1** | **Collect** objective: progress = sum of **`ItemData`** quantities across **all party members** (carried + subspace v1; carried only v0) unless **`actorRequirement`** limits to one member's inventory. |
| **R7.6.2** | **Deliver** objective: on turn-in at **giver**, **`QuestService`** removes required items from party (prefer **quest item instances** bound to quest id). |
| **R7.6.3** | **`QuestItem`** vs **`PlotItem`** — see §11 (locked distinction). |
| **R7.6.4** | **Drop** quest item: warn confirm (reuse destructive drop config); dropping **does not** fail quest v0 — player must re-acquire. |
| **R7.6.5** | **Forced dungeon exit** retains quest items on living members ([Dungeon time §G6](Dungeon-Time-Requirements.md)). |
| **R7.6.6** | Optional: **`ItemInstance.questBindingId`** links drop to quest for “this exact MacGuffin” fetch. |

### 7.7 — Talk / visit objectives

| Rule | Detail |
|------|--------|
| **R7.7.1** | **Talk** completes when **`actorRequirement`** member starts dialog (or **`ActiveLeader`** only if configured). |
| **R7.7.2** | **Visit cell** uses required member's grid position if **`actorRequirement`** set; else **party leader**. |
| **R7.7.3** | Visits in **safe zone** count normally. |

---

## 8. Journal UI

### 8.1 — Access

| Rule | Detail |
|------|--------|
| **R8.1** | **`J`** opens **Quest Journal** (configurable in `GameControls`; secondary: menu button post-v0). |
| **R8.2** | Available in **town and dungeon** when no higher-priority modal (`BlocksGameplay` stack). |
| **R8.3** | **Does not consume a turn** (hub UI like inventory browse). |
| **R8.4** | **`Esc`** closes journal. |

### 8.2 — Layout & visual design (locked)

**Chrome:** Reuse **inventory overlay** frame — same outer margin, panel borders, and typography as [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (`InventoryUI` / `InventoryPanel` patterns). **Not** the bottom dialog box frame.

Reference: *BG3* journal / *WoW* quest log — **scrollable** list left, detail right.

```
┌─────────────────────────────────────────────────────────────┐
│  QUESTS                          [Active|Completed|Failed]   │
├──────────────────┬──────────────────────────────────────────┤
│ ▌ Hunt Skeletons │  Hunt the undead                          │
│   Greta's Errand │  Greta needs proof you can handle the pit. │
│ 📌 Barbarian Blade│                                          │
│   (scroll…)      │  Objectives:                              │
│                  │    ☑ Talk to Greta                        │
│                  │    ☐ Barbarian: Equip Giant's Sword (0/1) │
│                  │    ☐ Slay skeletons (1/3)               │
│                  │                                           │
│                  │  Rewards: 50 gold, Iron Ring              │
│                  │  Giver: Greta (town) — return to turn in  │
└──────────────────┴──────────────────────────────────────────┘
```

| Element | Spec |
|---------|------|
| **Tabs** | **Active** (default), **Completed**, **Failed** |
| **List** | **Scrollable** vertical list — **no cap** on active quests |
| **Sort** | **Pinned** quests first (📌), then accepted order; within unpinned, optionally **newest accepted first** as soft auto-track |
| **Pin** | Player may **pin one or more** quests to top (`P` or context action); pinned entries stay above unpinned when scrolling |
| **Detail pane** | Title, description, objective list with progress, reward summary, giver hint |
| **Status borders** | Each list row uses a **distinct border color** by quest state so status is visible at a glance (see §8.3) |
| **Empty states** | “No active quests.” / “No completed quests this run.” / “No failed quests.” |
| **Turn-in hint** | When **ReadyToTurnIn**, show **“Return to {giverName}”** in accent text |
| **Objective complete feedback** | **`[Quest]` log line only** — no voice barks v0 |

### 8.3 — Status borders (locked)

| Quest state | List row border | Notes |
|-------------|-----------------|-------|
| **Active** | **Amber / gold** accent (e.g. `#c8a060`) | In-progress |
| **ReadyToTurnIn** | **Bright gold** + optional glow | Sub-state of Active; same tab |
| **Completed** | **Muted green** (e.g. `#4a8a5a`) | Completed tab |
| **Failed** | **Muted red** (e.g. `#8a4a4a`) | Failed tab |

Detail pane header repeats the same border accent as the selected row.

### 8.4 — Completed & Failed tabs

| Rule | Detail |
|------|--------|
| **R8.4.1** | **Completed** tab: read-only; newest first; shows rewards **received**. |
| **R8.4.2** | **Failed** tab: read-only; shows fail reason string when authored; **muted red** border. |
| **R8.4.3** | Optional **“Hide completed”** filter post-v0. |

### 8.5 — Pin behavior (locked)

| Rule | Detail |
|------|--------|
| **R8.5.1** | **Manual pin:** player toggles pin on any **Active** quest; pinned quests sort to **top**. |
| **R8.5.2** | **Soft auto-track:** newly **accepted** quest scrolls into view and may show a subtle **“New”** marker until dismissed — does **not** replace manual pin. |
| **R8.5.3** | Multiple pins allowed; order among pins = pin time. |
| **R8.5.4** | HUD quest tracker strip (objective text on gameplay HUD) is **post-v0**; v0 is journal-only. |

---

## 9. Rewards

### 9.1 — Reward bundle

On successful turn-in (or auto-complete):

| Reward type | v0 | Implementation |
|-------------|----|----------------|
| **Gold** | Yes | `PartyCurrencyLedger.Add` |
| **Item** | Yes | `InventoryManager.AddItem` to active shopper / leader |
| **Party XP** | Optional | `PartyExperienceService` |
| **Story flag** | Yes | `GameStoryFlagService.Set` |
| **Unlock quest** | Yes | Sets **`offerPrerequisites`** on other quests (implicit) |
| **Reputation** | Defer | — |
| **Town phase advance** | Defer | Hook [Town time](Town-Time-And-Calendar-Requirements.md) |

**Locked:**

| Rule | Detail |
|------|--------|
| **R9.1** | Rewards grant **atomically** — all or none (transaction rollback on inventory full). |
| **R9.2** | Show **reward popup** or dialog line before closing turn-in (“Received: 50 gold”). |
| **R9.3** | If inventory full for item reward → **block turn-in** with message; keep quest **ReadyToTurnIn**. |

### 9.2 — Optional objective bonuses

Post-v0: bonus gold for optional objectives (e.g. “kill extra 2 skeletons”).

---

## 10. Data & services

### 10.1 — `QuestDefinition` (ScriptableObject)

| Field | Purpose |
|-------|---------|
| **`questId`** | Stable id |
| **`displayTitle` / `journalDescription`** | UI strings |
| **`giverNpcId`** | Turn-in target + journal hint |
| **`acceptPrerequisites[]`** | Offer gates |
| **`objectives[]`** | Progress templates |
| **`rewards`** | Gold, items, flags, xp |
| **`autoCompleteOnObjectives`** | Skip turn-in |
| **`setsFlagsOnAccept / setsFlagsOnComplete`** | Story integration |
| **`sortOrder`** | Journal list tie-break among unpinned quests |
| **`pinnedQuestIds`** | Player pin order (runtime, journal UI — stored in `QuestService` / UI prefs) |

Asset path: `Assets/Data/Quest/`.

### 10.2 — `QuestService` (DDOL run layer)

Mirror pattern: `TownShopStateService`, `TownTimeService`.

| API | Behavior |
|-----|----------|
| **`TryOffer(questId, out denyReason)`** | Prerequisites for showing offer |
| **`TryAccept(questId, out denyReason)`** | Hidden → Active |
| **`TryTurnIn(questId, npcId, out denyReason)`** | **`npcId` must match `giverNpcId`** + objectives done + consume items + grant rewards |
| **`PinQuest` / `UnpinQuest`** | Journal sort |
| **`GetActiveQuests()` / `GetCompletedQuests()`** | Journal |
| **`GetProgress(questId, objectiveId)`** | UI + dialog |
| **`NotifyEnemyKilled(speciesId, uniqueId)`** | Objective hook |
| **`NotifyInventoryChanged()`** | Re-scan collect objectives |
| **`ResetForNewRun()`** | Called from `DungeonRunState.BeginRun` |

**Persistence (v0):** in-memory on DDOL service for run; **not** saved to disk mid-run.

### 10.3 — Relationship to `GameStoryFlagService`

| Layer | Use |
|-------|-----|
| **Flags** | Low-level booleans (`talked_npc_1`, `lever_a_on`) — dialog, levers, doors |
| **Quests** | Structured objectives + journal + rewards |
| **Bridge** | Quest complete **sets** flags; flag objectives **read** flags; dialog conditions can use either |

**Do not** encode kill counts in raw flags — use **`QuestService`** progress.

---

## 11. Inventory & quest items — `QuestItem` vs `PlotItem` (locked)

| Category | Role | Turn-in consume? | Shop sell? | Example |
|----------|------|------------------|------------|---------|
| **`QuestItem`** | Fetch / delivery payload for a quest objective | **Yes** — removed on deliver turn-in | **No** | Greta's crate of ore, bundled delivery package |
| **`PlotItem`** | Story key, signet, letter — persists across objectives | **No** — kept after quest (unless authored destroy) | **No** | Royal signet, dungeon key quest MacGuffin |

| Rule | Detail |
|------|--------|
| **R11.1** | **`DeliverItemObjective`** consumes **`QuestItem`** stacks/instances on giver turn-in. |
| **R11.2** | **`PlotItem`** may gate dialog or doors via **`HasItem`** prerequisites; completing quest **sets flags** rather than consuming the item (default). |
| **R11.3** | Stackable fetch (10 herbs): regular **`ItemData`** + **`CollectItemObjective`** — no special category required. |
| **R11.4** | Both categories show **quest badge** in inventory inspect (“For: {quest title}” / “Story item”). |
| **R11.5** | Shop **sell** blocked for both ([Shop NPCs §7.1](Shop-NPC-Requirements.md)). |
| **R11.6** | **Junk mark** automation must not mark quest or plot items. |
| **R11.7** | Turn-in **`acceptGenericStacks`**: when true, deliver may consume matching **`ItemData`** even if not `QuestItem`. |
| **R11.8** | Optional **`ItemInstance.questBindingId`** for unique **`QuestItem`** instances. |

---

## 12. Example quests (content brainstorm)

| Id | Giver | Flow | Objectives | Rewards |
|----|-------|------|------------|---------|
| **`quest_mira_intro`** | Mira | Talk chain | Talk to Luc | Flag `met_luc` |
| **`quest_skeleton_proof`** | Town guard NPC | Town → dungeon → town | Kill 3 `skeleton` | 30 gold |
| **`quest_barbarian_blade`** | Town trainer NPC | Town | **BarbarianWarrior:** equip `Giants_Blade` (character-bound) | 20 gold |
| **`quest_greta_fetch`** | Greta (shop) | Accept → find item in dungeon → **return to Greta** | Deliver 1 **`QuestItem`** crate | 80 gold |
| **`quest_lever_secrets`** | Edda | After flag | Bump town time lever once | Lore flag |
| **`quest_portal_ready`** | Portal NPC | Main gate | Complete 2 side quests + talk at giver | Flag `portal_blessed` |

These reuse existing systems (species id, shop NPC, levers, flags).

---

## 13. Failure, abandon, and edge cases

| Scenario | v0 behavior |
|----------|-------------|
| **Giver NPC unavailable** | Turn-in **only** at **`giverNpcId`**; journal still shows **Return to {giver}** |
| **Wrong NPC complete branch** | `CompleteQuest` on non-giver → dialog line “You should speak to {giverName}.” |
| **Party member death** | Shared progress kept; **character-bound** objectives stuck if required member dead |
| **Forced dungeon exit** | Active quests persist; kill progress kept; collect items kept on survivors |
| **Duplicate accept** | Rejected with log |
| **Turn-in without items** | Blocked with message |
| **Abandon quest** | **Not v0** — add `TryAbandon` post-v0 |

---

## 14. Acceptance criteria (v0 target)

| ID | Test |
|----|------|
| **AC1** | NPC dialog offers quest → Accept → appears in journal **Active** tab. |
| **AC2** | Kill objective increments on skeleton death; journal shows `1/3`. |
| **AC3** | Collect objective completes when party holds required qty. |
| **AC4** | Turn-in at **giver only** removes deliver items, grants gold, moves quest to **Completed**. |
| **AC5** | Completed / Failed quests use **distinct list borders**; appear on correct tab. |
| **AC6** | **`QuestItem`** cannot be sold at shop; **`PlotItem`** retained after plot quest. |
| **AC7** | Accept blocked when prerequisite flag missing. |
| **AC8** | Quest state persists after dungeon → town portal travel. |
| **AC9** | **Barbarian-only equip** objective completes only when **`BarbarianWarrior`** equips target weapon. |
| **AC10** | Player **pins** quest → stays at top of scroll list after accepting others. |

---

## 15. Implementation phases

| Phase | Deliverables |
|-------|--------------|
| **P0 — Core** | `QuestDefinition`, `QuestService`, kill + flag + talk + **equip** objectives, **`actorRequirement`**, dialog offer/complete nodes, journal (**Active** tab, scroll, borders, pin) |
| **P1 — Fetch** | Collect / deliver objectives, **QuestItem** vs **PlotItem**, inventory hooks |
| **P2 — Polish** | **Completed** + **Failed** tabs, reward popup, visit objectives, HUD tracker (optional) |
| **P3 — Hub depth** | Town phase prerequisites, quest board, repeatable quests |

---

## 16. Resolved design decisions

| # | Decision | Locked answer |
|---|----------|---------------|
| **Q1** | Party vs per-member quests | **Party-shared** quest instances; objectives may require a **specific character** via **`actorRequirement`** (e.g. Barbarian equips Giant's Sword). |
| **Q2** | Tracking | **Both:** player **pins** quests to top; newly accepted quests get soft **“New”** / scroll-into-view (optional marker). |
| **Q3** | Journal chrome | **Inventory overlay frame** — match `InventoryUI` panel styling. |
| **Q4** | Objective complete feedback | **Log only** (`[Quest]` prefix). **Status borders** on journal rows: **Active** (amber), **Completed** (green), **Failed** (red). |
| **Q5** | Active quest cap | **No cap** — journal list is **scrollable**. |
| **Q6** | **`PlotItem`** vs **`QuestItem`** | **Yes — distinct** (§11): `QuestItem` consumed on deliver; `PlotItem` story key, not consumed by default. |
| **Q7** | Turn-in location | **Giver-only turn-in is required** — player must return to **`giverNpcId`** and complete via dialog; other NPCs cannot turn in. Exception: **`autoCompleteOnObjectives`**. |

---

## 17. Implementation checklist (when approved)

- [x] `QuestDefinition` + objective ScriptableObject types
- [x] `QuestService` DDOL + run reset hook
- [x] Event hooks: enemy death, inventory change, dialog, floor activate
- [x] Dialog graph: offer / complete / quest condition nodes
- [x] `QuestJournalUI` — Active / Completed / Failed tabs; inventory chrome; scroll; pin; status borders
- [x] `EquipItemObjective` + **`QuestActorRequirement`**
- [ ] Inventory quest item badge + drop confirm
- [x] Unit tests: accept, kill progress, turn-in transaction, prerequisites
- [ ] Sample content: 1 kill + 1 fetch quest in town/dungeon test scenes
- [ ] Cross-link from [NPC dialog](NPC-Dialog-Requirements.md) (remove “quest journal out of scope” when shipped)
