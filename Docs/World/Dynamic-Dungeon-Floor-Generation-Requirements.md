# Dynamic dungeon floor generation — Requirements (draft)

**Status:** **v0 locked** — implement as **v0a** (vertical slice) then **v0b** (full v0). Parent spec for layout/portal/lighting; v1+ in §2.4 and §17.

**Purpose:** Move from **hand-authored SampleScene** to **per-run dungeon floors** with **pre-baked layouts** (v0), multi-floor persistence, and Barbarian-style portals. v1 adds procedural room-and-corridor (§17). **Macro habitat zones:** [Dungeon zone layout](Dungeon-Zone-Layout-Requirements.md). **Per-zone population:** [Dungeon zone population](Dungeon-Zone-Population-Requirements.md).

**Depends on:** `MapManager`, `GridManager`, `PartyManager`, `TurnManager`, `VisibilityManager`, `LightingService`, `HazardService`, `TrapService`, `InteractableTileService`, `DoorService`, `EnemySpawnService`, floor economy (`FloorItemPileService`, `FloorEssenceService`), [Fog of War](Fog-Of-War-Requirements.md), [Lighting](Lighting-Requirements.md), [Traps](../Combat/Traps-Requirements.md), [Environmental hazards](../Combat/Environmental-Hazards-Requirements.md), [Interactables](../Combat/Interactable-Tiles-Requirements.md), [Doors](Door-Requirements.md), [Altars](Altar-And-Map-Interact-Requirements.md), [Enemy spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md), [Floor items](../Inventory/Floor-Item-Pile-Requirements.md), [Essence drops](../Essence/Enemy-Essence-Drops-Requirements.md).

**Related scenes:**

| Scene | Role |
|-------|------|
| **`SampleScene.unity`** | **Hand-authored dungeon scene** — same production hierarchy as dynamic floors, **fixed** tilemap/content, **no** Generate button. All existing feature QA (lighting phases, etc.) continues to work unchanged. |
| **`DungeonFloor.unity`** (TBD name) | Production **dynamic** dungeon shell — `DungeonFloorRuntime` generates content per visit. |
| **`DungeonFloorTest.unity`** (TBD name) | **Test-only** duplicate of dungeon shell + **Generate Test Floor** control (editor or play-mode) for iteration without touching SampleScene. |

**Explicitly out of scope (v0 generation milestone):** Full save/load of run state; online co-op; full DCSS parity; automatic art pipeline for every vault size. **In scope (specified below):** floor-to-floor transitions, revisiting floors, portal heuristics (§8).

---

## 1. Scene strategy (recommended)

### R1.1 — Question: one scene per floor ID, one reusable scene, or new scene per run?

| Option | Summary | Verdict |
|--------|---------|---------|
| **A — Saved scene per dungeon floor ID** (e.g. `DungeonFloor_01.unity`, `DungeonFloor_02.unity`) | Each floor *type* has its own Unity scene file with fixed hierarchy; generation only fills tilemaps. | **Useful as a template once**, but **do not multiply scenes per run**. Good for authoring **default hierarchy** (managers, cameras, UI). |
| **B — Single reusable runtime scene** | One `DungeonFloor.unity` (or `DungeonRun.unity`) loaded for **every** floor visit; content is **cleared and regenerated** from `DungeonFloorDefinition` + run seed. | **Recommended (primary).** Matches DCSS (“same engine, new map each game”). |
| **C — `SceneManager.CreateScene` per floor per run** | New empty scene at runtime for each floor. | **Not recommended** unless you need hard isolation for debugging. Adds load/unload cost, breaks DontDestroyOnLoad assumptions, and duplicates work B already solves. |

**Locked recommendation:**

1. **Production:** **one reusable dungeon floor scene** (shell) + **data-driven generation** per visit.
2. **SampleScene:** keep as **hand-authored dungeon** scene (fixed layout, lighting QA, feature regression — §5.12).
3. **Optional:** **prefab-based scene template** (`DungeonFloorSceneRoot.prefab`) instantiated into B if you prefer not to maintain a `.unity` per environment — still **one logical shell**, not N scenes per run.

**Per-run flow (illustrative):**

```text
Enter dungeon (run start)
  → Load RunBootstrap + dungeon shell (§1.4)
  → First visit Floor 1: create DungeonFloorInstance, stamp layout + populate once
  → Spawn party at playerStart formation (§6.5)
  → Play
Portal to Floor 2: park Floor 1, activate/create Floor 2, spawn at portal arrival
Return to Floor 1: park Floor 2, re-activate Floor 1 instance unchanged
Exit dungeon (later): destroy all floor instances
```

**Multi-floor (locked):** The run keeps **multiple dungeon floors alive at once**. Floor 1 → Floor 2 → return to Floor 1 must **restore Floor 1 exactly as left** — **no** second layout stamp, **no** repopulation pass, **no** teardown of Floor 1 when leaving. See §1.3–1.4.

### R1.2 — Locked: hybrid persistence (DDOL party + multi-floor instances)

Use a **three-part** model:

| Layer | Lifetime | What lives here |
|-------|----------|-----------------|
| **Run layer (DDOL)** | From dungeon entry until **exit dungeon** (later) or game over | Party, UI, `PartyManager`, `DungeonRunState`, **`DungeonFloorInstanceManager`** |
| **Floor instances (DDOL, dormant or active)** | Created on **first visit** to that `floorId`; destroyed only on **exit dungeon** | Per-floor tilemaps, containers, service snapshots, fog/lighting memory for that floor |
| **Active floor view** | Exactly **one** `floorId` at a time | `MapManager` / `GridManager` / `VisibilityManager` wired to the active instance; party positioned on that grid |

**Do not** destroy Floor 1’s tilemaps or enemy state when the player walks to Floor 2. **Deactivate** (or park) Floor 1’s root; **activate** Floor 2’s root.

**Exit dungeon (later feature):** Single hook `DungeonRunState.ExitDungeon()` → destroy **all** floor instances, clear registry, then transition to overworld/hub (TBD). That is the **only** normal teardown of floor content mid-run.

### R1.3 — DDOL party + floor switching (detailed)

#### Why DDOL for the party

- Player **travels between dungeon floors** with the **same roster**, gear, HP, buffs, and resources.
- Floor transitions must **not** destroy party prefabs.
- `PartyManager`, inventories, and formation history stay on the **run** root.

#### Floor transition (A → B) — **no teardown of A**

```text
Player uses portal on active Floor A
  → Park Floor A: deactivate A's floor root (or snapshot + disable — §1.4)
  → Floor A state remains: dead enemies, open doors, taken items, explored fog, etc.
  → If Floor B first visit: create DungeonFloorInstance(B), run generation phases once
  → If Floor B already exists: activate existing instance (no regeneration)
  → Wire MapManager / GridManager / VisibilityManager to B
  → Place party formation at B's portal arrival anchor (§6.5)
  → Set activeFloorId = B
```

#### Return visit (B → A)

```text
Park Floor B (same as above)
  → Activate existing DungeonFloorInstance(A) — **must not** run LayoutStampPhase or population again
  → Rewire managers to A's tilemaps
  → Place party at A's **fixed arrival anchor** for the portal link used (§8.8) — same cells every time that portal is used
  → Enemies, items, traps, altar state on A are exactly as when the player left
```

#### Exit dungeon (later — **only** full floor teardown)

```text
DungeonRunState.ExitDungeon()  // hub, overworld, camp — out of scope v0
  → For each floorId in DungeonFloorInstanceManager: Destroy instance + Unity roots
  → Clear fog snapshots, trap registrations, etc.
  → Party may persist to hub (DDOL) or scene transition — TBD
```

Between **enter dungeon** and **exit dungeon**, floor instances **accumulate** (Floor 1, then 2, then 1 again uses existing 1).

#### Visit policies (for special floors only)

Default dungeon floors use **`PersistFullFloorState`** (locked). Optional per-floor overrides on `DungeonFloorDefinition`:

| Policy | When leaving | When returning | Use |
|--------|--------------|----------------|-----|
| **`PersistFullFloorState`** (**default**) | Instance parked, full state kept | Same instance activated | **Floor 1 ↔ Floor 2 gameplay** |
| **`CacheLayoutRegeneratePopulation`** | Layout kept | New enemy/item rolls | Rare challenge floors |
| **`RegenerateOnEveryVisit`** | Instance destroyed | Full regen | Debug / roguelike reset floors |

**Do not** use regenerate policies for the main dungeon chain unless explicitly authored.

#### Portal linking

