import { defineConfig, devices } from "@playwright/test";

// End-to-end tests drive a real browser against the whole running system:
// Next.js + Catalog API + Orders API + Orders.Worker + Docker infrastructure.
// Next.js is started here if it isn't already running; the .NET services must be running
// (see README "Run it" or scripts/dev-up.sh).
export default defineConfig({
  testDir: "./e2e",
  timeout: 60_000,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [["list"], ["html", { open: "never" }]] : "list",
  use: {
    baseURL: "http://localhost:3000",
    trace: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: "npm run dev",
    url: "http://localhost:3000",
    reuseExistingServer: true,
    timeout: 120_000,
  },
});
