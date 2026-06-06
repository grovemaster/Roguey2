# Phase 3 — Requirements (refined): Barbarian Spirit Imprint v0

Phase 3 is the first **race-specific progression vertical**: **Barbarian** + **Spirit Imprint**, using existing `RacialLoadoutDefinition` / `RacialLoadoutApplier` / passive hooks where possible, and adding only the data + runtime needed for a **small, revisable** imprint tree.

**Out of scope for Phase 3** (unless explicitly pulled in): equipment legality / body-capability overrides, Elf contracts, Tiefling respec, Human class. **Shipped gameplay:** imprint **actives** are **data-only** on nodes (no player hotbar). **Development:** a **test-only** path may execute a racial `AbilityAction` in-scene for validation; it **must not** exist in final builds (see §7.5, N4.4).

---

## 1. Goals

**G1 — Vertical slice**  
A Barbarian actor can have a **Spirit Imprint rank** and **chosen imprint nodes** that grant passives (and optionally reference actives as data) with **permanent** choices for Barbarians.

**G2 — NPC defaults**  
Anonymous Barbarian NPCs default to **rank 0** (or equivalent): only the **root** imprint node (see **D2.0**), no extra branches, unless an archetype asset overrides.

**G3 — Player-facing clarity**  
Designers can author and inspect imprint structure in the Editor; debug logs or a minimal overlay are acceptable instead of full UI.

**G4 — Persistence**  
Chosen nodes and rank (or equivalent) serialize with the actor or save blob you use for party state; loading restores the same mechanical state without manual re-pick.

---

## 2. Data model

### D2.0 — Root node = imprint level 0 (required)

- The **first node** of the Spirit Imprint graph (the **root**) is the canonical **“imprint exists but is at level 0”** state.
- That root node **carries no gameplay payload**: **no** stat modifications, **no** passive effects, and **no** active abilities (all lists / references empty). It is still a real node with a stable **id** (for saves and UI) and display copy such as “Spirit Imprint (dormant)” or “Rank 0” as you prefer.
- **Rank 0** Barbarians (including default NPCs) are defined as: **imprint rank** `0` **and** the only committed/chosen imprint position is this **root** (or equivalently: chosen-node set contains only the root id—pick one representation and document it).
- Advancing the imprint (e.g. to “level 1”) means unlocking or committing **non-root** nodes per your progression rules; the root remains in the chosen path as ancestry for the graph.

### D2.1 — Spirit Imprint graph (tree, forward-only)

- **Resolved topology:** **Tree only** (each node has at most one parent; root has none).
- **Resolved navigation:** The Barbarian **never moves backward** along the imprint: no unpicking nodes, no replacing a parent with a different branch after a deeper child is chosen (unless you later add a rare story exception—out of scope for Phase 3). New picks only **extend** the path **forward** toward children. Permanent choices + forward-only match the “single path” fantasy of progressing deeper into the imprint.
- **Node** fields (minimum), **in addition to D2.0** for non-root nodes (non-root may use any subset of payloads; root must stay empty):

  - Stable **id** (string or int) for saves.
  - **Display name** + optional description.
  - **Parent link** or **child links** (tree: one parent per non-root node).
  - **Optional depth gate:** Prefer **depth-from-root** (or “must have picked ancestor X”) instead of a separate **`minRankToPick`** field whenever possible — because **`imprintRank` is derived from path length** (D2.2), a numeric rank gate is usually **redundant** with “must be at least N nodes deep” unless you intentionally decouple them (not allowed in v0 per rank invariant).
  - **Payload** — **resolved:** use **lists** (each may be empty) so one node on the path can carry everything significant for that beat:
    - **Stat modifications** — list, same shape as `RacialLoadoutDefinition` / `EssenceData` attribute modifiers (and optionally resistance rows).
    - **Passive abilities** — list of `PassiveEffect` references.
    - **Active abilities** — list of `AbilityAction` references (execution policy per §7.5).
  - **Exclusivity** (optional v0): e.g. “choose at most one child of parent P” via **exclusivity group id** on edges or nodes.

