export class PopupManager {
  constructor(root = document.body) {
    this.root = root;
    this.host = document.createElement("div");
    this.host.className = "popup-host hidden";
    this.host.innerHTML = `
      <article class="popup-card" role="dialog" aria-modal="true" aria-labelledby="popup-title">
        <button class="popup-close" type="button" aria-label="Cerrar">×</button>
        <p class="eyebrow" id="popup-tone">Loader</p>
        <h2 id="popup-title">Mensaje</h2>
        <p id="popup-message" class="popup-message"></p>
        <div class="popup-actions" id="popup-actions"></div>
      </article>
    `;

    this.root.appendChild(this.host);
    this.host.querySelector(".popup-close").addEventListener("click", () => this.hide());
    this.host.addEventListener("click", (event) => {
      if (event.target === this.host) {
        this.hide();
      }
    });
  }

  show(options) {
    const title = options.title || "Loader";
    const message = options.message || "";
    const tone = options.tone || "neutral";
    const actions = Array.isArray(options.actions) ? options.actions : [];

    this.host.className = "popup-host " + tone;
    this.host.querySelector("#popup-tone").textContent = tone === "error" ? "Error" : "Estado";
    this.host.querySelector("#popup-title").textContent = title;
    this.host.querySelector("#popup-message").textContent = message;

    const actionHost = this.host.querySelector("#popup-actions");
    actionHost.innerHTML = "";

    actions.forEach((action) => {
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = action.label;
      button.className = action.kind === "primary" ? "popup-action primary" : "popup-action";
      button.addEventListener("click", () => {
        if (typeof action.handler === "function") {
          action.handler();
        }

        if (action.keepOpen !== true) {
          this.hide();
        }
      });
      actionHost.appendChild(button);
    });
  }

  hide() {
    this.host.classList.add("hidden");
  }
}
