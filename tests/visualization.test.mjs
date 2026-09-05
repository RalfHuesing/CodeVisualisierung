import { describe, expect, it } from "vitest";
import sampleGraph from "../contracts/graph-universe/fixtures/minimal.json" with { type: "json" };
import edgeCasesGraph from "../contracts/graph-universe/fixtures/edge-cases.json" with { type: "json" };
import { normalizeGraph } from "../apps/viewer/src/domain/graph.js";
import {
  createVisualGraphData,
  filterGraph,
  findLinkMetric,
  findNodeMetric,
  findSearchMatches,
  getFilterOptions,
  getNodeNeighborhood,
  scaleValue
} from "../apps/viewer/src/rendering/graph-mapping.js";
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

  it("filters nodes and keeps only links between visible nodes", () => {
    const graph = normalizeGraph(sampleGraph);

    expect(filterGraph(graph, { kind: "storage" }).nodes.map((node) => node.id)).toEqual(["database"]);
    expect(filterGraph(graph, { linkKind: "depends-on" }).links).toHaveLength(2);
    expect(getFilterOptions(graph)).toMatchObject({ groups: ["core", "data", "web"], kinds: ["service", "storage"] });
    expect(filterGraph(graph, { hiddenKinds: ["service"] }).nodes.map((node) => node.id)).toEqual(["database"]);
  });

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
