# Dungeon Crawl 32×32 tiles (subset)

Full pack not imported — only tiles needed for **Lava** and **Poison Gas** hazards.

See `LICENSE.txt` for terms.

## Mapping

| Game hazard | Source file(s) | Unity sprite (`../../Sprites/`) |
|-------------|----------------|----------------------------------|
| **Lava** | `originals/lava0.png` | `LavaTile.png` |
| **Poison gas overlay** | `originals/cloud_poison1.png` | `PoisonGasOverlay.png` |
| Alt gas frame | `cloud_poison0.png` | `PoisonGasOverlay_Alt0.png` |

Unused in repo but extracted: `lava1.png`, `cloud_miasma.png`.

## Unity import

- **Pixels Per Unit:** 32  
- **Filter Mode:** Point (no filter)  
- **Compression:** None (crisp pixels)  
- Lava: paint on `Hazard_Overlay` or replace floor visual per spec.  
- Gas: semi-transparent overlay above `Floor_Layer`; keep floor tile visible underneath.
