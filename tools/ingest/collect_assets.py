#!/usr/bin/env python3
"""Collect and inventory client asset roots into a canonical Epsilon manifest."""

from __future__ import annotations

import argparse
import json
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT_FILE_KEYS = {
    "crossdomain.xml": "security_policy",
    "external_variables.txt": "gamedata",
    "external_flash_texts.txt": "gamedata",
    "flash_texts.txt": "gamedata",
    "furnidata.txt": "gamedata",
    "furnidata.xml": "gamedata",
    "productdata.txt": "gamedata",
    "figuredata.xml": "gamedata",
    "figuremap.xml": "gamedata",
    "config_habbo.xml": "gamedata",
    "Habbo.swf": "habboswf",
    "HabboRoomContent.swf": "roomcontent",
}

IGNORED_PATH_PARTS = {".git", "__MACOSX"}
IGNORED_FILE_NAMES = {".DS_Store", "Thumbs.db", ".gitkeep"}

FLAT_CLOTHES_PREFIXES = (
    "hair_",
    "hat_",
    "shirt_",
    "jacket_",
    "shoes_",
    "trousers_",
    "acc_",
)

FLAT_CLOTHES_NAMES = {
    "animation.xml",
    "draworder.xml",
    "partsets.xml",
}

FLAT_PET_NAMES = {
    "bear",
    "cat",
    "chicken",
    "croco",
    "dog",
    "dragon",
    "frog",
    "lion",
    "pig",
    "rhino",
    "spider",
    "terrier",
    "turtle",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Collect a local asset root into a canonical Epsilon asset-collection manifest."
    )
    parser.add_argument("profile", type=Path, help="Path to an ingest profile JSON file.")
    parser.add_argument("--output", type=Path, required=True, help="Output manifest path.")
    return parser.parse_args()


def load_profile(profile_path: Path) -> dict[str, Any]:
    return json.loads(profile_path.read_text(encoding="utf-8"))


def classify_artifact(relative_path: str) -> str:
    normalized = relative_path.replace("\\", "/")
    lower_path = normalized.lower()
    marked_path = f"/{lower_path}"
    file_name = Path(normalized).name
    lower_name = file_name.lower()

    if "/" not in normalized and file_name in ROOT_FILE_KEYS:
        return ROOT_FILE_KEYS[file_name]

    if file_name.startswith("hh_room_") and file_name.endswith(".swf"):
        return "public_rooms"
    if "/gordon/" in marked_path:
        return "gordon"
    if "/hof_furni/" in marked_path or "/hot_furni/" in marked_path:
        return "furnitures"
    if "/album1584/" in marked_path or "/badges/" in marked_path:
        return "badges"
    if "/badgeparts/" in marked_path:
        return "badgeparts"
    if "/effects/" in marked_path or "fx_icon_" in lower_path:
        return "effects"
    if "/pets/" in marked_path or "/pet/" in marked_path:
        return "pets"
    if "/" not in normalized and lower_name.endswith(".swf"):
        bare_name = lower_name.removesuffix(".swf")
        if bare_name in FLAT_PET_NAMES or bare_name.startswith("h_"):
            return "pets"
    if "/hotelview" in lower_path or "hotel_view" in lower_path:
        return "hotelview"
    if "/promo/" in marked_path or "landingpage" in lower_path or "top_story" in lower_path or "campaign" in lower_path:
        return "promo"
    if lower_path.endswith(".mp3") or "sound_machine_sample" in lower_path:
        return "mp3"
    if "/figurepartconfig/" in marked_path or "/hh_human_" in marked_path or "/clothes/" in marked_path:
        return "clothes"
    if "/" not in normalized:
        if lower_name in FLAT_CLOTHES_NAMES:
            return "clothes"
        if lower_name.endswith(".swf") and lower_name.startswith(FLAT_CLOTHES_PREFIXES):
            return "clothes"
    if "/c_images/" in marked_path and ("/album1581/" in marked_path or "/catalogue/" in marked_path or "/icons/" in marked_path):
        return "icons"
    if marked_path.startswith("/c_images/"):
        return "image_library"
    if "/gamedata/" in marked_path:
        return "gamedata"
    if "/catalogue/" in marked_path:
        return "catalogue"
    if "/catalog-sqls/" in marked_path or lower_name.endswith(".sql"):
        return "catalog_sql"
    if "/game/" in marked_path:
        return "game_packages"
    if lower_name in {"readme.md", "note.txt", "missing-queries"}:
        return "source_notes"

    return "unclassified"


def collect_root(root_path: Path) -> tuple[list[dict[str, Any]], Counter[str]]:
    files: list[dict[str, Any]] = []
    counts: Counter[str] = Counter()

    for candidate in sorted(path for path in root_path.rglob("*") if path.is_file()):
        if _should_ignore(candidate, root_path):
            continue
        relative_path = candidate.relative_to(root_path).as_posix()
        artifact_key = classify_artifact(relative_path)
        counts[artifact_key] += 1
        files.append(
            {
                "relativePath": relative_path,
                "artifactKey": artifact_key,
                "sizeBytes": candidate.stat().st_size,
            }
        )

    return files, counts


def _should_ignore(candidate: Path, root_path: Path) -> bool:
    relative_parts = candidate.relative_to(root_path).parts
    return (
        any(part in IGNORED_PATH_PARTS for part in relative_parts)
        or candidate.name in IGNORED_FILE_NAMES
    )


def build_manifest(profile: dict[str, Any]) -> dict[str, Any]:
    roots = profile.get("roots", [])
    root_summaries: list[dict[str, Any]] = []
    artifact_samples: dict[str, list[str]] = defaultdict(list)
    artifact_counts: Counter[str] = Counter()
    total_files = 0

    for root in roots:
        root_path = Path(root["path"]).expanduser().resolve()
        files, counts = collect_root(root_path)
        total_files += len(files)
        artifact_counts.update(counts)

        for file_entry in files:
            artifact_key = file_entry["artifactKey"]
            samples = artifact_samples[artifact_key]
            if len(samples) < 5:
                samples.append(file_entry["relativePath"])

        root_summaries.append(
            {
                "rootKey": root["rootKey"],
                "path": str(root_path),
                "fileCount": len(files),
                "counts": dict(sorted(counts.items())),
            }
        )

    artifacts = [
        {
            "artifactKey": artifact_key,
            "count": count,
            "samples": artifact_samples[artifact_key],
        }
        for artifact_key, count in sorted(artifact_counts.items())
    ]

    return {
        "manifestVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "profileKey": profile["profileKey"],
        "clientFamily": profile["clientFamily"],
        "eraKey": profile["eraKey"],
        "sourceKind": profile["sourceKind"],
        "domain": profile.get("domain"),
        "revisionKey": profile.get("revisionKey"),
        "totalFileCount": total_files,
        "roots": root_summaries,
        "artifacts": artifacts,
    }


def main() -> int:
    args = parse_args()
    profile_path = args.profile.resolve()
    output_path = args.output.resolve()

    if not profile_path.exists() or not profile_path.is_file():
        raise SystemExit(f"Profile does not exist or is not a file: {profile_path}")

    profile = load_profile(profile_path)
    manifest = build_manifest(profile)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
