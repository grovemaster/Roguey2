# Lighting — Requirements

Per-party-member **sight range** (grid LOS distance) combines with a per-cell **light model** (emitters, receivers, overhead/day–night ambient, carried light sources) to decide what the player **sees live** versus what appears as a **dark tile** (in range but too dim). **Dark Vision** is a first-class capability (racial trait, essence, future buffs) that improves vision in low light and is designed so **magical darkness** can cap it later. **Fog of war** stores a **lighting snapshot** when a cell was last seen; off-screen lighting changes do **not** update explored memory ([Fog of War](Fog-Of-War-Requirements.md) §5).

**Depends on:** `CharacterStats` (`sight` stat), `PartyManager`, `ShadowCaster`, `VisibilityManager`, `MapManager`, `TurnManager`, `EssenceData` / `EssenceSlotManager`, racial trait / `BodyCapabilityFlags` (or dedicated flags), [Fog of War](Fog-Of-War-Requirements.md), [Interactable tiles](../Combat/Interactable-Tiles-Requirements.md) (wall-torch activation), [Inventory](../Inventory/Inventory-UI-Redesign-Requirements.md) (torch items), `EnemyAiBrain` / `SenseSightService` (enemy sight + alert).

**Related:** *Surviving the Game as a Barbarian* — overhead/day–night floors; DCSS — torches, LOS, fog memory (terrain frozen off-screen). **SampleScene QA + wall/carried torch v0:** [Lighting QA and Torch v0](Lighting-QA-And-Torch-v0-Requirements.md).

**Explicitly out of scope (v0):** **Magical darkness** gameplay (zones that override Dark Vision); full save/load of per-cell light state; dynamic light propagation through open doors (optional phase 2); colored light / mood lighting; light-based stealth damage modifiers.

---

## 1. Goals

**G1 — Per-member sight range**  
Each party member uses their own **`CharacterStats.sight`** (tiles) as the base **LOS radius** for lighting + fog, not a single global `VisibilityManager.viewRange` (supersedes [Fog of War](Fog-Of-War-Requirements.md) **G8** for production).

**G2 — Tile light emitters and receivers**  
Cells may **emit** light (torch wall, luminescent fungus), **receive** light (floor/wall affected by nearby emitters or overhead ambient), or both. Emitted intensity is **data-authored** and **mutable at runtime** (events, player lighting a torch).

**G3 — Overhead / day–night ambient**  
Some floors or regions expose **light from above** with optional **turn-based day/night cycles** (variable period **X** turns per phase).

**G4 — Player light sources**  
Carried items (e.g. **torch**) and **conditional tile changes** (light an unlit wall torch while holding a light source) add or enable emitters.

**G5 — Dark Vision**  
Defined capability improving vision in darkness (race + essence + extensible list). Must respect future **magical darkness** caps without rewriting core math.

**G6 — Presentation: dark tiles**  
Tiles within geometric LOS and sight range but **below** the effective light threshold render as **dark tiles** (distinct from fog **unseen** and **explored** memory).

**G7 — Fog memory freezes lighting**  
When a cell is **explored**, the stored snapshot includes **light levels at last visibility**; live torch extinguish off-screen does **not** dim explored memory.

**G8 — Enemy alert from player light**  
Enemies can enter **alert** when a **party light source** is detectable within that enemy’s **sight range** (existing cone/LOS AI), separate from seeing the actor silhouette.

**G9 — Extensible registry**  
Lighting uses **cell metadata + overlay** (same discipline as traps, hazards, interactables) — not a monolithic special floor tile type.

---

## 2. Relationship to fog of war

| System | Responsibility |
|--------|----------------|
| **Lighting** | How bright a cell is; whether a cell in LOS is **lit** vs **dark tile**; effective sight modifiers from Dark Vision; emitter/receiver rules. |
| **Fog of war** | **Unseen / Explored / Visible** knowledge; terrain snapshot; entity gating. |

**Integration (locked):**

1. **Geometric LOS** — Still `ShadowCaster` from each member origin out to **effective sight range** (§5).
2. **Lit visibility** — Subset of LOS cells that pass §7 visibility rules → drive **live** presentation and **Visible** fog state.
3. **Snapshot** — When a cell becomes **Visible**, fog captures terrain **and** lighting fields (§9). When it becomes **Explored**, lighting in memory is **frozen**.
4. **Explored tiles** — Render terrain + **snapshot lighting** (dimmed per FoW `memColor`); **do not** poll live `LightingService` for explored cells.

