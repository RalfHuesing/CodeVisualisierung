import ForceGraph3D from "3d-force-graph";
import * as THREE from "three";
import { createVisualGraphData, filterGraph, findSearchMatches, getNodeNeighborhood, VIEWER_CONFIG } from "./graph-mapping.js";

const INCOMING_COLOR = "#fbbf24";
const OUTGOING_COLOR = "#38bdf8";
const RELATED_COLOR = "#a78bfa";
const DIMMED_NODE_COLOR = "#1e293b";
const DIMMED_LINK_COLOR = "#1e293b";
const NODE_GEOMETRIES = new Map();

export function createGraphRenderer(container, onNodeClick) {
  let graphInstance;
  let currentGraph;
  let selectedNodeId;
  let searchQuery = "";
  let viewOptions = {};
  let fitState = { requested: true, count: 0 };

  function render(graph, options = {}) {
    destroy();
    currentGraph = graph;
    viewOptions = options;
    fitState = { requested: true, count: 0 };
    container.dataset.autoFitCount = "0";
    const visualData = createVisualGraphData(filterGraph(graph, viewOptions.filters, viewOptions.profile), viewOptions);
    graphInstance = createForceGraph(
      container,
      onNodeClick,
      visualData,
      graph,
      (node) => getNodeColor(node, currentGraph, selectedNodeId, searchQuery),
      (link) => getLinkColor(link, graph, selectedNodeId, searchQuery),
      (node) => createNodeObject(node, getNodeColor(node, currentGraph, selectedNodeId, searchQuery)),
      () => fitGraphToContent(graphInstance, container, fitState)
    );
    graphInstance.graphData(visualData);
    updateDataAttributes(container, visualData);
    refreshStyles();
    return graphInstance;
  }

  function updateOptions(options = {}) {
    viewOptions = { ...viewOptions, ...options, filters: options.filters ?? viewOptions.filters };
    if (!graphInstance || !currentGraph) {
      return;
    }

    const visualData = createVisualGraphData(filterGraph(currentGraph, viewOptions.filters, viewOptions.profile), viewOptions);
    fitState.requested = true;
    graphInstance.graphData(visualData);
    updateDataAttributes(container, visualData);
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
      .nodeThreeObject((node) => createNodeObject(node, getNodeColor(node, currentGraph, selectedNodeId, searchQuery)))
      .linkColor((link) => getLinkColor(link, currentGraph, selectedNodeId, searchQuery))
      .linkDirectionalArrowColor((link) => getLinkColor(link, currentGraph, selectedNodeId, searchQuery));
  }

  function destroy() {
    if (graphInstance) {
      graphInstance._destructor();
      graphInstance = undefined;
    }
    delete container.dataset.autoFitCount;
    container.replaceChildren();
  }

  return { destroy, focus, render, reset, search, updateOptions };
}

function updateDataAttributes(container, visualData) {
  container.dataset.nodeCount = String(visualData.nodes.length);
  container.dataset.linkCount = String(visualData.links.length);
  container.dataset.layoutGroupCount = String(visualData.groupCount);
  if (visualData.layoutProfileId) {
    container.dataset.layoutProfileId = visualData.layoutProfileId;
  } else {
    delete container.dataset.layoutProfileId;
  }
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

function fitGraphToContent(graphInstance, container, fitState) {
  if (!fitState.requested || !graphInstance) {
    return;
  }

  fitState.requested = false;
  fitState.count += 1;
  container.dataset.autoFitCount = String(fitState.count);
  graphInstance.zoomToFit(400, 40);
}

function createForceGraph(container, onNodeClick, visualData, graph, nodeColor, linkColor, nodeObject, onEngineStop) {
  const graphInstance = new ForceGraph3D(container, { controlType: "orbit" })
    .backgroundColor(graph.theme?.background ?? "#0b1120")
    .enableNodeDrag(false)
    .showNavInfo(false)
    .nodeRelSize(VIEWER_CONFIG.node.relativeSize)
    .nodeLabel((node) => `${node.label} (${node.kind})`)
    .nodeVal((node) => node.visualValue)
    .nodeColor(nodeColor)
    .nodeThreeObject(nodeObject)
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
    .onEngineStop(onEngineStop)
    .warmupTicks(80)
    .cooldownTime(1500);
  graphInstance.d3Force("link").distance((link) => link.distance ?? VIEWER_CONFIG.link.distance);
  graphInstance.d3Force("charge").strength(VIEWER_CONFIG.link.chargeStrength);
  return graphInstance;
}

function createNodeObject(node, color) {
  const geometry = getNodeGeometry(node.visualShape);
  const material = new THREE.MeshBasicMaterial({ color });
  const mesh = new THREE.Mesh(geometry, material);
  mesh.scale.setScalar(Math.max(0.75, node.visualValue / 2));
  return mesh;
}

function getNodeGeometry(shape) {
  const normalizedShape = shape ?? "tetrahedron";
  if (NODE_GEOMETRIES.has(normalizedShape)) {
    return NODE_GEOMETRIES.get(normalizedShape);
  }

  const geometry = createNodeGeometry(normalizedShape);
  NODE_GEOMETRIES.set(normalizedShape, geometry);
  return geometry;
}

function createNodeGeometry(shape) {
  if (shape === "sphere") {
    return new THREE.SphereGeometry(1, 12, 8);
  }
  if (shape === "box") {
    return new THREE.BoxGeometry(1.5, 1.5, 1.5);
  }
  if (shape === "octahedron") {
    return new THREE.OctahedronGeometry(1.2, 0);
  }
  if (shape === "cylinder") {
    return new THREE.CylinderGeometry(0.9, 0.9, 1.5, 10);
  }
  return new THREE.TetrahedronGeometry(1.2, 0);
}

function getLinkColor(link, graph, selectedNodeId, searchQuery) {
  const sourceId = getEndpointId(link.source);
  const targetId = getEndpointId(link.target);
  if (searchQuery.trim() && !matchesSearchId(sourceId, searchQuery) && !matchesSearchId(targetId, searchQuery)) {
    return DIMMED_LINK_COLOR;
  }
  if (!selectedNodeId) {
    return link.color ?? graph.theme?.linkColor ?? "#94a3b8";
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
  return link.color ?? graph.theme?.linkColor ?? "#94a3b8";
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
