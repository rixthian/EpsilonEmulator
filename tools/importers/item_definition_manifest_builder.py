#!/usr/bin/env python3
"""Build canonical Epsilon item-definition manifests from legacy furnidata."""

from __future__ import annotations

import argparse
import json
import re
import xml.etree.ElementTree as ET
from collections import Counter
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


TEXT_VARIANT = "text"
XML_VARIANT = "xml"


@dataclass(frozen=True)
class LegacyItemRecord:
    item_definition_id: int
    public_name: str
    internal_name: str
    item_type_code: str
    sprite_id: int
    stack_height: float
    can_stack: bool
    can_sit: bool
    is_walkable: bool
    allow_recycle: bool
    allow_trade: bool
    allow_marketplace_sell: bool
    allow_gift: bool
    allow_inventory_stack: bool
    interaction_type_code: str
    interaction_modes_count: int
    revision: int
    width: int
    length: int
    default_direction: int
    colors: list[str]
    offer_id: int | None
    ad_url: str | None
    source_variant: str
    source_class: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a canonical item-definition manifest from a furnidata source."
    )
    parser.add_argument("source_file", type=Path, help="Path to furnidata.txt or furnidata.xml.")
    parser.add_argument("--manifest-key", required=True, help="Canonical manifest key.")
    parser.add_argument("--output", type=Path, required=True, help="Output manifest path.")
    parser.add_argument(
        "--seed-output",
        type=Path,
        help="Optional output path for the runtime item-definition seed projection.",
    )
    return parser.parse_args()


def parse_furnidata(source_file: Path) -> tuple[str, list[LegacyItemRecord]]:
    suffix = source_file.suffix.lower()
    if suffix == ".xml":
        return XML_VARIANT, parse_xml_furnidata(source_file)
    return TEXT_VARIANT, parse_text_furnidata(source_file)


def parse_text_furnidata(source_file: Path) -> list[LegacyItemRecord]:
    payload = source_file.read_text(encoding="utf-8", errors="replace").lstrip("\ufeff").strip()
    try:
        records_raw = json.loads(payload)
    except json.JSONDecodeError:
        records_raw = []
        for raw_line in payload.splitlines():
            line = raw_line.strip()
            if not line:
                continue
            parsed_line = json.loads(line)
            if isinstance(parsed_line, list):
                records_raw.extend(parsed_line)

    records: list[LegacyItemRecord] = []

    for entry in records_raw:
        if not isinstance(entry, list) or len(entry) < 3:
            continue

        item_type_code = read_text_field(entry, 0, "s").lower()
        item_definition_id = read_int_field(entry, 1, 0)
        internal_name = read_text_field(entry, 2, f"item_{item_definition_id}")
        revision = read_int_field(entry, 3, 0)
        default_direction = read_int_field(entry, 4, 0)
        width = read_int_field(entry, 5, 1)
        length = read_int_field(entry, 6, 1)
        colors = parse_color_field(read_text_field(entry, 7))
        public_name = read_text_field(entry, 8, internal_name)
        description = read_text_field(entry, 9)
        ad_url = normalize_nullable_text(read_text_field(entry, 10))
        offer_id = read_nullable_int_field(entry, 11)

        interaction_type_code, interaction_modes_count = infer_interaction(
            internal_name=internal_name,
            public_name=public_name,
            description=description,
        )
        can_sit = interaction_type_code in {"seat", "bed"}
        is_walkable = interaction_type_code in {"teleport", "gate", "one_way_gate"}

        records.append(
            LegacyItemRecord(
                item_definition_id=item_definition_id,
                public_name=public_name,
                internal_name=internal_name,
                item_type_code=item_type_code,
                sprite_id=item_definition_id,
                stack_height=0.0,
                can_stack=item_type_code == "s",
                can_sit=can_sit,
                is_walkable=is_walkable,
                allow_recycle=True,
                allow_trade=not internal_name.startswith("ads_"),
                allow_marketplace_sell=not internal_name.startswith("ads_"),
                allow_gift=not internal_name.startswith("ads_"),
                allow_inventory_stack=False,
                interaction_type_code=interaction_type_code,
                interaction_modes_count=interaction_modes_count,
                revision=revision,
                width=width,
                length=length,
                default_direction=default_direction,
                colors=colors,
                offer_id=offer_id,
                ad_url=ad_url,
                source_variant=TEXT_VARIANT,
                source_class=internal_name,
            )
        )

    return records


