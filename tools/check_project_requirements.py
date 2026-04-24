#!/usr/bin/env python3
"""Check local SDK/runtime requirements for Epsilon development."""

from __future__ import annotations

import json
import shutil
import subprocess
from pathlib import Path


UNITY_HUB = Path("/Applications/Unity Hub.app/Contents/MacOS/Unity Hub")
UNITY_EDITORS = Path("/Applications/Unity/Hub/Editor")
TARGET_UNITY_VERSION = "6000.3.14f1"


def run(command: list[str], timeout: int = 15) -> tuple[int, str]:
    try:
        completed = subprocess.run(
            command,
            check=False,
            capture_output=True,
            text=True,
            timeout=timeout,
        )
        output = (completed.stdout or "") + (completed.stderr or "")
        return completed.returncode, output.strip()
    except Exception as exc:  # pragma: no cover - diagnostic script
        return 1, str(exc)


def command_version(command: str, args: list[str]) -> str | None:
    binary = shutil.which(command)
    if not binary:
        return None

    code, output = run([binary, *args])
    if code != 0 or not output:
        return "installed"
    return output.splitlines()[0].strip()


def unity_hub_installed() -> bool:
    return UNITY_HUB.exists()


def unity_editors() -> list[str]:
    if not UNITY_EDITORS.exists():
        return []

    return sorted(
        child.name
        for child in UNITY_EDITORS.iterdir()
        if child.is_dir()
    )


def unity_modules(version: str) -> dict[str, bool]:
    root = UNITY_EDITORS / version
    if not root.exists():
        return {
            "webgl": False,
            "mac-il2cpp": False,
        }

    playback_engines = root / "PlaybackEngines"
    return {
        "webgl": (playback_engines / "WebGLSupport").exists(),
        "mac-il2cpp": (playback_engines / "MacStandaloneSupport").exists(),
    }


def main() -> int:
    editors = unity_editors()
    modules = unity_modules(TARGET_UNITY_VERSION)
    status = {
        "dotnet": command_version("dotnet", ["--version"]),
        "node": command_version("node", ["--version"]),
        "npm": command_version("npm", ["--version"]),
        "unityHub": str(UNITY_HUB) if unity_hub_installed() else None,
        "unityTargetVersion": TARGET_UNITY_VERSION,
        "unityEditors": editors,
        "unityTargetInstalled": TARGET_UNITY_VERSION in editors,
        "unityModules": modules,
    }

    print(json.dumps(status, indent=2))

    required = [
        bool(status["dotnet"]),
        bool(status["node"]),
        bool(status["npm"]),
        bool(status["unityHub"]),
    ]

    return 0 if all(required) else 1


if __name__ == "__main__":
    raise SystemExit(main())