### D2.2 — Barbarian runtime state (on actor or dedicated component)

- `RacialSubsystemKind` = `SpiritImprintBarbarian` when applicable.
- **`chosenPathNodeIds`:** **Ordered list** of node ids from **root → deepest chosen** (see §7.7). This is the canonical spine save.
- **`imprintRank`:** **Derived invariant:** must always equal the number of **non-root** nodes on the path — i.e. `imprintRank == chosenPathNodeIds.Count - 1` when the list always starts with the root id. **Rank never runs ahead** of picks: each **non-root** node on the path represents one **committed** step along the imprint; there is no separate XP pool that raises rank without extending the path.
- **Phase 3 v0 — where the path comes from:** For this phase, each Barbarian’s imprint **path is fixed before play**: set via **prefab / serialized component / preset asset** (authoring-time), not by a runtime “pick UI” or world gate. You **compile**, place the actor with the desired `chosenPathNodeIds`, and **enter play mode** — the runtime **resolves and applies** imprint effects from that list only.
- **Later phase (not Phase 3):** A **special NPC** (or event) will **authorize the next single-node** extension of the path; that gate and persistence flow are **implemented with the NPC**, not required for Phase 3 delivery. **See:** [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md).
- **Single-node advancement (when dynamic progression exists):** A Barbarian gains **at most one new node per progression event** — never append two or more new ids in one unlock transaction.
- **Commitment policy** = `Permanent` for Barbarian (no respec UI this phase).

### D2.3 — Composition with `RacialLoadoutApplier` — **Resolved: Pattern B**

- **`RacialLoadoutApplier`** holds only the **static** Barbarian **`RacialLoadoutDefinition`** (baseline); it is **not** merged with imprint node payloads.
- **Spirit Imprint** is applied by a **separate runtime** (e.g. `SpiritImprintRuntime`): node payloads use **distinct sources** (e.g. per `nodeId`) for stats and passives so stack/remove and lifecycle hooks stay correct next to the applier.
- Use a **coordinator** or strict call order so imprint passives receive **`Refresh`** / **`OnTurnStart`** wherever required (alongside `RacialPassiveHooks` / essence).

### D2.4 — Content assets

- At least one **`RacialLoadoutDefinition`** (or composed equivalent) for **Barbarian baseline** (can start empty except `requiredRace` / subsystem flags).
- A **Spirit Imprint graph asset** (ScriptableObject or table) holding at least: the **empty root** (D2.0) + enough non-root nodes to prove **one** progression step (e.g. one child of root with a trivial stat or passive), and optionally **one branch split** (two children, pick one) to prove exclusivity.

---

## 3. Functional requirements

**F3.1 — Eligibility**

- Only `Race.Barbarian` actors participate in Spirit Imprint logic; others ignore the component (or it is absent).

**F3.2 — Path resolution (forward-only, preset in Phase 3 v0; NPC later; one node per advance)**

- **Invariant:** `imprintRank == chosenPathNodeIds.Count - 1` (root first in list). **Rank never runs ahead** of the path.
- **Phase 3 v0:** `chosenPathNodeIds` is **authored before play** (prefab / component / preset). No in-world **NPC gate** or runtime branch-picker is required for this phase. **Debug** may still mutate the path in-editor or via dev-only hooks to test mechanics.
- **Later (with special NPC):** Each progression event grants **at most one** new node — append **exactly one** child id along a valid forward edge, then re-apply effects and save. No batch multi-node unlock in a single transaction. **Shaman NPC spec:** [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md).
- **Forward-only:** The stored path is always a valid root-to-leaf walk in the tree; no backward moves or mid-tree swaps.

**F3.2b — Depth / rank gates**

- With **`imprintRank == chosenPathNodeIds.Count - 1`**, prefer gating (in data) by **depth-from-root** or **required ancestors**, not a second floating counter. Remove **F3.2b** if all gates are expressed as tree constraints only.

