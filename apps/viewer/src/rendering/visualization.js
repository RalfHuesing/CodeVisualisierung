import ForceGraph3D from "3d-force-graph";
import { createVisualGraphData, filterGraph, findSearchMatches, getNodeNeighborhood, VIEWER_CONFIG } from "./graph-mapping.js";

const INCOMING_COLOR = "#fbbf24";
const OUTGOING_COLOR = "#38bdf8";
const RELATED_COLOR = "#a78bfa";
const DEFAULT_LINK_COLOR = "#94a3b8";
const DIMMED_NODE_COLOR = "#1e293b";
const DIMMED_LINK_COLOR = "#1e293b";

export function createGraphRenderer(container, onNodeClick) {
  let graphInstance;
  let currentGraph;
  let selectedNodeId;
  let searchQuery = "";
  let viewOptions = {};

  function render(graph, options = {}) {
    destroy();
    currentGraph = graph;
    viewOptions = options;
    const visualData = createVisualGraphData(filterGraph(graph, viewOptions.filters), viewOptions);
    graphInstance = createForceGraph(
      container,
      onNodeClick,
      visualData,
      (node) => getNodeColor(node, currentGraph, selectedNodeId, searchQuery),
      (link) => getLinkColor(link, selectedNodeId, searchQuery)
    );
    updateDataAttributes(visualData);
    refreshStyles();
    return graphInstance;
  }

  function updateOptions(options = {}) {
    viewOptions = { ...viewOptions, ...options, filters: options.filters ?? viewOptions.filters };
    if (!graphInstance || !currentGraph) {
      return;
    }

    const visualData = createVisualGraphData(filterGraph(currentGraph, viewOptions.filters), viewOptions);
    graphInstance.graphData(visualData);
    updateDataAttributes(visualData);
    refreshStyles();
  }

  function focus(nodeId) {
    selectedNodeId = nodeId ?? undefined;
    container.dataset.selectedNodeId = nodeId ?? "";
    refreshStyles();
  }

  function search(query) {
    searchQuery = query;
    container.dataset.searchQuery = query;
    container.dataset.searchMatchCount = String(findSearchMatches(currentGraph, query).size);
    refreshStyles();
  }

  function reset() {
    selectedNodeId = undefined;
    searchQuery = "";
    delete container.dataset.selectedNodeId;
    delete container.dataset.searchQuery;
    delete container.dataset.searchMatchCount;
    refreshStyles();
  }

  function refreshStyles() {
    if (!graphInstance || !currentGraph) {
      return;
    }

    graphInstance
      .nodeColor((node) => getNodeColor(node, currentGraph, selectedNodeId, searchQuery))
      .linkColor((link) => getLinkColor(link, selectedNodeId, searchQuery))
      .linkDirectionalArrowColor((link) => getLinkColor(link, selectedNodeId, searchQuery));
  }

  function destroy() {
    if (graphInstance) {
      graphInstance._destructor();
      graphInstance = undefined;
    }
    container.replaceChildren();
  }

  function updateDataAttributes(visualData) {
    container.dataset.nodeCount = String(visualData.nodes.length);
    container.dataset.linkCount = String(visualData.links.length);
  }

  return { destroy, focus, render, reset, search, updateOptions };
}

export function getNodeFocus(graph, nodeId) {
  const neighborhood = nodeId ? getNodeNeighborhood(graph, nodeId) : null;
  return {
    incoming: new Set(neighborhood?.incomingLinks ?? []),
    neighbors: new Set(neighborhood?.neighborIds ?? []),
    outgoing: new Set(neighborhood?.outgoingLinks ?? []),
    undirected: new Set(neighborhood?.undirectedLinks ?? [])
  };
}

function getNodeColor(node, graph, selectedNodeId, searchQuery) {
  if (node.id === selectedNodeId) {
    return "#ffffff";
  }
  const focus = getNodeFocus(graph, selectedNodeId);
  const isFocusDimmed = selectedNodeId && !focus.neighbors.has(node.id);
  const isSearchDimmed = searchQuery.trim() && !matchesSearch(node, searchQuery);
  if (isFocusDimmed || isSearchDimmed) {
    return DIMMED_NODE_COLOR;
  }
  if (matchesSearch(node, searchQuery)) {
    return "#fbbf24";
  }
  return node.color;
}


function createForceGraph(container, onNodeClick, visualData, nodeColor, linkColor) {
  const graphInstance = new ForceGraph3D(container, { controlType: "orbit" })
    .backgroundColor(VIEWER_CONFIG.background)
    .showNavInfo(false)
    .nodeRelSize(VIEWER_CONFIG.node.relativeSize)
    .nodeLabel((node) => `${node.label} (${node.kind})`)
    .nodeVal((node) => node.visualValue)
    .nodeColor(nodeColor)
    .nodeOpacity(1)
    .linkLabel((link) => link.kind)
    .linkWidth((link) => link.visualWidth)
    .linkColor(linkColor)
    .linkOpacity(1)
    .linkDirectionalArrowLength((link) => link.directed ? VIEWER_CONFIG.link.arrowLength : 0)
    .linkDirectionalArrowRelPos(1)
    .linkDirectionalArrowColor(linkColor)
    .onNodeClick((node) => onNodeClick(node.id))
    .onBackgroundClick(() => onNodeClick(null))
    .onEngineStop(() => graphInstance.zoomToFit(400, 40))
    .warmupTicks(80)
    .cooldownTime(1500);
  graphInstance.d3Force("link").distance(VIEWER_CONFIG.link.distance);
  graphInstance.d3Force("charge").strength(VIEWER_CONFIG.link.chargeStrength);
  graphInstance.graphData(visualData);
  return graphInstance;
}

function getLinkColor(link, selectedNodeId, searchQuery) {
  const sourceId = getEndpointId(link.source);
  const targetId = getEndpointId(link.target);
  if (searchQuery.trim() && !matchesSearchId(sourceId, searchQuery) && !matchesSearchId(targetId, searchQuery)) {
    return DIMMED_LINK_COLOR;
  }
  if (!selectedNodeId) {
    return DEFAULT_LINK_COLOR;
  }
  if (link.directed && targetId === selectedNodeId) {
    return INCOMING_COLOR;
  }
  if (link.directed && sourceId === selectedNodeId) {
    return OUTGOING_COLOR;
  }
  if (!link.directed && (sourceId === selectedNodeId || targetId === selectedNodeId)) {
    return RELATED_COLOR;
  }
  return DEFAULT_LINK_COLOR;
}

function matchesSearch(node, query) {
  const normalizedQuery = query.trim().toLocaleLowerCase();
  return normalizedQuery.length > 0 && `${node.id} ${node.label}`.toLocaleLowerCase().includes(normalizedQuery);
}

function matchesSearchId(nodeId, query) {
  return nodeId.toLocaleLowerCase().includes(query.trim().toLocaleLowerCase());
}

function getEndpointId(endpoint) {
  return typeof endpoint === "object" ? endpoint.id : endpoint;
}
