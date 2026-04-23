export interface CmsSiteConfig {
  siteKey: string;
  displayName: string;
  era: string;
  launcherUrl: string;
  gatewayHealthUrl: string;
  gatewayReadinessUrl: string;
  gatewayDiagnosticsUrl: string;
  adminUrl: string;
  publicPlayerLookupPattern?: string;
  habbowoodSnapshotUrl?: string;
}

export interface CmsSiteRecord {
  key: string;
  displayName: string;
  era: string;
  rootDir: string;
  configPath: string;
  entryPage: string;
}
