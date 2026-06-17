# Town NPC world sprites — DCSS (CC0)

Town human NPCs use the [Dungeon Crawl 32×32 tiles](https://opengameart.org/content/dungeon-crawl-32x32-tiles) pack (CC0).

## Shipped town sprites

| Output | Recipe |
|--------|--------|
| `NPC_Mira.png` | `human_f` + `pigtails_brown` hair + `china_red2` body + `brown` cloak |
| `NPC_Luc.png` | `dc-mon/human.png` (standalone) |
| `NPC_Edda.png` | `human_f` + `long_white` hair + `china_red` body + `hood_white` + `book_blue` |

Source layers are copied under `originals/` (paths mirror the crawl-tiles archive).

## Unity import

- **32 PPU**, **Point** filter, **no mipmaps**, pivot **(0.5, 0.25)** for world sprites
- Run **JRogue → Town → Configure DCSS Town NPC Sprites** after replacing PNGs

## Adding more NPCs

1. Pick layers from `crawl-tiles Oct-5-2010/player/` (base, hair, body, cloak, head, hand2) or `dc-mon/` standalone humans.
2. Alpha-composite onto a 32×32 RGBA canvas (base first, equipment on top).
3. Save to `Assets/Art/NPC/Sprites/NPC_<Name>.png` and assign on the town NPC prefab variant.

Full pack not imported — only layers used by current NPCs. Download the archive from OpenGameArt when expanding.
