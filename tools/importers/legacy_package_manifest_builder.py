#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path


PACKAGE_EXTENSIONS = {".dcr", ".dir", ".cct", ".cst", ".src", ".swf", ".zip"}


@dataclass
class LegacyPackageEntry:
    packageKey: str
    relativePath: str
    extension: str
    sizeBytes: int


def build_manifest(root: Path) -> dict:
    packages: list[LegacyPackageEntry] = []
    for path in sorted(root.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in PACKAGE_EXTENSIONS:
            continue

        relative_path = path.relative_to(root).as_posix()
        packages.append(
            LegacyPackageEntry(
                packageKey=relative_path.replace("/", "__").replace(".", "_").lower(),
                relativePath=relative_path,
                extension=path.suffix.lower(),
                sizeBytes=path.stat().st_size,
            )
        )

    return {
        "manifestVersion": "1.0.0",
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "rootPath": str(root),
        "packages": [asdict(package_entry) for package_entry in packages],
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Build a package manifest from legacy client package files.")
    parser.add_argument("root")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    root = Path(args.root).expanduser().resolve()
    output_path = Path(args.output).expanduser().resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(build_manifest(root), indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
