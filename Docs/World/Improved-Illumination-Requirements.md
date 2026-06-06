# Improved Illumination — Town Torches & Per-Cell Illumination Gating

**Purpose:** Ship **production-quality local lighting** in **town** — real wall-torch art, **always-lit emitters** on the wall map, **town phase → ambient** sync so torches read clearly at **night**, and **per-cell illumination gating** so darkness and torch pools shape what the party can see (not an omnidirectional radius from the player). Establishes the content + presentation pattern reused in **dungeon** floors later.

**Status:** Implemented (town v0 — manual QA §8.4 still recommended after playtest).

**Depends on:** [Lighting — Requirements](Lighting-Requirements.md) (`LightingService`, `LightEmitterDefinition`, `LightCellData`, `VisibilityManager`, `LightLevel`), [Lighting QA and Torch v0](Lighting-QA-And-Torch-v0-Requirements.md) (wall torch + emitter math; SampleScene harness), [Town time & calendar](Town-Time-And-Calendar-Requirements.md) (`TownTimeService`, `TownTimePhase`), [Fog of War](Fog-Of-War-Requirements.md), [Interactable tiles](../Combat/Interactable-Tiles-Requirements.md), `MapManager` wall/floor tilemaps, `LightingInitPhase`, `TownNpcSetupPhase` / `Stamp_TownPlaza_20x20`.

