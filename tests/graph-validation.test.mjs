import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { normalizeGraph, parseGraphText, resolveVisualToken, validateGraph } from "../apps/viewer/src/domain/graph.js";
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

  it("accepts summary links with explicit source link IDs", () => {
    const graph = structuredClone(validGraph);
    graph.links[0].derivedFrom = ["depends-orders-database"];

    expect(validateGraph(graph)).toEqual({ valid: true, errors: [] });
  });

  it("rejects the deliberately invalid fixture", () => {
    const validation = validateGraph(invalidFixture);

    expect(validation.valid).toBe(false);
    expect(validation.errors.some((error) => error.message.includes("duplicated"))).toBe(true);
    expect(validation.errors.some((error) => error.message.includes("does not reference"))).toBe(true);
    expect(validation.errors).toContainEqual({
      kind: "semantic",
      path: "/nodes/0/typeId",
      message: "Node type 'missing-node-type' does not reference a node type."
    });
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
});

describe("graph parsing", () => {
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

describe("graph-universe 0.2 definitions", () => {
  it("validates type references and definition IDs", () => {
    const graph = structuredClone(validGraph);

    graph.nodes[0].typeId = "unknown";
    expect(validateGraph(graph).errors).toContainEqual({
      kind: "semantic",
      path: "/nodes/0/typeId",
      message: "Node type 'unknown' does not reference a node type."
    });

    const duplicateDefinitionGraph = structuredClone(validGraph);
    duplicateDefinitionGraph.nodeTypes.push({ id: "service" });
    expect(validateGraph(duplicateDefinitionGraph).errors).toContainEqual({
      kind: "semantic",
      path: "/nodeTypes/6/id",
      message: "nodeTypes ID 'service' is duplicated."
    });

    const invalidMetricGraph = structuredClone(validGraph);
    invalidMetricGraph.viewProfiles[0].nodeMetric = "missing-metric";
    expect(validateGraph(invalidMetricGraph).errors).toContainEqual({
      kind: "semantic",
      path: "/viewProfiles/0/nodeMetric",
      message: "metric 'missing-metric' does not reference a defined metric."
    });

    const invalidDerivedLinkGraph = structuredClone(validGraph);
    invalidDerivedLinkGraph.links[0].derivedFrom = ["missing-link"];
    expect(validateGraph(invalidDerivedLinkGraph).errors).toContainEqual({
      kind: "semantic",
      path: "/links/0/derivedFrom/0",
      message: "Derived link 'missing-link' does not reference a link ID."
    });
  });
});

describe("graph normalization compatibility", () => {
  it("keeps 0.1-style graphs valid and adds only display defaults", () => {
    const legacyGraph = {
      format: { name: "graph-universe", version: "0.1" },
      nodes: [{ id: "one" }],
      links: [{ source: "one", target: "one" }]
    };

    expect(validateGraph(legacyGraph)).toEqual({ valid: true, errors: [] });
    expect(normalizeGraph(legacyGraph)).toMatchObject({
      facets: [],
      filterSources: [],
      projections: [],
      containmentRules: [],
      hierarchy: { containmentLinkTypes: [], acyclic: true },
      visualTokens: {},
      theme: {},
      nodes: [{ typeId: "node" }],
      links: [{ typeId: "related-to" }]
    });
  });

  it("resolves unknown visual tokens through the theme fallback", () => {
    const graph = normalizeGraph(validGraph);
    const resolved = resolveVisualToken(graph, "missing-token");

    expect(resolved).toEqual({
      id: "node",
      token: graph.visualTokens.node,
      usedFallback: true
    });
  });
});
