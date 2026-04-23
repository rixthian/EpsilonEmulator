#!/usr/bin/env python3
"""Capture and diff public upstream source metadata for Epsilon."""

from __future__ import annotations

import argparse
import hashlib
import json
import mimetypes
from dataclasses import asdict, dataclass
from datetime import UTC, datetime
from pathlib import Path
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.parse import urlparse
from urllib.request import Request, urlopen


DEFAULT_BODY_LIMIT_BYTES = 5 * 1024 * 1024
HTTP_TIMEOUT_SECONDS = 20


@dataclass(slots=True)
class SourceSnapshot:
    name: str
    family: str
    artifact_kind: str
    locator: str
    status: str
    checked_at_utc: str
    content_type: str | None
    content_length: int | None
    etag: str | None
    last_modified: str | None
    sha256: str | None
    requires_manual_review: bool
    manual_review_reason: str | None
    error: str | None


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Capture and diff Epsilon source-intelligence snapshots."
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    snapshot_parser = subparsers.add_parser(
        "snapshot",
        help="Capture a normalized snapshot from a source-watch profile.",
    )
    snapshot_parser.add_argument("profile", type=Path, help="Path to the watch profile JSON file.")
    snapshot_parser.add_argument("output", type=Path, help="Path to the output snapshot JSON file.")

    diff_parser = subparsers.add_parser(
        "diff",
        help="Compare two snapshots and emit an update report.",
    )
    diff_parser.add_argument("previous", type=Path, help="Path to the previous snapshot JSON file.")
    diff_parser.add_argument("current", type=Path, help="Path to the current snapshot JSON file.")
    diff_parser.add_argument("output", type=Path, help="Path to the output diff JSON file.")

    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if args.command == "snapshot":
        profile = load_json(args.profile)
        snapshot = build_snapshot(profile)
        write_json(args.output, snapshot)
        return 0

    if args.command == "diff":
        previous = load_json(args.previous)
        current = load_json(args.current)
        report = build_diff_report(previous, current)
        write_json(args.output, report)
        return 0

    raise SystemExit(f"Unsupported command: {args.command}")


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=False) + "\n", encoding="utf-8")


def build_snapshot(profile: dict[str, Any]) -> dict[str, Any]:
    checked_at_utc = utc_now()
    body_limit = int(profile.get("downloadBodyLimitBytes", DEFAULT_BODY_LIMIT_BYTES))
    sources = [capture_source(record, checked_at_utc, body_limit) for record in profile.get("sources", [])]

    return {
        "profileName": profile.get("profileName", "unknown"),
        "version": int(profile.get("version", 1)),
        "checkedAtUtc": checked_at_utc,
        "sources": [asdict(source) for source in sources],
    }


def capture_source(record: dict[str, Any], checked_at_utc: str, body_limit: int) -> SourceSnapshot:
    name = str(record.get("name", "unnamed"))
    family = str(record.get("family", "unknown"))
    artifact_kind = str(record.get("artifactKind", "unknown"))
    locator = str(record.get("url") or record.get("path") or "")
    protected = bool(record.get("protected", False))
    hash_mode = str(record.get("hashMode", "metadata"))

    if protected:
        return SourceSnapshot(
            name=name,
            family=family,
            artifact_kind=artifact_kind,
            locator=locator,
            status="manual-review",
            checked_at_utc=checked_at_utc,
            content_type=None,
            content_length=None,
            etag=None,
            last_modified=None,
            sha256=None,
            requires_manual_review=True,
            manual_review_reason="source marked as protected; automatic bypass is not supported",
            error=None,
        )

    parsed = urlparse(locator)
    try:
        if parsed.scheme in {"http", "https"}:
            return capture_http_source(
                name,
                family,
                artifact_kind,
                locator,
                checked_at_utc,
                hash_mode,
                body_limit,
            )

        if parsed.scheme in {"", "file"}:
            path = Path(parsed.path if parsed.scheme == "file" else locator)
            return capture_file_source(name, family, artifact_kind, path, checked_at_utc, hash_mode)

        return SourceSnapshot(
            name=name,
            family=family,
            artifact_kind=artifact_kind,
            locator=locator,
            status="manual-review",
            checked_at_utc=checked_at_utc,
            content_type=None,
            content_length=None,
            etag=None,
            last_modified=None,
            sha256=None,
            requires_manual_review=True,
            manual_review_reason=f"unsupported locator scheme '{parsed.scheme}'",
            error=None,
        )
    except (FileNotFoundError, HTTPError, URLError, TimeoutError, ValueError) as exc:
        return SourceSnapshot(
            name=name,
            family=family,
            artifact_kind=artifact_kind,
            locator=locator,
            status="error",
            checked_at_utc=checked_at_utc,
            content_type=None,
            content_length=None,
            etag=None,
            last_modified=None,
            sha256=None,
            requires_manual_review=False,
            manual_review_reason=None,
            error=str(exc),
        )


