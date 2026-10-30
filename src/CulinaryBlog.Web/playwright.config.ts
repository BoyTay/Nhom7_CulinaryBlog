import { defineConfig } from "@playwright/test";

// Runs against a live stack (docker compose up). Uses the locally installed Chrome so no browser download is needed.
export default defineConfig({
  testDir: "./e2e",
  timeout: 60_000,
  fullyParallel: false,
  workers: 1,
  reporter: [["list"]],
  use: {
    baseURL: process.env.BASE_URL ?? "http://localhost",
    channel: process.env.E2E_BROWSER_CHANNEL ?? "chrome",
    trace: "retain-on-failure",
  },
});
