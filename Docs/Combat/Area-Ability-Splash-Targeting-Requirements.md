# Area ability splash targeting — Requirements

**Multi-tile abilities** (Fireball, future charge attacks, line blasts) use the existing **single reticle** targeting mode: the player moves **one** cursor tile, confirms once, and the game resolves effect on a **primary target cell** plus optional **splash cells**. During targeting, the UI shows a **white** reticle on the **primary** tile and **red** reticle markers on every **splash** tile (cells affected in addition to or besides the primary, per shape rules). Shape logic is **data-driven and extensible** so new patterns (disk, line, cone later) do not require duplicating preview or combat code.

**Depends on:** `AbilityAction` (`requiresTarget`, `range`, `splashRadius` today), `PlayerCommandProcessor` / `InputState.Targeting`, `TargetingReticleView`, `TargetingResolver`, `FireballAbility`, [Fireball scroll](Inventory/Fireball-Scroll-Requirements.md), [Multi-tile enemies](Multi-Tile-Enemy-Requirements.md) (footprint-aware hit detection).

**Related:** `Assets/Scripts/Item/Ability/Targeting/TargetingData` (early stub — superseded by this spec), [Telekinesis essence](../Essence/Telekinesis-Essence-Requirements.md) (single-tile targeting, invalid-confirm pattern).

**Explicitly out of scope (v0):** Animated VFX on cast (separate milestone); full line-of-sight gating for splash preview; friendly-fire **preview coloring** on reticle (see [Friendly fire confirmation](Friendly-Fire-Confirmation-Requirements.md) for confirm dialog); targeting through walls unless ability adds it later; save/load mid-targeting.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **One cursor** — Player still moves a **single** reticle; splash tiles update automatically from primary + ability shape. |
| **G2** | **Dual-color preview** — **White** = primary target tile; **Red** = splash zone tiles (see §6). |
| **G3** | **Extensible shapes** — Fireball disk, throwing-knife single tile, future **line** charge, etc. share one **splash zone** API. |
| **G4** | **Preview = resolution** — Cells shown in red are exactly the cells used for AoE hit queries at confirm (unless ability documents explicit exceptions). |
| **G5** | **Footprint-aware hits** — Splash resolution reuses footprint rules (`IGridFootprint`, `TargetingResolver`) so multi-tile enemies are hit once. |
| **G6** | **Future VFX hook** — Preview layer is separate from cast VFX; swapping art later does not change shape math. |
| **G7** | **All targeting sources** — Essence, equipment, mage spells, inventory scrolls, and bow-unrelated targeted abilities use the same preview pipeline when they carry a splash shape. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Primary tile** | The tile under the player’s reticle (`TargetingReticleView.Position`). Confirm passes this as `targetTile` to `AbilityAction.Execute(user, targetTile)`. |
| **Splash tiles** | Grid cells highlighted in **red** during targeting — the AoE footprint of the ability at the current primary tile. |
| **Splash zone / shape** | Algorithm that, given **origin** (usually caster cell), **primary tile**, and authored parameters, returns the set of splash cells. |
| **Primary-only ability** | Shape with zero splash cells beyond primary (e.g. throwing knife); only white reticle visible. |
| **Preview** | Editor/runtime visuals during `InputState.Targeting` only; no damage until confirm. |
| **Resolution** | `ExecuteCore(user, targetTile)` — applies gameplay using the same cell set as preview. |

---

## 3. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Targeting input** | `PlayerCommandProcessor.EnterTargetingMode` → `TargetingReticleView.Show` / `Move`; confirm → `Execute(user, target)`. |
| **Reticle visuals** | **Single** prefab (or yellow procedural fallback); **no** splash overlay. |
| **AoE data** | `AbilityAction.splashRadius` (int); Fireball uses **Chebyshev-style** distance via `TargetingResolver.GetTargetsInRadius` (nearest footprint distance ≤ radius). |
| **Damage** | `FireballAbility` calls `GetTargetsInRadius(targetTile, splashRadius)` — actors listed, not tile list exposed to UI. |
| **Range** | `AbilityAction.range` exists; **not** enforced in reticle movement (v0 gap). |
| **Stub** | `JRogue.Abilities.Targeting.TargetingData` enum (`SingleTile`, `AreaOfEffect`, `Line`) — **not wired** to runtime. |

---

## 4. Design principles

### P1 — Single control, derived splash

The player never moves a second cursor. Moving the primary reticle recomputes splash cells every frame (or on position change).

### P2 — Shape strategy, not hard-coded Fireball

```text
AbilityAction.splashZone (SplashZoneDefinition)
    → ISplashZoneShape.ComputeCells(SplashZoneContext) → IReadOnlyList<Vector3Int>
         ↑ used by TargetingReticleView (preview)
         ↑ used by FireballAbility / other abilities (resolution)
```

### P3 — Primary tile is always explicit

Even when the shape includes the primary cell in the blast (Fireball), the **white** reticle still marks the **aimed** cell. Red marks the full splash set (which may include the primary cell — if so, primary white draws **on top** or uses a distinct outline so aim point stays obvious).

