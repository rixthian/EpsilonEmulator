#!/usr/bin/env python3
"""Build a canonical Epsilon avatar-asset manifest from a flat Flash asset bundle."""

from __future__ import annotations

import argparse
import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

from swf_file_analyzer import analysis_to_json_dict, analyze_swf_file


AVATAR_PART_PREFIXES = (
    "hair_",
    "hat_",
    "shirt_",
    "shoes_",
    "trousers_",
    "jacket_",
    "acc_",
    "face_",
    "misc_",
)

PET_KEYS = {
    "bear",
    "cat",
    "chicken",
    "croco",
    "dog",
    "dragon",
    "duck",
    "frog",
    "lion",
    "pig",
    "rhino",
    "spider",
    "terrier",
    "turtle",
    "niko",
    "bunnyevil",
    "perry",
    "wade",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a canonical avatar-asset manifest from a flat Flash asset bundle."
    )
    parser.add_argument("source_root", type=Path, help="Root directory of the avatar asset bundle.")
    parser.add_argument("--bundle-key", required=True, help="Canonical Epsilon bundle identifier.")
    parser.add_argument("--era-key", required=True, help="Canonical era key.")
    parser.add_argument("--output", type=Path, required=True, help="Output JSON manifest path.")
    return parser.parse_args()


def classify_asset(stem: str) -> str:
    lower_stem = stem.lower()

    if lower_stem.startswith("hh_human") or lower_stem.startswith("figure_"):
        return "figure_library"
    if lower_stem.startswith("pet_") or lower_stem in PET_KEYS:
        return "pet_companion"
    if lower_stem.startswith(AVATAR_PART_PREFIXES):
        return "avatar_part"
    return "action_effect"


def list_files(root: Path, suffix: str) -> list[Path]:
    return sorted(path for path in root.rglob(f"*{suffix}") if path.is_file())


def build_manifest(source_root: Path, bundle_key: str, era_key: str) -> dict:
    swf_files = list_files(source_root, ".swf")
    gamedata_files = sorted(
        path.relative_to(source_root).as_posix()
        for path in source_root.joinpath("gamedata").rglob("*")
        if path.is_file()
    ) if source_root.joinpath("gamedata").is_dir() else []

    category_counts: Counter[str] = Counter()
    swf_version_counts: Counter[int] = Counter()
    signature_counts: Counter[str] = Counter()
    assets: list[dict] = []

    for swf_path in swf_files:
        relative_path = swf_path.relative_to(source_root).as_posix()
        asset_key = swf_path.stem
        category = classify_asset(asset_key)
        analysis = analysis_to_json_dict(analyze_swf_file(swf_path))
        header = analysis["header"]

        category_counts[category] += 1
        swf_version_counts[header["version"]] += 1
        signature_counts[header["signature"]] += 1

        assets.append(
            {
                "assetKey": asset_key,
                "relativePath": relative_path,
                "category": category,
                "signature": header["signature"],
                "swfVersion": header["version"],
                "frameCount": header["frame_count"],
                "stageWidth": header["frame_size"]["width_pixels"],
                "stageHeight": header["frame_size"]["height_pixels"],
                "tagCount": analysis["tag_count"],
                "bitmapTagCount": analysis["bitmap_tag_count"],
                "actionTagCount": analysis["action_tag_count"],
                "symbolClassCount": len(analysis["symbol_classes"]),
                "warnings": analysis["warnings"],
            }
        )

    return {
        "manifestVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "bundleKey": bundle_key,
        "eraKey": era_key,
        "sourceRoot": str(source_root.resolve()),
        "swfCount": len(assets),
        "gamedataFiles": gamedata_files,
        "categoryCounts": dict(sorted(category_counts.items())),
        "swfVersionCounts": {str(key): value for key, value in sorted(swf_version_counts.items())},
        "signatureCounts": dict(sorted(signature_counts.items())),
        "assets": assets,
    }


def main() -> int:
    args = parse_args()
    source_root = args.source_root.resolve()
    output_path = args.output.resolve()

    if not source_root.exists() or not source_root.is_dir():
        raise SystemExit(f"Source root does not exist or is not a directory: {source_root}")

    manifest = build_manifest(source_root, args.bundle_key, args.era_key)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
