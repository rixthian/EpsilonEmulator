import { createReadStream, existsSync, statSync } from "node:fs";
import { createServer } from "node:http";
import { extname, join, normalize, resolve } from "node:path";
import { loadSiteRegistry } from "./registry.js";

const sites = loadSiteRegistry();
const host = process.env.CMS_HOST ?? "127.0.0.1";
const port = Number(process.env.CMS_PORT ?? "4100");
const gatewayBaseUrl = process.env.CMS_GATEWAY_URL ?? "http://127.0.0.1:5100";
const launcherBaseUrl = process.env.CMS_LAUNCHER_URL ?? "http://127.0.0.1:5001";

const contentTypes: Record<string, string> = {
  ".css": "text/css; charset=utf-8",
  ".gif": "image/gif",
  ".htm": "text/html; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".ico": "image/x-icon",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".png": "image/png",
  ".svg": "image/svg+xml",
  ".txt": "text/plain; charset=utf-8",
  ".webp": "image/webp"
};

type ServerRequest = import("node:http").IncomingMessage;
type ServerResponse = import("node:http").ServerResponse;

type CmsNewsItem = {
  slug: string;
  category: string;
  title: string;
  summary: string;
  ctaLabel: string;
  ctaHref: string;
};

type CmsPhotoItem = {
  slug: string;
  roomName: string;
  caption: string;
  palette: string;
};

type CmsLeaderboardEntry = {
  rank: number;
  username: string;
  label: string;
  score: string;
};

type CmsSupportTopic = {
  key: string;
  title: string;
  summary: string;
  audience: string;
};

type CmsLauncherPackage = {
  platformKey: string;
  label: string;
  fileKind: string;
  status: "planned" | "ready";
  downloadUrl: string | null;
};

type CmsHotelStatus = {
  gatewayUp: boolean;
  launcherUp: boolean;
  persistenceProvider: string;
  persistenceReady: boolean;
  realtimeTransport: string;
  activeRoomCount: number;
  activeGameSessionCount: number;
  totalScore: number;
};

type CmsSessionSummary = {
  authenticated: boolean;
  ticket: string | null;
  accountId: number | null;
  characterId: number | null;
  username: string | null;
  publicId: string | null;
  motto: string | null;
  figure: string | null;
  roomId: number | null;
  canLaunch: boolean;
  collectorTier: string | null;
  ownedCollectibleCount: number;
  launchMissingKeys: string[];
};

const cmsNews: CmsNewsItem[] = [
  {
    slug: "welcome",
    category: "Portal",
    title: "Bienvenido a Epsilon Hotel",
    summary: "Regístrate o entra para preparar tu acceso a la app de Epsilon desde la CMS.",
    ctaLabel: "Ir al portal",
    ctaHref: "/sites/epsilon-access/"
  },
  {
    slug: "launcher-access",
    category: "Launcher",
    title: "El juego se abre desde la app de Epsilon",
    summary: "La CMS autentica tu cuenta y genera tu código. La app instalada ejecuta el launcher y desde ahí corre el loader.",
    ctaLabel: "Ver acceso",
    ctaHref: "/sites/epsilon-access/#access"
  },
  {
    slug: "access-steps",
    category: "Ayuda",
    title: "Acceso paso a paso",
    summary: "1. Entra a tu cuenta. 2. Genera el código. 3. Abre la app. 4. El emulador confirma tu entrada real.",
    ctaLabel: "Abrir ayuda",
    ctaHref: "/sites/epsilon-access/#help"
  }
];

const cmsPhotos: CmsPhotoItem[] = [];

const cmsLeaderboard: CmsLeaderboardEntry[] = [];

const cmsSupportTopics: CmsSupportTopic[] = [
  {
    key: "account",
    title: "Cuenta y acceso",
    summary: "Registro, login y gestión de la cuenta web antes de abrir la app.",
    audience: "Todos"
  },
  {
    key: "launcher",
    title: "Launcher app",
    summary: "Genera tu código, abre la app instalada y deja que el launcher ejecute el loader.",
    audience: "Todos"
  },
  {
    key: "security",
    title: "Seguridad",
    summary: "No compartas tu código de inicio. Si dudas, genera uno nuevo desde la CMS.",
    audience: "Todos"
  },
  {
    key: "support",
    title: "Centro de ayuda",
    summary: "Soporte básico para acceso, launcher y problemas de cuenta.",
    audience: "Usuarios"
  }
];

