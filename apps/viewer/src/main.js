import sampleGraph from "../../../contracts/graph-universe/fixtures/minimal.json";
import { normalizeGraph, parseGraphText, validateGraph } from "./domain/graph.js";
import { createGraphRenderer } from "./rendering/visualization.js";
import { getNodeNeighborhood } from "./rendering/graph-mapping.js";

const fileInput = document.querySelector("#graph-file");
const dropZone = document.querySelector("#drop-zone");
const sampleButton = document.querySelector("#load-sample");
const detailsButton = document.querySelector("#toggle-details");
const closeDetailsButton = document.querySelector("#close-details");
const resetButton = document.querySelector("#reset-view");
const searchInput = document.querySelector("#node-search");
const nodeSelect = document.querySelector("#node-select");
const status = document.querySelector("#graph-status");
const dropHint = document.querySelector("#drop-hint");
const errorsPanel = document.querySelector("#graph-errors");
const errorList = document.querySelector("#graph-error-list");
const graphTitle = document.querySelector("#graph-title");
const graphMeta = document.querySelector("#graph-meta");
const graphCanvas = document.querySelector("#graph-canvas");
const detailsCard = document.querySelector("#details-card");
const detailsKicker = document.querySelector("#details-kicker");
const detailsTitle = document.querySelector("#details-title");
const detailsDescription = document.querySelector("#details-description");
const nodeCount = document.querySelector("#node-count");
const linkCount = document.querySelector("#link-count");
const selectedDetails = document.querySelector("#selected-node-details");
const graphRenderer = createGraphRenderer(graphCanvas, handleNodeClick);

let currentGraph;

loadSampleGraph();

fileInput.addEventListener("change", handleFileSelection);
sampleButton.addEventListener("click", loadSampleGraph);
detailsButton.addEventListener("click", toggleGraphDetails);
closeDetailsButton.addEventListener("click", closeDetails);
resetButton.addEventListener("click", resetView);
searchInput.addEventListener("input", handleSearch);
nodeSelect.addEventListener("change", handleNodeSelection);
document.addEventListener("keydown", handleKeyDown);
dropZone.addEventListener("dragover", handleDragOver);
dropZone.addEventListener("dragleave", handleDragLeave);
dropZone.addEventListener("drop", handleDrop);

function handleFileSelection(event) {
  const [file] = event.target.files;
  if (file) {
    loadFile(file);
  }
}

function handleDragOver(event) {
  event.preventDefault();
  dropZone.classList.add("is-dragging");
}

function handleDragLeave() {
  dropZone.classList.remove("is-dragging");
}

function handleDrop(event) {
  event.preventDefault();
  dropZone.classList.remove("is-dragging");

  const [file] = event.dataTransfer.files;
  if (file) {
    loadFile(file);
  }
}

async function loadFile(file) {
  try {
    const result = parseGraphText(await file.text());
    if (!result.valid) {
      showErrors(result.errors);
      return;
    }

    showGraph(result.graph, file.name);
  } catch {
    showErrors([{ kind: "file", path: "$", message: "Die Datei konnte nicht gelesen werden." }]);
  }
}

function loadSampleGraph() {
  const validation = validateGraph(sampleGraph);
  if (!validation.valid) {
    showErrors(validation.errors);
    return;
  }

  showGraph(normalizeGraph(sampleGraph), "Beispieldaten");
}

function showGraph(graph, sourceName) {
  currentGraph = graph;
  errorsPanel.hidden = true;
  errorList.replaceChildren();
  dropHint.hidden = true;
  selectedDetails.hidden = true;
  graphRenderer.render(graph);
  graphRenderer.focus(null);
  graphRenderer.search(searchInput.value);
  updateNodeSelector(graph);
  graphTitle.textContent = graph.meta?.title ?? sourceName;
  graphMeta.textContent = getGraphMeta(graph, sourceName);
  nodeCount.textContent = String(graph.nodes.length);
  linkCount.textContent = String(graph.links.length);
  status.textContent = `${sourceName} erfolgreich geladen.`;
  closeDetails();
}

