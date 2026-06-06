# Dungeon Zone Population — Enemies, Hazards, Traps, Items & Interactables

**Purpose:** Specify **per-zone population** for `ZoneComposite` floors — which enemies, hazards, traps, floor items, and interactables spawn **inside each habitat** after [zone layout](Dungeon-Zone-Layout-Requirements.md) resolves the `ZoneCellMap`. Extends [Dynamic dungeon floor generation §7](Dynamic-Dungeon-Floor-Generation-Requirements.md) (floor-wide tables for `PreBakedStamp` v0).

**Status:** Requirements draft — **not implemented**.

**Depends on:** [Dungeon zone layout](Dungeon-Zone-Layout-Requirements.md) (`ZoneCellMap`, `DungeonZoneDefinition`, `populationProfile` hook), [Dynamic dungeon floor generation](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`DungeonGenerationContext`, population phases, Chebyshev safe zone §7.1, persistence §1.3), [Conditional enemy spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md), [Floor item piles](../Inventory/Floor-Item-Pile-Requirements.md), [Enemy essence drops](../Essence/Enemy-Essence-Drops-Requirements.md), [Traps](../Combat/Traps-Requirements.md), [Environmental hazards](../Combat/Environmental-Hazards-Requirements.md), [Interactable tiles](../Combat/Interactable-Tiles-Requirements.md), vault entity placements ([§9 parent](Dynamic-Dungeon-Floor-Generation-Requirements.md)).

**Related:** [Multi-tile enemies](../Combat/Multi-Tile-Enemy-Requirements.md). [Improved illumination](Improved-Illumination-Requirements.md) (zone ambient affects spawn **presentation**, not population gates v1).

**Explicitly out of scope (v1):** Experience-budget monster packs (DCSS-style); shop NPCs inside zones; dynamic repopulation on revisit; spawn tables keyed by dungeon **depth** beyond `floorId`; save/load population state across sessions (in-memory floor instance only, same as v0).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Desert spawns desert things** — population candidates filtered by **`zoneId`** on each cell. |
| **G2** | **Reuse v0 population machinery** — extend existing phases (`EnemyPopulationPhase`, etc.), do not fork parallel systems. |
| **G3** | **Author on zone** — `DungeonZonePopulationProfile` on `DungeonZoneDefinition`; floor-wide tables remain **fallback** for non-zone floors. |
| **G4** | **Same persistence rules** — spawned entities live in `DungeonFloorInstance` snapshot; revisit unchanged ([parent §1.3](Dynamic-Dungeon-Floor-Generation-Requirements.md)). |
| **G5** | **Respect global exclusions** — player safe zone, portal cells, vault footprints, occupied cells ([parent §7.1](Dynamic-Dungeon-Floor-Generation-Requirements.md)). |
| **G6** | **Composable with vaults** — vault-placed entities **win**; zone scatter skips vault cells. |
| **G7** | **Optional zone silence** — a zone may define **zero** enemies (empty desert) while still having hazards or items. |

---

## 2. Glossary

| Term | Meaning |
|------|---------|
| **Zone population profile** | ScriptableObject (or embedded) listing tables for one **`zoneId`**. |
| **Floor population** | Existing arrays on `DungeonFloorDefinition` — used when **`layoutMode != ZoneComposite`** or as **fallback** when zone profile omits a category. |
| **Population category** | Enemies, hazards, traps, floor items, interactables. |
| **Candidate cell** | Walkable floor cell passing global + zone filters. |
| **Zone filter** | `ZoneCellMap[cell] == targetZoneId`. |
| **Scatter pass** | Table-driven random placement (existing v0 algorithm). |

---

## 3. Current state vs gap

| Area | v0 today | Gap |
|------|----------|-----|
| **Enemies** | `EnemyPopulationPhase` reads floor `enemyPopulation` | No `zoneId` filter |
| **Hazards / traps / items / interactables** | Same pattern per floor | Same |
| **Candidates** | `PopulationPlacementUtility.CollectFloorCandidates` | Whole floor walkable set |
| **Zone map** | Not implemented | Required input to population |

