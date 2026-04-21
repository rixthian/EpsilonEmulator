#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path


IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp"}


@dataclass
class VisualAssetEntry:
    assetKey: str
    relativePath: str
    extension: str
    assetKind: str
    sizeBytes: int
    width: int | None
    height: int | None


def detect_png_size(path: Path) -> tuple[int | None, int | None]:
    with path.open("rb") as handle:
        header = handle.read(24)
    if len(header) >= 24 and header[:8] == b"\x89PNG\r\n\x1a\n":
        return int.from_bytes(header[16:20], "big"), int.from_bytes(header[20:24], "big")
    return None, None


def classify_asset(path: Path) -> str:
    path_text = path.as_posix().lower()
    file_name = path.name.lower()
    if "furni" in path_text:
        return "furni_visual"
    if "icon" in file_name or "icons" in path_text:
        return "icon"
    if "room" in path_text:
        return "room_visual"
    if "group" in path_text:
        return "group_visual"
    if "avatar" in path_text or "user" in path_text:
        return "avatar_visual"
    return "generic_visual"


def build_manifest(root: Path) -> dict:
    assets: list[VisualAssetEntry] = []

    for path in sorted(root.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in IMAGE_EXTENSIONS:
            continue

        width, height = (None, None)
        if path.suffix.lower() == ".png":
            width, height = detect_png_size(path)

        relative_path = path.relative_to(root).as_posix()
        assets.append(
            VisualAssetEntry(
                assetKey=relative_path.replace("/", "__").replace(".", "_").lower(),
                relativePath=relative_path,
                extension=path.suffix.lower(),
                assetKind=classify_asset(path),
                sizeBytes=path.stat().st_size,
                width=width,
                height=height,
            )
        )

    return {
        "manifestVersion": "1.0.0",
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "rootPath": str(root),
        "assets": [asdict(asset) for asset in assets],
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Build a visual asset manifest from a directory.")
    parser.add_argument("root")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    root = Path(args.root).expanduser().resolve()
    output_path = Path(args.output).expanduser().resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(build_manifest(root), indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
