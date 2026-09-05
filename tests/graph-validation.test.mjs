import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { validateGraph } from "../apps/viewer/src/domain/graph.js";

const fixturePath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../contracts/graph-universe/fixtures/minimal.json"
);
const validGraph = JSON.parse(fs.readFileSync(fixturePath, "utf8"));

describe("validateGraph", () => {
  it("accepts the minimal graph fixture", () => {
    expect(validateGraph(validGraph)).toEqual({ valid: true, errors: [] });
  });

  it("rejects duplicate node IDs", () => {
    const graph = structuredClone(validGraph);
    graph.nodes.push({ id: "api" });

    expect(validateGraph(graph).errors).toContainEqual({
      kind: "semantic",
      path: "/nodes/3/id",
      message: "Node ID 'api' is duplicated."
    });
  });

  it("rejects links to unknown nodes", () => {
    const graph = structuredClone(validGraph);
    graph.links[0].target = "missing";

    expect(validateGraph(graph).errors).toContainEqual({
      kind: "semantic",
      path: "/links/0/target",
      message: "Link target 'missing' does not reference a node."
    });
  });

  it("rejects non-finite metric values at the JSON schema boundary", () => {
    const graph = structuredClone(validGraph);
    graph.nodes[0].metrics.importance = Number.POSITIVE_INFINITY;

    expect(validateGraph(graph).errors).toContainEqual({
      kind: "schema",
      path: "/nodes/0/metrics/importance",
      message: "must be number"
    });
  });
});
