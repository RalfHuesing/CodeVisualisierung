export const VIEWER_CONFIG = Object.freeze({
  background: "#0b1120",
  link: Object.freeze({
    arrowLength: 3,
    chargeStrength: -120,
    distance: 90,
    maxWidth: 0.8,
    minWidth: 0.2
  }),
  node: Object.freeze({
    colors: Object.freeze(["#38bdf8", "#a78bfa", "#34d399", "#fbbf24"]),
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
  const nodes = graph.nodes.map((node) => ({
    ...node,
    color: getNodeKindColor(node, graph),
    visualValue: scaleValue(node.metrics?.[nodeMetric], nodeValues, VIEWER_CONFIG.node.minValue, VIEWER_CONFIG.node.maxValue)
  }));
  const links = graph.links.map((link) => ({
    ...link,
    visualWidth: scaleValue(getLinkValue(link, linkMetric), linkValues, VIEWER_CONFIG.link.minWidth, VIEWER_CONFIG.link.maxWidth)
  }));

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
  return {
    groups: getUniqueValues(graph.nodes, "groupId"),
    kinds: getUniqueValues(graph.nodes, "kind"),
    linkKinds: getUniqueValues(graph.links, "kind"),
    tags: [...new Set(graph.nodes.flatMap((node) => node.tags ?? []))].sort()
  };
}

export function filterGraph(graph, filters = {}) {
  const nodes = graph.nodes.filter((node) => matchesNodeFilters(node, filters));
  const nodeIds = new Set(nodes.map((node) => node.id));
  const links = graph.links.filter((link) => nodeIds.has(link.source) && nodeIds.has(link.target) && matchesLinkFilters(link, filters));
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
  const kinds = [...new Set(graph.nodes.map((item) => item.kind ?? "node"))];
  const kindIndex = kinds.indexOf(node.kind ?? "node");
  return VIEWER_CONFIG.node.colors[kindIndex % VIEWER_CONFIG.node.colors.length];
}

function getLinkValue(link, metricName) {
  if (metricName === "weight" && Number.isFinite(link.weight)) {
    return link.weight;
  }

  return Number.isFinite(link.metrics?.[metricName]) ? link.metrics[metricName] : 1;
}

function getUniqueValues(items, propertyName) {
  return [...new Set(items.map((item) => item[propertyName]).filter(Boolean))].sort();
}

function matchesNodeFilters(node, filters) {
  const hiddenKind = filters.hiddenKinds?.includes(node.kind);
  const groupMatches = !filters.groupId || node.groupId === filters.groupId;
  const kindMatches = !filters.kind || node.kind === filters.kind;
  const tagMatches = !filters.tag || node.tags?.includes(filters.tag);
  return !hiddenKind && groupMatches && kindMatches && tagMatches;
}

function matchesLinkFilters(link, filters) {
  return !filters.linkKind || link.kind === filters.linkKind;
}
