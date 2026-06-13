# Human Mage — Spells & spellbooks (requirements)

**Purpose:** Specify how **Human Mages** (`Race.Human`, `HumanClass.Mage`) **become** Mages, then **learn**, **equip**, and **cast** arcane spells. Class commitment is a **one-way town quest** (pay **5 gold** to a **Mage Tutor**). Spell learning is **Dungeon Crawl Stone Soup–style**: read a **spellbook** to add spells to the known library; spellbooks are **destroyed on use**.

**Status:** Implemented (v0).

**Depends on:** [Human — Class powers](Human-Class-Powers-Requirements.md) (Mage commitment, essence/Soul Power prohibition, Magic Power pools, `HumanMageSpellsRuntime`), `HumanClassCommitment`, `MageSpellDefinition`, `HumanClassRules`, `ItemCategory.Spellbook`, [Quest system](../World/Quest-Requirements.md), [NPC dialog](../World/NPC-Dialog-Requirements.md), [Inventory UI](../Inventory/Inventory-UI-Redesign-Requirements.md), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (`HotbarEntryKind.HumanMageSpell`), [Town shop NPCs](../World/Shop-NPC-Requirements.md), [Safe zone](../World/Safe-Zone-Requirements.md), [Fireball scroll](../Inventory/Fireball-Scroll-Requirements.md) (consumable scroll ≠ spellbook), [Dragonian — Spell memory](Dragonian-Spell-Memory-Requirements.md) (contrast — Dragonian spells are race-exclusive).

**Related:** [Human — Class powers](Human-Class-Powers-Requirements.md) (STBGB class model), [Area ability splash targeting](../Combat/Area-Ability-Splash-Targeting-Requirements.md) (Fireball AoE preview), [Sudden Strength essence](../Essence/Sudden-Strength-Essence-Requirements.md) (essence pattern Mages **cannot** use), [Safe zone](../World/Safe-Zone-Requirements.md) (town-only equip changes — same discipline as Dragonian loadout).

**Explicitly out of scope (v0):** Human Mage **racial abilities menu** UI (`K` body — later; equip via debug/preset or minimal UI); **unlearn** / respec known library; spell **mastery ranks**; identifying cursed spellbooks; spell failure on read; writing your own spellbooks; casting from the inventory without equipping; gamepad layout; cross-class spell theft; learning **Dragonian** or other folk spells; Knight/Priest class-commitment quests (separate future docs).

---

## Locked decisions (user)