function showErrors(errors) {
  errorList.replaceChildren();
  errors.forEach((error) => {
    const item = document.createElement("li");
    item.textContent = `${error.path}: ${error.message}`;
    errorList.append(item);
  });

  errorsPanel.hidden = false;
  status.textContent = "Die Graphdaten sind ungültig.";
}

function handleNodeClick(nodeId) {
  if (!nodeId) {
    clearSelection();
    return;
  }

  if (!currentGraph) {
    return;
  }

  const node = currentGraph.nodes.find((item) => item.id === nodeId);
  if (node) {
    showNodeDetails(node);
  }
}

function handleNodeSelection(event) {
  handleNodeClick(event.target.value || null);
}

function showNodeDetails(node) {
  const neighborhood = getNodeNeighborhood(currentGraph, node.id);
  graphRenderer.focus(node.id);
  nodeSelect.value = node.id;
  detailsKicker.textContent = "Ausgewählter Node";
  detailsTitle.textContent = node.label ?? node.id;
  detailsDescription.textContent = node.kind ?? "Node";
  document.querySelector("#selected-node-id").textContent = node.id;
  document.querySelector("#selected-node-kind").textContent = node.kind ?? "–";
  document.querySelector("#selected-node-group").textContent = node.groupId ?? "–";
  document.querySelector("#selected-node-tags").textContent = node.tags?.join(", ") || "–";
  document.querySelector("#selected-node-incoming").textContent = String(neighborhood.incomingLinks.length);
  document.querySelector("#selected-node-outgoing").textContent = String(neighborhood.outgoingLinks.length);
  document.querySelector("#selected-node-undirected").textContent = String(neighborhood.undirectedLinks.length);
  document.querySelector("#selected-node-metrics").textContent = formatDetailsCode(node.metrics);
  document.querySelector("#selected-node-attributes").textContent = formatDetailsCode(node.attributes);
  selectedDetails.hidden = false;
  openDetails();
}

function toggleGraphDetails() {
  if (detailsCard.hidden) {
    showGraphDetails();
  } else {
    closeDetails();
  }
}

function showGraphDetails() {
  if (!currentGraph) {
    return;
  }

  graphRenderer.focus(null);
  selectedDetails.hidden = true;
  detailsKicker.textContent = "Graphdetails";
  detailsTitle.textContent = currentGraph.meta?.title ?? "Graph";
  detailsDescription.textContent = currentGraph.meta?.description ?? "Keine Beschreibung vorhanden.";
  openDetails();
}

function openDetails() {
  detailsCard.hidden = false;
  detailsButton.setAttribute("aria-expanded", "true");
}

function closeDetails() {
  detailsCard.hidden = true;
  detailsButton.setAttribute("aria-expanded", "false");
}

function resetView() {
  searchInput.value = "";
  nodeSelect.value = "";
  if (currentGraph) {
    graphRenderer.reset();
  }
  selectedDetails.hidden = true;
  closeDetails();
}

function clearSelection() {
  if (!currentGraph) {
    return;
  }

  graphRenderer.focus(null);
  nodeSelect.value = "";
  selectedDetails.hidden = true;
  closeDetails();
}

function handleSearch() {
  if (currentGraph) {
    graphRenderer.search(searchInput.value);
  }
}

function handleKeyDown(event) {
  if (event.key === "Escape") {
    clearSelection();
  }
}

function updateNodeSelector(graph) {
  const defaultOption = document.createElement("option");
  defaultOption.value = "";
  defaultOption.textContent = "Node auswählen";
  nodeSelect.replaceChildren(defaultOption);
  graph.nodes.forEach((node) => {
    const option = document.createElement("option");
    option.value = node.id;
    option.textContent = node.label ?? node.id;
    nodeSelect.append(option);
  });
}

function getGraphMeta(graph, sourceName) {
  const format = `${graph.format.name} ${graph.format.version}`;
  const source = graph.meta?.source?.name ? ` · Quelle: ${graph.meta.source.name}` : "";
  return `${format}${source} · ${sourceName}`;
}

function formatDetailsCode(value) {
  return value && Object.keys(value).length > 0 ? JSON.stringify(value, null, 2) : "Keine Angaben";
}
