# Fog of war (DCSS-style) — Requirements

Party **line-of-sight** drives a three-state **tile knowledge** model inspired by **Dungeon Crawl Stone Soup (DCSS)**: tiles never seen stay **unseen**; tiles currently in LOS are **visible** (live terrain); tiles previously seen but outside LOS remain **explored** and render as a **last-seen (memory)** snapshot until seen again. **Actors and floor items** are shown only while **visible**; explored tiles show **terrain only**, not live monsters or pickups.

**Depends on:** `VisibilityManager`, `ShadowCaster` (`Assets/Scripts/Manager/Visibility/Algorithm/ShadowCaster.cs`), `MapManager` (`IsWalkable`, `floorMap`, `wallMap`), `PartyManager`, `TurnManager`, `BaseActor` / `GridPosition`, [Multi-tile enemies](../Combat/Multi-Tile-Enemy-Requirements.md) (footprint reveal), [Traps](../Combat/Traps-Requirements.md) (disguise vs revealed display), [Floor item piles](../Inventory/Floor-Item-Pile-Requirements.md) (`FloorItemWorldView`, tile data).

**Related:** `CombatThreatCoordinator` (combat tension LOS — must stay separate from **display** fog), `ConeSightUtility` (enemy AI sight only; cone + peripheral), `EnemyAiBrain` (AI memory is independent of player fog).

**Explicitly out of scope (v0):** Autoexplore, magic mapping, “sense monster” through walls, **last-known enemy position** ghosts on explored tiles, enemy-side fog / AI map memory, per-character light radius from stats (use global `viewRange` only), save/load of explored state (documented for a later phase), separate fog overlay tilemap (v0 may extend current `Tilemap.SetColor` tint approach).

---

## 1. Goals

**G1 — DCSS-style tile states**  
Each map cell on the current floor has **`Unseen`**, **`Explored`** (memory / last-seen), or **`Visible`**. Transitions follow §5.

**G2 — Terrain memory snapshot**  
When a cell is **visible**, store a **snapshot** of terrain appearance (floor, wall, decorations on fog-managed layers). When it becomes **explored**, render from that snapshot (dimmed), not from live tilemaps for unseen changes off-screen.

**G3 — Live updates while visible**  
If the map changes while a cell is **visible** (e.g. destroyed wall, trap revealed), update the snapshot immediately for that cell.

**G4 — Party LOS**  
Recompute visibility from **all active party members** using **omnidirectional shadow-casting** (same rules as today’s `VisibilityManager.RefreshVision` / `CombatThreatCoordinator.PartyHasTileShadowLos`), unioning visible sets. Opacity: **`!MapManager.IsWalkable(pos)`** (walls block light; missing floor = opaque).

**G5 — Automatic refresh**  
Refresh fog on gameplay events (§6), not via debug keyboard shortcuts.

**G6 — Entity display gating**  
Enemies, party (optional policy), floor item views, and trap overlays follow **visibility** rules (§8–10). Multi-tile enemies: visible if **any** footprint cell is **visible** ([Multi-tile doc](../Combat/Multi-Tile-Enemy-Requirements.md) §11).

**G7 — Remove debug cheats**  
Strip or gate debug paths that break fog state or grant omniscient combat LOS (§4, §12).

**G8 — Shared sight range config**  
Single authoritative **party view range** used by display fog and combat tile-LOS (§7); no wall-piercing “scrying” for tension in production.

---

## 2. Current implementation (audit)

