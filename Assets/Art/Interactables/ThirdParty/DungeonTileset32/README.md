# 32×32 Dungeon Tileset (CC0) — lever subset

Imported for **lever switch** interactables (`LeverSwitch_Off.png`, `LeverSwitch_On.png`).

| File | Role |
|------|------|
| `dungeon_tileset_source.png` | Full 480×480 sheet (15×15 @ 32 px) |
| `LICENSE.txt` | CC0 provenance and slice coordinates |
| `../Sprites/LeverSwitch_Off.png` | Sliced handle **right** (off) — sheet **(4, 9)** |
| `../Sprites/LeverSwitch_On.png` | Sliced handle **left** (on) — sheet **(3, 9)** |

Re-slice after re-download:

```bash
python3 - <<'PY'
from PIL import Image
src = "Assets/Art/Interactables/ThirdParty/DungeonTileset32/dungeon_tileset_source.png"
out = "Assets/Art/Interactables/Sprites"
img = Image.open(src).convert("RGBA")
for name, c, r in [("LeverSwitch_Off.png", 4, 9), ("LeverSwitch_On.png", 3, 9)]:
    img.crop((c*32, r*32, (c+1)*32, (r+1)*32)).save(f"{out}/{name}")
PY
```

Then reimport in Unity (or touch the PNGs).