**Recommended v0:** White reticle on primary; red on **non-primary** splash cells only. Primary cell is not duplicated in red. Fireball: red = disk minus primary.

### P4 — Visuals are pluggable

v0: instantiated **tile sprites** (white quad / red quad).  
Later: particle fields, animated rings, line beams — implement `ISplashZoneVisualizer` without changing shape math.

---

## 5. Data model

### D5.1 — `SplashZoneDefinition` (ScriptableObject)

| Field | Purpose |
|-------|---------|
| `shapeKind` | Enum → factory for shape implementation |
| `radius` | Disk shapes (Fireball); 0 = primary only |
| `maxLength` | Line / beam shapes |
| `includePrimaryInSplash` | If true, primary is in damage set but not red (default **false** for Fireball-style) |
| `distanceMetric` | Manhattan vs Chebyshev for disks (Fireball: match current `GetTargetsInRadius` behavior) |

**Menu:** `Create → JRogue → Targeting → Splash Zone Definition`

### D5.2 — `AbilityAction` (extend)

| Field | Change |
|-------|--------|
| `splashZone` | Reference to `SplashZoneDefinition` (preferred) |
| `splashRadius` | **Deprecated** — migrate Fireball to zone asset; keep field temporarily with `[Obsolete]` or auto-migrate in editor |

Abilities with **no** `splashZone` and `splashRadius == 0` → primary-only (white only).

### D5.3 — `SplashZoneContext`

| Input | Use |
|-------|-----|
| `Vector3Int casterCell` | Line/charge origin |
| `Vector3Int primaryTile` | Reticle position |
| `FacingDirection casterFacing` | Line direction when target not used |
| `MapManager` / `GridManager` | Optional walkability filter for preview (future) |

### D5.4 — Shape catalog (extensible)

| `SplashZoneShapeKind` | Cells included (splash, excluding primary unless noted) | Example ability |
|------------------------|---------------------------------------------------------|-----------------|
| **None** | ∅ | Throwing knife |
| **DiskChebyshev** | All cells with Chebyshev distance ≤ `radius` from primary, minus primary | Fireball (`radius = 2`) |
| **DiskManhattan** | Manhattan ball ≤ `radius` | Future frost nova |
| **LineFromCaster** | Straight line from `casterCell` through `primaryTile`, length ≤ `maxLength`, **excluding** caster | Charge / lightning bolt |
| **LineFromPrimary** | Ray from primary in facing direction | Future |
| **CustomAsset** | ScriptableObject implements `ISplashZoneShape` | Boss mechanics |

New shapes = new enum value + class; **no** changes to `TargetingReticleView` beyond calling the interface.

---

## 6. Visual specification (v0)

### F6.1 — Colors and layering

| Layer | Color | Tiles |
|-------|-------|-------|
| **Primary** | **White** (or current reticle prefab tint) | `primaryTile` only |
| **Splash preview** | **Red**, ~50–70% alpha | All splash cells from shape (§5.4 P3: **exclude** primary) |
| **Invalid / out of range** (optional v0.1) | Dim gray or hide red | No confirm; scroll stays open per Telekinesis pattern |

**Sort order:** Splash red **below** primary white (lower `sortingOrder`) so aim point stays visible.

### F6.2 — `TargetingReticleView` responsibilities

| Method | Behavior |
|--------|----------|
| `Show(primary, SplashZoneDefinition zone, SplashZoneContext ctx)` | Spawn primary + pool of splash markers |
| `Move(direction)` | Update primary; recompute splash set; reposition markers |
| `Hide()` | Return markers to pool / deactivate |

**Pooling:** Reuse splash marker GameObjects; avoid Instantiate per tile per frame.

### F6.3 — Procedural fallback (no art)

If prefabs unset:

- Primary: existing yellow/white fallback quad (current behavior), tint **white**
- Splash: smaller or same quad, tint **red** `Color(1, 0.2, 0.2, 0.65)`

### F6.4 — Future visual milestone (out of v0)

| Feature | Notes |
|---------|--------|
| `ISplashZoneVisualizer` | Replace quads with VFX prefabs per shape |
| Per-ability palette | e.g. holy = gold splash, poison = green |
| Pulse / edge-only highlight | Cosmetic only |

Document in backlog; **shape math unchanged**.

---

## 7. Runtime flow

### F7.1 — Enter targeting

When `EnterTargetingMode` (or inventory/bow variants) starts with an `AbilityAction` that has a non-empty splash zone:

1. Build `SplashZoneContext` (caster = active member anchor).
2. `reticleView.Show(primary: actor.GridPosition, zone, ctx)`.
3. Store `pendingTargetedAbility` + reference to same `AbilityAction` / zone for confirm validation.

### F7.2 — Reticle move

On each `Move`:

1. `primaryTile += direction` (existing).
2. `splashCells = shape.ComputeCells(ctx with updated primary)`.
3. Update red markers to match set (symmetric diff vs previous frame for efficiency).

