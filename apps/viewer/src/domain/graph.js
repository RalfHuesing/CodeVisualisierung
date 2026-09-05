import Ajv from "ajv/dist/2020.js";
import addFormats from "ajv-formats";
import graphSchema from "../../../../contracts/graph-universe/schema/graph-universe.schema.json";

const schemaValidator = createSchemaValidator();

export const GRAPH_DEFAULTS = Object.freeze({
  linkKind: "related-to",
  nodeKind: "node"
});

export function validateGraph(graph) {
  const schemaIsValid = schemaValidator(graph);
  const errors = createSchemaErrors(schemaValidator.errors ?? []);

  if (schemaIsValid) {
    errors.push(...createSemanticErrors(graph));
  }

  return {
    valid: errors.length === 0,
    errors
  };
}

export function parseGraphText(text) {
  let graph;

  try {
    graph = JSON.parse(text);
  } catch {
    return {
      errors: [{ kind: "json", path: "$", message: "Die Datei enthält kein gültiges JSON." }],
      graph: null,
      valid: false
    };
  }

  const validation = validateGraph(graph);
  if (!validation.valid) {
    return {
      errors: validation.errors,
      graph: null,
      valid: false
    };
  }

  return {
    errors: [],
    graph: normalizeGraph(graph),
    valid: true
  };
}

export function normalizeGraph(graph) {
  return {
    ...graph,
    nodes: graph.nodes.map((node) => ({
      ...node,
      attributes: { ...(node.attributes ?? {}) },
      groupId: node.groupId ?? null,
      kind: node.kind ?? GRAPH_DEFAULTS.nodeKind,
      label: node.label ?? node.id,
      metrics: { ...(node.metrics ?? {}) },
      tags: [...(node.tags ?? [])]
    })),
    links: graph.links.map((link) => ({
      ...link,
      attributes: { ...(link.attributes ?? {}) },
      directed: link.directed ?? true,
      kind: link.kind ?? GRAPH_DEFAULTS.linkKind,
      metrics: { ...(link.metrics ?? {}) }
    }))
  };
}

function createSchemaValidator() {
  const ajv = new Ajv({ allErrors: true, strict: true });
  addFormats(ajv);
  return ajv.compile(graphSchema);
}

function createSchemaErrors(schemaErrors = []) {
  return schemaErrors.map((error) => ({
    kind: "schema",
    path: error.instancePath || "$",
    message: error.message || "Schema validation failed."
  }));
}

function createSemanticErrors(graph) {
  const errors = [];
  const nodeIds = collectNodeIds(graph.nodes, errors);

  validateNodeMetrics(graph.nodes, errors);
  validateLinks(graph.links, nodeIds, errors);

  return errors;
}

function collectNodeIds(nodes, errors) {
  const nodeIds = new Set();

  nodes.forEach((node, index) => {
    if (typeof node.id !== "string" || node.id.length === 0) {
      return;
    }

    if (nodeIds.has(node.id)) {
      errors.push({
        kind: "semantic",
        path: `/nodes/${index}/id`,
        message: `Node ID '${node.id}' is duplicated.`
      });
      return;
    }

    nodeIds.add(node.id);
  });

  return nodeIds;
}

function validateNodeMetrics(nodes, errors) {
  nodes.forEach((node, nodeIndex) => {
    validateMetricObject(node.metrics, `/nodes/${nodeIndex}/metrics`, errors);
  });
}

function validateLinks(links, nodeIds, errors) {
  links.forEach((link, linkIndex) => {
    const sourcePath = `/links/${linkIndex}/source`;
    const targetPath = `/links/${linkIndex}/target`;

    if (!nodeIds.has(link.source)) {
      errors.push({
        kind: "semantic",
        path: sourcePath,
        message: `Link source '${link.source}' does not reference a node.`
      });
    }

    if (!nodeIds.has(link.target)) {
      errors.push({
        kind: "semantic",
        path: targetPath,
        message: `Link target '${link.target}' does not reference a node.`
      });
    }

    if (link.weight !== undefined && !Number.isFinite(link.weight)) {
      errors.push({
        kind: "semantic",
        path: `/links/${linkIndex}/weight`,
        message: "Link weight must be a finite number."
      });
    }

    validateMetricObject(link.metrics, `/links/${linkIndex}/metrics`, errors);
  });
}

function validateMetricObject(metrics, basePath, errors) {
  if (metrics === undefined) {
    return;
  }

  Object.entries(metrics).forEach(([name, value]) => {
    if (!Number.isFinite(value)) {
      errors.push({
        kind: "semantic",
        path: `${basePath}/${name}`,
        message: "Metric must be a finite number."
      });
    }
  });
}
