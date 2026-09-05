import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./apps/viewer/tests",
  use: {
    baseURL: "http://127.0.0.1:4173"
  },
  webServer: {
    command: "npm run build && npm run preview -- --host 127.0.0.1",
    port: 4173,
    reuseExistingServer: true
  }
});
