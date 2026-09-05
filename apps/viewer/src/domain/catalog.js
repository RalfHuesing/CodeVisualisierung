import largeGraph from "../../../../contracts/graph-universe/fixtures/large.json";
import mediumGraph from "../../../../contracts/graph-universe/fixtures/medium.json";
import minimalGraph from "../../../../contracts/graph-universe/fixtures/minimal.json";
import performanceGraph from "../../../../contracts/graph-universe/fixtures/performance.json";
import smallGraph from "../../../../contracts/graph-universe/fixtures/small.json";

export const EXAMPLE_CATALOG = Object.freeze([
  createExample("minimal", "Minimal", "3 Nodes · 2 Links", minimalGraph),
  createExample("small", "Klein", "14 Nodes · 26 Links", smallGraph),
  createExample("medium", "Mittel", "52 Nodes · 100 Links", mediumGraph),
  createExample("large", "Groß", "208 Nodes · 408 Links", largeGraph),
  createExample("performance", "Belastung", "588 Nodes · 1.164 Links", performanceGraph)
]);

export function getExampleGraph(exampleId) {
  return EXAMPLE_CATALOG.find((example) => example.id === exampleId)?.graph ?? null;
}

function createExample(id, label, summary, graph) {
  return Object.freeze({ id, label, summary, graph });
}