**F3.3 — Exclusivity (if in v0)**

- Selecting a node in an exclusivity group **locks** siblings per design rules; deselect is not allowed for Barbarian.

**F3.4 — Presets (player and enemies)**

- **Enemies:** Barbarian (and other) enemies **always** use **preset** imprint data — same serialized `chosenPathNodeIds` / graph reference pattern as the player for consistency; no expectation of mid-combat imprint growth unless a future system explicitly adds it.
- **Player / allies:** In Phase 3 v0, imprint path is also **preset** before play (see F3.2). Default anonymous Barbarian NPC: **root only** (rank 0). Named or boss variants: richer **preset** path in data.

**F3.5 — Integration with existing hooks**

- Imprint passives participate in **`Refresh`** / **`OnTurnStart`** the same way as racial loadout / essence passives (no duplicate firing unless explicitly designed).

**F3.6 — Active abilities on nodes**

- **Shipped / data:** Each node carries a **list** of `AbilityAction` references for authoring and future unified hotbar (later feature). **No** player-facing execution from imprint in Phase 3 builds.
- **Test-only execution:** For current-scene testing, a **development-only** entry point may call `AbilityAction.Execute` (or your command path) for a selected imprint active — e.g. debug menu, `[SerializeField]` test button on a dev object, or keyboard gated by `#if UNITY_EDITOR` / `DEVELOPMENT_BUILD` / custom scripting define. **Requirement:** this path **must be stripped or hard-disabled** in release so it is **not** available in the final game (see N4.4).

---

## 4. Non-functional requirements

**N4.1 — Authoring**  
Designers add nodes by **editing assets**, not by writing new `MonoBehaviour` subclasses per node.

**N4.2 — Tests**  
At least: graph resolution from a **preset** `chosenPathNodeIds` (or dev-only single-node append) → expected passive/stat applied; save/load or serialize round-trip for **`chosenPathNodeIds`** and derived rank; **rank 0 = root only**; invariant **`imprintRank == chosenPathNodeIds.Count - 1`** when root is first.

**N4.3 — Migration**  
Existing Barbarian actors without imprint state default to **rank 0 / root** without breaking saves.

**N4.4 — Test-only racial active trigger (non-shipping)**  
Any in-scene or hotkey path that fires an imprint **active** for development **must** be compiled out, `#if`-stripped, or behind a define that is **off** in player/release builds, and **must not** ship in the final game. Document the define(s) in code review. The eventual **unified ability hotbar** (race + other actives) replaces this hack.

---

## 5. Acceptance criteria (examples)

- Given a Barbarian at imprint rank 0, the **root** is the only chosen node and **no** imprint-driven stat/passive/active applies from the graph.
- Given rank 1 and a **preset** path whose deepest node grants +1 Strength, that modifier applies and persists after save/load.
- Given exclusivity between two branch children, a valid path that commits to one branch **cannot** be extended to include the excluded sibling; validation (and reload) preserve that rule.
- Given a random Barbarian NPC with default preset, behavior matches “minimal imprint” (rank 0, root only).

---

## 6. Phase 2 follow-ups (optional, not blocking Phase 3)

- Every **party member** prefab that is not the shared Player prefab should use the same **`DefaultHumanRacialLoadout`** (or `requiredRace: Unset` empty asset) + `RacialLoadoutApplier` if you want parity with the Player prefab hookup.

---

## 7. Design decisions (resolved + elaboration)

### 7.1 Topology — **Resolved: tree, forward-only**

- **Tree:** One parent per non-root node; no joins, no cycles; saves and validation stay simple.
- **Forward-only:** Once the path includes a node, the Barbarian does not **retract** to an earlier depth or **swap** a committed branch for a sibling at the same depth. Progression only **appends** picks along valid children. That matches “imprint deepens” and avoids respec-like behavior without building an undo stack.
- **Implication:** A “wrong” pick is permanent by design; branch exclusivity at a parent still works (pick child A **or** B, then continue forward from that child only).