---

## 4. Authoring model

### 4.1 — `DungeonZonePopulationProfile` (ScriptableObject)

**Menu:** `JRogue/World/Dungeon Zone Population Profile`

Referenced from **`DungeonZoneDefinition.populationProfile`**.

| Section | Field shape | Notes |
|---------|-------------|-------|
| **Enemies** | `ZoneEnemyPopulationEntry[]` | Extends floor entry + optional **`weight`** per species row |
| **Hazards** | `ZoneHazardPopulationEntry[]` | Same as floor + zone scope |
| **Traps** | `ZoneTrapPopulationEntry[]` | |
| **Floor items** | `ZoneFloorItemPopulationEntry[]` | Piles via `FloorItemPileService` |
| **Interactables** | `ZoneInteractablePopulationEntry[]` | Levers, altars (non-vault) |
| **Essences** | optional `ZoneEssencePopulationEntry[]` | Floor essences in zone (future density) |

Each entry mirrors existing structs on `DungeonFloorDefinition` (`EnemyPopulationEntry`, etc.) with additions:

| Added field | Purpose |
|-------------|---------|
| **`minCount` / `maxCount`** | Rolled per **zone instance** on the floor (not per floor total) |
| **`weight`** | When multiple enemy rows in one zone, relative pick for each spawn slot (optional v1) |
| **`densityMode`** | `ScatterCount` (default) or `DensityPer100Tiles` (v1.1) |
| **`requiresTag`** | Spawn only if zone def or layout piece has tag (e.g. `outdoor`) |
| **`forbiddenNearEdge`** | Min Chebyshev distance from zone **AABB** edge (keep center spawns) |

**Locked v1:** **`minCount` / `maxCount`** apply to **each placed zone instance** of that `zoneId`. If `desert` appears once on Floor 1, desert table rolls once. If jigsaw places two `cave` pieces sharing same `zoneId`, roll **per piece instance** (context carries `zoneInstanceId`).

### 4.2 — Floor-level fallback

| `layoutMode` | Population source |
|--------------|-------------------|
| **`PreBakedStamp`** | Floor arrays only (unchanged v0) |
| **`ZoneComposite`** | For each category: **zone profile** if present; else floor array row matching **`zoneId`** wildcard or global floor fallback |

Optional on `DungeonFloorDefinition`:

| Field | Purpose |
|-------|---------|
| **`useFloorPopulationAsFallback`** | **true** (default) — zones without a profile section inherit floor table for that category |
| **`zonePopulationOverrides`** | Rare floor-specific patches without editing shared zone defs |

### 4.3 — Zone silence

If a zone profile has **empty** enemy list **and** fallback disabled → **no scatter enemies** in that zone. Vault enemies still allowed.

---

## 5. Candidate selection (locked)

### 5.1 — Algorithm

```text
CollectFloorCandidates(map, context)           // existing global rules
  → exclude safe zone, portals, vault footprints, occupied

For each active zoneInstance on floor:
  candidates[zoneInstanceId] = filter cells where:
    ZoneCellMap[cell] == zoneId
    AND cell in global candidates

For each population entry on that zone's profile:
  N = roll minCount..maxCount (seeded)
  shuffle candidates[zoneInstanceId]
  place up to N entities using existing TrySpawn / Register APIs
```

### 5.2 — Global exclusions (unchanged)

From [parent §7.1](Dynamic-Dungeon-Floor-Generation-Requirements.md):

- Chebyshev **`playerSafeRadius`** around formation / `playerStart`
- Portal reservation cells
- Vault footprint (pre-marked occupied during vault phase)
- Non-walkable cells

Zone population **never** bypasses safe zone.

### 5.3 — Boundary interaction

| Zone boundary kind | Population impact |
|--------------------|-------------------|
| **`Open`** | Candidates include interface cells; **`zoneId`** still determines table (desert side vs dungeon side) |
| **`Wall`** | Non-walkable — excluded automatically |
| **`Corridor`** | Walkable throat cells belong to **one** zone id (locked: **host piece’s zone** owns corridor cells) |