**Supersedes:** FoW doc “per-character light radius from stats (use global `viewRange` only)” — lighting milestone makes **`CharacterStats.sight`** authoritative per member.

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Sight range** | Max tile distance for shadow-cast LOS from a party member (`CharacterStats.sight` + modifiers). |
| **Light level** | Non-negative scalar on a cell (integer tiers v0: **0–255** or **0–10** — implementer picks one scale and locks it). |
| **Emitter** | Cell that **outputs** light into the map (torch, luminescent wall). |
| **Receiver** | Cell whose **received** light is computed from emitters + ambient (typical floor). |
| **Ambient (overhead)** | Region or floor-wide baseline light not tied to a single emitter (day/night). |
| **Received light** | Computed intensity at a receiver after falloff / aggregation (§6). |
| **Emission** | Current `emitLight` on an emitter cell (may be 0 if unlit). |
| **Dark Vision** | Capability that lowers the light threshold and/or extends effective sight in low ambient (§8). |
| **Magical darkness** | Future zone flag that caps Dark Vision and imposes a minimum light requirement (§8.4). |
| **Dark tile** | In LOS and sight range but **underlit** — rendered with `darkTileColor`, not full brightness. |
| **Party light source** | Any light originating from the party: member **carried** emitter, member **occupied** tile bonus, or party-activated **tile** emitter. |
| **Lighting snapshot** | `{ emitLight, receivedLight, ambientContribution }` stored in fog cell knowledge when last **Visible**. |
| **Day/night cycle** | Scheduled change to regional **ambient** every **X** turns (X per cycle definition). |

---

## 4. Design decision — lighting data on cells

### Recommendation (locked)

**Per-cell lighting registry** on walkable / wall coordinates; optional **overlay** for torch flame / glow VFX. Do **not** encode all lighting in `floorMap` tile type alone.

| Layer | Role |
|-------|------|
| **`LightingService`** | Singleton service: queries, recompute received light, day/night tick, snapshot helpers. |
| **`LightCellData`** | Per `Vector3Int`: role flags (emitter/receiver), base emission, ambient region id, runtime state. |
| **`LightEmitterDefinition`** | ScriptableObject: max emission, falloff radius, falloff curve, blocks LOS (torches usually **no**). |
| **Overlay / tint** | `VisibilityManager` or dedicated overlay applies `visibleColor` / `darkTileColor` / fog colors. |

**Consequences**

- Procedural floors attach definitions at bake/spawn time.
- Events call `LightingService.SetEmission(cell, value)` or `EnableEmitter(cell, definition)`.
- [Interactable wall torch](../Combat/Interactable-Tiles-Requirements.md) effect implements “light torch” by raising emission when precondition passes (§10).

---

## 5. Sight range (per party member)

### R5.1 — Base value

| Source | Rule |
|--------|------|
| **Primary** | `CharacterStats.sight.GetValue()` (grid tiles, integer). |
| **Fallback** | If party system missing, use `VisibilityManager.viewRange` dev-only. |

### R5.2 — Modifiers (v0)

| Modifier | When applied |
|----------|----------------|
| **Dark Vision — range bonus** | While computing LOS for that member, add `darkVisionSightBonusTiles` in regions where **ambient + local received** at member’s cell is below `lowLightThreshold` (§8). |
| **Status / buff** | Future: register in same pipeline as `Stat` modifiers. |
| **Magical darkness** | Future: **no** bonus inside `MagicalDarkness` zones (§8.4). |

### R5.3 — Party union

For **display** and **fog Visible set**, union **lit-visible** cells from **every** active party member (each with their own sight range and Dark Vision).

### R5.4 — Combat tile-LOS

`CombatThreatCoordinator` tile-LOS uses the **same per-member sight** values as lighting (no global-only `viewRange` in production). Remote sense / scrying remains governed by [Fog of War](Fog-Of-War-Requirements.md) §12 (not lighting).

---

## 6. Light levels and propagation (v0)

### D6.1 — Scale

Use a single project-wide **`LightLevel`** integer (**0 = pitch dark** for gameplay thresholds). v0 recommendation: **0–10** tiers for designer-friendly authoring (torch = 6, luminescent wall = 4, full daylight ambient = 10).

### D6.2 — Emitter

| Field | Meaning |
|-------|---------|
| **`baseEmissionMax`** | Max `emitLight` when fully on (definition). |
| **`emitLight` (runtime)** | Current output, `0 … baseEmissionMax`. |
| **`falloffRadius`** | Chebyshev or Manhattan radius (locked v0: **Manhattan** to match grid). |
| **`falloffPerTile`** | Subtracted (or divided) per tile distance from emitter. |

**Received contribution from one emitter at cell `C`:**

