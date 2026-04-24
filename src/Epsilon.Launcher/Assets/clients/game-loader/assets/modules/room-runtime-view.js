export class RoomRuntimeView {
  constructor(documentRef = document) {
    this.heroCopy = documentRef.getElementById("hero-copy");
    this.sessionChip = documentRef.getElementById("session-chip");
    this.statusChip = documentRef.getElementById("status-chip");
    this.roomName = documentRef.getElementById("room-name");
    this.roomStatePill = documentRef.getElementById("room-state-pill");
    this.layoutLabel = documentRef.getElementById("layout-label");
    this.actorsLabel = documentRef.getElementById("actors-label");
    this.itemsLabel = documentRef.getElementById("items-label");
    this.presenceCopy = documentRef.getElementById("presence-copy");
    this.actorsGrid = documentRef.getElementById("actors-grid");
    this.usernameLabel = documentRef.getElementById("username-label");
    this.publicIdLabel = documentRef.getElementById("public-id-label");
    this.collectorLabel = documentRef.getElementById("collector-label");
    this.mottoLabel = documentRef.getElementById("motto-label");
    this.chatFeed = documentRef.getElementById("chat-feed");
    this.chatForm = documentRef.getElementById("chat-form");
    this.chatInput = documentRef.getElementById("chat-input");
    this.chatHelp = documentRef.getElementById("chat-help");
  }

  renderState(snapshot) {
    this.heroCopy.textContent = snapshot.copy;
    this.statusChip.textContent = snapshot.label;
    this.statusChip.className = "state-pill " + snapshot.tone;
    this.setChatEnabled(snapshot.canChat);

    if (snapshot.phase === "missing_ticket") {
      this.sessionChip.textContent = "inválida";
      this.renderPendingRoom("Sin ticket válido.");
    }

    if (snapshot.phase === "failed") {
      this.sessionChip.textContent = "error";
      this.renderFailure(snapshot.error || "Fallo desconocido.");
    }
  }

  renderBootstrap(bootstrap) {
    const session = bootstrap.session || null;
    const collector = bootstrap.collector || null;

    this.sessionChip.textContent = session ? "válida" : "pendiente";
    this.usernameLabel.textContent = session ? "Sesión validada" : "Cuenta pendiente";
    this.publicIdLabel.textContent = session ? "Cuenta " + session.accountId : "-";
    this.collectorLabel.textContent = collector
      ? `${collector.collectorTier} / ${collector.ownedCollectibleCount} items`
      : "Sin colección vinculada";
    this.mottoLabel.textContent = "Esperando perfil del avatar.";
  }

  renderProfile(currentSession, fallbackCollector) {
    const profile = currentSession &&
      currentSession.bootstrap &&
      currentSession.bootstrap.character &&
      currentSession.bootstrap.character.profile
      ? currentSession.bootstrap.character.profile
      : null;

    if (!profile) {
      return {
        username: null,
        characterId: null
      };
    }

    const collector = currentSession.collector || fallbackCollector || null;
    this.usernameLabel.textContent = profile.username || "Avatar";
    this.publicIdLabel.textContent = profile.publicId || "-";
    this.collectorLabel.textContent = collector
      ? `${collector.collectorTier} / ${collector.ownedCollectibleCount} items`
      : "Sin colección vinculada";
    this.mottoLabel.textContent = profile.motto || "Sin misión";

    return {
      username: profile.username || null,
      characterId: readIdentityValue(profile.characterId)
    };
  }

  renderPendingRoom(message) {
    this.roomName.textContent = "Entrada pendiente";
    this.roomStatePill.textContent = "pendiente";
    this.roomStatePill.className = "state-pill neutral";
    this.layoutLabel.textContent = "-";
    this.actorsLabel.textContent = "-";
    this.itemsLabel.textContent = "-";
    this.presenceCopy.textContent = message || "El emulador todavía no confirmó presencia dentro del hotel.";
    this.actorsGrid.innerHTML = "<div class=\"empty-copy\">La sala aparece cuando el emulador confirme tu avatar dentro.</div>";
  }

  renderRoomSnapshot(snapshot, sessionContext, presenceConfirmed) {
    const actors = Array.isArray(snapshot.actors) ? snapshot.actors : [];
    const items = Array.isArray(snapshot.room && snapshot.room.items) ? snapshot.room.items : [];
    const room = snapshot.room && snapshot.room.room ? snapshot.room.room : null;
    const layout = snapshot.room && snapshot.room.layout ? snapshot.room.layout : null;
    const self = actors.find((actor) =>
      (sessionContext.characterId !== null && Number(actor.actorId) === Number(sessionContext.characterId)) ||
      (sessionContext.username && actor.displayName === sessionContext.username));
    const hasPresence = Boolean(presenceConfirmed && self);

    if (!hasPresence) {
      this.renderPendingRoom("Entrada solicitada. El Runtime Gateway respondió, pero el avatar aún no está confirmado como presente.");
      this.renderChat([]);
      return { hasPresence: false, snapshot };
    }

    this.roomName.textContent = room && room.name ? room.name : "Sala " + sessionContext.roomId;
    this.roomStatePill.textContent = "confirmada";
    this.roomStatePill.className = "state-pill live";
    this.layoutLabel.textContent = layout && layout.layoutCode ? layout.layoutCode : "sin plano";
    this.actorsLabel.textContent = String(actors.length);
    this.itemsLabel.textContent = String(items.length);
    this.presenceCopy.textContent = "El emulador confirmó tu avatar dentro. Desde aquí el chat ya opera contra el runtime.";

    this.renderActors(actors, sessionContext);
    this.renderChat(snapshot.chatMessages);
    return { hasPresence: true, snapshot };
  }

  renderActors(actors, sessionContext) {
    if (!Array.isArray(actors) || actors.length === 0) {
      this.actorsGrid.innerHTML = "<div class=\"empty-copy\">No hay personas visibles todavía.</div>";
      return;
    }

    this.actorsGrid.innerHTML = actors.map((actor) => {
      const isSelf = sessionContext.characterId !== null && Number(actor.actorId) === Number(sessionContext.characterId);
      const kind = actor.actorKind === 0 ? "Usuario" : actor.actorKind === 2 ? "Bot" : "Actor";
      const position = actor.position ? `${actor.position.x},${actor.position.y}` : "n/a";
      return `
        <article class="actor-card ${isSelf ? "self" : ""}">
          <div class="actor-name">${escapeHtml(actor.displayName || "Actor")}</div>
          <div class="actor-meta">
            <div>${kind}</div>
            <div>Posición ${position}</div>
          </div>
        </article>
      `;
    }).join("");
  }

  renderChat(messages) {
    if (!Array.isArray(messages) || messages.length === 0) {
      this.chatFeed.innerHTML = "<div class=\"empty-copy\">El chat aparece cuando exista actividad real en la sala.</div>";
      return;
    }

    this.chatFeed.innerHTML = messages.slice().reverse().map((entry) => `
      <article class="chat-entry">
        <strong>${escapeHtml(entry.senderName || "Hotel")}</strong>
        <p>${escapeHtml(entry.message || "")}</p>
      </article>
    `).join("");
  }

  renderFailure(errorMessage) {
    this.roomName.textContent = "Fallo de conexión";
    this.roomStatePill.textContent = "error";
    this.roomStatePill.className = "state-pill error";
    this.presenceCopy.textContent = errorMessage;
    this.actorsGrid.innerHTML = "<div class=\"empty-copy\">No se pudo completar la entrada real al hotel.</div>";
    this.chatFeed.innerHTML = "<div class=\"empty-copy\">Chat no disponible.</div>";
    this.setChatEnabled(false);
  }

  setChatEnabled(enabled) {
    if (this.chatInput) {
      this.chatInput.disabled = !enabled;
      this.chatInput.placeholder = enabled
        ? "Escribe un mensaje"
        : "Chat disponible después de entrar al hotel";
    }

    const button = this.chatForm ? this.chatForm.querySelector("button") : null;
    if (button) {
      button.disabled = !enabled;
    }

    if (this.chatHelp) {
      this.chatHelp.textContent = enabled
        ? "Chat conectado al runtime confirmado."
        : "El chat se activa solo cuando el emulador confirma presencia real.";
    }
  }

  bindChat(handler) {
    if (!this.chatForm || !this.chatInput) {
      return;
    }

    this.chatForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      const message = this.chatInput.value.trim();
      if (!message) {
        return;
      }

      await handler(message);
    });
  }

  clearChatInput() {
    if (this.chatInput) {
      this.chatInput.value = "";
    }
  }
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#39;");
}

function readIdentityValue(value) {
  if (typeof value === "number") {
    return value;
  }

  if (value && typeof value.value === "number") {
    return value.value;
  }

  return null;
}
