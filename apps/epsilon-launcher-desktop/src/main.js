const { app, BrowserWindow, ipcMain, shell } = require("electron");
const fs = require("node:fs/promises");
const os = require("node:os");
const path = require("node:path");

const DEFAULT_LOCAL_CONFIG = {
  launcherId: "desktop-main",
  locale: "es-ES",
  hotelBaseUrl: "http://127.0.0.1:4100",
  launcherApiBaseUrl: "http://127.0.0.1:5001",
  defaultChannel: "stable",
  defaultProfileKey: "web-alpha",
  autoLaunchOnRedeem: true,
  closeCmsOnLaunch: false,
  hardwareAcceleration: true,
  telemetryEnabled: true,
  rememberLastProfile: true
};

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
    return {
      ...DEFAULT_LOCAL_CONFIG,
      ...parsed,
      platform: detectPlatformKind()
    };
  } catch {
    return {
      ...DEFAULT_LOCAL_CONFIG,
      platform: detectPlatformKind()
    };
  }
}

async function writeLocalConfig(partialConfig) {
  const configPath = getConfigPath();
  const current = await readLocalConfig();
  const next = {
    ...current,
    ...partialConfig,
    platform: detectPlatformKind()
  };
  await fs.mkdir(path.dirname(configPath), { recursive: true });
  await fs.writeFile(configPath, JSON.stringify(next, null, 2), "utf8");
  return next;
}

async function fetchJson(baseUrl, pathname, options = {}) {
  const targetUrl = new URL(pathname, ensureTrailingSlash(baseUrl)).toString();
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

async function resolveLauncherBaseUrl() {
  const localConfig = await readLocalConfig();
  return localConfig.launcherApiBaseUrl || DEFAULT_LOCAL_CONFIG.launcherApiBaseUrl;
}

async function openClientWindow({ launchUrl, title }) {
  if (!launchUrl) {
    throw new Error("launch_url_required");
  }

  const clientWindow = new BrowserWindow({
    width: 1480,
    height: 980,
    minWidth: 1180,
    minHeight: 760,
    autoHideMenuBar: true,
    title: title || "Epsilon Hotel",
    backgroundColor: "#08131e"
  });

  await clientWindow.loadURL(launchUrl);
  return { opened: true };
}

function createMainWindow() {
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
      contextIsolation: true,
      nodeIntegration: false
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

  await shell.openExternal(targetUrl);
  return { opened: true };
});

app.whenReady().then(async () => {
  await writeLocalConfig({});
  createMainWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createMainWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});