| # | Decision |
|---|----------|
| **L1** | Only **Human Mages** learn and cast Human Mage spells (`MageSpellDefinition`). |
| **L2** | Mages **cannot consume essences** (*STBGB*) — **equipping an essence is consuming an essence**; no essence slots, no Soul Power, no essence actives; **Magic Power** instead. |
| **L3** | Spells are **learned** by **reading spellbooks** (DCSS-style), not by level-up alone. |
| **L4** | Each spell may be **learned once** per mage; duplicate learn attempts are skipped (idempotent). |
| **L5** | A spellbook lists **one or more** spells; the mage may **use** the book only if it contains **≥ 1 spell not yet known**. |
| **L6** | Spellbooks are **consumed** (removed from inventory) on successful **Read**. |
| **L7** | Spell **tier** is **1 … 9** (**1 = highest**, **9 = lowest**). Tier sets base equip cost; spells may add **`extraEquipCost ≥ 0`**. |
| **L8** | Total equip cost per spell: **`(10 - tier) + extraEquipCost`**. Equipped loadout must satisfy **Σ equipCost ≤ MaxMagicPower**. |
| **L9** | **Cast cost** is per-spell **`magicPowerCost`**, paid from **`currentMagicPower`** on successful cast — independent of equip cost. |
| **L10** | Mages **cannot learn Dragonian spells** (`DragonianSpellDefinition`) or any non–Human-Mage spell type. |
| **L11** | v0 sample spells: **Arcane Might** (ally Strength buff), **Fireball** (AoE fire), **Lightning Bolt** (single-tile lightning damage). Each has **distinct** equip and cast costs. |
| **L12** | v0 spellbooks: **one book per sample spell**; all three sold by one **town shop NPC** for **1 gold** each (QA bootstrap). |
| **L13** | Class commitment to **Mage** requires **`HumanClass.None`** — cannot change from **Knight** or **Priest**. |
| **L14** | **Consumed essences block** Mage training — no essence may be **equipped** (equipped = consumed) at accept/turn-in. **Unconsumed** essence **items** in inventory do not block training. After commit, the mage **cannot consume** (equip) essences. |
| **L15** | On Mage commit: **`maxEssenceSlots = 0`**, **`MaxSoulPower` / `currentSoulPower = 0`**, **`MaxMagicPower` / `currentMagicPower`** initialized per Mage rules. |
| **L16** | v0 Mage commitment gate: **town quest** from a **Mage Tutor NPC** — pay **5 gold** on turn-in to become a Mage. |
| **L17** | **Mage Tutor** (class) and **Arcane Vendor** (spellbooks) are **separate NPCs** — tutor first, spell shopping after commitment. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **DCSS clarity** — Finding a spellbook, reading it, and seeing new spells in the known library is the core Mage progression loop. |
| **G2** | **Separate learn vs equip vs cast** — Reading adds to **known**; **equip** spends Magic Power **budget**; **cast** spends **current** Magic Power. |
| **G3** | **Race/class exclusivity** — Engine rejects Dragonian (and other) spell pipelines on Human Mages at validation time. |
| **G4** | **Testable v0 content** — Three spells + three spellbooks + shop stock prove learn → equip → hotbar → cast end-to-end. |
| **G5** | **Extensible acquisition** — Spellbook item type supports shop, loot, and quest rewards without changing learn rules. |
| **G6** | **Safe degradation** — Invalid saves (known spell missing definition, over-budget equip) clamp with warnings on load. |
| **G7** | **Class gate clarity** — Players understand they must be **unclassed**, have **consumed no essences** (none equipped), and pay the **tutor** before spellbooks matter. |
| **G8** | **Permanent choice** — Mage commitment is **one-way**; messaging blocks Knight/Priest reroll. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Mage spell** | `MageSpellDefinition`: ability payload + **tier** + **extraEquipCost** + **magicPowerCost**. |
| **Known spells** | Permanent library on `HumanMageSpellsRuntime` after learning. |
| **Equipped spells** | Subset of known spells active for casting / hotbar assign. |
| **Equip cost** | `(10 - tier) + extraEquipCost` — loadout budget only. |
| **Cast cost** | `magicPowerCost` deducted from `currentMagicPower` on successful execution. |
| **Spellbook** | Inventory item (`ItemCategory.Spellbook`) referencing one or more mage spells by stable **`spellId`**. |
| **Read (spellbook)** | Inventory action that learns all **unknown** spells listed in the book, then **destroys** the book. |
| **Magic Power budget** | `MaxMagicPower - Σ equipCost(equipped)` = **`RemainingEquipCapacity`**. |
| **Class commitment** | Irreversible `HumanClass.None` → **`HumanClass.Mage`** via tutor quest (§5). |
| **Mage Tutor** | Town NPC offering the **Arcane Apprenticeship** quest (§5). |
| **Consume (essence)** | **Equip** an essence into an essence slot — **equipping and consuming are the same operation** in JRogue. Executes essence actives from equipped slots. **Forbidden** for Mages after commit. |
| **Consumed essence** | An `EssenceData` bound to an essence slot on `EssenceSlotManager` (synonym: **equipped essence**). |
| **Essence item (inventory)** | An unconsumed `ItemInstance` of essence category in bag/subspace — not yet **equipped/consumed**; does **not** block Mage training. |

---

## 3. Mage power economy (summary)

Full rules live in [Human — Class powers §8](Human-Class-Powers-Requirements.md). This doc adds **learning** only.

| Resource | Mage | Knight / None | Priest |
|----------|------|---------------|--------|
| **Essences** | **Forbidden** — Mages **cannot consume** (equip) essences (*STBGB*); see §5.2.1 | Allowed | **Forbidden** |
| **Soul Power** | **0** | Yes | **0** |
| **Divine Power** | **0** | **0** | Yes |
| **Magic Power (max)** | **`Intelligence × 5 + levelMagicPowerBonus`** (v0) | **0** | **0** |
| **Magic Power (current)** | Spent on **cast** | **0** | **0** |

**Invariant:** Equipping spells **does not** reduce `currentMagicPower`. Casting **does not** free or consume equip capacity.

---

## 4. Exclusivity & validation

### 4.1 — Who may learn / cast Mage spells

| Check | Rule |
|-------|------|
| Race | `CharacterStats.race == Race.Human` |
| Class | `CharacterStats.humanClass == HumanClass.Mage` |
| Subsystem | `RacialSubsystemKind.HumanSpecialization` |
| Runtime | `HumanMageSpellsRuntime` present |

### 4.2 — What Mages cannot learn or cast

| Blocked | Reason |
|---------|--------|
| `DragonianSpellDefinition` / `HotbarEntryKind.DragonianSpell` | Dragonian-only ([Dragonian spell memory §4](Dragonian-Spell-Memory-Requirements.md)) |
| Priest skill actives | Wrong class pipeline |
| Essence actives | Mage has **0** essence slots |
| Raw `AbilityAction` not bound to a **known Mage spell** in equipped set | Spells must flow through `HumanMageSpellsRuntime` |

### 4.3 — What other folk cannot do

