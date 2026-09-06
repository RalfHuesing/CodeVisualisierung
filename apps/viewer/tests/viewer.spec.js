import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";

const nestedUniverseFixturePath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../contracts/graph-universe/fixtures/nested-universe.json"
);

test("loads the sample graph and shows its summary", async ({ page }) => {
  await page.goto("/");

  await expect(page.locator("#graph-status")).toHaveText("Beispieldaten erfolgreich geladen.");
  await expect(page.locator("#node-count")).toHaveText("3");
  await expect(page.locator("#link-count")).toHaveText("2");
  await expect(page.locator("#drop-hint")).toBeHidden();
  await expect(page.locator("#graph-canvas canvas")).toBeVisible();
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "3");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-link-count", "2");
});

test("opens graph details and node details", async ({ page }) => {
  await page.goto("/");

  await page.locator("#toggle-details").click();
  await expect(page.locator("#details-card")).toBeVisible();
  await expect(page.locator("#details-description")).toContainText("A tiny graph");

  await page.locator("#node-select").selectOption("orders");
  await expect(page.locator("#details-kicker")).toHaveText("Ausgewählter Node");
  await expect(page.locator("#details-title")).toHaveText("Orders");
  await expect(page.locator("#selected-node-id")).toHaveText("orders");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-selected-node-id", "orders");
});

test("selects a node from the canvas without a pointer-up position error", async ({ page }) => {
  const consoleErrors = [];
  const pageErrors = [];
  page.on("console", (message) => {
    if (message.type() === "error") {
      consoleErrors.push(message.text());
    }
  });
  page.on("pageerror", (error) => pageErrors.push(error.message));

  await page.goto("/");
  await page.waitForTimeout(3000);
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-auto-fit-count", "1");
  const canvas = page.locator("#graph-canvas canvas");
  const bounds = await canvas.boundingBox();
  expect(bounds).not.toBeNull();

  const screenshot = await page.screenshot();
  const nodePoint = await page.evaluate(async ({ imageData, canvasBounds }) => {
    const image = new globalThis.Image();
    image.src = `data:image/png;base64,${imageData}`;
    await image.decode();
    const probe = globalThis.document.createElement("canvas");
    probe.width = image.width;
    probe.height = image.height;
    const context = probe.getContext("2d");
    context.drawImage(image, 0, 0);
    const pixels = context.getImageData(0, 0, image.width, image.height).data;
    let bestNode = null;
    for (let y = Math.ceil(canvasBounds.y); y < canvasBounds.y + canvasBounds.height; y += 2) {
      for (let x = Math.ceil(canvasBounds.x); x < canvasBounds.x + canvasBounds.width * 0.75; x += 2) {
        let nodePixels = 0;
        for (let offsetY = -6; offsetY <= 6; offsetY += 1) {
          for (let offsetX = -6; offsetX <= 6; offsetX += 1) {
            const index = ((y + offsetY) * image.width + x + offsetX) * 4;
            if (pixels[index] >= 30 && pixels[index] <= 90 && pixels[index + 1] >= 140 && pixels[index + 2] >= 200) {
              nodePixels += 1;
            }
          }
        }
        if (nodePixels > (bestNode?.pixels ?? 0)) {
          bestNode = { x, y, pixels: nodePixels };
        }
      }
    }
    return bestNode;
  }, { imageData: screenshot.toString("base64"), canvasBounds: bounds });
  expect(nodePoint).not.toBeNull();
  await page.mouse.move(nodePoint.x, nodePoint.y);
  await page.waitForTimeout(100);
  await page.mouse.click(nodePoint.x, nodePoint.y);

  await expect(page.locator("#selected-node-details")).toBeVisible();
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-selected-node-id", /.+/);
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-auto-fit-count", "1");
  expect([...consoleErrors, ...pageErrors].filter((message) => message.includes("reading 'x'")).length).toBe(0);
});

test("focuses the neighborhood and supports search and reset", async ({ page }) => {
  await page.goto("/");

  await page.locator("#node-select").selectOption("orders");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-selected-node-id", "orders");
  await expect(page.locator("#selected-node-incoming")).toHaveText("1");
  await expect(page.locator("#selected-node-outgoing")).toHaveText("1");

  await page.locator("#node-search").fill("database");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-search-query", "database");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-search-match-count", "1");

  await page.locator("#reset-view").click();
  await expect(page.locator("#details-card")).toBeHidden();
  await expect(page.locator("#graph-canvas")).not.toHaveAttribute("data-selected-node-id");
  await expect(page.locator("#graph-canvas")).not.toHaveAttribute("data-search-query");
  await expect(page.locator("#node-search")).toHaveValue("");
});

test("shows a useful error for malformed uploaded JSON", async ({ page }) => {
  await page.goto("/");
  await page.locator("#graph-file").setInputFiles({
    buffer: Buffer.from("{ broken"),
    mimeType: "application/json",
    name: "broken.json"
  });

  await expect(page.locator("#graph-errors")).toBeVisible();
  await expect(page.locator("#graph-status")).toHaveText("Die Graphdaten sind ungültig.");
  await expect(page.locator("#graph-error-list")).toContainText("Die Datei enthält kein gültiges JSON.");
});

test("switches examples and exposes active visual metrics", async ({ page }) => {
  await page.goto("/");

  await page.locator("#example-select").selectOption("small");
  await page.locator("#zoom-select").selectOption("detail");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "18");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-link-count", "30");
  await expect(page.locator("#legend-node-metric")).toContainText("Komplexität");
  await expect(page.locator("#legend-link-metric")).toContainText("Gewicht");
});

