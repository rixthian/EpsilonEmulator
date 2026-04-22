#!/usr/bin/env python3
"""Build a canonical Epsilon client-build manifest from a client SWF."""

from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from swf_file_analyzer import analysis_to_json_dict, analyze_swf_file


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a canonical client-build manifest from a single client SWF."
    )
    parser.add_argument("swf_file", type=Path, help="Path to the client SWF file.")
    parser.add_argument("--build-key", required=True, help="Canonical Epsilon build identifier.")
    parser.add_argument("--client-family", required=True, help="Canonical client family key.")
    parser.add_argument("--era-key", required=True, help="Canonical era key.")
    parser.add_argument("--output", type=Path, required=True, help="Output JSON manifest path.")
    return parser.parse_args()


def derive_runtime_surface(analysis: dict[str, Any]) -> dict[str, Any]:
    symbol_names = [entry["name"] for entry in analysis["symbol_classes"]]
    lowered = [name.lower() for name in symbol_names]

    def has_token(*tokens: str) -> bool:
        return any(any(token in name for token in tokens) for name in lowered)

    return {
        "hasGuideSurface": has_token("guide", "help"),
        "hasCallForHelpSurface": has_token("call_for_help", "callforhelp"),
        "hasMessengerSurface": has_token("messenger", "friendbar", "console_message"),
        "hasInventoryEffectsSurface": has_token("fx_icon", "inventorycom_fx"),
        "hasCameraSurface": has_token("camera", "shutter"),
        "hasWiredSurface": has_token("wired", "userdefinedroomevents", "ude_"),
        "hasGameSurface": has_token("snowwar", "battleball", "gamescom", "wobble"),
        "hasFloorplanEditorSurface": has_token("floorplan", "floor_plan"),
        "hasAvatarRenderSurface": has_token("avatarrender", "avatar"),
    }


def build_manifest(
    swf_file: Path,
    build_key: str,
    client_family: str,
    era_key: str,
) -> dict[str, Any]:
    analysis = analysis_to_json_dict(analyze_swf_file(swf_file))
    header = analysis["header"]

    top_tags = sorted(
        analysis["tag_summaries"],
        key=lambda item: (-item["count"], item["name"]),
    )[:12]

    return {
        "manifestVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "buildKey": build_key,
        "clientFamily": client_family,
        "eraKey": era_key,
        "sourceFileName": swf_file.name,
        "sourcePath": str(swf_file.resolve()),
        "container": {
            "signature": header["signature"],
            "swfVersion": header["version"],
            "declaredFileLength": header["declared_file_length"],
            "frameRate": header["frame_rate"],
            "frameCount": header["frame_count"],
            "stageWidth": header["frame_size"]["width_pixels"],
            "stageHeight": header["frame_size"]["height_pixels"],
        },
        "rendering": {
            "spriteCount": analysis["sprite_count"],
            "shapeTagCount": analysis["shape_tag_count"],
            "bitmapTagCount": analysis["bitmap_tag_count"],
            "actionTagCount": analysis["action_tag_count"],
        },
        "runtimeSurface": derive_runtime_surface(analysis),
        "tagInventory": {
            "tagCount": analysis["tag_count"],
            "topTags": top_tags,
        },
        "warnings": analysis["warnings"],
    }


def main() -> int:
    args = parse_args()
    swf_file = args.swf_file.resolve()
    output_path = args.output.resolve()

    if not swf_file.exists() or not swf_file.is_file():
        raise SystemExit(f"SWF file does not exist or is not a file: {swf_file}")

    manifest = build_manifest(
        swf_file=swf_file,
        build_key=args.build_key,
        client_family=args.client_family,
        era_key=args.era_key,
    )
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