- Non–Human Mages **cannot** learn, equip, or execute `MageSpellDefinition` entries (dev override logs warning).
- **Dragonians** use `DragonianSpellsRuntime` — never `HumanMageSpellsRuntime` spell learn.

### 4.4 — Spellbooks vs scrolls

| Item | Who uses | Effect |
|------|----------|--------|
| **Spellbook** | **Human Mage** only | Adds spells to **known library**; book destroyed |
| **Scroll** (e.g. Fireball scroll) | Any eligible actor per scroll rules | **One-shot cast**; does **not** teach Mage spells |

A Human Mage **may** still use a Fireball **scroll** as a consumable without learning the Mage spell — separate systems.

---

## 5. Class commitment — Mage apprenticeship quest

Humans begin as **`HumanClass.None`**. Becoming a **Mage** is a **permanent, one-way** commitment gated by a **simple town quest**: pay **5 gold** to a **Mage Tutor**. Until commitment succeeds, the actor **cannot** read Mage spellbooks (§8) or equip Mage spells.

Parent rules: [Human — Class powers §5–§8](Human-Class-Powers-Requirements.md). This section specifies the **Mage-only** gate and quest content.

### 5.1 — Allowed transitions (locked)

| From | To Mage | Allowed |
|------|---------|---------|
| **`HumanClass.None`** | **Mage** | **Yes**, once, when §5.2 prerequisites pass and §5.4 quest completes |
| **`HumanClass.Knight`** | **Mage** | **No** — permanent class |
| **`HumanClass.Priest`** | **Mage** | **No** — permanent class |
| **`HumanClass.Mage`** | **Mage** | N/A — already committed |

Engine enforcement already exists in `HumanClassRules.CanCommitToClass` (`from` must be `None`). This doc adds **essence** and **quest** gates before `HumanClassCommitment.TryCommit`.

### 5.2 — Prerequisites (accept + turn-in)

All must pass for the **active party member (speaker)** at talk time:

| Check | Rule | Failure message (template) |
|-------|------|----------------------------|
| **Race** | `Race.Human` | *“Only humans may train as mages here.”* |
| **Class** | `humanClass == HumanClass.None` | *“You have already committed to another path.”* |
| **Consumed essences** | **Zero** — no essence equipped in any slot (`EssenceSlotManager`; **equip = consume**) | *“You must relinquish all consumed essences before you can study the arcana.”* |
| **Location** | **Safe zone (town)** — not dungeon, not combat | *“You can only begin arcane training in town.”* |
| **Gold (turn-in)** | Party **`PartyCurrencyLedger` ≥ 5** | *“The tutor requires 5 gold for initiation.”* |

**Consumed essences (locked — *STBGB*):**

```text
CountConsumedEssences(actor) == 0   // i.e. every essence slot empty
CountEquippedEssences(actor) == 0   // same check — equip ≡ consume
```

**Terminology (locked):** In all Human Mage / class-power docs, **“equip an essence”** and **“consume an essence”** mean the same thing: bind `EssenceData` into an `EssenceSlotManager` slot. UI copy may say either word; validation uses slot occupancy.

**Design intent (*Surviving the Game as a Barbarian*):** arcane study requires **relinquishing essence consumption**. A mage **does not consume essences** — the essence slot pipeline is incompatible with Magic Power. The tutor therefore insists the aspirant have **no essences equipped/consumed** before initiation.

| Scope | Blocks training? | After Mage commit |
|-------|------------------|-------------------|
| **Consumed / equipped** essences in slots | **Yes** — must unequip (unconsume) first | **Impossible** (`totalSlots == 0`) |
| **Unconsumed** essence **items** in inventory / subspace | **No** | May **carry** as loot; **cannot consume** (equip) |

- Player must **unequip** (unconsume) essences manually before accept/turn-in — the quest does **not** auto-strip slots or destroy inventory essence items.
- **Inventory essence items** are inert for Mages: consume/equip UI disabled; attempts fail with *“Mages cannot consume essences.”*

#### 5.2.1 — Mages cannot consume essences (locked)

Mirrors [Human — Class powers §M8.1](Human-Class-Powers-Requirements.md) and *STBGB* fiction. **Equip = consume** throughout.

| Action | Human `None` / Knight | Human **Mage** |
|--------|----------------------|----------------|
| **Consume (equip)** essence into slot | Yes (if slots > 0) | **No** — `maxEssenceSlots == 0` |
| Execute actives from **consumed** (equipped) slots | Yes | **No** |
| Carry **unconsumed** essence **item** in bag | Yes | **Yes** — sell, drop, or stash |

**Training gate vs post-commit:** only **consumed/equipped** essences are checked before the tutor quest. **Unconsumed** essence items in inventory are not consumption.

**Not required (v0):**

- Empty inventory of essence items.
- Minimum character level.
- Story flags (may add later).

