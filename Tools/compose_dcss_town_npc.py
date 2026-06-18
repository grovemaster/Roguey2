#!/usr/bin/env python3
"""Compose 32x32 DCSS town NPC sprites from crawl-tiles layer PNGs.

Requires: crawl-tiles Oct-5-2010.zip extracted locally.
Default archive path: Assets/Art/NPC/StyleComparison/_temp/crawl-tiles Oct-5-2010

Example:
  python3 Tools/compose_dcss_town_npc.py --all
  python3 Tools/compose_dcss_town_npc.py --name NPC_Fenn
"""
from __future__ import annotations

import argparse
import os
import shutil
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[1]
SPRITES = REPO / "Assets/Art/NPC/Sprites"
ORIGINALS = REPO / "Assets/Art/NPC/ThirdParty/DungeonCrawl32/originals"
DEFAULT_TILES_ROOT = (
    REPO / "Assets/Art/NPC/StyleComparison/_temp/crawl-tiles Oct-5-2010"
)

# 10 human composites + 16 non-human (2 per other race).
RECIPES: dict[str, list[str]] = {
    # --- Humans (10) ---
    "NPC_Mira": [
        "player/base/human_f.png",
        "player/hair/pigtails_brown.png",
        "player/body/china_red2.png",
        "player/cloak/brown.png",
    ],
    "NPC_Luc": ["dc-mon/human.png"],
    "NPC_Edda": [
        "player/base/human_f.png",
        "player/hair/long_white.png",
        "player/body/china_red.png",
        "player/head/hood_white.png",
        "player/hand2/misc/book_blue.png",
    ],
    "NPC_Fenn": [
        "player/base/human_m.png",
        "player/hair/brown1.png",
        "player/body/aragorn.png",
        "player/cloak/green.png",
    ],
    "NPC_Greta": [
        "player/base/human_f.png",
        "player/hair/fem_red.png",
        "player/body/arwen.png",
        "player/cloak/brown.png",
    ],
    "NPC_MageTutor": [
        "player/base/human_m.png",
        "player/body/robe_blue_white.png",
        "player/head/hood_cyan.png",
        "player/hand2/misc/book_blue.png",
    ],
    "NPC_KnightDrillMaster": [
        "player/base/human_m.png",
        "player/body/chainmail.png",
        "player/head/helm_plume.png",
    ],
    "NPC_ArcaneVendor": [
        "player/base/human_f.png",
        "player/body/robe_white_blue.png",
        "player/head/hood_gray.png",
        "player/hand2/misc/book_cyan.png",
    ],
    "NPC_PriestShrineSteward": [
        "player/base/human_m.png",
        "player/body/robe_white_green.png",
        "player/head/hood_white.png",
    ],
    "NPC_DemoHost": [
        "player/base/human_m.png",
        "player/body/banded.png",
        "player/cloak/blue.png",
    ],
    # --- Barbarian (2) ---
    "NPC_ShamanBarbarian": ["dc-mon/orc_priest.png"],
    "NPC_Barbarian_Warchief": [
        "player/base/ogre_m.png",
        "player/body/animal_skin.png",
    ],
    # --- Dwarf (2) ---
    "NPC_ForgeBrothersSteward": [
        "player/base/dwarf_m.png",
        "player/body/chainmail.png",
        "player/head/helm_gimli.png",
    ],
    "NPC_StoneWardensSteward": [
        "player/base/dwarf_f.png",
        "player/body/bplate_metal1.png",
        "player/head/hood_gray.png",
    ],
    # --- Beastman (2) ---
    "NPC_BeastBloodMerchant": ["dc-mon/gnoll.png"],
    "NPC_Beastman_Brute": ["player/base/minotaur_m.png"],
    # --- Dragonian (2) ---
    "NPC_DragonianElderVolscale": ["player/base/draconian_gold_m.png"],
    "NPC_Dragonian_Guard": ["player/base/draconian_red_m.png"],
    # --- Tiefling (2) ---
    "NPC_FleshmetalForgemaster": [
        "player/base/demonspawn_red_m.png",
        "player/body/robe_red3.png",
    ],
    "NPC_Tiefling_Smith": [
        "player/base/demonspawn_black_m.png",
        "player/body/leather_armour2.png",
    ],
    # --- Fairy (2) ---
    "NPC_FairyMerchant": ["player/base/spriggan_f.png"],
    "NPC_Fairy_Spriggan": ["player/base/spriggan_m.png"],
    # --- Elf (2) ---
    "NPC_Elf_Ranger": [
        "player/base/elf_m.png",
        "player/hair/elf_red.png",
        "player/body/leather_armour2.png",
        "player/cloak/green.png",
    ],
    "NPC_Elf_Sage": [
        "player/base/elf_f.png",
        "player/hair/elf_white.png",
        "player/body/robe_green.png",
        "player/hand2/misc/book_green.png",
    ],
    # --- Undead (2) ---
    "NPC_Undead_Wight": ["player/base/mummy_m.png"],
    "NPC_Undead_Revenant": ["player/base/vampire_m.png"],
}


def composite(layers: list[Path]) -> Image.Image:
    out = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    for path in layers:
        out.alpha_composite(Image.open(path).convert("RGBA"))
    return out


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--tiles-root",
        default=str(DEFAULT_TILES_ROOT),
        help="Extracted crawl-tiles Oct-5-2010 folder",
    )
    parser.add_argument("--all", action="store_true", help="Build all shipped town NPCs")
    parser.add_argument("--name", choices=sorted(RECIPES), help="Single output base name")
    args = parser.parse_args()

    tiles_root = Path(args.tiles_root)
    if not tiles_root.is_dir():
        raise SystemExit(f"Missing tiles root: {tiles_root}")

    names = sorted(RECIPES) if args.all else [args.name or "NPC_Mira"]
    SPRITES.mkdir(parents=True, exist_ok=True)
    ORIGINALS.mkdir(parents=True, exist_ok=True)

    for name in names:
        rel_paths = RECIPES[name]
        abs_paths = [tiles_root / rel for rel in rel_paths]
        missing = [str(p) for p in abs_paths if not p.exists()]
        if missing:
            raise SystemExit(f"{name}: missing layers:\n" + "\n".join(missing))

        for src in abs_paths:
            rel = str(src.relative_to(tiles_root)).replace(os.sep, "__")
            dst = ORIGINALS / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)

        out = SPRITES / f"{name}.png"
        composite(abs_paths).save(out)
        print(f"Wrote {out}")


if __name__ == "__main__":
    main()
