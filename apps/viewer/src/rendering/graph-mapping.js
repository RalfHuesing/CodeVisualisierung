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

export function createVisualGraphData(graph) {
  const nodeMetric = findNodeMetric(graph);
  const nodeValues = graph.nodes.map((node) => node.metrics?.[nodeMetric]).filter(Number.isFinite);
  const linkValues = graph.links.map((link) => getLinkValue(link)).filter(Number.isFinite);
  const nodes = graph.nodes.map((node) => ({
    ...node,
    color: getNodeColor(node, graph),
    visualValue: scaleValue(node.metrics?.[nodeMetric], nodeValues, VIEWER_CONFIG.node.minValue, VIEWER_CONFIG.node.maxValue)
  }));
  const links = graph.links.map((link) => ({
    ...link,
    visualWidth: scaleValue(getLinkValue(link), linkValues, VIEWER_CONFIG.link.minWidth, VIEWER_CONFIG.link.maxWidth)
  }));

  return { links, nodeMetric, nodes };
}

export function findNodeMetric(graph) {
  const metricNames = Object.keys(graph.metricDefinitions ?? {});
  return metricNames.find((name) => graph.nodes.some((node) => Number.isFinite(node.metrics?.[name]))) ?? null;
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

function getNodeColor(node, graph) {
  const kinds = [...new Set(graph.nodes.map((item) => item.kind ?? "node"))];
  const kindIndex = kinds.indexOf(node.kind ?? "node");
  return VIEWER_CONFIG.node.colors[kindIndex % VIEWER_CONFIG.node.colors.length];
}

function getLinkValue(link) {
  if (Number.isFinite(link.weight)) {
    return link.weight;
  }

  return Object.values(link.metrics ?? {}).find(Number.isFinite) ?? 1;
}
