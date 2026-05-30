# Monster map presence — Requirements

While specific **bosses and special monsters** are **alive**, they can alter the dungeon: disable spawns, seal portals, place traps, change lighting, and more. In *Surviving the Game as a Barbarian*, **Riakas (Lord of Chaos)** disabled **all monster spawns** and **portals to other floors** for the duration of the fight. This project needs an **extensible** pipeline so many species can each contribute **one or more map effects** with clear **apply** and **revert** semantics.

**Depends on:** `EnemyController` / `EnemySpeciesDefinition`, `TrapService`, `MapManager`, `EnemySpawnService` (future spawn gates), interactable spawn effects (orthogonal).

**Related:** [Traps](../Combat/Traps-Requirements.md), [Doors](Door-Requirements.md), spawn definitions under `Assets/Data/Spawn/`.

**Explicitly out of scope (v0):** Riakas-scale global spawn/portal lock (specified, not implemented); save/load of presence state; multiplayer; procedural gen hooks; permanent map edits that survive revert (separate effect type, stub only).

---

## 1. Goals

**G1 — Extensible effects**  
Designers author **`MonsterMapPresenceProfile`** assets listing **`MonsterMapPresenceEffect`** instances (ScriptableObjects). New effect types are added by subclassing, not by editing a central switch for every monster.

**G2 — Bind while alive, revert on death**  
Effects **apply** when the host enemy becomes active (v0: `Start` after spawn). Effects **revert** when the host **dies** (`EnemyController.Die`) before `Destroy`, so traps and gates clean up reliably.

**G3 — Permanent-on-spawn (future)**  
Some monsters will **permanently** alter the map the moment they appear (e.g. collapse a wall). Profiles may include effects flagged **permanent** that **do not revert** on death (v0: field + doc only, or no revert hook).

**G4 — Riakas pattern (future)**  
Global systems (spawn suppression, portal lock) subscribe to **`MonsterMapPresenceService`** host counts or tagged effects — not hard-coded to one species id.

**G5 — v0 QA**  
**Skeleton map-presence test:** while a designated skeleton lives, a **visible floor trap** (pit; v0 uses bear-trap art) exists at **`(-1, -1)`**; when the skeleton dies, that trap is **removed**.

---

## 2. Reference — STBGB / Riakas

| Behavior | Target implementation |
|----------|------------------------|
| No new monster spawns while Riakas lives | `DisableMonsterSpawnsWhileAliveEffect` (future) → `MonsterMapPresenceService` ref-count |
| No floor portals while Riakas lives | `DisableFloorPortalsWhileAliveEffect` (future) |
| Boss-specific trap fields | `TrapWhileAliveMapEffect` (v0) |

---

## 3. Core types (v0)

### 3.1 — `MonsterMapPresenceProfile`

| Field | Purpose |
|-------|---------|
| `displayName` | Logging / inspector |
| `effects` | Ordered list of `MonsterMapPresenceEffect` |
| `permanentOnSpawn` | If true, effects that support permanence skip revert (future) |

Assigned on **`MonsterMapPresenceHost.profileOverride`** and/or **`EnemySpeciesDefinition.mapPresenceProfileAsset`** (cast at runtime to `MonsterMapPresenceProfile`; avoids Data-assembly coupling).

### 3.2 — `MonsterMapPresenceEffect` (abstract)

| Method | When |
|--------|------|
| `Apply(MonsterMapPresenceContext ctx)` | Host bound |
| `Revert(MonsterMapPresenceContext ctx)` | Host unbound / death |

Effects register revert work on the context (e.g. closure that unregisters a trap).

### 3.3 — `MonsterMapPresenceContext`

| Field | Purpose |
|-------|---------|
| `Owner` | `EnemyController` |
| `Profile` | Source profile |
| `RegisterRevert(Action)` | Stack revert callbacks |

### 3.4 — `MonsterMapPresenceHost` (MonoBehaviour)

- Resolves profile: **inspector override** → else **`Species.mapPresenceProfile`**
- **`Bind()`** — once, from `Start`
- **`Unbind()`** — idempotent, from `Die` + `OnDestroy` safety
- Logs with prefix **`[MapPresence]`**

