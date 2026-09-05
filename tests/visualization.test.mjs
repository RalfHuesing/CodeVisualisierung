import { describe, expect, it } from "vitest";
import sampleGraph from "../contracts/graph-universe/fixtures/minimal.json" with { type: "json" };
import { normalizeGraph } from "../apps/viewer/src/domain/graph.js";
import { createGraphLayout, findNodeMetric, scaleValue } from "../apps/viewer/src/visualization.js";

describe("graph visualization calculations", () => {
  it("uses the first available node metric and stable positions", () => {
    const graph = normalizeGraph(sampleGraph);
    const layout = createGraphLayout(graph);

    expect(findNodeMetric(graph)).toBe("importance");
    expect(layout.nodes.map(({ id, x, y }) => ({ id, x: Math.round(x), y: Math.round(y) }))).toEqual([
      { id: "api", x: 450, y: 90 },
      { id: "orders", x: 762, y: 345 },
      { id: "database", x: 138, y: 345 }
    ]);
    expect(layout.links.every((link) => link.sourceNode && link.targetNode)).toBe(true);
  });

  it("scales values into the configured output range", () => {
    expect(scaleValue(5, [0, 10], 2, 12)).toBe(7);
    expect(scaleValue(5, [5, 5], 2, 12)).toBe(7);
    expect(scaleValue(undefined, [0, 10], 2, 12)).toBe(7);
  });
});