def parse_xml_furnidata(source_file: Path) -> list[LegacyItemRecord]:
    root = ET.fromstring(source_file.read_text(encoding="utf-8", errors="replace").lstrip("\ufeff"))
    records: list[LegacyItemRecord] = []

    for item_type_code, section_name in (("s", "roomitemtypes"), ("i", "wallitemtypes")):
        section = root.find(section_name)
        if section is None:
            continue

        for node in section.findall("furnitype"):
            item_definition_id = read_xml_int(node, "id")
            internal_name = node.get("classname", f"item_{item_definition_id}")
            revision = read_xml_child_int(node, "revision")
            default_direction = read_xml_child_int(node, "defaultdir")
            width = read_xml_child_int(node, "xdim", 1)
            length = read_xml_child_int(node, "ydim", 1)
            public_name = read_xml_child_text(node, "name", internal_name)
            description = read_xml_child_text(node, "description", "")
            ad_url = normalize_nullable_text(read_xml_child_text(node, "adurl", ""))
            offer_id = read_nullable_xml_child_int(node, "offerid")
            can_stand_on = read_xml_child_int(node, "canstandon")
            can_sit_on = read_xml_child_int(node, "cansiton")
            can_lay_on = read_xml_child_int(node, "canlayon")
            colors = [color.text.strip() for color in node.findall("./partcolors/color") if color.text and color.text.strip()]

            interaction_type_code, interaction_modes_count = infer_interaction(
                internal_name=internal_name,
                public_name=public_name,
                description=description,
            )
            can_sit = can_sit_on == 1 or can_lay_on == 1 or interaction_type_code in {"seat", "bed"}
            is_walkable = can_stand_on == 1 or interaction_type_code in {"teleport", "gate", "one_way_gate"}

            records.append(
                LegacyItemRecord(
                    item_definition_id=item_definition_id,
                    public_name=public_name,
                    internal_name=internal_name,
                    item_type_code=item_type_code,
                    sprite_id=item_definition_id,
                    stack_height=0.0,
                    can_stack=item_type_code == "s" and can_stand_on == 1,
                    can_sit=can_sit,
                    is_walkable=is_walkable,
                    allow_recycle=read_xml_child_int(node, "bc") == 0,
                    allow_trade=read_xml_child_int(node, "buyout") == 1 or offer_id in {None, -1},
                    allow_marketplace_sell=offer_id not in {None, -1},
                    allow_gift=not internal_name.startswith("ads_"),
                    allow_inventory_stack=False,
                    interaction_type_code=interaction_type_code,
                    interaction_modes_count=interaction_modes_count,
                    revision=revision,
                    width=width,
                    length=length,
                    default_direction=default_direction,
                    colors=colors,
                    offer_id=offer_id,
                    ad_url=ad_url,
                    source_variant=XML_VARIANT,
                    source_class=internal_name,
                )
            )

    return records


def read_text_field(entry: list[Any], index: int, default: str = "") -> str:
    if index >= len(entry):
        return default
    value = entry[index]
    if value is None:
        return default
    return str(value)


def read_int_field(entry: list[Any], index: int, default: int = 0) -> int:
    value = read_text_field(entry, index)
    if not value:
        return default
    try:
        return int(value)
    except ValueError:
        return default


def read_nullable_int_field(entry: list[Any], index: int) -> int | None:
    value = read_text_field(entry, index)
    if not value:
        return None
    try:
        parsed = int(value)
    except ValueError:
        return None
    return None if parsed == -1 else parsed


def read_xml_int(node: ET.Element, attribute_name: str, default: int = 0) -> int:
    value = node.get(attribute_name)
    if not value:
        return default
    try:
        return int(value)
    except ValueError:
        return default


def read_xml_child_text(node: ET.Element, child_name: str, default: str = "") -> str:
    child = node.find(child_name)
    if child is None or child.text is None:
        return default
    return child.text.strip()


def read_xml_child_int(node: ET.Element, child_name: str, default: int = 0) -> int:
    value = read_xml_child_text(node, child_name, "")
    if not value:
        return default
    try:
        return int(value)
    except ValueError:
        return default


def read_nullable_xml_child_int(node: ET.Element, child_name: str) -> int | None:
    value = read_xml_child_text(node, child_name, "")
    if not value:
        return None
    try:
        parsed = int(value)
    except ValueError:
        return None
    return None if parsed == -1 else parsed


def parse_color_field(value: str) -> list[str]:
    if not value or value == "0,0,0":
        return []
    return [token.strip() for token in value.split(",") if token.strip()]


