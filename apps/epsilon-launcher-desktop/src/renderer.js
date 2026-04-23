const state = {
  runtimeInfo: null,
  localConfig: null,
  desktopConfig: null,
  channels: [],
  profiles: [],
  redeemed: null,
  selectedProfileKey: null,
  selectedProfileResult: null
};

const elements = {};

document.addEventListener("DOMContentLoaded", async () => {
  bindElements();
  bindEvents();
  await bootstrap();
});

function bindElements() {
  elements.redeemBanner = document.getElementById("redeem-banner");
  elements.redeemCode = document.getElementById("redeem-code");
  elements.redeemButton = document.getElementById("redeem-button");
  elements.profileSelect = document.getElementById("profile-select");
  elements.profileSummary = document.getElementById("profile-summary");
  elements.launchButton = document.getElementById("launch-button");
  elements.metricPlatform = document.getElementById("metric-platform");
  elements.metricChannel = document.getElementById("metric-channel");
  elements.metricDefaultProfile = document.getElementById("metric-default-profile");
  elements.metricRedeemed = document.getElementById("metric-redeemed");
  elements.hotelUrl = document.getElementById("hotel-url");
  elements.launcherUrl = document.getElementById("launcher-url");
  elements.toggleAutoLaunch = document.getElementById("toggle-auto-launch");
  elements.toggleRememberProfile = document.getElementById("toggle-remember-profile");
  elements.saveConfigButton = document.getElementById("save-config-button");
  elements.channelsList = document.getElementById("channels-list");
  elements.activityLog = document.getElementById("activity-log");
  elements.refreshButton = document.getElementById("refresh-button");
  elements.openCmsButton = document.getElementById("open-cms-button");
}

function bindEvents() {
  elements.refreshButton.addEventListener("click", () => bootstrap());
  elements.redeemButton.addEventListener("click", () => redeemCode());
  elements.profileSelect.addEventListener("change", () => selectProfile(elements.profileSelect.value));
  elements.launchButton.addEventListener("click", () => launchClient());
  elements.saveConfigButton.addEventListener("click", () => saveLocalConfig());
  elements.openCmsButton.addEventListener("click", async () => {
    if (!state.localConfig) {
      return;
    }

    await window.epsilonLauncher.openUrl(`${state.localConfig.hotelBaseUrl}/sites/epsilon-access/`);
  });
}

async function bootstrap() {
  try {
    addLog("Cargando contrato del launcher desktop.");
    state.runtimeInfo = await window.epsilonLauncher.getRuntimeInfo();
    state.localConfig = await window.epsilonLauncher.getLocalConfig();
    state.desktopConfig = await window.epsilonLauncher.getDesktopConfig();
    const channelResponse = await window.epsilonLauncher.getUpdateChannels();
    state.channels = channelResponse.channels || [];
    state.profiles = [];
    state.redeemed = null;
    state.selectedProfileKey = null;
    state.selectedProfileResult = null;
    await refreshProfiles();
    hydrateConfigForm();
    renderChannels();
    renderMetrics();
    renderProfileSummary();
    setRedeemBanner("", "");
    addLog("Contrato desktop cargado.");
  } catch (error) {
    setRedeemBanner(`No se pudo cargar el launcher desktop: ${normalizeError(error)}`, "error");
    addLog(`Error cargando launcher: ${normalizeError(error)}`);
  }
}

async function refreshProfiles(channel) {
  const selectedChannel = channel || state.desktopConfig?.defaultChannel || state.localConfig?.defaultChannel || "stable";
  const response = await window.epsilonLauncher.getLaunchProfiles({
    platformKind: state.runtimeInfo?.platformKind,
    channel: selectedChannel
  });
  state.profiles = response.profiles || [];
  if (!state.selectedProfileKey) {
    state.selectedProfileKey =
      state.localConfig?.lastProfileKey ||
      response.defaultProfileKey ||
      state.desktopConfig?.defaultProfileKey ||
      state.profiles[0]?.profileKey ||
      null;
  }
  renderProfiles();
  if (state.selectedProfileKey) {
    await selectProfile(state.selectedProfileKey, false);
  }
}

function hydrateConfigForm() {
  elements.hotelUrl.value = state.localConfig?.hotelBaseUrl || "";
  elements.launcherUrl.value = state.localConfig?.launcherApiBaseUrl || "";
  elements.toggleAutoLaunch.checked = Boolean(state.localConfig?.autoLaunchOnRedeem);
  elements.toggleRememberProfile.checked = Boolean(state.localConfig?.rememberLastProfile);
}

async function saveLocalConfig() {
  try {
    state.localConfig = await window.epsilonLauncher.saveLocalConfig({
      hotelBaseUrl: elements.hotelUrl.value.trim(),
      launcherApiBaseUrl: elements.launcherUrl.value.trim(),
      autoLaunchOnRedeem: elements.toggleAutoLaunch.checked,
      rememberLastProfile: elements.toggleRememberProfile.checked,
      lastProfileKey: state.selectedProfileKey || state.localConfig?.lastProfileKey || null
    });
    setRedeemBanner("Config local guardada.", "success");
    renderMetrics();
    addLog("Config local guardada.");
  } catch (error) {
    setRedeemBanner(`No se pudo guardar la config local: ${normalizeError(error)}`, "error");
  }
}

function renderChannels() {
  elements.channelsList.innerHTML = "";
  for (const channel of state.channels) {
    const card = document.createElement("article");
    card.className = "channel-card";
    card.innerHTML = `
      <span>${escapeHtml(channel.channelKey || "channel")}</span>
      <strong>${escapeHtml(channel.displayName || channel.channelKey || "Unknown")}</strong>
    `;
    elements.channelsList.appendChild(card);
  }
}

