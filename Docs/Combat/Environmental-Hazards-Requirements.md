# Environmental hazards — Requirements (DCSS-style)

**Environmental hazards** are **floor-cell** terrain effects inspired by **Dungeon Crawl Stone Soup**: some tiles **block passage** unless the mover meets a **condition** (v0: **Strength ≥ 50** instead of unimplemented fly/swim); others are **passable** but apply a **per-turn effect while occupied** (v0: **Poison Gas** — **1 damage** per trigger instead of **Poisoned** status until [status effects](Status-Effects-Requirements.md) ship).

v0 content: **Lava** (blocking) and **Poison Gas** (passable + confirm + occupancy damage). Extensible for deep water, clouds, etc.

**Depends on:** `MapManager`, `PlayerCommandProcessor`, `FormationRushService`, `PartyManager`, `TurnManager`, `CharacterStats` (`StatType.Strength`), `HealthComponent`, `DamageType`, [Auto-pickup confirmation](../Inventory/Auto-Pickup-Confirmation-Requirements.md) (move-gate UX), [Traps](Traps-Requirements.md) (distinct system — sprung traps vs terrain), [Status effects](Status-Effects-Requirements.md) (future: gas applies Poisoned).

**Related:** SampleScene floor tile `Scavengers_SpriteSheet_25` / sheet `fbe1e7b94bb1a4a099d5bcb15a00141c` for **underlying floor** under overlays.

**Explicitly out of scope (v0):** Flying, swimming, procedural hazard placement, enemy pathfinding around gas (except formation STR/lava rules), animated hazard tiles (static sprites OK), deep water tile.

---

## 1. Goals

**G1 — All hazards are floor cells**  
Every hazard is bound to a **`Vector3Int`** floor coordinate. No wall hazards in this spec.

**G2 — Two hazard families**  
**Passage** hazards block entry unless `PassageCondition` passes. **Persistent** hazards allow entry (with confirm) and harm actors **while they occupy** the cell across turns.

**G3 — Extensibility**  
New hazards = new **`EnvironmentalHazardDefinition`** assets + optional overlay art; core **`HazardService`** stays stable. Future **Fly** / **Swim** conditions replace or complement Strength checks without rewriting Lava.

**G4 — v0 placeholders for flight**  
Lava uses **Strength ≥ 50** (not fly). Document migration path to `PassageCondition.Fly` when implemented.

**G5 — Poison Gas without status system**  
Until Poisoned is coded, gas deals **1 damage** (thematically `DamageType.Poison`) on each occupancy trigger (§6).

**G6 — SampleScene**  
Place **Lava** and **Poison Gas** in **`SampleScene`** for manual QA (painted markers or hazard prefabs).

---

## 2. Design decision — poison gas tile vs component

### Question

Should a cell be a **“poison gas tile”** (monolithic terrain type), or should **poison gas be a component/overlay** on a normal floor cell?

### Recommendation (locked)

**Layered cell model — base floor + hazard registration + optional overlay.**  
Do **not** use a single tilemap cell that is *only* poison gas with no underlying floor.

| Layer | Role |
|-------|------|
| **Base floor** | `floorMap` keeps a normal walkable floor tile (visible ground). |
| **Hazard registry** | `HazardService` maps `Vector3Int` → `EnvironmentalHazardDefinition` + kind (passage vs persistent). |
| **Hazard overlay** | `hazardOverlayMap` (or sprites) draws lava or green gas **on top** of floor; does not replace walkability logic alone. |

| Approach | Verdict |
|----------|---------|
| **Overlay + registry (chosen)** | Matches DCSS “floor + cloud”; poison gas shows floor beneath cloud; data drives behavior. |
| **Whole-cell poison tile only** | Rejected — breaks future proc-gen (gas on varied floors), couples art to behavior, hard to reuse normal floor under cloud. |
| **Invisible metadata only, no overlay** | Rejected for v0 — player must see gas and lava. |

**Lava (v0):** May **replace** floor **visual** with lava art on overlay (or terrain layer), but cell is still a **hazard registration** with `PassageBlocked` rules — not merely a decorative sprite.

**Poison gas (v0):** **Base floor visible** + **gas overlay** + **persistent** registry entry.