const launcherPackages: CmsLauncherPackage[] = [
  { platformKey: "windows", label: "Windows", fileKind: ".exe", status: "planned", downloadUrl: null },
  { platformKey: "macos", label: "macOS", fileKind: ".dmg", status: "ready", downloadUrl: `${launcherBaseUrl}/launcher/downloads/macos-arm64` },
  { platformKey: "linux", label: "Linux", fileKind: ".AppImage", status: "planned", downloadUrl: null },
  { platformKey: "iphone", label: "iPhone", fileKind: "App Store", status: "planned", downloadUrl: null },
  { platformKey: "android", label: "Android", fileKind: ".apk", status: "planned", downloadUrl: null }
];

function sendJson(response: ServerResponse, status: number, payload: unknown): void {
  response.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "cache-control": "no-store, no-cache, must-revalidate"
  });
  response.end(JSON.stringify(payload, null, 2));
}

function sendText(response: ServerResponse, status: number, payload: string): void {
  response.writeHead(status, {
    "content-type": "text/plain; charset=utf-8",
    "cache-control": "no-store, no-cache, must-revalidate"
  });
  response.end(payload);
}

function sendFile(response: ServerResponse, path: string): void {
  const ext = extname(path).toLowerCase();
  response.writeHead(200, {
    "content-type": contentTypes[ext] ?? "application/octet-stream",
    "cache-control": ext === ".html" || ext === ".js" || ext === ".css"
      ? "no-store, no-cache, must-revalidate"
      : "public, max-age=300"
  });
  createReadStream(path).pipe(response);
}

async function readJsonBody(request: ServerRequest): Promise<unknown> {
  const chunks: Buffer[] = [];

  for await (const chunk of request) {
    chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
  }

  if (chunks.length === 0) {
    return null;
  }

  const raw = Buffer.concat(chunks).toString("utf8").trim();
  return raw.length === 0 ? null : JSON.parse(raw);
}

async function readFormBody(request: ServerRequest): Promise<URLSearchParams> {
  const chunks: Buffer[] = [];

  for await (const chunk of request) {
    chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
  }

  return new URLSearchParams(Buffer.concat(chunks).toString("utf8"));
}

function parseCookies(request: ServerRequest): Map<string, string> {
  const cookieHeader = request.headers.cookie ?? "";
  const entries = cookieHeader
    .split(";")
    .map((part: string) => part.trim())
    .filter((part: string) => part.includes("="))
    .map((part: string) => {
      const separatorIndex = part.indexOf("=");
      return [part.slice(0, separatorIndex), decodeURIComponent(part.slice(separatorIndex + 1))] as const;
    });

  return new Map(entries);
}

function sendRedirect(response: ServerResponse, location: string, cookieHeader?: string | null): void {
  const headers: Record<string, string> = {
    location,
    "cache-control": "no-store, no-cache, must-revalidate"
  };

  if (cookieHeader) {
    headers["set-cookie"] = cookieHeader;
  }

  response.writeHead(302, headers);
  response.end();
}

function resolveSitePath(siteRoot: string, requestPath: string): string | null {
  const safePath = normalize(requestPath).replace(/^(\.\.[/\\])+/, "");
  const resolved = resolve(siteRoot, "." + safePath);
  if (!resolved.startsWith(resolve(siteRoot))) {
    return null;
  }

  if (existsSync(resolved) && statSync(resolved).isDirectory()) {
    const indexPath = join(resolved, "index.html");
    return existsSync(indexPath) ? indexPath : null;
  }

  return existsSync(resolved) ? resolved : null;
}

async function fetchJson(
  baseUrl: string,
  path: string,
  init?: RequestInit,
  sessionTicket?: string
): Promise<{ status: number; payload: unknown }> {
  const headers = new Headers(init?.headers ?? {});
  if (!headers.has("content-type") && init?.body) {
    headers.set("content-type", "application/json");
  }

  if (sessionTicket) {
    headers.set("X-Epsilon-Session-Ticket", sessionTicket);
  }

  const httpResponse = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers
  });

  const text = await httpResponse.text();
  let payload: unknown = null;

  if (text.length > 0) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = { raw: text };
    }
  }

  return { status: httpResponse.status, payload };
}

async function delay(milliseconds: number): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, milliseconds));
}

