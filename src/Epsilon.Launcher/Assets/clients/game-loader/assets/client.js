import { LauncherApiClient } from "./modules/api-client.js";
import { LoaderPhase, LoaderStateMachine } from "./modules/loader-state-machine.js";
import { PopupManager } from "./modules/popup-manager.js";
import { ProgressTracker } from "./modules/progress-tracker.js";
import { RoomRuntimeView } from "./modules/room-runtime-view.js";

const params = new URLSearchParams(window.location.search);
const ticket = params.get("ticket") || "";
let runtimeRoomId = normalizeRoomId(params.get("roomId"));
let pollHandle = null;

const view = new RoomRuntimeView(document);
const progress = new ProgressTracker(document);
const popup = new PopupManager(document.body);
const api = new LauncherApiClient({
  ticket,
  profileKey: "loader-desktop",
  clientKind: "loader"
});

const sessionContext = {
  username: null,
  characterId: null,
  roomId: runtimeRoomId
};

const state = new LoaderStateMachine((snapshot) => {
  view.renderState(snapshot);
  progress.render(snapshot);
});

view.bindChat(sendChat);
boot();

window.addEventListener("beforeunload", () => {
  if (pollHandle) {
    window.clearInterval(pollHandle);
  }
});

async function boot() {
  if (!ticket) {
    state.transition(LoaderPhase.MissingTicket);
    popup.show({
      title: "Ticket requerido",
      message: "Abre el loader desde el portal/CMS o desde la Launcher App. Sin ticket no existe sesión válida.",
      tone: "error"
    });
    return;
  }

  try {
    state.transition(LoaderPhase.ValidatingTicket);
    view.renderPendingRoom("Validando el ticket con el Launcher Backend antes de solicitar entrada al hotel.");
    await api.sendTelemetry("client_boot_started", "Loader iniciado. Todavía no hay presencia real.", {
      roomId: String(runtimeRoomId)
    });

    const bootstrap = await api.getBootstrap();
    validateLaunchEntitlement(bootstrap);
    view.renderBootstrap(bootstrap);
    applyBootstrapContext(bootstrap);
    state.transition(LoaderPhase.BootstrapReady);

    await api.recordClientStarted();
    await api.sendTelemetry("client_runtime_registered", "El loader fue registrado por el Launcher Backend como proceso iniciado.", {
      roomId: String(runtimeRoomId)
    });
    state.transition(LoaderPhase.ClientStarted);

    const currentSession = await api.getCurrentSession().catch(() => null);
    const profileContext = view.renderProfile(currentSession, bootstrap.collector);
    sessionContext.username = profileContext.username || sessionContext.username;
    sessionContext.characterId = Number(profileContext.characterId || sessionContext.characterId || 0) || null;

    const alreadyPresent = await synchronizeRuntime({ allowMissingRoom: true });
    if (alreadyPresent) {
      state.transition(LoaderPhase.PresenceConfirmed, { roomId: runtimeRoomId });
      startRuntimePolling();
      return;
    }

    state.transition(LoaderPhase.RoomEntryRequested, { roomId: runtimeRoomId });
    await api.sendTelemetry("room_entry_requested", "Solicitud de entrada enviada al emulador.", {
      roomId: String(runtimeRoomId)
    });

    await ensureRoomEntry();
    state.transition(LoaderPhase.RoomEntryAccepted, { roomId: runtimeRoomId });
    await api.sendTelemetry("room_entry_accepted", "El Runtime Gateway aceptó la entrada. Falta confirmar presencia.", {
      roomId: String(runtimeRoomId)
    });

    const confirmed = await waitForPresence();
    if (confirmed) {
      state.transition(LoaderPhase.PresenceConfirmed, { roomId: runtimeRoomId });
      await api.sendTelemetry("room_presence_confirmed", "El emulador confirmó presencia real del avatar.", {
        roomId: String(runtimeRoomId)
      });
    } else {
      state.transition(LoaderPhase.PresenceWaiting, { roomId: runtimeRoomId });
      await api.sendTelemetry("room_presence_pending", "La entrada fue aceptada, pero aún no hay presencia real.", {
        roomId: String(runtimeRoomId)
      });
      popup.show({
        title: "Entrada pendiente",
        message: "El Runtime Gateway respondió, pero el emulador aún no confirmó el avatar dentro del hotel. El loader seguirá reintentando.",
        tone: "warning"
      });
    }

    startRuntimePolling();
  } catch (error) {
    state.fail(error, { roomId: runtimeRoomId });
    await api.sendTelemetry("client_boot_failed", stringifyError(error), {
      roomId: String(runtimeRoomId)
    });
    popup.show({
      title: "No se pudo iniciar el hotel",
      message: stringifyError(error),
      tone: "error"
    });
  }
}

