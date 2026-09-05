const SVG_NAMESPACE = "http://www.w3.org/2000/svg";

export const VIEWER_CONFIG = Object.freeze({
  canvas: Object.freeze({
    height: 520,
    padding: 90,
    width: 900
  }),
  link: Object.freeze({
    maxWidth: 6,
    minWidth: 1.5
  }),
  node: Object.freeze({
    colors: Object.freeze(["#38bdf8", "#a78bfa", "#34d399", "#fbbf24"]),
    maxRadius: 34,
    minRadius: 18
  })
});

export function createGraphLayout(graph) {
  const nodeMetric = findNodeMetric(graph);
  const nodeValues = graph.nodes.map((node) => node.metrics?.[nodeMetric]).filter(Number.isFinite);
  const linkValues = graph.links.map((link) => getLinkValue(link)).filter(Number.isFinite);
  const nodesById = new Map();
  const nodes = graph.nodes.map((node, index) => {
    const position = getNodePosition(index, graph.nodes.length);
    const layoutNode = {
      ...node,
      color: getNodeColor(node, graph),
      radius: scaleValue(node.metrics?.[nodeMetric], nodeValues, VIEWER_CONFIG.node.minRadius, VIEWER_CONFIG.node.maxRadius),
      x: position.x,
      y: position.y
    };
    nodesById.set(node.id, layoutNode);
    return layoutNode;
  });
  const links = graph.links.map((link) => ({
    ...link,
    sourceNode: nodesById.get(link.source),
    targetNode: nodesById.get(link.target),
    width: scaleValue(getLinkValue(link), linkValues, VIEWER_CONFIG.link.minWidth, VIEWER_CONFIG.link.maxWidth)
  }));

  return { links, nodes, nodeMetric };
}

export function findNodeMetric(graph) {
  const metricNames = Object.keys(graph.metricDefinitions ?? {});
  return metricNames.find((name) => graph.nodes.some((node) => Number.isFinite(node.metrics?.[name]))) ?? null;
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

export function renderGraph(svg, graph) {
  const layout = createGraphLayout(graph);
  svg.replaceChildren();
  svg.setAttribute("viewBox", `0 0 ${VIEWER_CONFIG.canvas.width} ${VIEWER_CONFIG.canvas.height}`);
  svg.setAttribute("aria-label", `${graph.meta?.title ?? "Graph"}: ${layout.nodes.length} Nodes, ${layout.links.length} Links`);

  const defs = createSvgElement("defs");
  defs.append(createArrowMarker());
  const linkGroup = createSvgElement("g");
  linkGroup.classList.add("graph-links");
  layout.links.forEach((link) => linkGroup.append(createLinkElement(link)));
  const nodeGroup = createSvgElement("g");
  nodeGroup.classList.add("graph-nodes");
  layout.nodes.forEach((node) => nodeGroup.append(createNodeElement(node)));
  svg.append(defs, linkGroup, nodeGroup);

  return layout;
}

function createArrowMarker() {
  const marker = createSvgElement("marker");
  marker.setAttribute("id", "graph-arrow");
  marker.setAttribute("markerHeight", "8");
  marker.setAttribute("markerWidth", "8");
  marker.setAttribute("orient", "auto");
  marker.setAttribute("refX", "7");
  marker.setAttribute("refY", "4");
  marker.setAttribute("viewBox", "0 0 8 8");
  const path = createSvgElement("path");
  path.setAttribute("d", "M 0 0 L 8 4 L 0 8 z");
  path.setAttribute("fill", "#94a3b8");
  marker.append(path);
  return marker;
}

function createLinkElement(link) {
  const line = createSvgElement("line");
  line.classList.add("graph-link");
  line.dataset.source = link.source;
  line.dataset.target = link.target;
  line.setAttribute("stroke-width", String(link.width));
  line.setAttribute("x1", String(link.sourceNode.x));
  line.setAttribute("x2", String(link.targetNode.x));
  line.setAttribute("y1", String(link.sourceNode.y));
  line.setAttribute("y2", String(link.targetNode.y));
  if (link.directed) {
    line.setAttribute("marker-end", "url(#graph-arrow)");
  }
  return line;
}

function createNodeElement(node) {
  const group = createSvgElement("g");
  group.classList.add("graph-node");
  group.dataset.nodeId = node.id;
  group.setAttribute("role", "img");
  group.setAttribute("aria-label", node.label ?? node.id);
  group.setAttribute("transform", `translate(${node.x} ${node.y})`);

  const circle = createSvgElement("circle");
  circle.setAttribute("fill", node.color);
  circle.setAttribute("r", String(node.radius));
  const label = createSvgElement("text");
  label.classList.add("graph-node-label");
  label.textContent = node.label ?? node.id;
  label.setAttribute("y", String(node.radius + 20));
  group.append(circle, label);
  return group;
}

function createSvgElement(tagName) {
  return document.createElementNS(SVG_NAMESPACE, tagName);
}

function getNodePosition(index, nodeCount) {
  const centerX = VIEWER_CONFIG.canvas.width / 2;
  const centerY = VIEWER_CONFIG.canvas.height / 2;
  if (nodeCount === 1) {
    return { x: centerX, y: centerY };
  }

  const orbitX = centerX - VIEWER_CONFIG.canvas.padding;
  const orbitY = centerY - VIEWER_CONFIG.canvas.padding;
  const angle = -Math.PI / 2 + (index * 2 * Math.PI) / nodeCount;
  return {
    x: centerX + Math.cos(angle) * orbitX,
    y: centerY + Math.sin(angle) * orbitY
  };
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
