const registerForm = document.querySelector("#register-form");
const loginForm = document.querySelector("#login-form");
const roomEntryForm = document.querySelector("#room-entry-form");
const showRegisterButton = document.querySelector("#show-register");
const showLoginButton = document.querySelector("#show-login");
const registerPanel = document.querySelector("#register-panel");
const loginPanel = document.querySelector("#login-panel");
const backButtons = document.querySelectorAll("[data-back-to-welcome]");
const refreshHealthButton = document.querySelector("#refresh-health");
const refreshStateButton = document.querySelector("#refresh-state");
const clearSessionButton = document.querySelector("#clear-session");
const moveButton = document.querySelector("#move-button");
const chatButton = document.querySelector("#chat-button");
const gatewayStatus = document.querySelector("#gateway-status");
const launcherStatus = document.querySelector("#launcher-status");
const sessionPanel = document.querySelector("#session-panel");
const hotelPanel = document.querySelector("#hotel-panel");
const characterLabel = document.querySelector("#character-label");
const publicIdLabel = document.querySelector("#public-id");
const ticketLabel = document.querySelector("#ticket-label");
const launchStatus = document.querySelector("#launch-status");
const roomStatus = document.querySelector("#room-status");
const bootstrapOutput = document.querySelector("#bootstrap-output");
const registerUsernameInput = registerForm?.querySelector('input[name="username"]');
const loginNameInput = loginForm?.querySelector('input[name="loginName"]');
const loginPasswordInput = loginForm?.querySelector('input[name="password"]');
const roomIdInput = roomEntryForm?.querySelector('input[name="roomId"]');

const sessionStorageKey = "epsilon.portal.ticket";

function appendLog(label, payload) {
  console.info("[epsilon-access]", label, payload);
}

function showWelcome() {
  registerPanel?.classList.add("hidden");
  loginPanel?.classList.add("hidden");
}

function showRegister() {
  registerPanel?.classList.remove("hidden");
  loginPanel?.classList.add("hidden");
  registerUsernameInput?.focus();
}

function showLogin() {
  loginPanel?.classList.remove("hidden");
  registerPanel?.classList.add("hidden");
  loginNameInput?.focus();
}

function setAuthenticatedUi(isAuthenticated) {
  sessionPanel?.classList.toggle("hidden", !isAuthenticated);
  hotelPanel?.classList.toggle("hidden", !isAuthenticated);
}

function setTicket(ticket) {
  if (ticket) {
    window.localStorage.setItem(sessionStorageKey, ticket);
  } else {
    window.localStorage.removeItem(sessionStorageKey);
  }
}

function getTicket() {
  return window.localStorage.getItem(sessionStorageKey) ?? "";
}

async function request(path, init) {
  const response = await fetch(path, init);
  const payload = await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(JSON.stringify(payload ?? { error: response.statusText }));
  }

  return payload;
}

async function performLogin(loginName, password) {
  const result = await request("/api/epsilon/login", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ loginName, password })
  });
  const ticket = result.session?.ticket;
  setTicket(ticket);
  appendLog("login_ok", result);
  await prepareCollectorAccess();
  await refreshState();
  return result;
}

async function refreshHealth() {
  try {
    const payload = await request("/api/epsilon/health");
    gatewayStatus.textContent = payload.gateway?.status ?? "down";
    launcherStatus.textContent = payload.launcher?.status ?? "down";
  } catch (error) {
    gatewayStatus.textContent = "down";
    launcherStatus.textContent = "down";
    appendLog("health_failed", String(error));
  }
}

async function refreshGenerations() {
  return null;
}

