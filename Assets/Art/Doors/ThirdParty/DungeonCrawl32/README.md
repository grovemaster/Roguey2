# Dungeon Crawl 32×32 tiles — doors (subset)

Source: [Dungeon Crawl 32×32 tiles](https://opengameart.org/content/dungeon-crawl-32x32-tiles) / [crawl/tiles on GitHub](https://github.com/crawl/tiles)

See `LICENSE.txt` for terms (same as hazard DCSS subset).

## Mapping

| Game sprite | Source (`originals/`) | Unity (`../../Sprites/`) |
|-------------|----------------------|---------------------------|
| Closed horizontal | `closed_door.png` | `Door_Closed_H.png` |
| Open horizontal | `open_door.png` | `Door_Open_H.png` |
| Broken horizontal | `bars_red01.png` | `Door_Broken_H.png` |
| Closed vertical | `vgate_closed_middle.png` | `Door_Closed_V.png` |
| Open vertical | `vgate_open_middle.png` | `Door_Open_V.png` |
| Broken vertical | `vgate_sealed_middle.png` | `Door_Broken_V.png` |

Test key icon: `originals/key.png` → `Assets/Art/Items/Sprites/Key_Test_A.png`

## Unity import

- **Pixels Per Unit:** 32  
- **Filter Mode:** Point  
- **Compression:** None  

Re-run **JRogue → Doors → Create Door v0 Assets** after adding sprites so `DoorDefinition` assets pick up references.
