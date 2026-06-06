# Light-Emitting Items — Handheld Torch & Helmet of Light

**Purpose:** Ship **player-carried light** for dark dungeon floors and nighttime areas: a **Handheld Torch** accessory (always-on while equipped) and a **Helmet of Light** head armor (timed active ability with cooldown). Establishes reusable patterns for **`LightSourceItemData`**, **virtual party emitters**, and an **extensible ability cooldown** service.

**Status:** Implemented (manual QA §9 recommended in dark dungeon / town night).

**Depends on:** [Lighting — Requirements](Lighting-Requirements.md) (`LightingService`, `LightEmitterDefinition`, `LightLevel`, per-cell illumination gating), [Improved Illumination](Improved-Illumination-Requirements.md) (town torches + `receivedLight` gate), [Lighting QA and Torch v0 §8](Lighting-QA-And-Torch-v0-Requirements.md) (carried torch spec draft), [Dungeon time](Dungeon-Time-Requirements.md) (player-phase boundary), `TurnManager`, `EquipmentManager`, `AbilityAction`, `ItemData` / `ItemInstance`, [Floor item piles](Dynamic-Dungeon-Floor-Generation-Requirements.md) §7 (`floorItemPopulation`), [Shop NPCs](Shop-NPC-Requirements.md) (Greta sell-only shop).

