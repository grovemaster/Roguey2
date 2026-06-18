# Town NPC world sprites — DCSS (CC0)

Town NPCs use the [Dungeon Crawl 32×32 tiles](https://opengameart.org/content/dungeon-crawl-32x32-tiles) pack (CC0).

**26 sprites** — 10 human composites, 2 per other playable race.

Preview sheet: `Assets/Art/NPC/StyleComparison/dcss_town_npcs_full_roster.png`

## Humans (10)

| Output | Assigned prefab | Recipe |
|--------|-----------------|--------|
| `NPC_Mira.png` | TownNpc_Mira | `human_f` + `pigtails_brown` + `china_red2` + `brown` cloak |
| `NPC_Luc.png` | TownNpc_Luc | `dc-mon/human.png` |
| `NPC_Edda.png` | TownNpc_Edda | `human_f` + `long_white` + `china_red` + `hood_white` + `book_blue` |
| `NPC_Fenn.png` | TownNpc_Fenn | `human_m` + `brown1` hair + `aragorn` + `green` cloak |
| `NPC_Greta.png` | TownNpc_Greta | `human_f` + `fem_red` hair + `arwen` + `brown` cloak |
| `NPC_MageTutor.png` | TownNpc_MageTutor | `human_m` + `robe_blue_white` + `hood_cyan` + `book_blue` |
| `NPC_KnightDrillMaster.png` | TownNpc_KnightDrillMaster | `human_m` + `chainmail` + `helm_plume` |
| `NPC_ArcaneVendor.png` | *(reserve)* | `human_f` + `robe_white_blue` + `hood_gray` + `book_cyan` |
| `NPC_PriestShrineSteward.png` | TownNpc_PriestShrineSteward | `human_m` + `robe_white_green` + `hood_white` |
| `NPC_DemoHost.png` | TownNpc_DemoHost | `human_m` + `banded` + `blue` cloak |

## Other races (2 each)

| Race | Output | Assigned prefab | Recipe |
|------|--------|-----------------|--------|
| Barbarian | `NPC_ShamanBarbarian.png` | TownNpc_ShamanBarbarian | `dc-mon/orc_priest.png` |
| Barbarian | `NPC_Barbarian_Warchief.png` | *(reserve)* | `ogre_m` + `animal_skin` |
| Dwarf | `NPC_ForgeBrothersSteward.png` | TownNpc_ForgeBrothersSteward | `dwarf_m` + `chainmail` + `helm_gimli` |
| Dwarf | `NPC_StoneWardensSteward.png` | TownNpc_StoneWardensSteward | `dwarf_f` + `bplate_metal1` + `hood_gray` |
| Beastman | `NPC_BeastBloodMerchant.png` | TownNpc_BeastBloodMerchant | `dc-mon/gnoll.png` |
| Beastman | `NPC_Beastman_Brute.png` | *(reserve)* | `minotaur_m` |
| Dragonian | `NPC_DragonianElderVolscale.png` | TownNpc_DragonianElderVolscale | `draconian_gold_m` |
| Dragonian | `NPC_Dragonian_Guard.png` | *(reserve)* | `draconian_red_m` |
| Tiefling | `NPC_FleshmetalForgemaster.png` | *(reserve)* | `demonspawn_red_m` + `robe_red3` |
| Tiefling | `NPC_Tiefling_Smith.png` | TownNpc_FleshmetalForgemaster | `demonspawn_black_m` + `leather_armour2` |
| Fairy | `NPC_FairyMerchant.png` | *(reserve)* | `spriggan_f` |
| Fairy | `NPC_Fairy_Spriggan.png` | TownNpc_FairyMerchant | `spriggan_m` |
| Elf | `NPC_Elf_Ranger.png` | *(reserve)* | `elf_m` + `elf_red` hair + `leather_armour2` + `green` cloak |
| Elf | `NPC_Elf_Sage.png` | TownNpc_ArcaneVendor | `elf_f` + `elf_white` hair + `robe_green` + `book_green` |
| Undead | `NPC_Undead_Wight.png` | *(reserve)* | `mummy_m` |
| Undead | `NPC_Undead_Revenant.png` | *(reserve)* | `vampire_m` |

Source layers are copied under `originals/` (paths mirror the crawl-tiles archive).

## Rebuild

```bash
python3 Tools/compose_dcss_town_npc.py --all
```

Extract [crawl-tiles Oct-5-2010](https://opengameart.org/content/dungeon-crawl-32x32-tiles) to  
`Assets/Art/NPC/StyleComparison/_temp/crawl-tiles Oct-5-2010` (or pass `--tiles-root`).

## Unity import

- **32 PPU**, **Point** filter, **no mipmaps**, pivot **(0.5, 0.25)**
- Run **JRogue → Town → Configure DCSS Town NPC Sprites** after replacing PNGs
- Run **JRogue → Town → Assign DCSS Town NPC Sprites** to wire sprites onto town prefabs
