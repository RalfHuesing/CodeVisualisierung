import fs from "node:fs";
import path from "node:path";
import { createVisualGraphData, filterGraph } from "../apps/viewer/src/rendering/graph-mapping.js";

const fixtureDirectory = path.resolve("contracts/graph-universe/fixtures");
const fixtureNames = ["small", "medium", "large", "performance"];
const repetitions = 20;
const budgetMsPerRun = 100;

for (const fixtureName of fixtureNames) {
  const graph = JSON.parse(fs.readFileSync(path.join(fixtureDirectory, `${fixtureName}.json`), "utf8"));
  const start = process.hrtime.bigint();
  let result;
  for (let repetition = 0; repetition < repetitions; repetition += 1) {
    result = createVisualGraphData(filterGraph(graph, { hiddenKinds: repetition % 2 === 0 ? ["method"] : [] }));
  }
  const elapsedMs = Number(process.hrtime.bigint() - start) / 1_000_000;
  const perRunMs = elapsedMs / repetitions;
  console.log(`${fixtureName}: ${graph.nodes.length} nodes, ${graph.links.length} links, ${perRunMs.toFixed(2)} ms/run, ${result.nodes.length} visible`);
  if (perRunMs > budgetMsPerRun) {
    process.exitCode = 1;
  }
}