`portalLinkId` pairs portals across floors (§8). Arrival position on return comes from the **linked portal rule** on the destination floor, not a new `playerStart` roll.

### R1.4 — Multiple active floors — implementation thoughts

**Recommended v0 architecture:** `DungeonFloorInstanceManager` on DDOL run root:

```text
DungeonRun (DDOL)
├── Party / UI / Input  (existing)
├── DungeonFloorInstanceManager
└── Floors/
    ├── dungeon_floor_01/   ← Grid, tilemaps, EnemyContainer, floor services scoped to 01
    └── dungeon_floor_02/
```

| Concern | Approach |
|---------|----------|
| **One MapManager** | On switch: `MapManager.SetActiveFloor(instance)` rebinds tilemap references to the active child |
| **Services** (`TrapService`, `HazardService`, …) | **v0a:** global services OK for single active floor; **v0b:** on switch, **export/import** registrations into `DungeonFloorInstance` (or per-floor sub-services) so Floor 1 state does not bleed into Floor 2 |
| **Fog / lighting** | Per-floor `VisibilityManager` state or snapshot blob on park |
| **Memory** | Cost = sum of visited floors; acceptable for typical run depth (2–10). Cap max parked floors if needed later |
| **Alternative (v0 fallback)** | Serialize instance to `FloorSnapshot` when parking, destroy GameObjects, **rehydrate** on return — logically preserved, heavier IO; use if single-scene tilemap swap is too risky |

**Why not teardown-on-switch:** Your design (return to Floor 1 and find it unchanged) requires either **parked roots** or **lossless snapshot**. Teardown + `Initialize()` from seed violates “not recreated.”

**Sync with party:** Party is **not** parented under floor roots long-term; on switch, move `GridMover` anchors to cells on the **active** floor’s grid.

#### Scene loading — **locked for v0**

| Decision | Choice |
|----------|--------|
| **Pattern** | **B —** `DungeonRun` (DDOL) + `Floors/{floorId}/` child roots (§16) |
| **Park** | **`SetActive(false)`** on inactive floor root; enemies/UI on that floor disabled, not destroyed |
| **Snapshot fallback** | Defer unless disable roots cause unacceptable memory — not v0a |

**Locked intent:** Party/UI **DDOL**; **one `DungeonFloorInstance` per visited `floorId`** until exit dungeon; **switch = park/activate**, not regenerate.

---

## 2. DCSS-style generation — feedback for JRogue

Dungeon Crawl Stone Soup does **not** store a finished map per run in a scene file. It **generates** each branch level from:

1. **Layout algorithm** (room-and-corridor, caves, themed variants) → grid of walls/floor/doors.
2. **Post-process** (minimap, travel, sealing, etc.).
3. **Vault injection** — hand-authored `.vault` chunks (XML) with tags, placed at valid anchors for “hand-crafted feel.”
4. **Population passes** — monsters, items, traps, shops, altars — driven by **depth**, **branch**, and **tables**, with **placement masks** and **distance rules** (e.g. avoid stairs, avoid entry).

### R2.1 — What to adopt (phased)

| DCSS concept | JRogue v0 proposal |
|--------------|-------------------|
| **Branch / floor identity** | `DungeonFloorDefinition` ScriptableObject (`floorId`, size, tables, ambience). |
| **Seeded RNG** | `runSeed` + `floorIndex` + salt per pass → reproducible debug. |
| **Pre-baked layout (v0)** | `DungeonLayoutStamp` ScriptableObject or binary grid — stamped into tilemaps (§2.4). |
| **Room-and-corridor (post-v0)** | Procedural generator milestone. |
| **Vaults** | `DungeonVaultDefinition` — stamp of tiles + entity placements; placed 0–N times per floor. |
| **Population tables** | Scriptable lists: enemy weights, hazard density, trap density, item tables, interactable events. **ZoneComposite:** per-zone profiles — [Dungeon zone population](Dungeon-Zone-Population-Requirements.md). |
| **Safe zone around start** | **Chebyshev 5** default on `DungeonFloorDefinition` (§7.1) — applies to enemies, hazards, and traps unless overridden per floor. |
| **Layout-specific features** | Doors per-floor policy (§6.6); day/night per floor (§6.4); portal heuristics per floor (§8). |

### R2.2 — What to defer (post pre-baked v0)

- Procedural **room-and-corridor** / cave / themed branch layouts (§2.4).
- Full DCSS **layout weight** tables per branch.
- **Shop**, **timed portal** variants beyond heuristic placement.
- **Monster placement by experience budget** (start with count + table + safe radius).
- **Trap / item theme by depth** beyond a single table per floor.
- **Disk save/load** of parked floor snapshots (§1.3 — in-memory persistence is v0).

### R2.3 — Suggested new assets / code areas

| Asset / type | Role |
|--------------|------|
| `DungeonFloorDefinition` | Per-floor-id: size, ambience, day/night, safe radii, generator profile, vault list, population tables. |
| `DungeonGeneratorProfile` | Algorithm params (room count, min size, corridor width, door chance). |
| `DungeonVaultDefinition` | Rect footprint, tile stamp, entity placements (enemies, items, altar, lever, hazards). |
| `DungeonPopulationTable` | Weighted entries for enemies / items / traps / hazards / interactables. |
| `DungeonFloorRuntime` | Scene orchestrator: clear, generate, bootstrap services, spawn party. |
| `IDungeonGenerationPhase` | Extensibility hook (§10). |
| `DungeonLayoutStamp` | Pre-baked floor/wall grid for v0 (§2.4). |
| `PortalPlacementRule` | Per-floor portal heuristic (§8). |
| `PartyFormationSpawnProfile` | 1–6 cell footprint relative to anchor (§6.5). |

Existing **placement sets** (`TrapPlacementSet`, `InteractablePlacementSet`, `AltarPlacementSet`, `EnvironmentalHazardBootstrap` pattern) become **outputs** of generation (runtime arrays), not only inspector lists on SampleScene.

### R2.4 — Layout: v0 pre-baked stamp and future milestones

#### v0 (first milestone) — **pre-baked layout**

| Step | Behavior |
|------|----------|
| 1 | Author **`DungeonLayoutStamp`** per floor id (or per “layout variant”): `width`, `height`, floor/wall cell arrays, optional fixed markers (`playerStart`, reserved portal slots). |
| 2 | `LayoutStampPhase` copies stamp into `MapManager` tilemaps (no room-and-corridor RNG). |
| 3 | Population passes run on walkable cells from the stamp (§7). |
| 4 | Vaults may **overwrite** rectangular regions of the stamp (§9). |

**Why pre-baked first:** Unblocks DDOL/floor teardown, containers, portal rules, population, and SampleScene parity without blocking on proc-gen quality.

**Floor 1 / 2 sizes:** Stamps authored at **30×30** and **20×20** respectively (per `DungeonFloorDefinition`).

#### Beyond v0 — layout roadmap (future improvements)

| Milestone | Delivers | Notes |
|-----------|----------|-------|
| **v1 — Room-and-corridor** | Random rooms, MST/corridor connection, configurable room count/size | DCSS “dungeon” feel baseline; stamp optional per branch |
| **v1.1 — Cave generator** | Blob / cellular automata caves | Themed floors |
| **v2 — Layout weights** | `DungeonBranchProfile` picks generator + params by depth/run | Like DCSS branch types |
| **v2.1 — Prefab sub-layouts** | “Mini-stamps” snapped together | Bridge between pure stamp and full proc |
| **v3 — Vault-heavy proc** | Proc gen reserves anchor sites for vaults before corridors | Vaults feel intentional |
| **v4 — Connectivity QA** | Validate all rooms reachable; fix orphans; guarantee portal site exists | Required before random portals |
| **v5 — Themed constraints** | Forest edge bands, “building interior” no day/night, etc. | Pairs with §6.4 / §8 |
| **v6 — Persisted floor snapshot** | Save explored tiles + population state (§1.3 `PersistFullFloorState`) | Roguelike continue |

**Generator profile** on `DungeonFloorDefinition` evolves:

```text
layoutMode: PreBakedStamp | ProceduralRoomCorridor | ProceduralCave | ...
layoutStamp: (when PreBakedStamp)
generatorProfile: (when procedural)
```

---

## 3. Production scene hierarchy (professional layout)

Today **SampleScene** mixes production systems, hand-painted `Grid`, party/enemy instances, and QA lighting/harness objects at the **root** with no stable taxonomy. Production dungeon scene should use **empty parent GameObjects** and **one component per concern** where possible.

### R3.1 — Recommended root tree