| Component | Behavior today | Gap |
|-----------|----------------|-----|
| **`VisibilityManager`** | Binary tint: all cells → `fogColor`, then visible cells → `visibleColor`. `InitializeMap()` on `Start`. | No **explored** state; full-map reset each `UpdateVisibility` would erase memory if added naively. |
| **`RefreshVision()`** | Party union `ShadowCaster.GetVisibleTiles(origin, viewRange, isOpaque)`. | **Not called** from movement/turn code — only **Space** in `Update()` (debug). |
| **`viewRange`** | Serialized on `VisibilityManager` (default **8** in code; **SampleScene = 3**). | Tune for prod; keep in sync with combat fallback. |
| **`CombatThreatCoordinator`** | `PartyHasTileShadowLos` uses same `ShadowCaster` + `vis.viewRange`. Also **`remoteSenseChebyshevRadius` (16)** — enemies count for InCombat **without** LOS. | Display fog must not use remote sense; remove or zero scrying for shipping (§12). |
| **`ConeSightUtility`** | Enemy cone + peripheral + multi-origin LOS. | **Not** used for party map fog (correct for roguelike player FOV). |
| **Actors / items** | No visibility gating on renderers. | Required for DCSS feel (§8–9). |
| **Explored / snapshot** | None in codebase (`explored`, `lastSeen`, `VisibilityGrid` absent). | New data layer (§5). |

**Opacity / LOS algorithm (keep):** `ShadowCaster` with strict diagonal light blocking (walkable notch between two opaque orthogonals does not light). Party and combat tile-LOS should continue to share this implementation.

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Unseen** | Cell never entered party LOS on this floor. Render: near-black (`unseenColor`). |
| **Visible** | Cell in current party LOS. Render: full brightness; live terrain; entities shown per §8–10. |
| **Explored** | Previously **visible**, now outside LOS. Render: **memory snapshot**, dimmed (`memColor`). DCSS “mem” tile. |
| **Memory / last-seen** | Synonym for **explored** terrain appearance frozen at last visibility. |
| **Snapshot** | Per-cell stored terrain data (floor/wall/decor refs or sprite ids) updated only while **visible**. |
| **Party LOS** | Union of shadow-cast visible tiles from each active `PartyManager` member. |
| **Display fog** | Player-facing tile tint / overlay driven by §5–6. |
| **Combat tile-LOS** | `ShadowCaster.IsVisible` for `CombatThreatCoordinator` tension — **no** explored-tile omniscience. |
| **Remote sense / scrying** | `CombatThreatCoordinator.remoteSenseChebyshevRadius` — Chebyshev distance sense **without** wall LOS; **debug/cheat**, not display fog. |

---

## 4. Debug and prototype code — removal before implementation

The following **must be removed or behind a dev-only flag** (e.g. `#if UNITY_EDITOR` + explicit “Debug Tools” component) so production fog is deterministic:

| Location | Remove / gate |
|----------|----------------|
| `VisibilityManager.Update()` | **Space** → `RefreshVision()` |
| `VisibilityManager.Update()` | **`;`** → `DebugOverlayEnemySight()` |
| `VisibilityManager` | Fields `enemySightDebugTint`, `enemySightDebugBlend` and method `DebugOverlayEnemySight` |
| `VisibilityManager` | Commented “Temporary Test Logic” block (delete) |
| `CombatThreatCoordinator` | **`remoteSenseChebyshevRadius`** used for InCombat without LOS — set **0** for production or remove; document replacement (pursuit, hearing, damage) |
| `MapManager.Update()` | Left-click `Debug.Log` walkability probe (optional cleanup; same “debug in Update” class) |

**Optional dev-only (not blocking fog v0):** `CombatThreatCoordinator.verboseLogging`, verbose `TurnManager` action logs.

**Replace** keyboard-driven refresh with §6 hooks.

---

## 5. Tile visibility state machine

```text
        ┌─────────┐
        │ Unseen  │
        └────┬────┘
             │ first enters party LOS
             ▼
        ┌─────────┐     leaves LOS      ┌──────────┐
        │ Visible │ ──────────────────► │ Explored │
        └────┬────┘                     └────┬─────┘
             │                               │
             │ re-enters LOS                   │ stays Explored until floor change
             └───────────────────────────────┘
                        (Visible again; refresh snapshot while visible)
```

### R5.1 — Transitions

