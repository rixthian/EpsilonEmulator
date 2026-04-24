(function () {
  const params = new URLSearchParams(window.location.search);
  const page = document.body.dataset.page || "home";
  const banner = document.getElementById("status-banner");
  let currentLauncherCode = "";
  let currentLauncherPackages = [];

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
    if (!banner) {
      return;
    }

    if (!text) {
      banner.className = "notice hidden";
      banner.textContent = "";
      return;
    }

    banner.textContent = text;
    banner.className = "notice " + mode;
  }

  if (params.get("error")) {
    const key = params.get("error");
    setBanner(messages[key] || ("Error: " + key), "error");
  }

  if (params.get("welcome")) {
    setBanner("Tu cuenta web está lista. Cuando quieras jugar, abre la app de Epsilon.", "success");
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
      accessGuest?.classList.add("hidden");
      accessAuthed?.classList.remove("hidden");

      if (sessionPill) {
        sessionPill.textContent = "Cuenta conectada";
        sessionPill.className = "session-pill ok";
      }

      const username = session.username || "Habbo";
      const launcherLinks = document.querySelectorAll("#launcher-cta-top, #account-launcher-link, #hero-launcher-link");
      launcherLinks.forEach((node) => node.setAttribute("href", "#access"));

      const initials = document.getElementById("account-initials");
      const accountUsername = document.getElementById("account-username");
      const accountMotto = document.getElementById("account-motto");
      const accountCollector = document.getElementById("account-collector");
      const accountLaunch = document.getElementById("account-launch");

      if (initials) {
        initials.textContent = username.slice(0, 2).toUpperCase();
      }
      if (accountUsername) {
        accountUsername.textContent = username;
      }
      if (accountMotto) {
        accountMotto.textContent = session.motto || "Tu cuenta web está lista para preparar el acceso.";
      }
      if (accountCollector) {
        accountCollector.textContent = "Cuenta web conectada";
      }
      if (accountLaunch) {
        accountLaunch.textContent = session.canLaunch
          ? "Puedes generar código para la app"
          : "Tu acceso al launcher todavía no está habilitado";
      }
    } else {
      if (sessionPill) {
        sessionPill.textContent = "Invitado";
        sessionPill.className = "session-pill neutral";
      }
      accessGuest?.classList.remove("hidden");
      accessAuthed?.classList.add("hidden");
    }
  }

  function renderLauncherPackages(items) {
    const root = document.getElementById("launcher-packages");
    if (!root) {
      return;
    }

    currentLauncherPackages = Array.isArray(items) ? items : [];

    if (currentLauncherPackages.length === 0) {
      root.innerHTML = `
        <article class="support-card package-card">
          <strong>Launcher</strong>
          <p>La descarga pública todavía no está publicada.</p>
        </article>
      `;
      return;
    }

    root.innerHTML = currentLauncherPackages.map((item) => `
      <article class="support-card package-card">
        <strong>${item.label}</strong>
        <p>${item.status === "ready" ? "Descarga disponible" : "Disponible más adelante"}</p>
        ${item.downloadUrl
          ? `<a class="button-link soft" href="${item.downloadUrl}">Descargar</a>`
          : `<span class="category-pill">Próximamente</span>`}
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
        ? "Abre la app de Epsilon y usa este código para iniciar sesión."
        : "Genera un código temporal para usarlo en la app.";
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

  function getReadyLauncherPackage() {
    return currentLauncherPackages.find((item) => item && item.status === "ready" && item.downloadUrl) || null;
  }

  function buildLauncherDeepLink(code) {
    return "epsilonlauncher://open";
  }

  async function ensureLauncherCode() {
    if (currentLauncherCode) {
      return currentLauncherCode;
    }

    const payload = await request("/cms/api/launcher/code", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ platformKind: "native_app" })
    });

    applyLauncherCode(payload);
    return currentLauncherCode;
  }

  async function openInstalledLauncher() {
    const launchButton = document.getElementById("launcher-cta-access");

    try {
      if (launchButton) {
        launchButton.disabled = true;
        launchButton.textContent = "Abriendo app…";
      }

      if (!currentLauncherCode) {
        setBanner("Primero obtén y copia tu código de inicio desde la CMS.", "error");
        return;
      }

      setBanner("La app debe abrirse ahora. Después pega tu código dentro del launcher.", "success");
      window.location.href = buildLauncherDeepLink(currentLauncherCode);
    } catch (error) {
      const requestError = error && typeof error === "object" ? error : null;
      const payload = requestError && "payload" in requestError ? requestError.payload : null;
      const rawError = payload && typeof payload === "object" && payload && "error" in payload ? String(payload.error) : "";

      if (rawError === "launch_not_available") {
        setBanner("Tu cuenta web todavía no tiene acceso habilitado para el launcher.", "error");
      } else {
        setBanner("No se pudo abrir la app instalada. Usa la descarga de tu plataforma si hace falta.", "error");
      }
    } finally {
      if (launchButton) {
        window.setTimeout(() => {
          launchButton.disabled = false;
          launchButton.textContent = "Abrir app instalada";
        }, 800);
      }
    }
  }

  function renderNews(items) {
    const featured = document.getElementById("featured-news");
    const root = document.getElementById("news-list");
    const list = Array.isArray(items) ? items : [];
    const first = list[0];
    const rest = list.slice(1, 4);

    if (featured && first) {
      featured.innerHTML = `
        <div class="featured-art" style="background:${first.palette || "linear-gradient(135deg, #2d5ca8 0%, #194372 48%, #52b3d9 100%)"}"></div>
        <div class="featured-copy">
          <strong>${first.title}</strong>
          <p>${first.summary}</p>
          <a href="${first.ctaHref}">${first.ctaLabel}</a>
        </div>
      `;
    }

    if (root) {
      root.innerHTML = rest.map((item) => `
        <article class="news-card">
          <p class="category-pill">${item.category}</p>
          <h3>${item.title}</h3>
          <p>${item.summary}</p>
          <a href="${item.ctaHref}">${item.ctaLabel}</a>
        </article>
      `).join("");
    }
  }

  function renderPhotos(items) {
    const root = document.getElementById("photo-grid");
    if (!root) {
      return;
    }

    if (!items || items.length === 0) {
      root.innerHTML = `
        <article class="photo-card empty-card">
          <div class="photo-copy">
            <strong>Sin fotos públicas todavía</strong>
            <p>Cuando el hotel publique capturas reales, aparecerán aquí.</p>
          </div>
        </article>
      `;
      return;
    }

    root.innerHTML = (items || []).map((item) => `
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

    const list = Array.isArray(items) ? items : [];
    const html = list.length > 0
      ? list.map((item) => `
      <article class="leader-row">
        <div class="rank-badge">#${item.rank}</div>
        <div>
          <strong>${item.username}</strong>
          <p>${item.label}</p>
        </div>
        <span class="score-pill">${item.score}</span>
      </article>
    `).join("")
      : `
        <article class="support-card">
          <strong>Sin actividad pública todavía</strong>
          <p>La CMS no inventa rankings. Aquí solo aparecerán datos reales cuando existan.</p>
        </article>
      `;

    roots.forEach((root) => {
      root.innerHTML = html;
    });
  }

  function renderSupport(items) {
    const root = document.getElementById("support-list");
    if (!root) {
      return;
    }

    if (!items || items.length === 0) {
      root.innerHTML = `
        <article class="support-card">
          <strong>Ayuda del hotel</strong>
          <p>La guía pública aparecerá aquí cuando esté publicada.</p>
        </article>
      `;
      return;
    }

    root.innerHTML = (items || []).map((item) => `
      <article class="support-card">
        <strong>${item.title}</strong>
        <p>${item.summary}</p>
      </article>
    `).join("");
  }

  async function hydrateHome() {
    try {
      const payload = await request("/cms/api/home");
      const hotelStatus = document.getElementById("hotel-status-copy");

      if (hotelStatus) {
        hotelStatus.textContent = payload.hotel.gatewayUp && payload.hotel.launcherUp
          ? "Portal y launcher disponibles"
          : "Portal en preparación";
      }

      applySession(payload.session);
      renderNews(payload.news || []);
      renderPhotos(payload.photos || []);
      renderLeaderboard(payload.leaderboard || []);
      renderSupport(payload.support || []);
      renderLauncherPackages(payload.settings?.launcherPackages || []);

      if (payload.session && payload.session.authenticated) {
        await hydrateLauncherAccess();
      }
    } catch {
      setBanner("La CMS no pudo cargar la portada.", "error");
    }
  }

  if (page === "home") {
    const codeButton = document.getElementById("launcher-code-button");
    const codeCopy = document.getElementById("launcher-code-copy");
    const accessButton = document.getElementById("launcher-cta-access");

    if (accessButton) {
      accessButton.addEventListener("click", async function () {
        await openInstalledLauncher();
      });
    }

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
          codeButton.textContent = "Obtener código";
          setBanner("Código listo. Cópialo y úsalo dentro de la app.", "success");
        } catch {
          setBanner("No se pudo generar el código de inicio.", "error");
          codeButton.textContent = "Obtener código";
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
