#!/usr/bin/env python3
"""Verify the real CMS -> launcher -> emulator connection flow."""

from __future__ import annotations

import argparse
import http.cookiejar
import json
import random
import string
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from typing import Any


SESSION_TICKET_HEADER = "X-Epsilon-Session-Ticket"


class VerificationError(RuntimeError):
    """Raised when a real-state assertion fails."""


@dataclass
class HttpResult:
    status: int
    payload: Any
    headers: dict[str, str]


class JsonSession:
    def __init__(self) -> None:
        self.cookie_jar = http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(self.cookie_jar)
        )

    def request_json(
        self,
        method: str,
        url: str,
        payload: Any | None = None,
        headers: dict[str, str] | None = None,
        timeout: float = 10.0,
    ) -> HttpResult:
        request_headers = {
            "Accept": "application/json",
        }
        if headers:
            request_headers.update(headers)

        data: bytes | None = None
        if payload is not None:
            data = json.dumps(payload).encode("utf-8")
            request_headers["Content-Type"] = "application/json; charset=utf-8"

        request = urllib.request.Request(
            url,
            data=data,
            headers=request_headers,
            method=method,
        )

        try:
            with self.opener.open(request, timeout=timeout) as response:
                body = response.read().decode("utf-8", "replace")
                return HttpResult(
                    status=response.status,
                    payload=_decode_payload(body),
                    headers=dict(response.headers.items()),
                )
        except urllib.error.HTTPError as error:
            body = error.read().decode("utf-8", "replace")
            return HttpResult(
                status=error.code,
                payload=_decode_payload(body),
                headers=dict(error.headers.items()),
            )


def _decode_payload(body: str) -> Any:
    stripped = body.strip()
    if not stripped:
        return None
    try:
        return json.loads(stripped)
    except json.JSONDecodeError:
        return stripped


def require(condition: bool, message: str) -> None:
    if not condition:
        raise VerificationError(message)


def require_status(result: HttpResult, expected: int, context: str) -> None:
    require(
        result.status == expected,
        f"{context}: expected HTTP {expected}, got {result.status}, payload={result.payload!r}",
    )


def require_phase(
    state: dict[str, Any],
    expected_phase: str,
    context: str,
    *,
    presence_confirmed: bool,
    current_room_id: int | None,
) -> None:
    actual_phase = state.get("phaseKey")
    actual_presence = state.get("presenceConfirmed")
    actual_room_id = state.get("currentRoomId")
    require(
        actual_phase == expected_phase,
        f"{context}: expected phase {expected_phase}, got {actual_phase}, payload={state!r}",
    )
    require(
        actual_presence is presence_confirmed,
        f"{context}: expected presenceConfirmed={presence_confirmed}, got {actual_presence}, payload={state!r}",
    )
    require(
        actual_room_id == current_room_id,
        f"{context}: expected currentRoomId={current_room_id}, got {actual_room_id}, payload={state!r}",
    )


def gateway_headers(ticket: str) -> dict[str, str]:
    return {SESSION_TICKET_HEADER: ticket}


def random_wallet_address() -> str:
    alphabet = "0123456789abcdef"
    return "0x" + "".join(random.choice(alphabet) for _ in range(40))


def random_username(prefix: str) -> str:
    suffix = "".join(random.choice(string.digits) for _ in range(6))
    return f"{prefix}_{suffix}"


def fetch_connection_state(
    session: JsonSession,
    launcher_base_url: str,
    ticket: str,
) -> dict[str, Any]:
    result = session.request_json(
        "GET",
        f"{launcher_base_url}/launcher/connection-state?ticket={urllib.parse.quote(ticket)}",
    )
    require_status(result, 200, "launcher connection-state")
    require(isinstance(result.payload, dict), "launcher connection-state: payload is not an object")
    return result.payload


def fetch_cms_connection_state(session: JsonSession, cms_base_url: str) -> dict[str, Any]:
    result = session.request_json("GET", f"{cms_base_url}/cms/api/connection/current")
    require_status(result, 200, "cms connection/current")
    require(isinstance(result.payload, dict), "cms connection/current: payload is not an object")
    return result.payload


def fetch_cms_launcher_access(session: JsonSession, cms_base_url: str) -> dict[str, Any]:
    result = session.request_json("GET", f"{cms_base_url}/cms/api/launcher/access")
    require_status(result, 200, "cms launcher/access")
    require(isinstance(result.payload, dict), "cms launcher/access: payload is not an object")
    return result.payload


