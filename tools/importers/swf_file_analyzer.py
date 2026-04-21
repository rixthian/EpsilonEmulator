#!/usr/bin/env python3
"""
Low-level SWF metadata analyzer for Epsilon import pipelines.

The analyzer is intentionally conservative:
- it parses container-level metadata
- it inventories tags and selected symbol/linkage information
- it does not attempt full decompilation or rendering
"""

from __future__ import annotations

import io
import json
import lzma
import struct
import zlib
from dataclasses import asdict, dataclass
from pathlib import Path


TAG_NAMES: dict[int, str] = {
    0: "End",
    1: "ShowFrame",
    2: "DefineShape",
    6: "DefineBits",
    9: "SetBackgroundColor",
    12: "DoAction",
    20: "DefineBitsLossless",
    21: "DefineBitsJPEG2",
    22: "DefineShape2",
    26: "PlaceObject2",
    28: "RemoveObject2",
    32: "DefineShape3",
    35: "DefineBitsJPEG3",
    36: "DefineBitsLossless2",
    39: "DefineSprite",
    43: "FrameLabel",
    45: "SoundStreamHead2",
    48: "DefineFont2",
    56: "ExportAssets",
    57: "ImportAssets",
    59: "DoInitAction",
    60: "DefineVideoStream",
    61: "VideoFrame",
    62: "DefineFontInfo2",
    69: "FileAttributes",
    70: "PlaceObject3",
    71: "ImportAssets2",
    76: "SymbolClass",
    82: "DoABC",
    83: "DefineShape4",
    84: "DefineMorphShape2",
    87: "DefineBinaryData",
    88: "DefineFontName",
    90: "DefineBitsJPEG4",
}


@dataclass(slots=True)
class SwfRectangle:
    x_min: int
    x_max: int
    y_min: int
    y_max: int
    width_pixels: float
    height_pixels: float


@dataclass(slots=True)
class SwfHeader:
    signature: str
    version: int
    declared_file_length: int
    frame_size: SwfRectangle
    frame_rate: float
    frame_count: int


@dataclass(slots=True)
class SwfTagSummary:
    code: int
    name: str
    count: int


@dataclass(slots=True)
class SwfSymbolLinkage:
    character_id: int
    name: str


@dataclass(slots=True)
class SwfAnalysis:
    source_filename: str
    header: SwfHeader
    tag_count: int
    tag_summaries: list[SwfTagSummary]
    exported_assets: list[SwfSymbolLinkage]
    symbol_classes: list[SwfSymbolLinkage]
    sprite_count: int
    shape_tag_count: int
    bitmap_tag_count: int
    action_tag_count: int
    warnings: list[str]


class _BitReader:
    def __init__(self, payload: bytes) -> None:
        self._payload = payload
        self._bit_position = 0

    def read_unsigned(self, width: int) -> int:
        value = 0

        for _ in range(width):
            byte_index = self._bit_position // 8
            bit_index = 7 - (self._bit_position % 8)
            bit = (self._payload[byte_index] >> bit_index) & 1
            value = (value << 1) | bit
            self._bit_position += 1

        return value

    def read_signed(self, width: int) -> int:
        value = self.read_unsigned(width)
        sign_bit = 1 << (width - 1)
        if value & sign_bit:
            value -= 1 << width
        return value

    @property
    def bytes_consumed(self) -> int:
        return (self._bit_position + 7) // 8