**Related:** [Lighting — Future backlog](Lighting-Future-Backlog.md) (Phase E day/night, Phase F emitters, item **#40** torch VFX). [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (dungeon torch pass deferred here §11). **Player-carried light items:** [Light-Emitting Items](Light-Emitting-Items-Requirements.md).

**Explicitly out of scope (this milestone):** Carried torch accessory (see [Light-Emitting Items](Light-Emitting-Items-Requirements.md)); player **ignite unlit** wall torch gameplay (town torches ship **pre-lit**); URP 2D Light components as gameplay truth; colored/mood lighting; enemy alert from party light; magical darkness zones; save/load of per-torch on/off state beyond run persistence; more than **three** town torches; full dungeon torch population (see §11).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | Import a **real authored torch sprite** (not a flat QA placeholder) suitable for **wall mounting**; store under project art paths. |
| **G2** | Place **three wall torches** on **`town_main`** — each is a **wall tile** with a **live emitter** (`emitLight > 0` at load). |
| **G3** | **Town ambient tracks `TownTimePhase`** — at **Night**, plaza ambient drops enough that **torch pools are obvious** vs **Day/Morning**. |
| **G4** | **Per-cell illumination gating** — live visibility follows **lit tiles** in geometric LOS; **`receivedLight = 0`** → invisible; dim **1–2** → **dark tile**; at night away from torches, only **occupied** tiles visible. |
| **G5** | **Reusable pipeline** — same wall-torch + emitter authoring pattern documented for a **later dungeon pass** (§11). |
| **G6** | **Multi-emitter aggregation** — overlapping torch light uses **sum of contributions, capped at `LightLevel.Max`** (already implemented in `LightingService`). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Wall torch** | A **wall map** cell with torch sprite + `LightCellData` emitter; v0 town torches are **always lit**. |
| **Emitter** | Cell that outputs light (`emitLight`); Manhattan falloff per `LightEmitterDefinition`. |
| **Ambient (town)** | Overhead light for town receivers from `LightingService` default region (or town-specific region id). |
| **Received light** | Per-cell intensity: **`min(LightLevel.Max, sum(emitter contribs) + ambient)`** — see §5.4. |
| **Geometric sight (`S_base`)** | `CharacterStats.sight.GetValue()` — max distance for **shadow-cast LOS** (walls block). **Not** reduced by darkness; light gates which LOS cells become live-visible. |
| **Illumination gate** | A cell in **`S_base`** LOS is **live-visible** only if **`receivedLight(C) > 0`** (exceptions: §5.2 R7.1). |
| **Dark tile** | Live-visible, **`0 < receivedLight(C) < threshold`** (default threshold **3**) — dim tint, not unseen. |
| **Pitch black tile** | **`receivedLight(C) = 0`** — **not live-visible** even if inside geometric LOS (FoW **unseen** or **explored** memory only). |
| **Town phase ambient table** | Mapping `TownTimePhase` → `currentAmbientLight` for town floor (§8). |

---

## 3. Current state vs gap

| Area | Already exists | Gap (this doc) |
|------|----------------|----------------|
| **Ambient regions** | `LightingService`, `AmbientRegion`, `LightLevel.FullDaylightAmbient` | Town phase **does not** drive ambient ([Town time §9](Town-Time-And-Calendar-Requirements.md) deferred hook). |
| **Emitters** | `LightEmitterDefinition` (`Torch.asset`: emission **6**, radius **8**, falloff **1**/tile) | No **production torch sprite** on wall map; QA torch is SampleScene-only. |
| **Wall torch interactable** | `WallTorchInteractable` + `SetTileEmissionEffect` (ignite flow) | Town uses **static lit** emitters — no bump-to-light required v0. |
| **Sight / visibility** | Per-member `CharacterStats.sight` LOS; light affects **brightness threshold** inside full LOS | **No per-cell gate** — unlit cells in LOS can appear as **dark tiles**; torch edge wrongly reveals into blackness |
| **Presentation** | `darkTileColor`, debug warmth overlay **`L`** | Town never feels **night-dark**; torches not placed in town. |
| **Town time** | `TownTimeService` Morning / Day / Night | Night is logical only — no lighting feedback. |

---

## 4. Torch art (real sprite)

### 4.1 — Sourcing (locked)

| Rule | Detail |
|------|--------|
| **Must be real art** | Not a generated flat-color QA square. Prefer **pixel-art wall torch** matching town **32 PPU**, **point** filter. |
| **License** | **CC0** or project-clear commercial use; record attribution in `Assets/Art/Lighting/CREDITS.md` (new file, one line per asset). |
| **Orientation** | **Wall-mounted** — flame readable from default town camera (orthogonal top-down / slight tilt). |
| **Suggested sources** | OpenGameArt / Kenney / itch.io “dungeon tileset” wall torch sheets — pick **one** frame, not a whole unintegrated pack unless vetted. |
| **Rejected** | Reusing party field sprites, NPC portraits, or existing shop/town character sprites as torch stand-ins. |

### 4.2 — Project paths (locked)

| Asset | Path |
|-------|------|
| **Sprite texture** | `Assets/Art/Lighting/Sprites/WallTorch_Lit.png` |
| **Optional unlit** | `Assets/Art/Lighting/Sprites/WallTorch_Unlit.png` *(future ignite flow; not required for always-lit town v0)* |
| **Tile / prefab hook** | Wall tile references sprite via `Tile` asset or runtime `WallMap.SetTile` in generation phase |
| **Emitter definition** | Reuse `Assets/Prefabs/Lighting/Torch.asset` unless tuning requires `LightEmitterDefinition_TownTorch.asset` |

### 4.3 — Import settings

| Setting | Value |
|---------|-------|
| **Texture type** | Sprite |
| **PPU** | **32** (match town wall/floor) |
| **Filter mode** | Point |
| **Pivot** | Bottom-center or wall-attach convention matching existing wall tiles |
| **Alpha** | Transparency on; no mipmaps |

---

## 5. Visibility — geometric sight + per-cell illumination (locked)

This section **replaces** the draft **`L_obs → S_eff`** model (observer light shrinking LOS in all directions). The locked rules are:

| # | Rule |
|---|------|
| **1** | **Per-cell illumination gating** is the primary visibility rule — a cell in geometric LOS is live-visible only if **`receivedLight(C) > 0`** (plus R7.1 exceptions). |
| **2** | **`S_base`** remains the **geometric LOS cap only** (`CharacterStats.sight` → `ShadowCaster`). Darkness does **not** shrink **`S_base`**. |
| **3** | **Torch edge** — visibility follows the **lit footprint** (intersection of LOS and illuminated tiles), **not** omnidirectional expansion into unlit plaza. |

```text
liveVisible(M) = { C | C ∈ losCells(M, S_base) ∧ (R7.1 exceptions ∨ receivedLight(C) > 0) }
```

### 5.1 — Terms: `S_base` (kept) vs `S_eff` (rejected)

| Symbol | Meaning | Status |
|--------|---------|--------|
| **`S_base`** | `CharacterStats.sight.GetValue()` — how far **`ShadowCaster`** reaches through walls. The **stat sight range**. | **Keep** — always used for geometric LOS. |
| **`S_eff`** | Draft idea: shrink **`S_base`** based on light at the **player’s feet** (`L_obs`). | **Rejected** — produces a **symmetric circle** into unlit tiles at torch edges; does not match torch geometry. |

Darkness limits vision by **which tiles are lit**, not by shrinking LOS equally in all directions.

### 5.2 — Locked visibility pipeline (per party member `M`, then union)

**Step 1 — Geometric LOS (unchanged cap)**

```text
losCells(M) = ShadowCaster(origin(M), S_base, wall opacity)
```

**Step 2 — Illumination gate (new)**

Cell **`C`** is **live-visible** for **`M`** if:

| Condition | Rule |
|-----------|------|
| **R7.1 occupied** | Any party member stands on **`C`** → always live-visible, full bright. |
| **R7.1 emitter** | **`C`** is an emitter with **`emitLight > 0`**, **`C ∈ losCells(M)`** → always live-visible, full bright. |
| **Illuminated receiver** | **`C ∈ losCells(M)`** AND **`receivedLight(C) > 0`** → live-visible (brightness in step 3). |
| **Pitch black** | **`C ∈ losCells(M)`** BUT **`receivedLight(C) = 0`** → **NOT live-visible** (not a dark tile). |

**Step 3 — Brightness (inside live-visible set)**

| `receivedLight(C)` | Presentation |
|--------------------|----------------|
| **`≥ threshold`** (default **3**) | Full bright (`visibleColor`) |
| **`1 … threshold − 1`** | **Dark tile** (`darkTileColor`) — dim but seen |
| **`0`** | Not in live-visible set (§5.2 gate) |

**Party union:** Union live-visible sets and lit/dark presentation across all active party members (parent §5.3).

### 5.3 — Pitch dark at night (answers original question)

| Scenario | Result |
|----------|--------|
| **Night**, ambient **0**, not in torch pool | **`receivedLight = 0`** on adjacent tiles → **only occupied party tiles** live-visible |
| **At torch edge** | See **torch-shaped** lit footprint in LOS — **not** omnidirectional expansion into plaza blackness |
| **Day**, ambient **10** | Most receivers **`receivedLight > 0`** → full plaza visible within **`S_base`** |

### 5.4 — Multi-emitter aggregation (locked)

When a cell is lit by **multiple sources** (overlapping torches, torch + ambient, etc.):

```text
contrib(E, C) = max(0, emitLight(E) − falloffPerTile × manhattanDistance(E, C))
receivedFromEmitters(C) = sum over all emitters E of contrib(E, C)
receivedLight(C) = min(LightLevel.Max, receivedFromEmitters(C) + ambientAtRegion(C))
```

| Decision | Locked answer | Rationale |
|----------|---------------|-----------|
| **Sum vs max** | **Sum**, then **cap at `LightLevel.Max` (10)** | Matches existing `LightingService.ComputeReceivedLightAt` and parent [Lighting §6.2](Lighting-Requirements.md). Overlapping torches **stack** until cap — rewarding placement; union of pools is natural. |
| **Max-only alternative** | **Not v0** | Two torches on one tile would not brighten it; less intuitive for “more light here.” Revisit only if stacking feels exploitable. |
| **Visibility union** | Automatic | A cell lit by **either** torch has **`receivedLight > 0`** → visible if in LOS. Overlap **brightens** (may cross full-bright threshold). |

**Example:** Two torches each contributing **3** at cell **`C`** → **`receivedLight(C) = 6`** (before ambient) → full bright at threshold **3**.

### 5.5 — Dark Vision (future hook)

| Capability | Suggested behavior (implement in parent §8 pass) |
|------------|--------------------------------------------------|
| **Dark Vision** | May treat **`receivedLight = 0`** cells within **`N`** tiles as **dark tiles** (not full bright), **or** lower **`effectiveLightThreshold`** — **not** omnidirectional **`S_eff`**. |
| **Magical darkness** | Future cap on effective threshold / Dark Vision — unchanged from parent §8.4. |

### 5.6 — Supersedes / clarifies parent doc

| Parent rule | This milestone |
|-------------|----------------|
| Dark tile for under-threshold in LOS (§7.4) | **Narrows:** only when **`receivedLight > 0`**. Zero light = **invisible**, not dark tile. |
| Sight = stat only (§5.1) | **`S_base`** unchanged; **illumination gate** adds light coupling |
| FoW unexplored outside LOS | Unchanged |

---

## 6. Wall torch content model

### 6.1 — Always-lit town torches (v0)

| Field | Value |
|-------|-------|
| **Role** | `LightCellData`: **emitter** on **wall cell**; adjacent floor cells remain **receivers** |
| **`emitLight` at load** | **`LightLevel.TorchEmission` (6)** — **not** 0 |
| **Ignite interactable** | **Not required** on town plaza torches v0 (skip bump / `SetTileEmissionEffect`) |
| **Definition** | `Assets/Prefabs/Lighting/Torch.asset` |
| **LOS** | `blocksLos = false` (torch does not block shadow cast) |
| **Presentation** | Wall sprite on `WallMap`; optional dev **`L`** warmth overlay unchanged |

### 6.2 — Wall placement rules

| Rule | Detail |
|------|--------|
| **Map layer** | Torch occupies **`WallMap`** cell; must border ≥1 **walkable floor** orthogonally (plaza edge / building facade). |
| **Blocking** | Wall cell blocks movement (existing wall rules). |
| **Emitter cell** | Light registers on **wall coordinate**; falloff reaches adjacent floor tiles. |
| **Duplicate** | One torch per wall cell; no stacked emitters. |

### 6.3 — Optional future: unlit → lit

Dungeon / interactable torches may later use **`emitLight = 0`** + ignite flow from [Lighting QA §4](Lighting-QA-And-Torch-v0-Requirements.md). Town v0 skips this to reduce scope.

---

## 7. Town deployment — three torches

### 7.1 — Floor & phase

| Field | Value |
|-------|-------|
| **Floor id** | **`town_main`** |
| **Count** | **3** wall torches |
| **Spawn** | New generation phase **`TownTorchSetupPhase`** (or extend stamp + setup), after wall/floor bake, before / after `LightingInitPhase` |

### 7.2 — Suggested cells (initial — finalize in stamp authoring)

Plaza **`Stamp_TownPlaza_20x20`**: place torches on **perimeter wall** facing inward so light reaches plaza floor. Illustrative anchors (adjust to actual wall geometry):

| Id | Suggested wall cell | Rationale |
|----|---------------------|-----------|
| **`town_torch_w`** | **(0, 10, 0)** | West perimeter wall — lights west plaza |
| **`town_torch_n`** | **(10, 19, 0)** | North perimeter wall — near portal row |
| **`town_torch_e`** | **(19, 10, 0)** | East perimeter wall — balances west torch |

**Requirement:** each torch ≥ **4 Manhattan tiles** from another torch **and** from **`playerStart`** (avoid spawn glare stack). Document final cells in stamp **`markers`** or dedicated **`TownTorchPlacement`** asset.

### 7.3 — Stamp markers (recommended)

Add to `StampMarkerIds`:

```csharp
public const string TownTorchWest = "town_torch_w";
public const string TownTorchNorth = "town_torch_n";
public const string TownTorchEast = "town_torch_e";
```

Editor menu **JRogue/Town/Place Town Torches** writes markers + validates wall cells.

### 7.4 — Acceptance (placement)

| ID | Test |
|----|------|
| **AC-T1** | Exactly **3** lit wall torches on `town_main` load. |
| **AC-T2** | Each torch wall cell has **`GetEmitLight == 6`**. |
| **AC-T3** | Plaza floor cells within **4** tiles of a torch have **`receivedLight > 0`** at load (day ambient). |
| **AC-T4** | Sprites are **`WallTorch_Lit`** art, not QA placeholder colors. |

---

## 8. Town phase → ambient lighting

### 8.1 — Hook (implements [Town time §9](Town-Time-And-Calendar-Requirements.md) option **A**)

| Event | Action |
|-------|--------|
| **`TownTimeService` phase change** | Push ambient to town lighting region |
| **`OnTownFloorActivated`** | Apply ambient for **current** phase |
| **Dungeon → town return** | Re-apply (**Day** phase per town time §7) |

Subscribe from `TownLightingSync` (new) or extend `TownTimeService` — prefer **dedicated component** to keep time service free of lighting refs.

### 8.2 — Ambient table (locked v0)

Town uses default floor ambient region **0** (same as `LightingInitPhase` / `LightingService.defaultFloorAmbientRegionId`).

| `TownTimePhase` | `currentAmbientLight` | Notes |
|-----------------|-------------------------|-------|
| **Morning** | **8** | Soft bright — torches visible but not critical |
| **Day** | **10** | `LightLevel.FullDaylightAmbient` |
| **Night** | **0** | `LightLevel.PitchDark` — unlit tiles **`receivedLight = 0`**; only **occupied** tiles live-visible unless in a torch pool (§5.3) |

**Design intent:** **Night** ambient **0** makes unlit plaza tiles **`receivedLight = 0`** → invisible beyond party feet (§5.3). Torch pools stand out against **`darkTileColor`** rims and full-bright cores.

### 8.3 — Recompute & presentation

| Step | Detail |
|------|--------|
| 1 | `LightingService.SetAmbientLight(0, level)` |
| 2 | Recompute all town receivers |
| 3 | `VisibilityManager.OnPartyVisionActivity()` (or existing refresh entry point) |
| 4 | Log: **`[TownLighting] Phase {phase} → ambient {level}`** |

### 8.4 — QA script

1. Load town — **Day** — full plaza live-visible within **`S_base`**.  
2. Advance time lever to **Night** — ambient **0** — step away from torches → **only occupied tile** live-visible per member.  
3. Walk to torch edge — visibility follows **torch footprint** only; step into unlit plaza from edge → black tiles behind you stay **invisible**, not dark tiles.  
4. Stand in overlap of two torches — cell **`receivedLight`** sums (cap **10**); brighter core where pools overlap.  
5. Advance to **Morning** — ambient **8** — plaza navigable without torch dependency.

---

## 9. Presentation summary

| State | Rule |
|-------|------|
| **Unseen (live)** | Outside **`S_base`** LOS **or** **`receivedLight = 0`** inside LOS |
| **Visible + lit** | Live-visible, **`receivedLight ≥ threshold`** (default **3**) |
| **Visible + dark tile** | Live-visible, **`0 < receivedLight < threshold`** — e.g. dim **1–2** at torch rim |
| **Self tile** | Always live-visible, full bright (R7.1) |
| **Emitter in LOS** | Lit torch wall tile full bright when in **`S_base`** LOS (R7.1) |
| **Explored memory** | FoW snapshot — unchanged; live torch changes do not rewrite explored lighting |
| **Debug overlay** | Existing **`L`** warmth overlay optional in dev builds |

---

## 10. Services & code layout (recommended)

| Piece | Responsibility |
|-------|----------------|
| **`TownLightingSync`** | Listen `TownTimeService` phase/day events; apply §8.2 ambient |
| **`TownTorchSetupPhase`** | Spawn 3 wall tiles + register emitters from stamp markers |
| **`VisibilityManager`** | Implement §5.2 illumination gate in lit-visible / refresh path ( **`IsCellFullyVisibleForMember`** + fog visible set) |
| **`LightingService`** | Keep **sum + cap** aggregation (§5.4); no change to propagation math |
| **`TownTorchPackCreator`** (Editor) | Import sprite helper, place markers, validate cells |
| **`CREDITS.md`** | Torch art attribution |

**Do not** duplicate propagation math — use existing `LightingService`.

---

## 11. Dungeon follow-up (deferred — same pattern)

| Item | Plan |
|------|------|
| **Art** | Reuse **`WallTorch_Lit`** / unlit variant |
| **Ambient** | Floor default **`PitchDark` (0)** per [Lighting QA §2.1](Lighting-QA-And-Torch-v0-Requirements.md) |
| **Placement** | Vault / room templates + `LightingCellMarker` or generation phase |
| **Ignite flow** | Optional unlit emitters + interactable (QA path already spec’d) |
| **Visibility** | Same **§5 per-cell gate** on dungeon floors |
| **Day/night** | [Dungeon time](Dungeon-Time-Requirements.md) drives **dungeon** ambient separately from town |

This milestone delivers the **reference implementation** in town only; dungeon is a **content pass**, not new math.

---

## 12. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | Real **`WallTorch_Lit`** sprite in project with documented license. |
| **AC2** | **3** always-lit wall torches on **`town_main`**. |
| **AC3** | **Night** → town ambient **0**; **Day** → **10**; **Morning** → **8**. |
| **AC4** | At **Night**, away from torches → each member sees **only their occupied tile** live. |
| **AC5** | At **Night**, at torch edge → live visibility matches **lit footprint**, not omnidirectional **`S_base`** into black tiles. |
| **AC6** | At **Night**, torch rim cells with **`receivedLight` 1–2** show as **dark tiles**. |
| **AC7** | Overlapping torch pools **sum** contributions (unit test: two contribs → higher **`receivedLight`**, cap **10**). |
| **AC8** | At **Day**, torch light is noticeable but **not required** to navigate plaza. |
| **AC9** | Phase lever advance triggers ambient + visibility refresh without scene reload. |

---

## 13. Implementation checklist

- [x] Source & import **`WallTorch_Lit.png`** + **`CREDITS.md`**
- [x] **`TownTorchSetupPhase`** + stamp markers **`town_torch_w/n/e`**
- [x] Wall tile / sprite binding on **`WallMap`**
- [x] Emitter registration (`emitLight = 6`) on torch wall cells
- [x] **`TownLightingSync`** + ambient table §8.2
- [x] **`VisibilityManager`** illumination gate §5.2 ( **`receivedLight = 0`** → not live-visible)
- [x] Cross-link from [Town time §9](Town-Time-And-Calendar-Requirements.md) → this doc
- [x] Editor: **JRogue/Town/Place Town Torches**
- [x] Unit tests: gate §5.2, sum §5.4 / AC7
- [ ] Manual QA §8.4

---

## 14. Resolved design decisions

| # | Question | Locked answer |
|---|----------|---------------|
| **Q1** | Should low light limit vision? | **Yes** — per-cell gate: **`receivedLight = 0`** → not live-visible; only **occupied** tile at night in black. |
| **Q2** | Dim vs black? | **`1 … threshold−1`** → **dark tile**; **`0`** → **invisible** (not dark tile). |
| **Q3** | `S_eff` vs per-cell gate? | **Reject `S_eff`**; keep **`S_base`** + illumination gate (§5.1). |
| **Q4** | Multi-source brightness? | **Sum** emitter contribs, **cap at `LightLevel.Max` (10)**; then add ambient (§5.4). |
| **Q5** | Town torches ignitable? | **No** v0 — always lit. |
| **Q6** | How many town torches? | **3** on plaza perimeter. |
| **Q7** | Night ambient? | **0** — torches stand out. |
| **Q8** | Torch sprite | **Real CC0/CC-BY wall torch**, 32 PPU — §4. |
| **Q9** | Dungeon in this milestone? | **No** — pattern only §11. |

---

## 15. Debug logging

| Prefix | When |
|--------|------|
| **`[TownLighting]`** | Phase → ambient apply |
| **`[TownTorch]`** | Spawn cell + emission register |
| **`[Lighting:Sight]`** | **`S_base`** per member at origin (existing verbose flag) |
| **`[Lighting:Gate]`** | Cell in LOS but **`receivedLight = 0`** excluded from live-visible (new, optional verbose) |
| **`[Lighting:DarkTile]`** | Under-threshold visible cells (existing) |

---

## 16. References

- Parent spec: [Lighting — Requirements](Lighting-Requirements.md)  
- QA / ignite torch: [Lighting QA and Torch v0](Lighting-QA-And-Torch-v0-Requirements.md)  
- Town clock: [Town time & calendar](Town-Time-And-Calendar-Requirements.md)  
- Emitter asset: `Assets/Prefabs/Lighting/Torch.asset`  
- Visibility: `Assets/Scripts/Manager/Visibility/VisibilityManager.cs`  
- Town floor: `town_main` / `Stamp_TownPlaza_20x20`