def wait_for_phase(
    session: JsonSession,
    launcher_base_url: str,
    ticket: str,
    expected_phase: str,
    *,
    timeout_seconds: float,
) -> dict[str, Any]:
    deadline = time.time() + timeout_seconds
    last_state: dict[str, Any] | None = None

    while time.time() < deadline:
        state = fetch_connection_state(session, launcher_base_url, ticket)
        last_state = state
        if state.get("phaseKey") == expected_phase:
            return state
        time.sleep(0.5)

    raise VerificationError(
        f"timeout waiting for phase {expected_phase}; last observed state={last_state!r}"
    )


def print_step(name: str, payload: Any) -> None:
    print(f"[verify] {name}")
    print(json.dumps(payload, indent=2, sort_keys=True))


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Verify the real CMS -> launcher -> emulator connection flow."
    )
    parser.add_argument("--cms-base-url", default="http://127.0.0.1:4100")
    parser.add_argument("--launcher-base-url", default="http://127.0.0.1:5001")
    parser.add_argument("--gateway-base-url", default="http://127.0.0.1:5100")
    parser.add_argument("--room-id", type=int, default=10)
    parser.add_argument("--platform-kind", default="macOS")
    parser.add_argument("--profile-key", default="loader-desktop")
    parser.add_argument("--username-prefix", default="verifyflow")
    parser.add_argument("--password", default="TestPass123")
    parser.add_argument("--timeout-seconds", type=float, default=10.0)
    args = parser.parse_args()

    session = JsonSession()

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

    cms_me = session.request_json("GET", f"{args.cms_base_url}/cms/api/me")
    require_status(cms_me, 200, "cms me")
    require(isinstance(cms_me.payload, dict), "cms me: payload is not an object")
    require(cms_me.payload.get("authenticated") is True, "cms me: session is not authenticated")
    require(cms_me.payload.get("username") == username, "cms me: username mismatch")
    require(cms_me.payload.get("canLaunch") is False, "cms me: canLaunch should still be false before entitlement")
    launch_missing_keys = cms_me.payload.get("launchMissingKeys")
    require(
        isinstance(launch_missing_keys, list) and "wallet_link" in launch_missing_keys and "premium_collectible" in launch_missing_keys,
        f"cms me: launchMissingKeys should expose real blockers, payload={cms_me.payload!r}",
    )
    print_step("cms_me", cms_me.payload)

    initial_state = fetch_connection_state(session, args.launcher_base_url, ticket)
    require_phase(
        initial_state,
        "launch_blocked",
        "initial connection-state",
        presence_confirmed=False,
        current_room_id=None,
    )
    print_step("state_initial", initial_state)

    blocked_issue = session.request_json(
        "POST",
        f"{args.launcher_base_url}/launcher/access-codes",
        {
            "ticket": ticket,
            "platformKind": args.platform_kind,
        },
    )
    require_status(blocked_issue, 403, "launcher access-code issue before entitlement")
    print_step("blocked_code_issue", blocked_issue.payload)

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
    require(isinstance(challenge.payload, dict), "wallet challenge: payload is not an object")
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
    require(entitled_state.get("launchPermitted") is True, "launch should be permitted after ownership")
    require_phase(
        entitled_state,
        "cms_authenticated",
        "post-entitlement connection-state",
        presence_confirmed=False,
        current_room_id=None,
    )
    print_step("state_cms_authenticated", entitled_state)

    cms_state_before_code = fetch_cms_connection_state(session, args.cms_base_url)
    require_phase(
        cms_state_before_code,
        "cms_authenticated",
        "cms connection/current before code",
        presence_confirmed=False,
        current_room_id=None,
    )
    print_step("cms_state_before_code", cms_state_before_code)

    cms_launcher_access_before_code = fetch_cms_launcher_access(session, args.cms_base_url)
    require(
        cms_launcher_access_before_code.get("appLaunchCode") is None,
        f"cms launcher/access before code issue: appLaunchCode should be null, payload={cms_launcher_access_before_code!r}",
    )
    print_step("cms_launcher_access_before_code", cms_launcher_access_before_code)

    issued = session.request_json(
        "POST",
        f"{args.launcher_base_url}/launcher/access-codes",
        {
            "ticket": ticket,
            "platformKind": args.platform_kind,
        },
    )
    require_status(issued, 200, "launcher access-code issue")
    require(isinstance(issued.payload, dict), "launcher access-code issue: payload is not an object")
    code = issued.payload.get("code")
    require(isinstance(code, str) and code, "launcher access-code issue: code missing")
    print_step("code_issued", issued.payload)

    current_code = session.request_json(
        "GET",
        f"{args.launcher_base_url}/launcher/access-codes/current?ticket={urllib.parse.quote(ticket)}",
    )
    require_status(current_code, 200, "launcher access-code current")
    require(isinstance(current_code.payload, dict), "launcher access-code current: payload is not an object")
    require(current_code.payload.get("code") == code, "launcher access-code current: code mismatch")
    print_step("code_current", current_code.payload)

    cms_launcher_access_after_code = fetch_cms_launcher_access(session, args.cms_base_url)
    app_launch_code = cms_launcher_access_after_code.get("appLaunchCode")
    require(isinstance(app_launch_code, dict), "cms launcher/access after code issue: appLaunchCode should exist")
    require(app_launch_code.get("code") == code, "cms launcher/access after code issue: code mismatch")
    print_step("cms_launcher_access_after_code", cms_launcher_access_after_code)

    issued_state = fetch_connection_state(session, args.launcher_base_url, ticket)
    require_phase(
        issued_state,
        "code_issued",
        "connection-state after code issue",
        presence_confirmed=False,
        current_room_id=None,
    )
    print_step("state_code_issued", issued_state)

    redeemed = session.request_json(
        "POST",
        f"{args.launcher_base_url}/launcher/access-codes/redeem",
        {
            "code": code,
            "platformKind": args.platform_kind,
            "deviceLabel": "anti-fail-verifier",
        },
    )
    require_status(redeemed, 200, "launcher access-code redeem")
    require(isinstance(redeemed.payload, dict), "launcher access-code redeem: payload is not an object")
    require(redeemed.payload.get("succeeded") is True, "launcher access-code redeem: succeeded should be true")
    require(redeemed.payload.get("ticket") == ticket, "launcher access-code redeem: ticket mismatch")
    print_step("code_redeemed", redeemed.payload)

    redeemed_again = session.request_json(
        "POST",
        f"{args.launcher_base_url}/launcher/access-codes/redeem",
        {
            "code": code,
            "platformKind": args.platform_kind,
            "deviceLabel": "anti-fail-verifier",
        },
    )
    require_status(redeemed_again, 404, "launcher access-code second redeem")
    print_step("code_redeemed_again", redeemed_again.payload)

    redeemed_state = fetch_connection_state(session, args.launcher_base_url, ticket)
    require_phase(
        redeemed_state,
        "code_redeemed",
        "connection-state after redeem",
        presence_confirmed=False,
        current_room_id=None,
    )
    print_step("state_code_redeemed", redeemed_state)

    profile_selection = session.request_json(
        "POST",
        f"{args.launcher_base_url}/launcher/launch-profiles/select",
        {
            "ticket": ticket,
            "profileKey": args.profile_key,
            "platformKind": args.platform_kind,
        },
    )
    require_status(profile_selection, 200, "launcher launch-profile select")
    require(isinstance(profile_selection.payload, dict), "launcher launch-profile select: payload is not an object")
    require(profile_selection.payload.get("succeeded") is True, "launch-profile select: succeeded should be true")
    print_step("profile_selected", profile_selection.payload)

    client_started = session.request_json(
        "POST",
        f"{args.launcher_base_url}/launcher/client-started",
        {
            "ticket": ticket,
            "profileKey": args.profile_key,
            "clientKind": "loader",
            "platformKind": args.platform_kind,
        },
    )
    require_status(client_started, 200, "launcher client-started")
    print_step("client_started", client_started.payload)

    client_started_state = fetch_connection_state(session, args.launcher_base_url, ticket)
    require_phase(
        client_started_state,
        "client_started",
        "connection-state after client-started",
        presence_confirmed=False,
        current_room_id=None,
    )
    print_step("state_client_started", client_started_state)

    gateway_connection_before_room = session.request_json(
        "GET",
        f"{args.gateway_base_url}/hotel/connection",
        headers=gateway_headers(ticket),
    )
    require_status(gateway_connection_before_room, 200, "gateway connection before room-entry")
    require(isinstance(gateway_connection_before_room.payload, dict), "gateway connection before room-entry: payload is not an object")
    require(
        gateway_connection_before_room.payload.get("currentRoomId") is None,
        f"gateway connection before room-entry: currentRoomId should be null, payload={gateway_connection_before_room.payload!r}",
    )
    print_step("gateway_connection_before_room_entry", gateway_connection_before_room.payload)

    room_entry = session.request_json(
        "POST",
        f"{args.launcher_base_url}/launcher/runtime/room-entry",
        {
            "ticket": ticket,
            "roomId": args.room_id,
            "spectatorMode": False,
        },
    )
    require_status(room_entry, 200, "launcher room-entry")
    print_step("room_entry", room_entry.payload)

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

    gateway_connection_after_room = session.request_json(
        "GET",
        f"{args.gateway_base_url}/hotel/connection",
        headers=gateway_headers(ticket),
    )
    require_status(gateway_connection_after_room, 200, "gateway connection after room-entry")
    require(isinstance(gateway_connection_after_room.payload, dict), "gateway connection after room-entry: payload is not an object")
    require(
        gateway_connection_after_room.payload.get("currentRoomId") == args.room_id,
        f"gateway connection after room-entry: expected room {args.room_id}, payload={gateway_connection_after_room.payload!r}",
    )
    print_step("gateway_connection_after_room_entry", gateway_connection_after_room.payload)

    cms_state_after_room = fetch_cms_connection_state(session, args.cms_base_url)
    require_phase(
        cms_state_after_room,
        "presence_confirmed",
        "cms connection/current after room-entry",
        presence_confirmed=True,
        current_room_id=args.room_id,
    )
    print_step("cms_state_after_room", cms_state_after_room)

    telemetry = session.request_json(
        "GET",
        f"{args.launcher_base_url}/launcher/telemetry/current?ticket={urllib.parse.quote(ticket)}",
    )
    require_status(telemetry, 200, "launcher telemetry/current")
    require(isinstance(telemetry.payload, list), "launcher telemetry/current: payload is not an array")
    event_keys = [candidate.get("eventKey") for candidate in telemetry.payload if isinstance(candidate, dict)]
    required_events = [
        "launcher_code_issued",
        "launcher_code_redeemed",
        "launcher_profile_selected",
        "launcher_client_started",
    ]
    missing_events = [event_key for event_key in required_events if event_key not in event_keys]
    require(not missing_events, f"launcher telemetry/current: missing events {missing_events!r}, payload={telemetry.payload!r}")
    print_step("telemetry", telemetry.payload)

    disconnect = session.request_json(
        "POST",
        f"{args.gateway_base_url}/hotel/connection/disconnect",
        headers=gateway_headers(ticket),
    )
    require_status(disconnect, 200, "gateway disconnect")
    print_step("disconnect", disconnect.payload)

    gateway_connection_after_disconnect = session.request_json(
        "GET",
        f"{args.gateway_base_url}/hotel/connection",
        headers=gateway_headers(ticket),
    )
    require_status(gateway_connection_after_disconnect, 401, "gateway connection after disconnect")
    print_step("gateway_connection_after_disconnect", gateway_connection_after_disconnect.payload)

    launcher_state_after_disconnect = session.request_json(
        "GET",
        f"{args.launcher_base_url}/launcher/connection-state?ticket={urllib.parse.quote(ticket)}",
    )
    require_status(launcher_state_after_disconnect, 401, "launcher connection-state after disconnect")
    print_step("launcher_state_after_disconnect", launcher_state_after_disconnect.payload)

    cms_me_after_disconnect = session.request_json("GET", f"{args.cms_base_url}/cms/api/me")
    require_status(cms_me_after_disconnect, 200, "cms me after disconnect")
    require(isinstance(cms_me_after_disconnect.payload, dict), "cms me after disconnect: payload is not an object")
    require(
        cms_me_after_disconnect.payload.get("authenticated") is False,
        f"cms me after disconnect: authenticated should be false, payload={cms_me_after_disconnect.payload!r}",
    )
    print_step("cms_me_after_disconnect", cms_me_after_disconnect.payload)

    cms_connection_after_disconnect = session.request_json(
        "GET",
        f"{args.cms_base_url}/cms/api/connection/current",
    )
    require_status(cms_connection_after_disconnect, 401, "cms connection/current after disconnect")
    print_step("cms_connection_after_disconnect", cms_connection_after_disconnect.payload)

    print("[verify] result")
    print(
        json.dumps(
            {
                "succeeded": True,
                "username": username,
                "ticket": ticket,
                "roomId": args.room_id,
                "finalPhase": final_state.get("phaseKey"),
                "disconnectValidated": True,
            },
            indent=2,
            sort_keys=True,
        )
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except VerificationError as error:
        print(f"[verify] FAILED: {error}", file=sys.stderr)
        raise SystemExit(1)
