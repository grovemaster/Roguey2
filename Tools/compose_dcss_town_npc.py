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

from PIL import Image, ImageDraw, ImageFont

REPO = Path(__file__).resolve().parents[1]
SPRITES = REPO / "Assets/Art/NPC/Sprites"
PLAYER_SPRITES = REPO / "Assets/Art/Player/Sprites"
ORIGINALS = REPO / "Assets/Art/NPC/ThirdParty/DungeonCrawl32/originals"
PLAYER_ORIGINALS = REPO / "Assets/Art/Player/ThirdParty/DungeonCrawl32/originals"
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

# Playable race world sprites (player doll layers; distinct from town NPC roster).
PLAYER_RECIPES: dict[str, list[str]] = {
    "Player_Human": [
        "player/base/human_m.png",
        "player/hair/brown1.png",
        "player/body/aragorn.png",
    ],
    "Player_Elf": [
        "player/base/elf_m.png",
        "player/hair/elf_red.png",
        "player/body/leather_armour2.png",
        "player/cloak/green.png",
    ],
    "Player_Barbarian": [
        "player/base/ogre_m.png",
        "player/body/animal_skin.png",
    ],
    "Player_Dwarf": [
        "player/base/dwarf_m.png",
        "player/body/chainmail.png",
    ],
    "Player_Beastman": [
        "player/base/minotaur_m.png",
        "player/body/leather_armour2.png",
    ],
    "Player_Dragonian": [
        "player/base/draconian_red_m.png",
    ],
    "Player_Tiefling": [
        "player/base/demonspawn_red_m.png",
        "player/body/leather_armour2.png",
    ],
    "Player_Undead": [
        "player/base/vampire_m.png",
    ],
}


PLAYER_PREVIEW = REPO / "Assets/Art/Player/player_race_preview.png"
PLAYER_PREVIEW_LABELS: dict[str, str] = {
    "Player_Human": "Human",
    "Player_Elf": "Elf",
    "Player_Barbarian": "Barbarian",
    "Player_Dwarf": "Dwarf",
    "Player_Beastman": "Beastman",
    "Player_Dragonian": "Dragonian",
    "Player_Tiefling": "Tiefling",
    "Player_Undead": "Undead",
}


def composite(layers: list[Path]) -> Image.Image:
    out = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    for path in layers:
        out.alpha_composite(Image.open(path).convert("RGBA"))
    return out


def build_player_preview(scale: int = 4) -> Path:
    """Compose a labeled preview sheet for all player race sprites."""
    names = [n for n in PLAYER_PREVIEW_LABELS if n in PLAYER_RECIPES]
    sprites: list[tuple[str, Image.Image]] = []
    for name in names:
        path = PLAYER_SPRITES / f"{name}.png"
        if not path.exists():
            raise SystemExit(f"Missing player sprite for preview: {path}")
        sprites.append((PLAYER_PREVIEW_LABELS[name], Image.open(path).convert("RGBA")))

    cell = 32 * scale
    pad = 12
    label_h = 18
    cols = len(sprites)
    width = pad + cols * (cell + pad)
    height = pad + label_h + cell + pad
    sheet = Image.new("RGBA", (width, height), (40, 40, 44, 255))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("DejaVuSans.ttf", 13)
    except OSError:
        font = ImageFont.load_default()

    for i, (label, sprite) in enumerate(sprites):
        x0 = pad + i * (cell + pad)
        y0 = pad + label_h
        scaled = sprite.resize((cell, cell), Image.NEAREST)
        sheet.paste(scaled, (x0, y0), scaled)
        bbox = draw.textbbox((0, 0), label, font=font)
        text_w = bbox[2] - bbox[0]
        draw.text((x0 + (cell - text_w) // 2, pad - 2), label, fill=(220, 220, 220, 255), font=font)

    PLAYER_PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(PLAYER_PREVIEW)
    return PLAYER_PREVIEW


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--tiles-root",
        default=str(DEFAULT_TILES_ROOT),
        help="Extracted crawl-tiles Oct-5-2010 folder",
    )
    parser.add_argument("--all", action="store_true", help="Build all shipped town NPCs")
    parser.add_argument(
        "--player",
        action="store_true",
        help="Build playable race player sprites (Player_Human, Player_Elf, …)",
    )
    parser.add_argument(
        "--name",
        help="Single output base name (town NPC or player sprite)",
    )
    parser.add_argument(
        "--preview",
        action="store_true",
        help="With --player: write Assets/Art/Player/player_race_preview.png",
    )
    parser.add_argument(
        "--preview-only",
        action="store_true",
        help="Only rebuild player_race_preview.png from existing Player_*.png files",
    )
    args = parser.parse_args()

    if args.preview_only:
        preview = build_player_preview()
        print(f"Wrote {preview}")
        return

    tiles_root = Path(args.tiles_root)
    if not tiles_root.is_dir():
        raise SystemExit(f"Missing tiles root: {tiles_root}")

    if args.player:
        recipe_map = PLAYER_RECIPES
        out_dir = PLAYER_SPRITES
        originals_dir = PLAYER_ORIGINALS
        default_name = "Player_Human"
    else:
        recipe_map = RECIPES
        out_dir = SPRITES
        originals_dir = ORIGINALS
        default_name = "NPC_Mira"

    if args.name is not None and args.name not in recipe_map:
        raise SystemExit(
            f"Unknown name {args.name!r}. Choices: {', '.join(sorted(recipe_map))}"
        )

    names = sorted(recipe_map) if args.all else [args.name or default_name]
    out_dir.mkdir(parents=True, exist_ok=True)
    originals_dir.mkdir(parents=True, exist_ok=True)

    for name in names:
        rel_paths = recipe_map[name]
        abs_paths = [tiles_root / rel for rel in rel_paths]
        missing = [str(p) for p in abs_paths if not p.exists()]
        if missing:
            raise SystemExit(f"{name}: missing layers:\n" + "\n".join(missing))

        for src in abs_paths:
            rel = str(src.relative_to(tiles_root)).replace(os.sep, "__")
            dst = originals_dir / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)

        out = out_dir / f"{name}.png"
        composite(abs_paths).save(out)
        print(f"Wrote {out}")

    if args.player and args.preview:
        preview = build_player_preview()
        print(f"Wrote {preview}")


if __name__ == "__main__":
    main()
