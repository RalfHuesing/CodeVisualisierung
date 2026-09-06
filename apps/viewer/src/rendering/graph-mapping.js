import { resolveVisualToken } from "../domain/graph.js";

const FALLBACK_NODE_COLOR = "#38bdf8";
const FALLBACK_LINK_COLOR = "#94a3b8";
const FALLBACK_NODE_SHAPE = "tetrahedron";

export const VIEWER_CONFIG = Object.freeze({
  link: Object.freeze({
    arrowLength: 3,
    chargeStrength: -120,
    distance: 90,
    maxWidth: 0.8,
    minWidth: 0.2
  }),
  node: Object.freeze({
    maxValue: 8,
    minValue: 2,
    relativeSize: 4
  })
});

export function createVisualGraphData(graph, options = {}) {
  const nodeMetric = options.nodeMetric ?? findNodeMetric(graph);
  const linkMetric = options.linkMetric ?? findLinkMetric(graph);
  const nodeValues = graph.nodes.map((node) => node.metrics?.[nodeMetric]).filter(Number.isFinite);
  const linkValues = graph.links.map((link) => getLinkValue(link, linkMetric)).filter(Number.isFinite);
  const nodes = graph.nodes.map((node) => {
    const visualStyle = getNodeVisualStyle(node, graph);
    return {
      ...node,
      color: visualStyle.color,
      visualShape: visualStyle.shape,
      visualTokenId: visualStyle.tokenId,
      visualValue: scaleValue(node.metrics?.[nodeMetric], nodeValues, VIEWER_CONFIG.node.minValue, VIEWER_CONFIG.node.maxValue)
    };
  });
  const links = graph.links.map((link) => {
    const visualStyle = getLinkVisualStyle(link, graph);
    return {
      ...link,
      color: visualStyle.color,
      visualTokenId: visualStyle.tokenId,
      visualWidth: scaleValue(getLinkValue(link, linkMetric), linkValues, VIEWER_CONFIG.link.minWidth, VIEWER_CONFIG.link.maxWidth)
    };
  });

  return { linkMetric, links, nodeMetric, nodes };
}

export function findNodeMetric(graph) {
  const metricNames = getMetricNames(graph, "node");
  return metricNames.find((name) => graph.nodes.some((node) => Number.isFinite(node.metrics?.[name]))) ?? null;
}

export function findLinkMetric(graph) {
  if (graph.links.some((link) => Number.isFinite(link.weight))) {
    return "weight";
  }

  return getMetricNames(graph, "link").find((name) => graph.links.some((link) => Number.isFinite(link.metrics?.[name]))) ?? null;
}

export function getMetricNames(graph, type) {
  const items = type === "node" ? graph.nodes : graph.links;
  const definedNames = Object.keys(graph.metricDefinitions ?? {});
  const observedNames = items.flatMap((item) => Object.keys(item.metrics ?? {}));
  const metricNames = [...new Set([...definedNames, ...observedNames])];
  return metricNames.filter((name) => items.some((item) => Number.isFinite(item.metrics?.[name])));
}

export function getMetricLabel(graph, metricName) {
  if (metricName === "weight") {
    return "Gewicht";
  }

  return graph.metricDefinitions?.[metricName]?.label ?? metricName ?? "Keine Metrik";
}

export function getFilterOptions(graph) {
  const sources = (graph.filterSources ?? []).flatMap((source) => {
    const facet = (graph.facets ?? []).find((item) => item.id === source.facetId);
    if (!facet?.source?.field) {
      return [];
    }

    return [{
      field: facet.source.field,
      id: source.id,
      label: source.label ?? facet.label ?? source.id,
      scope: facet.source.scope,
      values: getFacetValues(graph, facet)
    }];
  });

  return {
    groups: getUniqueValues(graph.nodes, "groupId"),
    kinds: getUniqueValues(graph.nodes, "kind"),
    linkKinds: getUniqueValues(graph.links, "kind"),
    sources,
    tags: [...new Set(graph.nodes.flatMap((node) => node.tags ?? []))].sort()
  };
}

export function getViewProfiles(graph) {
  return graph.viewProfiles ?? [];
}

export function getDefaultViewProfile(graph) {
  return [...getViewProfiles(graph)].sort((left, right) => (right.detailLevel ?? 0) - (left.detailLevel ?? 0))[0] ?? null;
}

export function getViewProfile(graph, profileId) {
  return getViewProfiles(graph).find((profile) => profile.id === profileId) ?? null;
}

export function filterGraph(graph, filters = {}, profile = null) {
  const nodes = graph.nodes.filter((node) => matchesNodeFilters(node, filters, graph, profile));
  const nodeIds = new Set(nodes.map((node) => node.id));
  const links = graph.links.filter(
    (link) => nodeIds.has(link.source) && nodeIds.has(link.target) && matchesLinkFilters(link, filters, graph, profile)
  );
  return { ...graph, links, nodes };
}