```text
contrib = max(0, emitLight_at_E - falloffPerTile * distance(E, C))
```

**Multiple emitters:** `receivedFromEmitters = sum(contribs)` capped at `maxLightLevel` (v0: **sum capped**, not physically accurate — document for possible “max only” phase 2).

### D6.3 — Ambient (overhead)

| Field | Meaning |
|-------|---------|
| **`AmbientRegion`** | Id + `currentAmbientLight` + optional cycle schedule. |
| **Floor default** | Each floor may reference one ambient region (STGaAB-style surface floors). |

**Received light at cell `C`:**

```text
receivedLight(C) = min(maxLight, receivedFromEmitters(C) + ambientAtRegion(C))
```

Cells without explicit receiver flag still use **receiver** behavior if they have a floor tile (default **receiver = true**).

### D6.4 — Day/night cycle

| Field | Meaning |
|-------|---------|
| **`cycleLengthTurns` (X)** | Turns until next phase (variable per region / season). |
| **`phases[]`** | Ordered `{ ambientLight, durationTurns }` or keyframed curve. |
| **Tick** | On **`TurnManager`** player-phase boundary (or global turn counter), decrement timer; on expiry advance phase and set `currentAmbientLight`. |

**Debug log:** `[Lighting:Cycle] Region {id} → ambient {level} (turn {n})`.

Implementers store **next transition turn** on `LightingService` or region instance.

### D6.5 — Recompute triggers

| Event | Action |
|-------|--------|
| Party move / turn complete | Recompute **live** lit set for all members |
| Emitter `emitLight` changed | Invalidate affected region (bounding box of falloff) |
| Ambient phase change | Recompute all receivers in region |
| Floor load | Build registry from scene markers / baked data |

**Performance v0:** Full recompute on small SampleScene maps is acceptable; later: dirty regions only.

---

## 7. Visibility rules (lit vs dark tile)

Evaluation order for cell `C` for party member `M` (then union across party):

### R7.1 — Always visible (full brightness)

| Condition | Rule |
|-----------|------|
| **Occupied** | Any party member standing on `C` → **always visible** at full brightness (ignores underlit). |
| **Emitter in range** | `C` is an **emitter** with `emitLight > 0` and `C` is within **`M`’s geometric LOS** and **sight range** → **always visible** (show lit torch / glowing wall). |

### R7.2 — Receiver (normal case)

`C` is in **`M`’s LOS** and within **`M`’s sight range** AND:

```text
receivedLight(C) >= effectiveLightThreshold(M, C)
```

→ **fully visible** (live terrain + entities per fog rules).

Else if in LOS and sight range but below threshold → **dark tile** (§7.4).

### R7.3 — Effective light threshold

```text
effectiveLightThreshold(M, C) = baseVisibilityThreshold
    - darkVisionThresholdReduction(M)   // if M has Dark Vision
    - optionalFutureBuffs
```

Clamp minimum **0**. **`baseVisibilityThreshold`** — global tuning constant (v0 suggest **3** on 0–10 scale).

**Dark Vision does not bypass** future **`MagicalDarkness`** minimum (§8.4).

### R7.4 — Dark tile presentation

| Property | Rule |
|----------|------|
| **Fog state** | Still **Visible** for knowledge (player “knows” the cell exists in LOS) OR treat as visible-with-dim — **locked: fog state = Visible**, presentation = dark tile tint. |
| **Tint** | `darkTileColor` (serialized, e.g. deep blue-grey ~15–25% brightness). |
| **Entities** | v0: **hide** or **silhouette** creatures on dark tiles — **locked: hide** non-party entities on underlit cells (player must improve light or move closer). Party members on adjacent lit cells still shown on their own tiles per R7.1. |
| **Snapshot** | Store `receivedLight` and `emitLight` at full visibility capture even if presentation is dark (for consistency when player gains light later). |

### R7.5 — Outside LOS or sight range

Governed only by [Fog of War](Fog-Of-War-Requirements.md): **unseen** or **explored** — lighting does not reveal.

---

## 8. Dark Vision

### D8.1 — Definition

**Dark Vision** is the ability to function in **low light**: treated as seeing **farther in darkness** by (a) **lowering** the light level required to treat cells as fully lit, and (b) optionally **extending sight range** when the observer is in a **low-light** cell.

Not the same as **omnidirectional LOS through walls** or **fog removal** without LOS.

### D8.2 — Capability sources (v0 design)

