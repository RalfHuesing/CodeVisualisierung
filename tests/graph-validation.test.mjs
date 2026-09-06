import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { normalizeGraph, parseGraphText, resolveVisualToken, validateGraph } from "../apps/viewer/src/domain/graph.js";
import { EXAMPLE_CATALOG, getExampleGraph } from "../apps/viewer/src/domain/catalog.js";
import invalidFixture from "../contracts/graph-universe/fixtures/invalid.json" with { type: "json" };
import edgeCasesFixture from "../contracts/graph-universe/fixtures/edge-cases.json" with { type: "json" };
import familyFixture from "../contracts/graph-universe/fixtures/family.json" with { type: "json" };
import companyFixture from "../contracts/graph-universe/fixtures/company.json" with { type: "json" };
import csharpReferenceFixture from "../contracts/graph-universe/fixtures/csharp-reference.json" with { type: "json" };
import spatialFixture from "../contracts/graph-universe/fixtures/spatial.json" with { type: "json" };
import nestedUniverseFixture from "../contracts/graph-universe/fixtures/nested-universe.json" with { type: "json" };

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
      "performance",
      "family",
      "company"
    ]);
    expect(EXAMPLE_CATALOG.every((example) => validateGraph(example.graph).valid)).toBe(true);
    expect(getExampleGraph("medium").nodes.length).toBe(64);
  });

  it("provides domain-neutral family and company fixtures with stable graph references", () => {
    for (const fixture of [familyFixture, companyFixture]) {
      const nodeIds = new Set(fixture.nodes.map((node) => node.id));
      const linkIds = new Set(fixture.links.map((link) => link.id));

      expect(nodeIds.size).toBe(fixture.nodes.length);
      expect(linkIds.size).toBe(fixture.links.length);
      expect(fixture.links.every((link) => nodeIds.has(link.source) && nodeIds.has(link.target))).toBe(true);
      expect(validateGraph(fixture)).toEqual({ valid: true, errors: [] });
    }

    expect(familyFixture.links.find((link) => link.id === "ancestor-anna-fiona")).toMatchObject({
      summary: true,
      derivedFrom: ["parent-anna-clara", "parent-clara-fiona"]
    });
    expect(companyFixture.links.find((link) => link.id === "exposure-portfolio-delta")).toMatchObject({
      summary: true,
      derivedFrom: ["owns-aurora-birch", "invests-birch-delta"]
    });
  });
});

describe("C# reference fixture", () => {
  it("validates the deterministic C# reference fixture without catalog registration", () => {
    const nodeIds = new Set(csharpReferenceFixture.nodes.map((node) => node.id));
    const linkIds = new Set(csharpReferenceFixture.links.map((link) => link.id));
    const nodeTypeIds = new Set(csharpReferenceFixture.nodeTypes.map((type) => type.id));

    expect(validateGraph(csharpReferenceFixture)).toEqual({ valid: true, errors: [] });
    expect([nodeIds.size, linkIds.size]).toEqual([csharpReferenceFixture.nodes.length, csharpReferenceFixture.links.length]);
    expect(csharpReferenceFixture.nodes.every((node) => nodeTypeIds.has(node.typeId))).toBe(true);
    expect(csharpReferenceFixture.links.every((link) => nodeIds.has(link.source) && nodeIds.has(link.target))).toBe(true);
    expect(csharpReferenceFixture.links
      .filter((link) => link.derivedFrom)
      .every((link) => (Array.isArray(link.derivedFrom) ? link.derivedFrom : [link.derivedFrom])
        .every((linkId) => linkIds.has(linkId)))).toBe(true);

    expect(csharpReferenceFixture.nodes.find((node) => node.id === "class-repository")).toMatchObject({
      typeId: "class",
      attributes: { partial: true }
    });
    expect(csharpReferenceFixture.nodes.find((node) => node.id === "file-generated-client")).toMatchObject({
      typeId: "generated-artifact",
      attributes: { generated: true }
    });
    expect(csharpReferenceFixture.nodes.filter((node) => node.label === "Get")).toHaveLength(3);
    expect(csharpReferenceFixture.nodes.find((node) => node.id === "method-repository-get-generic").attributes.genericArity).toBe(1);
    expect(csharpReferenceFixture.links.filter((link) => link.summary)).toHaveLength(6);
    expect(csharpReferenceFixture.links.find((link) => link.id === "summary-type-repository-order")).toMatchObject({
      typeId: "summary-depends-on",
      source: "class-repository",
      target: "record-order",
      derivedFrom: expect.arrayContaining(["repository-get-uses-order", "repository-get-returns-order"])
    });
    expect(csharpReferenceFixture.links.find((link) => link.id === "summary-class-calls")).toMatchObject({
      derivedFrom: ["client-load-calls-repository"],
      metrics: { occurrences: 4, relationshipWeight: 12 }
    });
    expect(csharpReferenceFixture.links.find((link) => link.id === "summary-project-calls")).toMatchObject({
      derivedFrom: ["client-load-calls-repository"],
      metrics: { occurrences: 4, relationshipWeight: 12 }
    });
  });
});