export function findSearchMatches(graph, query) {
  const normalizedQuery = query.trim().toLocaleLowerCase();
  return new Set(
    graph.nodes
      .filter((node) => `${node.id} ${node.label}`.toLocaleLowerCase().includes(normalizedQuery))
      .map((node) => node.id)
  );
}

export function scaleValue(value, values, outputMin, outputMax) {
  if (!Number.isFinite(value) || values.length === 0) {
    return (outputMin + outputMax) / 2;
  }

  const inputMin = Math.min(...values);
  const inputMax = Math.max(...values);
  if (inputMin === inputMax) {
    return (outputMin + outputMax) / 2;
  }

  const ratio = (value - inputMin) / (inputMax - inputMin);
  return outputMin + Math.min(1, Math.max(0, ratio)) * (outputMax - outputMin);
}

export function getNodeNeighborhood(graph, nodeId) {
  const incomingLinks = graph.links.filter((link) => link.directed && link.target === nodeId);
  const outgoingLinks = graph.links.filter((link) => link.directed && link.source === nodeId);
  const undirectedLinks = graph.links.filter(
    (link) => !link.directed && (link.source === nodeId || link.target === nodeId)
  );
  const neighborIds = new Set([
    ...incomingLinks.map((link) => link.source),
    ...outgoingLinks.map((link) => link.target),
    ...undirectedLinks.flatMap((link) => [link.source, link.target])
  ]);
  neighborIds.delete(nodeId);

  return { incomingLinks, neighborIds, outgoingLinks, undirectedLinks };
}

export function getNodeKindColor(node, graph) {
  return getNodeVisualStyle(node, graph).color;
}

export function getNodeVisualStyle(node, graph) {
  const definition = getDefinition(graph.nodeTypes, node.typeId ?? node.kind);
  const resolved = resolveVisualToken(graph, node.visualToken ?? definition?.visualToken);
  return {
    color: resolved.token?.color ?? FALLBACK_NODE_COLOR,
    shape: resolved.token?.shape ?? FALLBACK_NODE_SHAPE,
    tokenId: resolved.id,
    usedFallback: resolved.usedFallback
  };
}

export function getLinkVisualStyle(link, graph) {
  const definition = getDefinition(graph.linkTypes, link.typeId ?? link.kind);
  const resolved = resolveVisualToken(graph, link.visualToken ?? definition?.visualToken);
  return {
    color: resolved.token?.color ?? FALLBACK_LINK_COLOR,
    tokenId: resolved.id,
    usedFallback: resolved.usedFallback
  };
}

export function getNodeTypeLabel(node, graph) {
  const definition = getDefinition(graph.nodeTypes, node.typeId ?? node.kind);
  return definition?.label ?? node.typeId ?? node.kind ?? "Node";
}

function getLinkValue(link, metricName) {
  if (metricName === "weight" && Number.isFinite(link.weight)) {
    return link.weight;
  }

  return Number.isFinite(link.metrics?.[metricName]) ? link.metrics[metricName] : 1;
}

function getFacetValues(graph, facet) {
  const items = facet.source.scope === "link" ? graph.links : graph.nodes;
  return [...new Set(items.flatMap((item) => {
    return getItemValues(item, facet.source.field);
  }))].sort();
}

function getUniqueValues(items, propertyName) {
  return [...new Set(items.map((item) => item[propertyName]).filter(Boolean))].sort();
}

function matchesNodeFilters(node, filters, graph, profile) {
  if (profile?.visibleNodeTypes && !profile.visibleNodeTypes.includes(node.typeId ?? node.kind)) {
    return false;
  }

  return matchesFacetFilters(node, filters, graph, "node");
}

function matchesLinkFilters(link, filters, graph, profile) {
  if (profile?.visibleLinkTypes && !profile.visibleLinkTypes.includes(link.typeId ?? link.kind)) {
    return false;
  }

  return matchesFacetFilters(link, filters, graph, "link");
}

function matchesFacetFilters(item, filters, graph, scope) {
  return getFilterOptions(graph).sources
    .filter((source) => source.scope === scope && filters[source.id])
    .every((source) => getItemValues(item, source.field).includes(filters[source.id]));
}

function getItemValues(item, field) {
  const value = field.split(".").reduce((current, segment) => current?.[segment], item);
  return Array.isArray(value) ? value : value === undefined || value === null ? [] : [value];
}

function getDefinition(definitions, id) {
  if (Array.isArray(definitions)) {
    return definitions.find((definition) => definition.id === id);
  }

  return definitions?.[id];
}