def capture_file_source(
    name: str,
    family: str,
    artifact_kind: str,
    path: Path,
    checked_at_utc: str,
    hash_mode: str,
) -> SourceSnapshot:
    if not path.is_file():
        raise FileNotFoundError(f"file source does not exist: {path}")

    payload = path.read_bytes() if hash_mode == "body" else b""
    stat = path.stat()

    return SourceSnapshot(
        name=name,
        family=family,
        artifact_kind=artifact_kind,
        locator=str(path),
        status="ok",
        checked_at_utc=checked_at_utc,
        content_type=mimetypes.guess_type(path.name)[0],
        content_length=stat.st_size,
        etag=None,
        last_modified=datetime.fromtimestamp(stat.st_mtime, UTC).isoformat(),
        sha256=hashlib.sha256(payload).hexdigest() if payload else hash_file(path),
        requires_manual_review=False,
        manual_review_reason=None,
        error=None,
    )


def capture_http_source(
    name: str,
    family: str,
    artifact_kind: str,
    url: str,
    checked_at_utc: str,
    hash_mode: str,
    body_limit: int,
) -> SourceSnapshot:
    head_request = Request(url, method="HEAD")
    with urlopen(head_request, timeout=HTTP_TIMEOUT_SECONDS) as response:
        headers = response.headers
        content_type = headers.get_content_type()
        content_length = try_parse_int(headers.get("Content-Length"))
        etag = headers.get("ETag")
        last_modified = headers.get("Last-Modified")

    sha256: str | None = None
    manual_review_reason: str | None = None
    requires_manual_review = False

    if hash_mode == "body":
        if content_length is not None and content_length > body_limit:
            requires_manual_review = True
            manual_review_reason = (
                f"content length {content_length} exceeds body hash limit {body_limit}; "
                "metadata captured only"
            )
        else:
            get_request = Request(url, method="GET")
            with urlopen(get_request, timeout=HTTP_TIMEOUT_SECONDS) as response:
                payload = response.read()
            sha256 = hashlib.sha256(payload).hexdigest()

    return SourceSnapshot(
        name=name,
        family=family,
        artifact_kind=artifact_kind,
        locator=url,
        status="ok",
        checked_at_utc=checked_at_utc,
        content_type=content_type,
        content_length=content_length,
        etag=etag,
        last_modified=last_modified,
        sha256=sha256,
        requires_manual_review=requires_manual_review,
        manual_review_reason=manual_review_reason,
        error=None,
    )


def build_diff_report(previous: dict[str, Any], current: dict[str, Any]) -> dict[str, Any]:
    previous_sources = {record["name"]: record for record in previous.get("sources", [])}
    current_sources = {record["name"]: record for record in current.get("sources", [])}

    all_names = sorted(set(previous_sources) | set(current_sources))
    changes: list[dict[str, Any]] = []

    summary = {
        "newCount": 0,
        "changedCount": 0,
        "removedCount": 0,
        "unchangedCount": 0,
        "manualReviewCount": 0,
        "errorCount": 0,
    }

    for name in all_names:
        before = previous_sources.get(name)
        after = current_sources.get(name)
        change = classify_change(before, after)
        summary_key = {
            "new": "newCount",
            "changed": "changedCount",
            "removed": "removedCount",
            "unchanged": "unchangedCount",
            "manual-review": "manualReviewCount",
            "error": "errorCount",
        }[change["changeKind"]]
        summary[summary_key] += 1
        changes.append(change)

    return {
        "profileName": current.get("profileName", previous.get("profileName", "unknown")),
        "generatedAtUtc": utc_now(),
        "previousCheckedAtUtc": previous.get("checkedAtUtc"),
        "currentCheckedAtUtc": current.get("checkedAtUtc"),
        "summary": summary,
        "changes": changes,
    }