describe("C# reference fixture contract declarations", () => {
  it("declares C# profiles, metrics, and visual contract references", () => {
    expect(csharpReferenceFixture.viewProfiles.map((profile) => profile.id)).toEqual([
      "assembly-overview",
      "type-detail",
      "method-detail"
    ]);
    expect(csharpReferenceFixture.projections).toEqual(expect.arrayContaining([
      expect.objectContaining({ fromProfile: "assembly-overview", toProfile: "type-detail", linkTypeId: "summary-depends-on" }),
      expect.objectContaining({ fromProfile: "type-detail", toProfile: "method-detail", linkTypeId: "summary-calls" })
    ]));
    const visualTokenIds = new Set(Object.keys(csharpReferenceFixture.visualTokens));
    expect(csharpReferenceFixture.nodeTypes.every((type) => visualTokenIds.has(type.visualToken))).toBe(true);
    expect(csharpReferenceFixture.linkTypes.every((type) => visualTokenIds.has(type.visualToken))).toBe(true);
    expect(csharpReferenceFixture.linkTypes.map((type) => type.id)).toEqual(expect.arrayContaining([
      "project-reference",
      "references-assembly",
      "inherits",
      "implements",
      "overrides",
      "calls",
      "uses-type",
      "parameter-type",
      "returns-type"
    ]));
    expect(csharpReferenceFixture.metricDefinitions).toMatchObject({
      complexity: { unit: "branches", valueKind: "raw" },
      callCount: { unit: "calls" },
      dependencyStrength: { unit: "score", valueKind: "raw" },
      importance: { valueKind: "normalized-score", range: [0, 1] },
      footprint: { valueKind: "normalized-score", range: [0, 1] }
    });
    expect(csharpReferenceFixture.nodes.find((node) => node.id === "class-repository").metrics).toMatchObject({
      loc: 82,
      fileCount: 2,
      partialDeclarationCount: 2,
      importance: 0.8,
      footprint: 0.9
    });
    expect(csharpReferenceFixture.links.filter((link) => link.summary).every((link) => (
      link.metrics.occurrences > 0
      && link.metrics.relationshipWeight > 0
      && link.attributes.aggregation.origin === "detail-relations"
    ))).toBe(true);
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

describe("spatial layout definitions", () => {
  it("accepts the spatial fixture with declarative layout data", () => {
    expect(validateGraph(spatialFixture)).toEqual({ valid: true, errors: [] });
    expect(spatialFixture.nodes.map((node) => node.groupId)).toEqual(expect.arrayContaining(["north", "south"]));
    expect(spatialFixture.links.find((link) => link.summary)).toMatchObject({ summary: true });
    expect(spatialFixture.nodeTypes).toEqual(expect.arrayContaining([
      expect.objectContaining({ id: "group", visualRole: "container", baseSize: 2.4 }),
      expect.objectContaining({ id: "item", visualRole: "entity", baseSize: 1.2 })
    ]));
    expect(spatialFixture.viewProfiles).toEqual(expect.arrayContaining([
      expect.objectContaining({ layoutProfileId: "spatial-overview" }),
      expect.objectContaining({ layoutProfileId: "spatial-detail" })
    ]));
    expect(spatialFixture.layoutProfiles[0]).toMatchObject({ groupDistance: 48 });
    expect(spatialFixture.layoutProfiles[0].containmentDistances).toHaveLength(2);
  });

  it("rejects unknown layout profile references", () => {
    const graph = structuredClone(spatialFixture);
    graph.viewProfiles[0].layoutProfileId = "missing-layout";

    expect(validateGraph(graph).errors).toContainEqual({
      kind: "semantic",
      path: "/viewProfiles/0/layoutProfileId",
      message: "layout profile 'missing-layout' does not reference a defined layout profile."
    });
  });

  it("rejects unknown containment distance type references", () => {
    const graph = structuredClone(spatialFixture);
    graph.layoutProfiles[0].containmentDistances[0].childTypeId = "missing-type";

    expect(validateGraph(graph).errors).toContainEqual({
      kind: "semantic",
      path: "/layoutProfiles/0/containmentDistances/0/childTypeId",
      message: "node type 'missing-type' does not reference a defined node type."
    });
  });

  it("rejects non-positive layout distances", () => {
    const graph = structuredClone(spatialFixture);
    graph.layoutProfiles[0].containmentDistances[0].distance = 0;

    expect(validateGraph(graph).errors).toContainEqual({
      kind: "schema",
      path: "/layoutProfiles/0/containmentDistances/0/distance",
      message: "must be > 0"
    });
  });

  it("allows unknown optional layout fields", () => {
    const graph = structuredClone(spatialFixture);
    graph.layoutProfiles[0].futureLayoutHint = { packing: "radial" };
    graph.layoutProfiles[0].containmentDistances[0].futureDistanceMode = "soft";

    expect(validateGraph(graph)).toEqual({ valid: true, errors: [] });
  });
});

describe("nested universe reference fixture", () => {
  it("validates all generic type, link, containment, metric, and profile references", () => {
    const nodeIds = new Set(nestedUniverseFixture.nodes.map((node) => node.id));
    const nodeTypeIds = new Set(nestedUniverseFixture.nodeTypes.map((type) => type.id));
    const linkTypeIds = new Set(nestedUniverseFixture.linkTypes.map((type) => type.id));

    expect(validateGraph(nestedUniverseFixture)).toEqual({ valid: true, errors: [] });
    expect(nestedUniverseFixture.nodeTypes.map((type) => type.id)).toEqual([
      "galaxy",
      "system",
      "star",
      "planet",
      "moon"
    ]);
    expect(nestedUniverseFixture.nodes).toHaveLength(18);
    expect(nestedUniverseFixture.nodes.every((node) => nodeTypeIds.has(node.typeId))).toBe(true);
    expect(nestedUniverseFixture.links.every((link) => (
      linkTypeIds.has(link.typeId) && nodeIds.has(link.source) && nodeIds.has(link.target)
    ))).toBe(true);
    expect(nestedUniverseFixture.nodes.map((node) => node.groupId)).toEqual(expect.arrayContaining(["alpha", "beta"]));
    expect(nestedUniverseFixture.nodeTypes.every((type) => type.visualRole && type.baseSize > 0)).toBe(true);
    expect(Object.keys(nestedUniverseFixture.metricDefinitions)).toEqual([
      "mass",
      "complexity",
      "dependencyCount",
      "referenceStrength"
    ]);
    expect(nestedUniverseFixture.links.filter((link) => link.typeId === "references")).toEqual(
      expect.arrayContaining([expect.objectContaining({ weight: 0.8 })])
    );
    expect(nestedUniverseFixture.links.find((link) => link.id === "summary-galaxy-alpha-beta")).toMatchObject({
      summary: true,
      derivedFrom: ["references-planet-alpha-beta", "references-moon-alpha-beta"]
    });
  });

  it("declares all four containment levels in both layout profiles", () => {
    const expectedLevels = [
      ["galaxy", "system"],
      ["system", "star"],
      ["star", "planet"],
      ["planet", "moon"]
    ];

    expect(nestedUniverseFixture.containmentRules.map((rule) => [rule.parentTypeId, rule.childTypeId])).toEqual(expectedLevels);
    expect(nestedUniverseFixture.layoutProfiles).toHaveLength(2);
    nestedUniverseFixture.layoutProfiles.forEach((profile) => {
      expect(profile.containmentDistances).toHaveLength(4);
      expect(profile.containmentDistances.map((distance) => [distance.parentTypeId, distance.childTypeId])).toEqual(expectedLevels);
    });
    expect(nestedUniverseFixture.viewProfiles.map((profile) => profile.layoutProfileId)).toEqual([
      "nested-overview",
      "nested-detail"
    ]);
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

describe("graph-universe 1.0 definitions", () => {
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

describe("graph normalization", () => {
  it("rejects definition objects", () => {
    const graph = structuredClone(validGraph);
    graph.nodeTypes = { service: { label: "Service" } };

    expect(validateGraph(graph).valid).toBe(false);
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

  it("defaults missing layout profiles to an empty array", () => {
    const graph = structuredClone(validGraph);
    delete graph.layoutProfiles;

    expect(normalizeGraph(graph).layoutProfiles).toEqual([]);
  });
});
