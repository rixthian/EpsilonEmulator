#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]
SCAN_DIRS = ("src", "tests", "docs", "references")
SKIP_PARTS = {"bin", "obj", ".git"}
BLOCKED_TOKENS = (
    "butterfly",
    "phoenix",
    "holograph",
    "havana",
    "silverwave",
    "uberemu",
    "arcturus",
    "snowlight",
    "chop",
)


def should_skip(path: Path) -> bool:
    return any(part in SKIP_PARTS for part in path.parts)


def iter_files() -> list[Path]:
    files: list[Path] = []
    for directory in SCAN_DIRS:
        base = ROOT / directory
        if not base.exists():
            continue
        for path in base.rglob("*"):
            if path.is_file() and not should_skip(path):
                files.append(path)
    return files


def main() -> int:
    failures: list[str] = []

    for path in iter_files():
        try:
            content = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue

        lowered = content.casefold()
        for token in BLOCKED_TOKENS:
            if token in lowered:
                failures.append(f"{path}: contains blocked token '{token}'")

    if failures:
        print("Repository hygiene check failed:")
        for failure in failures:
            print(f" - {failure}")
        return 1

    print("Repository hygiene check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
