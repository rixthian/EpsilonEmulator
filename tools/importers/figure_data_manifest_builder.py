#!/usr/bin/env python3
"""Build a canonical Epsilon figure-data manifest from figuredata.xml."""

from __future__ import annotations

import argparse
import json
import xml.etree.ElementTree as ET
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a canonical figure-data manifest from a figuredata.xml file."
    )
    parser.add_argument("figure_data", type=Path, help="Path to figuredata.xml.")
    parser.add_argument("--manifest-key", required=True, help="Canonical Epsilon figure-data identifier.")
    parser.add_argument("--era-key", required=True, help="Canonical era key.")
    parser.add_argument("--avatar-bundle", type=Path, help="Optional avatar-asset manifest path for context.")
    parser.add_argument("--output", type=Path, required=True, help="Output JSON manifest path.")
    return parser.parse_args()


def to_bool(value: str | None) -> bool:
    return value == "1"


def load_avatar_bundle_context(path: Path | None) -> dict[str, Any] | None:
    if path is None:
        return None

    data = json.loads(path.read_text(encoding="utf-8"))
    return {
        "bundleKey": data["bundleKey"],
        "categoryCounts": data["categoryCounts"],
        "swfCount": data["swfCount"],
        "swfVersionCounts": data["swfVersionCounts"],
    }


def build_manifest(
    figure_data_path: Path,
    manifest_key: str,
    era_key: str,
    avatar_bundle_context: dict[str, Any] | None,
) -> dict[str, Any]:
    root = ET.parse(figure_data_path).getroot()

    settypes: list[dict[str, Any]] = []
    palette_summaries: list[dict[str, Any]] = []
    gender_counts: Counter[str] = Counter()
    part_type_counts: Counter[str] = Counter()
    color_counts_by_palette: dict[str, int] = {}

    for settype in root.findall("./sets/settype"):
        sets = []
        total_parts = 0

        for set_entry in settype.findall("set"):
            parts = []

            for part in set_entry.findall("part"):
                part_type = part.attrib["type"]
                part_type_counts[part_type] += 1
                total_parts += 1
                parts.append(
                    {
                        "id": int(part.attrib["id"]),
                        "type": part_type,
                        "index": int(part.attrib["index"]),
                        "colorable": to_bool(part.attrib.get("colorable")),
                        "colorIndex": int(part.attrib.get("colorindex", "0")),
                    }
                )

            gender = set_entry.attrib.get("gender", "U")
            gender_counts[gender] += 1

            sets.append(
                {
                    "id": int(set_entry.attrib["id"]),
                    "gender": gender,
                    "clubLevel": int(set_entry.attrib.get("club", "0")),
                    "colorable": to_bool(set_entry.attrib.get("colorable")),
                    "selectable": to_bool(set_entry.attrib.get("selectable")),
                    "preselectable": to_bool(set_entry.attrib.get("preselectable")),
                    "sellable": to_bool(set_entry.attrib.get("sellable")),
                    "partCount": len(parts),
                    "parts": parts,
                }
            )

        settypes.append(
            {
                "type": settype.attrib["type"],
                "paletteId": int(settype.attrib["paletteid"]),
                "mandatory": {
                    "male0": to_bool(settype.attrib.get("mand_m_0")),
                    "female0": to_bool(settype.attrib.get("mand_f_0")),
                    "male1": to_bool(settype.attrib.get("mand_m_1")),
                    "female1": to_bool(settype.attrib.get("mand_f_1")),
                },
                "setCount": len(sets),
                "partCount": total_parts,
                "sets": sets,
            }
        )

    for palette in root.findall("./colors/palette"):
        colors = []

        for color in palette.findall("color"):
            colors.append(
                {
                    "id": int(color.attrib["id"]),
                    "index": int(color.attrib["index"]),
                    "clubLevel": int(color.attrib.get("club", "0")),
                    "selectable": to_bool(color.attrib.get("selectable")),
                    "rgbHex": (color.text or "").strip(),
                }
            )

        color_counts_by_palette[palette.attrib["id"]] = len(colors)
        palette_summaries.append(
            {
                "id": int(palette.attrib["id"]),
                "colorCount": len(colors),
                "colors": colors,
            }
        )

    return {
        "manifestVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "manifestKey": manifest_key,
        "eraKey": era_key,
        "sourcePath": str(figure_data_path.resolve()),
        "setTypeCount": len(settypes),
        "paletteCount": len(palette_summaries),
        "genderSetCounts": dict(sorted(gender_counts.items())),
        "partTypeCounts": dict(sorted(part_type_counts.items())),
        "paletteColorCounts": color_counts_by_palette,
        "avatarBundleContext": avatar_bundle_context,
        "setTypes": settypes,
        "palettes": palette_summaries,
    }


def main() -> int:
    args = parse_args()
    figure_data_path = args.figure_data.resolve()
    output_path = args.output.resolve()

    if not figure_data_path.exists() or not figure_data_path.is_file():
        raise SystemExit(f"Figure data file does not exist or is not a file: {figure_data_path}")

    avatar_bundle_context = load_avatar_bundle_context(
        args.avatar_bundle.resolve() if args.avatar_bundle else None
    )
    manifest = build_manifest(
        figure_data_path=figure_data_path,
        manifest_key=args.manifest_key,
        era_key=args.era_key,
        avatar_bundle_context=avatar_bundle_context,
    )
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