Population does **not** place enemies **on** wall interface cells.

---

## 6. Per-category rules

### 6.1 — Enemies

| Rule | Detail |
|------|--------|
| **Phase** | Extend **`EnemyPopulationPhase`** (or **`ZoneEnemyPopulationPhase`** wrapper that iterates zone instances). |
| **Service** | `EnemySpawnService.TrySpawn` — unchanged |
| **Order** | After vaults, portals, hazards (keep [parent §7.3](Dynamic-Dungeon-Floor-Generation-Requirements.md) order unless zone doc supersedes) |
| **Underfill** | Log warning; floor still generates |

**Example — Floor 1 desert zone:**

| Species | min | max |
|---------|-----|-----|
| Scorpion (TBD) | 2 | 5 |
| Desert bandit (TBD) | 0 | 2 |

**Example — dungeon center:**

| Species | min | max |
|---------|-----|-----|
| Skeleton | 4 | 6 |

### 6.2 — Hazards

| Rule | Detail |
|------|--------|
| **Phase** | Extend **`HazardPopulationPhase`** |
| **Clustering** | Optional **`clusterSize`** on zone entry (2×2 lava in `volcanic` zone) |
| **Outdoor** | `requiresTag: outdoor` for poison gas in `witch_forest` only |

### 6.3 — Traps

| Rule | Detail |
|------|--------|
| **Phase** | Extend **`TrapPopulationPhase`** |
| **Wall vs floor** | Respect `TrapDefinition.placement` — filter candidates by cell type |
| **Density** | Higher trap counts in `dungeon` vs `Open` deserts (authoring) |

### 6.4 — Floor items

| Rule | Detail |
|------|--------|
| **Delivery** | **`FloorItemPileService`** only ([parent §7.4.1](Dynamic-Dungeon-Floor-Generation-Requirements.md)) |
| **Phase** | Extend **`FloorItemPopulationPhase`** |
| **Example** | Handheld torch piles **only** in `dungeon` zone; cactus juice potion in `desert` (future content) |

### 6.5 — Interactables

| Rule | Detail |
|------|--------|
| **Phase** | Extend **`InteractablePopulationPhase`** |
| **Vault overlap** | Skip cells stamped by vault interactables |
| **Example** | Random levers only in `dungeon`; none in `snow` |

### 6.6 — Essences (optional v1.1)

Floor essences scattered by zone using `FloorEssenceService` — same candidate filter. Defer if v0 essences remain kill-linked only.

---

## 7. Seeding

```text
populationRng = Derive(runSeed, floorId, "Population")
perZoneRng    = Derive(runSeed, floorId, "Population", zoneInstanceId, category)
```

Same run + floor → identical scatter positions for a given zone instance.

---

## 8. Pipeline integration

After **`ZoneBoundaryPhase`**, before or after vaults per category:

| Order | Phase | Zone-aware |
|-------|-------|------------|
| … | `VaultPlacementPhase` | Vault entities use embedded placements |
| … | `PortalPlacementPhase` | — |
| … | `LightingInitPhase` | — |
| … | `DoorPlacementPhase` | — |
| … | **`HazardPopulationPhase`** | **Yes** |
| … | **`TrapPopulationPhase`** | **Yes** |
| … | **`FloorItemPopulationPhase`** | **Yes** |
| … | **`InteractablePopulationPhase`** | **Yes** |
| … | **`EnemyPopulationPhase`** | **Yes** (last among scatter — [parent §7.3](Dynamic-Dungeon-Floor-Generation-Requirements.md)) |

**Context additions:**

| Field | Purpose |
|-------|---------|
| **`ZoneInstances`** | `{ zoneInstanceId, zoneId, bounds, pieceId }` |
| **`GetCandidatesForZone(instanceId)`** | Cached list for phases |

---

## 9. Persistence

| Rule | Detail |
|------|--------|
| **First visit** | All scatter spawns registered on instance; snapshot via existing `DungeonFloorServiceBinder` |
| **Return visit** | **No** repopulation; dead enemies stay dead |
| **Zone map** | Frozen with layout — population does not re-derive zone ids |