def classify_change(before: dict[str, Any] | None, after: dict[str, Any] | None) -> dict[str, Any]:
    if before is None and after is not None:
        return build_change_payload(after, None, "new", "new source discovered")

    if before is not None and after is None:
        return build_change_payload(before, None, "removed", "source missing from current snapshot")

    assert before is not None and after is not None

    if after.get("status") == "error":
        return build_change_payload(after, before, "error", after.get("error") or "source capture failed")

    if after.get("requires_manual_review"):
        return build_change_payload(
            after,
            before,
            "manual-review",
            after.get("manual_review_reason") or "manual review required",
        )

    if signature_for_change(before) == signature_for_change(after):
        return build_change_payload(after, before, "unchanged", "source signature unchanged")

    return build_change_payload(after, before, "changed", explain_change(before, after))


def build_change_payload(
    record: dict[str, Any],
    previous_record: dict[str, Any] | None,
    change_kind: str,
    reason: str,
) -> dict[str, Any]:
    return {
        "name": record.get("name"),
        "family": record.get("family"),
        "artifactKind": record.get("artifact_kind") or record.get("artifactKind"),
        "changeKind": change_kind,
        "reason": reason,
        "locator": record.get("locator"),
        "previousSha256": previous_record.get("sha256") if previous_record else None,
        "currentSha256": record.get("sha256"),
        "recommendations": build_recommendations(record.get("artifact_kind") or record.get("artifactKind"), change_kind),
    }


def build_recommendations(artifact_kind: str | None, change_kind: str) -> list[str]:
    if change_kind in {"unchanged", "removed"}:
        return []

    artifact = artifact_kind or "unknown"
    mapping = {
        "external_variables": ["rebuild-launcher-profile", "refresh-client-capabilities"],
        "external_texts": ["refresh-localization-import"],
        "figuredata": ["rebuild-figure-manifest", "recheck-avatar-assets"],
        "furnidata": ["rebuild-item-definitions", "recheck-catalog-content"],
        "productdata": ["rebuild-productdata-manifest", "recheck-catalog-pages"],
        "client_swf": ["inspect-swf-build", "refresh-client-build-manifest"],
        "avatar_swf": ["inspect-avatar-assets", "refresh-avatar-manifest"],
        "public_room_swf": ["inspect-public-room-assets", "refresh-public-room-manifest"],
        "protected_asset": ["manual-review-only"],
    }
    return mapping.get(artifact, ["manual-review-update"])


def explain_change(before: dict[str, Any], after: dict[str, Any]) -> str:
    if before.get("sha256") and after.get("sha256") and before.get("sha256") != after.get("sha256"):
        return "sha256 changed"
    if before.get("etag") and after.get("etag") and before.get("etag") != after.get("etag"):
        return "etag changed"
    if before.get("last_modified") != after.get("last_modified"):
        return "last-modified changed"
    if before.get("content_length") != after.get("content_length"):
        return "content length changed"
    return "metadata changed"


def signature_for_change(record: dict[str, Any]) -> tuple[Any, ...]:
    return (
        record.get("status"),
        record.get("sha256"),
        record.get("etag"),
        record.get("last_modified"),
        record.get("content_length"),
    )


def hash_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        while chunk := handle.read(1024 * 1024):
            digest.update(chunk)
    return digest.hexdigest()


def try_parse_int(value: str | None) -> int | None:
    if value is None:
        return None
    try:
        return int(value)
    except ValueError:
        return None


def utc_now() -> str:
    return datetime.now(UTC).isoformat()


if __name__ == "__main__":
    raise SystemExit(main())