```text
DungeonFloor                          # Scene root (or DungeonFloorSceneRoot prefab)
├── Runtime
│   └── DungeonFloorRuntime           # Orchestrates init / generate / teardown
├── Systems
│   ├── Map
│   │   ├── MapManager
│   │   └── GridManager
│   ├── TurnCombat
│   │   ├── TurnManager
│   │   └── CombatThreatCoordinator
│   ├── WorldFeatures
│   │   ├── HazardService             # + EnvironmentalHazardBootstrap OR gen applies registrations
│   │   ├── TrapService               # + TrapBootstrap OR gen-only Register()
│   │   ├── InteractableTileService   # + bootstrap from gen
│   │   ├── DoorService               # + DoorTileBootstrap if floor has doors
│   │   └── AdjacentMapInteractableService  # + AltarBootstrap from gen
│   ├── FloorEconomy                  # Optional grouping; may stay on Party DDOL
│   │   ├── FloorItemPileService
│   │   └── FloorEssenceService
│   └── Progression                   # If not DDOL with party
│       └── (PartyExperienceService host — TBD)
├── InputParty
│   ├── InputHandler
│   ├── PartyManager                  # Or DDOL from RunBootstrap
│   └── TargetingReticleView
├── Presentation
│   ├── Main Camera                   # CameraFollow
│   ├── Global Light 2D
│   └── UI
│       ├── Canvas                    # InventoryUI, etc.
│       └── EventSystem
├── World
│   ├── Tilemaps
│   │   └── Grid                      # Unity Grid + child tilemaps only (no scripts on layers)
│   │       ├── Floor_Layer
│   │       ├── Wall_Layer
│   │       ├── Hazard_Overlay
│   │       ├── Trap_Overlay
│   │       ├── Door_Overlay
│   │       ├── Interactable_Overlay
│   │       └── Altar_Overlay
│   └── Containers
│       ├── PartyContainer            # Runtime-spawned party (not hand-placed in prod)
│       ├── EnemyContainer            # All enemy instances parented here
│       ├── WorldItemContainer        # Legacy WorldItem prefabs (if still used)
│       └── DynamicViews              # Optional: service-created view roots
└── Lighting
    ├── LightingService
    └── LightingBootstrap             # Production only — NO LightingScenarioController
```

### R3.2 — Rules

| Rule | Rationale |
|------|-----------|
| **Systems never parented to Tilemaps** | Regenerating map clears tiles without destroying managers. |
| **Containers for spawned actors** | `EnemyContainer` / `WorldItemContainer` — find, cull, and destroy on regen. |
| **Single `DungeonFloorRuntime`** | One entry point: `Initialize(definition, seed)` — avoids scattered `Start()` bootstraps fighting order. |
| **Bootstrap order documented** | e.g. tilemaps → `MapManager` bounds → topology gen → overlay services → population → party spawn → `VisibilityManager.Refresh`. |

### R3.3 — `EnemyContainer` / `ItemContainer` (your proposal)

**Recommendation: adopt both.**

| Container | Holds | Created by |
|-----------|-------|------------|
| **`EnemyContainer`** | All `EnemyController` instances | `EnemySpawnService` + floor gen population pass |
| **`WorldItemContainer`** | `WorldItem` scene objects (legacy ground items) | Item spawn pass |
| **`DynamicViews`** (optional) | `FloorItemPileService` / `FloorEssenceService` view roots | Services read serialized transform ref on `DungeonFloorRuntime` |

**API change (generation milestone):** `EnemySpawnService` and item spawners accept optional `Transform parent`; default to `EnemyContainer` / `WorldItemContainer` when present.

**Note:** Floor piles and essences are **service-driven** (not necessarily under `WorldItemContainer`); document both patterns until legacy `WorldItem` is retired.

---

## 4. QA vs production — exclusion list

SampleScene (and editor tooling) may keep QA components. **Dynamic dungeon scene must not.**

### R4.1 — QA-only controller / scene scripts (do not ship on dungeon scene)

| Script | Location today | Action for production dungeon |
|--------|----------------|------------------------------|
| `LightingScenarioController` | SampleScene `LightingSystem` | **Omit** — phase switching is QA-only. |
| `LightingDebugOverlay` | SampleScene `LightingSystem` | **Omit** — debug gizmo overlay. |
| `LightingCellMarker` | SampleScene markers | **Omit** — manual cell probes. |
| `LightingPhase3SampleContent` | `WallTorch_Test`, phase roots | **Omit** — sample torch wiring. |
| `LightingQaPlacementResolver` | Code + editor QA pack | **Do not call** from production gen; use `LightingPlacementSet` from data or gen pass. |
| Scene roots `LightingPhase_*` | SampleScene | **Omit** entire subtrees. |
| `LightingTest_DarkPocket` + children | SampleScene | **Omit**. |
| `SampleSceneHazardPlacements` | Not on SampleScene root but QA script | **Replace** with hazard population pass + `EnvironmentalHazardBootstrap` fed by gen. |
| `SampleSceneInteractablePlacements` | QA hard-coded cells | **Replace** with gen / vault placements. |
| `EssenceTestHarness` | `_InputManager` | **Omit** — context-menu essence testing. |
| `BowKitSampleSceneBootstrap` | On `Party_Barbarian_Warrior` | **Omit** on runtime-spawned party. |
| `MonsterMapPresenceHost` | `Enemy_MapPresenceTestSkeleton` only | **Omit** unless floor definition enables map-presence test mode. |
| Hand-placed QA enemies | `Enemy_MapPresenceTestSkeleton` | **Omit** — gen spawns from tables only. |

### R4.2 — Editor-only (never in player build)

| Asset / script | Notes |
|----------------|-------|
| `LightingScenarioSampleSceneBootstrap` | Editor menu wires SampleScene. |
| `LightingScenarioQaPack` / `LightingScenarioAssetPackCreator` | QA asset creation. |
| `LightingScenarioControllerEditor` | Inspector helper. |
| `AltarAssetPackCreator`, `EssenceAssetPackCreator` | Content authoring — OK in editor, not in scene. |

### R4.3 — Production equivalents (keep)

| Script | Role in dungeon scene |
|--------|------------------------|
| `LightingBootstrap` + `LightingService` | Apply `DungeonFloorDefinition` ambience (§5). |
| `TrapBootstrap` **or** gen calls `TrapService.Register` | Traps from population pass. |
| `EnvironmentalHazardBootstrap` **or** gen registers hazards | Hazards from population pass. |
| `InteractableTileBootstrap` **or** gen registers interactables | Levers, etc. |
| `AltarBootstrap` **or** gen registers altars | Offering altars from vaults/tables. |
| `DoorTileBootstrap` | Only if `DungeonFloorDefinition.hasDoors`. |

### R4.4 — SampleScene policy

- **Retain** SampleScene for feature QA, lighting phases, hand-placed regression.
- **Do not** duplicate its ad-hoc root list into the dungeon scene.
- Add **`#if UNITY_EDITOR` or `[SerializeField] bool enableQaHarness`** only where dual-use is unavoidable — prefer **separate QA scene** or **QA prefab** over branching production code.

---

## 5. Production scene — GameObjects and scripts inventory

Target: **`DungeonFloor.unity`** (name TBD). Below: **components to host**, not hand-placed enemies/party.

### R5.1 — `Runtime`

| GameObject | Scripts |
|------------|---------|
| `DungeonFloorRuntime` | `DungeonFloorRuntime` (**new**) |

### R5.2 — `Systems/Map`

| GameObject | Scripts |
|------------|---------|
| `MapManager` | `MapManager` |
| `GridManager` | `GridManager` |

### R5.3 — `Systems/TurnCombat`

| GameObject | Scripts |
|------------|---------|
| `TurnManager` | `TurnManager` |
| `CombatThreatCoordinator` | `CombatThreatCoordinator` |

### R5.4 — `Systems/WorldFeatures`

| GameObject | Scripts |
|------------|---------|
| `HazardService` | `HazardService` |
| `TrapService` | `TrapService` |
| `InteractableTileService` | `InteractableTileService` |
| `DoorService` | `DoorService` |
| `MapInteract` | `AdjacentMapInteractableService` |

**Bootstraps:** Either dedicated child `FeatureBootstrap` with `TrapBootstrap`, `InteractableTileBootstrap`, `AltarBootstrap`, `DoorTileBootstrap`, `EnvironmentalHazardBootstrap` **disabled until gen completes**, or `DungeonFloorRuntime` registers placements directly (preferred long-term).

### R5.5 — `Systems/FloorEconomy` (if not on DDOL `PartyManager`)

