import { describe, expect, it } from "vitest";
import sampleGraph from "../contracts/graph-universe/fixtures/minimal.json" with { type: "json" };
import edgeCasesGraph from "../contracts/graph-universe/fixtures/edge-cases.json" with { type: "json" };
import spatialGraph from "../contracts/graph-universe/fixtures/spatial.json" with { type: "json" };
import nestedUniverseGraph from "../contracts/graph-universe/fixtures/nested-universe.json" with { type: "json" };
import { normalizeGraph } from "../apps/viewer/src/domain/graph.js";
import {
  createVisualGraphData,
  filterGraph,
  findLinkMetric,
  findNodeMetric,
  findSearchMatches,
  getDefaultViewProfile,
  getFilterOptions,
  getLinkVisualStyle,
  getNodeNeighborhood,
  getNodeVisualStyle,
  scaleValue
} from "../apps/viewer/src/rendering/graph-mapping.js";
import {
  createDeterministicPositions,
  getActiveLayoutProfile,
  getLayoutGroup,
  getLinkDistance
} from "../apps/viewer/src/rendering/layout.js";
import { isWebGLSupported } from "../apps/viewer/src/rendering/webgl.js";

describe("graph visualization calculations", () => {
  it("uses the first available node metric for visual size", () => {
    const graph = normalizeGraph(sampleGraph);
    const visualData = createVisualGraphData(graph);

    expect(findNodeMetric(graph)).toBe("importance");
    expect(visualData.nodes.map(({ id, visualValue }) => ({ id, visualValue: Number(visualValue.toFixed(2)) }))).toEqual([
      { id: "api", visualValue: 8 },
      { id: "orders", visualValue: 5 },
      { id: "database", visualValue: 2 }
    ]);
    expect(visualData.links.every((link) => link.source && link.target)).toBe(true);
    expect(findLinkMetric(graph)).toBe("weight");
  });

  it("supports explicit node and link metrics", () => {
    const graph = normalizeGraph(sampleGraph);
    const visualData = createVisualGraphData(graph, { linkMetric: "callCount", nodeMetric: "importance" });

    expect(visualData.nodeMetric).toBe("importance");
    expect(visualData.linkMetric).toBe("callCount");
    expect(visualData.links.map((link) => link.visualWidth)).toEqual([0.8, 0.2]);
  });

  it("creates filters and profiles from graph declarations", () => {
    const graph = normalizeGraph(sampleGraph);
    const detailProfile = graph.viewProfiles[1];

    expect(getFilterOptions(graph).sources.map(({ id, field, values }) => ({ id, field, values }))).toEqual([
      { id: "node-type", field: "typeId", values: ["service", "storage"] },
      { id: "group", field: "groupId", values: ["core", "data", "web"] },
      { id: "tag", field: "tags", values: ["entrypoint"] }
    ]);
    expect(filterGraph(graph, { "node-type": "storage" }).nodes.map((node) => node.id)).toEqual(["database"]);
    expect(getFilterOptions(graph)).toMatchObject({ groups: ["core", "data", "web"], kinds: ["service", "storage"] });
    expect(filterGraph(graph, {}, graph.viewProfiles[0]).nodes).toHaveLength(3);
    expect(filterGraph(graph, {}, detailProfile).links.map((link) => link.id)).toEqual([
      "summary-api-orders",
      "depends-orders-database"
    ]);
  });

  it("shows only declared projections for a profile and keeps existing summary links", () => {
    const graph = normalizeGraph(sampleGraph);
    const detailProfile = graph.viewProfiles[1];
    const withoutProjection = { ...graph, projections: [] };
    const withUnknownProjection = {
      ...graph,
      projections: [{ id: "unknown-path", fromProfile: "overview", toProfile: "detail", linkTypeId: "unknown" }]
    };

    expect(filterGraph(withoutProjection, {}, detailProfile).links.map((link) => link.id)).toEqual([
      "depends-orders-database"
    ]);
    expect(filterGraph(graph, {}, detailProfile).links.map((link) => link.id)).toContain("summary-api-orders");
    expect(filterGraph(withUnknownProjection, {}, detailProfile).links.map((link) => link.id)).toEqual([
      "depends-orders-database"
    ]);
  });

  it("resolves nested facet fields", () => {
    const graph = normalizeGraph(structuredClone(sampleGraph));
    graph.nodes[0].attributes.team = "platform";
    graph.nodes[1].attributes.team = "orders";
    graph.facets.push({ id: "team", label: "Team", source: { scope: "node", field: "attributes.team" } });
    graph.filterSources.push({ id: "team", facetId: "team", label: "Team" });

    expect(getFilterOptions(graph).sources.find((source) => source.id === "team").values).toEqual(["orders", "platform"]);
    expect(filterGraph(graph, { team: "platform" }).nodes.map((node) => node.id)).toEqual(["api"]);
  });

  it("uses the theme fallback for unknown visual tokens", () => {
    const graph = normalizeGraph(structuredClone(sampleGraph));
    graph.nodeTypes = graph.nodeTypes.map((type) => type.id === "service" ? { ...type, visualToken: "missing" } : type);
    graph.linkTypes = graph.linkTypes.map((type) => type.id === "depends-on" ? { ...type, visualToken: "missing" } : type);

    expect(getNodeVisualStyle(graph.nodes[0], graph)).toMatchObject({
      color: "#38bdf8",
      shape: "sphere",
      tokenId: "node",
      usedFallback: true
    });
    expect(getLinkVisualStyle(graph.links[1], graph)).toMatchObject({
      color: "#38bdf8",
      tokenId: "node",
      usedFallback: true
    });
  });

});