### F7.3 — Confirm

1. Optional: `CanConfirm(primaryTile)` — range, line-of-sight (future).
2. `ability.Execute(user, primaryTile)` — ability internally calls **`SplashZoneResolver.GetCells`** (shared with preview) then `TargetingResolver.GetTargetsOnTiles(cells)` or equivalent.
3. `ExitTargetingMode` → hide all markers.

### F7.4 — Invalid confirm

If execute returns false: **no** turn spent; reticle **stays**; splash preview **unchanged** (match Telekinesis / scroll rules).

### F7.5 — Cancel

Existing `CancelTarget` — hide primary + splash; no consumption.

---

## 8. Combat resolution alignment

### F8.1 — Fireball migration

| Step | Action |
|------|--------|
| 1 | Add `SplashZone_Fireball_Disk2.asset` (`DiskChebyshev`, radius 2) |
| 2 | Point `Fireball_Standard` at zone asset |
| 3 | `FireballAbility`: `var cells = SplashZoneResolver.GetCells(...)`; damage all actors on those cells (footprint-aware, dedupe by `Owner`) |
| 4 | Remove direct `splashRadius` usage from ability code |

### F8.2 — Single-tile abilities

`SplashZoneShapeKind.None` → preview shows **white only**; resolution uses `GetTargetsOnTile(primary)` only.

### F8.3 — Line charge (future content)

Example: `LineFromCaster`, `maxLength = 5`:

- Primary reticle at distant end tile player selects.
- Red cells = line from caster to primary (clipped to length), **excluding** caster and primary (primary white).
- Damage: same cell list.

---

## 9. Range and validation (phased)

| Phase | Rule |
|-------|------|
| **v0** | Preview always shows shape at reticle (even out of range); confirm may still fail in ability (document per ability). |
| **v0.1** | If `AbilityAction.range > 0`, gray out red splash and block confirm when Manhattan(caster, primary) > range. |
| **v1** | LOS: splash cells blocked by walls excluded from red preview and damage. |

---

## 10. Integration map

| Component | Change |
|-----------|--------|
| `TargetingReticleView` | Multi-marker splash preview; accept zone + context |
| `PlayerCommandProcessor` | Pass ability/zone into `Show`; optional range check on confirm |
| `AbilityAction` | `splashZone` reference |
| `SplashZoneResolver` | Static or service: cells from definition + context |
| `TargetingResolver` | Add `GetTargetsInCells(IReadOnlyList<Vector3Int>)` (dedupe footprints) |
| `FireballAbility` | Use shared cell resolver |
| `TargetingData` stub | Remove or redirect to `SplashZoneDefinition` |

---

## 11. Content authoring

### Fireball (reference)

| Asset | Values |
|-------|--------|
| `SplashZone_Fireball.asset` | `DiskChebyshev`, `radius = 2`, exclude primary from red |
| `Fireball_Standard.asset` | `splashZone` → above |

### Throwing knife

| Asset | Values |
|-------|--------|
| `SplashZone_None.asset` | `None` — white reticle only |

### Future charge

| Asset | Values |
|-------|--------|
| `SplashZone_ChargeLine5.asset` | `LineFromCaster`, `maxLength = 5` |

---

## 12. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Fireball targeting shows **white** on reticle tile, **red** on other tiles in radius-2 disk. |
| **AC2** | Moving reticle updates red tiles immediately. |
| **AC3** | Confirm damages same actors as preview cells (footprint-aware). |
| **AC4** | Throwing knife / `splashRadius == 0` shows **no** red tiles. |
| **AC5** | Essence fireball and fireball scroll share identical preview + splash. |
| **AC6** | Adding a new `SplashZoneDefinition` asset does not require code changes to `TargetingReticleView` (only new shape kind if new algorithm). |
| **AC7** | Cancel hides white and red markers; confirm success hides all. |

---

## 13. Implementation checklist

| Item | Status |
|------|--------|
| `SplashZoneDefinition` + `SplashZoneShapeKind` + resolver | Done |
| Shape kinds (None, DiskChebyshev, DiskManhattan, LineFromCaster) | Done |
| `TargetingResolver.GetTargetsInCells` | Done |
| `TargetingReticleView` splash pool (red) + primary (white) | Done |
| `PlayerCommandProcessor` wires zone on enter targeting | Done |
| Migrate `Fireball_Standard` + `FireballAbility` | Done |
| Unit tests: shape cell sets (disk, line) | Done |
| Line shape + sample charge ability (future) | Pending content |
| `ISplashZoneVisualizer` / cast VFX (future) | Pending |

---

## 14. Traceability

| Request | Section |
|---------|---------|
| Multi-tile abilities like Fireball | §1, §8 |
| Single reticle | §2, §4 P1, §7 |
| White primary, red splash | §6 |
| Extensible shapes (line charge, etc.) | §5.4, §8.3 |
| Different visual effect later | §6.4, §4 P4 |