### 5.3 — Bootstrap on successful commit

Call **`HumanClassCommitment.TryCommit(actor, HumanClass.Mage)`** after gold is deducted. Expected state change (existing bootstrap + this doc):

| Field / system | After commit |
|--------------|--------------|
| **`humanClass`** | **`HumanClass.Mage`** |
| **`racialSubsystem`** | **`HumanSpecialization`** |
| **`EssenceSlotManager.totalSlots`** | **0** |
| **Consumed / equipped essences** | **0** (validated pre-commit; load/save may strip if illegal) |
| **`MaxSoulPower`** | **0** |
| **`currentSoulPower`** | **0** |
| **`MaxMagicPower`** | **`Intelligence × 5 + levelMagicPowerBonus`** (v0 formula) |
| **`currentMagicPower`** | **`MaxMagicPower`** (full on commit via `RefreshResourcePoolsToMax`) |
| **`MaxDivinePower` / current** | **0** |
| **`HumanMageSpellsRuntime`** | Present; **known** list empty unless preset; **equipped** list empty |
| **Hotbar** | Mage spell entries stale until player assigns after learning/equipping |

**Player-facing turn-in copy (template):**

> *“{speakerName} surrenders mortal attunements and accepts the burden of arcana. Magic Power flows where Soul Power once lived. Seek spellbooks to fill your grimoire.”*

### 5.4 — Quest: Arcane Apprenticeship (v0)

| Field | Value |
|-------|--------|
| **`questId`** | `quest_mage_tutor_apprenticeship` |
| **Title** | Arcane Apprenticeship |
| **Giver / turn-in** | **Mage Tutor** NPC (`human_mage_tutor`) |
| **Ownership** | **`PerPartyMember`** — each Human in the roster commits separately (mirror [Dragonian Elder per-member quests](Dragonian-Spell-Learning-Elder-Quests-Requirements.md)) |
| **Accept prerequisites** | §5.2 (except gold — checked at turn-in) |
| **Objectives (v0)** | **None** — accept → return to tutor → turn-in with payment |
| **Turn-in cost** | **5 gold** deducted from **`PartyCurrencyLedger`** atomically with rewards |
| **Primary reward** | **`commitHumanClass: Mage`** on **quest owner** (new quest reward type — see §5.5) |
| **Secondary rewards** | None in v0 |
| **Repeat** | **No** — completed permanently for that member |
| **Journal summary** | *“Pay the Mage Tutor 5 gold to begin arcane training.”* |

**Dialog flow (v0):**

```
Enter (adjacent + facing Mage Tutor, town safe zone)
  → if speaker already Mage: “You already walk the arcane path.”
  → if speaker Knight/Priest: “You have already committed to another path.”
  → if speaker has consumed (equipped) essences: “Relinquish your consumed essences before I can teach you. A mage cannot consume essences.”
  → if quest not started: offer “Begin apprenticeship?” [Accept] [Decline]
  → Accept → quest Active (PerPartyMember instance for speaker)
  → Active + at tutor: “Complete training (5 gold)?” [Pay & commit] [Not yet]
  → Pay & commit:
        validate §5.2 + gold ≥ 5
        deduct 5 gold
        HumanClassCommitment.TryCommit(owner, Mage)
        complete quest → Completed tab
```

**Turn cost:** v0 — **no turn** (same as shop / inventory management).

### 5.5 — Quest system extensions

| Extension | Requirement |
|-----------|-------------|
| **`QuestOwnership.PerPartyMember`** | Reuse Dragonian Elder pattern if already implemented; else add for this quest |
| **`requiredRace`** | `Race.Human` on accept + turn-in |
| **`requiredHumanClass`** | **`HumanClass.None`** on accept + turn-in (new precondition field or custom evaluator) |
| **`requiresNoEquippedEssences`** | **true** for this quest (new flag on `QuestDefinition` or hard-coded in `HumanMageTutorQuestLogic`) |
| **Turn-in gold cost** | **`turnInGoldCost: 5`** (new field) **or** reward script that deducts before `commitHumanClass` |
| **`commitHumanClass` reward** | Enum: `HumanClass.Mage` — invokes `HumanClassCommitment.TryCommit(questOwner, Mage)`; quest fails turn-in if commit fails |

**Atomic turn-in order:**

1. Validate owner + prerequisites + gold.
2. Deduct **5 gold**.
3. `TryCommit` → on failure, **refund gold** and abort (transactional).
4. Mark quest **Completed**; set optional story flag `human_mage_apprenticeship_complete_{partyMemberId}` for dialog.

### 5.6 — Mage Tutor NPC (v0)

