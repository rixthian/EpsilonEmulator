#!/usr/bin/env python3
"""Build a neutral Epsilon manifest from a client content root."""

from __future__ import annotations

import argparse
import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable


CORE_GAME_DATA_FILE_GROUPS = (
    {"external_flash_texts.txt"},
    {"external_variables.txt"},
    {"figuredata.xml"},
    {"furnidata.xml", "furnidata.txt"},
    {"productdata.txt"},
)

IGNORED_PATH_PARTS = {".git", "__MACOSX"}
IGNORED_FILE_NAMES = {".DS_Store", "Thumbs.db", ".gitkeep"}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a canonical client asset root manifest from a legacy asset tree."
    )
    parser.add_argument("source_root", type=Path, help="Root directory of the client asset tree.")
    parser.add_argument("--root-id", required=True, help="Canonical Epsilon root identifier.")
    parser.add_argument("--output", type=Path, required=True, help="Output JSON manifest path.")
    return parser.parse_args()


def list_relative_files(root: Path) -> list[str]:
    return sorted(
        str(path.relative_to(root)).replace("\\", "/")
        for path in root.rglob("*")
        if path.is_file() and not _should_ignore(path, root)
    )


def _should_ignore(candidate: Path, root: Path) -> bool:
    relative_parts = candidate.relative_to(root).parts
    return (
        any(part in IGNORED_PATH_PARTS for part in relative_parts)
        or candidate.name in IGNORED_FILE_NAMES
    )


def collect_prefixed_files(files: list[str], prefixes: tuple[str, ...]) -> list[str]:
    return [path for path in files if path.startswith(prefixes)]


def collect_named_files(files: list[str], names: set[str]) -> list[str]:
    return [path for path in files if Path(path).name in names]


def collect_root_named_files(files: list[str], names: set[str]) -> list[str]:
    return [path for path in files if "/" not in path and Path(path).name in names]


def classify_gamedata_files(files: list[str]) -> dict:
    file_set = set(files)
    return {
        "files": files,
        "hasCoreContentMetadata": all(
            any(path.endswith(name) for path in files for name in group)
            for group in CORE_GAME_DATA_FILE_GROUPS
        ),
        "hasFigurePartConfig": any(
            path.startswith("gamedata/figurepartconfig/") or Path(path).name in {"animation.xml", "draworder.xml", "partsets.xml"}
            for path in files
        ),
        "hasSecurityCast": any(path.endswith("sec.cct") for path in file_set),
        "textBundleFiles": [path for path in files if path.endswith(".txt")],
    }


def classify_furniture_assets(files: list[str]) -> dict:
    named = [Path(path).name for path in files]
    prefixes = Counter()

    for name in named:
        stem = Path(name).stem
        token = stem.split("_", 1)[0] if "_" in stem else stem
        prefixes[token] += 1

    return {
        "count": len(files),
        "adAssetCount": sum(1 for name in named if name.startswith("ads_")),
        "gameAssetCount": sum(1 for name in named if name.startswith(("bb_", "sf_", "lt_"))),
        "currencyAssetCount": sum(1 for name in named if name.startswith(("CF_", "CFC_"))),
        "topPrefixes": [
            {"prefix": prefix, "count": count}
            for prefix, count in prefixes.most_common(20)
        ],
        "sample": named[:40],
    }


def classify_image_library(files: list[str]) -> dict:
    prefixes = Counter()
    for path in files:
        relative = path.split("/", 1)[1] if "/" in path else path
        folder = relative.split("/", 1)[0] if "/" in relative else "."
        prefixes[folder] += 1

    return {
        "count": len(files),
        "topFolders": [
            {"folder": folder, "count": count}
            for folder, count in prefixes.most_common(20)
        ],
        "badgeAssetCount": sum(1 for path in files if "/badges/" in f"/{path}" or "/album1584/" in f"/{path}"),
        "campaignAssetCount": sum(
            1 for path in files if any(token in path.lower() for token in ("campaign", "landing", "officialrooms", "hotel_view", "top_story", "adverts", "ads"))
        ),
        "sample": files[:40],
    }