| Source | Authoring |
|--------|-----------|
| **Racial trait** | e.g. `BodyCapabilityFlags.DarkVision` on `CharacterStats` / race definition; or racial passive ScriptableObject with `darkVisionSightBonusTiles`, `darkVisionThresholdReduction`. |
| **Essence** | `EssenceData` grants the same capability while equipped (OR with racial). |
| **Future** | Spells, items, status effects register on `CharacterStats` lighting contribution dictionary (mirror body-equipment contribution pattern). |

**Suggested essence path:** `Assets/Resources/Item/Essence/DarkVision.asset` + optional stat modifiers.

### D8.3 — Parameters (per actor)

| Parameter | Typical use |
|-----------|-------------|
| **`hasDarkVision`** | Bool OR capability flag. |
| **`darkVisionThresholdReduction`** | Subtract from `effectiveLightThreshold` (e.g. **2** on 0–10 scale). |
| **`darkVisionSightBonusTiles`** | Add to `sight` when ambient at actor cell `< lowLightThreshold` (e.g. **+2** tiles). |
| **`lowLightThreshold`** | Ambient + received at actor cell below this counts as “darkness” for bonus (e.g. **4**). |

### D8.4 — Extensibility: magical darkness (future)

Reserve on cell or region:

| Field | Purpose |
|-------|---------|
| **`MagicalDarknessStrength`** | `0` = none; `>0` imposes floor on required light. |
| **`darkVisionAllowed`** | If **false**, Dark Vision grants **no** threshold reduction in this zone. |
| **`maxDarkVisionSightBonus`** | Cap bonus tiles inside zone (often **0**). |

**API shape (implement even if unused in v0):**

```csharp
int GetEffectiveLightThreshold(BaseActor member, Vector3Int cell, int baseThreshold);
int GetEffectiveSightRange(BaseActor member, Vector3Int originCell);
```

`LightingService` / `DarkVisionResolver` consults region flags **before** applying racial/essence bonuses.

---

## 9. Fog of war — lighting snapshot (locked)

When cell `C` transitions to **Visible** (enters lit-visible or dark-tile-visible set per policy):

| Snapshot field | Source |
|----------------|--------|
| `snapshotEmitLight` | `emitLight` at `C` if emitter else 0 |
| `snapshotReceivedLight` | `receivedLight(C)` at capture time |
| `snapshotAmbient` | Regional ambient at capture time |
| `presentationWasDarkTile` | Whether live render used `darkTileColor` |

When `C` becomes **Explored**:

- **Do not** update snapshot lighting when live world changes (torch extinguished, day→night).
- Render explored terrain using **snapshot** lighting to pick tint (dim memory consistent with last seen).

When `C` becomes **Visible** again:

- Refresh snapshot from **live** `LightingService`.
- Live presentation uses current light, not stale snapshot.

**Debug log:** `[Lighting:Fog] Snapshot frozen at {C} emit={e} recv={r}` on explore transition.

---

## 10. Player light sources and tile interaction

### R10.1 — Carried items (torch)

| Field | Requirement |
|-------|-------------|
| **Item category** | `ItemCategory.Accessory` with light-source fields — see [Lighting QA and Torch v0](Lighting-QA-And-Torch-v0-Requirements.md) §8. |
| **Behavior** | While **equipped** in an **accessory slot** and **lit**, attach **virtual emitter** centered on bearer’s `GridPosition`. |
| **`emitLight`** | From definition (e.g. **6**). |
| **Falloff** | Short radius (e.g. **3** Manhattan). |
| **Turn cost** | Lighting/extinguishing torch may cost action (designer-authored); moving with lit torch does not re-light each turn. |

### R10.2 — Light a wall torch (tile change)

| Precondition | Rule |
|--------------|------|
| **Player has active light source** | Carried torch (or spell) with `canIgniteTiles = true`. |
| **Target cell** | Interactable or bump-activated **unlit wall torch** emitter with `emitLight == 0`. |
| **Action** | Bump or Use → set `emitLight` to definition max; consume player action; optional consume torch charge. |

Implement via [Interactable tiles](../Combat/Interactable-Tiles-Requirements.md) **effect** type: `SetTileEmissionEffect`.

### R10.3 — Events changing emission

Any system may call:

```csharp
LightingService.SetEmission(Vector3Int cell, int level);
LightingService.EnableEmitter(Vector3Int cell, LightEmitterDefinition def);
```

Examples: lever opens door revealing lit room; quest script extinguishes all torches in hall; trap douses light.

**Debug log:** `[Lighting:Emit] {cell} → {level} (reason: {id})`.

---

## 11. Enemy alert from player light

### R11.1 — Detection

When evaluating enemy `E` sight each turn or on player action:

