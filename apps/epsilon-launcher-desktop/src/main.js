const { app, BrowserWindow, ipcMain, shell } = require("electron");
const fs = require("node:fs/promises");
const os = require("node:os");
const path = require("node:path");

const DEFAULT_LOCAL_CONFIG = {
  launcherId: "desktop-main",
  locale: "es-ES",
  hotelBaseUrl: "http://127.0.0.1:8081",
  launcherApiBaseUrl: "http://127.0.0.1:5001",
  defaultChannel: "stable",
  defaultProfileKey: "loader-desktop",
  autoLaunchOnRedeem: true,
  closeCmsOnLaunch: false,
  hardwareAcceleration: true,
  telemetryEnabled: true,
  rememberLastProfile: true
};

const HTTP_PROTOCOLS = new Set(["http:", "https:"]);

let mainWindow;

function detectPlatformKind() {
  switch (process.platform) {
    case "win32":
      return "Windows";
    case "darwin":
      return "macOS";
    case "linux":
      return "Linux";
    default:
      return process.platform;
  }
}

function getConfigPath() {
  return path.join(app.getPath("userData"), "config.json");
}

async function readLocalConfig() {
  const configPath = getConfigPath();
  try {
    const raw = await fs.readFile(configPath, "utf8");
    const parsed = JSON.parse(raw);
    return normalizeLocalConfig(parsed, { strict: false });
  } catch {
    return normalizeLocalConfig({}, { strict: false });
  }
}

async function writeLocalConfig(partialConfig) {
  const configPath = getConfigPath();
  const current = await readLocalConfig();
  const next = normalizeLocalConfig({
    ...current,
    ...pickLocalConfig(partialConfig || {})
  }, { strict: true });
  await fs.mkdir(path.dirname(configPath), { recursive: true });
  await fs.writeFile(configPath, JSON.stringify(next, null, 2), "utf8");
  return next;
}

async function fetchJson(baseUrl, pathname, options = {}) {
  const safeBaseUrl = normalizeHttpUrl(baseUrl, null, "launcher_base_url");
  const targetUrl = new URL(pathname, ensureTrailingSlash(safeBaseUrl)).toString();
  const response = await fetch(targetUrl, options);
  const text = await response.text();
  let payload = null;
  try {
    payload = text ? JSON.parse(text) : null;
  } catch {
    payload = { raw: text };
  }

  if (!response.ok) {
    const error = new Error((payload && payload.error) || response.statusText || "launcher_request_failed");
    error.payload = payload;
    error.status = response.status;
    throw error;
  }

  return payload;
}

function ensureTrailingSlash(value) {
  return value.endsWith("/") ? value : `${value}/`;
}

function pickLocalConfig(value) {
  if (!value || typeof value !== "object") {
    return {};
  }

  const result = {};
  for (const key of Object.keys(DEFAULT_LOCAL_CONFIG)) {
    if (Object.prototype.hasOwnProperty.call(value, key)) {
      result[key] = value[key];
    }
  }
  if (Object.prototype.hasOwnProperty.call(value, "lastProfileKey")) {
    result.lastProfileKey = value.lastProfileKey;
  }
  return result;
}

function normalizeLocalConfig(value, { strict }) {
  const raw = {
    ...DEFAULT_LOCAL_CONFIG,
    ...pickLocalConfig(value || {})
  };

  const fallback = normalizeHttpUrl(DEFAULT_LOCAL_CONFIG.launcherApiBaseUrl, null, "launcher_base_url");
  let hotelBaseUrl;
  let launcherApiBaseUrl;

  try {
    hotelBaseUrl = normalizeHttpUrl(raw.hotelBaseUrl, DEFAULT_LOCAL_CONFIG.hotelBaseUrl, "hotel_base_url");
    launcherApiBaseUrl = normalizeHttpUrl(raw.launcherApiBaseUrl, fallback, "launcher_base_url");
  } catch (error) {
    if (strict) {
      throw error;
    }
    hotelBaseUrl = DEFAULT_LOCAL_CONFIG.hotelBaseUrl;
    launcherApiBaseUrl = fallback;
  }

  return {
    ...raw,
    hotelBaseUrl,
    launcherApiBaseUrl,
    autoLaunchOnRedeem: Boolean(raw.autoLaunchOnRedeem),
    closeCmsOnLaunch: Boolean(raw.closeCmsOnLaunch),
    hardwareAcceleration: Boolean(raw.hardwareAcceleration),
    telemetryEnabled: Boolean(raw.telemetryEnabled),
    rememberLastProfile: Boolean(raw.rememberLastProfile),
    platform: detectPlatformKind()
  };
}

function normalizeHttpUrl(value, fallback, label) {
  const rawValue = typeof value === "string" && value.trim() ? value.trim() : fallback;
  if (!rawValue) {
    throw new Error(`${label}_required`);
  }

  let url;
  try {
    url = new URL(rawValue);
  } catch {
    throw new Error(`${label}_invalid`);
  }

  if (!HTTP_PROTOCOLS.has(url.protocol)) {
    throw new Error(`${label}_unsupported_scheme`);
  }

  url.username = "";
  url.password = "";
  return url.toString().replace(/\/$/, "");
}

