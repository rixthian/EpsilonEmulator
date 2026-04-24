export const LoaderPhase = Object.freeze({
  MissingTicket: "missing_ticket",
  ValidatingTicket: "validating_ticket",
  BootstrapReady: "bootstrap_ready",
  ClientStarted: "client_started",
  RoomEntryRequested: "room_entry_requested",
  RoomEntryAccepted: "room_entry_accepted",
  PresenceWaiting: "presence_waiting",
  PresenceConfirmed: "presence_confirmed",
  Reconnecting: "reconnecting",
  Failed: "failed"
});

const phaseMeta = Object.freeze({
  [LoaderPhase.MissingTicket]: {
    label: "sin ticket",
    copy: "Falta el ticket entregado por el portal/CMS o la Launcher App.",
    tone: "error",
    progress: 0
  },
  [LoaderPhase.ValidatingTicket]: {
    label: "validando",
    copy: "Validando el ticket de sesión contra el backend del launcher.",
    tone: "neutral",
    progress: 12
  },
  [LoaderPhase.BootstrapReady]: {
    label: "sesión válida",
    copy: "La sesión fue validada. Preparando el arranque del loader.",
    tone: "neutral",
    progress: 28
  },
  [LoaderPhase.ClientStarted]: {
    label: "loader abierto",
    copy: "El loader está ejecutándose y fue registrado por el Launcher Backend.",
    tone: "neutral",
    progress: 44
  },
  [LoaderPhase.RoomEntryRequested]: {
    label: "solicitando entrada",
    copy: "Solicitando entrada al hotel. Aún no se marca presencia real.",
    tone: "neutral",
    progress: 60
  },
  [LoaderPhase.RoomEntryAccepted]: {
    label: "entrada aceptada",
    copy: "El Runtime Gateway aceptó la entrada. Falta confirmación del emulador.",
    tone: "neutral",
    progress: 74
  },
  [LoaderPhase.PresenceWaiting]: {
    label: "esperando presencia",
    copy: "El loader está esperando que el emulador confirme el avatar dentro del hotel.",
    tone: "warning",
    progress: 84
  },
  [LoaderPhase.PresenceConfirmed]: {
    label: "dentro del hotel",
    copy: "El emulador confirmó la presencia real del avatar.",
    tone: "live",
    progress: 100
  },
  [LoaderPhase.Reconnecting]: {
    label: "reconectando",
    copy: "La sincronización falló temporalmente. Reintentando sin asumir entrada.",
    tone: "warning",
    progress: 84
  },
  [LoaderPhase.Failed]: {
    label: "error",
    copy: "El loader no pudo completar el flujo de entrada.",
    tone: "error",
    progress: 100
  }
});

export class LoaderStateMachine {
  constructor(onChange) {
    this.onChange = onChange;
    this.phase = LoaderPhase.ValidatingTicket;
    this.detail = null;
    this.error = null;
  }

  transition(phase, detail = null) {
    this.phase = phase;
    this.detail = detail;
    this.error = null;
    this.emit();
  }

  fail(error, detail = null) {
    this.phase = LoaderPhase.Failed;
    this.detail = detail;
    this.error = normalizeError(error);
    this.emit();
  }

  get snapshot() {
    const meta = phaseMeta[this.phase] || phaseMeta[LoaderPhase.Failed];
    return {
      phase: this.phase,
      label: meta.label,
      copy: meta.copy,
      tone: meta.tone,
      progress: meta.progress,
      detail: this.detail,
      error: this.error,
      canChat: this.phase === LoaderPhase.PresenceConfirmed,
      hasRealPresence: this.phase === LoaderPhase.PresenceConfirmed
    };
  }

  emit() {
    if (typeof this.onChange === "function") {
      this.onChange(this.snapshot);
    }
  }
}

function normalizeError(error) {
  if (!error) {
    return "unknown_error";
  }

  if (error.payload && error.payload.error) {
    return String(error.payload.error);
  }

  return error.message || String(error);
}