async function performLoginWithRetry(loginName: unknown, password: unknown): Promise<{ status: number; payload: unknown }> {
  let lastResult = await fetchJson(gatewayBaseUrl, "/auth/development/login", {
    method: "POST",
    body: JSON.stringify({
      loginName,
      plainTextSecret: password,
      remoteAddress: "127.0.0.1"
    })
  });

  const initialPayload = lastResult.payload as Record<string, unknown> | null;
  if (lastResult.status < 400 || initialPayload?.failureCode !== "character_not_found") {
    return lastResult;
  }

  for (let attempt = 0; attempt < 4; attempt += 1) {
    await delay(120);
    lastResult = await fetchJson(gatewayBaseUrl, "/auth/development/login", {
      method: "POST",
      body: JSON.stringify({
        loginName,
        plainTextSecret: password,
        remoteAddress: "127.0.0.1"
      })
    });

    const payload = lastResult.payload as Record<string, unknown> | null;
    if (lastResult.status < 400 || payload?.failureCode !== "character_not_found") {
      return lastResult;
    }
  }

  return lastResult;
}

async function resolveCmsSession(request: ServerRequest): Promise<CmsSessionSummary> {
  const cookies = parseCookies(request);
  const ticket = cookies.get("epsilon_ticket")?.trim() ?? "";
  if (!ticket) {
    return {
      authenticated: false,
      ticket: null,
      accountId: null,
      characterId: null,
      username: null,
      publicId: null,
      motto: null,
      figure: null,
      roomId: null,
      canLaunch: false,
      collectorTier: null,
      ownedCollectibleCount: 0,
      launchMissingKeys: []
    };
  }

  const sessionResult = await fetchJson(gatewayBaseUrl, `/auth/development/sessions/${encodeURIComponent(ticket)}`);
  if (sessionResult.status >= 400) {
    return {
      authenticated: false,
      ticket: null,
      accountId: null,
      characterId: null,
      username: null,
      publicId: null,
      motto: null,
      figure: null,
      roomId: null,
      canLaunch: false,
      collectorTier: null,
      ownedCollectibleCount: 0,
      launchMissingKeys: []
    };
  }

  const sessionPayload = sessionResult.payload as Record<string, unknown>;
  const characterId = Number(sessionPayload.characterId ?? 0);
  const accountId = Number(sessionPayload.accountId ?? 0);

  const [characterResult, connectionResult, collectorResult, launchAccessResult] = await Promise.all([
    fetchJson(gatewayBaseUrl, `/hotel/characters/${characterId}`),
    fetchJson(gatewayBaseUrl, "/hotel/connection", { method: "GET" }, ticket),
    fetchJson(gatewayBaseUrl, "/hotel/collectibles/profile", { method: "GET" }, ticket),
    fetchJson(gatewayBaseUrl, "/hotel/collectibles/launch-access", { method: "GET" }, ticket)
  ]);

  const characterPayload = characterResult.payload as Record<string, unknown> | null;
  const profile = characterPayload?.profile as Record<string, unknown> | undefined;
  const connectionPayload = connectionResult.payload as Record<string, unknown> | null;
  const collectorPayload = collectorResult.payload as Record<string, unknown> | null;
  const launchPayload = launchAccessResult.payload as Record<string, unknown> | null;
  const launchRules = Array.isArray(launchPayload?.rules)
    ? launchPayload?.rules as Array<Record<string, unknown>>
    : [];

  return {
    authenticated: true,
    ticket,
    accountId: Number.isFinite(accountId) ? accountId : null,
    characterId: Number.isFinite(characterId) ? characterId : null,
    username: typeof profile?.username === "string" ? profile.username : null,
    publicId: typeof profile?.publicId === "string" ? profile.publicId : null,
    motto: typeof profile?.motto === "string" ? profile.motto : null,
    figure: typeof profile?.figure === "string" ? profile.figure : null,
    roomId: typeof connectionPayload?.currentRoomId === "number" ? connectionPayload.currentRoomId : null,
    canLaunch: Boolean(launchPayload?.canLaunch),
    collectorTier: typeof collectorPayload?.collectorTier === "string" ? collectorPayload.collectorTier : null,
    ownedCollectibleCount: typeof collectorPayload?.ownedCollectibleCount === "number" ? collectorPayload.ownedCollectibleCount : 0,
    launchMissingKeys: launchRules
      .filter((rule) => rule?.isSatisfied === false)
      .map((rule) => String(rule.ruleKey ?? "unknown"))
  };
}

function buildLauncherUrl(ticket: string): string {
  return `${launcherBaseUrl}/launcher/loader?ticket=${encodeURIComponent(ticket)}`;
}