def analyze_swf_file(path: Path) -> SwfAnalysis:
    raw_payload = path.read_bytes()
    if len(raw_payload) < 8:
        raise ValueError(f"SWF file is too small: {path}")

    signature = raw_payload[:3].decode("ascii", errors="strict")
    version = raw_payload[3]
    declared_file_length = struct.unpack("<I", raw_payload[4:8])[0]
    body = _decode_body(signature, raw_payload)

    header_stream = io.BytesIO(body)
    frame_size = _read_rect(header_stream)
    frame_rate_raw = struct.unpack("<H", header_stream.read(2))[0]
    frame_count = struct.unpack("<H", header_stream.read(2))[0]
    frame_rate = frame_rate_raw / 256.0

    warnings: list[str] = []
    tag_counts: dict[int, int] = {}
    exported_assets: list[SwfSymbolLinkage] = []
    symbol_classes: list[SwfSymbolLinkage] = []
    sprite_count = 0
    shape_tag_count = 0
    bitmap_tag_count = 0
    action_tag_count = 0
    tag_count = 0

    while True:
        record_header = header_stream.read(2)
        if len(record_header) == 0:
            warnings.append("Unexpected end of tag stream without End tag.")
            break
        if len(record_header) < 2:
            warnings.append("Truncated tag header.")
            break

        tag_code_and_length = struct.unpack("<H", record_header)[0]
        tag_code = tag_code_and_length >> 6
        short_length = tag_code_and_length & 0x3F
        tag_length = short_length

        if short_length == 0x3F:
            long_length_bytes = header_stream.read(4)
            if len(long_length_bytes) < 4:
                warnings.append(f"Truncated extended length for tag {tag_code}.")
                break
            tag_length = struct.unpack("<I", long_length_bytes)[0]

        tag_payload = header_stream.read(tag_length)
        if len(tag_payload) < tag_length:
            warnings.append(f"Truncated payload for tag {tag_code}.")
            break

        tag_count += 1
        tag_counts[tag_code] = tag_counts.get(tag_code, 0) + 1

        if tag_code == 0:
            break

        if tag_code == 39:
            sprite_count += 1
        if tag_code in {2, 22, 32, 83, 84}:
            shape_tag_count += 1
        if tag_code in {6, 20, 21, 35, 36, 90}:
            bitmap_tag_count += 1
        if tag_code in {12, 59, 82}:
            action_tag_count += 1
        if tag_code == 56:
            exported_assets.extend(_parse_named_asset_table(tag_payload))
        if tag_code == 76:
            symbol_classes.extend(_parse_named_asset_table(tag_payload))

    tag_summaries = [
        SwfTagSummary(code=code, name=TAG_NAMES.get(code, f"Tag{code}"), count=count)
        for code, count in sorted(tag_counts.items())
    ]

    analysis = SwfAnalysis(
        source_filename=path.name,
        header=SwfHeader(
            signature=signature,
            version=version,
            declared_file_length=declared_file_length,
            frame_size=frame_size,
            frame_rate=frame_rate,
            frame_count=frame_count,
        ),
        tag_count=tag_count,
        tag_summaries=tag_summaries,
        exported_assets=exported_assets,
        symbol_classes=symbol_classes,
        sprite_count=sprite_count,
        shape_tag_count=shape_tag_count,
        bitmap_tag_count=bitmap_tag_count,
        action_tag_count=action_tag_count,
        warnings=warnings,
    )

    return analysis


def analysis_to_json_dict(analysis: SwfAnalysis) -> dict:
    return asdict(analysis)


def _decode_body(signature: str, raw_payload: bytes) -> bytes:
    body = raw_payload[8:]

    if signature == "FWS":
        return body
    if signature == "CWS":
        return zlib.decompress(body)
    if signature == "ZWS":
        if len(raw_payload) < 12:
            raise ValueError("ZWS header is truncated.")
        compressed_length = struct.unpack("<I", raw_payload[8:12])[0]
        compressed_payload = raw_payload[12 : 12 + compressed_length]
        return lzma.decompress(compressed_payload)

    raise ValueError(f"Unsupported SWF signature '{signature}'.")


def _read_rect(stream: io.BytesIO) -> SwfRectangle:
    preview = stream.read(16)
    if not preview:
        raise ValueError("Missing RECT data in SWF header.")

    bit_reader = _BitReader(preview)
    nbits = bit_reader.read_unsigned(5)
    x_min = bit_reader.read_signed(nbits)
    x_max = bit_reader.read_signed(nbits)
    y_min = bit_reader.read_signed(nbits)
    y_max = bit_reader.read_signed(nbits)
    bytes_consumed = bit_reader.bytes_consumed
    stream.seek(bytes_consumed - len(preview), io.SEEK_CUR)

    return SwfRectangle(
        x_min=x_min,
        x_max=x_max,
        y_min=y_min,
        y_max=y_max,
        width_pixels=(x_max - x_min) / 20.0,
        height_pixels=(y_max - y_min) / 20.0,
    )


def _parse_named_asset_table(payload: bytes) -> list[SwfSymbolLinkage]:
    if len(payload) < 2:
        return []

    stream = io.BytesIO(payload)
    count = struct.unpack("<H", stream.read(2))[0]
    assets: list[SwfSymbolLinkage] = []

    for _ in range(count):
        identifier_bytes = stream.read(2)
        if len(identifier_bytes) < 2:
            break
        character_id = struct.unpack("<H", identifier_bytes)[0]
        name = _read_null_terminated_string(stream)
        assets.append(SwfSymbolLinkage(character_id=character_id, name=name))

    return assets


def _read_null_terminated_string(stream: io.BytesIO) -> str:
    buffer = bytearray()

    while True:
        byte = stream.read(1)
        if byte in {b"", b"\x00"}:
            break
        buffer.extend(byte)

    return buffer.decode("utf-8", errors="replace")


def main() -> int:
    import argparse

    parser = argparse.ArgumentParser(description="Analyze a single SWF file and emit JSON metadata.")
    parser.add_argument("swf_file", type=Path, help="Path to the SWF file to inspect.")
    args = parser.parse_args()

    analysis = analyze_swf_file(args.swf_file.resolve())
    print(json.dumps(analysis_to_json_dict(analysis), indent=2, ensure_ascii=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