| Field | Value |
|-------|--------|
| **Stable id** | `human_mage_tutor` |
| **Display name** | Mage Tutor *(narrative name TBD — e.g. “Sage Aldric”)* |
| **Race sprite** | Human, distinct robe/staff portrait |
| **Marker** | `town_npc_mage_tutor` in plaza stamp (≥ 2 cells from Arcane Vendor §11) |
| **Role** | **Class commitment only** — does **not** sell spellbooks |
| **Interaction** | Standard `Enter` talk + quest dialog (`NpcTalkInteraction`) |

**Relationship to Arcane Vendor (§11):**

| NPC | When | Purpose |
|-----|------|---------|
| **Mage Tutor** | Before class commit | Pay **5 gold** → become **Mage** |
| **Arcane Vendor** | After class commit | Buy **spellbooks** (1 gold each) |

### 5.7 — Post-commit player loop

```
Human (None, no consumed essences)
  → Mage Tutor quest → pay 5 gold → HumanClass.Mage
  → Arcane Vendor → buy spellbooks
  → inventory Read → known spells
  → town safe zone → equip spells within Magic Power budget
  → hotbar assign → dungeon cast
```

### 5.8 — `HumanMageClassCommitService` (recommended)

Centralize gates used by dialog, quest turn-in, and tests:

```csharp
bool CanBeginMageTraining(BaseActor human, out string denyReason);
bool TryCompleteMageApprenticeship(BaseActor human, out string failureReason);
// wraps gold deduct + HumanClassCommitment.TryCommit
```

---

## 6. Spell tiers & costs

### 6.1 — Tier scale

| Tier | Design intent | Base equip cost `(10 - tier)` |
|------|---------------|----------------------------------|
| **1** | Flagship / endgame | **9** |
| **2** | **8** |
| **3** | Strong combat (Fireball v0) | **7** |
| **4** | **6** |
| **5** | Lightning Bolt v0 | **5** |
| **6** | **4** |
| **7** | Utility (Arcane Might v0) | **3** |
| **8** | **2** |
| **9** | Cantrip-tier | **1** |

### 6.2 — Extra equip cost

Add field on `MageSpellDefinition`:

```text
extraEquipCost ≥ 0   // designer-authored; default 0
equipCost(spell) = (10 - tier) + extraEquipCost
```

**Example:** tier 5 spell with `extraEquipCost = 1` → equip cost **6**.

### 6.3 — Equip validation

```text
RemainingEquipCapacity = MaxMagicPower - Σ equipCost(s) for equipped spells s

TryEquip(spell) succeeds iff:
  spell ∈ KnownSpells
  AND RemainingEquipCapacity >= equipCost(spell)
  AND spell not already equipped
```

Unequip frees capacity immediately. Equip changes follow **town safe zone + not in combat** (same policy as Dragonian memorize — reuse or mirror `SafeZonePolicyService`).

### 6.4 — Cast validation

```text
TryCast(equippedIndex) succeeds iff:
  spell is equipped
  AND currentMagicPower >= magicPowerCost(spell)
  AND ability.CanExecute / Execute succeed
→ then currentMagicPower -= magicPowerCost(spell)
```

---

## 7. Known vs equipped

| Set | Population | UI label (future menu) |
|-----|------------|-------------------------|
| **Known** | `TryLearnSpell` / spellbook read / v0 preset | “Known spells” / “Grimoire” |
| **Equipped** | `TryEquip` / `TryUnequip` | “Prepared spells” / “Equipped” |

**Cast rule:** Only **equipped** spells are executable from the hotbar (`HotbarEntryKind.HumanMageSpell`).

**Learning rule:** Adding to **known** does **not** auto-equip. Player must equip within budget separately.

---

## 8. Learning from spellbooks

### 8.1 — Spellbook data model

**Asset:** `MageSpellbookDefinition` (ScriptableObject) or embedded list on `SpellbookItemData`.

| Field | Requirement |
|-------|-------------|
| **`spellbookId`** | Stable string (e.g. `spellbook_arcane_might`) |
| **`displayName`** | Player-facing (e.g. `Spellbook of Arcane Might`) |
| **`spellIds`** | Ordered list of **`MageSpellDefinition.spellId`** entries (≥ 1) |
| **`description`** | Optional fluff; inventory inspect text |

**Item:** `SpellbookItemData` extends `ItemData`:

| Field | Requirement |
|-------|-------------|
| **`category`** | `ItemCategory.Spellbook` |
| **`spellbook`** | Reference to `MageSpellbookDefinition` |
| **`weight`** | v0: **1** (match scrolls) |
| **`buyValue` / `sellValue`** | v0 shop: **buy 1 / sell 0** (or sell 0 — books consumed, not re-sold) |

### 8.2 — Read eligibility

