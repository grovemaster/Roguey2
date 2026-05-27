# Environmental hazards — SampleScene QA

Add **`SampleSceneHazardPlacements`** to the **GameSystems** object in `Assets/Scenes/SampleScene.unity` (same object as `MapManager`). On play it creates a **`HazardService`** if missing and registers:

| Hazard | Cells (grid) | Test |
|--------|----------------|------|
| **Lava** | `(6,1)`, `(7,1)`, `(6,2)`, `(7,2)` | STR &lt; 50 cannot enter; STR ≥ 50 can walk through |
| **Poison gas** | `(3,4)` … `(8,4)` | Confirm on enter; 1 poison damage on enter, wait, and each new player phase while standing |

Optional: add a **`Hazard_Overlay`** tilemap under **Grid**, assign it on `MapManager.hazardOverlayMap` and `HazardService` for visible lava/gas sprites (definitions reference sprites in `Assets/Art/Hazards/Sprites/`).

Assets: `Assets/Resources/Hazards/EnvironmentalHazard_Lava.asset`, `EnvironmentalHazard_PoisonGas.asset`.
