import { expect, test } from "@playwright/test";

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
