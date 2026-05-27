# Interactable tile data (Option B)

Author levers as **ScriptableObject** assets, place them with a **placement set** per level, and wire **`InteractableTileBootstrap`** in each scene.

## One-time setup

1. Ensure **GameSystems** (or similar) has **`InteractableTileService`**.
2. Menu: **Assets → Create → JRogue → Interactables → Create QA Lever Asset Pack**  
   (Also on the top menu bar: **JRogue → Interactables → …**.)  
   Regenerates/updates the sample assets below (safe to re-run). Skip if `Assets/Data/Interactables/` already has the lever assets.

## Asset layout

| Path | Purpose |
|------|---------|
| `LeverSwitch_*.asset` | Lever definitions (id, sprites, preconditions, effects) |
| `Preconditions/` | Reusable precondition assets |
| `Effects/` | Reusable effect assets |
| `PlacementSets/` | Cell + definition lists per level/room |
| `../../Art/Interactables/Sprites/` | Lever off/on sprites |

## Add levers to a level

### 1. Create or duplicate a lever definition

**Create → JRogue → Interactables → Interactable Tile Definition**

| Field | Notes |
|-------|--------|
| `interactableId` | Unique enum value (`LeverSwitchFirst`, etc.) |
| `blocksOccupancy` | **true** for levers |
| `bumpEnabled` | **false** for script-only levers |
| `preconditions` | Drag assets from `Preconditions/` |
| `onActivateEffects` | Drag assets from `Effects/` |
| `spriteOff` / `spriteOn` | Right = off, left = on |

### 2. Create a placement set for the level

**Create → JRogue → Interactables → Interactable Placement Set**

Add one row per lever:

- **Cell** — grid coordinates (e.g. `4, -6, 0`)
- **Definition** — your `LeverSwitch_*.asset`

Example: `PlacementSets/SampleScene_Levers.asset` (four QA levers in a row).

### 3. Wire the scene

On a manager object (e.g. **GameSystems**):

1. **Add Component → `InteractableTileService`** (once per scene).
2. **Add Component → `InteractableTileBootstrap`**.
3. Assign **Placement Set** → your `MyDungeon_Levers.asset`.  
   (Optional: use inline **Placements** instead if you prefer not to share a set.)

### 4. Level design rules

- Keep a **walkable floor** tile under each lever.
- Player bumps into the lever cell from an **adjacent** cell (orthogonal or diagonal).
- Each `interactableId` used in **OtherInteractableOn** / **ActivateInteractable** effects must match exactly one instance in the scene.

## QA test sequence (SampleScene)

Uses `PlacementSets/SampleScene_Levers.asset` at cells `(4–7, -6)`:

1. Bump **Lever 1** from `(4, -7)` north.
2. Bump **Lever 2** only after Lever 1 is on → chains **Lever 3**.
3. **Lever 3** cannot be bumped by the player.
4. Bump **Lever 4** after Lever 3 is on → +25 party XP.

## New level checklist

- [ ] `InteractableTileService` in scene  
- [ ] `InteractableTileBootstrap` + placement set assigned  
- [ ] Floor tiles under all lever cells  
- [ ] Unique `interactableId` per lever instance  
- [ ] Play mode: `[Interactable]` logs on bump  

See [Interactable-Tiles-Requirements.md](../../Docs/Combat/Interactable-Tiles-Requirements.md).