### 7.2 Pattern A vs B — **Resolved: Pattern B**

- **`RacialLoadoutApplier`** = static Barbarian baseline `RacialLoadoutDefinition` only.
- **Spirit Imprint** = separate runtime applying node payloads with **per-node sources**; coordinate `Refresh` / `OnTurnStart` with the rest of the actor (see D2.3).
- **Why not A for this project:** You want base race and imprint **separate** in tooling and saves; Pattern B matches that without a merge composer.

*(Pattern A comparison remains valid for other races or tools but is **out** for Barbarian Spirit Imprint v0.)*

### 7.3 Imprint progression — **Phase 3 v0: preset path; NPC + single-node advance later; no numeric formula**

- **This phase:** Imprint **state is authored** before running the game (serialized `chosenPathNodeIds` on the actor or a linked preset). The code **validates** the path against the tree and **applies** Pattern B effects at runtime. **No** special-NPC gate, no runtime “pick next node” flow required for Phase 3 completion.
- **Later phase (with NPC):** A dedicated **NPC / event** authorizes extending the path by **exactly one** child node per interaction (no multi-node batch). **No XP / imprint score formula** is required then either; story gating is enough. **Shaman NPC spec:** [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md).
- **Rank rule (hard):** `imprintRank` **always** equals non-root count on the spine (`chosenPathNodeIds.Count - 1` with root leading the list).

### 7.4 UI — **Resolved: debug for now; race-specific character sheet later**

- **Agreed:** Phase 3 does **not** require a character sheet. **Debug** (commands, small menu, logging) is enough to pick nodes, inspect rank/path, and verify saves.
- **Later:** A **race-specific character sheet** (Barbarian → Spirit Imprint panel; other races → their own layout) is a sensible **post–Phase 3 UI** milestone once mechanics exist—no need to block imprint on bespoke UI.

### 7.5 Active abilities — **Resolved: data-only in shipping builds; test-only trigger for dev**

- **Shipping (Phase 3):** Node **`AbilityAction` lists are data only** — authored and saved, **not** fired through a player hotbar. The future **unified ability hotbar** (race + items + essences, etc.) is explicitly **later**.
- **Testing in current scenes:** A **test-only** path is allowed: e.g. debug key, dev `MonoBehaviour` button, or menu command that invokes **`AbilityAction.Execute`** on a chosen imprint active for the selected actor. **Must** comply with **N4.4** (no release / final-player access).
- **Pros of this split:** You can balance and script actives early **without** building the hotbar; you still get **manual** combat validation when needed.
- **Cons:** Easy to forget the dev gate—treat **stripping the test path** as a **release checklist** item.

### 7.6 Payload cardinality — **Resolved: lists per node**

- Each node carries **lists** of stat mods, passives, and actives so one **significant** step on the path can bundle everything for that tier.
- **Apply/remove** must walk lists in a **fixed order** and tag modifiers with a **stable source** (e.g. node id) so when the path **extends**, earlier nodes’ effects stay and new ones add on top (forward-only does not remove parent payloads).

### 7.7 Save representation — **Resolved: `chosenPathNodeIds`; fixed tree size at ship**

- **Canonical save field:** **`chosenPathNodeIds`** — ordered **`[root, …, deepest chosen]`**. Best fit for a forward-only tree; validates in O(n) against parent pointers in the graph asset.
- **`imprintRank`:** **Redundant but allowed** for readability if it is **always derived** from the list on load and **asserted equal** to `Count - 1` (non-root count). Never persist a rank that disagrees with the path.
- **Content stability:** Once you lock the **final** Spirit Imprint tree for ship, you expect **no further graph edits** in production; size is modest (**≤ ~50 nodes**, depth **≤ ~10–20** leaves from root). That keeps saves small and validation cheap.
- **`graphVersion`:** Optional during heavy iteration; once the tree is **frozen**, version bumps are rare (only if you ship a sequel-scale rebalance).
- **Corrupt save:** Validate on load; on failure → **root only**, `imprintRank == 0`, log warning.

