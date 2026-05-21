# Tiefling — Cyborg implants (requirements)

Tieflings use a racial subsystem distinct from **Barbarian Spirit Imprint** and **Elf Elemental Spirits**: **slot-based cyborg implants** that can be **replaced per location** (old implant removed, new implant applied). A racial **Fire damage resistance** and **horns** (blocking helmets) come from the **folk baseline**, not from the implant system.

**Subsystem kind (code):** `RacialSubsystemKind.TieflingImplants` (see `RacialSubsystemKind.cs`).

**Commitment policy:** `RacialCommitmentPolicy.RespecAllowed` — implant choices in a slot may change; contrast with Barbarian **Permanent** imprint path.

**Depends on:** Phase 1–2 (`RacialLoadoutDefinition` / `RacialLoadoutApplier`), Phase 4 (`BodyCapabilityFlags.Horns`, `EquipmentLegalityEvaluator`, helmet `equipExcludesActorFlags`).

**Contrast:** [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) (summon/upkeep, abilities only while summoned). [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (single forward-only tree; no benefit/restriction lists on nodes). [Undead — Race](Undead-Race-Requirements.md) (D4 skill tree; **same `IRacialProgressionPayload`** on nodes as Tiefling implants).

**Shared code:** `IRacialProgressionPayload`, `RacialBenefitDefinition`, `RacialRestrictionDefinition`, `RacialProgressionPayloadApplicator` in `Assets/Data/Racial/`. **`CyborgImplantDefinition`** implements the interface; Undead skill nodes will use the same contract.

---

## 1. Goals

**G1 — Vertical slice (v0)**  
A Tiefling actor has a **predetermined implant in each filled slot** (preset loadout), **Fire resistance** and **horns** from folk baseline, and **cannot equip helmets** under Phase 4 rules. The runtime can **replace** the implant in a given slot and correctly **tear down** the old implant before applying the new one.

**G2 — Data-driven implants**  
Designers author implants as assets (stat mods, passives, actives) and assign them to **body locations** without new code per implant.

**G3 — Replace correctness**  
Swapping an implant in one slot **fully removes** the previous implant’s stats, passives, and actives (stable sources) before applying the replacement—no stacked duplicates from the same slot.

**G4 — NPC / preset parity**  
v0: implants come from **prefab / serialized preset** (like Barbarian imprint path and Elf contract roster). **Later:** special NPC installs or offers swaps from an authored catalog.

**G6 — Playable Tiefling prefab**  
A dedicated **`TieflingPlayer`** actor prefab exists (same family as `HumanPlayer` / `BarbarianPlayer`) so designers can place or spawn a correctly configured Tiefling without hand-wiring components.

**G5 — Persistence (v0 minimum)**  
Serialize **implant id per slot** (or empty slot); safe defaults on load; replace operations update save state.

---

## 2. Conceptual model (vs other folk)

| | **Barbarian — Spirit Imprint** | **Elf — Elemental Spirits** | **Tiefling — Cyborg implants** |
|--|-------------------------------|-----------------------------|----------------------------------|
| **Structure** | Single **tree**, forward-only path | Many spirits, summon state | **Fixed slots** (7 body locations) |
| **When effects apply** | Chosen path nodes (always on) | Only while **summoned** | **Always on** while implant installed in slot |
| **Change policy** | **Permanent** picks | Contract roster + levels (later); summon is fluid | **Replace per slot** (respec allowed) |
| **Ongoing cost** | None | Soul Power upkeep while summoned | None (unless a specific implant adds one later) |
| **v0 authoring** | Preset path | Preset contracts + levels | Preset **slot → implant** map |
| **Later progression** | NPC extends path one node | NPC forms contract / levels spirit | **NPC** installs or swaps implants |

---

## 3. Folk baseline (not implants)

These belong on the Tiefling **`RacialLoadoutDefinition`** (applied by `RacialLoadoutApplier`), separate from cyborg implants.

### D3.1 — Fire resistance

- Tieflings have a **racial passive resistance to Fire-type damage** (`DamageType.Fire`).
- Author via **`resistanceModifiers`** on the Tiefling baseline loadout (same shape as `EssenceData` / `RacialLoadoutDefinition`), with **`this`** loadout as modifier source.
- **Not** duplicated on every implant unless an implant explicitly adds more Fire resistance in data.

### D3.2 — Horns and helmets (Phase 4)

- Tieflings **have horns**: intrinsic `CharacterStats.bodyCapabilities` includes **`BodyCapabilityFlags.Horns`** (saved with identity snapshot).
- They **cannot equip helmets**: head-slot items that conflict with horns use **`ItemData.equipExcludesActorFlags`** including **`Horns`** (see Phase 4 / `EquipmentLegalityEvaluator`).
- **Cyborg `Head` implant slot** is **not** the same as **equipment `EquipmentSlot.Head`**: an implant in the **Head** body location does not override horn anatomy unless a specific implant data row grants **`NoHorns`** or an exclusion bypass (out of v0 unless content requests it).

---

## 4. Glossary

| Term | Meaning |
|------|--------|
| **Implant slot** | One of seven **body locations**; at most **one** implant per slot. |
| **Cyborg implant** | Data-defined package: stat mods, optional passives, optional actives. |
| **Installed** | An implant is assigned to a slot and its effects are **active**. |
| **Replace** | Remove the current implant in a slot, then install a different implant in that slot. |
| **Preset loadout (v0)** | Slot → implant mapping fixed before play (prefab / component / archetype asset). |
| **Soul Power** | Used only by implant **actives** that set `AbilityAction.soulPowerCost` &gt; 0; implants have no summon/upkeep cost unless added in data later. |

---

## 5. Implant slots

### D5.1 — `ImplantSlot` enum (required values)

| Slot | Notes |
|------|--------|
| **LeftArm** | |
| **RightArm** | |
| **Torso** | |
| **Heart** | |
| **Head** | Cyborg cranial implant; distinct from helmet equipment slot. |
| **LeftLeg** | |
| **RightLeg** | |

- **Seven** slots total; enum values are **stable** for saves (explicit numeric backing recommended, same discipline as `Race`).
- Empty slot = no implant; no modifiers from that slot.

---

## 6. Data model

### D6.1 — `CyborgImplantDefinition` (ScriptableObject or equivalent)

Per implant (stable **`implantId`** for saves):

| Field | Requirement |
|-------|-------------|
| **`implantId`** | Stable string (or int) for saves and UI. |
| **Display** | Name, description, icon optional. |
| **`allowedSlots`** | Which `ImplantSlot` values may host this implant (usually one; allow list if an implant is valid in multiple locations). |
| **`racialRestrictions`** | List, **zero or more** `RacialRestrictionDefinition` assets — **Tiefling + Undead only** (progression payload) |
| **`racialBenefits`** | List, **zero or more** `RacialBenefitDefinition` assets — **Tiefling + Undead only** |
| **Stat modifications** | `statModifiers` + `resistanceModifiers`, same shape as `RacialLoadoutDefinition` / `EssenceData` |
| **Passive abilities** | List, **zero or more** `PassiveEffect` references. |
| **Active abilities** | List, **zero or more** `AbilityAction` references. |

**Interface:** `CyborgImplantDefinition` implements **`IRacialProgressionPayload`**. Install/replace/remove/refresh uses **`RacialProgressionPayloadApplicator`** with per-slot source (`TieflingImplant:{slot}`).

**Not on folk baseline:** `DefaultTieflingRacialLoadout` keeps stats/resistances/passives only (e.g. Fire resist, horns via `bodyCapabilities`)—no `racialBenefits` / `racialRestrictions` lists on `RacialLoadoutDefinition`.

**Content rules:**

- Different implants in the same slot location **may** grant different stats and abilities.
- The same implant asset **may** be authored for multiple slots only if `allowedSlots` includes them (unusual; document per asset).

### D6.2 — Active abilities (implants)

- Reuse existing `AbilityAction` (`soulPowerCost`, `CanExecute`, `Execute`).
- Per-active flags (consumes turn, repeatable same turn) may reuse the Elf contract metadata pattern when the unified hotbar lands; v0 may be **data-only** in shipping builds with **dev-only** execution (same policy as Phase 3 / Elf docs).
- Implant actives are available whenever the implant is **installed** in a valid slot (no summon step).

### D6.3 — Tiefling runtime state (`TieflingImplantsRuntime` or equivalent)

On eligible actors (`Race.Tiefling`, subsystem `TieflingImplants`):

| State | Description |
|-------|-------------|
| **`installedImplants`** | Map or fixed array: `ImplantSlot` → `implantId` (or null / empty). |

**Apply/remove source id (required):**  
Use a **stable source per slot**, e.g. `TieflingImplant:{slot}` or the `CyborgImplantDefinition` instance plus slot disambiguation, so **replace** clears the old slot contribution before applying the new implant. **Do not** use only `implantId` as the sole source if the same implant could exist in two slots in future content.

**v0 — preset only:** `installedImplants` initialized from **prefab / serialized component / preset asset** on load; no NPC interaction.

### D6.4 — Composition with `RacialLoadoutApplier` — **Pattern B**

- **`RacialLoadoutApplier`**: Tiefling **baseline** only (Fire resistance, any folk-wide passives; **not** per-slot implants).
- **Cyborg implants**: **separate runtime** applying per-slot payloads with **distinct sources** from baseline and from other slots.
- **Coordinator:** implant passives receive **`Refresh`** / **`OnTurnStart`** with racial loadout, essences, and (on other actors) other subsystems.

### D6.5 — Content assets (v0 minimum)

- **`RacialLoadoutDefinition`** for Tiefling baseline: Fire resistance + any folk passives; `requiredRace: Tiefling`.
- At least **two** **`CyborgImplantDefinition`** ScriptableObject assets (see D6.6 — **not** GameObject prefabs), covering different slots or different payloads.
- Sample helmet **`ItemData`** (or existing asset) with `equipExcludesActorFlags` including **`Horns`** for Phase 4 validation.

### D6.6 — Deliverables: actor prefab vs implant data

**Actor prefab (required)**

- Create **`Assets/Prefabs/Actor/Race/TieflingPlayer.prefab`** as a **variant** of the shared **`Player`** prefab (same pattern as `HumanPlayer` / `BarbarianPlayer`).
- Minimum configuration on the prefab:
  - `CharacterStats.race` = `Tiefling`
  - `CharacterStats.racialSubsystem` = `TieflingImplants`
  - `CharacterStats.bodyCapabilities` includes **`Horns`**
  - `RacialLoadoutApplier` → Tiefling baseline `RacialLoadoutDefinition` (Fire resistance)
  - **Tiefling implants runtime** component with **preset** `installedImplants` map referencing implant **data assets** below
  - Other standard player components inherited from the base prefab

**Cyborg implant “prefab”? (resolved: data assets, not GameObject prefabs)**

- **v0 does not require** a **GameObject prefab** per cyborg implant. Implants are **data** (`CyborgImplantDefinition` ScriptableObjects), like essences or racial loadout rows — not world pickups or body meshes.
- **Required:** at least two implant **data assets** under a sensible folder (e.g. `Assets/Data/Racial/Tiefling/Implants/`).
- **Optional (later):** cosmetic or UI prefabs (slot icons, surgery VFX) referenced from implant data or the future NPC flow; out of v0 unless presentation is in scope.

---

## 7. Functional requirements

### 7.1 — Eligibility

**F7.1** Only `Race.Tiefling` actors with `RacialSubsystemKind.TieflingImplants` run implant logic; others ignore the component.

**F7.2** Folk baseline (`RacialLoadoutApplier`) applies even when all implant slots are empty.

### 7.2 — Install and replace (v0)

**F7.3** **Install** (slot empty): validate implant’s `allowedSlots` contains target slot → apply stats, passives, actives → record `implantId` in slot.

**F7.4** **Replace** (slot occupied):  
1. **Remove** current implant in that slot (stats, resistances if any, passives `OnRemove`, clear actives).  
2. **Install** new implant per F7.3.  
3. No duplicate modifiers from the previous implant remain.

**F7.5** v0 must expose **replace** for tests and future NPC (e.g. `TryReplaceImplant(ImplantSlot, CyborgImplantDefinition)`); UI and NPC dialog are not required for v0 completion.

**F7.6** **Remove** (slot cleared without replacement): same teardown as F7.4 step 1; slot empty.

### 7.3 — Slot rules

**F7.7** At most **one** implant per `ImplantSlot`.

**F7.8** Cannot install an implant into a slot not listed in its `allowedSlots`.

**F7.9** Replacing an implant in **LeftArm** does **not** affect **RightArm** or other slots.

### 7.4 — Baseline and equipment

**F7.10** Fire resistance from baseline loadout applies independently of implants.

**F7.11** With horns and standard helmet data, `EquipmentLegalityEvaluator` **blocks** helmet equip; implants do not bypass unless an implant registers Phase 4 bypass (document if added).

### 7.5 — Later: special NPC (out of v0)

**F7.12 (later)** NPC interaction **offers** install/replace from an authored catalog (cost, quest flags, faction, etc.).

**F7.13 (later)** NPC may gate **which** `CyborgImplantDefinition` assets are available; v0 presets skip this gate.

### 7.6 — Presets and NPCs

**F7.14** Anonymous Tiefling NPC: minimal preset (e.g. one implant or empty slots except baseline).

**F7.15** Named / boss Tieflings: richer preset maps in data.

---

## 8. Non-functional requirements

**N8.1 — Authoring**  
Designers add implants by **editing assets**, not subclasses per implant.

**N8.2 — Tests (v0 minimum)**  
- Preset load → expected stats/passives from baseline + implants.  
- Replace in one slot → old modifiers gone, new modifiers present.  
- Replace does not affect other slots.  
- Invalid slot for implant → reject.  
- Tiefling with horns cannot equip horn-excluding helmet (integration with Phase 4).  
- Fire resistance present with zero implants installed.

**N8.3 — Migration**  
Tieflings without implant state → all slots empty, baseline only.

**N8.4 — Persistence**  
Save `ImplantSlot` → `implantId`; on load, validate ids and fall back to empty slot + warning on unknown ids.

**N8.5 — Phase 4**  
Implants that change anatomy use `RegisterBodyEquipmentContribution` with keys such as `TieflingImplant:{slot}`, cleared on remove/replace.

---

## 9. Acceptance criteria (examples)

- Given a Tiefling with baseline loadout only, **Fire resistance** applies and **helmets** that exclude horns fail equip.
- Given preset **LeftArm** implant A, stats/passives from A are active; **Replace** with implant B in **LeftArm** removes A’s contribution and applies B’s.
- Given implants in **LeftArm** and **Torso**, replacing **LeftArm** leaves **Torso** unchanged.
- Given an implant valid only for **Heart**, install into **Torso** fails.
- Given preset NPC Tiefling, load/play matches serialized slot map without NPC.
- **`TieflingPlayer.prefab`** exists under `Assets/Prefabs/Actor/Race/`, drops into a scene, and enters play with Tiefling race, horns, Fire resistance, subsystem, and preset implants as authored.

---

## 10. Out of scope (v0)

- Special **NPC** install/swap flow (dialogue, shop, quest rewards).
- Full **character sheet** / implant management UI (debug acceptable).
- Unified **ability hotbar** for implant actives (follow Phase 3 / Elf policy until wired).
- Elf contracts, Barbarian imprint changes, Human class.
- **Removing horns** via implant (unless explicitly authored later with Phase 4 bypass rules).

---

## 11. Open decisions (resolve before implementation)

| # | Question | Recommendation |
|---|----------|----------------|
| O1 | Same `implantId` in two slots allowed? | **No** in v0 — one instance per slot; source id includes **slot**. |
| O2 | Replace allowed anytime in v0? | **Yes** via API/debug; **later** NPC adds when/where/cost. |
| O3 | Empty slots in preset vs require all seven filled? | **Any subset**; empty slots are valid. |
| O4 | Implant resistances stack with baseline Fire resist? | **Yes**, cross-source stacking per Phase 0. |
| O5 | `allowedSlots` missing on asset? | Treat as **invalid** at import/apply time. |

---

## 12. Relation to other docs

| Doc | Relationship |
|-----|----------------|
| [Phase0 — Glossary](Phase0-Glossary-And-Data-Contracts.md) | Add **implant slot**, **replace** when implementing. |
| [Phase3 — Barbarian Spirit Imprint](Phase3-Requirements.md) | Opposite commitment policy; same **preset v0** delivery style. |
| [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) | Different lifecycle (summon vs always-on install). |
| [Phase5 — Requirements](Phase5-Requirements.md) | Tiefling fulfills **slot replace / RespecAllowed** shape. |
| Phase 4 (equip / anatomy) | Horns + helmet exclusion; optional implant body contributions. |