| GameObject | Scripts |
|------------|---------|
| `FloorItemPileService` | `FloorItemPileService` |
| `FloorEssenceService` | `FloorEssenceService` |
| `EnemyLootService` | `EnemyLootService` |
| `ManaStoneAutoPickupService` | `ManaStoneAutoPickupService` |
| `PartyManaStoneLedger` | `PartyManaStoneLedger` |

**Note:** Today many of these are `PartyManager.EnsureComponent` — **TBD** whether floor scene or run bootstrap owns them (§12).

### R5.6 — `InputParty`

| GameObject | Scripts |
|------------|---------|
| `InputHandler` | `InputHandler`, `PlayerInput` (Input System) |
| `PartyManager` | `PartyManager`, `PartyExperienceService`, `PartyRestState`, `RestSessionService`, … |
| `TargetingReticleView` | `TargetingReticleView` |

`PlayerCommandProcessor` — **not** a MonoBehaviour; constructed by `InputHandler`.

### R5.7 — `Presentation`

| GameObject | Scripts |
|------------|---------|
| `Main Camera` | `Camera`, `CameraFollow`, URP camera data, `AudioListener` |
| `Global Light 2D` | `Light2D` |
| `Canvas` | `InventoryUI`, `CanvasScaler`, `GraphicRaycaster`, child UI views |
| `EventSystem` | `EventSystem`, `InputSystemUIInputModule` |

**UI modals** (`EssencePickupConfirmDialogUI`, `TrapConfirmDialogUI`, etc.) — remain **runtime-created** (no scene node required).

### R5.8 — `World/Tilemaps`

| GameObject | Scripts |
|------------|---------|
| `Grid` | Unity `Grid` only |
| `Floor_Layer` | `Tilemap`, `TilemapRenderer` |
| `Wall_Layer` | `Tilemap`, `TilemapRenderer` |
| Overlay layers | `Tilemap`, `TilemapRenderer` (hazard, trap, door, interactable, altar) |

### R5.9 — `World/Containers`

| GameObject | Scripts |
|------------|---------|
| `PartyContainer` | *(empty)* — party prefabs instantiated at run |
| `EnemyContainer` | *(empty)* |
| `WorldItemContainer` | *(empty)* |

### R5.10 — `Lighting` (production)

| GameObject | Scripts |
|------------|---------|
| `Lighting` | `LightingService`, `LightingBootstrap` |

### R5.11 — `Visibility`

| GameObject | Scripts |
|------------|---------|
| `VisibilityManager` | `VisibilityManager` |

**Not in scene (code-only):** `FloorLifetimeTicker`, `EnemyMeleeCombat`, `EnemySpawnService`, `EnemyLootRoller`, generation phases.

### R5.12 — SampleScene vs production dungeon scenes

| | **SampleScene** | **DungeonFloor** / **DungeonFloorTest** |
|---|-----------------|----------------------------------------|
| Layout | Hand-painted tilemaps | Generated from stamp (v0) |
| Party / enemies | Hand-placed | Spawned at runtime |
| Generate button | **No** | **Yes** on `DungeonFloorTest` only |
| Lighting QA phases | **Yes** (optional roots) | **No** (production lighting only) |
| Purpose | Feature regression, lighting QA, integration | Run progression, proc population |

**SampleScene-only objects:** `Party_*`, `Enemy`, `GiantSkeletonEnemy`, `Enemy_MapPresenceTestSkeleton`, `Sword Item`, world items, `LightingPhase_*` / markers, `_InputManager`, QA bootstraps on party/enemy.

**Requirement:** Refactoring hierarchy for dynamic scenes **must not** break SampleScene play mode (AC11).

---

## 6. Floor definition data (`DungeonFloorDefinition`)

Author under `Assets/Data/Dungeon/Floors/` (path TBD).

### R6.1 — Core fields (v0)

| Field | Type | Example | Notes |
|-------|------|---------|-------|
| `floorId` | string | `dungeon_floor_01` | Stable id for saves/logs. |
| `displayName` | string | `Floor 1` | UI / debug. |
| `width` | int | **30** | Tile extent (see §6.2). |
| `height` | int | **30** | |
| `layoutMode` | enum | `PreBakedStamp` (v0) | §2.4. |
| `layoutStamp` | `DungeonLayoutStamp` | floor_01_stamp | Required when pre-baked. |
| `generatorProfile` | asset | — | Used when layout becomes procedural. |
| `vaults` | list | floor-1 vaults | Weighted vault placements (§9). |
| `defaultAmbientLight` | `LightLevel` | **maximum** | §6.3. |
| `dayNightCycle` | optional ref | null or profile | §6.4. |
| `doorPolicy` | enum | §6.6 | `None`, `Procedural`, `VaultOnly`, `StampOnly`. |
| `playerStart` | cell | stamp marker | First-time / default spawn anchor (§6.5). |
| `partyFormationSpawn` | profile | 1–6 cells | §6.5. |
| `playerSafeRadius` | int | **5** | **Chebyshev** — enemies, hazards, traps (§7.1). |
| `portalRules` | list | §8 | One or more `PortalPlacementRule` assets. |
| `visitPolicy` | enum | `PersistFullFloorState` (default) | §1.3 — override only for special floors. |
| `enemyPopulation` | table | skeleton weights | §7. |
| `hazardPopulation` | table | lava/gas | §7. |
| `trapPopulation` | table | spike/bear/dart | §7. |
| `floorItemPopulation` | table | potions, gear | §7. |
| `interactablePopulation` | table | levers, altars | §7. |

**v0 examples (implementer confirms in assets):**

- **Floor 1:** 30×30 stamp, max ambient, `portalRules` = four orthogonal edge portals (§8.2), `playerSafeRadius` = 5, `doorPolicy` per design.
- **Floor 2:** 20×20 stamp, max ambient, portal rules TBD (e.g. single exit heuristic).

### R6.2 — Grid bounds

- Generation writes only within `[0, width) × [0, height)` — origin **(0, 0)** = bottom-left (v0 locked).
- `MapManager` / `GridManager` must support **resizing** or **preallocated** tilemaps cleared each gen — **refactor required** (§10).

### R6.3 — Default light ambience (v0)

- Set **`defaultAmbientLight`** to project **maximum** light level (per [Lighting](Lighting-Requirements.md) scale once locked).
- `LightingBootstrap` applies floor ambient to all receiver cells (or region covering full floor).
- Vaults may place emitters (torches) on top.

### R6.4 — Day / night cycle per floor (elaboration)

Dungeon floors differ in whether **outside light** can reach the play space. Lighting is already modeled in [Lighting-Requirements.md](Lighting-Requirements.md) as **overhead / regional ambient** with optional **turn-based cycles** (§3 G3, glossary “Day/night cycle”).

#### Three authoring modes (per `DungeonFloorDefinition`)

| Mode | `dayNightCycle` | Player-visible behavior | Examples |
|------|-----------------|-------------------------|----------|
| **Constant ambient** | `null` | Entire floor stays at `defaultAmbientLight` (v0: **max**) forever | Deep cave, sealed dungeon, underground building |
| **Cycling ambient** | `DayNightCycleDefinition` | Floor-wide ambient oscillates every **X** player phases between `ambientMin` and `ambientMax` | Outdoor ruin, open-air plateau, shallow mine with sky light |
| **Regional override (future)** | Cycle + `AmbientRegion` masks | Sub-areas ignore cycle (always dark) or use different curves | Forest canopy vs clearing; building interior tile rect |

#### What “no outside light” means in systems

- **`LightingBootstrap`** applies constant max (or authored floor default) to all receiver cells at init.
- **`TurnManager.NotifyPartyTurnStart`** does **not** tick day/night when `dayNightCycle` is null.
- Enemies and fog still use normal sight rules; only **ambient contribution** is frozen.

#### What “has a cycle” means

- `DayNightCycleDefinition` fields (illustrative):
  - `periodPlayerPhases` — **X** turns per phase (e.g. 30 turns day, 30 turns night).
  - `ambientDay` / `ambientNight` — scalars on project light scale (v0 may snap between two tiers).
  - `startPhase` — `Day` or `Night` at first visit.
- On each **player phase boundary**, `LightingService` advances cycle index and recomputes floor ambient (full recompute acceptable v0).

#### Pairing with layout and vaults

| Floor type | Typical lighting |
|------------|------------------|
| Indoor / cave stamp | Constant max or constant dim (author choice) |
| Outdoor stamp | Cycling ambient |
| Vault “torch room” | Local **emitters** in vault stamp; unaffected by whether floor has global cycle |