function validateLaunchEntitlement(bootstrap) {
  if (bootstrap && bootstrap.launchEntitlement && bootstrap.launchEntitlement.canLaunch === false) {
    const missing = Array.isArray(bootstrap.launchEntitlement.missingRequirementKeys)
      ? bootstrap.launchEntitlement.missingRequirementKeys.join(", ")
      : "requisitos pendientes";
    throw new Error("Acceso bloqueado por entitlement: " + missing);
  }
}

function applyBootstrapContext(bootstrap) {
  if (bootstrap && bootstrap.session && bootstrap.session.characterId) {
    sessionContext.characterId = Number(bootstrap.session.characterId) || null;
  }
}

async function ensureRoomEntry() {
  const connection = await api.getConnection().catch(() => null);
  if (connection && connection.currentRoomId) {
    runtimeRoomId = Number(connection.currentRoomId);
    sessionContext.roomId = runtimeRoomId;
    return;
  }

  await api.enterRoom(runtimeRoomId);
}

async function waitForPresence() {
  for (let attempt = 0; attempt < 10; attempt += 1) {
    state.transition(LoaderPhase.PresenceWaiting, {
      roomId: runtimeRoomId,
      attempt: attempt + 1
    });

    const confirmed = await synchronizeRuntime({ allowMissingRoom: false }).catch(() => false);
    if (confirmed) {
      return true;
    }

    await delay(650);
  }

  return false;
}

async function synchronizeRuntime(options = {}) {
  const connectionState = await api.getConnectionState();
  if (connectionState.currentRoomId) {
    runtimeRoomId = Number(connectionState.currentRoomId);
    sessionContext.roomId = runtimeRoomId;
  } else if (options.allowMissingRoom) {
    view.renderPendingRoom("Sesión válida. Todavía no existe sala activa en el emulador.");
    return false;
  }

  const snapshot = await api.getRoomSnapshot(runtimeRoomId);
  const presenceConfirmed = Boolean(
    connectionState.presenceConfirmed &&
    Number(connectionState.currentRoomId) === Number(runtimeRoomId)
  );
  const renderResult = view.renderRoomSnapshot(snapshot, sessionContext, presenceConfirmed);
  return renderResult.hasPresence;
}

function startRuntimePolling() {
  if (pollHandle) {
    window.clearInterval(pollHandle);
  }

  pollHandle = window.setInterval(async () => {
    try {
      const confirmed = await synchronizeRuntime({ allowMissingRoom: false });
      if (confirmed && !state.snapshot.hasRealPresence) {
        state.transition(LoaderPhase.PresenceConfirmed, { roomId: runtimeRoomId });
        await api.sendTelemetry("room_presence_confirmed", "Presencia confirmada durante sincronización continua.", {
          roomId: String(runtimeRoomId)
        });
      }

      if (!confirmed && state.snapshot.hasRealPresence) {
        state.transition(LoaderPhase.Reconnecting, { roomId: runtimeRoomId });
      }
    } catch (error) {
      if (state.snapshot.hasRealPresence) {
        state.transition(LoaderPhase.Reconnecting, {
          roomId: runtimeRoomId,
          error: stringifyError(error)
        });
      }
    }
  }, 4000);
}

async function sendChat(message) {
  if (!state.snapshot.canChat) {
    popup.show({
      title: "Chat bloqueado",
      message: "El chat se activa solo después de que el emulador confirme presencia real dentro del hotel.",
      tone: "warning"
    });
    return;
  }

  try {
    await api.sendRoomChat(runtimeRoomId, message);
    view.clearChatInput();
    await api.sendTelemetry("room_chat_sent", "Mensaje enviado desde el loader confirmado.", {
      roomId: String(runtimeRoomId),
      messageLength: String(message.length)
    });
    await synchronizeRuntime({ allowMissingRoom: false });
  } catch (error) {
    popup.show({
      title: "No se pudo enviar",
      message: stringifyError(error),
      tone: "error"
    });
  }
}

function normalizeRoomId(rawValue) {
  const parsed = Number(rawValue || "10");
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 10;
}

function delay(milliseconds) {
  return new Promise((resolve) => window.setTimeout(resolve, milliseconds));
}

function stringifyError(error) {
  if (!error) {
    return "unknown_error";
  }

  if (error.payload && error.payload.error) {
    return String(error.payload.error);
  }

  return error.message || String(error);
}
