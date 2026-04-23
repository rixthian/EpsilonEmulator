import { spawnSync } from "node:child_process";

const result = spawnSync("npx", ["tsc", "-p", "tsconfig.json"], {
  stdio: "inherit",
  shell: false
});

process.exit(result.status ?? 1);