**Implementation sketch:** `EnvironmentalHazardMarker` prefab at cell **or** painted `EnvironmentalHazardTile` on `HazardOverlay` tilemap whose `Tile` references `EnvironmentalHazardDefinition`.

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Hazard definition** | `EnvironmentalHazardDefinition` ScriptableObject. |
| **Passage hazard** | Blocks entry unless condition met (Lava v0). |
| **Persistent hazard** | Passable; effect while occupying (Poison Gas v0). |
| **Passage condition** | v0: `MinimumStrength(50)`; future: `Fly`, `Swim`, … |
| **Occupancy** | Actor’s anchor `GridPosition` equals hazard cell. |
| **Begin turn on cell** | Actor starts their action phase on that cell: **after move onto it**, or **Wait** without leaving. |
| **Party member** | `BaseActor` in `PartyManager.partyMembers`. |

---

## 4. Extensibility — hazard catalog

### D4.1 — `EnvironmentalHazardId`

```csharp
public enum EnvironmentalHazardId
{
    None = 0,
    Lava = 1,
    PoisonGas = 2,
    // Future: DeepWater, ToxicBog, ...
}
```

### D4.2 — `EnvironmentalHazardKind`

| Kind | Behavior |
|------|----------|
| **Passage** | Blocks move into cell if condition fails. |
| **Persistent** | Allows move (confirm); applies effect on occupancy triggers (§6). |

### D4.3 — `PassageCondition` (v0 + future)

| Condition | v0 | Parameter |
|-----------|-----|-----------|
| **MinimumStrength** | **Yes** (Lava) | `requiredStrength = 50` |
| **Fly** | No | — |
| **Swim** | No | — |
| **AlwaysAllow** | No | — |

`HazardPassageEvaluator.CanEnter(cell, actor)` evaluates conditions from definition.

### D4.4 — `EnvironmentalHazardDefinition` (base SO)

| Field | Purpose |
|-------|---------|
| `hazardId` | Enum |
| `displayName` | UI / dialogs |
| `kind` | Passage \| Persistent |
| `passageCondition` | For Passage kind |
| `requiredStrength` | When condition = MinimumStrength |
| `overlayTile` / `revealedSprite` | Visual on `hazardOverlayMap` |
| `underlyingFloorPreserves` | Poison gas: **true** (show floor under cloud) |
| `persistentDamagePerTrigger` | v0 gas: **1** |
| `persistentDamageType` | v0 gas: **Poison** |
| `futureStatusOnTrigger` | Optional `StatusEffectDefinition` when Poisoned exists |

Menu: **`JRogue/Hazards/Environmental Hazard`**.

### D4.5 — Future hazards (placeholder only)

| Hazard | Kind | Condition (future) |
|--------|------|-------------------|
| **Deep water** | Passage | Swim |
| **Lava (DCSS-accurate)** | Passage | Fly |
| **Toxic bog** | Persistent | Slow + poison status |

---

## 5. Passage hazards — Lava (v0)

### F5.1 — Behavior

| Rule | Detail |
|------|--------|
| **Entry** | Party member cannot **enter** lava cell unless `Strength.GetValue() >= 50`. |
| **Failed entry** | Move **rejected** (same as blocked wall); **no** turn consumed; log e.g. `[Hazard] {name} cannot enter Lava (STR {current} < 50).` |
| **Success** | Move proceeds; turn consumed as normal. |
| **Confirmation** | **None** for blocked attempt; optional message only. |
| **Enemies** | v0: same STR rule if enemies can path onto hazard cells; otherwise exclude from enemy wander. |

### F5.2 — Movement integration

Extend move validation **before** `TryMove`:

```text
if (!HazardPassageEvaluator.CanEnter(dest, mover)) → block move
```

`MapManager.IsWalkable(dest)` may still be **true** (lava is a floor cell with hazard metadata). **R5.2.1** Passage hazards **override** naive walkability for **entry**; lava cell is **not** walkable for weak actors.

### F5.3 — Formation

`FormationRushService.IsValidMove`: reject lava for followers **unless** that follower has STR ≥ 50 (per-actor check). Leader manual move uses same rule.

### F5.4 — Art

- Overlay/tile: **lava** sprite (§10).
- Underlying floor: optional hidden or charred; v0 may show lava art only on hazard layer.

### F5.5 — Authoring

