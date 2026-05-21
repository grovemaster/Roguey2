# Undead — Race requirements

Undead are a playable **folk** (`Race.Undead`, numeric value **8** in `StatTypes.cs`) with a **static racial package** (restrictions, paired benefits, resistances) and a separate **Necrotic skill tree** subsystem shaped like **Diablo 4’s** class skill tree: point allocation across clusters and node types, **respec allowed**, with optional **story/event mutations** to the tree documented for a later phase.

**Subsystem kind (proposed code):** `RacialSubsystemKind.UndeadSkillTree` (see `RacialSubsystemKind.cs`).

**Commitment policy:** `RacialCommitmentPolicy.RespecAllowed` for skill-tree point allocation (contrast with Barbarian **Permanent** Spirit Imprint path).

**Depends on:** Phase 1–2 (`RacialLoadoutDefinition` / `RacialLoadoutApplier`, stacking-by-source, `RacialPassiveHooks`), inventory use pipeline (`ItemCategory.Potion`, `InventoryUsability`), Phase 3 graph/node payload patterns (reuse where they fit; **do not** copy Barbarian forward-only permanent rules).

**Contrast:** [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (forward-only, permanent path; nodes lack benefit/restriction lists). [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) (**shared progression payload** with Undead; slot replace/respec, not a point tree). [Beastman — Soul Beast](Beastman-Soul-Beast-Requirements.md) (linear bond chain, permanent).

**Shared code (progression payload):** `IRacialProgressionPayload`, `RacialProgressionPayload`, `RacialBenefitDefinition`, `RacialRestrictionDefinition`, `RacialProgressionPayloadApplicator` under `Assets/Data/Racial/`. **`CyborgImplantDefinition`** and **`UndeadSkillTreeNodeData`** both implement the contract.

**Implementation (v0 shipped):** Code under `Assets/Data/Racial/Undead/`; sample content `UndeadSkillTree_Sample.asset`; prefab `UndeadPlayer.prefab`. Payloads are **flat while rank ≥ 1** (rank increases do not re-stack stats in v0).

---

## 1. Goals

**G1 — Vertical slice (v0)**  
An Undead actor applies **folk baseline** modifiers (Radiant weakness, Poison immunity, potion ban + paired benefit), allocates **skill points** on the **sample Undead skill tree**, and can **respec** those allocations under documented rules. Ranked nodes apply their full payload (restrictions, benefits, stats, passives, actives—any non-empty lists). Invalid or corrupt tree state degrades safely (empty tree / root only + warning — mirror imprint policy).

**G2 — Data-driven content**  
Designers author the Undead skill tree and node payloads as assets without per-node `MonoBehaviour` subclasses. Each node may attach **zero or more** entries in every payload category (§7.2).

**G7 — Sample Undead skills (required)**  
Shipping v0 content includes a **sample Undead skill tree** with multiple **sample skills** (nodes) that prove cluster gates, node kinds, rank caps, and mixed payloads—including at least one node that uses each major payload type. See **§7.8** and **§13**.

**G3 — Restrictions vs benefits are explicit**  
Racial **restrictions** and **benefits** are documented and implemented in **separate, labeled** buckets so UI, tooling, and balance can treat them independently even when they are mechanically related (e.g. potion ban ↔ alternate healing).

**G4 — Playable Undead prefab (required)**  
A dedicated **`UndeadPlayer`** actor prefab exists (same family as `HumanPlayer` / `DwarfPlayer`) so designers can place or spawn a correctly configured Undead without hand-wiring components. See **§8**.

**G5 — Modifiers stack with gear**  
Radiant weakness and Poison immunity from race use **distinct sources** (Pattern B: baseline loadout vs skill-tree node sources). **Items and essences** may offset or amplify Radiant resistance; Poison immunity may be reinforced or (if design allows later) suppressed only via explicit content flags—not silently by generic gear rules without documentation.

**G6 — Future gates (documented, not v0)**  
**World events** that add, lock, or transform skill-tree nodes are **out of v0**; the data model and save format must not preclude them. See **§11**.

---

## 2. Should racial benefits and restrictions be categorized separately?

**Yes — resolved for this spec.**

| Layer | What goes here | Why separate |
|-------|----------------|--------------|
| **Racial restrictions** | Hard prohibitions or eligibility blocks (e.g. cannot use potions) | Drives inventory/UI denial, tooltips, and validation at use time |
| **Racial benefits** | Always-on advantages (e.g. Poison immunity, alternate healing channel) | Drives combat/resistance/passive application |
| **Racial stat modifications** | Numeric resistance/stat rows on baseline loadout | Same pipeline as Tiefling Fire resist; clear in balance sheets |
| **Folk baseline loadout** | Always-on race package (`RacialLoadoutDefinition`) | Stats, resistances, passives, actives—**no** `racialBenefits` / `racialRestrictions` lists (same as other folk) |
| **Undead skill tree (subsystem)** | Point-spent nodes (Diablo 4–style) | Uses **`IRacialProgressionPayload`**; respec allowed |

### 2.1 — Tiefling + Undead: unique progression payload — **Resolved**

Only **two** folk use **`IRacialProgressionPayload`** on **progression nodes**:

| Folk | Progression shape | Payload contract |
|------|-------------------|------------------|
| **Tiefling** | Seven **cyborg implant slots** (not a D4 point tree) | Each installed **`CyborgImplantDefinition`** |
| **Undead** | **Diablo 4–style skill tree** (clusters, ranks, respec) | Each ranked **`UndeadSkillTreeNodeData`** |

**All other racial subgraphs** (Barbarian Spirit Imprint, Dwarf Ancestor nodes, Beastman Soul Beast chain, Elf spirit levels, etc.) use **stats + passives + actives only**—no dedicated benefit/restriction lists.

**Undead skill-tree nodes** carry the five payload categories via **`IRacialProgressionPayload`** (or embedded **`RacialProgressionPayload`**). Effects apply **only while rank &gt; 0**, sourced per **`nodeId`** (Pattern B).

**Authoring:** **`DefaultUndeadRacialLoadout`** = folk baseline (potion ban, Poison immunity, Radiant −50 via stats/passives/inventory hooks as today). **Undead skill tree** = progression payload. Do not merge tree into `RacialLoadoutApplier`.

**Requirements doc structure:** Keep **§4–§6** as **design categories** for the Undead **race** (baseline + examples on tree nodes). On tree nodes, encode restrictions/benefits as **`RacialRestrictionDefinition`** / **`RacialBenefitDefinition`** assets, not ad-hoc passives, when they are true racial rules.

---

## 3. Reference — Diablo 4 skill tree (what Undead progression emulates)

This section defines the **player-facing model** the Undead subsystem should approximate. Exact node names and cluster count are **Undead content**, not a copy of a D4 class.

### 3.1 — Overall shape

- Each **class** in Diablo 4 has **one** large skill panel per character.
- The panel is organized into **clusters** (sections) that **unlock as you spend points** in earlier clusters—not as a single forward-only spine like Barbarian Spirit Imprint.
- Progression uses a **finite pool of skill points** earned from **character level** (historically through level 50, with further power moving to Paragon in D4). **Renown** and expansions can add more points; Undead should define its own point source (level, quests, etc.) in a later economy doc.

### 3.2 — Cluster unlock thresholds (illustrative D4 pattern)

Typical D4 gates (points spent **in the tree overall** or in prior clusters—Undead implementation should document which rule it uses):

| Cluster (typical order) | Approx. points required to access |
|---------------------------|-----------------------------------|
| Basic skills | 0 |
| Core skills | 2 |
| Class skills (tier 1) | 6 |
| Class skills (tier 2) | 11 |
| Class skills (tier 3) | 16 |
| Ultimate | 23 |
| Key passive (pick one) | 33 |

Undead content may use **fewer clusters** and different thresholds; the requirement is **cluster gating by spent points**, not identical D4 numbers.

### 3.3 — Node types (D4)

| Node shape (D4 UI) | Role | Typical point cap per node |
|--------------------|------|----------------------------|
| **Skill** (square) | Active ability | Up to **5** ranks |
| **Upgrade** (diamond) | Modifies a specific active | **1** rank |
| **Passive** (circle) | Permanent stat or rule tweak | Up to **3** ranks |

Edges connect nodes **within and across clusters**; some nodes require a parent investment before they unlock.

### 3.4 — Allocation and respec (D4)

- Players **spend** skill points on unlocked nodes up to each node’s cap.
- **Respec:** refund points (D4: right-click node; pays **gold** scaling with level). Undead need not use gold—define a **respec cost** resource (gold, soul, shrine, free at NPC, etc.) in content or a follow-up economy doc.
- Respec **does not** change cluster unlock thresholds; it reallocates the **same** point pool.

### 3.5 — Mapping to JRogue (Undead)

| D4 concept | Undead requirement |
|------------|-------------------|
| Clusters | **Cluster** assets or regions on one `UndeadSkillTreeDefinition` with `pointsRequiredToUnlock` |
| Node types | Enum: `Skill`, `Upgrade`, `Passive` with `maxRanks` per node |
| Point pool | `availableSkillPoints` / `spentSkillPoints` on actor save; v0 may **preset** points like other races preset paths |
| Respec | **`RacialCommitmentPolicy.RespecAllowed`** — full or partial refund per design rules |
| Ultimate / key passive | **Mutually exclusive** choice groups at designated clusters (at most one picked per group) |
| Paragon / post-50 | **Out of scope** unless JRogue adds a parallel endgame board later |

**Not** Barbarian Spirit Imprint: Undead do **not** use “single forward-only `chosenPathNodeIds` only” as the only model; they use **rank per node** + **refund** + **cluster gates**.

---

## 4. Racial restrictions

### R4.1 — Cannot drink potions — **Required**

- Undead actors **cannot consume** items with `ItemCategory.Potion` (includes healing, mana, buff potions unless a specific item is later whitelisted by id).
- Enforcement points (all that apply in v0):
  - **UI:** `InventoryUsability.AppearsUsableNow` (or successor) returns **false** for potions when `CharacterStats.race == Race.Undead`.
  - **Use pipeline:** consume/activate handler **rejects** potion use for Undead even if UI is bypassed (debug/commands).
- **Feedback:** clear failure reason in UI/log (e.g. “Undead cannot drink potions”).
- **Allies:** Undead cannot drink potions from their own bag; whether an ally can **feed** a potion to an Undead is **out of v0** — default **no** unless added explicitly.

### R4.2 — Scrolls and other consumables

- **Scrolls** are **not** banned by this restriction unless content adds a separate rule.
- Other `ItemCategory` consumables (evocables, etc.) follow global rules unless an Undead-specific restriction is added later.

---

## 5. Racial benefits

### B5.1 — Opposite of “cannot drink potions” — **Required (paired benefit)**

The potion ban must have a **documented alternate** way to achieve what potions normally provide (healing, curing, buffs). This is a **design pairing**, not an automatic mirror in code.

**Requirement:**

- Baseline Undead package includes **at least one** sanctioned healing or recovery channel that potions would have covered for other folk.
- **v0 content (minimum):** define the channel in data/docs even if implementation is stubbed—e.g. “**Necrotic sustenance**” consumable category, **rest at grave shrines**, **life drain** active from skill tree, or **essence**-based recovery only.
- **Implementation v0:** may be a **passive placeholder** + inventory category whitelist, as long as eligibility differs from `ItemCategory.Potion`.

**Open (content):** exact fiction and item categories for the alternate channel—see **§12**.

### B5.2 — Immune to Poison — **Required**

- Undead are **immune to Poison damage** and **poisoned** status effects (when those systems exist).
- **Baseline:** express via `resistanceModifiers` on `DefaultUndeadRacialLoadout` and/or a dedicated **`PassiveEffect`** (preferred if combat uses status immunity flags not driven by resistance alone).
- **Immunity definition:** Poison damage dealt to Undead is **0** (or fully absorbed); poison DoT / “Poisoned” debuff application **fails** with no partial stacks unless a future boss effect explicitly uses an “ignores poison immunity” tag (out of v0).
- **Stacking:** racial source is the loadout (or passive asset referenced by loadout). Essences/items may add **other** resistances; they must not accidentally **reduce** Poison immunity below immune without an explicit curse/artifact rule.

---

## 6. Racial stat modifications

### S6.1 — Weakness to Radiant damage — **Required**

- Undead have **−50** effective resistance to **`DamageType.Radiant`** (weakness: takes **more** Radiant damage than neutral).
- Author on **`DefaultUndeadRacialLoadout`** via `resistanceModifiers`:

  ```yaml
  - type: Radiant
    value: -50
  ```

- Uses the same `DamageResistanceModifier` shape as Tiefling Fire resistance (`type` + `value`); applied with the loadout as **modifier source** for add/remove.
- **Modifiable:** items, essences, buffs, and skill-tree nodes may add Radiant resistance (positive `value`) that **stacks** with the racial −50 per global modifier pipeline (Phase 0 stacking rules). Example: racial −50 + item +30 ⇒ net −20 unless caps are defined elsewhere.

### S6.2 — Other baseline stats

- No additional baseline stat mods are required for v0 beyond Poison immunity / Radiant weakness and restriction passives.
- Content may add empty or placeholder stat rows later.

---

## 7. Undead skill tree (subsystem)

### U7.1 — Purpose

- Primary **Undead progression** is a **Diablo 4–style** skill tree: clusters, typed nodes, per-node ranks, point pool, **respec**.
- Distinct from **folk baseline** (§4–§6).

### U7.2 — Data model (minimum)

| Asset / type | Role |
|--------------|------|
| **`UndeadSkillTreeDefinition`** | ScriptableObject: clusters, nodes, edges, exclusivity groups, cluster unlock thresholds |
| **`UndeadSkillTreeNodeData`** | Per node: identity, topology; implements **`IRacialProgressionPayload`** (or embeds **`RacialProgressionPayload`**) — same contract as **`CyborgImplantDefinition`** |
| **`UndeadSkillTreeRuntime`** | MonoBehaviour: `RacialProgressionPayloadApplicator` per ranked node; **stable source per `nodeId`** |

#### U7.2.1 — `IRacialProgressionPayload` (shared with Tiefling implants) — **Required**

**Code contract** (implemented for Tiefling; Undead nodes must match):

| Type | Role |
|------|------|
| **`IRacialProgressionPayload`** | Interface: five payload categories (zero or more each) |
| **`RacialProgressionPayload`** | `[Serializable]` embeddable list bag for Undead node authoring |
| **`RacialBenefitDefinition`** | `ScriptableObject` base for benefit assets (`OnApply` / `OnRemove` / `Refresh` / `OnTurnStart`) |
| **`RacialRestrictionDefinition`** | `ScriptableObject` base for restriction assets (`OnApply` / `OnRemove`) |
| **`RacialProgressionPayloadApplicator`** | Apply/remove/refresh all categories with a stable **source** object |

**`CyborgImplantDefinition`** already implements **`IRacialProgressionPayload`** (see [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) §6.1).

**Payload categories** (every category may be **empty**; any combination is valid):

| Category | List field(s) | Notes |
|----------|---------------|--------|
| **Racial restrictions** | `racialRestrictions` | `RacialRestrictionDefinition` assets; active while node ranked / implant installed |
| **Racial benefits** | `racialBenefits` | `RacialBenefitDefinition` assets |
| **Racial stat modifications** | `statModifiers`, `resistanceModifiers` | Same shapes as `EssenceData` / loadout |
| **Passive abilities** | `passiveEffects` | `PassiveEffect` assets |
| **Active abilities** | `activeAbilities` | `AbilityAction` references (F9.12) |

**Resolved rules:**

- **Zero or more** entries per list per node—no minimum payload on non-root nodes.
- **Per rank:** When a node supports multiple ranks (`maxRanks` &gt; 1), implementation must define whether payloads **scale with rank** (e.g. duplicate modifier magnitude × rank) or are **flat while rank ≥ 1**—document the chosen rule in code; v0 samples may use **flat while ranked** unless a sample explicitly demonstrates scaling.
- **Apply/remove:** On rank increase, add that node’s payloads under source `nodeId`; on rank decrease or respec refund, remove **all** categories from that source for that node.
- **Stacking:** Tree-node restrictions/benefits **stack with** folk baseline (and gear) using **distinct sources**; baseline potion ban is not removed by ranking a node unless a node’s benefit explicitly overrides (content must say so).

**Other node fields (minimum):** `nodeId`, display name/description, `UndeadSkillNodeKind` (`Skill` / `Upgrade` / `Passive`), `maxRanks`, parent prerequisites, cluster id, optional `mutualExclusivityGroupId`.

**Save fields (proposed):**

- `undeadSkillPointsAvailable` (int)
- `undeadSkillNodeRanks` — map or list of `{ nodeId, rank }` for nodes with rank &gt; 0
- Optional: `undeadSkillTreeGraphVersion` during iteration

### U7.3 — Allocation rules

- Spending a point increases **rank** on one node if: cluster is **unlocked**, prerequisites satisfied, rank &lt; `maxRanks`, and player has **unspent** points.
- **Upgrade** nodes require their parent **Skill** node at least rank 1 (or max—document per node in content).
- **Exclusivity:** nodes in the same `mutualExclusivityGroupId` — at most **one** node in the group may have rank &gt; 0 (Ultimate / key-passive pattern).

### U7.4 — Respec rules — **Required**

- Undead may **refund** ranks and reallocate (policy `RespecAllowed`).
- On refund: remove **all** payload categories (restrictions, benefits, stat/resistance mods, passives, actives) from that node’s source; restore point to pool.
- **v0:** respec may be **free**, **debug-only**, or gated by a simple cost—document the chosen rule in implementation PR; default **free in editor / dev**, designer-toggle for shipping.
- **Partial respec:** allowed (refund one node at a time) unless content requires full reset at a shrine.

### U7.5 — v0 authoring

- Like other racial vertical slices: **preset** `undeadSkillNodeRanks` and point pool on **`UndeadPlayer`** prefab for playtests.
- **Later:** level-ups and NPCs grant `undeadSkillPointsAvailable`.

### U7.6 — Composition order (recommended)

1. `RacialLoadoutApplier` — `DefaultUndeadRacialLoadout` (restrictions, benefits, Radiant/Poison baseline)
2. `UndeadSkillTreeRuntime` — allocated node ranks only

Passives: single `Refresh` / `OnTurnStart` participation via `RacialPassiveHooks` without duplicate firing per source.

### U7.7 — Reuse vs new code

- **Reuse:** **`RacialProgressionPayloadApplicator`** (wraps `RacialAbilityPayloadApplicator` for stats/passives); Spirit Imprint graph validation where topology fits.
- **Done (Tiefling):** `IRacialProgressionPayload` on **`CyborgImplantDefinition`**; **`TieflingImplantsRuntime`** calls shared applicator.
- **Implemented:** `UndeadSkillTreeDefinition`, `UndeadSkillTreeNodeData` (**`IRacialProgressionPayload`**), `UndeadSkillTreeRuntime`, point pool, ranks, cluster gates, respec/refund API.

### U7.8 — Sample Undead skills (content) — **Required**

Create **sample skill-tree content** under e.g. `Assets/Data/Racial/Undead/SkillTree/` so vertical-slice playtests do not rely on an empty tree.

**Minimum tree (`UndeadSkillTree_Sample` or equivalent):**

- At least **3 clusters** (e.g. Basic / Core / Class) with documented unlock thresholds.
- At least **8 nodes** total, mixing `Skill`, `Upgrade`, and `Passive` kinds.
- At least **one** exclusivity group (e.g. pick-one Ultimate or key passive).

**Minimum sample skills (nodes)—prove payload categories:**

| Sample node (working name) | Kind | Payload intent (v0) |
|----------------------------|------|---------------------|
| **Grave Touch** | Skill | `activeAbilities` only (data-only in shipping builds) |
| **Grave Touch — Linger** | Upgrade | Parent = Grave Touch; `passiveEffects` only |
| **Calcified Hide** | Passive | `statModifiers` only (e.g. +1 Constitution per rank or flat) |
| **Embrace the Dark** | Passive | `resistanceModifiers` only (e.g. +N Necrotic) |
| **Pale Consumption** | Skill | `racialBenefits` only (e.g. enables necrotic sustenance channel—paired-benefit proof) |
| **Sun-scorched** | Passive | `racialRestrictions` only (e.g. extra Radiant vulnerability while ranked—optional stacking with baseline −50) |
| **Bone Mend** | Passive | `statModifiers` + `passiveEffects` (mixed lists) |
| **Lich’s Bargain** | Skill (exclusivity group) | `racialBenefits` + `racialRestrictions` on same node (tradeoff node) |

Exact names, numbers, and ability assets are **content**; the **requirement** is that shipped samples exist and that **each of the five payload categories** appears on at least one node in the sample tree.

**`UndeadPlayer` preset:** Wire the sample tree on `UndeadSkillTreeRuntime` with enough **preset points** to rank at least **Grave Touch** (rank 1) and **Calcified Hide** (rank 1) for default playtest.

---

## 8. Deliverables: `UndeadPlayer` prefab — **Required**

Create **`Assets/Prefabs/Actor/Race/UndeadPlayer.prefab`** as a **variant** of the shared **`Player`** prefab (same pattern as `DwarfPlayer` / `TieflingPlayer`).

| Field / component | Value |
|-------------------|--------|
| `m_Name` | `UndeadPlayer` |
| `CharacterStats.race` | `Race.Undead` (`8`) |
| `CharacterStats.racialSubsystem` | `RacialSubsystemKind.UndeadSkillTree` (proposed next id: `6` or next free byte—assign when implementing) |
| `CharacterStats.bodyCapabilities` | TBD (authoring default; document when equip rules exist) |
| `RacialLoadoutApplier.loadout` | **`DefaultUndeadRacialLoadout`** (`Assets/Data/Racial/Undead/DefaultUndeadRacialLoadout.asset`) |
| Other player components | Inherited from base **`Player`** prefab |

**When skill tree runtime ships:** add **`UndeadSkillTreeRuntime`** on the same prefab with a **preset** node rank map for vertical-slice testing (e.g. one point in a Basic passive). Do **not** create a second Undead player prefab.

**Status:** **Created** — `Assets/Prefabs/Actor/Race/UndeadPlayer.prefab` with `UndeadSkillTreeRuntime` and preset ranks on **Grave Touch** + **Calcified Hide**.

---

## 9. Functional requirements

**F9.1 — Eligibility**  
Only `Race.Undead` actors use Undead skill tree components; others ignore or omit them.

**F9.2 — Potion ban**  
Undead cannot use `ItemCategory.Potion` through UI or consume pipeline.

**F9.3 — Paired benefit**  
At least one non-potion recovery channel is defined and wired enough to prove the pairing (stub acceptable in v0 if documented).

**F9.4 — Poison immunity**  
Poison damage and poison status do not harm Undead under normal rules.

**F9.5 — Radiant weakness**  
Baseline −50 Radiant resistance applies from loadout source; removable only by overriding modifiers from other systems, not by disabling the loadout silently.

**F9.6 — Skill allocation**  
Valid spend increases node rank and applies **all non-empty payload lists** on that node; invalid spend is rejected with reason.

**F9.6b — Node payload categories**  
Per node: **zero or more** racial restrictions, **zero or more** racial benefits, **zero or more** racial stat modifications (`statModifiers` / `resistanceModifiers`), **zero or more** passive abilities, **zero or more** active abilities.

**F9.7 — Respec**  
Refund reduces rank, removes that node’s payloads, returns points per U7.4.

**F9.8 — Cluster gates**  
Nodes in locked clusters cannot receive points until thresholds are met.

**F9.9 — Exclusivity**  
Mutually exclusive groups enforce at most one non-zero node per group.

**F9.10 — Presets (v0)**  
Player and NPC Undead may use serialized point pool + ranks; default NPC may be **empty tree** + baseline only.

**F9.11 — Integration hooks**  
Tree passives use the same refresh/turn pipeline as racial loadout and imprint.

**F9.12 — Actives on nodes**  
Same policy as Phase 3 §7.5: **data-only** in shipping v0 unless test-only execution is explicitly gated.

---

## 10. Non-functional requirements

**N10.1 — Authoring**  
Designers edit tree and baseline in Editor—no per-node code subclasses.

**N10.2 — Tests**  
At minimum:

- Undead + potion → not usable / use rejected.
- Undead + Radiant hit → takes increased damage vs neutral target (when combat applies resistance).
- Undead + Poison → no damage / no poison status.
- Allocate rank → modifier applies; respec → modifier removed, point returned.
- Exclusivity group → second node in group cannot be ranked.
- Save round-trip for point pool and node ranks.
- Non-Undead actor → tree runtime no-op.

**N10.3 — Migration**  
Existing actors without Undead tree state: empty ranks, baseline loadout only.

---

## 11. Future: events that alter the skill tree — **Not v0**

Document now so saves and assets stay compatible:

| Event type (examples) | Requirement on framework |
|-----------------------|---------------------------|
| Grant **bonus points** | Increase `undeadSkillPointsAvailable` |
| **Lock** a node or cluster | Node flag `lockedByEventId`; allocation rejected |
| **Unlock** hidden branch | Reveal nodes or lower cluster threshold |
| **Transform** a node | Swap payload asset id while preserving rank, or force respec |
| **Curse** | Negative ranks or mandatory “dead” nodes—needs explicit design |

**v0:** none of the above are implemented; preset-only trees.

---

## 12. Design decisions (resolved + open)

### 12.1 — Separate restriction vs benefit buckets — **Resolved: yes** (§2)

### 12.2 — Skill tree model — **Resolved: Diablo 4–style, respec allowed**

### 12.3 — Barbarian imprint — **Not used as the sole model**

Forward-only permanent path is **wrong** for Undead; reuse **node payloads** and validation only.

### 12.4 — Open (content, not blocking requirements)

- Exact **paired benefit** fiction (necrotic vials, shrine rest, drain, etc.).
- **Skill point** earn rate (level curve, quests).
- **Respec cost** (gold, soul, free at NPC).
- Final balance numbers on sample skills (names are fixed in §7.8 as working titles).
- **`bodyCapabilities`** default on `UndeadPlayer`.
- UI: skill panel layout (cluster columns vs radial), respec button, restriction tooltips on potions.

---

## 13. Content assets (minimum for v0 proof)

| Asset | Purpose | Status |
|-------|---------|--------|
| `DefaultUndeadRacialLoadout` | `requiredRace: Undead`; Radiant −50; Poison +999; necrotic sustenance passive | **Created** |
| `UndeadPlayer` prefab | Variant of `Player`; baseline + skill tree runtime | **Created** |
| `UndeadSkillTree_Sample` | Sample tree per §7.8 (9 nodes, 3 clusters, exclusivity) | **Created** |
| Sample abilities / passives / benefit / restriction assets | Under `Assets/Data/Racial/Undead/` and `Assets/Data/Ability/` | **Created** |
| `UndeadSkillTreeRuntime` + tests | `Assets/Tests/UnitTests/Racial/UndeadSkillTreeRuntimeTests.cs` | **Created** |

---

## 14. Acceptance criteria (examples)

- Given an **Undead** with a healing potion in bag, **Use** does not appear (or fails) with a clear reason.
- Given a **Human** with the same potion, use still works (regression).
- Given Undead baseline only, **Radiant** resistance total includes **−50** from racial source.
- Given Undead baseline, **Poison** damage is **0** and poison status does not apply.
- Given **10** unspent skill points and an unlocked node, spending 1 point sets rank 1 and applies that node’s stat mod.
- Given rank 1 on a node, **respec** removes the mod and returns 1 point.
- Given two nodes in the same exclusivity group, ranking one **blocks** ranking the other.
- Given **`UndeadPlayer.prefab`** in scene, `CharacterStats.race == Undead` and `DefaultUndeadRacialLoadout` is applied via `RacialLoadoutApplier`.
- Given preset ranks on **Grave Touch** and **Calcified Hide**, only those nodes’ payload categories apply; unranked sample nodes apply nothing.
- Given **Lich’s Bargain** at rank 1, both its `racialBenefits` and `racialRestrictions` are active; refunding rank removes both.
- Given the sample tree asset in project, **each** of the five payload **categories** (§7.2.1) is used on at least one sample node.

---

## 15. Code touchpoints (implementation checklist)

| Area | Action |
|------|--------|
| `RacialSubsystemKind` | `UndeadSkillTree = 6` |
| Inventory | `InventoryUsability` + `InventoryConsumePolicy` potion ban |
| Data | `Assets/Data/Racial/Undead/` — loadout, tree, effects |
| Runtime | `UndeadSkillTreeRuntime` + `RacialPassiveHooks` wiring |
| Prefabs | `UndeadPlayer.prefab` |
| Tests | `UndeadSkillTreeRuntimeTests.cs` |
| Saves | Serialized `presetNodeRanks` on runtime (full save blob later) |
| UI | Skill tree panel, respec UX — **still open** |

---

## 16. Related documents

- [Phase 0 — Glossary and data contracts](Phase0-Glossary-And-Data-Contracts.md)
- [Phase 1 — Implementation summary](Phase1-Implementation.md)
- [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (node payloads, not progression policy)
- [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) (respec / replace policy)
- [Beastman — Soul Beast](Beastman-Soul-Beast-Requirements.md) (prefab deliverable pattern)
- [Phase 5 — Additional folk & subsystem shapes](Phase5-Requirements.md)
