#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]
SCAN_ROOTS = [ROOT / "src", ROOT / "tests"]
MAX_NAME_LENGTH = 40
BLOCKED_TOKENS = {
    "butterfly",
    "phoenix",
    "havana",
    "silverwave",
    "holograph",
    "uberemu",
    "arcturus",
    "snowlight",
}


def iter_files() -> list[Path]:
    files: list[Path] = []
    for root in SCAN_ROOTS:
        for path in root.rglob("*"):
            if not path.is_file():
                continue
            if "bin" in path.parts or "obj" in path.parts:
                continue
            files.append(path)
    return files


def main() -> int:
    violations: list[str] = []

    for path in iter_files():
        name = path.name
        stem = path.stem.lower()

        if len(name) > MAX_NAME_LENGTH:
            violations.append(f"{path}: filename too long ({len(name)} > {MAX_NAME_LENGTH})")

        for token in BLOCKED_TOKENS:
            if token in stem:
                violations.append(f"{path}: blocked token '{token}' in filename")

    if violations:
        print("File naming violations found:", file=sys.stderr)
        for violation in violations:
            print(f"- {violation}", file=sys.stderr)
        return 1

    print("File naming check passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
