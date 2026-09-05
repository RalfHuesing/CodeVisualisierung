import { expect, test } from "@playwright/test";

test("loads the sample graph and shows its summary", async ({ page }) => {
  await page.goto("/");

  await expect(page.locator("#graph-status")).toHaveText("Beispieldaten erfolgreich geladen.");
  await expect(page.locator("#node-count")).toHaveText("3");
  await expect(page.locator("#link-count")).toHaveText("2");
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