function renderMetrics() {
  elements.metricPlatform.textContent = state.runtimeInfo?.platformKind || "-";
  elements.metricChannel.textContent = state.desktopConfig?.defaultChannel || state.localConfig?.defaultChannel || "-";
  elements.metricDefaultProfile.textContent = state.desktopConfig?.defaultProfileKey || state.localConfig?.defaultProfileKey || "-";
  elements.metricRedeemed.textContent = state.redeemed ? "sí" : "no";
}

function renderProfiles() {
  elements.profileSelect.innerHTML = "";
  for (const profile of state.profiles) {
    const option = document.createElement("option");
    option.value = profile.profileKey;
    option.textContent = `${profile.displayName} (${profile.clientKind})`;
    option.selected = profile.profileKey === state.selectedProfileKey;
    elements.profileSelect.appendChild(option);
  }
}

async function selectProfile(profileKey, announce = true) {
  state.selectedProfileKey = profileKey;
  state.selectedProfileResult = null;
  elements.launchButton.disabled = true;

  if (!profileKey) {
    renderProfileSummary();
    return;
  }

  if (!state.redeemed) {
    renderProfileSummary("Selecciona un código y canjéalo primero.");
    return;
  }

  try {
    state.selectedProfileResult = await window.epsilonLauncher.selectProfile({
      ticket: state.redeemed.ticket,
      profileKey
    });
    if (state.localConfig?.rememberLastProfile) {
      state.localConfig = await window.epsilonLauncher.saveLocalConfig({ lastProfileKey: profileKey });
    }
    renderProfileSummary();
    elements.launchButton.disabled = !state.selectedProfileResult.canStartNow;
    if (announce) {
      addLog(`Perfil seleccionado: ${profileKey}.`);
    }
  } catch (error) {
    renderProfileSummary(`No se pudo seleccionar el perfil: ${normalizeError(error)}`);
    addLog(`Error seleccionando perfil: ${normalizeError(error)}`);
  }
}

function renderProfileSummary(forcedMessage) {
  if (forcedMessage) {
    elements.profileSummary.textContent = forcedMessage;
    return;
  }

  if (!state.selectedProfileResult) {
    elements.profileSummary.textContent = "Selecciona un perfil después de canjear un código.";
    return;
  }

  const result = state.selectedProfileResult;
  const profile = result.profile || {};
  const lines = [
    `Perfil: ${profile.displayName || profile.profileKey || "-"}`,
    `Cliente: ${profile.clientKind || "-"}`,
    `Estrategia: ${result.launchStrategy || "-"}`,
    `Arranque inmediato: ${result.canStartNow ? "sí" : "no"}`
  ];

  if (result.blockingReason) {
    lines.push(`Bloqueo: ${result.blockingReason}`);
  }

  elements.profileSummary.textContent = lines.join("\n");
}

async function redeemCode() {
  const code = elements.redeemCode.value.trim();
  if (!code) {
    setRedeemBanner("Falta el código del launcher.", "error");
    return;
  }

  elements.redeemButton.disabled = true;
  try {
    state.redeemed = await window.epsilonLauncher.redeemCode({ code });
    renderMetrics();
    setRedeemBanner(`Código canjeado. Ticket recibido para ${state.redeemed.profile?.displayName || "launcher"}.`, "success");
    addLog(`Código canjeado: ${code}.`);
    await refreshProfiles();
    if (state.localConfig?.autoLaunchOnRedeem && state.selectedProfileResult?.canStartNow) {
      await launchClient();
    }
  } catch (error) {
    state.redeemed = null;
    renderMetrics();
    setRedeemBanner(`No se pudo canjear el código: ${normalizeError(error)}`, "error");
    addLog(`Error canjeando código: ${normalizeError(error)}`);
  } finally {
    elements.redeemButton.disabled = false;
  }
}

async function launchClient() {
  if (!state.redeemed || !state.selectedProfileResult?.canStartNow) {
    return;
  }

  const result = state.selectedProfileResult;
  const profile = result.profile || {};

  try {
    await window.epsilonLauncher.clientStarted({
      ticket: state.redeemed.ticket,
      profileKey: profile.profileKey,
      clientKind: profile.clientKind
    });

    await window.epsilonLauncher.openClient({
      launchUrl: result.launchUrl,
      title: profile.displayName || "Epsilon Hotel"
    });

    addLog(`Cliente abierto: ${profile.displayName || profile.profileKey}.`);
    setRedeemBanner("Cliente abierto. La presencia real depende ahora del emulador.", "success");
  } catch (error) {
    setRedeemBanner(`No se pudo abrir el cliente: ${normalizeError(error)}`, "error");
    addLog(`Error abriendo cliente: ${normalizeError(error)}`);
  }
}

function setRedeemBanner(message, mode) {
  if (!message) {
    elements.redeemBanner.className = "notice hidden";
    elements.redeemBanner.textContent = "";
    return;
  }

  elements.redeemBanner.className = `notice ${mode}`;
  elements.redeemBanner.textContent = message;
}

function addLog(message) {
  const entry = document.createElement("article");
  entry.className = "log-entry";
  const now = new Date();
  entry.innerHTML = `<time>${escapeHtml(now.toLocaleTimeString())}</time><div>${escapeHtml(message)}</div>`;
  elements.activityLog.prepend(entry);
}

function normalizeError(error) {
  if (!error) {
    return "error";
  }

  if (typeof error === "string") {
    return error;
  }

  if (error.message) {
    return error.message;
  }

  return "error";
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#39;");
}