### 3.5 — `MonsterMapPresenceService` (singleton)

- Tracks active hosts (optional v0: logging + future global gates)
- Lives on **GameSystems** in SampleScene (editor menu)

---

## 4. v0 effect — `TrapWhileAliveMapEffect`

| Field | Purpose |
|-------|---------|
| `cell` | Floor trap host cell |
| `trapDefinition` | `TrapDefinition` (floor); v0 QA uses visible bear/pit art |
| `logTag` | Optional suffix in logs |

**Apply:** `TrapService.Register(cell, definition)` if walkable and cell free.  
**Revert:** `TrapService.TryUnregisterFloorTrap(cell)` — clears overlay and dictionaries.

If register fails (occupied, not walkable), log warning; no revert registered.

---

## 5. Trap service extension

`TrapService` gains **`TryUnregisterFloorTrap(Vector3Int cell)`** (v0):

- Remove from `_floorTrapsByCell`, `_allInstances`
- `GridOverlayPainter.Clear` on trap overlay
- Log `[Trap] Unregistered …`

Wall traps unchanged in v0.

---

## 6. Enemy death hook

`EnemyController.Die()` calls **`GetComponent<MonsterMapPresenceHost>()?.Unbind()`** before loot/grid unregister/destroy.

---

## 7. Debug logging

Prefix: **`[MapPresence]`**

| Event | Example |
|-------|---------|
| Bind start / complete | `Bound Skeleton_MapPresenceTest …` |
| Per effect apply | `Apply TrapWhileAlive at (-1,-1) …` |
| Unbind | `Unbound …` |
| Per effect revert | `Revert TrapWhileAlive at (-1,-1) …` |
| Service host count | `Active hosts: N` |

Trap unregister also logs under **`[Trap]`**.

---

## 8. SampleScene QA (v0)

| Asset / object | Purpose |
|----------------|---------|
| `Profile_SkeletonPitTest` | `TrapWhileAlive` → `(-1,-1)`, `TrapDefinition_Bear` (visible pit) |
| `Enemy_MapPresenceTestSkeleton` prefab or scene instance | `MonsterMapPresenceHost` + profile |
| Editor menu | **JRogue → World → Place Map-Presence Test Skeleton in SampleScene** |

**Test steps**

1. **JRogue → Traps → Create QA Trap Asset Pack** (if traps missing)
2. **JRogue → World → Create Map Presence v0 Assets**
3. **JRogue → World → Wire Map Presence Service in SampleScene**
4. **JRogue → World → Place Map-Presence Test Skeleton in SampleScene**
5. Play — pit visible at `(-1,-1)`; kill skeleton — pit gone

---

## 9. Future effects (checklist)

- [ ] `DisableMonsterSpawnsWhileAliveEffect`
- [ ] `DisableFloorPortalsWhileAliveEffect`
- [ ] `PermanentTerrainMapEffect` (no revert)
- [ ] Profile on `EnemySpeciesDefinition` for Riakas
- [ ] Save/load presence snapshot

---

## 10. Acceptance criteria (v0)

| ID | Criterion |
|----|-----------|
| **AC1** | New effect type = new `MonsterMapPresenceEffect` subclass + asset, no changes to host/service switch |
| **AC2** | Skeleton test spawn → trap at `(-1,-1)` visible |
| **AC3** | Skeleton death → trap removed, tile walkable, overlay clear |
| **AC4** | `[MapPresence]` logs for bind, apply, unbind, revert |
| **AC5** | Editor menu places test skeleton in SampleScene |

---

## 11. Implementation checklist

- [x] Requirements (this doc)
- [x] `TrapService.TryUnregisterFloorTrap`
- [x] Map presence types + `TrapWhileAliveMapEffect`
- [x] `MonsterMapPresenceHost` + `EnemyController.Die` hook
- [x] `MonsterMapPresenceService` + editor wiring
- [x] v0 assets + SampleScene editor menu
- [ ] Unit test: register + unregister floor trap