**v0:** Default **`dayNightCycle = null`** (constant **maximum**) on Floor 1 and 2 unless a floor asset explicitly enables a test cycle.

**Not v0:** Magical darkness zones, weather, emitter-driven day/night (see [Lighting-Future-Backlog.md](Lighting-Future-Backlog.md)).

### R6.5 — Party spawn: multi-cell formation (1–6 members)

Party spawn is **not** a single tile when multiple members exist. Use a **`PartyFormationSpawnProfile`** (ScriptableObject or embedded on floor definition):

| Field | Purpose |
|-------|---------|
| `anchor` | Primary cell — usually `playerStart` from stamp or **portal arrival** cell |
| `relativeOffsets[]` | List of `Vector3Int` offsets from anchor for each party slot (index 0 = active member / leader default) |
| `maxSlots` | **6** — supports parties from **1 to 6** living members |

**Algorithm (`PartySpawnPhase`):**

```text
livingMembers = partyMembers where HP > 0, preserve roster order
offsets = formationProfile.GetOffsetsForCount(livingMembers.Count)  // 1..6 authored layouts
for i in 0..livingMembers.Count-1:
  targetCell = anchor + offsets[i]
  require all formation cells walkable and unoccupied
  if any cell invalid → try fallback offsets profile OR shift anchor (log warning)
  GridMover.InitializeAtGridAnchor(livingMembers[i], targetCell)
parent all under PartyContainer
Snap formation history / camera to leader
```

**Safe zone (generation only):** Chebyshev **5** (§7.1) applies when **first creating** that floor instance — population passes must not place enemies, hazards, or traps within Chebyshev 5 of **any** formation spawn cell. **Returning** to a persisted floor does **not** re-run population or safe-zone placement.

**Spawn anchor summary (locked):**

| Entry type | Where party appears | Near `playerStart`? |
|------------|---------------------|---------------------|
| **First visit to floor** | Formation around **`playerStart`** from layout stamp | **Yes** — start is the authored entry point |
| **Arrival via portal from another floor** | Formation around **portal arrival cell** on this floor (§8) | **Usually no** — can be map edge or forest end |
| **Any cross-floor transition (including return)** | Formation around **arrival anchor** for the **`portalLinkId` used** (§8.8) | **No** — not `playerStart` unless that portal's arrival is authored there |

**Player confirmation (Q6):** On **first visit**, the party spawns **at/near the floor starting point** (`playerStart`), with **Chebyshev 5** safe zone for initial population only. **Portal travel** never uses last-known position on the floor.

Author **6 preset offset sets** (or one profile with rows for count 1,2,3,4,5,6) — e.g. line south of anchor, 2×3 block, V-shape — document in data, not hard-coded in C#.

### R6.6 — Doors (per-floor policy)

Doors are **not** global. `DungeonFloorDefinition.doorPolicy`:

| Policy | Procedural door pass | Vault doors | Stamp doors |
|--------|----------------------|-------------|-------------|
| **`None`** | No procedural/stamp doors | **Yes** — vault stamp may still place door tiles (explicit override) | No |
| **`VaultOnly`** | No | Yes — only doors inside stamped vaults | No |
| **`StampOnly`** | No | Yes | Yes — doors only where pre-baked stamp marks door cells |
| **`Procedural`** | Yes (post-v0 layout) | Yes | Yes |

**Locked (user):** Some floors have **no doors anywhere** except inside **vaults** → use **`VaultOnly`** or **`None`** + vaults that include door tiles. Other floors may use stamp or future procedural doors.

**Implementation:** `DoorPlacementPhase` checks `doorPolicy` before registering `DoorInstance`. Vault stamp includes door layer cells → `DoorService.Register` during vault phase regardless of policy if policy is `VaultOnly` or includes vault doors.

---

## 7. Population algorithms (enemies, hazards, traps, items, interactables)

Common pattern for all passes:

```text
candidateCells = all walkable floor cells from generated topology
candidateCells = filter not in playerSafeZone
candidateCells = filter not occupied / not reserved (stairs, vault anchors, shop cells)
shuffle candidateCells with seeded RNG
place N entities using table weights until budget exhausted or cells exhausted
```

### R7.1 — Player safe zone (locked)

**Metric:** **Chebyshev** distance (8-connected “square” radius):

```text
chebyshev(a, b) = max(|ax - bx|, |ay - by|)
cell in safeZone(center) iff chebyshev(center, cell) <= playerSafeRadius
```

**Default:** `playerSafeRadius = 5` on `DungeonFloorDefinition` for **enemies, hazards, and traps** (single field — same radius for all three unless a future per-pass override is added).

**Centers for safe zone checks:**

| Pass | Center set |
|------|------------|
| First-time spawn | Every cell in **formation spawn** (§6.5), not only anchor |
| Portal arrival | Formation cells around **arrival anchor** |
| Random population | Also exclude cells within Chebyshev 5 of **`playerStart`** marker and **portal cells** once placed |

**Also exclude:** vault footprint cells (pre-placement), reserved portal sites, altar/lever cells from stamps.

### R7.2 — Enemies (initial generation)

| Step | Rule |
|------|------|
| 1 | Read `enemyPopulation`: `{ species/prefab, weight, minCount, maxCount }`. |
| 2 | Roll total count `N` in `[minCount, maxCount]` or fixed `targetCount`. |
| 3 | For each spawn: pick random **walkable** cell outside safe zone; verify **1×1** (or footprint) fits via `EnemySpawnPlacementResolver`-style checks. |
| 4 | `Instantiate` under **`EnemyContainer`**; `GridMover.InitializeAtGridAnchor`. |
| 5 | Failure to place → try next candidate; log if underfilled. |

**Reuse:** Extend `EnemySpawnService` with `TrySpawnAtAnchor` / `TrySpawnPopulation` used by gen (not only interactable effects).

**Multi-tile:** Use existing footprint placement rules ([Multi-tile enemies](../Combat/Multi-Tile-Enemy-Requirements.md)).

### R7.3 — Hazards and traps

| Pass | Algorithm |
|------|-----------|
| **Hazards** | Density or count from table; cluster optional (e.g. 2×2 lava pool) via vault preferred; scatter single cells from shuffled candidates. `HazardService.Register(cell, definition)`. |
| **Traps** | Same; respect `TrapPlacement.Floor` vs `Wall`; `TrapService.Register`. Uses same **Chebyshev 5** default as enemies/hazards. |

**Order:** Topology → (doors) → **hazards** → **traps** → items → enemies → interactables (enemies last avoids blocking spawn cells — **TBD**).

### R7.4 — Floor items

| Mode | Description |
|------|-------------|
| **Table-driven** | Weighted `ItemData` / loot entries via chosen delivery (§7.4.1). |
| **Per-floor config** | `floorItemPopulation`: min/max piles, tier filters, no spawn in safe zone. |
| **Vault-only** | Some floors disable random items; items only from vaults. |

#### R7.4.1 — Floor loot model (locked)

**Dynamic dungeon floors use only:**

| System | Content |
|--------|---------|
| **`FloorItemPileService`** | All ground loot: gear, potions, scrolls, mana stones in piles ([Floor item piles](../Inventory/Floor-Item-Pile-Requirements.md)) |
| **`FloorEssenceService`** | All floor essences ([Enemy essence drops](../Essence/Enemy-Essence-Drops-Requirements.md)) |

**Do not** spawn `WorldItem` GameObjects on dynamic floors. `WorldItem` remains **SampleScene / legacy** only until removed.

**Population / vault / enemy loot** call `FloorItemPileService.AddEntry` or `FloorEssenceService.SpawnEssence` — no `WorldItemContainer` on production dungeon hierarchy (§3.3 may omit or leave empty).

**Per-floor persistence:** Pile and essence entries live inside the **`DungeonFloorInstance`** snapshot; when returning to Floor 1, taken items and claimed essences stay gone.

### R7.5 — Interactables (levers, altars, events)

| Source | Mechanism |
|--------|-----------|
| **Population table** | Weighted `InteractableTileDefinition` + optional `InteractableEffect` chains (spawn enemy, XP, door unlock). |
| **Vault** | Fixed cells for altar / lever / shrine. |
| **Altar** | `AltarDefinition` + `AltarBootstrap.Register` or `AdjacentMapInteractableService` from gen. |

**Event on interactable:** Already supported via `InteractableEffect` ScriptableObjects ([Interactables](../Combat/Interactable-Tiles-Requirements.md), [Conditional spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md)). Gen **only places** instances; does not embed new event types in code.

### R7.6 — Doors

