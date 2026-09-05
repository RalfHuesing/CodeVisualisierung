import { expect, test } from "@playwright/test";

test("loads the sample graph and shows its summary", async ({ page }) => {
  await page.goto("/");

  await expect(page.locator("#graph-status")).toHaveText("Beispieldaten erfolgreich geladen.");
  await expect(page.locator("#node-count")).toHaveText("3");
  await expect(page.locator("#link-count")).toHaveText("2");
  await expect(page.locator("#graph-canvas .graph-node")).toHaveCount(3);
  await expect(page.locator("#graph-canvas .graph-link")).toHaveCount(2);
  await expect(page.locator("#graph-canvas .graph-link[marker-end]")).toHaveCount(2);
});

test("opens graph details and node details", async ({ page }) => {
  await page.goto("/");

  await page.locator("#toggle-details").click();
  await expect(page.locator("#details-card")).toBeVisible();
  await expect(page.locator("#details-description")).toContainText("A tiny graph");

  await page.locator("#graph-canvas .graph-node[data-node-id='orders']").click();
  await expect(page.locator("#details-kicker")).toHaveText("Ausgewählter Node");
  await expect(page.locator("#details-title")).toHaveText("Orders");
  await expect(page.locator("#selected-node-id")).toHaveText("orders");
  await expect(page.locator("#graph-canvas .graph-node.is-selected")).toHaveAttribute("data-node-id", "orders");
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
