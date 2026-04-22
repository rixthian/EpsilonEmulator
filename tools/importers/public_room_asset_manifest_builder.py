#!/usr/bin/env python3
"""
Build a canonical Epsilon manifest from a directory of Flash public-room SWF files.

This tool intentionally performs inventory and normalization only.
It does not attempt visual decompilation or renderer conversion.
"""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

SCRIPT_DIRECTORY = Path(__file__).resolve().parent
if str(SCRIPT_DIRECTORY) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIRECTORY))

from swf_file_analyzer import analysis_to_json_dict, analyze_swf_file


REGIONAL_SUFFIXES = {
    "au",
    "br",
    "ca",
    "ch",
    "cn",
    "cv",
    "de",
    "es",
    "fi",
    "fr",
    "it",
    "no",
    "se",
    "uk",
}

SEASONAL_MARKERS = {
    "xmas",
    "xms08",
}

BRAND_MARKERS = {
    "antena3",
    "calippo",
    "chupa",
    "coke",
    "disneyxd7",
    "dudesons",
    "fireservices",
    "garnier",
    "hpv",
    "iceage",
    "idol",
    "latin",
    "libra",
    "m62",
    "mtv",
    "nokia",
    "nrj",
    "percy",
    "protegeles",
    "tgt",
    "unicef",
}


@dataclass(slots=True)
class PublicRoomAssetEntry:
    assetId: str
    sourceFilename: str
    roomKey: str
    variantKey: str
    classification: str
    conversionStatus: str
    brandTags: list[str]
    localeTags: list[str]
    notes: list[str]
    swfMetadata: dict | None = None


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a canonical public-room asset manifest from Flash SWF packages."
    )
    parser.add_argument("source_directory", type=Path, help="Directory containing hh_room_*.swf files.")
    parser.add_argument(
        "--output",
        type=Path,
        required=True,
        help="Output JSON manifest path.",
    )
    parser.add_argument(
        "--include-swf-metadata",
        action="store_true",
        help="Analyze each SWF file and embed technical metadata in the manifest.",
    )
    return parser.parse_args()


def iter_public_room_files(source_directory: Path) -> Iterable[Path]:
    return sorted(
        path
        for path in source_directory.iterdir()
        if path.is_file()
        and path.suffix.lower() == ".swf"
        and path.name.lower().startswith("hh_room_")
    )


def build_entry(path: Path, include_swf_metadata: bool) -> PublicRoomAssetEntry:
    stem = path.stem.lower()
    trimmed = stem.removeprefix("hh_room_")
    tokens = [token for token in trimmed.split("_") if token]

    brand_tags = sorted({token for token in tokens if token in BRAND_MARKERS})
    locale_tags = sorted({token for token in tokens if token in REGIONAL_SUFFIXES})
    seasonal_tags = sorted({token for token in tokens if token in SEASONAL_MARKERS})

    canonical_tokens = [
        token
        for token in tokens
        if token not in BRAND_MARKERS
        and token not in REGIONAL_SUFFIXES
        and token not in SEASONAL_MARKERS
    ]

    room_key = "_".join(canonical_tokens) if canonical_tokens else trimmed
    variant_tokens = [token for token in tokens if token not in canonical_tokens]
    variant_key = "_".join(variant_tokens) if variant_tokens else "default"

    classification = classify_variant(brand_tags, locale_tags, seasonal_tags, variant_key)
    notes: list[str] = []

    if room_key == trimmed and variant_key == "default":
        notes.append("No variant markers detected from filename.")

    if classification == "unknown":
        notes.append("Filename requires manual review for canonical grouping.")

    if seasonal_tags:
        notes.append(f"Seasonal markers detected: {', '.join(seasonal_tags)}")

    swf_metadata = None
    conversion_status = "inventory_only"

    if include_swf_metadata:
        analysis = analyze_swf_file(path)
        swf_metadata = analysis_to_json_dict(analysis)
        conversion_status = "metadata_extracted"

    return PublicRoomAssetEntry(
        assetId=stem,
        sourceFilename=path.name,
        roomKey=room_key,
        variantKey=variant_key,
        classification=classification,
        conversionStatus=conversion_status,
        brandTags=brand_tags,
        localeTags=locale_tags,
        notes=notes,
        swfMetadata=swf_metadata,
    )


def classify_variant(
    brand_tags: list[str],
    locale_tags: list[str],
    seasonal_tags: list[str],
    variant_key: str,
) -> str:
    if brand_tags:
        return "brand_variant"
    if locale_tags:
        return "regional_variant"
    if seasonal_tags:
        return "seasonal_variant"
    if variant_key == "default":
        return "canonical"
    return "unknown"


def main() -> int:
    args = parse_args()
    source_directory = args.source_directory.resolve()
    output_path = args.output.resolve()

    if not source_directory.exists() or not source_directory.is_dir():
        raise SystemExit(f"Source directory does not exist or is not a directory: {source_directory}")

    entries = [asdict(build_entry(path, args.include_swf_metadata)) for path in iter_public_room_files(source_directory)]
    manifest = {
        "manifestVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "sourceDirectory": str(source_directory),
        "entries": entries,
    }

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
