import fs from "node:fs";
import path from "node:path";

const fixtureDirectory = path.resolve("contracts/graph-universe/fixtures");
const metricDefinitions = {
  complexity: { label: "Komplexität", unit: "score", valueType: "number" },
  fanIn: { label: "Eingehende Beziehungen", unit: "links", valueType: "number" },
  lines: { label: "Codezeilen", unit: "lines", valueType: "number" },
  callCount: { label: "Aufrufe", unit: "calls", valueType: "number" },
  dependencyWeight: { label: "Abhängigkeitsstärke", unit: "score", valueType: "number" }
};

function createGraph(title, description, namespaceCount, classesPerNamespace, methodsPerClass) {
  const nodes = [];
  const links = [];
  const namespaces = [];
  const classes = [];
  const methods = [];

  for (let namespaceIndex = 0; namespaceIndex < namespaceCount; namespaceIndex += 1) {
    const namespaceId = `ns-${namespaceIndex + 1}`;
    namespaces.push(namespaceId);
    nodes.push(createNode(namespaceId, `Namespace ${namespaceIndex + 1}`, "namespace", namespaceId, ["boundary"]));
    for (let classIndex = 0; classIndex < classesPerNamespace; classIndex += 1) {
      const fileId = `${namespaceId}-file-${classIndex + 1}`;
      nodes.push(createNode(fileId, `Component ${namespaceIndex + 1}.${classIndex + 1}.js`, "file", namespaceId, ["source"]));
      links.push(createLink(`contains-${namespaceId}-file-${classIndex}`, namespaceId, fileId, "contains", false, 1));
      const classId = `${namespaceId}-class-${classIndex + 1}`;
      classes.push(classId);
      nodes.push(createNode(classId, `Component ${namespaceIndex + 1}.${classIndex + 1}`, "class", namespaceId, ["type:component"]));
      links.push(createLink(`contains-${fileId}`, fileId, classId, "contains", false, 1));
      for (let methodIndex = 0; methodIndex < methodsPerClass; methodIndex += 1) {
        const methodId = `${classId}-method-${methodIndex + 1}`;
        methods.push(methodId);
        nodes.push(createNode(methodId, `execute${methodIndex + 1}()`, "method", namespaceId, ["executable"]));
        links.push(createLink(`contains-${classId}-${methodIndex}`, classId, methodId, "contains", false, 1));
      }
    }
    const nextNamespaceId = `ns-${((namespaceIndex + 1) % namespaceCount) + 1}`;
    links.push(createLink(`namespace-flow-${namespaceIndex}`, namespaceId, nextNamespaceId, "depends-on", true, 0.5));
  }

  methods.forEach((methodId, index) => {
    const target = methods[(index + 3) % methods.length];
    if (methodId !== target) {
      links.push(createLink(`call-${index}`, methodId, target, "calls", true, 0.25 + (index % 5) / 10));
    }
  });
  classes.forEach((classId, index) => {
    const target = classes[(index + 1) % classes.length];
    if (classId !== target) {
      links.push(createLink(`dependency-${index}`, classId, target, "depends-on", true, 0.4 + (index % 4) / 10));
    }
  });

  return {
    format: { name: "graph-universe", version: "0.1" },
    meta: {
      title,
      description,
      createdAt: "2026-09-05T12:00:00Z",
      source: { name: "deterministic-fixture-generator", version: "1.0.0" }
    },
    metricDefinitions,
    nodes,
    links
  };
}

function createNode(id, label, kind, groupId, tags) {
  const index = id.split("-").reduce((total, part) => total + part.length, 0);
  return {
    id,
    label,
    kind,
    groupId,
    tags,
    metrics: {
      complexity: 1 + (index % 10),
      fanIn: 2 + (index % 7),
      lines: 20 + (index * 13) % 240
    },
    attributes: { visibility: index % 3 === 0 ? "public" : "internal" }
  };
}

function createLink(id, source, target, kind, directed, weight) {
  return {
    id,
    source,
    target,
    kind,
    directed,
    weight,
    metrics: { callCount: directed ? 10 + id.length : 0, dependencyWeight: weight }
  };
}

function writeFixture(name, graph) {
  const filePath = path.join(fixtureDirectory, `${name}.json`);
  fs.writeFileSync(filePath, `${JSON.stringify(graph, null, 2)}\n`);
}

fs.mkdirSync(fixtureDirectory, { recursive: true });
writeFixture("small", createGraph("Small code graph", "A compact reference graph with namespaces, components and methods.", 2, 2, 2));
writeFixture("medium", createGraph("Medium service graph", "A multi-area graph with directed calls and dependencies.", 4, 3, 3));
writeFixture("large", createGraph("Large application graph", "A realistic larger graph with named metrics, tags and grouped code elements.", 8, 5, 4));
writeFixture("performance", createGraph("Performance graph", "A deterministic load fixture for renderer and filtering measurements.", 12, 8, 5));
writeFixture("invalid", {
  format: { name: "graph-universe", version: "0.1" },
  nodes: [{ id: "duplicate" }, { id: "duplicate" }],
  links: [{ source: "duplicate", target: "missing" }]
});