See **`doorPolicy`** (§6.6). Procedural doorway placement only when `doorPolicy == Procedural` and layout mode supports it (post-v0).

---

## 8. Portals (per-floor heuristic rules)

Portals connect dungeon floors (and eventually other zones). Placement is **data-driven per floor**, inspired by *Surviving the Game as a Barbarian* — not a single global algorithm.

### R8.1 — Concepts

| Term | Meaning |
|------|--------|
| **`PortalPlacementRule`** | ScriptableObject: heuristic + parameters for one logical portal (or a set of portals). |
| **`portalLinkId`** | Stable string pairing exits across floors (e.g. `main_south` on Floor 1 ↔ `entry_north` on Floor 2). |
| **`PortalInstance`** | Runtime on source floor: portal cell, `targetFloorId`, `portalLinkId`, interactable hook (TBD). |
| **`PortalArrivalBinding`** | Runtime on **destination** floor: `portalLinkId` → **fixed** `arrivalAnchor` cell + formation profile (§8.8). |
| **Arrival anchor** | Primary formation cell when entering a floor **through** a given link — **stable** for the life of that floor instance. |

### R8.2 — Example heuristics (authoring references)

| Source (Barbarian) | JRogue mapping |
|--------------------|----------------|
| **Floor 1: four portals at orthogonal ends of the map** | Rule type **`OrthogonalMapEdgeCount`**: `count = 4`, `edges = {North, South, East, West}`, place portal on walkable cell **inward** from each edge (e.g. 1–3 tiles from border), validated for party approach |
| **Floor 3 → 4: portal at end of a particular forest** | Rule type **`TaggedRegionEdge`**: layout stamp or proc region tag `forest`; pick **deepest** / **farthest-from-start** walkable cell in that region along a trail axis; single portal |

These are **different floors, different rules** — Floor 1 definition lists four `PortalPlacementRule` assets; Floor 3 lists a forest-end rule; Floor 4 lists matching **arrival** rules.

### R8.3 — Extensibility (locked)

- Each `DungeonFloorDefinition` owns its own **`portalRules`** list (zero or many rules).
- **New rule types** are added by implementing new `PortalPlacementRule` ScriptableObject subclasses (or enum + data) **without** changing floor transition code.
- **Per-floor tuning** (counts, insets, target floor ids, link ids) is entirely data — e.g. Floor 1 four-edge layout can be replaced later with forest-end rules on Floor 3.
- v0 ships **`OrthogonalMapEdgeCount`** + **`FixedStampMarker`**; add **`TaggedRegionEdge`** when region tags exist on stamps.

### R8.4 — `PortalPlacementRule` types (extensible)

| Rule type | Inputs | Resolves |
|-----------|--------|----------|
| **`OrthogonalMapEdgeCount`** | `count` (1–4), `insetFromEdge`, `targetFloorId`, `portalLinkId` | Up to N portal cells near map edges |
| **`FixedStampMarker`** | `markerId` on `DungeonLayoutStamp` | Exact cell from stamp (deterministic) |
| **`TaggedRegionEdge`** | `regionTag`, `metric` (maxManhattanFromStart, maxY, etc.) | One cell in tagged region |
| **`NearPlayerStartOpposite`** | `minDistance` | Debug / special floors |
| **`VaultMarker`** | `vaultId` + offset | Portal inside or adjacent to vault |

**Phase order:** Run **`PortalPlacementPhase`** after layout stamp, **before** or **after** population — **locked:** after layout, **before** enemy scatter, so portals are excluded from random spawns and safe-zone logic can treat portal tiles as reserved.

### R8.5 — Validation

- Portal cell must be **walkable**, not in player safe zone (unless design explicitly allows starter-adjacent portal with larger safe zone — avoid for Floor 1 edges).
- For **multi-portal** floors, all portals must be **pairwise separated** by minimum Chebyshev distance (configurable, default 5).
- **`portalLinkId`** must resolve to a rule on the target floor with **`arrivalOffset`** / formation anchor.

### R8.6 — Revisit and persistence

- **Portal cells** are part of the **persisted floor instance** (§1.3) — never re-placed on return.
- Interactable state (portal already used?) — **future**; v0 portals always active.

### R8.7 — v0 stub

- Implement **`OrthogonalMapEdgeCount`** for Floor 1 (four edge portals).
- Floor 2+ portals authored via **`FixedStampMarker`** until forest/region rules exist.
- Portal activation: **`Interact`** command (same family as altar — §8.9); step-on-tile deferred.

### R8.8 — Cross-floor transition spawn (locked — *Barbarian*-style)

When the player uses a portal to move from **Floor A → Floor B**, party placement is **fully determined by which portal was used** — not by where the party stood on A, and **not** by last visit position on B.

#### Rules (locked)

| # | Rule |
|---|------|
| **P1** | Each portal on A references a **`portalLinkId`** and **`targetFloorId`**. |
| **P2** | On **first creation** of Floor B, each inbound link resolves to a **`PortalArrivalBinding`**: `portalLinkId` → `arrivalAnchor` (`Vector3Int`) + optional formation offsets. |
| **P3** | **`arrivalAnchor` is immutable** for that floor instance (same as portal tiles — §8.6). Using the **same** portal again (today, tomorrow, after clearing the floor) → **same** arrival anchor. |
| **P4** | **Bidirectional travel:** Floor B's paired portal back to A uses A's binding for the reverse link — two bindings, one per direction. |
| **P5** | Spawn runs **`PartySpawnPhase`** with `anchor = arrivalAnchor` for the link (§6.5). |

#### *Surviving the Game as a Barbarian* alignment

| Barbarian behavior | JRogue |
|--------------------|--------|
| Floor 1 has portals at the **four sides**; each leads somewhere specific | `OrthogonalMapEdgeCount` + distinct `portalLinkId` per edge |
| Entering a floor through a portal puts you at a **predictable** place on the new floor | `PortalArrivalBinding.arrivalAnchor` |
| Re-entering via the same portal is **consistent** (muscle memory, routing) | Bindings persisted on `DungeonFloorInstance` |
| Third floor forest exit → fourth floor at a **fixed** kind of location | `TaggedRegionEdge` + authored arrival on Floor 4 for that link |

#### Placement of arrival anchor (authoring)

At **`PortalPlacementPhase`** on the **destination** floor (or when the **source** portal is first wired):

```text
Typical: arrivalAnchor = walkable cell adjacent to the inbound portal tile on B
         (e.g. one step south of the portal sprite on B's north entrance)
Authoring override: PortalPlacementRule.arrivalOffset from portal cell
Stamp: FixedStampMarker "arrival_floor02_from_floor01_south"
```

Store in `DungeonFloorInstance.portalArrivals: Dictionary<string, PortalArrivalBinding>` keyed by `portalLinkId`.

#### Blocked arrival (repeat visits)

If on a **return** visit one or more formation cells are **blocked** (enemy, hazard, closed door, party member corpse — TBD list):

1. Try **same** `arrivalAnchor` + default formation offsets.
2. If invalid, search **nearest walkable** cells preserving formation shape (BFS from anchor, Chebyshev ≤ 2 v0).
3. If still invalid, log warning and pick closest valid anchor — **do not** change the stored binding; only the **one-time** spawn position shifts.

**Never** fall back to `playerStart` or "last position on floor" for portal transitions.

#### Transition flow (reference)

```text
Player activates portal P on Floor A (linkId = L, target = B)
  → Park A
  → Activate B (existing or first visit)
  → binding = B.GetArrivalBinding(L)
  → PartySpawnPhase(anchor = binding.arrivalAnchor)
```

### R8.9 — Portal activation UX (v0 locked)

| Rule | Detail |
|------|--------|
| **Input** | **`Interact`** (`GameControls`) when orthogonally adjacent to portal cell (same adjacency rules as altar — §3 in [Altar doc](Altar-And-Map-Interact-Requirements.md)). |
| **v0a** | Single portal on test floor may use a simple `PortalInteractable` implementing `IAdjacentMapInteractable` or dedicated bump tile — minimum: adjacent Interact opens transition. |
| **Turn** | Portal transition consumes a player action (same as moving between floors — TBD exact turn cost; default **yes**, matches significant travel). |
| **Deferred** | Step-on-portal without Interact; multi-portal picker when several adjacent (use picker pattern from altar if needed). |

---

## 9. Vaults (DCSS-style hand-crafted chunks)

### R9.1 — Purpose

Inject **authored** pockets (altars, enemy ambush, item haul, hazard room) into procedural floors.

### R9.2 — `DungeonVaultDefinition` (new)

