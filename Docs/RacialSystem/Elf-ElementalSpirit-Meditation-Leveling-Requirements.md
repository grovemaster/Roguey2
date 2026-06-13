# Elf — Elemental Spirit meditation & leveling (requirements)

**Purpose:** Specify how an **Elf** deepens an existing **Elemental Spirit contract** by gaining **spirit experience** through a **meditation event**, then **leveling up** that contract instance when experience thresholds are met. This doc implements the **“level spirit”** gate deferred from [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) §5.8 (F4.16).

**Inspiration:** *Surviving the Game as a Barbarian* — Barbarians visit the **Shaman** to advance their imprint path; Elves perform **meditation** to bond more deeply with a contracted spirit. Barbarian = deterministic tree node; Elf = **per-instance spirit XP** → **contract level** increase.

**Status:** Implemented (v0).

**Depends on:** [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) (`ElementalSpiritDefinition`, `ElementalSpiritContractPreset`, `ElementalSpiritContractsRuntime`, cumulative level payloads, summon/dismiss), [Elf — Fairy Stone spirit contracts](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) (contract instance model, `contractInstanceId`), [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) (`CharacterStats.level`), [NPC dialog](../World/NPC-Dialog-Requirements.md) / interactable patterns, `PartyManager`, `CharacterStats.race`, `GameplayModalGate`, [Safe zone](../World/Safe-Zone-Requirements.md).

