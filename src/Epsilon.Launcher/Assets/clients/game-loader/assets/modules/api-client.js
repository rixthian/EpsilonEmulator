export class HttpRequestError extends Error {
  constructor(message, status, payload) {
    super(message);
    this.name = "HttpRequestError";
    this.status = status;
    this.payload = payload;
  }
}

// The loader talks to launcher/runtime APIs only. CMS account/community APIs are intentionally
// outside this client so the game loader cannot become a CMS surface by accident.
export class LauncherApiClient {
  constructor(options) {
    this.ticket = options.ticket || "";
    this.profileKey = options.profileKey || "loader-desktop";
    this.clientKind = options.clientKind || "loader";
    this.platformKind = options.platformKind || detectPlatformKind();
  }

  async request(path, init = {}) {
    const response = await fetch(path, init);
    const payload = await parsePayload(response);

    if (!response.ok) {
      const message = payload && payload.error
        ? String(payload.error)
        : response.statusText || "request_failed";
      throw new HttpRequestError(message, response.status, payload);
    }

    return payload;
  }

  async sendTelemetry(eventKey, detail, data = {}) {
    if (!this.ticket) {
      return;
    }

    try {
      await fetch("/launcher/telemetry", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({
          ticket: this.ticket,
          eventKey,
          detail: detail || null,
          data
        })
      });
    } catch {
      // Telemetry must never block loader boot.
    }
  }

  getBootstrap() {
    return this.request("/launcher/bootstrap?ticket=" + encodeURIComponent(this.ticket));
  }

  getCurrentSession() {
    return this.request("/launcher/session/current?ticket=" + encodeURIComponent(this.ticket));
  }

  recordClientStarted() {
    return this.request("/launcher/client-started", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({
        ticket: this.ticket,
        profileKey: this.profileKey,
        clientKind: this.clientKind,
        platformKind: this.platformKind
      })
    });
  }

  getConnection() {
    return this.request("/launcher/connection?ticket=" + encodeURIComponent(this.ticket));
  }

  getConnectionState() {
    return this.request("/launcher/connection-state?ticket=" + encodeURIComponent(this.ticket));
  }

  enterRoom(roomId) {
    return this.request("/launcher/runtime/room-entry", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({
        ticket: this.ticket,
        roomId,
        password: null,
        spectatorMode: false
      })
    });
  }

  getRoomSnapshot(roomId) {
    return this.request(
      "/launcher/runtime/room/" + encodeURIComponent(String(roomId)) +
      "?ticket=" + encodeURIComponent(this.ticket)
    );
  }

  sendRoomChat(roomId, message) {
    return this.request("/launcher/runtime/room-chat", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({
        ticket: this.ticket,
        roomId,
        message
      })
    });
  }
}

async function parsePayload(response) {
  const text = await response.text();
  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return { raw: text };
  }
}

function detectPlatformKind() {
  const platform = window.navigator.platform || "";
  const userAgent = window.navigator.userAgent || "";

  if (/mac/i.test(platform) || /mac os/i.test(userAgent)) {
    return "macOS";
  }

  if (/win/i.test(platform) || /windows/i.test(userAgent)) {
    return "Windows";
  }

  if (/linux/i.test(platform) || /linux/i.test(userAgent)) {
    return "Linux";
  }

  return "Unknown";
}