| Asset | Fields |
|-------|--------|
| `EnvironmentalHazard_Lava.asset` | `kind = Passage`, `MinimumStrength`, `requiredStrength = 50`, `hazardId = Lava` |

---

## 6. Persistent hazards — Poison Gas (v0)

### F6.1 — Behavior summary

| Rule | Detail |
|------|--------|
| **Passage** | **Allowed** for all party members (no STR gate). |
| **Visual** | **Floor tile visible** under **green gas overlay**. |
| **Move confirm** | **Yes** — before entering gas tile (§6.3). |
| **Effect v0** | **1** `DamageType.Poison` damage per **occupancy trigger** (not Poisoned status). |
| **Future** | Replace damage with `StatusEffectService.TryApply(Poisoned)` per [status spec](Status-Effects-Requirements.md). |

### F6.2 — When damage fires (occupancy triggers)

Apply **1 damage** on each **occupancy trigger** for a party member on the gas cell:

| Trigger | When |
|---------|------|
| **T1 — Enter** | Immediately **after** successful move onto gas (post-confirm **Yes**). Counts as “begin turn on cell” when move was their action for the phase. |
| **T2 — Wait on gas** | Actor uses **Wait** while `GridPosition` is gas cell → damage when wait resolves (turn consumed, still on gas). |
| **T3 — Start of turn still on gas** | Each **player phase** `NotifyPartyTurnStart`: for each member occupying gas, apply damage **once** at that boundary. |

**R6.2.1 — No double-dip on enter + same phase start**  
If T1 fired because member **moved** onto gas and that move **ended** their action for the phase, **do not** also fire T3 for that same member in the **same** `NotifyPartyTurnStart` that already passed earlier in the phase. T3 applies on **later** player phases while still occupying gas.

**R6.2.2 — Enter then next phase**  
Member ends turn on gas → next player phase T3 fires → 1 damage.

### F6.3 — Move confirmation (enter gas)

Mirror [trap](Traps-Requirements.md) / [auto-pickup confirm](../Inventory/Auto-Pickup-Confirmation-Requirements.md):

**Template:**

```text
{actorName} is about to enter {displayName}. Entering may harm you each turn you remain inside. Continue?
```

**No / Esc:** no move, no turn. **Yes:** move, T1 damage, turn consumed if move was action.

**Gate order** (before `TryMove`): Trap (visible) → Essence → **Hazard persistent confirm** → Auto-pickup confirm.

### F6.4 — Enemies in gas (v0)

| Rule | Detail |
|------|--------|
| **Damage** | If enemy ends turn on gas, apply **1** Poison damage at **enemy turn start** (parallel to status tick timing). |
| **Confirm** | **No** dialog for AI. |
| **Pathing** | v0: enemies may enter gas; no special avoidance. |

### F6.5 — Undead / resistance

Poison damage uses `HealthComponent` + `GetResistance(Poison)`; Undead high resistance reduces net damage. **Poisoned status** not applied in v0.

### F6.6 — Authoring

| Asset | Fields |
|-------|--------|
| `EnvironmentalHazard_PoisonGas.asset` | `kind = Persistent`, `persistentDamagePerTrigger = 1`, `persistentDamageType = Poison`, `underlyingFloorPreserves = true` |

---

## 7. Runtime architecture

### D7.1 — `HazardService`

| API | Purpose |
|-----|---------|
| `GetHazardAt(Vector3Int cell)` | Definition + kind |
| `bool CanEnter(cell, BaseActor actor)` | Passage evaluator |
| `bool RequiresEnterConfirm(cell)` | Persistent hazards |
| `void OnActorEntered(cell, actor)` | T1 |
| `void OnActorWaitOnCell(actor)` | T2 |
| `void TickOccupancyOnPlayerPhaseStart()` | T3 for all party members |
| `void TickOccupancyOnEnemyTurnStart(EnemyController)` | Enemy in gas |

### D7.2 — Tilemaps (suggested scene layout)

| Layer | Content |
|-------|---------|
| `Floor_Layer` | Existing walkable floors |
| `Wall_Layer` | Walls |
| **`Hazard_Overlay`** | Lava + gas sprites (sorting above floor) |

Registry can be built from overlay tiles at bake time or from `EnvironmentalHazardMarker` prefabs in SampleScene.

### D7.3 — Distinction from traps

