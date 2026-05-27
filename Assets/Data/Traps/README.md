# Trap data (SampleScene QA)

## One-time setup

1. **GameSystems** needs **`TrapService`** + **`TrapBootstrap`** (same object as hazards/interactables).
2. Menu: **Assets → Create → JRogue → Traps → Create QA Trap Asset Pack**  
   Creates spike/bear/dart definitions and `PlacementSets/SampleScene_Traps.asset`.

## SampleScene layout (`SampleScene_Traps`)

| Cell | Trap | Notes |
|------|------|--------|
| (-3, -2) | Spike (visible) | Move confirm before enter |
| (-2, -3) | Spike (invisible) | No confirm; Perception ≥ 12 reveals |
| (-1, -2) | Bear (once) | 15 pierce, fires once |
| (-6, -3) | Dart (wall) | **Wall host** at (-6,-3); step on **(-5,-3)** east floor to trigger; sprite draws on the wall tile |

## Wiring

- Assign **`SampleScene_Traps`** on **Trap Bootstrap → Placement Set**.
- Optional: assign **Trap_Overlay** tilemap on **Trap Service** (auto-created under Grid if empty).
- Move gate order: **Trap** → **Hazard** → **Auto-pickup** (in `PlayerCommandProcessor`).

See [Traps-Requirements.md](../../Docs/Combat/Traps-Requirements.md).