| From | To | Condition |
|------|-----|-----------|
| **Unseen** | **Visible** | Cell in current party LOS set. Capture **snapshot**. |
| **Visible** | **Explored** | Cell not in current party LOS. **Snapshot unchanged.** |
| **Explored** | **Visible** | Cell re-enters party LOS. Update **snapshot** from live map while visible. |
| **Explored** | **Unseen** | **Never** on same floor (unless explicit magic / floor wipe). |
| **Any** | **Unseen** | New floor / level load → reset grid (§11). |

### R5.2 — DCSS parity rules (locked)

1. **Terrain memory is a snapshot** — Door opened or wall destroyed off-screen: **explored** cells still show old layout until **visible** again.
2. **Explored ≠ visible** — Backing away from a corridor keeps it **dim explored**, not black **unseen**.
3. **Unseen cells** — No terrain snapshot; player sees void / black (current `fogColor` baseline).
4. **Monsters and items** — **Not** drawn on **explored** tiles (§8–9); only **visible** shows live entities.

### R5.3 — Data per cell (conceptual)

| Field | Notes |
|-------|--------|
| `state` | `Unseen` \| `Explored` \| `Visible` |
| `snapshot` | Optional until first seen; floor tile, wall presence, decoration layer ids (see §6.3) |

Storage: dense grid bounded by map `cellBounds`, or sparse `Dictionary<Vector3Int, CellKnowledge>` for v0 SampleScene size.

---

## 6. LOS computation and refresh triggers

### D6.1 — Algorithm (unchanged from prototype)

- **Origins:** Each active `PartyManager.partyMembers[i].GridPosition` with `z = 0` (match `MapManager` / `HasTile` keys).
- **Fallback:** Tagged `Player` transform floored to grid if party empty (dev only).
- **Cast:** `ShadowCaster.GetVisibleTiles(origin, viewRange, isOpaque)` per member; union into `HashSet<Vector3Int>`.
- **`isOpaque`:** `MapManager.Instance != null && !MapManager.Instance.IsWalkable(pos)`.

### D6.2 — When to call `RefreshPartyVision()` (locked)

| Event | Required |
|-------|----------|
| After party **move** completes (tile change) | Yes |
| **`TurnManager.OnPlayerActionComplete`** | Yes (covers move, pickup, etc.) |
| **Teleport** / forced reposition | Yes |
| **Start of player turn** (`NotifyPartyTurnStart` or equivalent) | Yes (catch-all) |
| **Floor / scene load** | Reset grid + `InitializeMap` / all **Unseen** |
| **Map edit while visible** (wall destroyed, etc.) | Refresh snapshot for affected **visible** cells |

**Must not** depend on `Update()` keyboard polling in shipping builds.

### D6.3 — Fog-managed tilemap layers (v0)

Match current `VisibilityManager.tilemaps` (SampleScene: floor + wall tilemap references). For each **visible** cell on refresh:

- Record floor tile/sprite from floor layer if present.
- Record wall tile/sprite from wall layer if present.
- Future: hazard/trap **disguise** layer entries when those systems paint overlays (§10).

### D6.4 — Repaint algorithm (replace `UpdateVisibility`)

**Do not** reset the entire map to fog each frame.

1. Compute `currentVisible` set (party LOS).
2. For each cell that was **Visible** last frame but not in `currentVisible` → set **Explored**, apply `memColor` tint from **snapshot**.
3. For each cell in `currentVisible` → set **Visible**, update snapshot from live tilemaps, apply `visibleColor`.
4. **Unseen** cells: only touched on floor init or when bounds expand; keep `unseenColor`.

Optimize later: diff visible set vs previous frame only.

### D6.5 — Colors (authoring)

| State | Serialized field | Suggested role |
|-------|------------------|----------------|
| **Unseen** | `unseenColor` | Near-black (migrate from current `fogColor`) |
| **Explored** | `memColor` | Dim snapshot (~40–55% brightness or dedicated grey-blue) |
| **Visible** | `visibleColor` | White / full brightness |