async function loadHotelStatusSnapshot(): Promise<CmsHotelStatus> {
  const [gatewayHealth, launcherHealth, readiness, intelligence] = await Promise.all([
    fetchJson(gatewayBaseUrl, "/health"),
    fetchJson(launcherBaseUrl, "/health"),
    fetchJson(gatewayBaseUrl, "/readiness"),
    fetchJson(gatewayBaseUrl, "/diagnostics/intelligence")
  ]);

  const readinessPayload = readiness.payload as Record<string, unknown> | null;
  const intelligencePayload = intelligence.payload as Record<string, unknown> | null;
  const intelligenceGateway = intelligencePayload?.gateway as Record<string, unknown> | undefined;
  const realtime = intelligenceGateway?.realtime as Record<string, unknown> | undefined;
  const signals = intelligencePayload?.signals as Record<string, unknown> | undefined;
  const scorecard = intelligencePayload?.scorecard as Record<string, unknown> | undefined;

  return {
    gatewayUp: gatewayHealth.status < 400,
    launcherUp: launcherHealth.status < 400,
    persistenceProvider: typeof readinessPayload?.provider === "string" ? readinessPayload.provider : "unknown",
    persistenceReady: Boolean(readinessPayload?.isReady),
    realtimeTransport: typeof realtime?.transport === "string" ? realtime.transport : "unknown",
    activeRoomCount: typeof signals?.activeRoomCount === "number" ? signals.activeRoomCount : 0,
    activeGameSessionCount: typeof signals?.activeGameSessionCount === "number" ? signals.activeGameSessionCount : 0,
    totalScore: typeof scorecard?.total === "number" ? scorecard.total : 0
  };
}

