import ForceGraph3D from "3d-force-graph";
import { createVisualGraphData, findSearchMatches, getNodeNeighborhood, VIEWER_CONFIG } from "./graph-mapping.js";

const DIMMED_OPACITY = 0.12;
const VISIBLE_LINK_OPACITY = 0.8;
const INCOMING_COLOR = "#fbbf24";
const OUTGOING_COLOR = "#38bdf8";
const RELATED_COLOR = "#a78bfa";
const DEFAULT_LINK_COLOR = "#94a3b8";

export function createGraphRenderer(container, onNodeClick) {
  let graphInstance;
  let currentGraph;
  let selectedNodeId;
  let searchQuery = "";

  function render(graph) {
    destroy();
    currentGraph = graph;
    const visualData = createVisualGraphData(graph);
    graphInstance = new ForceGraph3D(container, { controlType: "orbit" })
      .backgroundColor(VIEWER_CONFIG.background)
      .showNavInfo(false)
      .nodeLabel((node) => `${node.label} (${node.kind})`)
      .nodeVal((node) => node.visualValue)
      .nodeColor((node) => getNodeColor(node, currentGraph, selectedNodeId, searchQuery))
      .nodeOpacity((node) => getNodeOpacity(node, currentGraph, selectedNodeId, searchQuery))
      .linkLabel((link) => link.kind)
      .linkWidth((link) => link.visualWidth)
      .linkColor((link) => getLinkColor(link, selectedNodeId))
      .linkOpacity((link) => getLinkOpacity(link, currentGraph, selectedNodeId, searchQuery))
      .linkDirectionalArrowLength((link) => link.directed ? VIEWER_CONFIG.link.arrowLength : 0)
      .linkDirectionalArrowRelPos(1)
      .linkDirectionalArrowColor((link) => getLinkColor(link, selectedNodeId))
      .onNodeClick((node) => onNodeClick(node.id))
      .onBackgroundClick(() => onNodeClick(null))
      .onEngineStop(() => graphInstance.zoomToFit(400, 40))
      .warmupTicks(80)
      .cooldownTime(1500)
      .graphData(visualData);
    container.dataset.nodeCount = String(graph.nodes.length);
    container.dataset.linkCount = String(graph.links.length);
    refreshStyles();
    return graphInstance;
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
      .nodeOpacity((node) => getNodeOpacity(node, currentGraph, selectedNodeId, searchQuery))
      .linkColor((link) => getLinkColor(link, selectedNodeId))
      .linkOpacity((link) => getLinkOpacity(link, currentGraph, selectedNodeId, searchQuery))
      .linkDirectionalArrowColor((link) => getLinkColor(link, selectedNodeId));
  }

  function destroy() {
    if (graphInstance) {
      graphInstance._destructor();
      graphInstance = undefined;
    }
    container.replaceChildren();
  }

  return { destroy, focus, render, reset, search };
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
  if (matchesSearch(node, searchQuery)) {
    return "#fbbf24";
  }
  return node.color;
}

function getNodeOpacity(node, graph, selectedNodeId, searchQuery) {
  const focus = getNodeFocus(graph, selectedNodeId);
  const isFocusDimmed = selectedNodeId && node.id !== selectedNodeId && !focus.neighbors.has(node.id);
  const isSearchDimmed = searchQuery.trim() && !matchesSearch(node, searchQuery);
  return isFocusDimmed || isSearchDimmed ? DIMMED_OPACITY : 1;
}

function getLinkColor(link, selectedNodeId) {
  if (!selectedNodeId) {
    return DEFAULT_LINK_COLOR;
  }
  const sourceId = getEndpointId(link.source);
  const targetId = getEndpointId(link.target);
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

function getLinkOpacity(link, graph, selectedNodeId, searchQuery) {
  const sourceId = getEndpointId(link.source);
  const targetId = getEndpointId(link.target);
  const isFocusDimmed = selectedNodeId && sourceId !== selectedNodeId && targetId !== selectedNodeId;
  const searchMatches = new Set(graph.nodes.filter((node) => matchesSearch(node, searchQuery)).map((node) => node.id));
  const isSearchDimmed = searchQuery.trim() && !searchMatches.has(sourceId) && !searchMatches.has(targetId);
  return isFocusDimmed || isSearchDimmed ? DIMMED_OPACITY : VISIBLE_LINK_OPACITY;
}

function matchesSearch(node, query) {
  const normalizedQuery = query.trim().toLocaleLowerCase();
  return normalizedQuery.length > 0 && `${node.id} ${node.label}`.toLocaleLowerCase().includes(normalizedQuery);
}

function getEndpointId(endpoint) {
  return typeof endpoint === "object" ? endpoint.id : endpoint;
}
