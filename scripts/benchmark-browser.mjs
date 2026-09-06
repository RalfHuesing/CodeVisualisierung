/* global document, fetch, performance, requestAnimationFrame, setTimeout */

import { spawn } from "node:child_process";
import net from "node:net";
import path from "node:path";
import { chromium } from "@playwright/test";

const host = "127.0.0.1";
const fixtures = [
  { id: "large", expectedNodes: 248, expectedLinks: 448 },
  { id: "performance", expectedNodes: 684, expectedLinks: 1260 }
];
const fpsWindowMs = 1500;

function runCommand(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawnProcess(command, args);
    child.once("error", reject);
    child.once("exit", (code) => {
      if (code === 0) {
        resolve();
        return;
      }
      reject(new Error(`${command} ${args.join(" ")} exited with code ${code}`));
    });
  });
}

function spawnProcess(command, args) {
  if (process.platform === "win32") {
    const shellCommand = [command, ...args].join(" ");
    return spawn(process.env.ComSpec ?? "cmd.exe", ["/d", "/s", "/c", shellCommand], { stdio: "inherit" });
  }
  return spawn(command, args, { stdio: "inherit" });
}

function findFreePort() {
  return new Promise((resolve, reject) => {
    const server = net.createServer();
    server.once("error", reject);
    server.listen(0, host, () => {
      const { port } = server.address();
      server.close(() => resolve(port));
    });
  });
}

function startPreview(port) {
  const viteEntry = path.resolve("node_modules/vite/bin/vite.js");
  return spawn(process.execPath, [viteEntry, "preview", "--host", host, "--port", String(port)], { stdio: "inherit" });
}

async function waitForPreview(baseUrl) {
  const deadline = Date.now() + 30_000;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(`${baseUrl}/`);
      if (response.ok) {
        return;
      }
    } catch {
      // The preview process is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 250));
  }
  throw new Error(`Preview did not become available at ${baseUrl}`);
}

async function measureFixture(browser, baseUrl, fixture) {
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.addInitScript(() => {
    globalThis.__benchmarkStart = performance.now();
  });
  await page.goto(`${baseUrl}/`);
  await page.locator("#graph-canvas canvas").waitFor({ state: "visible" });
  const firstCanvasMs = await page.evaluate(() => performance.now() - globalThis.__benchmarkStart);

  const fixtureStart = await page.evaluate(() => performance.now());
  await page.locator("#example-select").selectOption(fixture.id);
  await page.locator("#graph-canvas").waitFor({ state: "visible" });
  await page.waitForFunction(({ nodes, links }) => {
    const canvas = document.querySelector("#graph-canvas");
    return canvas?.dataset.nodeCount === String(nodes) && canvas?.dataset.linkCount === String(links);
  }, { nodes: fixture.expectedNodes, links: fixture.expectedLinks });
  const fixtureReadyMs = await page.evaluate((start) => performance.now() - start, fixtureStart);

  const nodeId = await page.locator("#node-select option").nth(1).getAttribute("value");
  const selectionStart = await page.evaluate(() => performance.now());
  await page.locator("#node-select").selectOption(nodeId);
  await page.locator("#selected-node-details").waitFor({ state: "visible" });
  const selectionMs = await page.evaluate((start) => performance.now() - start, selectionStart);

  const fps = await page.evaluate(async (duration) => {
    const start = performance.now();
    let frames = 0;
    return new Promise((resolve) => {
      function sample(now) {
        frames += 1;
        if (now - start >= duration) {
          resolve({ frames, durationMs: now - start, fps: frames * 1000 / (now - start) });
          return;
        }
        requestAnimationFrame(sample);
      }
      requestAnimationFrame(sample);
    });
  }, fpsWindowMs);

  const heap = await page.evaluate(() => {
    if (!performance.memory) {
      return { usedMb: "unavailable", totalMb: "unavailable", limitMb: "unavailable" };
    }
    return {
      usedMb: performance.memory.usedJSHeapSize / 1_048_576,
      totalMb: performance.memory.totalJSHeapSize / 1_048_576,
      limitMb: performance.memory.jsHeapSizeLimit / 1_048_576
    };
  });
  const status = await page.locator("#graph-status").textContent();
  await context.close();

  return {
    fixture: fixture.id,
    nodes: fixture.expectedNodes,
    links: fixture.expectedLinks,
    timeToFirstVisibleCanvasMs: Number(firstCanvasMs.toFixed(2)),
    fixtureReadyMs: Number(fixtureReadyMs.toFixed(2)),
    nodeSelectionLatencyMs: Number(selectionMs.toFixed(2)),
    raf: {
      windowMs: Number(fps.durationMs.toFixed(2)),
      frames: fps.frames,
      fps: Number(fps.fps.toFixed(2))
    },
    jsHeap: heap,
    status
  };
}

async function main() {
  await runCommand(process.platform === "win32" ? "npm.cmd" : "npm", ["run", "build"]);
  const port = await findFreePort();
  const baseUrl = `http://${host}:${port}`;
  const preview = startPreview(port);
  let browser;
  try {
    browser = await chromium.launch();
    await waitForPreview(baseUrl);
    const results = [];
    for (const fixture of fixtures) {
      results.push(await measureFixture(browser, baseUrl, fixture));
    }
    console.log(JSON.stringify({
      measuredAt: new Date().toISOString(),
      browser: "Chromium",
      browserVersion: browser.version(),
      url: baseUrl,
      fpsWindowMs,
      results
    }, null, 2));
  } finally {
    try {
      await browser?.close();
    } finally {
      preview.kill();
    }
  }
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
