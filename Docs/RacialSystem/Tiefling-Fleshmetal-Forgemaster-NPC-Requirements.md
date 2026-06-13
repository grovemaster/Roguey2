# Tiefling — Fleshmetal Forgemaster NPC (implant gate)

**Purpose:** Specify a **town NPC** — the **Tiefling Fleshmetal Forgemaster** — who lets a **Tiefling party member** spend **gold**, **items**, and/or **story / quest gates** to **install**, **replace**, or **remove** **cyborg implants** in body slots — fulfilling the “special NPC” progression gate deferred from [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) §7.5 (F7.12–F7.13).

**Status:** Implemented (v0) — catalog dialog, install/replace/remove transactions, town NPC pack, dev Tiefling swap.

**Depends on:** [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) (`ImplantSlot`, `CyborgImplantDefinition`, `TieflingImplantsRuntime`, `RacialProgressionPayloadApplicator`, replace teardown), [NPC dialog](../World/NPC-Dialog-Requirements.md) (Enter adjacency + facing, `NpcDialogBoxUI`), `NpcController`, `PartyManager`, `CharacterStats.race`, `PartyCurrencyLedger`, `InventoryManager`, `GameStoryFlagService`, [Quest system](../World/Quest-Requirements.md), [Shop NPCs](../World/Shop-NPC-Requirements.md) (party gold + inventory mutation), [Safe zone](../World/Safe-Zone-Requirements.md), [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (read-only implant reference body — future).

**Related:** [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) (same town NPC + dynamic offer + greyed choices pattern). [Elf — Fairy Merchant / Meditation Shrine](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) (town split: acquire vs deepen).

**Explicitly out of scope (v0):** Upgrading **non-Tiefling** races; changing **multiple party Tieflings in one dialog** (one talk = one speaker); cosmetic surgery VFX / body mesh swaps; **batch** install of several implants in one transaction; implant **active** hotbar execution (follow existing hotbar policy); save/load beyond existing party persistence; final narrative identity of the Forgemaster (placeholder v0); **multiple specialized Forgemasters** (documented as **future** — §18); consuming story flags on purchase (v0: flags are gates only, same as Shaman).

---

## Locked decisions (v0)

| # | Decision |
|---|----------|
| **L1** | NPC display name: **Tiefling Fleshmetal Forgemaster** (placeholder; art/narrative may change). |
| **L2** | **Race gate** — non-Tiefling **speaker** gets a single rejection line; no implant UI. |
| **L3** | **Speaker** = active party leader at talk time; only **their** `TieflingImplantsRuntime` is mutated. |
| **L4** | **Offer list** = implants in this Forgemaster’s catalog (v0: one master, full catalog). |
| **L5** | Each offer shows **title**, **description**, **body slot**, **install/replace cost** (gold + items + quest/flag gates). |
| **L6** | **Install** into empty slot at **full buy cost**; **replace** occupied slot at **full buy cost** of the new implant (old implant fully removed first — no separate remove fee on replace). |
| **L7** | **Remove** (clear slot without replacement): **gold = floor(installGold / 2)**; item/flag remove costs **authored separately** via extensible struct (v0 may leave item list empty). |
| **L8** | Offers that fail **unlock** or **affordability** are **greyed out** and **not confirmable** (same UX as Shaman §10). |
| **L9** | Replace/install must call existing runtime APIs so **all** stats, resistances, passives, actives, and body contributions from the old implant are torn down before the new payload applies. |
| **L10** | **Cancel** always enabled; **Esc** closes without charging or changing implants. |
| **L11** | **No multi-slot implants** — each `CyborgImplantDefinition` targets **exactly one** body slot (`allowedSlots` length **1**). Multi-slot grafts are **out of scope** until explicitly requested later. |
| **L12** | **One graft per slot** — each `ImplantSlot` holds **zero or one** implant; never stacked or multiple in the same slot. |
| **L13** | **Single dialog page** — install/replace offers and remove offers appear on **one** choice screen (§6.3). |
| **L14** | **Gates only** — story flags / quest requirements **unlock** offers; they are **not consumed** on install, replace, or remove (v0). |
| **L15** | **One slot per implant type** — each implant asset maps to **one** slot only; the same `implantId` cannot be installed in **two** slots on one Tiefling (at most **one instance** per actor). |
| **L16** | **Forgemaster success rate 100%** — once unlock, affordability, and runtime pre-checks pass, **install and replace always succeed**; no random failure, complication, or “surgery failed” outcome in v0. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Placeholder Forgemaster NPC** in town — Tiefling appearance, unused world sprite + portrait. |
| **G2** | **Race gate** — non-Tiefling speaker rejected; no catalog. |
| **G3** | **Dynamic catalog** — dialog lists **every implant this NPC offers** with description, **body slot**, and **cost**. |
| **G4** | **Per-implant costs** — each implant defines install cost (gold, items, flags/quests). |
| **G5** | **Remove path** — occupied slots can be **cleared** for **half gold** (extensible for items later). |
| **G6** | **Choice UX** — one choice per install/replace offer + remove choices for occupied slots + **Cancel**; locked/unaffordable **greyed out**. |
| **G7** | **Replace correctness** — install into empty slot; replace in occupied slot with **full teardown** of previous implant properties. |
| **G8** | **Data-driven** — designers author costs on implant assets + Forgemaster catalog; dialog copy generated from data. |
| **G9** | **Extensible Forgemasters** — data model supports **multiple NPCs** with **subset catalogs** and **slot specialization** later without rewriting transactions. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Forgemaster** | Town `NpcController` that runs the implant install/replace/remove dialog flow. |
| **Speaker** | Active party leader at talk time (`PartyManager.GetActiveMember()`). |
| **Body slot** | One of seven `ImplantSlot` values (LeftArm, RightArm, Torso, Heart, Head, LeftLeg, RightLeg). |
| **Catalog entry** | Pairing of `CyborgImplantDefinition` + optional Forgemaster-specific overrides (future). |
| **Install cost** | Price to **place** an implant in its target slot (empty or replace). |
| **Remove cost** | Price to **clear** an occupied slot without installing a replacement. |
| **Unlock eligibility** | Story flags, quest state, level gates — player **sees** the offer but it is **greyed** until satisfied. |
| **Affordability** | Party has enough **gold** and **carried items** after unlock passes. |
| **Replace transaction** | Pay install cost → `TryReplaceImplant` (or install if slot empty) → refresh passives / hotbar assignables. |
| **Remove transaction** | Pay remove cost → `TryRemoveImplant` → refresh passives / hotbar assignables. |

---

## 3. Relationship to Tiefling cyborg implants

| Implants doc rule | Forgemaster behavior |
|-------------------|----------------------|
| **RespecAllowed** | Player may **replace** or **remove** implants via NPC (not permanent). |
| **One implant per slot** | Each slot holds **zero or one** graft; offer targets that implant’s **single** slot (§8.3). |
| **Replace teardown** | NPC **must** use `TryReplaceImplant` / `TryInstallImplant` — never stack modifiers. |
| **Pattern B runtime** | Mutates **`TieflingImplantsRuntime`** on speaker; `RacialLoadoutApplier` (Fire resist, horns) unchanged. |
| **Preset v0** | Prefabs may still ship preset implants; Forgemaster is the **player-facing** swap vector in town. |
| **Active abilities** | Available while implant **installed**; hotbar refresh after install/replace/remove (when hotbar wired). |

**Cross-reference:** [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) §7.5 F7.12 — *“Later: NPC offers install/replace from an authored catalog.”* **This document is that NPC.**

---

## 4. NPC identity (placeholder — will change later)

| Field | v0 value | Notes |
|-------|----------|-------|
| **Display name** | **Tiefling Fleshmetal Forgemaster** | Dialog name plate. |
| **Stable id** | `tiefling_fleshmetal_forgemaster` | `NpcDialogProfile.npcId`, stamp marker, logs. |
| **Race (folk)** | Tiefling | World sprite reads as Tiefling. |
| **Prefab** | **`TieflingNpc`** variant | Variant of `TieflingPlayer.prefab` with player-only components stripped + `NpcController`. |
| **World sprite** | **New unused asset** | Must not reuse existing town NPC or party field sprites. |
| **Portrait** | **New unused asset** | Must not reuse `Portrait_Race_Tiefling` (party default) or existing NPC portraits. Suggested: `Assets/Art/NPC/Sprites/NPC_FleshmetalForgemaster.png`, `Assets/Art/Portraits/NPC/Portrait_FleshmetalForgemaster.png`. |
| **Narrative role** | Placeholder smith | **Will be replaced** in a future content pass; mechanics survive art/narrative swaps. |

---

## 5. Interaction model (locked)

| Rule | Detail |
|------|--------|
| **Open talk** | **`Enter`** while orthogonally adjacent + facing — [NPC dialog §3](../World/NPC-Dialog-Requirements.md). |
| **Speaker** | Active party leader; only their implants change this session. |
| **Turn cost** | **None** for talk, browse, install, replace, or remove. |
| **Blocks gameplay** | `NpcDialogBoxUI.BlocksGameplay` (same stack as other dialog). |
| **Cancel** | **Cancel** choice or **Esc** closes without mutation. |
| **Safe zone** | Town safe zones only for v0; no combat field surgery. |

```
Enter (adjacent + facing Forgemaster)
  → Resolve speaker race + TieflingImplantsRuntime
  → Branch: non-Tiefling | catalog + current loadout summary
  → Choices: install/replace per catalog entry | remove per occupied slot | Cancel
  → On confirm (eligible + affordable): pay → runtime install/replace/remove → success → close
```

---

## 6. Dialog flows

### 6.1 — Non-Tiefling speaker

| Condition | Line (exact v0 copy) |
|-----------|----------------------|
| `speaker.stats.race != Race.Tiefling` | **“This forge works fleshmetal for Tieflings only.”** |

Single line → close. No choices.

### 6.2 — Tiefling speaker — no runtime

| Condition | Line |
|-----------|------|
| Tiefling but missing `TieflingImplantsRuntime` / wrong subsystem | **“Your body cannot accept fleshmetal grafts.”** |

### 6.3 — Tiefling speaker — catalog offer (dynamic)

**Prompt body** (template):

> The Forgemaster opens the graft catalog. **Your implants:**
>
> {for each ImplantSlot with installed implant}
> **{slot display}:** {installed displayName}
> {end for}
> {for empty slots — optional compact line}
> **{slot display}:** — empty —
> {end for}
>
> **Available grafts:**
>
> {for each catalog entry}
> **{displayName}** · **{slot display}** — {description}
> **Install cost:** {formattedInstallCost}
> {if slot occupied} *(replaces {current implant name})*
> {end for}
>
> {for each occupied slot}
> **Remove {slot display}** ({current implant name}) — **Remove cost:** {formattedRemoveCost}
> {end for}

**Choice list** (runtime):

| Choice label format | Behavior |
|---------------------|----------|
| **`{displayName} ({slot}), {shortInstallCost}`** | Install or replace when **eligible + affordable**. |
| **`Remove {slot display}, {shortRemoveCost}`** | Clear slot when **eligible + affordable**. |
| **`Cancel`** | Always enabled; no mutation. |

- **Order:** catalog entries in **Forgemaster catalog order** (stable); then **remove** choices in **slot enum order**; **Cancel** last.
- **Eligible + affordable:** normal color; confirm runs transaction (§11–§12).
- **Locked or unaffordable:** **greyed out**, `interactable = false`, keyboard cannot focus (Shaman §10).

**Success lines (v0 — pick one style in implementation):**

| Action | Line |
|--------|------|
| Install | **“The graft is set.”** |
| Replace | **“The old graft is cut free. The new one holds.”** |
| Remove | **“The graft is removed.”** |

**Failure line** (cost changed between open and confirm):

> **“You no longer have what the forge requires.”**

### 6.4 — Empty catalog (edge case)

| Condition | Line |
|-----------|------|
| Forgemaster catalog has zero entries **and** speaker has no removable implants | **“Nothing here for you today.”** |

---

## 7. Eligibility & target actor (locked)

| Rule | Detail |
|------|--------|
| **R7.1** | Only **speaker** checked for `Race.Tiefling`. |
| **R7.2** | Mutations apply to **`TieflingImplantsRuntime` on the speaker**, not other party Tieflings. |
| **R7.3** | No “pick which Tiefling” sub-menu in v0. |
| **R7.4** | Speaker must have `RacialSubsystemKind.TieflingImplants`. |
| **R7.5** | Implant must appear in **this NPC’s catalog** (v0: sole Forgemaster lists all v0 implants). |
| **R7.6** | Implant’s **`allowedSlots`** must contain **exactly one** slot (L11); that slot is the offer target. |
| **R7.7** | **Already installed** — if speaker already has **this** `implantId` in **any** slot, offer is **greyed** with **“Already installed”** (no-op purchase). |
| **R7.8** | **Duplicate graft** — same `implantId` on one actor **at most once**; two copies in different slots is **invalid** (L15). |

---

## 8. Data model

### 8.1 — Install cost on `CyborgImplantDefinition`

Extend each implant (mirrors Shaman `SpiritImprintUnlockCost`):

```csharp
[Serializable]
public struct CyborgImplantInstallCost
{
    [Min(0)] public int gold;
    public CyborgImplantItemCost[] items;
    public CyborgImplantFlagCost[] storyFlags;
    // Future: CyborgImplantQuestCost[] questRequirements;
}

[Serializable]
public struct CyborgImplantItemCost
{
    public ItemData item;
    [Min(1)] public int quantity;
}

[Serializable]
public struct CyborgImplantFlagCost
{
    public string flagId;
    public bool expectedValue; // default true
}
```

| Field on `CyborgImplantDefinition` | Purpose |
|-----------------------------------|---------|
| **`installCost`** | Price to install or replace with this implant. |
| **`removeCost`** | Optional remove-price override (§8.2). |
| *(existing)* **`displayName`**, **`description`** | Dialog copy. |
| *(existing)* **`allowedSlots`** | **Exactly one** `ImplantSlot` (L11). Assets with zero or &gt;1 entries are **invalid** at import / offer time. |

### 8.2 — Remove cost (extensible)

```csharp
[Serializable]
public struct CyborgImplantRemoveCost
{
    [Min(0)] public int gold;
    public CyborgImplantItemCost[] items;      // v0: usually empty
    public CyborgImplantFlagCost[] storyFlags; // v0: usually empty
}
```

| Field | Purpose |
|-------|---------|
| **`removeCost`** | Optional override on implant asset. |
| **Default when unset** | **`gold = installCost.gold / 2`** (integer floor); items/flags empty. |

Later design may require **Flux Salts ×1** (or similar) on remove — author in **`removeCost.items`** without code changes.

### 8.3 — Target slot (locked)

Each implant **I** has **exactly one** target slot **`S`** = the sole entry in **`I.allowedSlots`**.

| Rule | Detail |
|------|--------|
| **Authoring** | `allowedSlots.Count` must be **1**; otherwise asset fails validation / is omitted from catalog. |
| **Display** | Dialog shows **`S`** display name on every offer row (e.g. **Heart**, **Left Arm**). |
| **Install** | If **`S`** is empty → install **I** into **`S`**. |
| **Replace** | If **`S`** is occupied → replace existing graft in **`S`** with **I**. |
| **Future specialist NPC** | Forgemaster may **filter** which slots’ grafts appear; each offer still maps to **one** slot (§18). |

**Out of scope:** Multi-slot implants (one asset valid in several body regions) until explicitly requested in a future doc revision.

### 8.4 — Forgemaster catalog asset (v0 + future)

```csharp
[CreateAssetMenu(...)]
public class TieflingForgemasterDefinition : ScriptableObject
{
    public string forgemasterId;
    public List<CyborgImplantDefinition> offeredImplants;
    // Future:
    // public List<ImplantSlot> specializedSlotsOnly; // empty = all slots
    // public List<CyborgImplantDefinition> signatureImplants; // bonus UI badge
}
```

| Field | v0 | Future |
|-------|-----|--------|
| **`offeredImplants`** | All purchasable implants in town | Subset per NPC |
| **`specializedSlotsOnly`** | Empty (no restriction) | e.g. Heart-only smith |
| **`signatureImplants`** | Unused | Highlight specialist crafts |

**v0:** One asset **`DefaultFleshmetalForgemaster`** referencing all shipping implants.

### 8.5 — Affordability & unlock evaluation

Evaluate against **party-wide** pools (same as Shaman §8.3):

| Cost type | Pass condition |
|-----------|----------------|
| **Gold** | `PartyCurrencyLedger` total ≥ required gold |
| **Item** | Sum across **carried** inventories (all party members; equipped **excluded**) |
| **Story flag** | `GameStoryFlagService.IsSet(flagId) == expectedValue` — **gate only**; **not cleared** on pay (L14). |
| **Quest** (future) | Quest state ≥ required milestone — **gate only** unless a future doc adds consumption. |

**Install/replace:** all **installCost** lines must pass (**AND**).  
**Remove:** all **removeCost** lines (or defaults) must pass.

**Grey-out reasons** (show in choice label or tooltip when disabled):

| Reason | Example suffix |
|--------|----------------|
| Missing unlock | `— locked` |
| Missing gold/items | `— insufficient funds` |
| Already installed | `— already installed` |
| Duplicate implantId on actor | Offer hidden or greyed (should not appear if R7.7 enforced) |
| Wrong specialist (future) | `— not offered here` |

---

## 9. Install & replace transaction (locked)

On confirm of an **enabled** install/replace choice for implant **I** at resolved slot **S**:

1. **Re-validate** unlock + affordability.
2. **Re-validate** catalog: **I** offered by this Forgemaster; **S** allowed for **I**.
3. **Re-validate** not **already installed** (same id in **S**).
4. **Pay `installCost`** atomically (gold → party ledger; items → carried inventories, speaker first).
5. **Apply runtime** (always succeeds when pre-checks pass — L16):
   - If slot **S** empty → `TryInstallImplant(S, I, out reason)` — **must return true**; on false, **rollback payment** and show failure line (implementation bug).
   - If slot **S** occupied → `TryReplaceImplant(S, I, out reason)` — same guarantee.
6. **Post-apply:** refresh implant passives; **`AbilityHotbarUI.RefreshAll()`** when hotbar lists implant actives.
7. Success line → close dialog.
8. Log: `[TieflingImplant] {speaker} {install|replace} {I.implantId} at {S} via Forgemaster; paid {summary}.`

| Rule | Detail |
|------|--------|
| **R9.1** | **Atomic** — payment and runtime mutation succeed or fail together; **rollback** on unexpected runtime failure. |
| **R9.2** | **Full replace** — old implant stats, resistances, passives (`OnRemove`), actives, body contributions **gone** before new implant applies. |
| **R9.3** | **No partial item pay.** |
| **R9.4** | **No refund** of previous implant’s install cost on replace (player paid remove cost separately if they wanted an empty slot first). |
| **R9.5** | Persist via existing party / run save hooks for `TieflingImplantsRuntime` slot map. |
| **R9.6** | **No random failure** — Forgemaster install/replace does **not** roll success chance; **100%** success when the choice was enabled at confirm time (L16). |

---

## 10. Remove transaction (locked)

On confirm of an **enabled** remove choice for slot **S**:

1. **Re-validate** slot **S** is occupied.
2. **Re-validate** remove unlock + affordability (default or authored **`removeCost`**).
3. **Pay remove cost** atomically.
4. **`TryRemoveImplant(S)`** — full teardown per implants doc F7.6; **must succeed** when pre-checks pass (L16).
5. Refresh passives + hotbar → success line → close.

| Rule | Detail |
|------|--------|
| **R10.1** | Remove **does not** require a replacement implant in the same transaction. |
| **R10.2** | Default remove gold = **`floor(installGold / 2)`** of the **currently installed** implant’s **`installCost.gold`**. |
| **R10.3** | Removing clears **all** properties of that implant (passives, actives, stats, body contributions). |

---

## 11. UI — disabled dialog choices

Reuse [Shaman §10](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) (`DialogChoiceOptionData.enabled`):

| Requirement | Detail |
|-------------|--------|
| **U11.1** | Locked/unaffordable/already-installed choices **greyed**, not keyboard-selectable. |
| **U11.2** | Disabled label **still shows** cost so player knows what is missing. |
| **U11.3** | **Cancel** always enabled. |

---

## 12. Town placement (v0)

| Field | Suggested value |
|-------|-----------------|
| **Marker id** | `tiefling_fleshmetal_forgemaster` |
| **Cell** | Plaza stamp — **≥2 cells** from other NPCs (e.g. `(8, 6, 0)` — finalize in authoring). |
| **Setup** | Extend `Stamp_TownPlaza_20x20` + town NPC setup phase (same pipeline as Shaman). |

Forgemaster is **not** a shop NPC — custom dialog session handler.

---

## 13. Services & code layout (recommended)

| Piece | Responsibility |
|-------|----------------|
| **`TieflingImplantForgemasterService`** | Query catalog, resolve slot, evaluate unlock + afford, format costs, execute install/replace/remove. |
| **`TieflingForgemasterDialogSession`** | Builds dynamic `DialogChoiceStep` from service. |
| **`TieflingForgemasterNpcController`** | `INpcTalkTarget`; binds Forgemaster definition asset. |
| **Data** | `installCost` / `removeCost` on implants; `TieflingForgemasterDefinition` catalog. |
| **Tests** | Afford, pay, install, replace teardown, remove half-gold, grey-out, wrong race, already installed. |

**Do not** hard-code implant lists only in C# — catalog is **data-driven**.

---

## 14. Racial abilities menu (read-only reference)

Parallel to [Barbarian Spirit Imprint menu](../UI/Racial-Abilities-Menu-Requirements.md) and [Elf contract menu](Elf-Racial-Abilities-Menu-Requirements.md):

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Racial menu — Tiefling body** | View **installed implants per slot**, passives/actives summary | Install, replace, remove, or pay costs |
| **Forgemaster** | Install / replace / remove | Browse full reference sheet (optional compact summary only) |

**Banner (Tiefling body):** *“View only — visit the Tiefling Fleshmetal Forgemaster in town to install or change grafts.”*

**Out of v0 Forgemaster doc** — Tiefling menu body specified in **[Tiefling — racial abilities menu](Tiefling-Racial-Abilities-Menu-Requirements.md)** (requirements + mock; implementation pending); cross-link here.

---

## 15. Hotbar & equipment integration

| Event | Required behavior |
|-------|-------------------|
| **Install / replace** | Implant actives appear in racial hotbar assignables when policy allows; stale bindings cleared on replace. |
| **Remove** | Actives from removed implant **removed** from assignables; main-row slots referencing them go stale → empty per hotbar rules. |
| **Passives** | `RefreshPassives()` on runtime after any transaction. |
| **Equipment** | Horns / helmet rules unchanged unless implant registers body contribution. |

---

## 16. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | Human leader talks → rejection line → no choices. |
| **AC2** | Tiefling with empty **LeftArm** → affordable graft choice → pay → implant installed → stats/passives active. |
| **AC3** | Tiefling with **LeftArm** A → choose graft B for same slot → pay → **A’s** modifiers gone → **B’s** present. |
| **AC4** | Remove **LeftArm** implant → pay **half gold** → slot empty → modifiers gone. |
| **AC5** | Missing gold / item / flag → choice **greyed** and not confirmable. |
| **AC6** | Already installed graft → **greyed** “already installed”. |
| **AC7** | Replace on **LeftArm** does not alter **Torso** implant. |
| **AC8** | Transaction survives town → dungeon → town (party persistence). |
| **AC9** | Forgemaster uses **new** sprite + portrait not shared with existing NPCs. |
| **AC10** | Cancel / Esc → no gold/items spent → no runtime change. |
| **AC11** | Enabled install/replace choice → **always** succeeds (no failure branch except payment rollback on runtime bug). |
| **AC12** | Implant asset with `allowedSlots.Count != 1` → rejected at validation / excluded from catalog. |
| **AC13** | Speaker with graft **G** installed → second offer for same `implantId` is **greyed** (cannot duplicate across slots). |

---

## 17. Implementation checklist

- [x] Add `CyborgImplantInstallCost` / `CyborgImplantRemoveCost` to `CyborgImplantDefinition`
- [x] Author costs on sample implants (`IronSleeveArm`, `ThoracicPlate`)
- [x] Create `TieflingForgemasterDefinition` + default catalog asset (`Resources/Racial/Tiefling/DefaultFleshmetalForgemaster`)
- [x] `TieflingImplantForgemasterService` + `TieflingImplantForgemasterLogic`
- [x] `TieflingForgemasterDialogSession` + `TieflingForgemasterNpcController`
- [ ] New sprite, portrait, `TieflingNpc` prefab, town NPC prefab — run **JRogue → Town → Create Fleshmetal Forgemaster Pack**
- [x] Town stamp marker `(8, 6)` + `TownNpcSetupPhase` spawn entry
- [x] Unit tests (`TieflingImplantForgemasterLogicTests`)
- [x] Dev Tiefling swap — **Ctrl+Shift+T** or **JRogue → Dev → Convert Active Party Member To Tiefling**
- [x] Tiefling racial menu read-only body — see [Tiefling — racial abilities menu](Tiefling-Racial-Abilities-Menu-Requirements.md)

---

## 18. Future — multiple Forgemasters (out of v0)

| Feature | Behavior |
|---------|----------|
| **Several NPCs** | Each references its own `TieflingForgemasterDefinition` (`offeredImplants` subset). |
| **Slot specialist** | `specializedSlotsOnly = { Heart }` → only Heart grafts offered; other slots hidden or greyed at that NPC. |
| **Signature crafts** | Unique implants sold only by one smith (catalog exclusivity). |
| **Remove item cost** | Author `removeCost.items` per implant or globally. |
| **Quest-gated catalog rows** | `CyborgImplantQuestCost` on install/remove structs. |
| **Town phase / calendar** | Forgemaster available certain days ([Town time](../World/Town-Time-And-Calendar-Requirements.md)). |

Data model in §8.4 is designed so v0 single-NPC implementation **does not block** specialization.

---

## 19. Additional requirements (confirmed)

The following supporting requirements are **in scope** for v0 (author confirmed):

| Topic | Why it matters |
|-------|----------------|
| **Speaker / party leader rule** | Matches Shaman + NPC dialog; avoids multi-Tiefling ambiguity in v0. |
| **Unlock vs affordability** | “Cannot purchase **yet**” (flags/quests) vs “cannot **afford**” — both greyed, different reasons. |
| **Already installed** | Prevents paying full price for a no-op. |
| **Atomic pay + runtime** | Prevents lost gold with failed install. |
| **Replace vs remove economics** | Replace = full install cost only; remove = half gold (extensible items). |
| **Current loadout in dialog body** | Player sees what they have before swapping. |
| **Slot display names** | Human-readable body region on every offer. |
| **Post-transaction hotbar refresh** | Actives must match installed grafts. |
| **Racial menu read-only sheet** | Parity with Barbarian/Elf; Forgemaster owns mutations. |
| **Persistence** | Slot map survives run save/load. |
| **Safe zone / no turn cost** | Town service, not combat action. |
| **Placeholder NPC identity** | Art/narrative can change without rewriting transactions. |
| **Logging** | Debug and player feedback on success/failure. |
| **Unit tests + acceptance criteria** | Replace teardown is easy to regress. |

---

## 20. Resolved design decisions

| # | Question | Locked answer |
|---|----------|---------------|
| **Q1** | Multi-slot implants? | **No** — each asset targets **one** slot only (L11). Revisit only if design explicitly requests later. |
| **Q2** | Implants per slot? | **Zero or one** per `ImplantSlot`; never multiple in the same slot (L12). |
| **Q3** | Dialog layout? | **Single dialog** — install/replace choices, then remove choices, then Cancel (L13). |
| **Q4** | Consume flags/items on purchase? | **Gates only** in v0 — flags and quest gates **not consumed** on pay (L14). |
| **Q5** | Same implant in two slots? | **No** — one `implantId` per actor at most; each implant fits **one** slot only (L15). |
| **Q6** | Install/replace success rate? | **100%** through Forgemaster when choice was enabled; no random failure (L16). |
| **Q7** | Empty slot list in dialog body? | Show **occupied** rows; empty slots may collapse to a compact summary if noisy (implementation detail). |
| **Q8** | Exact dialog copy | Samples in §6 — tweak in content pass. |

---

## 21. Document history

| Date | Change |
|------|--------|
| 2026-06-13 | Initial draft — Fleshmetal Forgemaster NPC, catalog costs, remove half-gold, replace teardown, grey-out offers, multi-NPC extensibility. |
| 2026-06-13 | Locked Q1–Q6 — single-slot implants, one graft per slot, single dialog, gates-only costs, no duplicate implantId, 100% Forgemaster success. |
