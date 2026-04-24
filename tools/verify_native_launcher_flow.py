#!/usr/bin/env python3
"""Verify the real CMS -> native launcher app -> client -> emulator flow."""

from __future__ import annotations

import argparse
import json
import plistlib
import subprocess
import sys
import time
from pathlib import Path

from verify_connection_flow import (
    JsonSession,
    VerificationError,
    fetch_connection_state,
    gateway_headers,
    print_step,
    random_username,
    random_wallet_address,
    require,
    require_phase,
    require_status,
    wait_for_phase,
)


def verify_plist_url_scheme(plist_path: Path, expected_scheme: str) -> None:
    require(plist_path.exists(), f"launcher plist not found: {plist_path}")
    with plist_path.open("rb") as handle:
        payload = plistlib.load(handle)

    bundle_url_types = payload.get("CFBundleURLTypes", [])
    require(isinstance(bundle_url_types, list), "CFBundleURLTypes missing or invalid")

    schemes: list[str] = []
    for entry in bundle_url_types:
        if not isinstance(entry, dict):
            continue
        raw_schemes = entry.get("CFBundleURLSchemes", [])
        if isinstance(raw_schemes, list):
            schemes.extend(str(item) for item in raw_schemes)

    require(
        expected_scheme in schemes,
        f"launcher plist does not register {expected_scheme!r}; schemes={schemes!r}",
    )
    print_step(
        "launcher_plist",
        {
            "plistPath": str(plist_path),
            "registeredSchemes": schemes,
        },
    )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Verify the real CMS -> native launcher app -> client -> emulator flow."
    )
    parser.add_argument("--cms-base-url", default="http://127.0.0.1:4100")
    parser.add_argument("--launcher-base-url", default="http://127.0.0.1:5001")
    parser.add_argument("--gateway-base-url", default="http://127.0.0.1:5100")
    parser.add_argument("--room-id", type=int, default=10)
    parser.add_argument("--platform-kind", default="macOS")
    parser.add_argument("--profile-key", default="loader-desktop")
    parser.add_argument("--username-prefix", default="verifyapp")
    parser.add_argument("--password", default="TestPass123")
    parser.add_argument("--timeout-seconds", type=float, default=30.0)
    parser.add_argument(
        "--launcher-app",
        default="/Users/yasminluengo/Documents/Playground/EpsilonEmulator/apps/epsilon-launcher-native/dist/macos/EpsilonLauncher.app",
    )
    parser.add_argument(
        "--launcher-executable",
        default="/Users/yasminluengo/Documents/Playground/EpsilonEmulator/apps/epsilon-launcher-native/dist/macos/EpsilonLauncher.app/Contents/MacOS/EpsilonLauncher",
    )
    parser.add_argument("--launch-via-app-bundle", action="store_true", default=True)
    parser.add_argument(
        "--launcher-plist",
        default="/Users/yasminluengo/Documents/Playground/EpsilonEmulator/apps/epsilon-launcher-native/dist/macos/EpsilonLauncher.app/Contents/Info.plist",
    )
    parser.add_argument("--expected-scheme", default="epsilonlauncher")
    args = parser.parse_args()

    session = JsonSession()
    verify_plist_url_scheme(Path(args.launcher_plist), args.expected_scheme)

    health_urls = {
        "cms": f"{args.cms_base_url}/cms/api/status",
        "launcher": f"{args.launcher_base_url}/health",
        "gateway": f"{args.gateway_base_url}/health",
    }

    for key, url in health_urls.items():
        result = session.request_json("GET", url)
        require_status(result, 200, f"{key} health")
        print_step(f"{key}_health", result.payload)

    username = random_username(args.username_prefix)
    email = f"{username}@example.com"

    register = session.request_json(
        "POST",
        f"{args.cms_base_url}/cms/api/auth/register",
        {
            "username": username,
            "email": email,
            "password": args.password,
        },
    )
    require_status(register, 200, "cms register")
    require(isinstance(register.payload, dict), "cms register: payload is not an object")
    ticket = register.payload.get("ticket")
    require(isinstance(ticket, str) and ticket, "cms register: ticket missing")
    print_step("cms_register", register.payload)

    initial_state = fetch_connection_state(session, args.launcher_base_url, ticket)
    require_phase(
        initial_state,
        "launch_blocked",
        "initial connection-state",
        presence_confirmed=False,
        current_room_id=None,
    )
    print_step("state_initial", initial_state)

    wallet_address = random_wallet_address()
    challenge = session.request_json(
        "POST",
        f"{args.gateway_base_url}/hotel/collectibles/wallet/challenges",
        {
            "walletAddress": wallet_address,
            "walletProvider": "metamask",
        },
        headers=gateway_headers(ticket),
    )
    require_status(challenge, 200, "wallet challenge")
    challenge_id = challenge.payload.get("challengeId")
    nonce = challenge.payload.get("nonce")
    require(isinstance(challenge_id, str) and challenge_id, "wallet challenge: challengeId missing")
    require(isinstance(nonce, str) and nonce, "wallet challenge: nonce missing")
    print_step("wallet_challenge", challenge.payload)

    verify = session.request_json(
        "POST",
        f"{args.gateway_base_url}/hotel/collectibles/wallet/verify",
        {
            "challengeId": challenge_id,
            "signature": f"devsig:{nonce}",
        },
        headers=gateway_headers(ticket),
    )
    require_status(verify, 200, "wallet verify")
    print_step("wallet_verify", verify.payload)

    ownership = session.request_json(
        "POST",
        f"{args.gateway_base_url}/hotel/collectibles/dev/ownership",
        {
            "categoryKeys": ["avatar"],
        },
        headers=gateway_headers(ticket),
    )
    require_status(ownership, 200, "dev ownership")
    print_step("dev_ownership", ownership.payload)

    entitled_state = fetch_connection_state(session, args.launcher_base_url, ticket)
    require_phase(
        entitled_state,
        "cms_authenticated",
        "post-entitlement connection-state",
        presence_confirmed=False,
        current_room_id=None,
    )
    print_step("state_cms_authenticated", entitled_state)

    issued = session.request_json(
        "POST",
        f"{args.launcher_base_url}/launcher/access-codes",
        {
            "ticket": ticket,
            "platformKind": args.platform_kind,
        },
    )
    require_status(issued, 200, "launcher access-code issue")
    code = issued.payload.get("code")
    require(isinstance(code, str) and code, "launcher access-code issue: code missing")
    print_step("code_issued", issued.payload)

    app_path = Path(args.launcher_app)
    executable_path = Path(args.launcher_executable)
    if args.launch_via_app_bundle:
        require(app_path.exists(), f"launcher app not found: {app_path}")
        launch_command = ["open", "-na", str(app_path), "--args", f"--code={code}"]
        launch_descriptor = str(app_path)
    else:
        require(executable_path.exists(), f"launcher executable not found: {executable_path}")
        launch_command = [str(executable_path), f"--code={code}"]
        launch_descriptor = str(executable_path)

    process = subprocess.Popen(
        launch_command,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        stdin=subprocess.DEVNULL,
        start_new_session=True,
    )
    print_step(
        "launcher_process_started",
        {
            "pid": process.pid,
            "launcherTarget": launch_descriptor,
            "launchCommand": launch_command,
            "code": code,
        },
    )

    try:
        client_started_state = wait_for_phase(
            session,
            args.launcher_base_url,
            ticket,
            "client_started",
            timeout_seconds=args.timeout_seconds,
        )
        require_phase(
            client_started_state,
            "client_started",
            "connection-state after native launcher start",
            presence_confirmed=False,
            current_room_id=None,
        )
        print_step("state_client_started", client_started_state)

        final_state = wait_for_phase(
            session,
            args.launcher_base_url,
            ticket,
            "presence_confirmed",
            timeout_seconds=args.timeout_seconds,
        )
        require_phase(
            final_state,
            "presence_confirmed",
            "final connection-state",
            presence_confirmed=True,
            current_room_id=args.room_id,
        )
        print_step("state_presence_confirmed", final_state)

        gateway_connection = session.request_json(
            "GET",
            f"{args.gateway_base_url}/hotel/connection",
            headers=gateway_headers(ticket),
        )
        require_status(gateway_connection, 200, "gateway connection after native launcher flow")
        require(
            gateway_connection.payload.get("currentRoomId") == args.room_id,
            f"gateway connection after native launcher flow: expected room {args.room_id}, payload={gateway_connection.payload!r}",
        )
        print_step("gateway_connection_after_native_launcher_flow", gateway_connection.payload)

        return 0
    finally:
        try:
            process.terminate()
        except Exception:
            pass
        time.sleep(1.0)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except VerificationError as error:
        print(f"[verify-native] ERROR: {error}", file=sys.stderr)
        raise SystemExit(1)