| Rule | Detail |
|------|--------|
| **Actor** | **Active party member** must be Human Mage with `HumanMageSpellsRuntime` |
| **Location** | v0: readable **anywhere** (inventory Use); later may gate to safe zone |
| **Unknown spell required** | At least one `spellId` in the book is **not** in `KnownSpells` |
| **All known** | If every spell in the book is already known → **fail** with message *“You already know every spell in this book.”* — book **not** consumed |
| **Non-mage** | Fail with *“Only a Mage can study this spellbook.”* — book **not** consumed |

### 8.3 — Read flow (DCSS-style)

```
Inventory → select spellbook → Use / Read
  → validate actor + unknown spell exists
  → optional confirm dialog listing spells to be learned (v0.1; v0 may apply immediately)
  → for each spellId in book (in order):
        TryLearnSpell(spellId)  // skip if already known
  → remove spellbook stack from inventory (consume)
  → feedback: “You learn {names}.” / log per spell
```

**Turn cost:** v0 — **no turn** (study in inventory, like other inventory management).

**Combat:** v0 — allow read in combat if inventory policy allows (TBD; default **allow** unless `InventoryPolicy` blocks Spellbook category).

### 8.4 — `TryLearnSpell` contract

Add to `HumanMageSpellsRuntime`:

```csharp
bool TryLearnSpell(string spellId, out string failureReason)
```

| Rule | Behavior |
|------|----------|
| Resolve definition | Lookup in `MageSpellCatalogService` (or Resources catalog) by `spellId` |
| Idempotent | If already known → **success**, no duplicate entry |
| Validation | Unknown id → fail; non-Mage actor → fail |
| Persistence | Known list saved in party member snapshot |

### 8.5 — Multi-spell books

When a book lists multiple spells:

- Learn **all unknown** spells in one read.
- Consume book **once** after any new spell was learned.
- If book has 3 spells and mage knows 2 → learn the third only, still consume book.
- Order is **author order** in `spellIds`; no player choice inside one book (DCSS-style bundle).

### 8.6 — Spellbook acquisition (v0 + later)

| Source | v0 | Later |
|--------|-----|-------|
| **Town shop NPC** | **Yes** — Arcane Vendor (§11) | Restock rules |
| **Dungeon floor loot** | — | Weighted drops by depth |
| **Quest rewards** | — | Story-granted books |
| **Enemy drops** | — | Rare mage enemies |
| **Player crafting** | — | Out of scope |

---

## 9. Sample spells (v0 required)

Existing assets: `Spell_Fireball_Mage` (`mage_spell_fireball`). Add **`extraEquipCost`** field (default **0**). Add **Arcane Might** and **Lightning Bolt** spells + abilities as needed.

### 9.1 — Arcane Might

| Field | Value |
|-------|--------|
| **`spellId`** | `mage_spell_arcane_might` |
| **`displayName`** | Arcane Might |
| **`tier`** | **7** → base equip **3** |
| **`extraEquipCost`** | **0** → total equip **3** |
| **`magicPowerCost`** | **2** |
| **Ability** | New or variant: **targeted ally buff** — select **party member** tile; apply temporary **Strength** increase (reuse `SuddenStrengthBuffRuntime` pattern; **target** is ally, not self). |
| **`requiresTarget`** | **true** (friendly party member) |
| **Duration** | v0: **10 player phases**, +**100** STR (match Sudden Strength essence tuning until balance pass) |

**Design note:** Differs from Sudden Strength **essence** (self-buff, Soul Power folk). Mage version is **support** for allies.

### 9.2 — Fireball

| Field | Value |
|-------|--------|
| **`spellId`** | `mage_spell_fireball` *(existing)* |
| **`displayName`** | Fireball |
| **`tier`** | **3** → base equip **7** |
| **`extraEquipCost`** | **0** → total equip **7** |
| **`magicPowerCost`** | **5** *(existing asset)* |
| **Ability** | `Fireball_Standard` / `FireballAbility` — targeted tile, **splash** AoE, fire damage |
| **`requiresTarget`** | **true** |

### 9.3 — Lightning Bolt

| Field | Value |
|-------|--------|
| **`spellId`** | `mage_spell_lightning_bolt` |
| **`displayName`** | Lightning Bolt |
| **`tier`** | **5** → base equip **5** |
| **`extraEquipCost`** | **1** → total equip **6** |
| **`magicPowerCost`** | **4** |
| **Ability** | **New** `LightningBoltAbility`: targeted **single tile**, **no splash** (`splashRadius = 0` or primary-only zone); **Lightning** damage to actors on that tile; noise optional |
| **`requiresTarget`** | **true** |

**Distinct costs summary (v0 locked for implementation):**

| Spell | Tier | Extra | Equip | Cast |
|-------|------|-------|-------|------|
| Arcane Might | 7 | 0 | **3** | **2** |
| Fireball | 3 | 0 | **7** | **5** |
| Lightning Bolt | 5 | 1 | **6** | **4** |