describe("spatial visualization calculations", () => {

  it("applies type base sizes and keeps visual roles in prepared nodes", () => {
    const graph = normalizeGraph(spatialGraph);
    const visualData = createVisualGraphData(graph, { profile: graph.viewProfiles[0] });

    expect(visualData.nodes.find((node) => node.id === "group-north")).toMatchObject({
      baseSize: 2.4,
      visualRole: "container",
      visualValue: 8 * 2.4
    });
    expect(visualData.nodes.find((node) => node.id === "item-a")).toMatchObject({
      baseSize: 1.2,
      visualRole: "entity"
    });
  });

  it("falls back deterministically for missing and constant metrics", () => {
    const graph = normalizeGraph(structuredClone(spatialGraph));
    graph.nodes.forEach((node) => delete node.metrics.importance);
    const missingMetric = createVisualGraphData(graph);
    graph.nodes.forEach((node) => { node.metrics.importance = 1; });
    const constantMetric = createVisualGraphData(graph);

    expect(missingMetric.nodes.map((node) => node.visualValue)).toEqual(constantMetric.nodes.map((node) => node.visualValue));
    expect(missingMetric.nodes.every((node) => Number.isFinite(node.x) && Number.isFinite(node.y) && Number.isFinite(node.z))).toBe(true);
  });

  it("keeps normalized scores on their declared range after filtering", () => {
    const graph = normalizeGraph({
      ...structuredClone(spatialGraph),
      metricDefinitions: { importance: { valueKind: "normalized-score", range: [0, 1] } }
    });
    const full = createVisualGraphData(graph, { nodeMetric: "importance" });
    const visible = createVisualGraphData(filterGraph(graph, { group: "north" }), { nodeMetric: "importance" });
    const fullValues = new Map(full.nodes.map((node) => [node.id, node.visualValue]));

    expect(visible.nodes.map((node) => [node.id, node.visualValue])).toEqual(
      visible.nodes.map((node) => [node.id, fullValues.get(node.id)])
    );
  });

  it("selects layout profiles with first-profile fallback", () => {
    const graph = normalizeGraph(spatialGraph);

    expect(getActiveLayoutProfile(graph, graph.viewProfiles[1]).id).toBe("spatial-detail");
    expect(getActiveLayoutProfile(graph, { layoutProfileId: "missing" }).id).toBe("spatial-overview");
    expect(getActiveLayoutProfile(graph, null).id).toBe("spatial-overview");
  });

  it("defaults to the overview profile instead of the most detailed profile", () => {
    const graph = normalizeGraph(nestedUniverseGraph);

    expect(getDefaultViewProfile(graph).id).toBe("nested-overview");
  });

  it("uses containment, default, and cross-group link distances", () => {
    const graph = normalizeGraph({ ...structuredClone(spatialGraph), hierarchy: { containmentLinkTypes: ["contains"] } });
    const profile = getActiveLayoutProfile(graph, graph.viewProfiles[0]);
    const nodeMap = new Map(graph.nodes.map((node) => [node.id, node]));
    const defaultLink = { source: "item-a", target: "item-b", typeId: "relates" };
    const crossGroupContainment = structuredClone(graph);
    crossGroupContainment.nodes.find((node) => node.id === "item-a").groupId = "south";
    const crossGroupNodeMap = new Map(crossGroupContainment.nodes.map((node) => [node.id, node]));

    expect(getLinkDistance(graph.links[0], graph, profile, nodeMap)).toBe(20);
    expect(getLinkDistance(defaultLink, graph, profile, nodeMap)).toBe(26);
    expect(getLinkDistance(graph.links[7], graph, profile, nodeMap)).toBe(48);
    expect(getLinkDistance(crossGroupContainment.links[0], crossGroupContainment, profile, crossGroupNodeMap)).toBe(20);
  });

  it("creates stable positions with separated groups and nearby containment", () => {
    const graph = normalizeGraph({ ...structuredClone(spatialGraph), hierarchy: { containmentLinkTypes: ["contains"] } });
    const profile = getActiveLayoutProfile(graph, graph.viewProfiles[0]);
    const first = createDeterministicPositions(graph, profile);
    const second = createDeterministicPositions(graph, profile);
    const parent = first.get("group-north");
    const child = first.get("item-a");
    const nestedChild = first.get("detail-a");
    const otherGroup = first.get("group-south");
    const distance = (left, right) => Math.hypot(left.x - right.x, left.y - right.y, left.z - right.z);

    expect([...first.entries()]).toEqual([...second.entries()]);
    expect(distance(parent, child)).toBeLessThan(distance(parent, otherGroup));
    expect(distance(child, nestedChild)).toBe(12);
    expect(Math.abs(parent.x - otherGroup.x)).toBe(48);
    expect(getLayoutGroup(graph.nodes[0], "attributes.team")).toBe("north");
  });
});