async function handleCmsApi(request: ServerRequest, response: ServerResponse): Promise<boolean> {
  const url = new URL(request.url ?? "/", `http://${host}:${port}`);
  const pathname = url.pathname;

  if (pathname === "/cms/api/config" && request.method === "GET") {
    sendJson(response, 200, {
      siteName: "Epsilon Hotel",
      siteKey: "epsilon-access",
      launcherBaseUrl,
      launcherLoaderUrl: `${launcherBaseUrl}/launcher/loader`,
      defaultRoomId: 10
    });
    return true;
  }

  if (pathname === "/cms/api/status" && request.method === "GET") {
    const hotel = await loadHotelStatusSnapshot();
    sendJson(response, 200, {
      hotel
    });
    return true;
  }

  if (pathname === "/cms/api/hotel" && request.method === "GET") {
    sendJson(response, 200, await loadHotelStatusSnapshot());
    return true;
  }

  if (pathname === "/cms/api/me" && request.method === "GET") {
    const session = await resolveCmsSession(request);
    sendJson(response, 200, session);
    return true;
  }

  if (pathname === "/cms/api/telemetry/current" && request.method === "GET") {
    const session = await resolveCmsSession(request);
    if (!session.authenticated || !session.ticket) {
      sendJson(response, 401, { error: "unauthorized" });
      return true;
    }

    const result = await fetchJson(
      launcherBaseUrl,
      `/launcher/telemetry/current?ticket=${encodeURIComponent(session.ticket)}`
    );
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname === "/cms/api/connection/current" && request.method === "GET") {
    const session = await resolveCmsSession(request);
    if (!session.authenticated || !session.ticket) {
      sendJson(response, 401, { error: "unauthorized" });
      return true;
    }

    const result = await fetchJson(
      launcherBaseUrl,
      `/launcher/connection-state?ticket=${encodeURIComponent(session.ticket)}`
    );
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname.startsWith("/cms/api/profile/") && request.method === "GET") {
    const publicId = pathname.replace("/cms/api/profile/", "").trim();
    if (!publicId) {
      sendJson(response, 400, { error: "public_id_required" });
      return true;
    }

    const result = await fetchJson(gatewayBaseUrl, `/hotel/characters/public/${encodeURIComponent(publicId)}`);
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname === "/cms/api/news" && request.method === "GET") {
    sendJson(response, 200, cmsNews);
    return true;
  }

  if (pathname === "/cms/api/photos" && request.method === "GET") {
    sendJson(response, 200, cmsPhotos);
    return true;
  }

  if (pathname === "/cms/api/leaderboard" && request.method === "GET") {
    sendJson(response, 200, cmsLeaderboard);
    return true;
  }

  if (pathname === "/cms/api/community" && request.method === "GET") {
    sendJson(response, 200, {
      news: cmsNews,
      photos: cmsPhotos,
      leaderboard: cmsLeaderboard
    });
    return true;
  }

  if (pathname === "/cms/api/support" && request.method === "GET") {
    sendJson(response, 200, cmsSupportTopics);
    return true;
  }

  if (pathname === "/cms/api/settings" && request.method === "GET") {
    const hotel = await loadHotelStatusSnapshot();
    sendJson(response, 200, {
      cmsKind: "node-cms-backend",
      launcherSeparated: true,
      defaultLanguage: "es",
      launcherBaseUrl,
      gatewayBaseUrl,
      defaultRoomId: 10,
      launcherPackages,
      hotel
    });
    return true;
  }

  if (pathname === "/cms/api/home" && request.method === "GET") {
    const [session, hotel] = await Promise.all([
      resolveCmsSession(request),
      loadHotelStatusSnapshot()
    ]);

    sendJson(response, 200, {
      session,
      hotel,
      news: cmsNews,
      photos: cmsPhotos,
      leaderboard: cmsLeaderboard,
      support: cmsSupportTopics,
      settings: {
        cmsKind: "node-cms-backend",
        launcherSeparated: true,
        defaultLanguage: "es",
        launcherPackages
      }
    });
    return true;
  }

  if (pathname === "/cms/api/auth/register" && request.method === "POST") {
    const body = await readJsonBody(request) as Record<string, unknown> | null;
    const username = typeof body?.username === "string" ? body.username.trim() : "";
    const email = typeof body?.email === "string" ? body.email.trim() : "";
    const password = typeof body?.password === "string" ? body.password.trim() : "";

    const register = await fetchJson(gatewayBaseUrl, "/auth/register", {
      method: "POST",
      body: JSON.stringify({ username, email, password })
    });

    if (register.status >= 400) {
      sendJson(response, register.status, register.payload);
      return true;
    }

    const login = await performLoginWithRetry(username, password);
    const loginPayload = login.payload as Record<string, unknown> | null;
    const sessionTicket = loginPayload?.session && typeof loginPayload.session === "object"
      ? (loginPayload.session as Record<string, unknown>).ticket
      : null;

    if (login.status >= 400 || typeof sessionTicket !== "string") {
      sendJson(response, login.status, login.payload);
      return true;
    }

    response.setHeader("set-cookie", `epsilon_ticket=${encodeURIComponent(sessionTicket)}; Path=/; HttpOnly; SameSite=Lax`);
    sendJson(response, 200, {
      succeeded: true,
      launcherUrl: buildLauncherUrl(sessionTicket),
      nextUrl: "/sites/epsilon-access/#access",
      ticket: sessionTicket
    });
    return true;
  }

  if (pathname === "/cms/api/auth/login" && request.method === "POST") {
    const body = await readJsonBody(request) as Record<string, unknown> | null;
    const loginName = typeof body?.loginName === "string" ? body.loginName.trim() : "";
    const password = typeof body?.password === "string" ? body.password.trim() : "";
    const login = await performLoginWithRetry(loginName, password);
    const loginPayload = login.payload as Record<string, unknown> | null;
    const sessionTicket = loginPayload?.session && typeof loginPayload.session === "object"
      ? (loginPayload.session as Record<string, unknown>).ticket
      : null;

    if (login.status >= 400 || typeof sessionTicket !== "string") {
      sendJson(response, login.status, login.payload);
      return true;
    }

    response.setHeader("set-cookie", `epsilon_ticket=${encodeURIComponent(sessionTicket)}; Path=/; HttpOnly; SameSite=Lax`);
    sendJson(response, 200, {
      succeeded: true,
      launcherUrl: buildLauncherUrl(sessionTicket),
      nextUrl: "/sites/epsilon-access/#access",
      ticket: sessionTicket
    });
    return true;
  }

  if (pathname === "/cms/api/launcher/access" && request.method === "GET") {
    const session = await resolveCmsSession(request);
    if (!session.authenticated || !session.ticket) {
      sendJson(response, 401, { error: "unauthorized" });
      return true;
    }

    const currentCode = await fetchJson(
      launcherBaseUrl,
      `/launcher/access-codes/current?ticket=${encodeURIComponent(session.ticket)}`
    );

    sendJson(response, 200, {
      launcherUrl: buildLauncherUrl(session.ticket),
      appLaunchCode: currentCode.status < 400 ? currentCode.payload : null,
      launcherPackages
    });
    return true;
  }

  if (pathname === "/cms/api/launcher/code" && request.method === "POST") {
    const session = await resolveCmsSession(request);
    if (!session.authenticated || !session.ticket) {
      sendJson(response, 401, { error: "unauthorized" });
      return true;
    }

    const currentCode = await fetchJson(
      launcherBaseUrl,
      `/launcher/access-codes/current?ticket=${encodeURIComponent(session.ticket)}`
    );

    if (currentCode.status < 400 && currentCode.payload) {
      sendJson(response, 200, currentCode.payload);
      return true;
    }

    const body = await readJsonBody(request) as Record<string, unknown> | null;
    const platformKind = typeof body?.platformKind === "string" ? body.platformKind.trim() : null;
    const issued = await fetchJson(
      launcherBaseUrl,
      "/launcher/access-codes",
      {
        method: "POST",
        body: JSON.stringify({
          ticket: session.ticket,
          platformKind
        })
      }
    );

    sendJson(response, issued.status, issued.payload);
    return true;
  }

  if (pathname === "/cms/api/auth/logout" && request.method === "POST") {
    response.setHeader("set-cookie", "epsilon_ticket=; Path=/; Max-Age=0; SameSite=Lax");
    sendJson(response, 200, { succeeded: true });
    return true;
  }

  if (pathname === "/cms/api/launcher/session" && request.method === "POST") {
    const session = await resolveCmsSession(request);
    if (!session.authenticated || !session.ticket) {
      sendJson(response, 401, { error: "unauthorized" });
      return true;
    }

    sendJson(response, 200, {
      launcherUrl: buildLauncherUrl(session.ticket),
      roomId: session.roomId ?? 10,
      canLaunch: session.canLaunch
    });
    return true;
  }

  return false;
}

