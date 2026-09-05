import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { normalizeGraph, parseGraphText, validateGraph } from "../apps/viewer/src/domain/graph.js";
import { EXAMPLE_CATALOG, getExampleGraph } from "../apps/viewer/src/domain/catalog.js";
import invalidFixture from "../contracts/graph-universe/fixtures/invalid.json" with { type: "json" };
import edgeCasesFixture from "../contracts/graph-universe/fixtures/edge-cases.json" with { type: "json" };

const fixturePath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../contracts/graph-universe/fixtures/minimal.json"
);
const validGraph = JSON.parse(fs.readFileSync(fixturePath, "utf8"));

describe("fixture catalog", () => {
  it("provides several deterministic valid examples", () => {
    expect(EXAMPLE_CATALOG.map((example) => example.id)).toEqual([
      "minimal",
      "small",
      "medium",
      "large",
      "performance"
    ]);
    expect(EXAMPLE_CATALOG.every((example) => validateGraph(example.graph).valid)).toBe(true);
    expect(getExampleGraph("medium").nodes.length).toBe(64);
  });
});

describe("validateGraph", () => {

  it("accepts the minimal graph fixture", () => {
    expect(validateGraph(validGraph)).toEqual({ valid: true, errors: [] });
  });

  it("rejects the deliberately invalid fixture", () => {
    const validation = validateGraph(invalidFixture);

    expect(validation.valid).toBe(false);
    expect(validation.errors.some((error) => error.message.includes("duplicated"))).toBe(true);
    expect(validation.errors.some((error) => error.message.includes("does not reference"))).toBe(true);
  });

  it("accepts edge cases without requiring optional values", () => {
    expect(validateGraph(edgeCasesFixture)).toEqual({ valid: true, errors: [] });
    expect(normalizeGraph(edgeCasesFixture).nodes.find((node) => node.id === "isolated")).toMatchObject({ tags: [], metrics: {} });
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

  it("parses and normalizes valid JSON text", () => {
    const result = parseGraphText(JSON.stringify(validGraph));

    expect(result.valid).toBe(true);
    expect(result.graph.nodes[0].label).toBe("API");
    expect(result.graph.links[0].directed).toBe(true);
  });

  it("returns a JSON error for malformed text", () => {
    expect(parseGraphText("{ broken")).toEqual({
      errors: [{ kind: "json", path: "$", message: "Die Datei enthält kein gültiges JSON." }],
      graph: null,
      valid: false
    });
  });

  it("fills display defaults without mutating the source graph", () => {
    const graph = {
      ...structuredClone(validGraph),
      nodes: [{ id: "single" }],
      links: [{ source: "single", target: "single" }]
    };

    const normalized = normalizeGraph(graph);

    expect(normalized.nodes[0]).toMatchObject({
      groupId: null,
      kind: "node",
      label: "single",
      tags: []
    });
    expect(normalized.links[0]).toMatchObject({
      directed: true,
      kind: "related-to"
    });
    expect(graph.nodes[0]).toEqual({ id: "single" });
  });
});
