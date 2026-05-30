# Lighting QA and Torch v0 — Requirements

SampleScene **validation harness** and **first shippable torch loop** for the lighting system: regional ambient (global floor vs isolated dark areas), **wall torch** ignite + falloff, **debug warmth overlay**, and a **carried torch** follow-up scoped to **accessory equipment**. This document **narrows** [Lighting — Requirements](Lighting-Requirements.md) for v0 QA and torch content; it does **not** replace the parent spec (Dark Vision, magical darkness, enemy light alert, full FoW snapshot polish remain there).

**Depends on:** [Lighting — Requirements](Lighting-Requirements.md), [Fog of War](Fog-Of-War-Requirements.md), [Interactable tiles](../Combat/Interactable-Tiles-Requirements.md) (`SetTileEmissionEffect`), `LightingService`, `VisibilityManager`, `LightEmitterDefinition`, `LightingCellMarker`, `LightingBootstrap`, `LightingScenarioController`, `EquipmentManager` / `ItemData` (`ItemCategory.Accessory`, `EquipmentSlot.Accessory_*`), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md).

**Related:** [Lighting — Future backlog](Lighting-Future-Backlog.md) (item **#40** torch flame / glow overlay for shipping art).

**Explicitly out of scope (this milestone):** Carried torch **implementation** (specified in §8 for the **next** milestone); magical darkness; save/load lighting state; URP 2D Light components as gameplay truth; production-quality torch VFX; enemy alert from party light (Phase 6 QA scenario only); changing party spawn layout beyond the test torch placement.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | Support **floor-wide default ambient** with **overrides** in sub-areas (e.g. cave region on an otherwise bright floor). |
| **G2** | Support **dark-by-default floors** where visibility inside LOS depends on **emitters** (wall torches, later carried torches). |
| **G3** | Provide **repeatable SampleScene QA** for **dim tiles** (`darkTileColor`) and **lit vs under-lit** boundaries without accidental global brightening. |
| **G4** | Ship **wall torch v0**: unlit → lit via interactable, visible falloff, debug overlay shows illuminated radius. |
| **G5** | Specify **carried torch v1** (accessory equip → virtual emitter) without implementing it in the same milestone. |
| **G6** | Place the **wall torch test** on the **wall cell closest** to **`Party_Tiefling_Mage`** in SampleScene. |

---

## 2. Ambient model (floors and sub-areas)

### R2.1 — Floor default (global)

| Concept | Rule |
|---------|------|
| **Default region** | Each floor (or scene) configures `LightingService.defaultFloorAmbientRegionId` and `defaultFloorAmbientLight` (scale **0–10**, see `LightLevel`). |
| **Floor receivers** | Walkable cells without explicit markers inherit **default region** ambient when computing `receivedLight` (after registry finalize). |
| **Bright floor** | e.g. surface / town: default ambient **10** (`FullDaylightAmbient`). |
| **Dark floor** | e.g. dungeon / cave level: default ambient **0** (`PitchDark`). |

### R2.2 — Sub-areas (exception to global)

| Concept | Rule |
|---------|------|
| **Ambient region id** | Integer **≠ default** (e.g. `99` = “local cave pocket”). |
| **Receivers in sub-area** | `LightingCellMarker` or placement entries set `ambientRegionId` to the sub-area id. |
| **Sub-area ambient** | `LightingBootstrap` or runtime `SetAmbientLight(regionId, level)` sets that region’s `CurrentAmbientLight` independently of the floor default. |
| **Example** | Floor default **10**; enclosed cave pocket uses region **99** at **0** → inside pocket is dark; outside remains day-lit. |
| **Inverse example** | Floor default **0**; rare “daylight shaft” pocket uses region **2** at **8** without changing the rest of the floor. |

### R2.3 — Day/night cycles (QA hazard)

| Rule | Detail |
|------|--------|
| **Optional** | Regional `AmbientPhaseScheduleEntry[]` on `LightingBootstrap` (already supported). |
| **Dark-floor QA** | While testing **Option A** (pitch-dark ambient), bootstrap **must not** advance phases that raise ambient (e.g. **0 → 3** at turn 5) unless the test **intentionally** exercises day/night. |
| **Requirement** | SampleScene **dark-floor test profile** uses **zero phases** or a **single phase** at ambient **0** with no automatic transitions (see §7.2). |

### R2.4 — Light scale (locked v0)

| Constant | Value | Notes |
|----------|-------|--------|
| `PitchDark` | 0 | Below visibility threshold → dim tile (with default threshold **3**). |
| `TorchEmission` | 6 | Wall / carried torch when lit. |
| `baseVisibilityThreshold` | 3 | `VisibilityManager`; `receivedLight >= 3` → full bright presentation in LOS. |
| Falloff | Manhattan | `max(0, emit - distance * falloffPerTile)`; default torch asset: radius **8**, **1** per tile. |

---

## 3. Presentation — dim tiles vs debug overlay

### R3.1 — Gameplay presentation (existing)

| State | Rule |
|-------|------|
| **Unseen** | Fog `unseenColor` — not in knowledge. |
| **Explored** | Memory tint + **frozen lighting snapshot** (parent doc §9). |
| **Visible + lit** | `visibleColor` when `receivedLight >= GetEffectiveLightThreshold`. |
| **Visible + dark tile** | `darkTileColor` when in LOS but under threshold. |
| **Party cell** | Occupied party member cell always fully bright (R7.1). |
| **Active emitter in LOS** | Emitter cell with `emitLight > 0` always fully bright in LOS (R7.1). |

### R3.2 — Debug warmth overlay (v0 QA only)

| Field | Requirement |
|-------|-------------|
| **Purpose** | Make torch **influence radius** obvious during QA (DCSS-style orange halo), separate from `darkTileColor` truth. |
| **Build** | **Development / Editor** only, or guarded by `#if DEVELOPMENT_BUILD` / serialized `enableLightingDebugOverlay` default **false** in release builds. |
| **Toggle** | Hotkey **`L`** (or context menu on `LightingSystem`) flips overlay on/off; log once: `[Lighting:DebugOverlay] on|off`. |
| **Data source** | Live `LightingService.GetReceivedLight(cell)` (and optionally `GetEmitLight`) — **not** fog snapshot. |
| **Visual** | Warm tint (e.g. orange **#E8A040** at partial alpha) on **visible** floor cells in LOS; strength scales with `receivedLight / LightLevel.Max` or step function: full warm if `received >= threshold`, faint warm if `received > 0` but under threshold. |
| **Layer** | Prefer dedicated **overlay tilemap** or `Tilemap.SetColor` on an overlay layer — **do not** mutate base floor art permanently. |
| **Future** | Replace or augment with authored torch flame sprites ([Lighting — Future backlog](Lighting-Future-Backlog.md) #40). |

---

## 4. Wall torch v0 (in scope)

### R4.1 — Content

| Asset | Requirement |
|-------|-------------|
| **`LightEmitterDefinition`** | Reuse or author `LightEmitter_Torch` (emission max **6**, falloff per §2.4). |
| **Wall cell** | **Emitter** on **wall map** cell (not walkable floor); may also register as receiver for ambient region of adjacent floor. |
| **Initial state** | `emitLight == 0` (unlit) at scene load. |
| **Interactable** | `InteractableTileDefinition` on **adjacent floor** bump cell **or** wall bump policy per project convention; effect: `SetTileEmissionEffect` → full torch emission. |
| **Precondition (v0)** | **Relaxed for QA:** ignite allowed **without** carried torch (document as `requiresCarriedLightSource: false` on test definition). **Production** wall torch may require carried light per parent R10.2 — flag on definition. |
| **Action cost** | Successful ignite consumes **one player action** (same as lever bump). |
| **Recompute** | `LightingService.SetEmission` triggers receiver recompute + `VisibilityManager` refresh. |

### R4.2 — SampleScene placement (locked)

| Rule | Detail |
|------|--------|
| **Anchor actor** | `Party_Tiefling_Mage` in hierarchy / scene instance. |
| **Anchor cell** | `Vector3Int.FloorToInt(anchor.transform.position) + cellOffset` (same as `LightingCellMarker`). |
| **Target cell** | **Wall tile** on `MapManager.WallMap` that **minimizes Manhattan distance** to anchor cell; ties broken by **lowest Y**, then **lowest X**. |
| **Adjacent floor** | Ensure at least one **orthogonally adjacent walkable floor** cell for party bump/ignite QA. |
| **Authoring** | Editor menu **JRogue/Lighting/Place Wall Torch Near Tiefling Mage (SampleScene)** computes cell at edit time and writes markers + interactable registration (see §7). |
| **Phase root** | Place under `LightingPhase_Phase3_RuntimeEmitters` (or dedicated `LightingTest_WallTorch` child) so scenario controller can enable/disable. |

### R4.3 — Acceptance (wall torch)

| ID | Criterion |
|----|-----------|
| **AC1** | With floor/sub-area ambient **0**, cells in LOS beyond torch falloff use **`darkTileColor`**. |
| **AC2** | After ignite, cells within falloff show **`visibleColor`** (or warm debug overlay when enabled). |
| **AC3** | Emitter wall cell visible in LOS at full bright even if receiver math would dim. |
| **AC4** | Debug overlay **`L`** clearly shows orange-ish disk matching received-light falloff. |
| **AC5** | No ambient cycle bump (e.g. turn 5 → 3) during dark QA profile unless phase schedule enabled. |

---

## 5. Dark-floor QA pocket (SampleScene)

### R5.1 — Intent

Provide a **stable** place to test dim tiles **without** setting global `defaultFloorAmbientLight` to 0 for the entire scene (avoids fighting traps, doors, and party movement elsewhere).

### R5.2 — Layout (minimum)

| Element | Spec |
|---------|------|
| **Region id** | **99** (reserved QA; not used by default floor). |
| **Ambient** | Region **99** at **0** always for v0 QA profile. |
| **Geometry** | Small enclosed room (≥ 5×5 floor) **near** Tiefling anchor (within **12** tiles); floor painted on existing `FloorMap`. |
| **Receivers** | `LightingCellMarker` per floor cell in room: `isReceiver`, `ambientRegionId: 99`. |
| **Global floor** | Rest of SampleScene keeps default region **0** at designer-authored ambient (recommend **10** for normal play). |
| **Wall torch** | Placed per §4.2 on wall **closest to Tiefling** (may be on room perimeter or adjacent corridor wall). |

### R5.3 — Optional editor: “Apply dark QA profile”

| Action | Effect |
|--------|--------|
| Clear bootstrap **phases** on region **0** OR set single phase ambient **0**. |
| Set `defaultFloorAmbientLight` to **10** (global bright) + region **99** at **0** (pocket dark). |
| Activate lighting scenario **Phase3_RuntimeEmitters** (or test-only scenario id). |
| Log: `[Lighting:QA] Dark pocket profile applied.`. |

---

## 6. Relationship to existing QA harness

| Component | Use in this milestone |
|-----------|------------------------|
| `LightingScenarioController` | Enable phase root for wall torch + dark pocket; avoid Phase **2** (fog) as default when testing live lighting tint only. |
| `LightingPhase3SampleContent` | **Supersede or relocate** hard-coded `(4, -2)` torch → Tiefling-nearest wall per §4.2. |
| `LightingScenarioSampleSceneBootstrap` | Extend or sibling menu to run §7 menus. |
| Verbose flags | `LightingService.verboseReceiveLogs`, `VisibilityManager.verboseDarkTileLogs` for `[Lighting:DarkTile]` / receive logs. |

---

## 7. Editor and scene authoring (v0)

### R7.1 — Menus (under **JRogue/Lighting/**)

| Menu | Behavior |
|------|----------|
| **Place Wall Torch Near Tiefling Mage (SampleScene)** | Resolve anchor → nearest wall cell → emitters + interactable + optional overlay sprite placeholder. |
| **Create Dark QA Pocket Near Tiefling (SampleScene)** | Build §5.2 room + region 99 markers. |
| **Apply Dark QA Lighting Profile** | §5.3 bootstrap/service settings; disable ambient cycle surprises. |
| **Bootstrap SampleScene Lighting Harness** | Existing menu; document that it must be run once if `LightingSystem` missing. |

### R7.2 — Dark QA profile (locked settings)

| Setting | Value |
|---------|-------|
| `defaultFloorAmbientLight` | **10** (main play area unchanged) |
| Region **99** `currentAmbientLight` | **0** |
| Region **0** phases | **[]** (empty) **or** one entry: ambient **0**, duration **9999** |
| `LightingScenarioController.activeScenarioIndex` | Phase **3** (runtime emitters) after placement |

### R7.3 — Manual QA script (play mode)

1. Enter Play with dark pocket + wall torch placed.  
2. Confirm dim floor in pocket before ignite.  
3. Bump/interact to light torch; confirm bright core + dim ring + debug overlay disk.  
4. Press **`L`**; overlay on/off.  
5. Walk party away; confirm fog/explored behavior unchanged (Phase 2 scenario separate test).  
6. Advance **5+ turns**; confirm ambient **does not** jump to **3** unless day/night profile enabled.

---

## 8. Carried torch v1 (specified — next milestone)

> **Not implemented in wall-torch v0 milestone.** Parent [Lighting — Requirements](Lighting-Requirements.md) R10.1 is updated by reference to this section.

### R8.1 — Item

| Field | Requirement |
|-------|-------------|
| **Category** | `ItemCategory.Accessory` |
| **`slotType`** | One of `EquipmentSlot.Accessory_MainHand`, `Accessory_OffHand`, or `Accessory_Head` (v0 content: **`Accessory_MainHand`** unless art/UI prefers “lantern off-hand”). |
| **Definition extension** | `LightSourceItemData` (or embedded fields on accessory `ItemData`): reference `LightEmitterDefinition`, `bool startsLit`, optional `canIgniteTiles`. |
| **Equip rule** | Standard `EquipmentLegalityEvaluator` / accessory equip path. |

### R8.2 — Runtime behavior

| State | Rule |
|-------|------|
| **Equipped + lit** | Register **virtual emitter** at bearer `GridPosition` each turn / on equip change; emission from definition (default **6**). |
| **Unequipped or extinguished** | Remove virtual emitter; recompute lighting. |
| **Movement** | Emitter follows bearer; no per-step ignite cost. |
| **Light wall torch** | When `canIgniteTiles` and equipped lit, satisfy parent R10.2 precondition for production wall torches. |
| **Turn cost** | Toggle lit/extinguished may cost one action (authored per item; default **yes** for v1). |

### R8.3 — Visibility

| Rule | Detail |
|------|--------|
| **Party union** | Carried emitters from all members contribute to **lit-visible** set (parent §5.3 / Phase 4 QA scenario). |
| **Dark tile on bearer** | Bearer cell remains R7.1 bright; adjacent cells use received light including virtual emitter. |

### R8.4 — SampleScene seed (v1)

| Item | Menu |
|------|------|
| `Item_Torch_Accessory` | **JRogue/Lighting/Seed Test Torch (Tiefling)** — add to `Party_Tiefling_Mage` inventory and optionally auto-equip accessory slot for instant QA. |

### R8.5 — Acceptance (carried — v1)

| ID | Criterion |
|----|-----------|
| **AC6** | Equip lit torch → floor within falloff brightens live; unequip → reverts (in dark ambient). |
| **AC7** | Two party members with torches → union of lit regions (Phase 4). |
| **AC8** | Lit accessory satisfies wall-torch precondition when `requiresCarriedLightSource` is true. |

---

## 9. Implementation phases (recommended)

| Phase | Deliverable | Est. scope |
|-------|-------------|------------|
| **A** | Requirements doc (this file) + cross-link from [Lighting — Requirements](Lighting-Requirements.md) §13–14 | Done when merged |
| **B** | Editor: dark pocket + dark QA profile + relocate wall torch to Tiefling-nearest wall | Scene + editor scripts |
| **C** | `LightingDebugOverlay` + **`L`** toggle | Runtime dev component |
| **D** | Wall torch ignite polish, `requiresCarriedLightSource` flag on interactable def | Data + tests |
| **E** | Carried torch v1 (§8) | Item data, equipment hook, virtual emitter |

---

## 10. Implementation checklist

### Wall torch + QA (this milestone)

- [x] `Docs/World/Lighting-QA-And-Torch-v0-Requirements.md` (this document)
- [x] Cross-link from [Lighting — Requirements](Lighting-Requirements.md) §10 / §14
- [x] Editor: **Place Wall Torch Near Tiefling Mage**
- [x] Editor: **Create Dark QA Pocket** + **Apply Dark QA Profile**
- [x] Remove or redirect `LightingPhase3SampleContent` fixed `(4, -2)` to authored cell
- [x] `LightingDebugOverlay` (dev-only) + **`L`** toggle
- [ ] SampleScene: region **99** pocket + wall torch AC1–AC5 verified (run **JRogue/Lighting/Setup Lighting QA (All SampleScene Steps)** in Unity)
- [x] Unit test: nearest-wall resolver given mock anchor + wall cells

### Carried torch (next milestone)

- [ ] `LightSourceItemData` + `Item_Torch_Accessory`
- [ ] `EquipmentManager` / lighting bridge for virtual emitter
- [ ] Seed menu for Tiefling
- [ ] AC6–AC8

---

## 11. Debug logging

| Prefix | When |
|--------|------|
| `[Lighting:QA]` | Editor profile apply, torch placement cell |
| `[Lighting:DebugOverlay]` | Overlay toggle |
| `[Lighting:DarkTile]` | Visible but under threshold (existing) |
| `[Lighting:Emit]` | Torch ignite (existing) |
| `[Lighting:Cycle]` | Should **not** appear during dark QA profile |

---

## 12. Open questions (defaults chosen)

| # | Question | v0 default |
|---|----------|------------|
| 1 | Wall torch bump from floor vs wall | **Adjacent floor** bump into wall torch (match door/interactable adjacency) |
| 2 | Ignite without carried torch in QA | **Allowed** on `Interactable_WallTorch_Test` only |
| 3 | Accessory slot for carried torch | **`Accessory_MainHand`** |
| 4 | Global vs pocket dark for Option A | **Pocket region 99** at 0, global **10** |

---

## 13. References

- Parent spec: [Lighting — Requirements](Lighting-Requirements.md)  
- Backlog overlay art: [Lighting — Future backlog](Lighting-Future-Backlog.md)  
- Interactable effect: `SetTileEmissionEffect`  
- Existing torch emitter asset: `Assets/Prefabs/Lighting/Torch.asset`  
- SampleScene party anchor: `Party_Tiefling_Mage` (grid from transform, typically near **(1, -2)** with current placement)