**Equip example:** `MaxMagicPower = 20` → can equip Fireball (7) + Lightning Bolt (6) + Arcane Might (3) = **16**, `RemainingEquipCapacity = 4`. Cannot also equip a tier-1 spell (cost 9) without unequipping.

### 9.4 — Teleport (existing, not in v0 shop)

`Spell_Teleport_Mage` remains valid **preset/debug** content per parent doc §M8.5.2 but is **not** sold in the v0 Arcane Vendor spellbook set until a spellbook is authored.

---

## 10. Sample spellbooks (v0)

One book per spell for QA clarity (multi-spell books supported by data model).

| Item id | Book name | Spells inside | Shop price |
|---------|-----------|---------------|------------|
| `spellbook_arcane_might` | Spellbook of Arcane Might | `mage_spell_arcane_might` | **1 gold** |
| `spellbook_fireball` | Spellbook of Fireball | `mage_spell_fireball` | **1 gold** |
| `spellbook_lightning_bolt` | Spellbook of Lightning Bolt | `mage_spell_lightning_bolt` | **1 gold** |

**Suggested paths:**

- `Assets/Data/Racial/Human/Spellbook_*.asset`
- `Assets/Resources/Racial/Human/MageSpellCatalog.asset` (spell id → definition)
- `Assets/Resources/Item/Spellbook/*.asset`

---

## 11. Town shop — Arcane Vendor (v0)

Extend [Shop NPC](../World/Shop-NPC-Requirements.md) pattern with a **buy-only (player buys)** mage goods vendor.

| Field | Value |
|-------|--------|
| **NPC** | New town NPC — **Arcane Vendor** (Human, distinct sprite/portrait) |
| **Marker** | `town_npc_arcane_vendor` (cell TBD in plaza stamp; ≥ 2 cells from other NPCs) |
| **Role** | Player **buys** spellbooks; NPC does not buy from player in v0 |
| **Starting gold** | N/A (infinite stock pricing from item `buyValue`) |
| **Stock** | 3 × spellbooks (§9), **unlimited quantity** each at **1 gold** |
| **Dialog** | Same Yes/No shop shell as existing merchants |

**QA loop:** Complete **Mage Tutor** quest (§5) → buy books → read → equip in town → assign hotbar → cast in dungeon.

---

## 12. Runtime & services

| Component | Responsibility |
|-----------|----------------|
| **`HumanMageSpellsRuntime`** | Known list, equipped list, `TryLearnSpell`, `TryEquip` / `TryUnequip`, cast routing *(partially done)* |
| **`MageSpellCatalogService`** | Resolve `spellId` → `MageSpellDefinition` (mirror `DragonianSpellCatalogService`) |
| **`MageSpellbookReadService`** | Inventory Use handler for `ItemCategory.Spellbook` |
| **`HumanMageSpellLoadoutService`** | Safe-zone gate for equip/unequip *(mirror Dragonian)* |
| **`HotbarAssignabilityService`** | Pool only **equipped** Mage spells |
| **`PlayerCommandProcessor`** | `HumanMageSpell` source for targeted execution |

### 12.1 — Persistence

Party member save includes:

- `knownSpellIds[]`
- `equippedSpellIds[]`

Load: resolve ids through catalog; drop unknown ids with warning; rebuild equip respecting budget.

---

## 13. UI (later)

| Milestone | Scope |
|-----------|--------|
| **v0** | Learn via inventory read; equip via debug/preset or minimal inspector |
| **v0.1** | Human Mage body on **`K`** racial menu (known vs equipped columns — mirror [Dragonian menu](Dragonian-Racial-Abilities-Menu-Requirements.md)) |
| **v1** | Spell tier / cost tooltips; filter known library |

---

## 14. Acceptance criteria

### 14.1 — Class commitment

- Given **Human** with **`HumanClass.None`**, **no consumed (equipped) essences**, and party gold **≥ 5**, completing **`quest_mage_tutor_apprenticeship`** sets **`humanClass == Mage`**, deducts **5 gold**, and sets **`currentMagicPower == MaxMagicPower`**.
- Given same actor after commit: **`EssenceSlotManager.totalSlots == 0`**, **`MaxSoulPower == 0`**, **`currentSoulPower == 0`**.
- Given **Human Knight**, tutor dialog **rejects** commitment; class unchanged.
- Given **Human None** with **≥ 1 consumed (equipped) essence**, tutor **rejects** accept/turn-in; quest not completed.
- Given **Human None** with **unconsumed** essence items only in **inventory**, tutor turn-in **succeeds**; after commit, **consuming (equipping)** those items **fails**.
- Given **Human Mage**, reading spellbooks **succeeds**; given **Human None**, reading spellbooks **fails**.

### 14.2 — Spells & spellbooks

