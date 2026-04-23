(function () {
  const config = {
    launcherUrl: "/",
    gatewayHealthUrl: "/health",
    gatewayReadinessUrl: "/readiness",
    gatewayDiagnosticsUrl: "/diagnostics/summary",
    adminUrl: "/diagnostics/summary",
    habbowoodSnapshotUrl: "/hotel/events/habbowood"
  };

  function setText(id, value) {
    const node = document.getElementById(id);
    if (node) {
      node.textContent = value;
    }
  }

  function setMany(ids, value) {
    ids.forEach((id) => setText(id, value));
  }

  async function getJson(url) {
    try {
      const response = await fetch(url, { headers: { Accept: "application/json" } });
      if (!response.ok) {
        return { ok: false, status: response.status };
      }

      return { ok: true, data: await response.json() };
    } catch (error) {
      return { ok: false, error: String(error) };
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
    } else {
      setMany(["epsilon-runtime", "epsilon-runtime-inline"], "unavailable");
    }

    if (habbowood.ok && habbowood.data) {
      const state = habbowood.data.isActive ? "active" : "inactive";
      setMany(["epsilon-event-state", "epsilon-event-state-inline"], state);
      setText("epsilon-event-name", habbowood.data.displayName || "Habbowood");
    }
  }

  bindLinks();
  loadStatus();
})();