1. Build **party light origins**: each member with active carried emitter; each party-enabled tile emitter with `emitLight > 0`; optionally member occupied cell if treated as faint glow (v0: **carried + tile emitters only**).
2. For each origin `L`, if `L` is within **`E`’s vision range** and **enemy cone/LOS** rules (`SenseSightService`) → enemy detects **light**, even if actor body not seen.
3. Transition to **Suspicious** or **Alert** per `EnemyAiBrain` policy (v0: **Alert** with reason `"party_light"`).

### R11.2 — Intensity (optional v0)

Stronger `emitLight` may increase detection range slightly (v0: **optional**, default **same range** as sight).

### R11.3 — Dark tiles

Underlit party members on **dark tiles** do not add extra light beyond their tile occupancy rule (R7.1 shows member on cell; light detection uses explicit emitters).

**Debug log:** `[Lighting:Alert] {enemy} alerted by light at {cell} level={n}`.

---

## 12. Content examples (authoring)

| Feature | Emitter | Receiver | Ambient | Notes |
|---------|---------|----------|---------|-------|
| **Lit wall torch** | Yes, `emitLight 6`, falloff 4 | Wall cell | 0 | Player can ignite |
| **Unlit wall torch** | Yes, `emitLight 0` until lit | — | 0 | Interactable |
| **Luminescent wall** | Yes, `emitLight 4` permanent | — | 0 | Weaker than torch |
| **Cave floor** | No | Yes | Region **2** | Low ambient |
| **Surface floor (STGaAB)** | No | Yes | Region cycle **10 → 3 → 10** | Day/night X variable |
| **Party torch** | Virtual on bearer | — | — | Item-driven |

---

## 13. Current implementation (audit)

| Area | Today | Gap |
|------|--------|-----|
| **`CharacterStats.sight`** | Per-actor stat default **8**. | Not used by `VisibilityManager` (uses global `viewRange`). |
| **`VisibilityManager`** | Binary fog tint; shadow cast. | No light levels, dark tiles, or per-member range. |
| **Dark Vision** | **Undefined** in code. | §8 |
| **Tile emitters / ambient** | **None**. | §4, §6 |
| **Day/night** | **None**. | §6.4 |
| **FoW snapshot** | **None** (explored not implemented). | §9 + [Fog of War](Fog-Of-War-Requirements.md) |
| **Enemy light alert** | Sight/hearing only. | §11 |

---

## 14. Implementation checklist

- [ ] `LightLevel` scale + `LightEmitterDefinition` ScriptableObject
- [ ] `LightingService` registry (emitters, receivers, ambient regions)
- [ ] `DarkVisionResolver` + `BodyCapabilityFlags.DarkVision` (or racial SO)
- [ ] Essence asset hook for Dark Vision
- [ ] `GetEffectiveSightRange` / `GetEffectiveLightThreshold` with magical darkness stubs
- [ ] Integrate per-member sight into `ShadowCaster` origin loop (replace global-only path)
- [ ] `darkTileColor` + apply in visibility refresh
- [ ] Extend fog cell knowledge with lighting snapshot (§9) when FoW milestone lands
- [ ] Day/night turn tick on `TurnManager`
- [ ] Torch item + wall-torch interactable effect
- [ ] `EnemyAiBrain` party-light alert hook
- [ ] SampleScene: cave (low ambient), torch wall, one day/night region
- [ ] Debug logs: `[Lighting:…]` prefixes per §6.5, §9, §10, §11

---

## 15. Debug logging contract

| Prefix | When |
|--------|------|
| `[Lighting:Sight]` | Effective range for member at origin |
| `[Lighting:Receive]` | Recompute region (verbose dev flag) |
| `[Lighting:Emit]` | Emission changed |
| `[Lighting:Cycle]` | Ambient phase advance |
| `[Lighting:DarkTile]` | Cell in LOS but under threshold |
| `[Lighting:Fog]` | Snapshot capture / freeze |
| `[Lighting:Alert]` | Enemy alerted by party light |
| `[Lighting:DarkVision]` | Threshold/range bonus applied or blocked by magical darkness |

---

## 16. Open questions (defaults if silent)

| Question | v0 default |
|----------|------------|
| Falloff metric | **Manhattan** distance |
| Multi-emitter combine | **Sum capped** at max tier |
| Dark tile fog state | **Visible** with dim tint |
| Entities on dark tiles | **Hidden** |
| Torch slot | **Equipped** item with `LightSourceDefinition` |
| `lowLightThreshold` | **4** on 0–10 scale |
| `baseVisibilityThreshold` | **3** |

---

## 17. Document history

| Date | Note |
|------|------|
| 2026-05-25 | Initial lighting requirements |
