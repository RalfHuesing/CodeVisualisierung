import companyGraph from "../../../../contracts/graph-universe/fixtures/company.json";
import familyGraph from "../../../../contracts/graph-universe/fixtures/family.json";
import largeGraph from "../../../../contracts/graph-universe/fixtures/large.json";
import mediumGraph from "../../../../contracts/graph-universe/fixtures/medium.json";
import minimalGraph from "../../../../contracts/graph-universe/fixtures/minimal.json";
import performanceGraph from "../../../../contracts/graph-universe/fixtures/performance.json";
import smallGraph from "../../../../contracts/graph-universe/fixtures/small.json";

export const EXAMPLE_CATALOG = Object.freeze([
  createExample("minimal", "Minimal", "3 Nodes · 2 Links", minimalGraph),
  createExample("small", "Klein", "18 Nodes · 30 Links", smallGraph),
  createExample("medium", "Mittel", "64 Nodes · 112 Links", mediumGraph),
  createExample("large", "Groß", "248 Nodes · 448 Links", largeGraph),
  createExample("performance", "Belastung", "684 Nodes · 1.260 Links", performanceGraph),
  createExample("family", "Familienstammbaum", "7 Nodes · 20 Links", familyGraph),
  createExample("company", "Firmengeflecht", "6 Nodes · 11 Links", companyGraph)
]);

export function getExampleGraph(exampleId) {
  return EXAMPLE_CATALOG.find((example) => example.id === exampleId)?.graph ?? null;
}

function createExample(id, label, summary, graph) {
  return Object.freeze({ id, label, summary, graph });
}
