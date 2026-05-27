# Lighting — Future Backlog

Backlog for gameplay lighting (not the QA scenario harness). Sourced from [Lighting-Requirements.md](Lighting-Requirements.md), current codebase audit, and [Fog-Of-War-Requirements.md](Fog-Of-War-Requirements.md) integration notes.

Use this list to draft implementation prompts. Suggested build order is at the end.

---

## Already done (QA only — not gameplay lighting)

- `LightingScenarioDefinition` + Phase 1–6 scenario assets
- `LightingScenarioController` (enable/disable `LightingPhase_*` roots)
- Editor: QA pack creator, SampleScene bootstrap, inspector Prev/Next/Apply
- Harness log: `[Lighting:Scenario] Applied …`

---

## Phase A — Core data & service

1. **`LightLevel` scale** — Lock 0–10 (or 0–255) project-wide; document in one place.
2. **`LightEmitterDefinition` ScriptableObject** — `baseEmissionMax`, `falloffRadius`, Manhattan `falloffPerTile`, optional `blocksLos` (default false).
3. **`LightCellData` / per-cell registry** — Emitter/receiver flags, runtime `emitLight`, ambient region id; default receiver on floor tiles.
4. **`LightingService` singleton** — Registry build on floor load; queries `GetReceivedLight`, `GetEmitLight`, `SetEmission`, `EnableEmitter`; recompute API.
5. **Propagation math (v0)** — Manhattan falloff; multi-emitter **sum capped** at max tier.
6. **`AmbientRegion` + floor default** — Id, `currentAmbientLight`, optional phase schedule fields.
7. **Scene/bake markers** — Attach lighting data at spawn (procedural + SampleScene markers), not only `floorMap` tile type.
8. **Recompute triggers** — Party move/turn complete, emission change (dirty bbox), ambient phase change, floor load.
9. **Debug logging (core)** — `[Lighting:Receive]`, `[Lighting:Emit]` (+ verbose recompute flag).

---

## Phase B — Per-member sight (G1, §5, FoW supersede)

10. **`VisibilityManager` uses `CharacterStats.sight`** per party member — Replace production reliance on global `viewRange` (keep dev fallback).
11. **Party union LOS** — `ShadowCaster` per member; union geometric LOS for fog/combat display baseline.
12. **`CombatThreatCoordinator` tile-LOS** — Same per-member sight as lighting (align with FoW G8).
13. **`GetEffectiveSightRange(member, origin)`** — Pipeline for Dark Vision bonus + future status buffs.
14. **Debug** — `[Lighting:Sight]` effective range per member at origin.

---

## Phase C — Lit vs dark tile presentation (G6, §7)

15. **`baseVisibilityThreshold`** — Global tuning constant (doc default: 3 on 0–10).
16. **`GetEffectiveLightThreshold(member, cell)`** — Dark Vision reduction + magical-darkness stubs (API even if unused).
17. **Lit-visible set** — Per member: R7.1 always-visible rules + R7.2 receiver threshold; union across party.
18. **`darkTileColor`** — Serialized on visibility/overlay; apply in refresh.
19. **Dark tile rules** — In LOS + sight but under threshold → **Visible** fog state, dim tint (not unseen).
20. **Entities on dark tiles** — Hide non-party entities on underlit cells (v0 locked).
21. **Emitter-in-LOS always full bright** — Lit torch/glow wall visible in range even if receiver math would dim.
22. **Occupied cell always full bright** — Party member tile ignores underlit presentation.
23. **Debug** — `[Lighting:DarkTile]` when cell is LOS-visible but under threshold.

---

## Phase D — Fog integration (G7, §9 — depends on FoW v0)

24. **Extend terrain/cell knowledge** — `snapshotEmitLight`, `snapshotReceivedLight`, `snapshotAmbient`, `presentationWasDarkTile`.
25. **Capture on Visible** — Snapshot lighting when cell enters visible set (with terrain snapshot).
26. **Freeze on Explored** — Do not update lighting snapshot when live world changes off-screen.
27. **Explored render** — Use **snapshot** lighting for explored tint, not live `LightingService`.
28. **Re-visible refresh** — When cell becomes Visible again, refresh lighting from live service.
29. **Debug** — `[Lighting:Fog]` on explore freeze / snapshot capture.

---

## Phase E — Day/night (G3, §6.4 — Phase 5 scenario)

30. **Ambient phase schedule** — `phases[]` with `ambientLight` + `durationTurns`; `cycleLengthTurns`.
31. **Turn tick** — On `TurnManager` player-phase boundary; advance phase; set `currentAmbientLight`.
32. **Recompute on phase change** — All receivers in affected region.
33. **Debug** — `[Lighting:Cycle] Region {id} → ambient {level} (turn {n})`.
34. **SampleScene Phase 5 content** — Surface region with cycle e.g. **10 → 3 → 10**; visible bright/dim shift when playing.

---

## Phase F — Tile emitters & content (G2, §12 — Phases 1–3 scenarios)

35. **Lit / unlit wall torch** — Emitter cells; unlit `emitLight == 0` until ignited.
36. **Luminescent wall** — Permanent weaker emitter (e.g. 4 vs torch 6).
37. **Cave floor region** — Low ambient (e.g. 2), receivers only.
38. **`SetTileEmissionEffect`** — Interactable effect calling `LightingService.SetEmission`.
39. **Lever/quest/trap hooks** — Any system can change emission with reason id for logs.
40. **Torch flame / glow overlay** — Optional VFX layer (registry + overlay painter pattern).
41. **SampleScene geometry** — Under each `LightingPhase_*` root: Phase 1 core rooms, Phase 2 fog-memory layout, Phase 3 runtime torch, etc.