function isSafeHttpUrl(value) {
  try {
    normalizeHttpUrl(value, null, "url");
    return true;
  } catch {
    return false;
  }
}

async function resolveLauncherBaseUrl() {
  const localConfig = await readLocalConfig();
  return localConfig.launcherApiBaseUrl || DEFAULT_LOCAL_CONFIG.launcherApiBaseUrl;
}

async function openClientWindow({ launchUrl, title }) {
  if (!launchUrl) {
    throw new Error("launch_url_required");
  }

  const safeLaunchUrl = normalizeHttpUrl(launchUrl, null, "launch_url");

  const clientWindow = new BrowserWindow({
    width: 1480,
    height: 980,
    minWidth: 1180,
    minHeight: 760,
    autoHideMenuBar: true,
    title: title || "Epsilon Hotel",
    backgroundColor: "#08131e",
    webPreferences: {
      sandbox: true,
      contextIsolation: true,
      nodeIntegration: false,
      webSecurity: true,
      allowRunningInsecureContent: false
    }
  });

  clientWindow.webContents.setWindowOpenHandler(({ url }) => {
    if (isSafeHttpUrl(url)) {
      shell.openExternal(normalizeHttpUrl(url, null, "external_url"));
    }
    return { action: "deny" };
  });

  clientWindow.webContents.on("will-navigate", (event, url) => {
    if (!isSafeHttpUrl(url)) {
      event.preventDefault();
    }
  });

  await clientWindow.loadURL(safeLaunchUrl);
  return { opened: true };
}

function createLauncherWindow() {
  mainWindow = new BrowserWindow({
    width: 1240,
    height: 900,
    minWidth: 1080,
    minHeight: 760,
    autoHideMenuBar: true,
    title: "Epsilon Launcher Desktop",
    backgroundColor: "#08131e",
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      sandbox: true,
      contextIsolation: true,
      nodeIntegration: false,
      webSecurity: true,
      allowRunningInsecureContent: false
    }
  });

  mainWindow.loadFile(path.join(__dirname, "index.html"));
}

ipcMain.handle("launcher:get-runtime-info", async () => ({
  platformKind: detectPlatformKind(),
  electronVersion: process.versions.electron,
  chromeVersion: process.versions.chrome,
  nodeVersion: process.versions.node,
  userDataPath: app.getPath("userData"),
  configPath: getConfigPath()
}));

ipcMain.handle("launcher:get-local-config", async () => readLocalConfig());

ipcMain.handle("launcher:save-local-config", async (_event, partialConfig) => writeLocalConfig(partialConfig || {}));

ipcMain.handle("launcher:get-desktop-config", async () => {
  const launcherBaseUrl = await resolveLauncherBaseUrl();
  return fetchJson(launcherBaseUrl, "/launcher/desktop/config");
});

ipcMain.handle("launcher:get-update-channels", async () => {
  const launcherBaseUrl = await resolveLauncherBaseUrl();
  return fetchJson(launcherBaseUrl, "/launcher/update/channels");
});

ipcMain.handle("launcher:get-launch-profiles", async (_event, input) => {
  const launcherBaseUrl = await resolveLauncherBaseUrl();
  const query = new URLSearchParams();
  if (input && input.platformKind) {
    query.set("platformKind", input.platformKind);
  }
  if (input && input.channel) {
    query.set("channel", input.channel);
  }
  const suffix = query.toString() ? `?${query.toString()}` : "";
  return fetchJson(launcherBaseUrl, `/launcher/launch-profiles${suffix}`);
});

ipcMain.handle("launcher:redeem-code", async (_event, input) => {
  const launcherBaseUrl = await resolveLauncherBaseUrl();
  return fetchJson(launcherBaseUrl, "/launcher/access-codes/redeem", {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      code: input.code,
      deviceLabel: `${os.hostname()} (${detectPlatformKind()})`,
      platformKind: detectPlatformKind()
    })
  });
});

ipcMain.handle("launcher:select-profile", async (_event, input) => {
  const launcherBaseUrl = await resolveLauncherBaseUrl();
  return fetchJson(launcherBaseUrl, "/launcher/launch-profiles/select", {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      ticket: input.ticket,
      profileKey: input.profileKey,
      platformKind: detectPlatformKind()
    })
  });
});

ipcMain.handle("launcher:client-started", async (_event, input) => {
  const launcherBaseUrl = await resolveLauncherBaseUrl();
  return fetchJson(launcherBaseUrl, "/launcher/client-started", {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      ticket: input.ticket,
      profileKey: input.profileKey,
      clientKind: input.clientKind,
      platformKind: detectPlatformKind()
    })
  });
});

ipcMain.handle("launcher:open-client", async (_event, input) => openClientWindow(input));

ipcMain.handle("launcher:open-url", async (_event, targetUrl) => {
  if (!targetUrl) {
    return { opened: false };
  }

  const safeTargetUrl = normalizeHttpUrl(targetUrl, null, "external_url");
  await shell.openExternal(safeTargetUrl);
  return { opened: true };
});

app.whenReady().then(async () => {
  await writeLocalConfig({});
  createLauncherWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createLauncherWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