describe("nested universe visualization calculations", () => {
  it("uses all four declarative containment distances and separates groups", () => {
    const graph = normalizeGraph(nestedUniverseGraph);
    const profile = graph.layoutProfiles.find((item) => item.id === "nested-detail");
    const nodeMap = new Map(graph.nodes.map((node) => [node.id, node]));
    const distances = [
      ["contains-galaxy-alpha-system", 110],
      ["contains-system-alpha-star", 78],
      ["contains-star-alpha-inner", 48],
      ["contains-inner-alpha-moon-a", 24]
    ];

    distances.forEach(([linkId, expectedDistance]) => {
      const link = graph.links.find((item) => item.id === linkId);
      expect(getLinkDistance(link, graph, profile, nodeMap)).toBe(expectedDistance);
    });

    const positions = createDeterministicPositions(graph, profile);
    const distance = (left, right) => Math.hypot(left.x - right.x, left.y - right.y, left.z - right.z);
    expect(Math.abs(positions.get("galaxy-alpha").x - positions.get("galaxy-beta").x)).toBe(180);
    expect(distance(positions.get("galaxy-alpha"), positions.get("system-alpha"))).toBeCloseTo(110);
    expect(distance(positions.get("system-alpha"), positions.get("star-alpha"))).toBeCloseTo(78);
    expect(distance(positions.get("star-alpha"), positions.get("planet-alpha-inner"))).toBeCloseTo(48);
    expect(distance(positions.get("planet-alpha-inner"), positions.get("moon-alpha-inner-a"))).toBeCloseTo(24);
    expect(getLinkDistance(
      graph.links.find((link) => link.id === "summary-galaxy-alpha-beta"),
      graph,
      profile,
      nodeMap
    )).toBe(180);
  });

  it("maps named metrics and weighted reference links from the detail profile", () => {
    const graph = normalizeGraph(nestedUniverseGraph);
    const profile = graph.viewProfiles.find((item) => item.id === "nested-detail");
    const visualData = createVisualGraphData(graph, {
      linkMetric: profile.linkMetric,
      nodeMetric: profile.nodeMetric,
      profile
    });
    const strongReference = visualData.links.find((link) => link.id === "references-planet-alpha-beta");
    const weakReference = visualData.links.find((link) => link.id === "references-moon-alpha-beta");

    expect(visualData.nodeMetric).toBe("complexity");
    expect(visualData.linkMetric).toBe("referenceStrength");
    expect(visualData.nodes.find((node) => node.id === "galaxy-alpha")).toMatchObject({
      visualRole: "universe-container",
      baseSize: 4
    });
    expect(strongReference.weight).toBe(0.8);
    expect(strongReference.visualWidth).toBeGreaterThan(weakReference.visualWidth);
    expect(findLinkMetric(graph)).toBe("weight");
  });
});

describe("graph interaction calculations", () => {
  it("scales values into the configured output range", () => {
    expect(scaleValue(5, [0, 10], 2, 12)).toBe(7);
    expect(scaleValue(5, [5, 5], 2, 12)).toBe(7);
    expect(scaleValue(undefined, [0, 10], 2, 12)).toBe(7);
  });

  it("separates incoming and outgoing neighbors", () => {
    const graph = normalizeGraph(sampleGraph);
    const neighborhood = getNodeNeighborhood(graph, "orders");

    expect(neighborhood.incomingLinks.map((link) => link.source)).toEqual(["api"]);
    expect(neighborhood.outgoingLinks.map((link) => link.target)).toEqual(["database"]);
    expect(neighborhood.undirectedLinks).toHaveLength(0);
    expect([...neighborhood.neighborIds]).toEqual(["api", "database"]);
  });

  it("finds nodes by id or label", () => {
    const graph = normalizeGraph(sampleGraph);

    expect([...findSearchMatches(graph, "ord")]).toEqual(["orders"]);
    expect([...findSearchMatches(graph, "missing")]).toEqual([]);
  });

  it("detects supported and unavailable browser graphics contexts", () => {
    const supportedDocument = { createElement: () => ({ getContext: (name) => name === "webgl2" ? {} : null }) };
    const unsupportedDocument = { createElement: () => ({ getContext: () => null }) };

    expect(isWebGLSupported(supportedDocument)).toBe(true);
    expect(isWebGLSupported(unsupportedDocument)).toBe(false);
  });

  it("keeps isolated nodes and parallel links visible in mapping", () => {
    const graph = normalizeGraph(edgeCasesGraph);
    const visualData = createVisualGraphData(graph);

    expect(visualData.nodes).toHaveLength(3);
    expect(visualData.links).toHaveLength(2);
    expect(visualData.nodes.find((node) => node.id === "isolated").visualValue).toBe(5);
  });
});
