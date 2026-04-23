const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("epsilonLauncher", {
  getRuntimeInfo: () => ipcRenderer.invoke("launcher:get-runtime-info"),
  getLocalConfig: () => ipcRenderer.invoke("launcher:get-local-config"),
  saveLocalConfig: (partialConfig) => ipcRenderer.invoke("launcher:save-local-config", partialConfig),
  getDesktopConfig: () => ipcRenderer.invoke("launcher:get-desktop-config"),
  getUpdateChannels: () => ipcRenderer.invoke("launcher:get-update-channels"),
  getLaunchProfiles: (input) => ipcRenderer.invoke("launcher:get-launch-profiles", input),
  redeemCode: (input) => ipcRenderer.invoke("launcher:redeem-code", input),
  selectProfile: (input) => ipcRenderer.invoke("launcher:select-profile", input),
  clientStarted: (input) => ipcRenderer.invoke("launcher:client-started", input),
  openClient: (input) => ipcRenderer.invoke("launcher:open-client", input),
  openUrl: (targetUrl) => ipcRenderer.invoke("launcher:open-url", targetUrl)
});