test("filters the 3d graph without losing the source graph", async ({ page }) => {
  await page.goto("/");
  await page.locator("#example-select").selectOption("small");
  await page.locator("#node-select").selectOption({ label: "Component 1.1.js" });
  await expect(page.locator("#selected-node-details")).toBeVisible();

  await page.locator("#zoom-select").selectOption("overview");
  await expect(page.locator("#graph-status")).toContainText("Overview");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "10");
  await page.locator("#zoom-select").selectOption("detail");

  await page.locator("#node-type-filter").selectOption("method");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "8");
  await expect(page.locator("#selected-node-details")).toBeHidden();

  await page.locator("#reset-filters").click();
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "18");
  await page.locator("#toggle-details").click();
});

test("reports an empty but valid graph clearly", async ({ page }) => {
  await page.goto("/");
  await page.locator("#graph-file").setInputFiles({
    buffer: Buffer.from(JSON.stringify({ format: { name: "graph-universe", version: "1.0" }, nodes: [], links: [] })),
    mimeType: "application/json",
    name: "empty.json"
  });

  await expect(page.locator("#graph-status")).toHaveText("empty.json geladen: Der Graph ist leer.");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "0");
});

test("renders the large target fixture and survives a resize", async ({ page }) => {
  await page.goto("/");
  await page.locator("#example-select").selectOption("large");
  await page.locator("#zoom-select").selectOption("detail");

  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "248");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-link-count", "448");
  await page.locator("#node-select").selectOption({ label: "Component 1.1" });
  await expect(page.locator("#selected-node-details")).toBeVisible();
  await page.setViewportSize({ width: 1024, height: 700 });
  await expect(page.locator("#graph-canvas canvas")).toBeVisible();
});

test("communicates the full-mode limit and keeps larger graphs loadable", async ({ page }) => {
  await page.goto("/");

  await page.locator("#example-select").selectOption("large");
  await page.locator("#zoom-select").selectOption("detail");
  await expect(page.locator("#graph-status")).toContainText("Interaktiver Vollmodus geprüft");
  await expect(page.locator("#graph-status")).toContainText("248 Nodes / 448 Links");

  await page.locator("#example-select").selectOption("performance");
  await page.locator("#zoom-select").selectOption("detail");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "684");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-link-count", "1260");
  await expect(page.locator("#graph-status")).toContainText("Außerhalb des geprüften interaktiven Vollmodus");
  await expect(page.locator("#graph-status")).toContainText("weiterhin ladbar");
});

test("uploads the nested universe fixture and exposes declarative profiles and metrics", async ({ page }) => {
  await page.goto("/");
  await page.locator("#graph-file").setInputFiles(nestedUniverseFixturePath);

  await expect(page.locator("#node-count")).toHaveText("18");
  await expect(page.locator("#link-count")).toHaveText("19");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-layout-profile-id", "nested-overview");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "6");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-link-count", "5");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-layout-group-count", "2");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-auto-fit-count", "1");
  await expect(page.locator("#zoom-select option[value='nested-overview']")).toHaveText("Universe overview");
  await expect(page.locator("#node-metric-select option[value='mass']")).toHaveText("Mass");
  await expect(page.locator("#link-metric-select option[value='referenceStrength']")).toHaveText("Reference strength");

  await page.locator("#zoom-select").selectOption("nested-detail");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-layout-profile-id", "nested-detail");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "18");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-link-count", "19");
  await page.locator("#zoom-select").selectOption("nested-overview");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-layout-profile-id", "nested-overview");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-node-count", "6");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-auto-fit-count", "2");
  await expect(page.locator("#graph-canvas")).toHaveAttribute("data-link-count", "5");
  await page.locator("#node-metric-select").selectOption("complexity");
  await expect(page.locator("#legend-node-metric")).toContainText("Complexity");
  await expect(page.locator("#link-metric-select")).toHaveValue("dependencyCount");
  await expect(page.locator("#legend-link-metric")).toContainText("Connection count");
});

test("shows schema and semantic errors for an invalid graph", async ({ page }) => {
  await page.goto("/");
  await page.locator("#graph-file").setInputFiles({
    buffer: Buffer.from(JSON.stringify({
      format: { name: "graph-universe", version: "1.0" },
      nodes: [{ id: "same" }, { id: "same" }],
      links: [{ source: "same", target: "missing" }]
    })),
    mimeType: "application/json",
    name: "invalid.json"
  });

  await expect(page.locator("#graph-errors")).toBeVisible();
  await expect(page.locator("#graph-error-list")).toContainText("duplicated");
  await expect(page.locator("#graph-error-list")).toContainText("does not reference");
});

test("reports an unavailable WebGL context", async ({ page }) => {
  await page.addInitScript(() => {
    const canvasPrototype = globalThis.HTMLCanvasElement.prototype;
    const originalGetContext = canvasPrototype.getContext;
    canvasPrototype.getContext = function getContext(type, ...args) {
      if (type === "webgl2" || type === "webgl") {
        return null;
      }
      return originalGetContext.call(this, type, ...args);
    };
  });
  await page.goto("/");

  await expect(page.locator("#graph-errors")).toBeVisible();
  await expect(page.locator("#graph-error-list")).toContainText("WebGL");
});