def normalize_nullable_text(value: str) -> str | None:
    normalized = value.strip()
    return normalized or None


def infer_interaction(internal_name: str, public_name: str, description: str) -> tuple[str, int]:
    haystack = " ".join([internal_name, public_name, description]).lower()

    patterns: list[tuple[str, str, int]] = [
        (r"\btele(port|porter)?\b", "teleport", 2),
        (r"\bone.?way.?gate\b", "one_way_gate", 1),
        (r"\bgate\b", "gate", 1),
        (r"\bpressure[_ -]?pad\b", "pressure_pad", 1),
        (r"\broller\b", "roller", 1),
        (r"\bdice\b", "dice", 6),
        (r"\bscore(board)?\b", "scoreboard", 1),
        (r"\bvending\b", "vending", 1),
        (r"\bbed\b", "bed", 2),
        (r"\b(chair|seat|bench|sofa|stool|couch|armchair)\b", "seat", 2),
    ]

    for pattern, interaction_type_code, interaction_modes_count in patterns:
        if re.search(pattern, haystack):
            return interaction_type_code, interaction_modes_count

    return "default", 0


def build_manifest(source_file: Path, manifest_key: str) -> dict[str, Any]:
    source_variant, records = parse_furnidata(source_file)
    item_type_counts = Counter(record.item_type_code for record in records)
    interaction_counts = Counter(record.interaction_type_code for record in records)

    items = [
        {
            "itemDefinitionId": record.item_definition_id,
            "publicName": record.public_name,
            "internalName": record.internal_name,
            "itemTypeCode": record.item_type_code,
            "spriteId": record.sprite_id,
            "stackHeight": record.stack_height,
            "canStack": record.can_stack,
            "canSit": record.can_sit,
            "isWalkable": record.is_walkable,
            "allowRecycle": record.allow_recycle,
            "allowTrade": record.allow_trade,
            "allowMarketplaceSell": record.allow_marketplace_sell,
            "allowGift": record.allow_gift,
            "allowInventoryStack": record.allow_inventory_stack,
            "interactionTypeCode": record.interaction_type_code,
            "interactionModesCount": record.interaction_modes_count,
            "revision": record.revision,
            "width": record.width,
            "length": record.length,
            "defaultDirection": record.default_direction,
            "colors": record.colors,
            "offerId": record.offer_id,
            "adUrl": record.ad_url,
            "sourceClass": record.source_class,
            "sourceVariant": record.source_variant,
        }
        for record in records
    ]

    return {
        "manifestVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "manifestKey": manifest_key,
        "sourceFile": str(source_file.name),
        "sourceVariant": source_variant,
        "itemCount": len(items),
        "counts": {
            "byItemType": dict(sorted(item_type_counts.items())),
            "byInteraction": dict(sorted(interaction_counts.items())),
        },
        "items": items,
    }


def build_seed_projection(manifest: dict[str, Any]) -> list[dict[str, Any]]:
    return [
        {
            "itemDefinitionId": item["itemDefinitionId"],
            "publicName": item["publicName"],
            "internalName": item["internalName"],
            "itemTypeCode": item["itemTypeCode"],
            "spriteId": item["spriteId"],
            "stackHeight": item["stackHeight"],
            "canStack": item["canStack"],
            "canSit": item["canSit"],
            "isWalkable": item["isWalkable"],
            "allowRecycle": item["allowRecycle"],
            "allowTrade": item["allowTrade"],
            "allowMarketplaceSell": item["allowMarketplaceSell"],
            "allowGift": item["allowGift"],
            "allowInventoryStack": item["allowInventoryStack"],
            "interactionTypeCode": item["interactionTypeCode"],
            "interactionModesCount": item["interactionModesCount"],
        }
        for item in manifest["items"]
    ]


def main() -> int:
    args = parse_args()
    source_file = args.source_file.resolve()
    output_path = args.output.resolve()

    if not source_file.exists() or not source_file.is_file():
        raise SystemExit(f"Source file does not exist or is not a file: {source_file}")

    manifest = build_manifest(source_file, args.manifest_key)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")

    if args.seed_output is not None:
        seed_output_path = args.seed_output.resolve()
        seed_output_path.parent.mkdir(parents=True, exist_ok=True)
        seed_output_path.write_text(
            json.dumps(build_seed_projection(manifest), indent=2, ensure_ascii=True) + "\n",
            encoding="utf-8",
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