---

## Phase G — Player light sources (G4, §10 — Phase 4 scenario)

42. **`LightSourceDefinition`** — Item-linked virtual emitter (level, radius, falloff).
43. **Carried torch item** — Equipped/active slot policy; virtual emitter on bearer `GridPosition`.
44. **Ignite wall torch precondition** — Active light source with `canIgniteTiles`; bump/use sets emission.
45. **Turn/action cost** — Light/extinguish torch (designer-authored).
46. **Party light union** — Multiple bearers → union lit-visible (Phase 4 checklist).

---

## Phase H — Dark Vision (G5, §8)

47. **`BodyCapabilityFlags.DarkVision`** (or racial SO) — `hasDarkVision`, threshold reduction, sight bonus params.
48. **`DarkVisionResolver`** — Consult before applying bonuses; magical darkness caps (stub fields).
49. **Essence hook** — e.g. `DarkVision.asset` OR with racial capability.
50. **Low-light sight bonus** — +tiles when ambient+received at actor cell < `lowLightThreshold` (doc default 4).
51. **Future extensibility** — Status/spells register on stats lighting contribution dictionary.
52. **Debug** — `[Lighting:DarkVision]` applied or blocked by zone.

---

## Phase I — Enemy alert from light (G8, §11 — Phase 6 scenario)

53. **Party light origins** — Carried emitters + party-enabled tile emitters (`emitLight > 0`).
54. **`EnemyAiBrain` / `SenseSightService` hook** — Detect light in enemy sight/cone even if body not seen.
55. **Alert transition** — Reason `"party_light"` (v0: go to Alert).
56. **Optional intensity scaling** — Stronger emit → slightly larger detection range.
57. **Debug** — `[Lighting:Alert] {enemy} alerted by light at {cell} level={n}`.

---

## Phase J — Save/load & persistence

58. **Save per-floor lighting explored state** — With fog snapshots (compact).
59. **Do not save full dynamic emitter state v0** — Or define minimal save policy per doc “out of scope” note.

---

## Phase K — Performance & phase 2 (explicit deferrals in doc)

60. **Dirty-region recompute** — Instead of full map each event.
61. **Multi-emitter combine mode** — Optional “max only” vs sum-capped.
62. **Light through open doors** — Dynamic propagation when doors open.
63. **Colored / mood lighting** — Non-white light channels.
64. **Light-based stealth modifiers** — Combat/stealth integration.
65. **Dedicated `FogOverlay` tilemap** — Stop mutating base floor/wall colors (FoW doc future).

---

## Phase L — Magical darkness (doc: out of v0, design now)

66. **`MagicalDarknessStrength` on cell/region** — Floor on required light.
67. **`darkVisionAllowed` / `maxDarkVisionSightBonus`** — Zone overrides.
68. **Gameplay zones** — Content that caps Dark Vision.

---

## Phase M — Tooling & docs

69. **Update §13 audit table** in `Lighting-Requirements.md` when milestones land.
70. **Scenario inspector “not implemented” hints** — Per phase, what logs/visuals to expect (avoid Phase 5 confusion).
71. **Unit tests** — Propagation, threshold, dark tile set, day/night tick, snapshot freeze.
72. **Play-mode QA checklist** — Map scenario phases to manual steps (Zones A–G).

---

## Cross-cutting defaults (resolve in prompts if needed)

| Topic | v0 default (Lighting-Requirements §16) |
|--------|----------------------------------------|
| Falloff | Manhattan |
| Multi-emitter | Sum capped |
| Dark tile fog state | Visible + dim tint |
| Entities on dark tiles | Hidden |
| Torch slot | Equipped + `LightSourceDefinition` |
| `baseVisibilityThreshold` | 3 |
| `lowLightThreshold` | 4 |

---

## FoW dependency note

Lighting snapshot (Phase D) assumes FoW three-state + terrain snapshot (largely in place). Trap/hazard overlay in fog snapshot is separate FoW backlog.

---

## Suggested prompt order

1. **Phase A** — Service + definitions  
2. **Phase B + C** — Per-member sight + dark tiles (first visible win)  
3. **Phase D** — Fog lighting snapshot  
4. **Phase F + E** — Emitters + day/night content  
5. **Phase G** — Torch + wall ignite  
6. **Phase H** — Dark Vision  
7. **Phase I** — Enemy alert  
8. **Phase J, K, L** — As needed  

---

## Debug logging contract (full set when implemented)

| Prefix | When |
|--------|------|
| `[Lighting:Sight]` | Effective range for member at origin |
| `[Lighting:Receive]` | Recompute region (verbose dev flag) |
| `[Lighting:Emit]` | Emission changed |
| `[Lighting:Cycle]` | Ambient phase advance |
| `[Lighting:DarkTile]` | Cell in LOS but under threshold |
| `[Lighting:Fog]` | Snapshot capture / freeze |
| `[Lighting:Alert]` | Enemy alerted by party light |
| `[Lighting:DarkVision]` | Threshold/range bonus applied or blocked |
| `[Lighting:Scenario]` | QA harness only (exists today) |

---

## Document history

| Date | Note |
|------|------|
| 2026-05-27 | Initial future backlog for prompt planning |