async function handleLegacyEpsilonApi(request: ServerRequest, response: ServerResponse): Promise<boolean> {
  const url = new URL(request.url ?? "/", `http://${host}:${port}`);
  const pathname = url.pathname;
  const cookies = parseCookies(request);

  if (pathname === "/api/epsilon/config" && request.method === "GET") {
    sendJson(response, 200, {
      gatewayBaseUrl,
      launcherBaseUrl,
      defaultRoomId: 10,
      generations: sites.map((site) => ({
        key: site.key,
        displayName: site.displayName,
        era: site.era,
        href: `/sites/${site.key}/`
      }))
    });
    return true;
  }

  if (pathname === "/api/epsilon/health" && request.method === "GET") {
    const [gateway, launcher] = await Promise.all([
      fetchJson(gatewayBaseUrl, "/health"),
      fetchJson(launcherBaseUrl, "/health")
    ]);
    sendJson(response, 200, { gateway: gateway.payload, launcher: launcher.payload });
    return true;
  }

  if (pathname === "/api/epsilon/register" && request.method === "POST") {
    const body = await readJsonBody(request) as Record<string, unknown> | null;
    const result = await fetchJson(gatewayBaseUrl, "/auth/register", {
      method: "POST",
      body: JSON.stringify({
        username: body?.username,
        email: body?.email,
        password: body?.password
      })
    });
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname === "/api/epsilon/login" && request.method === "POST") {
    const body = await readJsonBody(request) as Record<string, unknown> | null;
    const result = await performLoginWithRetry(body?.loginName, body?.password);
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname === "/api/epsilon/register-and-login" && request.method === "POST") {
    const body = await readJsonBody(request) as Record<string, unknown> | null;
    const register = await fetchJson(gatewayBaseUrl, "/auth/register", {
      method: "POST",
      body: JSON.stringify({
        username: body?.username,
        email: body?.email,
        password: body?.password
      })
    });

    if (register.status >= 400) {
      sendJson(response, register.status, register.payload);
      return true;
    }

    const login = await performLoginWithRetry(body?.username, body?.password);
    sendJson(response, login.status, {
      register: register.payload,
      login: login.payload
    });
    return true;
  }

  if (pathname === "/api/epsilon/bootstrap" && request.method === "GET") {
    const sessionTicket = url.searchParams.get("ticket")?.trim() || cookies.get("epsilon_ticket");
    const result = await fetchJson(
      launcherBaseUrl,
      "/launcher/bootstrap",
      { method: "GET" },
      sessionTicket || undefined
    );
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname === "/api/epsilon/connection" && request.method === "GET") {
    const sessionTicket = url.searchParams.get("ticket")?.trim() || cookies.get("epsilon_ticket");
    const result = await fetchJson(
      gatewayBaseUrl,
      "/hotel/connection",
      { method: "GET" },
      sessionTicket || undefined
    );
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname === "/api/epsilon/prepare-collector" && request.method === "POST") {
    sendJson(response, 410, {
      error: "collector_auto_prepare_removed",
      detail: "Launcher access can no longer be granted from CMS automation. Use wallet verification and explicit access-code flow."
    });
    return true;
  }

  if (pathname === "/api/epsilon/rooms/entry" && request.method === "POST") {
    const body = await readJsonBody(request) as Record<string, unknown> | null;
    const sessionTicket =
      (typeof body?.ticket === "string" ? body.ticket.trim() : "") ||
      cookies.get("epsilon_ticket") ||
      "";
    const result = await fetchJson(
      gatewayBaseUrl,
      "/hotel/rooms/entry",
      {
        method: "POST",
        body: JSON.stringify({
          roomId: body?.roomId ?? 10,
          password: body?.password ?? null,
          spectatorMode: Boolean(body?.spectatorMode)
        })
      },
      sessionTicket || undefined
    );
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname === "/api/epsilon/rooms/move" && request.method === "POST") {
    const body = await readJsonBody(request) as Record<string, unknown> | null;
    const sessionTicket =
      (typeof body?.ticket === "string" ? body.ticket.trim() : "") ||
      cookies.get("epsilon_ticket") ||
      "";
    const result = await fetchJson(
      gatewayBaseUrl,
      "/hotel/rooms/move",
      {
        method: "POST",
        body: JSON.stringify({
          roomId: body?.roomId ?? 10,
          destinationX: body?.destinationX,
          destinationY: body?.destinationY
        })
      },
      sessionTicket || undefined
    );
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname === "/api/epsilon/rooms/chat" && request.method === "POST") {
    const body = await readJsonBody(request) as Record<string, unknown> | null;
    const sessionTicket =
      (typeof body?.ticket === "string" ? body.ticket.trim() : "") ||
      cookies.get("epsilon_ticket") ||
      "";
    const result = await fetchJson(
      gatewayBaseUrl,
      "/hotel/rooms/chat",
      {
        method: "POST",
        body: JSON.stringify({
          roomId: body?.roomId ?? 10,
          message: body?.message
        })
      },
      sessionTicket || undefined
    );
    sendJson(response, result.status, result.payload);
    return true;
  }

  if (pathname === "/api/epsilon/rooms/runtime" && request.method === "GET") {
    const roomId = Number(url.searchParams.get("roomId") ?? "10");
    const result = await fetchJson(gatewayBaseUrl, `/hotel/rooms/${roomId}/runtime`);
    sendJson(response, result.status, result.payload);
    return true;
  }

  return false;
}

