(function () {
  const params = new URLSearchParams(window.location.search);
  const page = document.body.dataset.page || "home";
  const banner = document.getElementById("status-banner");
  let currentLauncherCode = "";

  const messages = {
    username_taken: "Ese nombre Habbo ya existe.",
    email_taken: "Ese email ya existe.",
    username_invalid: "El nombre Habbo no es válido.",
    email_invalid: "El email no es válido.",
    password_too_short: "La contraseña es demasiado corta.",
    register_failed: "No se pudo completar el registro.",
    login_failed: "No se pudo completar el acceso.",
    character_not_found: "La cuenta todavía no está lista o no existe.",
    invalid_credentials: "Usuario o contraseña incorrectos.",
    portal_failure: "La CMS tuvo un fallo procesando la acción."
  };

  function setBanner(text, mode) {
    if (!banner || !text) {
      return;
    }

    banner.textContent = text;
    banner.className = "notice " + mode;
  }

  if (params.get("error")) {
    const key = params.get("error");
    setBanner(messages[key] || ("Error: " + key), "error");
  }

  const loginName = document.getElementById("login-name");
  if (loginName && params.get("username")) {
    loginName.value = params.get("username");
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
      const error = new Error("request_failed");
      error.payload = payload;
      throw error;
    }

    return payload;
  }

  function applySession(session) {
    const guestOnly = document.querySelectorAll(".guest-only");
    const authedOnly = document.querySelectorAll(".authed-only");
    const sessionPill = document.getElementById("session-pill");
    const accessGuest = document.getElementById("access-guest");
    const accessAuthed = document.getElementById("access-authed");

    if (session && session.authenticated) {
      guestOnly.forEach((node) => node.classList.add("hidden"));
      authedOnly.forEach((node) => node.classList.remove("hidden"));
      if (accessGuest) {
        accessGuest.classList.add("hidden");
      }
      if (accessAuthed) {
        accessAuthed.classList.remove("hidden");
      }
      if (sessionPill) {
        sessionPill.textContent = "Sesión lista";
        sessionPill.className = "session-pill ok";
      }

      const username = session.username || "Habbo";
      const launcherUrl = "http://127.0.0.1:5001/launcher/loader?ticket=" + encodeURIComponent(session.ticket || "");
      const launcherCtas = document.querySelectorAll("#launcher-cta-access, #launcher-cta-top, #account-launcher-link");
      launcherCtas.forEach((node) => {
        node.setAttribute("href", launcherUrl);
      });

      const accountGuest = document.getElementById("account-guest");
      const accountAuthed = document.getElementById("account-authed");
      if (accountGuest) {
        accountGuest.classList.add("hidden");
      }
      if (accountAuthed) {
        accountAuthed.classList.remove("hidden");
      }

      const initials = document.getElementById("account-initials");
      const accountUsername = document.getElementById("account-username");
      const accountPublicId = document.getElementById("account-public-id");
      const accountMotto = document.getElementById("account-motto");
      const accountRoom = document.getElementById("account-room");
      const accountCollector = document.getElementById("account-collector");
      const accountLaunch = document.getElementById("account-launch");

      if (initials) {
        initials.textContent = username.slice(0, 2).toUpperCase();
      }
      if (accountUsername) {
        accountUsername.textContent = username;
      }
      if (accountPublicId) {
        accountPublicId.textContent = session.publicId ? "ID " + session.publicId : "Sin PublicId";
      }
      if (accountMotto) {
        accountMotto.textContent = session.motto || "Sin misión todavía.";
      }
      if (accountRoom) {
        accountRoom.textContent = session.roomId ? "Sala " + session.roomId : "Fuera de sala";
      }
      if (accountCollector) {
        accountCollector.textContent = session.collectorTier
          ? session.collectorTier + " / " + session.ownedCollectibleCount + " items"
          : "Jugador estándar";
      }
      if (accountLaunch) {
        accountLaunch.textContent = session.canLaunch ? "Lista para jugar" : "Acceso bloqueado";
      }
    } else if (sessionPill) {
      sessionPill.textContent = "Invitado";
      sessionPill.className = "session-pill neutral";
      if (accessGuest) {
        accessGuest.classList.remove("hidden");
      }
      if (accessAuthed) {
        accessAuthed.classList.add("hidden");
      }
    }
  }

  function renderLauncherPackages(items) {
    const root = document.getElementById("launcher-packages");
    if (!root) {
      return;
    }

    root.innerHTML = (items || []).map((item) => `
      <article class="support-card package-card">
        <strong>${item.label}</strong>
        <p>${item.fileKind} / ${item.status === "ready" ? "listo" : "pendiente"}</p>
        ${item.downloadUrl
          ? `<a class="button-link secondary small" href="${item.downloadUrl}">Descargar</a>`
          : `<span class="audience-pill">Próximamente</span>`}
      </article>
    `).join("");
  }

  function applyLauncherCode(codePayload) {
    const codeValue = document.getElementById("launcher-code-value");
    const codeMeta = document.getElementById("launcher-code-meta");
    const copyButton = document.getElementById("launcher-code-copy");

    currentLauncherCode = codePayload && codePayload.code ? codePayload.code : "";

    if (codeValue) {
      codeValue.textContent = currentLauncherCode || "Sin generar";
    }
    if (codeMeta) {
      codeMeta.textContent = currentLauncherCode
        ? "Expira en pocos minutos. La app/launcher debe canjearlo contra el emulador."
        : "Genera un código temporal para la app.";
    }
    if (copyButton) {
      copyButton.disabled = !currentLauncherCode;
    }
  }

  async function hydrateLauncherAccess() {
    const access = await request("/cms/api/launcher/access");
    renderLauncherPackages(access.launcherPackages || []);
    applyLauncherCode(access.appLaunchCode);
  }

  function renderNews(items) {
    const root = document.getElementById("news-list");
    const featured = document.getElementById("featured-news");
    if (!root) {
      return;
    }

    const first = items[0];
    if (featured && first) {
      featured.innerHTML = `
        <div class="featured-art" style="background:${first.palette || "linear-gradient(135deg, #2e55a9 0%, #1d3e7b 45%, #71c7ef 100%)"}"></div>
        <div class="featured-copy">
          <strong>${first.title}</strong>
          <p>${first.summary}</p>
          <a href="${first.ctaHref}">${first.ctaLabel}</a>
        </div>
      `;
    }

    root.innerHTML = items.map((item) => `
      <article class="news-card">
        <p class="category-pill">${item.category}</p>
        <h3>${item.title}</h3>
        <p>${item.summary}</p>
        <a href="${item.ctaHref}">${item.ctaLabel}</a>
      </article>
    `).join("");
  }

  function renderPhotos(items) {
    const root = document.getElementById("photo-grid");
    if (!root) {
      return;
    }

    root.innerHTML = items.map((item) => `
      <article class="photo-card">
        <div class="photo-art" style="background:${item.palette}"></div>
        <div class="photo-copy">
          <strong>${item.roomName}</strong>
          <p>${item.caption}</p>
        </div>
      </article>
    `).join("");
  }

  function renderLeaderboard(items) {
    const roots = [
      document.getElementById("leaderboard-list"),
      document.getElementById("leaderboard-repeat")
    ].filter(Boolean);
    if (!roots.length) {
      return;
    }

    const html = items.map((item) => `
      <article class="leader-row">
        <div class="rank-badge">#${item.rank}</div>
        <div>
          <strong>${item.username}</strong>
          <p>${item.label}</p>
        </div>
        <span class="score-pill">${item.score}</span>
      </article>
    `).join("");

    roots.forEach((root) => {
      root.innerHTML = html;
    });
  }

  function renderSupport(items) {
    const root = document.getElementById("support-list");
    if (!root) {
      return;
    }

    root.innerHTML = items.map((item) => `
      <article class="support-card">
        <strong>${item.title}</strong>
        <p>${item.summary}</p>
        <span class="audience-pill">${item.audience}</span>
      </article>
    `).join("");
  }

  function renderHotelCore(hotel) {
    const root = document.getElementById("hotel-core");
    if (!root) {
      return;
    }

    root.innerHTML = `
      <article class="support-card">
        <strong>Datos del hotel</strong>
        <p>${hotel.persistenceProvider} / ${hotel.persistenceReady ? "listo" : "pendiente"}</p>
      </article>
      <article class="support-card">
        <strong>Conexión</strong>
        <p>${hotel.realtimeTransport}</p>
      </article>
      <article class="support-card">
        <strong>Salas activas</strong>
        <p>${hotel.activeRoomCount}</p>
      </article>
      <article class="support-card">
        <strong>Partidas activas</strong>
        <p>${hotel.activeGameSessionCount}</p>
      </article>
      <article class="support-card">
        <strong>Estado general</strong>
        <p>${hotel.totalScore}%</p>
      </article>
    `;
  }

  async function hydrateHome() {
    try {
      const payload = await request("/cms/api/home");
      const gatewayPill = document.getElementById("gateway-pill");
      const launcherPill = document.getElementById("launcher-pill");
      const hotelStatus = document.getElementById("hotel-status-copy");

      if (gatewayPill) {
        gatewayPill.textContent = payload.hotel.gatewayUp ? "Gateway listo" : "Gateway caído";
        gatewayPill.className = "service-pill " + (payload.hotel.gatewayUp ? "ok" : "warn");
      }
      if (launcherPill) {
        launcherPill.textContent = payload.hotel.launcherUp ? "Launcher listo" : "Launcher caído";
        launcherPill.className = "service-pill " + (payload.hotel.launcherUp ? "ok" : "warn");
      }
      if (hotelStatus) {
        hotelStatus.textContent = payload.hotel.gatewayUp && payload.hotel.launcherUp
          ? "Hotel abierto"
          : "Hotel en preparación";
      }

      applySession(payload.session);
      renderLauncherPackages(payload.settings?.launcherPackages || []);
      renderNews(payload.news || []);
      renderPhotos(payload.photos || []);
      renderLeaderboard(payload.leaderboard || []);
      renderSupport(payload.support || []);
      renderHotelCore(payload.hotel || {});

      if (payload.session && payload.session.authenticated) {
        await hydrateLauncherAccess();
      }
    } catch (error) {
      setBanner("La portada no pudo cargar el backend CMS.", "error");
    }
  }

  if (page === "home") {
    const codeButton = document.getElementById("launcher-code-button");
    const codeCopy = document.getElementById("launcher-code-copy");

    if (codeButton) {
      codeButton.addEventListener("click", async function () {
        try {
          codeButton.disabled = true;
          codeButton.textContent = "Generando…";
          const payload = await request("/cms/api/launcher/code", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ platformKind: "native_app" })
          });
          applyLauncherCode(payload);
          codeButton.textContent = "Regenerar código";
        } catch {
          codeButton.textContent = "Generar código";
        } finally {
          codeButton.disabled = false;
        }
      });
    }

    if (codeCopy) {
      codeCopy.addEventListener("click", async function () {
        if (!currentLauncherCode) {
          return;
        }

        try {
          await navigator.clipboard.writeText(currentLauncherCode);
          codeCopy.textContent = "Copiado";
          window.setTimeout(() => {
            codeCopy.textContent = "Copiar";
          }, 1200);
        } catch {
        }
      });
    }

    hydrateHome();
  }
})();