**Related:** [Evocable items](../Inventory/Evocable-Items-Requirements.md) (charge/recharge tick at player-phase boundary — **different** from ability cooldown). [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (equip slots, active ability invoke).

**Explicitly out of scope (this milestone):** Toggle extinguished/lit handheld torch (v1 is **always lit** when equipped); wall-torch ignite gameplay changes; URP 2D Light components; save/load of cooldown state across game sessions; enemy alert from party light; colored/mood lighting; multiple torch tiers; animated torch sprite in inventory (static icon v0).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | Import **distinct item sprites** for Handheld Torch and Helmet of Light (not wall-torch art). |
| **G2** | **Handheld Torch** — `ItemCategory.Accessory`; while **equipped**, bearer emits light from item properties; useful in **dark ambient** dungeon floors / night. |
| **G3** | Place **≥1 Handheld Torch** on the ground on **dungeon floor 1** (`dungeon_floor_01`). |
| **G4** | **Helmet of Light** — `ItemCategory.Armor`, `EquipmentSlot.Head`; **active ability** ( **0 soul power** ) emits light like the torch for **5 player turns**, then **3-turn cooldown** before reactivation. |
| **G5** | **Cooldown extensibility** — generic runtime service keyed by ability + owner so future abilities reuse the same tick / gate / UI hooks. |
| **G6** | **Helmet unequip semantics** — light removed immediately; **5-turn light** and **3-turn cooldown counters keep ticking**; re-equip restores light if **≥1 light turn** remains. |
| **G7** | **Greta shop** sells **Handheld Torch**, **Helmet of Light**, and **Giant's Sword** (`Giants_Blade`). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Light-emitting item** | Equipment whose bearer (or timed ability) registers a **virtual emitter** on the lighting grid. |
| **Virtual emitter** | Non-map `LightCellData` entry tracked by `LightingService` at the **bearer's `GridPosition`**, updated on move / equip / ability state. |
| **Handheld Torch** | Accessory; light **on** whenever equipped (v1). |
| **Helmet of Light** | Head armor; light **only while ability active** and helmet **equipped**. |
| **Light duration** | Helmet: remaining **player turns** the timed light stays active (starts at **5** on activate). |
| **Ability cooldown** | Helmet: remaining **player turns** before the ability can activate again (starts at **3** after light expires). |
| **Player turn / phase** | One completed player phase cycle — same boundary as `EvocableRechargeService.TickPartyAfterPlayerPhase()` (all living party members acted → enemy phase begins). |

---

## 3. Current state vs gap

| Area | Already exists | Gap (this doc) |
|------|----------------|----------------|
| **Wall / town torches** | Static map emitters, improved illumination gate | No **party-carried** emitters |
| **`PartyCarriedLightSource`** | Stub returns `false` | Must query equipped `LightSourceItemData` + helmet active state |
| **`Lighting-QA-And-Torch-v0` §8** | Carried torch spec draft | Not implemented; this doc **supersedes §8** for content + cooldown helmet |
| **`AbilityAction.cooldownTurns`** | Serialized field | **Not enforced** at runtime |
| **`EvocableRechargeService`** | Charge recharge ticks | **Not** ability cooldown — do not overload |
| **Greta shop** | 2 × `Giants_Blade` only | Add Handheld Torch + Helmet of Light stock |
| **Floor 1 items** | Healing potion piles only | Add Handheld Torch pile |

---

## 4. Art (imported)

### 4.1 — Handheld Torch

| Rule | Detail |
|------|--------|
| **Sprite** | `Assets/Art/Items/Sprites/Accessory_HandheldTorch.png` |
| **Source** | AntumDeluge animated torch — frame 0 of 32×32 sheet |
| **Third-party** | `Assets/Art/Items/ThirdParty/AntumDelugeTorch/` |
| **License** | **CC-BY 3.0** — attribute Jordan Irwin (AntumDeluge) in `Assets/Art/Items/CREDITS.md` |
| **Import** | 32 PPU, Point filter, no compression |

### 4.2 — Helmet of Light

| Rule | Detail |
|------|--------|
| **Sprite** | `Assets/Art/Items/Sprites/Armor_HelmetOfLight.png` |
| **Source** | Idylwild's Armory — `close_helmet.png` |
| **Third-party** | `Assets/Art/Items/ThirdParty/IdylwildArmory/` |
| **License** | **CC0** |
| **Import** | 32 PPU, Point filter, no compression |

### 4.3 — Credits

Record both assets in `Assets/Art/Items/CREDITS.md` (done).

---

## 5. Data model — light-emitting items

### 5.1 — `LightSourceItemData`

New ScriptableObject type (or equivalent fields on a shared base):

| Field | Handheld Torch | Helmet of Light |
|-------|----------------|-----------------|
| **`LightEmitterDefinition`** | Ref `Assets/Resources/Lighting/Torch.asset` (emission **6**, radius **8**, falloff **1**/tile) | Same (or authored `LightEmitterDefinition_HelmetOfLight` if tuning needed) |
| **`emitsWhenEquipped`** | `true` | `false` (ability-driven) |
| **`startsLit`** | `true` | N/A |
| **`canIgniteWallTorches`** | `false` (v1) | `false` |

**Menu path:** `JRogue/Item/Light Source`

### 5.2 — Handheld Torch item asset (locked)

| Field | Value |
|-------|-------|
| **Asset path** | `Assets/Resources/Item/Accessory/Accessory_HandheldTorch.asset` |
| **`itemName`** | Handheld Torch |
| **`category`** | `ItemCategory.Accessory` |
| **`slotType`** | `EquipmentSlot.Accessory_MainHand` |
| **`weight`** | 1.0 |
| **`goldValue` / `buyValue` / `sellValue`** | 8 / 12 / 4 (tune in editor) |
| **`requiresAppraisal`** | `true` |
| **`autoPickupOnStep`** | `false` (manual floor pickup) |
| **`icon`** | `Accessory_HandheldTorch.png` |
| **`activeAbilities`** | **[]** (passive light while equipped) |

### 5.3 — Helmet of Light item asset (locked)

| Field | Value |
|-------|-------|
| **Asset path** | `Assets/Resources/Item/Armor/Armor_HelmetOfLight.asset` |
| **`itemName`** | Helmet of Light |
| **`category`** | `ItemCategory.Armor` |
| **`slotType`** | `EquipmentSlot.Head` |
| **`weight`** | 2.5 |
| **`goldValue` / `buyValue` / `sellValue`** | 40 / 55 / 20 |
| **`requiresAppraisal`** | `true` |
| **`icon`** | `Armor_HelmetOfLight.png` |
| **Ability asset** | `Assets/Resources/Item/Ability/HelmetOfLight_Radiance.asset` |

### 5.4 — Helmet ability asset (locked)

| Field | Value |
|-------|-------|
| **`abilityName`** | Radiance |
| **`description`** | Emit light for 5 turns. |
| **`soulPowerCost`** | **0** |
| **`magicPowerCost` / `divinePowerCost`** | 0 |
| **`cooldownTurns`** | **3** (authoring default; runtime uses `AbilityCooldownService`) |
| **`requiresTarget`** | `false` |
| **`range`** | 0 |
| **Effect** | Start helmet **light duration = 5**; register virtual emitter if helmet equipped |

Implementation: `HelmetOfLightRadianceAbility : AbilityAction` (or generic `StartTimedLightAbility` if reused later).

---

## 6. Runtime — virtual emitters

### 6.1 — Registration rules

| Source | Emitter active when |
|--------|---------------------|
| **Handheld Torch** | Item equipped in any `Accessory_*` slot on a living party member **and** `emitsWhenEquipped && startsLit` |
| **Helmet of Light** | Helmet equipped in `Head` **and** `ItemInstance` light duration **> 0** |

| Event | Action |
|-------|--------|
| **Equip** (light-eligible) | Register / refresh virtual emitter at bearer cell |
| **Unequip** | **Remove** virtual emitter immediately (light contribution gone) |
| **Move** (party vision activity / grid step) | Move virtual emitter to new cell |
| **Death** | Remove bearer emitters |
| **Floor transition** | Rebuild from equipped state (`LightingService.ResetForActiveFloor` hook) |

### 6.2 — `PartyCarriedLightSource` (update)

Replace stub:

```csharp
public static bool AnyMemberHasLitAccessoryEmitter()
```

Return `true` when **any** party member has an active virtual emitter from a carried light item (handheld torch equipped, or helmet with duration > 0 while equipped). Used by wall-torch preconditions (`CarriedLitTorchPrecondition`).

### 6.3 — Party union

All active virtual emitters from all living members contribute to `receivedLight` (same aggregation as wall torches — sum capped at `LightLevel.Max`). Bearer cell remains R7.1 bright per parent lighting rules.

### 6.4 — Integration points

| System | Hook |
|--------|------|
| **`EquipmentManager`** | On equip/unequip → `PartyLightEmitterBridge.RefreshMember(actor)` |
| **`TurnManager`** | After move / party vision → refresh emitter positions |
| **`LightingService.OnPlayerPhaseBoundary`** | Tick helmet durations + cooldowns (§7) |
| **`LightingService.OnPartyVisionActivity`** | Reposition virtual emitters |

New component/service: **`PartyLightEmitterBridge`** (name locked unless refactor prefers static service).

---

## 7. Extensible ability cooldown

### 7.1 — Design (locked)

**Do not** reuse `EvocableItemData` charge fields for cooldown — evocables are **charges + recharge interval**; cooldowns are **post-use lockout**.

Introduce **`AbilityCooldownService`** (static, party-scoped):

| Concept | Rule |
|---------|------|
| **Key** | `(ownerInstanceId, abilityAssetId)` — supports multiple abilities per item later |
| **State** | `remainingCooldownTurns` (int ≥ 0), optional `activeEffectTurnsRemaining` for timed effects |
| **Tick** | Once per completed player phase — same call site as `EvocableRechargeService.TickPartyAfterPlayerPhase()` |
| **Gate** | `AbilityAction.CanExecute` consults service; helmet ability also checks helmet equipped + not on cooldown + not already active (or allow refresh policy: **no** — activating while active **ignored** v1) |
| **Authoring** | `AbilityAction.cooldownTurns` remains source of truth for **cooldown length** after timed effect ends |

### 7.2 — Helmet of Light state machine

States on **`ItemInstance`** (runtime fields — not serialized on `ItemData`):

| Field | Meaning |
|-------|---------|
| **`helmetLightTurnsRemaining`** | Countdown while light active; **0** = not emitting |
| **`helmetCooldownTurnsRemaining`** | Countdown after light expires; **0** = ability ready |

**Transitions:**

```
[Ready] --activate (equipped)--> [Active N=5]
[Active N>0] --player phase tick--> [Active N-1] ... --> [Active N=0] --> [Cooldown M=3]
[Cooldown M>0] --player phase tick--> [Cooldown M-1] ... --> [Cooldown M=0] --> [Ready]
```

| Rule | Detail |
|------|--------|
| **Activate** | Requires equipped + `helmetCooldownTurnsRemaining == 0` + `helmetLightTurnsRemaining == 0`; sets light **5**, registers emitter |
| **Tick (player phase)** | If `helmetLightTurnsRemaining > 0`: decrement; at **0** start cooldown **3** and remove emitter if still equipped |
| **Unequip while Active** | Remove emitter; **do not** reset `helmetLightTurnsRemaining` or `helmetCooldownTurnsRemaining` |
| **Re-equip while Active** | If `helmetLightTurnsRemaining > 0`: re-register emitter |
| **Tick while unequipped** | Counters **still decrement** each player phase (item in bag/subspace/equipped elsewhere in party) |

**Ownership:** Cooldown/light counters live on the **`ItemInstance`**, not the actor — transferring the helmet transfers state.

### 7.3 — Future abilities

Any `AbilityAction` with `cooldownTurns > 0` may register cooldown via the same service. Optional extension: `timedEffectTurns` field on ability for other duration-based actives without duplicating helmet-specific fields (refactor helmet to generic **timed effect + cooldown** when second item needs it).

### 7.4 — UI feedback (v1 minimum)

| Surface | Behavior |
|---------|--------|
| **Inventory inspect** | Show `Light: N turns` or `Cooldown: M turns` for Helmet of Light |
| **Ability invoke** | Gray out / fail `CanExecute` with log when on cooldown |
| **Handheld Torch** | Inspect line: `Emits light while equipped` |

---

## 8. Content placement

### 8.1 — Dungeon floor 1 ground spawn (locked)

Add to `Assets/Resources/Dungeon/Floor_dungeon_floor_01.asset` → `floorItemPopulation`:

| Field | Value |
|-------|-------|
| **`itemData`** | `Accessory_HandheldTorch` |
| **`minCount` / `maxCount`** | **1 / 1** |
| **`minQuantity` / `maxQuantity`** | 1 / 1 |

**Acceptance:** Every generated `dungeon_floor_01` run has **exactly one** Handheld Torch pile (subject to valid placement — `FloorItemPopulationPhase` retry rules).

**Editor:** Extend `DungeonV0aPackCreator` (or dedicated pack menu) to wire this entry when creating floor data.

### 8.2 — Greta shop stock (locked)

Update `ShopNpc_Greta` / `ShopNpcPackCreator.CreateGretaShop`:

| Item | Quantity |
|------|----------|
| `Giants_Blade` | 2 *(unchanged)* |
| `Accessory_HandheldTorch` | **2** |
| `Armor_HelmetOfLight` | **1** |

**Acceptance:** Greta sell menu lists Handheld Torch and Helmet of Light at authored `buyValue`.

Cross-link [Shop-NPC-Requirements.md](Shop-NPC-Requirements.md) § Giant's Sword merchant.

---

## 9. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Equip Handheld Torch in dark dungeon → cells within torch falloff become live-visible per illumination gate. |
| **AC2** | Unequip Handheld Torch → virtual emitter removed; darkness returns in previously lit cells. |
| **AC3** | Floor 1 generation places one Handheld Torch pile. |
| **AC4** | Activate Helmet of Light (0 SP) → 5 turns of light while equipped. |
| **AC5** | After 5 turns → light off; **3-turn cooldown** before reactivation. |
| **AC6** | Unequip helmet during active light → light gone; counters keep ticking; re-equip with turns left → light returns. |
| **AC7** | Greta sells Handheld Torch and Helmet of Light. |
| **AC8** | Two party members with lit sources → union of lit regions. |
| **AC9** | `AbilityCooldownService` unit tests: tick, gate, unequip persistence. |

---

## 10. Implementation checklist

### Art & data

- [x] Handheld Torch sprite + third-party folder + credits
- [x] Helmet of Light sprite + third-party folder + credits
- [x] `LightSourceItemData` ScriptableObject type
- [x] `Accessory_HandheldTorch.asset`
- [x] `Armor_HelmetOfLight.asset`
- [x] `HelmetOfLight_Radiance.asset` ability
- [x] Floor 1 `floorItemPopulation` entry
- [x] Greta shop stock entry (Handheld Torch + Helmet of Light)

### Runtime

- [x] `PartyLightEmitterBridge` + `LightingService` virtual emitter API
- [x] `PartyCarriedLightSource` implementation
- [x] `EquipmentManager` equip/unequip hooks
- [x] `AbilityCooldownService` + helmet timed state on `ItemInstance`
- [x] `HelmetOfLightRadianceAbility` (or equivalent)
- [x] Inventory inspect strings for duration/cooldown

### Tests & docs

- [x] Unit tests: cooldown tick, unequip/re-equip, emitter registration
- [ ] Cross-links from [Lighting-Requirements.md](Lighting-Requirements.md), [Lighting-QA-And-Torch-v0-Requirements.md](Lighting-QA-And-Torch-v0-Requirements.md) §8, [Shop-NPC-Requirements.md](Shop-NPC-Requirements.md)
- [ ] Manual QA: floor 1 pickup, Greta purchase, night/dark floor playtest

---

## 11. Debug logging

| Prefix | When |
|--------|------|
| `[Lighting:Carried]` | Virtual emitter register/remove/move |
| `[Lighting:Helmet]` | Activate, tick, cooldown start/end |
| `[Ability:Cooldown]` | Gate blocked / tick decrement |

---

## 12. Open questions (defaults chosen)

| # | Question | Default |
|---|----------|---------|
| 1 | Handheld torch accessory slot | **`Accessory_MainHand`** |
| 2 | Torch toggle (lit/extinguished) | **No** — always lit when equipped (v1) |
| 3 | Helmet activate while light active | **Ignore** (no stack/refresh) |
| 4 | Cooldown ticks while item in shop stash | N/A — item must be in party inventory |
| 5 | Emitter definition | Reuse `Torch.asset` |

---

## 13. References

- Emitter asset: `Assets/Resources/Lighting/Torch.asset`
- Carried torch draft: [Lighting QA §8](Lighting-QA-And-Torch-v0-Requirements.md)
- Floor 1 definition: `Assets/Resources/Dungeon/Floor_dungeon_floor_01.asset`
- Greta shop: `Assets/Resources/Shop/ShopNpc_Greta.asset`
- Art credits: `Assets/Art/Items/CREDITS.md`

---

## 14. Document history

| Date | Note |
|------|------|
| 2026-06-06 | Implemented runtime, content, and unit tests |
