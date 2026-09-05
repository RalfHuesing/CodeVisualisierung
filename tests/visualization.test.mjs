import { describe, expect, it } from "vitest";
import sampleGraph from "../contracts/graph-universe/fixtures/minimal.json" with { type: "json" };
import { normalizeGraph } from "../apps/viewer/src/domain/graph.js";
import { createVisualGraphData, findNodeMetric, findSearchMatches, getNodeNeighborhood, scaleValue } from "../apps/viewer/src/rendering/graph-mapping.js";

describe("graph visualization calculations", () => {
  it("uses the first available node metric for visual size", () => {
    const graph = normalizeGraph(sampleGraph);
    const visualData = createVisualGraphData(graph);

    expect(findNodeMetric(graph)).toBe("importance");
    expect(visualData.nodes.map(({ id, visualValue }) => ({ id, visualValue: Number(visualValue.toFixed(2)) }))).toEqual([
      { id: "api", visualValue: 8 },
      { id: "orders", visualValue: 4.5 },
      { id: "database", visualValue: 1 }
    ]);
    expect(visualData.links.every((link) => link.source && link.target)).toBe(true);
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
});
