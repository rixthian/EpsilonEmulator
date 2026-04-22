#!/usr/bin/env python3
"""Build a neutral launcher profile manifest from classic external variables."""

from __future__ import annotations

import argparse
import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a canonical launcher profile manifest from an external variables file."
    )
    parser.add_argument("source_file", type=Path, help="Path to external_variables.txt.")
    parser.add_argument("--profile-key", required=True, help="Canonical launcher profile key.")
    parser.add_argument("--output", type=Path, required=True, help="Output JSON manifest path.")
    return parser.parse_args()


def parse_variables(source_file: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    for raw_line in source_file.read_text(encoding="utf-8", errors="replace").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        values[key.strip()] = value.strip()
    return values


def select_section(values: dict[str, str], prefixes: tuple[str, ...], keywords: tuple[str, ...] = ()) -> dict[str, str]:
    selected: dict[str, str] = {}
    for key, value in values.items():
        lower_key = key.lower()
        if key.startswith(prefixes) or any(token in lower_key for token in keywords):
            selected[key] = value
    return dict(sorted(selected.items(), key=lambda item: item[0]))


def derive_capabilities(values: dict[str, str]) -> list[str]:
    capabilities: set[str] = set()

    if "furnidata.load.url" in values and "productdata.load.url" in values:
        capabilities.add("catalog.content.externalized")
    if "external.figurepartlist.txt" in values:
        capabilities.add("avatar.figure.externalized")
    if "flash.dynamic.download.url" in values or "dynamic.download.url" in values:
        capabilities.add("assets.dynamic_download")
    if any(key.startswith("landing.view.") for key in values):
        capabilities.add("campaigns.landing_view")
    if any("adwarning" in key.lower() or "roomenterad" in key.lower() for key in values):
        capabilities.add("ads.campaign_surfaces")
    if any(key.startswith("games.") for key in values):
        capabilities.add("games.feature_flags")
    if any(key.startswith("guidetool.") for key in values):
        capabilities.add("guides.support")
    if any(key.startswith("questing.") or "seasonalquest" in key.lower() for key in values):
        capabilities.add("quests.seasonal")
    if any("wired" in value.lower() or key.startswith("wf_") for key, value in values.items()):
        capabilities.add("wired.content")
    if any(key.startswith("moderator") or key.startswith("habboinfotool") or key.startswith("identityinformationtool") for key in values):
        capabilities.add("moderation.staff_tools")
    if any(key.startswith("friendbar.") for key in values):
        capabilities.add("social.friendbar")
    if any(key.startswith("effects.") for key in values):
        capabilities.add("avatar.effects")

    return sorted(capabilities)


def build_manifest(source_file: Path, profile_key: str) -> dict:
    values = parse_variables(source_file)
    prefix_counts = Counter(key.split(".", 1)[0] for key in values)

    manifest = {
        "manifestVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "profileKey": profile_key,
        "sourceFile": str(source_file.name),
        "variableCount": len(values),
        "capabilities": derive_capabilities(values),
        "sections": {
            "content": select_section(
                values,
                ("furnidata.", "productdata.", "external.figurepartlist", "external.texts.", "image.library.", "private.image.library."),
            ),
            "downloads": select_section(
                values,
                ("flash.dynamic.download.", "dynamic.download."),
            ),
            "room": select_section(
                values,
                ("room.",),
                ("roomenterad", "room_moderation"),
            ),
            "catalog": select_section(
                values,
                ("catalog.", "recycler.", "mysterybox.", "unique.limited.items."),
                ("ecotron", "catalog"),
            ),
            "games": select_section(
                values,
                ("games.",),
                ("snowstorm", "battleball", "freeze", "lympix"),
            ),
            "community": select_section(
                values,
                ("friendbar.", "link.format.", "eventinfo.", "feed.", "menu.", "purse."),
                ("stream", "friendbar", "communitygoal"),
            ),
            "campaigns": select_section(
                values,
                ("landing.view.", "competition.", "seasonalQuestCalendar.", "questing."),
                ("promo", "campaign", "daily.quest", "seasonalquest"),
            ),
            "moderation": select_section(
                values,
                ("guidetool.", "moderator", "habboinfotool", "identityinformationtool"),
                ("moderator", "identity", "hobba"),
            ),
            "featureFlags": {
                "enabled": dict(
                    sorted(
                        (key, value) for key, value in values.items()
                        if value.lower() in {"1", "true"}
                    )
                ),
                "disabled": dict(
                    sorted(
                        (key, value) for key, value in values.items()
                        if value.lower() in {"0", "false"}
                    )
                ),
            },
            "keyPrefixes": [
                {"prefix": prefix, "count": count}
                for prefix, count in prefix_counts.most_common(20)
            ],
        },
    }
    return manifest


def main() -> int:
    args = parse_args()
    source_file = args.source_file.resolve()
    output_path = args.output.resolve()

    if not source_file.exists() or not source_file.is_file():
        raise SystemExit(f"Source file does not exist or is not a file: {source_file}")

    manifest = build_manifest(source_file, args.profile_key)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