---

## 10. Worked examples

### 10.1 — Floor 1 (`dungeon` + optional `desert` / `snow`)

| Zone | Enemies | Hazards | Items |
|------|---------|---------|-------|
| `dungeon` | Skeletons 4–6 | — | Handheld torch 0–1 pile |
| `desert` | Scorpion 2–4 (if zone rolled) | — | — |
| `snow` | Ice beetle 1–3 (if zone rolled) | Slip hazard (future) | — |

Floor-wide **`enemyPopulation`** on `Floor_dungeon_floor_01` becomes **fallback only** when `layoutMode == ZoneComposite`.

### 10.2 — Barbarian Floor 3 analogue

| Zone | Population character |
|------|----------------------|
| `orc_castle` | High orc count, traps, boss vault **additional** |
| `witch_forest` | Poisons, witches, few melee |
| `mountain` | Yeti, choke traps, low item density |

---

## 11. Implementation plan

### Phase P0 — Context + filter

- [ ] `ZoneInstances` on `DungeonGenerationContext`
- [ ] `PopulationPlacementUtility.CollectZoneCandidates(map, context, zoneInstanceId)`
- [ ] Unit tests: cells filtered by `ZoneCellMap`; safe zone still excluded

### Phase P1 — Enemies + items

- [ ] `DungeonZonePopulationProfile` asset type
- [ ] Wire `DungeonZoneDefinition.populationProfile`
- [ ] `EnemyPopulationPhase` iterates zone instances when `ZoneComposite`
- [ ] `FloorItemPopulationPhase` zone-aware
- [ ] Example profiles for Floor 1 zones

### Phase P2 — Hazards, traps, interactables

- [ ] Remaining phases zone-aware
- [ ] `useFloorPopulationAsFallback` on floor def
- [ ] Debug overlay: spawn counts per zone in gen log

### Phase P3 — Density mode + tags

- [ ] `DensityPer100Tiles`, `requiresTag`, `forbiddenNearEdge`
- [ ] Essence scatter by zone (if needed)

---

## 12. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Skeletons spawn only in cells with `ZoneCellMap == dungeon`. |
| **AC2** | When desert zone not selected for a run, **zero** desert-table spawns occur. |
| **AC3** | Safe zone around player start has **no** zone-table spawns even if start cell’s zone id is `dungeon`. |
| **AC4** | Vault-placed enemy in orc boss vault not duplicated by zone scatter. |
| **AC5** | `PreBakedStamp` floor unchanged — floor arrays only. |
| **AC6** | Revisit floor — killed zone enemies do not respawn. |

---

## 13. Design decisions (locked)

| # | Decision | Rule |
|---|----------|------|
| 1 | Primary authoring | **`DungeonZonePopulationProfile`** on zone def |
| 2 | Floor tables | **Fallback** when `useFloorPopulationAsFallback` and zone section empty |
| 3 | Count scope | Per **zone instance** on the floor |
| 4 | Corridor cells | Owned by **host piece’s `zoneId`** for population |
| 5 | Open boundaries | Do **not** merge population tables — cell zone id picks table |

---

## 14. Traceability

| Need | Section |
|------|---------|
| Zone layout → population handoff | §5, §8 |
| Enemy spawn by habitat | §6.1, §10 |
| Extend v0 phases | §3, §6, §11 |
| Floor 1 content split | §10.1 |
| Barbarian Floor 3 flavour | §10.2 |

---

## 15. Document history

| Date | Note |
|------|------|
| 2026-06-06 | Initial draft — zone-scoped population tables and phase integration |

---

## 16. Cross-links

- [Dungeon zone layout](Dungeon-Zone-Layout-Requirements.md) — `ZoneCellMap`, boundaries, `populationProfile` hook
- [Dynamic dungeon floor generation §7](Dynamic-Dungeon-Floor-Generation-Requirements.md) — floor-wide population baseline
- [Dynamic dungeon floor generation §9](Dynamic-Dungeon-Floor-Generation-Requirements.md) — vault entities vs scatter