**Related:** [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) (parallel **instant** town upgrade UX). [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (contract roster + **spirit XP display** — next implementation slice). [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (spirit actives refresh after level-up).

**Explicitly out of scope (v0):** Leveling via **Fairy Stone** (remains form-contract only); respec / reduce contract level; spirit XP from **combat kills** or **dungeon events** (future sources may plug into the same XP API); cross-spirit “shared XP pool”; meditation **outside town / non-safe zone**; bespoke full-screen meditation minigame; save/load beyond existing party persistence; **Race.Fairy** folk content; **gate cooldowns**; **calendar / time passage** on meditate (future Persona-style hook).

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | **Meditation** is the **primary v0 gate** for raising **contract level** on an existing instance. |
| **L2** | Spirit XP and level-up apply to a **contract instance** (`contractInstanceId`), not to a `spiritId` globally. Two Ember Warden instances have **independent** XP and levels. |
| **L3** | **Contract level** (`contractLevel`) is the persisted progression field; **spirit experience** (`contractExperience`) is the persisted XP pool toward the **next** contract level. |
| **L4** | **Effective level cap** for an instance = `min(spirit.maxLevel, capFromPolicy)`. **v0 policy:** capFromPolicy = **picked Elf’s character level** (`CharacterStats.level`). Policy is **pluggable** (§5.3). |
| **L5** | An instance **cannot** gain XP or level up while `contractLevel >= effectiveCap`. UI greys out capped instances with reason text. **Block award** at cap — do not bank XP (O3). |
| **L6** | Level-up is **automatic** when `contractExperience >= xpRequiredForNextLevel(contractLevel)` after a meditation award. **Multiple level-ups in one event** allowed when XP overflow permits (O5). |
| **L7** | Successful level-up **re-applies** summoned payloads if that instance is summoned (parent F4.16): remove old level-sourced modifiers, apply new cumulative set. |
| **L8** | **Meditation event** does **not** consume a combat turn. Transaction is **instant** on confirm — same discipline as [Barbarian Shaman](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) imprint upgrade (O4). |
| **L9** | **Elf gate:** only `Race.Elf` actors with `RacialSubsystemKind.ElfElementalContracts` may receive spirit XP from meditation. |
| **L10** | **Party context:** when **multiple** Elves are in the party, player **chooses target Elf** then **contract instance** (Fairy Stone picker discipline). When **exactly one** Elf is in the party, **auto-select** that Elf — skip Elf picker (O7). |
| **L11** | **v0 delivery:** **Meditation shrine** interactable in **town** (O1). Gate definition remains delivery-agnostic so a special room, NPC, or zone can reuse the same service later. |
| **L12** | **v0 location:** Training **only** in **town** while in a **non-combat safe zone** (`SafeZonePolicyService`). Not available in dungeon or combat (O4). |
| **L13** | **Summon state irrelevant** for meditation — training allowed whether the target instance is summoned or dismissed (O4). Level-up still refreshes payloads if summoned (L7). |
| **L14** | **XP curve:** **Global default** `ElementalSpiritLevelCurve` asset; each `ElementalSpiritDefinition` may **override** with its own curve reference (O2). |
| **L15** | **No cooldown** on meditation gates in v0 (O6). Future: some town activities advance **calendar / time of day** (Persona 5–style); meditation may hook that later without changing core XP API. |
| **L16** | **Spirit XP UI:** Show `contractExperience` and progress toward next level in the **Elf racial abilities menu** (O9 — separate implementation slice after core meditation). Shrine dialog may show summary; racial menu is the persistent home. |
| **L17** | **No character de-level** is planned. If `CharacterStats.level` ever drops anyway, **`contractLevel` is never reduced** — spirits keep their achieved level; only **future** XP gain is blocked while `contractLevel >= effectiveCap` (O10). |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Town / safe progression** — Player can use a **meditation shrine** in town (safe zone) to deepen spirit bonds; instant confirm like Shaman imprint upgrade. |
| **G2** | **Per-instance growth** — Each contract instance tracks its own XP and level; duplicates of the same spirit type progress independently. |
| **G3** | **Character-level coupling** — Spirit contract level is **bounded by the Elf’s character level** in v0, so early Elves cannot max spirits immediately. |
| **G4** | **Extensible cap policy** — Designers can swap or extend the level-cap rule later (e.g. quest flag, Wisdom stat, party level) without rewriting meditation flow. |
| **G5** | **Data-driven curves** — XP thresholds and meditation awards are authored in assets, not hard-coded per spirit. |
| **G6** | **Runtime correctness** — Level changes update passives, hotbar assignables, and toggle state safely (no orphaned modifiers). |
| **G7** | **Clear feedback** — Player sees XP gained, level-up, cap-blocked, and error paths in dungeon log / dialog; **persistent** XP progress in Elf racial abilities menu (L16). |
| **G8** | **Service discipline** — All mutations go through a single API (mirror `ElementalSpiritContractService` / `SpiritImprintUpgradeService`). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Meditation event** | One player-initiated session at a meditation gate: pick Elf → pick contract instance → award spirit XP (and maybe pay costs) → resolve level-ups. |
| **Spirit experience (contract XP)** | Integer pool on a **contract instance** toward the next **contract level**. Not party XP (`CharacterStats.experience`). |
| **Contract level** | Existing field on `ElementalSpiritContractPreset` — `1 … maxLevel` for that instance. Determines cumulative level rows when summoned. |
| **Spirit max level** | `ElementalSpiritDefinition.maxLevel` — content ceiling for that spirit **type**. |
| **Effective level cap** | Maximum `contractLevel` this instance may reach **right now** (§5.3). |
| **Level cap policy** | Pluggable rule that computes effective cap from Elf + instance + world state. |
| **Meditation shrine** | v0 town interactable that starts a meditation event (L11). Same gate data can power other delivery hooks later. |
| **Target Elf** | Party Elf selected for this meditation (L10 / L10+O7 auto-select). |
| **Target instance** | One `contractInstanceId` on that Elf’s roster receiving XP. |

### 2.1 — Distinction from party leveling

| | **Party / character level** | **Spirit contract level** |
|--|------------------------------|---------------------------|
| **Resource** | `CharacterStats.experience` | `contractExperience` on instance |
| **Scope** | Whole party member | One contract instance on one Elf |
| **Primary source (v0)** | First-kill species XP, potions | Meditation events |
| **Cap** | `ExperienceCurve.maxLevel` (50) | `min(spirit.maxLevel, levelCapPolicy)` |
| **Combat effect** | Stats, Max HP, Max Soul Power | Summoned passives/actives only |

---

## 3. Relationship to parent Elf docs

| Parent rule | Meditation behavior |
|-------------|---------------------|
| F4.16 — level spirit via event/NPC/item | **This doc is that gate (v0).** |
| F4.15 — form contract | Unchanged — use [Fairy Stone](Elf-Fairy-Stone-Spirit-Contract-Requirements.md). |
| Contract instance model | XP and level stored **per instance** (`contractInstanceId`). |
| Cumulative level payloads | Level 2 instance grants level 1+2 rows when summoned (parent O1). |
| Summon / dismiss / actives | Unchanged; level-up refreshes payloads if summoned (L7). |
| Fairy Stone | **Does not** grant spirit XP or raise level in v0 (parent Fairy doc L scope). |

**Barbarian parallel:**

| Barbarian (Shaman) | Elf (Meditation shrine) |
|--------------------|-------------------------|
| Town NPC, safe zone | Town **shrine**, safe zone only (L12) |
| Spend gold/items | Spend **meditation cost** (§6.3) at gate |
| Pick **next imprint node** | Pick **contract instance** to train |
| **Instant** on confirm | **Instant** on confirm (L8) |
| Append one graph node | Award spirit XP → resolve level-up(s) |
| Path length = rank | `contractLevel` + `contractExperience` |
| Speaker = active leader | **Any party Elf** choosable; auto if sole Elf (L10, O7) |

---

## 4. Data model extensions

### D4.1 — Contract instance (runtime / serialized)

Extend each roster row beyond parent `{ contractInstanceId, spirit, contractLevel }`:

| Field | Type | Notes |
|-------|------|--------|
| **`contractExperience`** | int ≥ 0 | XP toward **next** level. Awards **blocked** at cap — never banked while capped (L5). |
| *(existing)* **`contractLevel`** | int | `1 … effectiveCap`. |
| *(existing)* **`contractInstanceId`** | string | Stable save key. |
| *(existing)* **`spirit`** | ref | `ElementalSpiritDefinition`. |

**Save:** Serialize with party member / `ElementalSpiritContractsRuntime`. New contracts from Fairy Stone start at `contractLevel = 1`, `contractExperience = 0`.

### D4.2 — Spirit level curve (ScriptableObject)

**`ElementalSpiritLevelCurve`** (or per-spirit embedded table on `ElementalSpiritDefinition`):

| Field | Requirement |
|-------|-------------|
| **`xpToReachLevel[L]`** | XP required to advance **from** level `L` **to** `L + 1`, for `L = 1 … maxLevel - 1`. |
| **Monotonic** | Thresholds non-decreasing (recommended). |
| **Per-spirit override** | **Locked (L14):** each `ElementalSpiritDefinition` may reference its own curve; when unset, use shared `ElementalSpiritDefaultLevelCurve.asset`. |

**Query API:**

- `GetXpRequiredForNextLevel(int currentContractLevel) → int`
- `GetTotalXpForLevel(int targetLevel) → int` (optional, for UI progress bars)

### D4.3 — Level cap policy (extensible)

**`IElementalSpiritLevelCapPolicy`** (or static resolver with registered strategies):

```text
int ResolveEffectiveCap(
    CharacterStats elfStats,
    ElementalSpiritContractPreset instance,
    ElementalSpiritDefinition spiritDef);
```

**v0 implementation — `CharacterLevelSpiritCapPolicy`:**

```text
effectiveCap = min(spiritDef.maxLevel, elfStats.level)
```

**Future examples (not v0):**

| Policy id | Cap rule |
|-----------|----------|
| `CharacterLevel` | `min(maxLevel, character.level)` — **v0 default** |
| `CharacterLevelPlusWisdom` | `min(maxLevel, character.level + wisdomBonus)` |
| `QuestUnlocked` | `min(maxLevel, baseCap + questBonus)` |
| `Uncapped` | `spiritDef.maxLevel` only |

**Registration:** `ElementalSpiritProgressionConfig` asset lists active policy id(s) or a single policy reference so designers can swap without code changes.

### D4.4 — Meditation gate definition

**`ElementalSpiritMeditationGateDefinition`** (referenced by shrine interactable; reusable by future room/NPC/zone hooks):

| Field | Requirement |
|-------|-------------|
| **`gateId`** | Stable string for logs/saves. |
| **`displayName`** | UI / dialog title (e.g. “Moonlit Glade”). |
| **`spiritXpAward`** | int ≥ 0 granted per successful meditation (v0 flat award). |
| **`cost`** | Optional bundle: party gold, items, story flags (mirror Shaman unlock cost pattern). |

**v0 environment rules (locked):** meditation **only** when `SafeZonePolicyService` reports town safe zone (L12). No dungeon gates in v0. **No cooldown** (L15). **Summon state** does not affect eligibility (L13).

---

## 5. Functional requirements

### 5.1 — Eligibility

**F5.1** Only `Race.Elf` with `RacialSubsystemKind.ElfElementalContracts` may be selected as **target Elf**.

**F5.2** Target Elf must have **at least one** contract instance in `contractedSpirits` to open instance picker (empty roster → rejection line).

**F5.3** Non-Elf party (no Elf in roster) → gate shows rejection; no meditation UI.

**F5.3a** Meditation **blocked** outside town safe zone (dungeon, combat) — rejection line; no UI (L12).

### 5.2 — Meditation event flow

**F5.4** Player initiates meditation at a **meditation shrine** (`Enter` / interact adjacent, same adjacency discipline as other town interactables).

**F5.5** Flow:

```text
Open shrine (town safe zone only)
  → If not safe zone: reject
  → If no Elf in party: reject
  → Pick target Elf (if multiple; auto if exactly one — L10/O7)
  → List contract instances on that Elf (name, element, contract level, XP progress, cap)
  → Pick instance OR Cancel / Esc
  → Validate: costs, cap (summon state ignored — L13)
  → Pay costs (if any)
  → Award spiritXpAward to contractExperience (instant — L8)
  → Resolve level-ups (§5.5)
  → Feedback lines + close
```

**F5.6** **Cancel** / **Esc** before confirm: no cost, no XP, no level change.

**F5.7** One meditation event trains **exactly one** contract instance (v0). Batch “meditate all spirits” is out of scope.

### 5.3 — Effective level cap

**F5.8** Before awarding XP or applying level-up:

```text
effectiveCap = levelCapPolicy.Resolve(elfStats, instance, spiritDef)
```

**F5.9** If `contractLevel >= effectiveCap`:

- Instance is **capped** — cannot gain XP or level further until cap rises (e.g. Elf gains character levels).
- UI: greyed choice + tooltip **“Spirit level cannot exceed your level (N).”** (exact copy tunable).
- If **`contractLevel > effectiveCap`** because the Elf’s character level dropped (edge case — L17): instance remains at its current **contract level**; still **capped** for new XP until `effectiveCap` catches up. **Never** auto-reduce `contractLevel`.

**F5.10** Cap uses **picked Elf’s** current `CharacterStats.level` at meditation time (O8).

**F5.11** `effectiveCap` is always `≥ 1` and `≤ spirit.maxLevel`. `contractLevel` may exceed `effectiveCap` only via L17 edge case; normal play advances both together.

### 5.4 — Spirit experience award

**F5.12** On successful meditation, add `gate.spiritXpAward` to `instance.contractExperience`.

**F5.13** Spirit XP is **not** shared with party XP and does not appear in species journal.

**F5.14** If award would apply to a capped instance, block transaction **before** deducting costs (fail closed).

### 5.5 — Level-up resolution

**F5.15** After XP award, while `contractLevel < effectiveCap` and `contractExperience >= GetXpRequiredForNextLevel(contractLevel)`:

1. Subtract threshold from `contractExperience` (carry overflow — recommended).
2. Increment `contractLevel` by 1.
3. Repeat until below threshold or at cap.

**F5.16** On each level gained:

- If instance **summoned**: teardown + re-apply cumulative payloads for new level (parent L7 / F4.16).
- If **not summoned**: only persist level + XP; payloads apply on next summon.
- Clear or preserve toggle state per active kind (toggle actives: recommend **clear toggles** on level change to avoid stale imbue state — document in implementation).

**F5.17** Hotbar assignable pool refreshes for that Elf (new actives from higher level rows).

### 5.6 — Costs

**F5.18** If gate defines **cost**, deduct atomically with XP award (same transaction). Unaffordable → grey choice, no partial pay.

**F5.19** **No cooldown** in v0 (L15). Repeat meditation at the same shrine is allowed whenever costs and cap permit.

### 5.7 — Service API

**F5.20** `ElementalSpiritMeditationService` (or extend `ElementalSpiritContractService`):

| Method | Behavior |
|--------|----------|
| `TryBeginMeditation(gate, party)` | Open flow / return eligibility |
| `TryAwardSpiritExperience(elf, instanceId, amount, source)` | Validate cap → add XP → resolve level-ups |
| `GetEffectiveLevelCap(elf, instance)` | Delegates to policy |
| `GetXpProgress(elf, instance)` | `(currentXp, xpToNext, contractLevel, effectiveCap)` for UI |

All roster mutations go through this service; no direct list editing from UI.

---

## 6. Meditation shrine — interaction model (v0, locked)

### 6.1 — Delivery (O1)

**v0:** **Interactable meditation shrine** placed in **town** (e.g. dedicated room or grove tile).

| Rule | Detail |
|------|--------|
| **Data** | Shrine references `ElementalSpiritMeditationGateDefinition` (award, cost, copy). |
| **Future delivery** | Same gate asset may power a special room trigger, NPC dialog, or quest callback without changing `ElementalSpiritMeditationService`. |
| **Dungeon shrines** | Out of v0; future extension may set `requiresSafeZone = false` on alternate gate assets. |

### 6.2 — Interaction rules (aligned with Shaman — O4)

| Rule | Detail |
|------|--------|
| **Open** | `Enter` adjacent + facing shrine (or project interact key pattern used by town objects) |
| **Location** | **Town safe zone only** — `SafeZonePolicyService.IsSafeZoneForActiveParty()` (L12) |
| **Turn cost** | **None** (L8) |
| **Transaction** | **Instant** on confirm — pay cost → award XP → level-up loop → feedback → close (mirror Shaman single-step upgrade) |
| **Blocks gameplay** | Uses dialog / modal stack (`GameplayModalGate`) |
| **Summon state** | **Ignored** — train instance whether summoned or dismissed (L13). If summoned, L7 refresh runs on level-up. |
| **Target Elf** | Picker when multiple Elves; **auto-select** when exactly one Elf in party (O7) |

### 6.3 — v0 costs (placeholder — tune in gate asset)

| Cost type | v0 recommendation |
|-----------|-------------------|
| **Gold** | **0** or **small flat fee** (e.g. 5 gold) |
| **Items** | None |
| **Spirit XP award** | **Flat +10** per session (placeholder; curve defines levels 1→2, 2→3, …) |
| **Cooldown** | **None** (O6) |
| **Time passage** | **None** in v0; future town activities may advance day phase (Persona 5–style) — hook documented in §13 |

---

## 7. UI / player feedback

### 7.1 — Instance list row (shrine dialog)

Each eligible instance shows:

| Column | Example |
|--------|---------|
| Spirit name | Ember Warden (2) |
| Contract level | Lv 2 / 5 |
| Cap hint | Cap: 4 (your level) |
| XP progress | 12 / 30 XP to Lv 3 |

Duplicate spirit types: append **(2)**, **(3)** disambiguator (same as Fairy Stone summon labels).

### 7.1a — Elf racial abilities menu (O9 — next slice)

Persistent display per contract instance on the Elf **racial abilities menu** ([Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md)):

| Column | Example |
|--------|---------|
| Spirit name | Ember Warden (2) |
| Contract level | Lv 2 |
| XP bar / text | 12 / 30 to Lv 3 |
| Cap | Max Lv 4 (your level) |

Shrine dialog may duplicate summary for the session; racial menu is the **authoritative** progress view (L16).

### 7.2 — Result messages (v0 copy placeholders)

| Event | Message |
|-------|---------|
| XP only | `{spiritName} gained {N} bond experience.` |
| Level-up | `{spiritName} reached contract level {L}!` |
| Capped | `Your bond with {spiritName} cannot deepen until you grow stronger (level {elfLevel}).` |
| Not in town | `You can only meditate with your spirits in town.` |
| No contracts | `You have no spirit contracts to nurture.` |

### 7.3 — Hotbar / overflow

After level-up, new actives appear in **Racial** assignable pool per parent §5.11 (deduped by ability asset). No automatic hotbar slot assignment.

---

## 8. Content examples

### 8.1 — Ember Warden at Elf level 3

| Field | Value |
|-------|-------|
| `spirit.maxLevel` | 3 |
| Elf `character.level` | 3 |
| **effectiveCap** | `min(3, 3) = 3` |
| `contractLevel` | Can reach 3 via meditation |
| Level 4+ | Blocked until Elf reaches character level 4 |

### 8.2 — Tide Shard with higher spirit max

| Field | Value |
|-------|-------|
| `spirit.maxLevel` | 5 |
| Elf `character.level` | 2 |
| **effectiveCap** | `min(5, 2) = 2` |
| At Elf level 10 | **effectiveCap** = `min(5, 10) = 5` (spirit content cap binds) |

### 8.3 — Two Ember Warden instances

| Instance | contractLevel | contractExperience |
|----------|---------------|-------------------|
| Instance A | 1 | 0 |
| Instance B | 2 | 15 |

Meditating on **A** does not affect **B**.

---

## 9. Non-functional requirements

**N5.1 — Authoring**  
Designers tune XP curves and gate awards in assets; no per-spirit C# subclasses.

**N5.2 — Tests (minimum)**

- Award XP → level-up when threshold met; overflow carries.
- Cap blocks XP at `contractLevel == elf.level`.
- Elf level-up (party XP) raises cap; next meditation can progress previously capped spirit.
- Level-up while summoned refreshes passives; dismiss removes correctly.
- Capped instance greyed in UI; cancel pays nothing.
- Two instances: XP applies only to selected instance.
- `CharacterLevelSpiritCapPolicy` swappable via config without changing meditation flow.
- Elf character level drops below `contractLevel`: `contractLevel` **unchanged**; further XP **blocked** until cap catches up (L17).
- Sole Elf in party: Elf picker skipped (O7).

**N5.3 — Migration**  
Existing saves without `contractExperience`: default **0**. Existing `contractLevel` preserved.

**N5.4 — Logging**  
Structured debug lines: `[SpiritMeditation] {elf} +{xp} → {instanceId} L{level} ({xp}/{next}) cap={cap}`.

---

## 10. Acceptance criteria (examples)

- Given Elf **level 2** with Ember Warden instance at **contract level 2** and cap 2, meditation **cannot** raise level until Elf reaches **character level 3**.
- Given Elf **level 5**, Ember Warden (`maxLevel 3`) at level 2, sufficient XP, meditation raises instance to **level 3** and unlocks level-3 actives when summoned.
- Given instance **summoned**, meditation **succeeds**; level-up refreshes passives without orphaned modifiers.
- Given meditation attempted **outside town safe zone**, rejected with no cost.
- Given two Tide Shard instances, meditating one grants XP **only** to that instance.
- Given meditation cancelled at picker, **no** gold/XP spent.

---

## 11. Out of scope (v0)

- Spirit XP from combat, quests, or consumable items (future: call `TryAwardSpiritExperience`).
- Fairy Stone raising level or granting spirit XP.
- Reducing contract level / respec.
- Meditation outside **town safe zone** (dungeon shrines — future).
- **Calendar / time advancement** on meditate (future Persona-style town time).
- Auto-meditation / idle bonding.
- Global spirit level shared across duplicate `spiritId`.
- Changing **character level cap policy** at runtime without config reload (future tooling).

---

## 12. Resolved decisions (formerly open)

| # | Decision | Locked as |
|---|----------|-----------|
| **O1** | Gate delivery | **Shrine** in town (L11); gate data reusable for other delivery later |
| **O2** | XP curve | **Global default** + per-spirit override on definition (L14) |
| **O3** | XP at cap | **Block award** — do not bank (L5) |
| **O4** | Where / summon state | **Town safe zone only**; summon state **irrelevant**; **instant** transaction (L8, L12, L13) |
| **O5** | Multi-level per event | **Yes** — overflow carry loop (L6) |
| **O6** | Cooldown | **None** in v0 (L15); future time passage separate |
| **O7** | Sole Elf in party | **Auto-select** — skip Elf picker (L10) |
| **O8** | Cap level source | **Picked Elf’s** `CharacterStats.level` (L4) |
| **O9** | Spirit XP UI | **Elf racial abilities menu** (L16 — next slice) |
| **O10** | Elf de-level | **No** `contractLevel` reduction; cap only blocks new XP (L17) |

---

## 13. Future extensions (documented hooks)

| Extension | Hook |
|-----------|------|
| **Alternate delivery** | Special room, NPC, or quest step calls `TryBeginMeditation` / `TryAwardSpiritExperience` with same gate asset. |
| **Alternate cap policy** | Implement `IElementalSpiritLevelCapPolicy`; register in `ElementalSpiritProgressionConfig`. |
| **Additional XP sources** | `TryAwardSpiritExperience(elf, instanceId, amount, source)` with `source` tag for analytics. |
| **Dungeon meditation shrines** | New gate assets outside town when design allows non-safe-zone training. |
| **Town time / calendar** | After successful meditate (or other town actions), advance **day phase** or **date** — Persona 5–style; meditation service stays unaware; orchestrator calls time system then award (L15). |
| **Wisdom / soul stat bonus** | New policy reading `CharacterStats.Wisdom` or custom stat. |
| **Bond milestones** | Quest flags raise cap for specific `spiritId` without raising character level. |

---

## 14. Relation to other docs

| Doc | Relationship |
|-----|----------------|
| [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) | Parent runtime; F4.16 implemented here. |
| [Elf — Fairy Stone](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) | Form contract only; leveling explicitly separate. |
| [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) | Supplies `CharacterStats.level` for v0 cap policy. |
| [Barbarian — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) | Parallel town progression UX. |
| [Phase0 — Glossary](Phase0-Glossary-And-Data-Contracts.md) | Add **contract experience**, **meditation shrine** when implementing. |
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | Spirit XP + roster reference + nicknames (L16). |
| [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) | Refresh assignables after level-up. |

---

## 15. Implementation checklist (high level)

- [ ] Extend `ElementalSpiritContractPreset` with `contractExperience`
- [ ] Add `ElementalSpiritLevelCurve` asset + queries
- [ ] Add `IElementalSpiritLevelCapPolicy` + `CharacterLevelSpiritCapPolicy` + config asset
- [ ] `ElementalSpiritMeditationService` with award + level-up loop
- [ ] Level-up hook on `ElementalSpiritContractsRuntime` (refresh summoned payloads)
- [ ] Town **meditation shrine** interactable + dialog / picker UI
- [ ] Elf racial abilities menu: spirit XP progress (L16 — may ship immediately after core)
- [ ] Tests per N5.2
- [ ] Update parent Elf doc §5.8 F4.16 cross-link to this doc
- [ ] Update Fairy Stone doc “out of scope” to point here for leveling
