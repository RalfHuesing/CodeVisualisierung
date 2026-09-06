import Ajv from "ajv/dist/2020.js";
import addFormats from "ajv-formats";
import graphSchema from "../../../../contracts/graph-universe/schema/graph-universe.schema.json";

const schemaValidator = createSchemaValidator();

export const GRAPH_DEFAULTS = Object.freeze({
  linkKind: "related-to",
  nodeKind: "node"
});

export function resolveVisualToken(graph, tokenId) {
  const tokens = graph.visualTokens ?? {};
  const requestedId = tokenId ?? graph.theme?.fallbackToken;
  const fallbackId = graph.theme?.fallbackToken;
  const selectedId = tokens[requestedId]
    ? requestedId
    : tokens[fallbackId]
      ? fallbackId
      : Object.keys(tokens)[0] ?? null;

  return {
    id: selectedId,
    token: selectedId === null ? null : tokens[selectedId],
    usedFallback: selectedId !== requestedId
  };
}

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
    facets: graph.facets ?? [],
    filterSources: graph.filterSources ?? [],
    viewProfiles: graph.viewProfiles ?? [],
    projections: graph.projections ?? [],
    containmentRules: graph.containmentRules ?? [],
    hierarchy: graph.hierarchy ?? { containmentLinkTypes: [], acyclic: true },
    visualTokens: graph.visualTokens ?? {},
    theme: graph.theme ?? {},
    nodes: graph.nodes.map((node) => ({
      ...node,
      attributes: { ...(node.attributes ?? {}) },
      groupId: node.groupId ?? null,
      kind: node.kind ?? GRAPH_DEFAULTS.nodeKind,
      label: node.label ?? node.id,
      metrics: { ...(node.metrics ?? {}) },
      tags: [...(node.tags ?? [])],
      typeId: node.typeId ?? node.kind ?? GRAPH_DEFAULTS.nodeKind
    })),
    links: graph.links.map((link) => ({
      ...link,
      attributes: { ...(link.attributes ?? {}) },
      directed: link.directed ?? true,
      kind: link.kind ?? GRAPH_DEFAULTS.linkKind,
      metrics: { ...(link.metrics ?? {}) },
      typeId: link.typeId ?? link.kind ?? GRAPH_DEFAULTS.linkKind
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
  const nodeTypeIds = validateDefinitions(graph.nodeTypes, "nodeTypes", errors);
  const linkTypeIds = validateDefinitions(graph.linkTypes, "linkTypes", errors);

  validateNodeMetrics(graph.nodes, errors);
  validateNodeTypeReferences(graph.nodes, nodeTypeIds, errors);
  validateDefinitionReferences(graph, nodeTypeIds, linkTypeIds, errors);
  validateLinks(graph.links, nodeIds, linkTypeIds, errors);

  return errors;
}

function validateDefinitions(definitions, name, errors) {
  if (definitions === undefined) {
    return null;
  }

  const ids = new Set();
  const entries = Array.isArray(definitions)
    ? definitions.map((definition, index) => [definition?.id, index])
    : Object.keys(definitions).map((id, index) => [id, index]);

  entries.forEach(([id, index]) => {
    if (typeof id !== "string" || id.length === 0) {
      return;
    }

    if (ids.has(id)) {
      errors.push({
        kind: "semantic",
        path: `/${name}/${index}/id`,
        message: `${name} ID '${id}' is duplicated.`
      });
      return;
    }

    ids.add(id);
  });

  return ids;
}

function validateNodeTypeReferences(nodes, nodeTypeIds, errors) {
  if (nodeTypeIds === null) {
    return;
  }

  nodes.forEach((node, index) => {
    const typeId = node.typeId ?? node.kind;
    if (typeId === undefined || !nodeTypeIds.has(typeId)) {
      errors.push({
        kind: "semantic",
        path: `/nodes/${index}/typeId`,
        message: `Node type '${typeId}' does not reference a node type.`
      });
    }
  });
}

function validateDefinitionReferences(graph, nodeTypeIds, linkTypeIds, errors) {
  const facetIds = validateDefinitions(graph.facets, "facets", errors);
  const profileIds = validateDefinitions(graph.viewProfiles, "viewProfiles", errors);
  const metricIds = graph.metricDefinitions === undefined
    ? null
    : new Set(Object.keys(graph.metricDefinitions));
  validateFacetReferences(graph, facetIds, errors);
  validateProfileReferences(graph, profileIds, nodeTypeIds, linkTypeIds, metricIds, errors);
  validateProjectionReferences(graph, profileIds, linkTypeIds, errors);
  validateContainmentReferences(graph, nodeTypeIds, linkTypeIds, errors);
  validateHierarchyReferences(graph, linkTypeIds, errors);
}

function validateLinks(links, nodeIds, linkTypeIds, errors) {
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

    const typeId = link.typeId ?? link.kind;
    if (linkTypeIds !== null && (typeId === undefined || !linkTypeIds.has(typeId))) {
      errors.push({
        kind: "semantic",
        path: `/links/${linkIndex}/typeId`,
        message: `Link type '${typeId}' does not reference a link type.`
      });
    }

    validateDerivedLinks(link.derivedFrom, links, linkIndex, errors);

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

function validateFacetReferences(graph, facetIds, errors) {
  if (facetIds === null || graph.filterSources === undefined) {
    return;
  }

  graph.filterSources.forEach((source, index) => {
    if (!facetIds.has(source.facetId)) {
      errors.push({
        kind: "semantic",
        path: `/filterSources/${index}/facetId`,
        message: `Filter source facet '${source.facetId}' does not reference a facet.`
      });
    }
  });
}

function validateProfileReferences(graph, profileIds, nodeTypeIds, linkTypeIds, metricIds, errors) {
  if (profileIds === null) {
    return;
  }

  graph.viewProfiles.forEach((profile, index) => {
    validateReferenceList(profile.visibleNodeTypes, nodeTypeIds, `/viewProfiles/${index}/visibleNodeTypes`, "node type", errors);
    validateReferenceList(profile.visibleLinkTypes, linkTypeIds, `/viewProfiles/${index}/visibleLinkTypes`, "link type", errors);
    validateReference(profile.nodeMetric, metricIds, `/viewProfiles/${index}/nodeMetric`, "metric", errors);
    validateReference(profile.linkMetric, metricIds, `/viewProfiles/${index}/linkMetric`, "metric", errors);
  });
}

function validateProjectionReferences(graph, profileIds, linkTypeIds, errors) {
  if (graph.projections === undefined) {
    return;
  }

  graph.projections.forEach((projection, index) => {
    validateReference(projection.fromProfile, profileIds, `/projections/${index}/fromProfile`, "view profile", errors);
    validateReference(projection.toProfile, profileIds, `/projections/${index}/toProfile`, "view profile", errors);
    validateReference(projection.linkTypeId, linkTypeIds, `/projections/${index}/linkTypeId`, "link type", errors);
  });
}

function validateContainmentReferences(graph, nodeTypeIds, linkTypeIds, errors) {
  if (graph.containmentRules === undefined) {
    return;
  }

  graph.containmentRules.forEach((rule, index) => {
    validateReference(rule.linkTypeId, linkTypeIds, `/containmentRules/${index}/linkTypeId`, "link type", errors);
    validateReference(rule.parentTypeId, nodeTypeIds, `/containmentRules/${index}/parentTypeId`, "node type", errors);
    validateReference(rule.childTypeId, nodeTypeIds, `/containmentRules/${index}/childTypeId`, "node type", errors);
  });
}

function validateHierarchyReferences(graph, linkTypeIds, errors) {
  if (graph.hierarchy?.containmentLinkTypes === undefined) {
    return;
  }

  graph.hierarchy.containmentLinkTypes.forEach((linkTypeId, index) => {
    validateReference(linkTypeId, linkTypeIds, `/hierarchy/containmentLinkTypes/${index}`, "link type", errors);
  });
}

function validateDerivedLinks(derivedFrom, links, linkIndex, errors) {
  const references = derivedFrom === undefined
    ? []
    : Array.isArray(derivedFrom)
      ? derivedFrom
      : [derivedFrom];
  const linkIds = new Set(links.map((link) => link.id).filter(Boolean));

  references.forEach((linkId, referenceIndex) => {
    if (!linkIds.has(linkId)) {
      errors.push({
        kind: "semantic",
        path: `/links/${linkIndex}/derivedFrom/${referenceIndex}`,
        message: `Derived link '${linkId}' does not reference a link ID.`
      });
    }
  });
}

function validateReferenceList(values, ids, path, label, errors) {
  if (values === undefined || ids === null) {
    return;
  }

  values.forEach((value, index) => validateReference(value, ids, `${path}/${index}`, label, errors));
}

function validateReference(value, ids, path, label, errors) {
  if (value === undefined || ids === null || ids.has(value)) {
    return;
  }

  errors.push({
    kind: "semantic",
    path,
    message: `${label} '${value}' does not reference a defined ${label}.`
  });
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
