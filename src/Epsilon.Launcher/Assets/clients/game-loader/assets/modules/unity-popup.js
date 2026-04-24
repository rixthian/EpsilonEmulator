export class UnityPopupManager {
  constructor(container) {
    this._container = container;
    this._host = null;
    this._iframe = null;
    this._visible = false;
    this._onKeyDown = (event) => {
      if (event.key === "Escape" && this._visible) {
        this.hide();
      }
    };
    this._buildDom();
  }

  _buildDom() {
    this._host = document.createElement("div");
    this._host.className = "unity-popup-host hidden";
    this._host.setAttribute("role", "dialog");
    this._host.setAttribute("aria-modal", "true");
    this._host.setAttribute("aria-label", "Unity WebGL — Epsilon Hotel");

    this._host.innerHTML = `
      <div class="unity-popup-frame">
        <div class="unity-popup-bar">
          <span class="unity-popup-title">Epsilon Hotel — Unity Web</span>
          <button class="unity-popup-close" type="button" aria-label="Cerrar Unity">✕</button>
        </div>
        <div class="unity-popup-stage">
          <iframe
            class="unity-popup-iframe"
            id="unity-iframe"
            allow="fullscreen"
            allowfullscreen
            sandbox="allow-scripts allow-same-origin allow-forms allow-popups"
            frameborder="0"
          ></iframe>
          <div class="unity-popup-loading" id="unity-loading">
            <p>Cargando Unity WebGL…</p>
          </div>
        </div>
      </div>
    `;

    this._iframe = this._host.querySelector("#unity-iframe");
    const loadingBanner = this._host.querySelector("#unity-loading");

    this._iframe.addEventListener("load", () => {
      loadingBanner.style.display = "none";
      this._iframe.style.opacity = "1";
    });

    this._host.querySelector(".unity-popup-close").addEventListener("click", () => {
      this.hide();
    });

    this._host.addEventListener("click", (event) => {
      if (event.target === this._host) {
        this.hide();
      }
    });

    this._container.appendChild(this._host);
  }

  show(unityUrl) {
    const safeUnityUrl = normalizeHttpUrl(unityUrl);
    if (!safeUnityUrl) {
      return;
    }

    if (this._visible) {
      return;
    }

    const loadingBanner = this._host.querySelector("#unity-loading");
    loadingBanner.style.display = "flex";
    this._iframe.style.opacity = "0";
    this._iframe.src = safeUnityUrl;

    this._host.classList.remove("hidden");
    this._visible = true;
    document.body.style.overflow = "hidden";
    document.addEventListener("keydown", this._onKeyDown);
  }

  hide() {
    if (!this._visible) {
      return;
    }

    this._host.classList.add("hidden");
    this._iframe.src = "about:blank";
    this._visible = false;
    document.body.style.overflow = "";
    document.removeEventListener("keydown", this._onKeyDown);
  }

  get isVisible() {
    return this._visible;
  }
}

function normalizeHttpUrl(value) {
  if (typeof value !== "string" || !value.trim()) {
    return null;
  }

  try {
    const url = new URL(value.trim(), window.location.href);
    if (url.protocol !== "http:" && url.protocol !== "https:") {
      return null;
    }

    url.username = "";
    url.password = "";
    return url.toString();
  } catch {
    return null;
  }
}