---

## 7. Party sight range

### R7.1 — v0 authoring

- Single **`viewRange`** (tiles) on `VisibilityManager` (or extracted `PartySightConfig` ScriptableObject referenced by visibility + combat).
- **SampleScene** currently uses **3** for testing; production default TBD (code default **8**).

### R7.2 — Combat sync

`CombatThreatCoordinator` must read the **same** `viewRange` for `PartyHasTileShadowLos` (today: `FindAnyObjectByType<VisibilityManager>()` + `tileSightRangeFallback`).

### R7.3 — Future (document only)

Per-character light radius, torches, spells — not v0.

### R7.4 — Asymmetry (locked)

| Observer | Model |
|----------|--------|
| **Party (display + combat tile-LOS)** | Omnidirectional `ShadowCaster`, `viewRange` |
| **Enemies (AI)** | `ConeSightUtility` — cone, peripheral multiplier, multi-origin union |

---

## 8. Enemies and party sprites

### R8.1 — Enemies

- **`SpriteRenderer`(s)** enabled **iff** any **footprint** cell is **Visible** to party.
- When hidden: disable renderers (or alpha 0); **do not** show last position on **explored** tiles (DCSS).
- AI **alert / pursuit** state is unchanged by fog (AI may still chase after heard/sensed); only **presentation** is gated.

### R8.2 — Multi-tile

When any footprint tile is **visible**, reveal **all** footprint cells for rendering (per [Multi-tile doc](../Combat/Multi-Tile-Enemy-Requirements.md) §11).

### R8.3 — Party

**v0 locked:** Party members on **visible** tiles always render (player always sees own tile). Optional: dim non-active members — **out of scope** unless requested.

---

## 9. Floor items and world props

### R9.1 — Floor piles / `FloorItemWorldView`

- Show world sprite **only** when pile cell is **Visible**.
- **Explored** tile: terrain memory only; no pile icon.

### R9.2 — Other world objects

Same rule for any `Entities` sorting-layer prop tied to a grid cell until a generic `IVisibilityGate` exists.

---

## 10. Traps and hazards (display integration)

Align with [Traps](../Combat/Traps-Requirements.md):

| Trap state | Display on **visible** cell | Display on **explored** cell |
|------------|----------------------------|------------------------------|
| Undetected invisible | Disguise (per trap doc) | **Disguise snapshot** from memory, not revealed art |
| Detected / revealed / visible-by-default | Revealed overlay | **Revealed snapshot** if seen before; else disguise memory |
| Triggered | Revealed | Revealed snapshot if ever seen while visible |

**Detection logic** (Perception, party-wide) remains as trap doc — fog does **not** auto-detect through walls. **Snapshot** captures overlay/disguise state at time of last **visible** observation.

[Environmental hazards](../Combat/Environmental-Hazards-Requirements.md): when implemented, hazard overlay ids included in §6.3 snapshot.

---

## 11. Persistence (future phase)

Documented for later; **not v0**:

- Save per-floor-id: **Explored** cells + **snapshots** (compact tile/sprite ids).
- **Visible** never saved — recompute on load from party position.
- Trap detected/triggered flags remain in trap save model, not fog grid.

---

## 12. Combat tension vs display fog

| System | Uses explored? | Uses remote scrying? |
|--------|----------------|----------------------|
| **Display fog** | Yes (dim terrain) | **No** |
| **CombatThreatCoordinator tile-LOS** | **No** (only current `ShadowCaster` LOS) | **No** in production |

**Pursuit**, hearing, damage notifications may still force **InCombat** without display visibility — separate from fog (existing `EnemyAiBrain` / pursuit decay).

---

## 13. API surface (implementation target)

Central service (expand `VisibilityManager` or `MapVisibilityService`):

```csharp
// Conceptual — names may change at implementation
bool IsVisible(Vector3Int cell);
bool IsExplored(Vector3Int cell);
bool IsUnseen(Vector3Int cell);
void RefreshPartyVision();
void ResetForNewFloor();
```