- Given **Human Mage** with empty known list, reading **Spellbook of Fireball** adds `mage_spell_fireball` to known and **removes** the book.
- Given mage who **already knows** every spell in a book, **Use** fails and book **remains**.
- Given **Knight**, reading a spellbook **fails**; book **remains**.
- Given mage learns Fireball + Lightning Bolt + Arcane Might, equipping all three succeeds at `MaxMagicPower = 20`; `RemainingEquipCapacity == 4`.
- Given Fireball equipped, casting reduces **`currentMagicPower`** by **5**, not Soul Power; equip capacity unchanged.
- Given **Dragonian** with Dragonian spells, **`TryLearnSpell` for `mage_spell_fireball`** fails at service layer.
- Given **Human Mage**, **`TryLearnSpell` for `dragonian_spell_*`** fails.
- Arcane Vendor sells each v0 spellbook for **1 gold**; purchase adds item to party inventory.
- **Lightning Bolt** hits only the **targeted tile** (no splash cells in preview or resolution).
- **Arcane Might** buffs a **selected party member**, not the caster (when caster ≠ target).

---

## 15. Code touchpoints

| Area | Action |
|------|--------|
| **`HumanClassCommitment`** | Extend **`TryCommit`** path with essence-empty validation (§5.2) when called from tutor quest |
| **`HumanMageClassCommitService`** | New — `CanBeginMageTraining`, `TryCompleteMageApprenticeship` |
| **`HumanMageTutorQuestLogic`** | Dialog + turn-in; gold deduct + commit |
| **`QuestDefinition` / rewards** | `turnInGoldCost`, **`commitHumanClass`**, `requiresNoEquippedEssences` |
| **`quest_mage_tutor_apprenticeship`** | Quest asset + Mage Tutor NPC (`human_mage_tutor`) |
| `MageSpellDefinition` | Add **`extraEquipCost`**; update `EquipCost` property |
| `HumanClassRules.GetSpellEquipCost` | Include `extraEquipCost` in total |
| `MageSpellCatalogService` | New — Resources catalog + lookup |
| `HumanMageSpellsRuntime` | `KnownSpells`, `TryLearnSpell`, expose known list |
| `MageSpellbookDefinition` + `SpellbookItemData` | New item type |
| `InventoryItemUse` | Route Spellbook → `MageSpellbookReadService` |
| `LightningBoltAbility` | New single-tile damage ability |
| `ArcaneMightAbility` (or targeted SuddenStrength variant) | Ally-target buff |
| `Assets/Editor/.../MageSpellPackCreator` | Spell + book + shop stock authoring |
| Tests | Learn idempotency, book consume rules, exclusivity, equip math with `extraEquipCost` |

---

## 16. Implementation status

| Item | Status |
|------|--------|
| `HumanClassCommitment.TryCommit` (None → Mage, bootstrap) | Done |
| **Essence-empty gate on Mage commit** | Done |
| **`quest_mage_tutor_apprenticeship` + Mage Tutor NPC** | Done |
| **`HumanMageClassCommitService`** | Done |
| `HumanMageSpellsRuntime` equip + cast | Done |
| `MageSpellDefinition` + sample Fireball/Teleport | Done |
| **`extraEquipCost`** on spells | Done |
| **`TryLearnSpell` + known library API** | Done |
| **Spellbook items + read pipeline** | Done |
| **Arcane Might + Lightning Bolt** spells/abilities | Done |
| **MageSpellCatalogService** | Done |
| **Arcane Vendor shop** | Done |
| **Mage racial menu (`K`)** | Later (§12) |
| Unit tests for this doc | Done |

---

## 17. Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Human — Class powers](Human-Class-Powers-Requirements.md) | §M8.6, §C5.3 — Mage tutor quest **Done** when shipped |
| [Quest system](../World/Quest-Requirements.md) | Sample quest row; `commitHumanClass` reward |
| [NPC dialog](../World/NPC-Dialog-Requirements.md) | Mage Tutor plaza marker |
| [Dragonian — Spell memory](Dragonian-Spell-Memory-Requirements.md) | §4 — Mage spell exclusivity cross-link |
| [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) | Mage spell assign prerequisites |
| [Shop NPC](../World/Shop-NPC-Requirements.md) | Arcane Vendor + Mage Tutor rows |

---

## 18. Document history

| Date | Change |
|------|--------|
| 2026-06-05 | Locked **equip ≡ consume** terminology for essences (*STBGB*). |
| 2026-06-05 | Locked essence rule — consumed/equipped essences block training; unconsumed inventory items allowed. |
| 2026-06-05 | Added §5 Mage apprenticeship quest — None-only commit, no equipped essences, 5 gold tutor, bootstrap rules. |
| 2026-06-05 | Initial draft — DCSS spellbooks, tier + extra equip cost, three sample spells, Arcane Vendor, Dragonian exclusivity. |
