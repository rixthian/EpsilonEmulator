import { readdirSync, existsSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";
import type { CmsSiteConfig, CmsSiteRecord } from "./types.js";

const cmsRoot = resolve(process.cwd(), "..");

function readConfig(path: string): CmsSiteConfig {
  return JSON.parse(readFileSync(path, "utf8")) as CmsSiteConfig;
}

export function loadSiteRegistry(): CmsSiteRecord[] {
  const entries = readdirSync(cmsRoot, { withFileTypes: true });
  const sites: CmsSiteRecord[] = [];

  for (const entry of entries) {
    if (!entry.isDirectory() || entry.name === "system") {
      continue;
    }

    const configPath = join(cmsRoot, entry.name, "config", "site.json");
    const siteRoot = join(cmsRoot, entry.name, "site");
    const entryPage = join(siteRoot, "index.html");

    if (!existsSync(configPath) || !existsSync(siteRoot) || !existsSync(entryPage)) {
      continue;
    }

    const config = readConfig(configPath);
    sites.push({
      key: config.siteKey,
      displayName: config.displayName,
      era: config.era,
      rootDir: siteRoot,
      configPath,
      entryPage
    });
  }

  return sites.sort((left, right) => left.era.localeCompare(right.era));
}