- **Single** party LOS computation path for `RefreshPartyVision` (UI and debug tools call this).
- **`IVisibilityQuery`** (optional): enemies/items query `IsVisible(cell)` without reaching into tilemaps.

---

## 14. Implementation architecture (recommended)

1. Add **`VisibilityGrid`** (or equivalent) holding §5.3 per-floor state.
2. Refactor `VisibilityManager` to **state-aware repaint** (§6.4); remove §4 debug paths.
3. Hook **`RefreshPartyVision()`** from §6.2 (mirror `CombatThreatCoordinator.EvaluateThreat` call sites).
4. Gate entity renderers via §8–9 (small component or service listening after refresh).
5. Zero **`remoteSenseChebyshevRadius`** (§4, §12).
6. Keep **`ShadowCaster`** unchanged unless a gameplay bug is found.

**Future:** dedicated **FogOverlay** tilemap so base `floorMap`/`wallMap` colors are not mutated.

---

## 15. Testing

### Unit / edit-mode

- `ShadowCaster` regression unchanged (existing tests if any; add diagonal crack case if missing).
- Grid transitions: **Unseen → Visible → Explored → Visible** on toy 5×5 map.
- Snapshot: wall present in memory after wall removed from live map while cell **explored** only.

### Play mode / manual

- Move into corridor, retreat: corridor stays **dim explored**, not black.
- Never-visited room beyond range stays **unseen**.
- Enemy walks behind wall: sprite hidden; reappears when tile **visible** again (no ghost on explored).
- Floor pile on tile: visible only in LOS.
- Removing **remote sense**: enemy across map does not force InCombat without LOS/pursuit.

---

## 16. Acceptance checklist (v0)

- [ ] Three tile states with correct transitions (§5).
- [ ] Terrain snapshot on explore; explored renders dim memory (§6).
- [ ] `RefreshPartyVision` on move/turn/teleport/load (§6.2).
- [ ] No Space/`;` debug in shipping `VisibilityManager` (§4).
- [ ] `remoteSenseChebyshevRadius == 0` (or removed) for production combat LOS (§4, §12).
- [ ] Enemies hidden when no footprint cell visible (§8).
- [ ] Floor items hidden when cell not visible (§9).
- [ ] `viewRange` shared with combat tile-LOS (§7).
- [ ] Multi-tile enemy footprint reveal when any cell visible (§8.2).

---

## 17. Open questions (resolve at implementation)

| # | Question | Default if unresolved |
|---|----------|------------------------|
| 1 | Restore `viewRange` to **8** in SampleScene for QA? | Use **8** unless level design needs **3** |
| 2 | Hide party followers outside LOS? | **No** — always show party |
| 3 | `TrapOverlay` layer in fog tilemap list when traps land? | Add when trap overlay exists |
| 4 | Dev-only debug FOV overlay (enemy cone on tilemap)? | Separate editor tool, not `VisibilityManager` |

---

## 18. File reference (current codebase)

| File | Role |
|------|------|
| `Assets/Scripts/Manager/Visibility/VisibilityManager.cs` | Prototype tint + debug input |
| `Assets/Scripts/Manager/Visibility/Algorithm/ShadowCaster.cs` | LOS shadow casting |
| `Assets/Scripts/Manager/Map/MapManager.cs` | Walkability / opacity |
| `Assets/Scripts/Manager/Combat/CombatThreatCoordinator.cs` | Combat LOS + remote scrying |
| `Assets/Scripts/Service/Sensing/SenseSightService.cs` | `ConeSightUtility` (enemies) |
| `Assets/Scripts/Manager/Turn/TurnManager.cs` | Hook point `OnPlayerActionComplete` |
| `Assets/Scenes/SampleScene.unity` | `VisibilityManager` `viewRange: 3`, floor+wall tilemaps |