async function handlePortalActions(request: ServerRequest, response: ServerResponse): Promise<boolean> {
  const url = new URL(request.url ?? "/", `http://${host}:${port}`);
  const pathname = url.pathname;
  const cookies = parseCookies(request);

  if (pathname === "/portal/register" && request.method === "POST") {
    const body = await readFormBody(request);
    const username = body.get("username")?.trim() ?? "";
    const email = body.get("email")?.trim() ?? "";
    const password = body.get("password")?.trim() ?? "";

    const register = await fetchJson(gatewayBaseUrl, "/auth/register", {
      method: "POST",
      body: JSON.stringify({ username, email, password })
    });
    const registerPayload = register.payload as Record<string, unknown> | null;
    if (register.status >= 400) {
      sendRedirect(
        response,
        `/sites/epsilon-access/register.html?error=${encodeURIComponent(String(registerPayload?.failureCode ?? "register_failed"))}`
      );
      return true;
    }

    const login = await performLoginWithRetry(username, password);
    const loginPayload = login.payload as Record<string, unknown> | null;
    const ticket = loginPayload?.session && typeof loginPayload.session === "object"
      ? (loginPayload.session as Record<string, unknown>).ticket
      : null;

    if (login.status >= 400 || typeof ticket !== "string") {
      sendRedirect(
        response,
        `/sites/epsilon-access/login.html?error=${encodeURIComponent(String(loginPayload?.failureCode ?? "login_failed"))}&username=${encodeURIComponent(username)}`
      );
      return true;
    }

    sendRedirect(
      response,
      "/sites/epsilon-access/?welcome=1#access",
      `epsilon_ticket=${encodeURIComponent(ticket)}; Path=/; HttpOnly; SameSite=Lax`
    );
    return true;
  }

  if (pathname === "/portal/login" && request.method === "POST") {
    const body = await readFormBody(request);
    const loginName = body.get("loginName")?.trim() ?? "";
    const password = body.get("password")?.trim() ?? "";
    const login = await performLoginWithRetry(loginName, password);
    const loginPayload = login.payload as Record<string, unknown> | null;
    const ticket = loginPayload?.session && typeof loginPayload.session === "object"
      ? (loginPayload.session as Record<string, unknown>).ticket
      : null;

    if (login.status >= 400 || typeof ticket !== "string") {
      sendRedirect(
        response,
        `/sites/epsilon-access/login.html?error=${encodeURIComponent(String(loginPayload?.failureCode ?? "login_failed"))}&username=${encodeURIComponent(loginName)}`
      );
      return true;
    }

    sendRedirect(
      response,
      "/sites/epsilon-access/?welcome=1#access",
      `epsilon_ticket=${encodeURIComponent(ticket)}; Path=/; HttpOnly; SameSite=Lax`
    );
    return true;
  }

  if (pathname === "/portal/logout" && request.method === "POST") {
    sendRedirect(response, "/sites/epsilon-access/", "epsilon_ticket=; Path=/; Max-Age=0; SameSite=Lax");
    return true;
  }

  if (
    (pathname === "/portal/hotel/enter" || pathname === "/portal/hotel/move" || pathname === "/portal/hotel/chat")
    && request.method === "POST"
  ) {
    const ticket = cookies.get("epsilon_ticket") ?? "";
    if (!ticket) {
      sendRedirect(response, "/sites/epsilon-access/login.html");
      return true;
    }

    sendRedirect(response, buildLauncherUrl(ticket));
    return true;
  }

  return false;
}

