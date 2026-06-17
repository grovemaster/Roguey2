#!/usr/bin/env python3
"""Compose 32x32 DCSS town NPC sprites from crawl-tiles layer PNGs.

Requires: crawl-tiles Oct-5-2010.zip extracted locally.
Default archive path: /tmp/crawl-tiles Oct-5-2010

Example:
  python3 Tools/compose_dcss_town_npc.py --all
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

RECIPES: dict[str, list[str]] = {
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
        default="/tmp/crawl-tiles Oct-5-2010",
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