| Field | Notes |
|-------|-------|
| `vaultId` | e.g. `vault_mana_altar_shrine` |
| `size` | WxH in cells |
| `tileStamp` | Relative floor/wall/door tiles |
| `entityPlacements` | Enemies, hazards, traps, items, altars, interactables (relative coords) |
| `placementTags` | e.g. `in_room`, `near_corridor`, `dead_end` |
| `weight` | Selection in vault pass |

### R9.3 — Placement algorithm (v0)

```text
for each vault slot in floorDefinition.vaults (weighted):
  pick random valid anchor (room floor rect fits vault size, not overlapping player safe zone)
  stamp tiles + register entities into services / containers
  mark cells consumed
```

**Failure:** Skip vault; do not fail entire floor gen.

---

## 10. Extensibility — generation pipeline

### R10.1 — Phase interface

```csharp
// Illustrative — names TBD
public interface IDungeonGenerationPhase
{
    void Execute(DungeonGenerationContext ctx);
}
```

| Phase (ordered v0) | Responsibility |
|------------------|----------------|
| `ClearPreviousFloorPhase` | Destroy containers, clear tilemaps, reset services. |
| `LayoutStampPhase` | Pre-baked stamp → floor/wall (§2.4). |
| `VaultPlacementPhase` | Stamp vaults (may include doors per §6.6). |
| `PortalPlacementPhase` | Portal heuristics (§8). |
| `DoorPlacementPhase` | Per `doorPolicy` (§6.6). |
| `LightingInitPhase` | Ambient max + optional day/night (§6.4). |
| `HazardPopulationPhase` | Chebyshev 5 safe zone (§7.1). |
| `TrapPopulationPhase` | |
| `FloorItemPopulationPhase` | Piles preferred (§7.4.1). |
| `EnemyPopulationPhase` | |
| `InteractablePopulationPhase` | |
| `PartySpawnPhase` | Formation 1–6 cells (§6.5). |
| `FinalizePhase` | `VisibilityManager.Refresh`, `CombatThreatCoordinator` eval. |

**Adding features:** New ScriptableObject phase or new class registered on `DungeonGeneratorProfile` — no change to `DungeonFloorRuntime` beyond phase list.

### R10.2 — Context object

`DungeonGenerationContext` holds: `DungeonFloorDefinition`, `RNG`, `Tilemap` refs, `playerStart`, mutable `occupiedCells`, references to `EnemyContainer`, etc.

---

## 11. Existing code — refactor & architecture notes

### R11.1 — Map / grid (required for gen)

| Area | Issue | Direction |
|------|-------|-----------|
| `MapManager` | Assumes pre-painted SampleScene tilemaps | Add **`SetCellFloor` / `SetCellWall` / `ClearAllTiles`** API; optional `ResizeBounds(width, height)`. |
| `GridManager` | Sized to existing map | Sync bounds after gen; clear occupancy on regen. |
| `DungeonUrban.prefab` | Unused tilemap prefab | Reference for tile assets or retire. |

### R11.2 — Spawn & containers

| Area | Issue | Direction |
|------|-------|-----------|
| `EnemySpawnService` | No parent transform; static | Add optional `Transform spawnParent` (default `EnemyContainer`). |
| `FloorItemPileService` / `FloorEssenceService` | View root created in `Awake` | Point `_viewRoot` at `DynamicViews` or container from `DungeonFloorRuntime`. |
| `WorldItem` | Legacy scene-placed items | Gen spawns under `WorldItemContainer`; migrate to piles over time. |

### R11.3 — Bootstraps vs generation

| Area | Issue | Direction |
|------|-------|-----------|
| `*Bootstrap` MonoBehaviours | `Start()` applies inspector/placement sets | Gen calls service `Register` directly; bootstraps **disabled** or fed runtime arrays from `DungeonFloorRuntime`. |
| `SampleSceneHazardPlacements` | Hard-coded cells | Delete from prod path; keep in SampleScene only. |
| `SampleSceneInteractablePlacements` | Same | Same. |

### R11.4 — Lighting

| Area | Issue | Direction |
|------|-------|-----------|
| `LightingScenarioController` | QA phase toggles | Not in dungeon scene; gen uses `LightingBootstrap` + floor definition only. |
| `LightingQaPlacementResolver` | QA | Replace with data-driven placements from gen/vaults. |

### R11.5 — Party / run flow

| Area | Issue | Direction |
|------|-------|-----------|
| SampleScene party | Hand-placed prefabs | `PartySpawnPhase` instantiates from **run roster** prefabs into `PartyContainer`. |
| `PartyManager.EnsureComponent<>` | Good pattern | Keep; clarify DDOL vs per-floor scene. |
| `BowKitSampleSceneBootstrap` | QA gear | Strip from production spawn pipeline. |

### R11.6 — Input / gameplay gaps (related)

| Area | Issue | Direction |
|------|-------|-----------|
| Ally swap + essence | Swap bypasses `EssenceMoveGate` | Fix when floor pickup is live ([Essence pickup](../Essence/Sudden-Strength-Skeleton-Drop-And-Floor-Pickup-Requirements.md)). |
| `Enemy.prefab` | Empty `attackProfiles` | Set `AdjacentSingle` on 1×1 population spawns. |

### R11.7 — Namespace organization (granular)

| Current | Suggested |
|---------|-----------|
| `JRogue.Hazards` + `JRogue.Traps` + `JRogue.Interactables` | Keep; add **`JRogue.World.Generation`** for floor gen, vaults, phases. |
| `JRogue.Manager.Floor` | Floor economy — OK. |
| `JRogue.Testing` | Move `EssenceTestHarness` here; asmdef optional. |
| `JRogue.Combat` (`BowKitSampleSceneBootstrap`) | Rename/move to `JRogue.Testing.SampleScene` or delete from prod. |
| Global `VisibilityManager` (no namespace) | Align to `JRogue.Manager.Visibility` when touching file. |

### R11.8 — Patterns

| Pattern | Recommendation |
|---------|----------------|
| Singleton `Instance` | Acceptable v0; `DungeonFloorRuntime` holds serialized refs to reduce `FindAnyObjectByType`. |
| Static services (`EnemySpawnService`) | Inject `DungeonGenerationContext` or static `CurrentFloor` for container parent. |
| ScriptableObject tables | DCSS-like data; avoid hard-coded SampleScene coordinates. |

---

## 12. Acceptance criteria

### v0a (vertical slice)

| # | Criterion |
|---|-----------|
| **AC-a1** | `RunBootstrap` (DDOL) + `DungeonFloorInstanceManager` with **park/activate** between two floor ids. |
| **AC-a2** | **Floor 1** stamp **30×30** paints tilemaps; `playerStart` formation spawn; Chebyshev **5** on first populate. |
| **AC-a3** | **One** portal link Floor 1 → Floor 2 (minimal Floor 2 stamp acceptable, e.g. 20×20). |
| **AC-a4** | Portal transition uses **fixed arrival anchor** per `portalLinkId` (§8.8). |
| **AC-a5** | Return Floor 1 → Floor 2 → Floor 1: Floor 1 **unchanged** (enemy death, taken loot persist). |
| **AC-a6** | `DungeonFloorTest` scene + **Generate Test Floor**; **SampleScene** still plays unchanged. |
| **AC-a7** | Loot on dynamic floors: **`FloorItemPileService`** + **`FloorEssenceService`** only. |

### v0b (complete v0)

| # | Criterion |
|---|-----------|
| **AC-b1** | Production **`DungeonFloor.unity`** hierarchy per §3–5 (no §4.1 QA scripts). |
| **AC-b2** | Floor 1: **four orthogonal edge portals**; Floor 2: authored stamp + portal rules + `doorPolicy`. |
| **AC-b3** | Population: enemies, hazards, traps, items, interactables per tables (§7). |
| **AC-b4** | At least **one vault** placed on a floor (§9). |
| **AC-b5** | Service state **isolates per floor** on switch (no trap/hazard bleed). |
| **AC-b6** | `MapManager.SetActiveFloor` + programmatic tile paint API (§11.1). |
| **AC-b7** | `EnemyContainer` parenting; max ambient lighting bootstrap (§6.3). |
| **AC-b8** | `ExitDungeon()` stub destroys all floor instances (hook only; hub out of scope). |

**Full v0** = all **AC-a*** and **AC-b*** pass.

---

## 13. Locked decisions (review)

