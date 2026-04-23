(function () {
  const config = {
    launcherUrl: "/",
    gatewayHealthUrl: "/health",
    gatewayReadinessUrl: "/readiness",
    gatewayDiagnosticsUrl: "/diagnostics/summary",
    adminUrl: "/diagnostics/summary",
    habbowoodSnapshotUrl: "/hotel/events/habbowood"
  };

  async function getJson(url) {
    try {
      const response = await fetch(url, {
        headers: { Accept: "application/json" }
      });

      if (!response.ok) {
        return { ok: false, status: response.status };
      }

      return { ok: true, data: await response.json() };
    } catch (error) {
      return { ok: false, error: String(error) };
    }
  }

  function setText(id, value) {
    const node = document.getElementById(id);
    if (node) {
      node.textContent = value;
    }
  }

  function setMany(ids, value) {
    ids.forEach((id) => setText(id, value));
  }

  function setHtml(id, value) {
    const node = document.getElementById(id);
    if (node) {
      node.innerHTML = value;
    }
  }

  function bindLinks() {
    document.querySelectorAll("[data-launcher-link]").forEach((node) => {
      node.setAttribute("href", config.launcherUrl);
    });

    document.querySelectorAll("[data-admin-link]").forEach((node) => {
      node.setAttribute("href", config.adminUrl);
    });
  }

  async function loadStatus() {
    const health = await getJson(config.gatewayHealthUrl);
    const readiness = await getJson(config.gatewayReadinessUrl);
    const diagnostics = await getJson(config.gatewayDiagnosticsUrl);
    const habbowood = await getJson(config.habbowoodSnapshotUrl);

    setMany(["epsilon-health", "epsilon-health-inline"], health.ok ? "online" : "unreachable");
    setMany(["epsilon-readiness", "epsilon-readiness-inline"], readiness.ok ? "ready" : "degraded");

    if (diagnostics.ok) {
      const overall =
        diagnostics.data.overallStatus ||
        diagnostics.data.overall ||
        diagnostics.data.status ||
        "unknown";
      setMany(["epsilon-runtime", "epsilon-runtime-inline"], String(overall));

      const lines = [];
      if (diagnostics.data.protocolHealth && diagnostics.data.protocolHealth.state) {
        lines.push("Protocol: " + diagnostics.data.protocolHealth.state);
      }
      if (diagnostics.data.realtime && typeof diagnostics.data.realtime.activeConnections !== "undefined") {
        lines.push("Realtime connections: " + diagnostics.data.realtime.activeConnections);
      }
      if (diagnostics.data.persistence && diagnostics.data.persistence.readiness) {
        lines.push("Persistence: " + diagnostics.data.persistence.readiness);
      }

      setHtml(
        "epsilon-diagnostics",
        lines.length ? "<li>" + lines.join("</li><li>") + "</li>" : "<li>No diagnostics detail available</li>"
      );
    } else {
      setMany(["epsilon-runtime", "epsilon-runtime-inline"], "unavailable");
      setHtml("epsilon-diagnostics", "<li>Diagnostics endpoint not available</li>");
    }

    if (habbowood.ok && habbowood.data) {
      const snapshot = habbowood.data;
      setText("epsilon-event-name", snapshot.displayName || "Habbowood");
      setMany(["epsilon-event-state", "epsilon-event-state-inline"], snapshot.isActive ? "active" : "inactive");
    }
  }

  bindLinks();
  loadStatus();
})();
