import { defineConfig } from "vite";

export default defineConfig({
  root: "apps/viewer",
  build: {
    emptyOutDir: true,
    outDir: "../../dist/viewer"
  }
});