| | **Trap** | **Environmental hazard** |
|--|----------|---------------------------|
| Trigger | Step on sprung trap | Terrain rule / occupancy |
| Hidden traps | Yes | No (always visible overlay) |
| Lava | — | Passage block |
| Gas | — | Persistent cloud |

---

## 8. SampleScene placement

| Hazard | Suggested test |
|--------|----------------|
| **Lava** | 2×2 pool blocking a corridor; STR &lt; 50 party member cannot enter; STR ≥ 50 can. |
| **Poison Gas** | Corridor cloud; confirm on enter; damage on wait and on next turn if standing still. |

Document tile coordinates in scene note or optional `Docs/Combat/Environmental-Hazards-SampleScene.md`.

---

## 9. Functional acceptance (F9.x)

**F9.1 — Lava blocks weak**  
STR 10 member cannot enter; move fails, no turn.

**F9.2 — Lava allows strong**  
STR 50+ member enters; move succeeds.

**F9.3 — Gas confirm cancel**  
Cancel dialog → no move, no damage.

**F9.4 — Gas enter damage**  
Yes → move onto gas → 1 damage.

**F9.5 — Gas wait damage**  
Wait on gas → 1 damage.

**F9.6 — Gas next phase**  
Stand on gas through phase end → next player phase → 1 damage (T3).

**F9.7 — Floor visible under gas**  
Overlay renders; base floor tile still present on `floorMap`.

**F9.8 — Formation**  
Follower rush avoids lava if STR &lt; 50; may path through gas (v0).

---

## 10. Art — Dungeon Crawl 32×32 (Option A, approved)

**Status:** **Imported** (product approval 2026-05-25). **Lava passage threshold:** **Strength ≥ 50** (locked).

| | |
|--|--|
| **License** | `Assets/Art/Hazards/ThirdParty/DungeonCrawl32/LICENSE.txt` — [OGA Dungeon Crawl 32×32 tiles](https://opengameart.org/content/dungeon-crawl-32x32-tiles) |
| **Originals** | `Assets/Art/Hazards/ThirdParty/DungeonCrawl32/originals/` |
| **Unity sprites** | `Assets/Art/Hazards/Sprites/` |

### Locked sprite mapping

| Hazard | DCSS source | Sprite |
|--------|-------------|--------|
| **Lava** | `dc-dngn/floor/lava0.png` | `LavaTile.png` |
| **Poison gas overlay** | `effect/cloud_poison1.png` | `PoisonGasOverlay.png` |
| Alt (optional animation later) | `cloud_poison0.png` | `PoisonGasOverlay_Alt0.png` |

Also on disk: `lava1.png`, `cloud_miasma.png` (not assigned in v0).

Unity: **PPU 32**, **Point** filter — see `ThirdParty/DungeonCrawl32/README.md`.

---

## 11. Implementation status

| Deliverable | Status |
|-------------|--------|
| `EnvironmentalHazardDefinition` | **Done** — `Assets/Scripts/Hazards/` |
| `HazardService` / passage evaluator | **Done** |
| `HazardMoveGate` + `HazardConfirmDialogUI` (gas confirm) | **Done** |
| `EnvironmentalHazard_Lava` / `_PoisonGas` assets | **Done** — `Assets/Resources/Hazards/` |
| SampleScene placements | **Done** — add `SampleSceneHazardPlacements` to scene (see [Environmental-Hazards-SampleScene.md](Environmental-Hazards-SampleScene.md)) |
| Hazard sprites (DCSS Option A) | **Imported** — §10 |
| Unit tests | **Done** — `EnvironmentalHazardTests` |
| Lava `requiredStrength` | **50** (locked) |
| Fly / Swim conditions | **Future** |
| Poison Gas → Poisoned status | **Future** ([status spec](Status-Effects-Requirements.md)) |

---

## 12. Traceability

| Request | Section |
|---------|---------|
| DCSS-inspired floor hazards | §1, §5–6 |
| Passage condition (STR ≥ 50 v0) | §4.3, §5 |
| Persistent gas, floor visible, confirm enter | §2, §6 |
| Damage 1 instead of poison status | §6.1 |
| Begin turn on tile (move / wait) | §6.2 |
| Lava + Poison Gas content | §5, §6 |
| Poison gas tile vs component question | §2 |
| Sprites — ask before download | §10 |