def classify_public_rooms(files: list[str]) -> dict:
    names = [Path(path).name for path in files]
    return {
        "count": len(names),
        "brandedVariantCount": sum(
            1
            for name in names
            if any(marker in name for marker in ("_branded", "_nokia", "_idol", "_mtv", "_coke"))
        ),
        "regionalVariantCount": sum(
            1
            for name in names
            if any(
                name.endswith(f"_{suffix}.swf")
                for suffix in ("au", "br", "ca", "ch", "cv", "de", "es", "fi", "fr", "it", "no", "se", "uk")
            )
        ),
        "sample": names[:50],
    }


def classify_avatar_assets(files: list[str]) -> dict:
    names = [Path(path).name for path in files]
    return {
        "count": len(names),
        "figureLibraryCount": sum(1 for name in names if name.startswith("figure_")),
        "humanLibraryCount": sum(1 for name in names if name.startswith("hh_human")),
        "partVariantCount": sum(
            1
            for name in names
            if name.startswith(
                (
                    "hair_",
                    "hat_",
                    "shirt_",
                    "shoes_",
                    "trousers_",
                    "jacket_",
                    "acc_",
                )
            )
        ),
        "sample": names[:50],
    }


def classify_pet_assets(files: list[str]) -> dict:
    names = [Path(path).name for path in files]
    return {
        "count": len(names),
        "sample": names[:30],
    }


def classify_core_client_packages(files: list[str]) -> dict:
    names = [Path(path).name for path in files]
    return {
        "count": len(names),
        "sample": names[:20],
    }


def classify_catalogue_images(files: list[str]) -> dict:
    names = [Path(path).name for path in files]
    extensions = Counter(Path(name).suffix.lower() or "<none>" for name in names)

    return {
        "count": len(names),
        "extensions": dict(sorted(extensions.items())),
        "iconCount": sum(1 for name in names if name.startswith("icon_")),
        "thumbnailCount": sum(1 for name in names if name.startswith(("th_floor_", "th_landscape_", "th_wall_"))),
        "sample": names[:40],
    }


def classify_catalog_sql(files: list[str]) -> dict:
    names = [Path(path).name for path in files]

    return {
        "count": len(names),
        "hasCatalogPages": any(name == "catalog_pages.sql" for name in names),
        "hasCatalogItems": any(name == "catalog_items.sql" for name in names),
        "hasItemBase": any(name == "items_base.sql" for name in names),
        "sample": names[:30],
    }


