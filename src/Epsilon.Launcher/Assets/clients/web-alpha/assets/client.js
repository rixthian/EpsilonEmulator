(function () {
  const params = new URLSearchParams(window.location.search);
  const ticket = params.get("ticket") || "";
  const preferredRoomId = Number(params.get("roomId") || "10");

  const heroCopy = document.getElementById("hero-copy");
  const sessionChip = document.getElementById("session-chip");
  const statusChip = document.getElementById("status-chip");
  const roomName = document.getElementById("room-name");
  const roomStatePill = document.getElementById("room-state-pill");
  const layoutLabel = document.getElementById("layout-label");
  const actorsLabel = document.getElementById("actors-label");
  const itemsLabel = document.getElementById("items-label");
  const presenceCopy = document.getElementById("presence-copy");
  const actorsGrid = document.getElementById("actors-grid");
  const usernameLabel = document.getElementById("username-label");
  const publicIdLabel = document.getElementById("public-id-label");
  const collectorLabel = document.getElementById("collector-label");
  const mottoLabel = document.getElementById("motto-label");
  const chatFeed = document.getElementById("chat-feed");
  const chatForm = document.getElementById("chat-form");
  const chatInput = document.getElementById("chat-input");

  let runtimeRoomId = preferredRoomId;
  let pollHandle = null;
  let sessionUsername = null;
  let sessionCharacterId = null;
  let entryConfirmed = false;

  function setStatus(text, mode) {
    statusChip.textContent = text;
    statusChip.className = mode || "";
  }

  async function request(path, init) {
    const response = await fetch(path, init);
    const text = await response.text();
    let payload = null;

    if (text) {
      try {
        payload = JSON.parse(text);
      } catch {
        payload = { raw: text };
      }
    }

    if (!response.ok) {
      throw new Error(JSON.stringify(payload || { error: response.statusText }));
    }

    return payload;
  }

  async function sendTelemetry(eventKey, detail, data) {
    if (!ticket) {
      return;
    }

    try {
      await fetch("/launcher/telemetry", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({
          ticket,
          eventKey,
          detail: detail || null,
          data: data || {}
        })
      });
    } catch {
    }
  }

  function renderActors(actors) {
    if (!Array.isArray(actors) || actors.length === 0) {
      actorsGrid.innerHTML = "<div class=\"empty-copy\">No hay actores visibles todavía.</div>";
      return;
    }

    actorsGrid.innerHTML = actors.map((actor) => {
      const isSelf = sessionCharacterId !== null && Number(actor.actorId) === Number(sessionCharacterId);
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

  function renderChat(messages) {
    if (!Array.isArray(messages) || messages.length === 0) {
      chatFeed.innerHTML = "<div class=\"empty-copy\">Todavía no hay mensajes en la sala.</div>";
      return;
    }

    chatFeed.innerHTML = messages.slice().reverse().map((entry) => `
      <article class="chat-entry">
        <strong>${escapeHtml(entry.senderName || "Habbo")}</strong>
        <p>${escapeHtml(entry.message || "")}</p>
      </article>
    `).join("");
  }

  function escapeHtml(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll("\"", "&quot;")
      .replaceAll("'", "&#39;");
  }

  async function ensureRoomEntry() {
    const connection = await request("/launcher/connection?ticket=" + encodeURIComponent(ticket));
    if (connection.currentRoomId) {
      runtimeRoomId = Number(connection.currentRoomId);
      return;
    }

    await request("/launcher/runtime/room-entry", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({
        ticket: ticket,
        roomId: runtimeRoomId,
        password: null,
        spectatorMode: false
      })
    });
  }

  async function refreshRuntime() {
    const snapshot = await request("/launcher/runtime/room/" + encodeURIComponent(String(runtimeRoomId)) + "?ticket=" + encodeURIComponent(ticket));
    const actors = Array.isArray(snapshot.actors) ? snapshot.actors : [];
    const items = Array.isArray(snapshot.room && snapshot.room.items) ? snapshot.room.items : [];
    const room = snapshot.room && snapshot.room.room ? snapshot.room.room : null;
    const layout = snapshot.room && snapshot.room.layout ? snapshot.room.layout : null;
    const self = actors.find((actor) =>
      (sessionCharacterId !== null && Number(actor.actorId) === Number(sessionCharacterId)) ||
      (sessionUsername && actor.displayName === sessionUsername));

    roomName.textContent = room && room.name ? room.name : ("Sala " + runtimeRoomId);
    roomStatePill.textContent = self ? "presente" : "validando";
    roomStatePill.className = self ? "state-pill live" : "state-pill neutral";
    layoutLabel.textContent = layout && layout.layoutCode ? layout.layoutCode : "desconocido";
    actorsLabel.textContent = String(actors.length);
    itemsLabel.textContent = String(items.length);
    presenceCopy.textContent = self
      ? "El emulador confirmó la presencia real del avatar en la sala."
      : "La sala existe, pero el avatar todavía no está confirmado dentro del runtime.";

    renderActors(actors);
    renderChat(snapshot.chatMessages);
    return { hasPresence: Boolean(self), snapshot };
  }

  async function confirmPresence() {
    for (let attempt = 0; attempt < 8; attempt += 1) {
      const result = await refreshRuntime();
      if (result.hasPresence) {
        return true;
      }

      await new Promise((resolve) => window.setTimeout(resolve, 600));
    }

    return false;
  }

  async function boot() {
    if (!ticket) {
      heroCopy.textContent = "Falta el ticket de sesión. Vuelve al launcher.";
      sessionChip.textContent = "inválida";
      setStatus("error", "state-pill error");
      roomName.textContent = "Sin ticket";
      chatForm.classList.add("hidden");
      return;
    }

    try {
      await sendTelemetry("client_boot_started", "Cliente web inicializado.", { roomId: String(runtimeRoomId) });
      sessionChip.textContent = "válida";
      setStatus("validando", "state-pill neutral");

      const bootstrap = await request("/launcher/bootstrap?ticket=" + encodeURIComponent(ticket));
      const session = bootstrap.session || null;
      const collector = bootstrap.collector || null;
      sessionCharacterId = session ? Number(session.characterId) : null;

      usernameLabel.textContent = session ? "Sesión activa" : "Cuenta";
      publicIdLabel.textContent = session ? ("Cuenta " + session.accountId) : "-";
      collectorLabel.textContent = collector ? (collector.collectorTier + " / " + collector.ownedCollectibleCount + " items") : "No collector";
      mottoLabel.textContent = bootstrap.interfacePreferences && bootstrap.interfacePreferences.chatStylePreference
        ? ("Chat " + bootstrap.interfacePreferences.chatStylePreference)
        : "Hotel web alpha";
      await sendTelemetry("bootstrap_validated", "Bootstrap del launcher resuelto.", {
        canLaunch: String(Boolean(bootstrap.launchEntitlement && bootstrap.launchEntitlement.canLaunch)),
        entryAssetUrl: bootstrap.entryAssetUrl || ""
      });

      heroCopy.textContent = "El cliente está abriendo tu sesión y entrando al hotel.";

      await sendTelemetry("room_entry_requested", "Solicitud de entrada enviada al hotel.", { roomId: String(runtimeRoomId) });
      await ensureRoomEntry();
      await sendTelemetry("room_entry_accepted", "El backend aceptó la entrada a la sala.", { roomId: String(runtimeRoomId) });

      const currentSession = await request("/launcher/session/current?ticket=" + encodeURIComponent(ticket)).catch(() => null);
      if (currentSession && currentSession.bootstrap && currentSession.bootstrap.character && currentSession.bootstrap.character.profile) {
        const profile = currentSession.bootstrap.character.profile;
        sessionUsername = profile.username || null;
        usernameLabel.textContent = profile.username || "Sesión activa";
        publicIdLabel.textContent = profile.publicId || "-";
        collectorLabel.textContent = currentSession.collector
          ? (currentSession.collector.collectorTier + " / " + currentSession.collector.ownedCollectibleCount + " items")
          : (collector ? (collector.collectorTier + " / " + collector.ownedCollectibleCount + " items") : "No collector");
        mottoLabel.textContent = profile.motto || "Sin misión";
      }

      const presenceConfirmed = await confirmPresence();
      entryConfirmed = presenceConfirmed;
      if (!presenceConfirmed) {
        heroCopy.textContent = "La sala abrió, pero el emulador todavía no confirma tu presencia dentro del hotel.";
        setStatus("pendiente", "state-pill neutral");
        await sendTelemetry("room_presence_pending", "La entrada fue aceptada pero todavía no hay presencia confirmada del avatar.", {
          roomId: String(runtimeRoomId)
        });
      } else {
        heroCopy.textContent = "Entrada confirmada por el emulador. El avatar ya está dentro del hotel.";
        setStatus("conectado", "state-pill live");
        await sendTelemetry("room_presence_confirmed", "El emulador confirmó la presencia real del avatar en la sala.", {
          roomId: String(runtimeRoomId)
        });
      }

      pollHandle = window.setInterval(async function () {
        try {
          const result = await refreshRuntime();
          if (result.hasPresence && !entryConfirmed) {
            entryConfirmed = true;
            heroCopy.textContent = "Entrada confirmada por el emulador. El avatar ya está dentro del hotel.";
            setStatus("conectado", "state-pill live");
            await sendTelemetry("room_presence_confirmed", "La presencia quedó confirmada durante la sincronización continua.", {
              roomId: String(runtimeRoomId)
            });
          }
        } catch {
          setStatus("reconectando", "state-pill neutral");
        }
      }, 4000);
    } catch (error) {
      heroCopy.textContent = "El cliente no pudo entrar al hotel.";
      sessionChip.textContent = "error";
      setStatus("error", "state-pill error");
      roomName.textContent = "Fallo de conexión";
      presenceCopy.textContent = String(error);
      actorsGrid.innerHTML = "<div class=\"empty-copy\">El cliente no pudo completar la entrada al hotel.</div>";
      chatFeed.innerHTML = "<div class=\"empty-copy\">Sin chat disponible.</div>";
      chatForm.classList.add("hidden");
      await sendTelemetry("client_boot_failed", String(error), { roomId: String(runtimeRoomId) });
    }
  }

  chatForm.addEventListener("submit", async function (event) {
    event.preventDefault();
    const message = chatInput.value.trim();
    if (!message) {
      return;
    }

    try {
      await request("/launcher/runtime/room-chat", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({
          ticket: ticket,
          roomId: runtimeRoomId,
          message: message
        })
      });
      chatInput.value = "";
      await refreshRuntime();
      await sendTelemetry("room_chat_sent", "Mensaje enviado desde el cliente web.", {
        roomId: String(runtimeRoomId),
        messageLength: String(message.length)
      });
    } catch (error) {
      presenceCopy.textContent = "No se pudo enviar el mensaje: " + String(error);
    }
  });

  window.addEventListener("beforeunload", function () {
    if (pollHandle) {
      window.clearInterval(pollHandle);
    }
  });

  boot();
})();