function renderIndex(): string {
  const items = sites.map((site) =>
    `<li><a href="/sites/${site.key}/">${site.displayName}</a> <small>${site.era}</small></li>`
  ).join("");

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>Epsilon CMS Generations</title>
</head>
<body>
  <h1>Epsilon CMS Generations</h1>
  <p>Preserved CMS packages served by the Node.js + TypeScript CMS system.</p>
  <ul>${items}</ul>
</body>
</html>`;
}

createServer(async (request: ServerRequest, response: ServerResponse) => {
  const url = new URL(request.url ?? "/", `http://${host}:${port}`);
  const pathname = url.pathname;

  if (pathname === "/") {
    response.writeHead(302, { location: "/sites/epsilon-access/" });
    return;
  }

  if (pathname === "/api/sites") {
    sendJson(response, 200, sites);
    return;
  }

  if (pathname.startsWith("/cms/api/")) {
    try {
      if (await handleCmsApi(request, response)) {
        return;
      }
    } catch (error) {
      sendJson(response, 500, {
        error: "cms_backend_failed",
        detail: error instanceof Error ? error.message : "Unknown error"
      });
      return;
    }
  }

  if (pathname.startsWith("/api/epsilon/")) {
    try {
      if (await handleLegacyEpsilonApi(request, response)) {
        return;
      }
    } catch (error) {
      sendJson(response, 500, {
        error: "epsilon_proxy_failed",
        detail: error instanceof Error ? error.message : "Unknown error"
      });
      return;
    }
  }

  if (pathname.startsWith("/portal/")) {
    try {
      if (await handlePortalActions(request, response)) {
        return;
      }
    } catch {
      sendRedirect(response, "/sites/epsilon-access/?error=portal_failure");
      return;
    }
  }

  if (pathname === "/portal") {
    response.writeHead(200, { "content-type": "text/html; charset=utf-8" });
    response.end(renderIndex());
    return;
  }

  if (pathname.startsWith("/sites/")) {
    const parts = pathname.split("/").filter(Boolean);
    const siteKey = parts[1];
    const site = sites.find((candidate) => candidate.key === siteKey);

    if (!site) {
      sendJson(response, 404, { error: "site_not_found" });
      return;
    }

    const relativePath = "/" + parts.slice(2).join("/");
    if (siteKey === "epsilon-access") {
      const cookies = parseCookies(request);
      const hasTicket = Boolean(cookies.get("epsilon_ticket"));
      const normalizedPath = relativePath === "/" ? "/index.html" : relativePath;

      if (normalizedPath === "/hotel.html" && !hasTicket) {
        sendRedirect(response, "/sites/epsilon-access/login.html");
        return;
      }

      if (normalizedPath === "/hotel.html" && hasTicket) {
        sendRedirect(
          response,
          buildLauncherUrl(cookies.get("epsilon_ticket") ?? "")
        );
        return;
      }

      if ((normalizedPath === "/login.html" || normalizedPath === "/register.html") && hasTicket) {
        sendRedirect(
          response,
          buildLauncherUrl(cookies.get("epsilon_ticket") ?? "")
        );
        return;
      }
    }

    const filePath = resolveSitePath(site.rootDir, relativePath === "/" ? "/index.html" : relativePath);
    if (!filePath) {
      sendJson(response, 404, { error: "asset_not_found" });
      return;
    }

    sendFile(response, filePath);
    return;
  }

  sendText(response, 404, "not_found");
}).listen(port, host, () => {
  console.log(`CMS system listening on http://${host}:${port}`);
  console.log(`Loaded ${sites.length} preserved generations.`);
});