def build_manifest(source_root: Path, root_id: str) -> dict:
    all_files = list_relative_files(source_root)

    gamedata_files = sorted(
        collect_prefixed_files(all_files, ("gamedata/",)) +
        collect_root_named_files(
            all_files,
            {
                "config_habbo.xml",
                "external_flash_texts.txt",
                "external_flash_override_texts.txt",
                "external_variables.txt",
                "external_override_variables.txt",
                "figuredata.xml",
                "figuremap.xml",
                "animation.xml",
                "draworder.xml",
                "partsets.xml",
                "promo_habbos.xml",
                "furnidata.txt",
                "productdata.txt",
                "safechat-en.txt",
                "crossdomain.xml",
            },
        )
    )
    c_images_files = collect_prefixed_files(all_files, ("c_images/",))
    dcr_furni_files = [
        path for path in all_files
        if (
            path.startswith("dcr/hof_furni/") or
            path.startswith("hot_furni/")
        ) and path.endswith(".swf")
    ]
    dcr_public_room_files = [
        path for path in all_files
        if (
            path.startswith("dcr/") or "/" not in path
        ) and Path(path).name.startswith("hh_room") and path.endswith(".swf")
    ]
    gordon_package_roots = sorted(
        {
            path.split("/", 2)[1]
            for path in all_files
            if path.startswith("gordon/") and "/" in path[len("gordon/") :]
        }
    )
    gordon_files = [path for path in all_files if path.startswith("gordon/")]
    gordon_core_client_files = [
        path
        for path in gordon_files
        if Path(path).name in {"Habbo.swf", "HabboRoomContent.swf", "Habbo10.swf"}
    ]
    root_core_client_files = collect_root_named_files(
        all_files,
        {"Habbo.swf", "HabboRoomContent.swf", "Habbo10.swf", "TileCursor.swf", "SelectionArrow.swf", "PlaceHolderFurniture.swf", "PlaceHolderWallItem.swf", "PlaceHolderPet.swf"},
    )
    game_package_files = collect_prefixed_files(all_files, ("game/",))
    catalogue_image_files = collect_prefixed_files(all_files, ("catalogue/",))
    catalog_sql_files = collect_prefixed_files(all_files, ("Catalog-SQLS/",))
    gordon_public_room_files = [
        path for path in gordon_files if Path(path).name.startswith("hh_room") and path.endswith(".swf")
    ]
    gordon_avatar_files = [
        path
        for path in gordon_files
        if Path(path).name.startswith(
            (
                "hh_human",
                "figure_",
                "hair_",
                "hat_",
                "shirt_",
                "shoes_",
                "trousers_",
                "jacket_",
                "acc_",
            )
        )
    ]
    root_avatar_files = [
        path
        for path in all_files
        if "/" not in path and Path(path).name.startswith(
            (
                "hh_human",
                "figure_",
                "hair_",
                "Hair_",
                "hat_",
                "Hat_",
                "shirt_",
                "Shirt_",
                "shoes_",
                "Shoes_",
                "trousers_",
                "Trousers_",
                "jacket_",
                "Jacket_",
                "acc_",
            )
        )
    ]
    gordon_pet_files = [
        path
        for path in gordon_files
        if Path(path).name.startswith(("h_", "sh_", "hh_pets", "pets_palettes"))
    ]
    root_pet_files = [
        path
        for path in all_files
        if "/" not in path and Path(path).name.startswith(("h_", "sh_", "hh_pets", "pets_palettes", "dog", "cat", "bear", "frog", "lion", "monkey", "pig", "rhino", "terrier", "croco", "dragon", "horse", "spider", "chicken", "turtle"))
    ]

    layout_kind = "classic_client_asset_root"
    if collect_root_named_files(all_files, {"Habbo.swf", "furnidata.txt", "external_variables.txt"}):
        layout_kind = "flat_client_content_root"

    manifest = {
        "manifestVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "rootId": root_id,
        "layoutKind": layout_kind,
        "sections": {
            "gamedata": classify_gamedata_files(gamedata_files),
            "imageLibrary": classify_image_library(c_images_files),
            "dcrFurnitureAssets": classify_furniture_assets(dcr_furni_files),
            "gordonPackages": {
                "count": len(gordon_package_roots),
                "packageKeys": gordon_package_roots,
            },
            "coreClientPackages": classify_core_client_packages(gordon_core_client_files or root_core_client_files),
            "gamePackages": classify_core_client_packages(game_package_files),
            "publicRoomPackages": classify_public_rooms(gordon_public_room_files or dcr_public_room_files),
            "avatarAssets": classify_avatar_assets(gordon_avatar_files or root_avatar_files),
            "petAssets": classify_pet_assets(gordon_pet_files or root_pet_files),
            "catalogueImages": classify_catalogue_images(catalogue_image_files),
            "catalogSql": classify_catalog_sql(catalog_sql_files),
        },
    }

    return manifest


def main() -> int:
    args = parse_args()
    source_root = args.source_root.resolve()
    output_path = args.output.resolve()

    if not source_root.exists() or not source_root.is_dir():
        raise SystemExit(f"Source root does not exist or is not a directory: {source_root}")

    manifest = build_manifest(source_root, args.root_id)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