| # | Decision |
|---|----------|
| 1 | **DDOL party + multiple parked floor instances**; switch without teardown; **exit dungeon** destroys all (§1.2–1.4). |
| 1b | Default **`PersistFullFloorState`** for dungeon floors. |
| 2 | **Chebyshev 5** safe radius — enemies, hazards, traps (§7.1). |
| 3 | **Day/night** elaborated in §6.4; v0 default constant max. |
| 4 | **`doorPolicy` per floor**; vault-only doors allowed (§6.6). |
| 5 | **v0 pre-baked layout**; procedural roadmap in §2.4. |
| 6 | **Multi-cell formation spawn** for 1–6 members (§6.5). |
| 7 | **Floor piles + essences only** — no `WorldItem` on dynamic floors (§7.4.1). |
| 7b | **PortalPlacementRule** per floor, new types addable in data (§8.3). |
| 8 | **Portal heuristics** per floor via `PortalPlacementRule` (§8). |
| 8b | **Portal transition spawn** = fixed **arrival anchor** per `portalLinkId`, Barbarian-style (§8.8). |
| 9 | **SampleScene** = fixed dungeon; **DungeonFloorTest** = Generate button (intro). |
| 10 | **v0 = v0a then v0b** (§16). |
| 11 | Run scene **pattern B**; park = **deactivate** floor root (§1.4). |
| 12 | Portal use **`Interact`** (§8.9). |
| 13 | **`doorPolicy: None`** still allows doors **inside vault stamps** (§6.6). |

### Remaining TBD (non-blocking)

- Floor 2 exact portal rules / `doorPolicy` asset values (author during v0b).
- Blocked arrival tile list for formation fallback (§8.8).
- `ExitDungeon` hub destination.

---

## 14. Implementation checklist

### v0a — vertical slice (ship first)

- [ ] `RunBootstrap` scene/prefab (DDOL): Party, UI, Input, `DungeonFloorInstanceManager`
- [ ] `Floors/dungeon_floor_01` + `Floors/dungeon_floor_02` child hierarchy (pattern B)
- [ ] `DungeonFloorInstance` + park/activate (`SetActive`)
- [ ] `DungeonLayoutStamp` Floor 1 (30×30) + minimal Floor 2 (20×20)
- [ ] `LayoutStampPhase` + `MapManager` tile paint API (minimal)
- [ ] `DungeonFloorDefinition` assets (floor_01, floor_02)
- [ ] `PartyFormationSpawnProfile` (counts 1–6)
- [ ] `PartySpawnPhase` at `playerStart` / portal arrival
- [ ] Population pass: enemies only (Chebyshev 5) — hazards/traps optional defer to v0b
- [ ] One `portalLinkId` pair + `PortalArrivalBinding` + **Interact** to transition (§8.9)
- [ ] `FloorItemPileService` / `FloorEssenceService` on run or floor root
- [ ] `DungeonFloorTest.unity` + Generate Test Floor button
- [ ] Manual test: AC-a1–AC-a7

### v0b — complete v0 (after v0a)

- [ ] `DungeonFloor.unity` production hierarchy (§3, §5)
- [ ] QA exclusion audit (§4.1)
- [ ] `DungeonFloorRuntime` + full phase pipeline (§10)
- [ ] Per-floor service export/import on switch
- [ ] `MapManager.SetActiveFloor` polish
- [ ] Floor 1: four-edge `OrthogonalMapEdgeCount` portals
- [ ] Floor 2: full stamp, portal rules, population tables
- [ ] Hazard, trap, item, interactable population passes
- [ ] One `DungeonVaultDefinition` + vault phase
- [ ] `doorPolicy` per floor
- [ ] `LightingBootstrap` max ambient
- [ ] `EnemyContainer` on all spawns; `AdjacentSingle` on 1×1 enemies
- [ ] `ExitDungeon()` destroys all instances (stub)
- [ ] Manual test: all AC-b* + regression SampleScene

---

## 16. v0 implementation plan (v0a + v0b)

### 16.1 — Dependency graph

```text
v0a: RunBootstrap → InstanceManager → Stamp → Populate → Portal link → Test scene
                                      ↓
v0b:  Production scene + full phases + vault + 4 portals + service isolation
```

**Rule:** Do not start v0b production scene until **AC-a5** (round-trip persistence) passes in `DungeonFloorTest`.

### 16.2 — v0a scope (what to build)

| System | v0a deliverable |
|--------|-----------------|
| **Scenes** | `RunBootstrap` + `DungeonFloorTest` only (production `DungeonFloor.unity` is v0b) |
| **Floors** | 2× `DungeonFloorInstance` (floor_01, floor_02) |
| **Layout** | Pre-baked stamps only |
| **Population** | Enemies from table; safe zone Chebyshev 5 |
| **Portals** | Minimum 1 link (e.g. south F1 → arrival on F2); Interact to travel |
| **Persistence** | Park F1 when on F2; return to F1 with state intact |
| **Party** | DDOL; formation spawn |
| **Loot** | Piles + essences (wire services; loot table can be minimal) |
| **Deferred to v0b** | Full hierarchy, 4 edge portals, vault, hazards/traps full pass, fog per-floor, exit dungeon hub |

### 16.3 — v0b scope (what completes the parent doc)

Everything in §3–§11 not required for v0a: production scene, full portal set for Floor 1, all population passes, vault injection, `doorPolicy`, lighting bootstrap on prod scene, per-floor trap/hazard/fog isolation, editor-authored Floor 2 content.

### 16.4 — Suggested code layout (`JRogue.World.Generation`)

| Type | Responsibility |
|------|----------------|
| `DungeonRunState` | Run seed, active `floorId`, visited floors |
| `DungeonFloorInstanceManager` | Create / park / activate / destroy all |
| `DungeonFloorInstance` | Floor root GO, tilemaps, bindings, snapshot of service data |
| `DungeonFloorRuntime` | Runs phase list on first visit only |
| `IDungeonGenerationPhase` | Pluggable passes (§10) |
| `DungeonLayoutStamp` | ScriptableObject grid |
| `PortalPlacementRule` | ScriptableObject heuristics |
| `PortalTransitionController` | Interact → park → activate → spawn at binding |

### 16.5 — v0a “done” demo

1. Play `DungeonFloorTest` → Generate → party on Floor 1 `playerStart`.
2. Walk to portal → Interact → Floor 2 at fixed arrival.
3. Kill an enemy on Floor 2, leave loot on ground.
4. Return via portal → Floor 1 exactly as left before step 2.
5. Return to Floor 2 → enemy still dead, loot still there.

---

## 17. v1 preview (post-v0)

**Not specified for implementation yet.** Direction from §2.4:

| v1 core | Replace or supplement `LayoutStampPhase` with **`RoomCorridorGenerationPhase`** (`layoutMode = ProceduralRoomCorridor`). |
|---------|------------------------------------------------------------------------------------------------------------------|
| **Unchanged from v0** | Multi-floor park/persist, portal bindings, population tables, vaults, Chebyshev safe zone on **first** populate |
| **v1 must add** | Connectivity validation (reachable floors), portal site reservation on generated maps, `DungeonGeneratorProfile` (room count/size) |
| **v1.1+** | Cave generator, layout weights (v2), themed regions (v5) — see [Dungeon zone layout](Dungeon-Zone-Layout-Requirements.md) for macro **zone** composition (habitats, jigsaw, selection rules). |

Author a separate **`Dynamic-Dungeon-Floor-Generation-v1-Requirements.md`** when v0b ships.

---

## 18. Traceability

| Request | Section |
|---------|---------|
| Scene per floor vs reuse vs dynamic | §1 |
| Multi-floor simultaneous / park-switch | §1.3–1.4 |
| DDOL party / exit dungeon teardown | §1.2–1.3 |
| Professional GameObject organization | §3, §5 |
| QA scripts to exclude | §4 |
| Full GO + script list | §5 |
| Floor dimensions 30×30 / 20×20 | §6, §2.4 |
| Max ambient + day/night | §6.3–6.4 |
| Pre-baked v0 + future proc | §2.4 |
| Vaults | §9 |
| Portals / Barbarian heuristics | §8 |
| Portal arrival spawn (fixed per link) | §8.8 |
| Chebyshev 5 safe radius | §7.1 |
| Doors per floor / vault-only | §6.6 |
| Formation spawn 1–6 | §6.5 |
| WorldItem vs piles | §7.4.1 |
| Floor items + interactables | §7.4–7.5 |
| Extensibility | §10 |
| EnemyContainer / ItemContainer | §3.3 |
| SampleScene vs test scene | intro, §13 |
| v0a / v0b plan | §16 |
| v1 preview | §17 |
| Refactors + namespaces | §11 |