async function refreshState() {
  const ticket = getTicket();
  if (!ticket) {
    setAuthenticatedUi(false);
    characterLabel.textContent = "none";
    publicIdLabel.textContent = "-";
    ticketLabel.textContent = "none";
    launchStatus.textContent = "unknown";
    roomStatus.textContent = "none";
    bootstrapOutput.textContent = "No session yet.";
    return;
  }

  try {
    const [bootstrap, connection] = await Promise.all([
      request(`/api/epsilon/bootstrap?ticket=${encodeURIComponent(ticket)}`),
      request(`/api/epsilon/connection?ticket=${encodeURIComponent(ticket)}`)
    ]);

    setAuthenticatedUi(true);
    const session = bootstrap.session;
    characterLabel.textContent = session ? `${session.characterId} / account ${session.accountId}` : "unknown";
    publicIdLabel.textContent = bootstrap.collector?.characterId?.value ? `usr_${Number(bootstrap.collector.characterId.value).toString(16).padStart(8, "0")}` : "-";
    ticketLabel.textContent = ticket;
    launchStatus.textContent = bootstrap.launchEntitlement?.canLaunch ? "yes" : "no";
    roomStatus.textContent = connection.currentRoomId ?? "none";
    bootstrapOutput.textContent = JSON.stringify(bootstrap, null, 2);
  } catch (error) {
    setTicket("");
    setAuthenticatedUi(false);
    characterLabel.textContent = "none";
    publicIdLabel.textContent = "-";
    ticketLabel.textContent = "none";
    launchStatus.textContent = "unknown";
    roomStatus.textContent = "none";
    bootstrapOutput.textContent = "No session yet.";
    appendLog("refresh_state_failed", String(error));
  }
}

async function prepareCollectorAccess() {
  const ticket = getTicket();
  if (!ticket) {
    return;
  }

  const payload = await request("/api/epsilon/prepare-collector", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ ticket })
  });
  appendLog("collector_prepared", payload);
}

registerForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const formData = new FormData(registerForm);
  const payload = {
    username: formData.get("username"),
    email: formData.get("email"),
    password: formData.get("password")
  };

  try {
    const result = await request("/api/epsilon/register-and-login", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(payload)
    });
    appendLog("register_ok", result.register ?? result);
    if (loginNameInput) {
      loginNameInput.value = String(payload.username);
    }
    if (loginPasswordInput) {
      loginPasswordInput.value = String(payload.password);
    }
    const sessionTicket = result.login?.session?.ticket;
    setTicket(sessionTicket);
    appendLog("login_ok", result.login ?? result);
    await prepareCollectorAccess();
    await refreshState();
    showWelcome();
  } catch (error) {
    appendLog("register_failed", String(error));
  }
});

loginForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const formData = new FormData(loginForm);
  const payload = {
    loginName: formData.get("loginName"),
    password: formData.get("password")
  };

  try {
    await performLogin(String(payload.loginName), String(payload.password));
    showWelcome();
  } catch (error) {
    appendLog("login_failed", String(error));
  }
});

roomEntryForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const roomId = Number(roomIdInput?.value ?? "10");

  try {
    const result = await request("/api/epsilon/rooms/entry", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ ticket: getTicket(), roomId, spectatorMode: false })
    });
    appendLog("room_entry_ok", result);
    await refreshState();
  } catch (error) {
    appendLog("room_entry_failed", String(error));
  }
});

moveButton.addEventListener("click", async () => {
  try {
    const result = await request("/api/epsilon/rooms/move", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ ticket: getTicket(), roomId: 10, destinationX: 14, destinationY: 7 })
    });
    appendLog("move_ok", result);
    await refreshState();
  } catch (error) {
    appendLog("move_failed", String(error));
  }
});

chatButton.addEventListener("click", async () => {
  try {
    const result = await request("/api/epsilon/rooms/chat", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ ticket: getTicket(), roomId: 10, message: "hola, primera prueba operativa" })
    });
    appendLog("chat_ok", result);
  } catch (error) {
    appendLog("chat_failed", String(error));
  }
});

refreshHealthButton.addEventListener("click", () => {
  refreshHealth().catch((error) => appendLog("health_failed", String(error)));
});

refreshStateButton.addEventListener("click", () => {
  refreshState().catch((error) => appendLog("refresh_state_failed", String(error)));
});

clearSessionButton.addEventListener("click", () => {
  setTicket("");
  showWelcome();
  setAuthenticatedUi(false);
  appendLog("session_cleared", "Local session removed.");
  refreshState().catch((error) => appendLog("refresh_state_failed", String(error)));
});

showRegisterButton?.addEventListener("click", () => {
  showRegister();
});

showLoginButton?.addEventListener("click", () => {
  showLogin();
});

backButtons.forEach((button) => {
  button.addEventListener("click", () => {
    showWelcome();
  });
});

refreshHealth().catch((error) => appendLog("health_failed", String(error)));
refreshState().catch((error) => appendLog("refresh_state_failed", String(error)));
showWelcome();
setAuthenticatedUi(false);
