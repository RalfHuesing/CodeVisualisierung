import sampleGraph from "../../../contracts/graph-universe/fixtures/minimal.json";
import { normalizeGraph, parseGraphText, validateGraph } from "./domain/graph.js";
import { applyGraphFocus, applyGraphSearch, getNodeNeighborhood, renderGraph } from "./visualization.js";

const fileInput = document.querySelector("#graph-file");
const dropZone = document.querySelector("#drop-zone");
const sampleButton = document.querySelector("#load-sample");
const detailsButton = document.querySelector("#toggle-details");
const closeDetailsButton = document.querySelector("#close-details");
const resetButton = document.querySelector("#reset-view");
const searchInput = document.querySelector("#node-search");
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

let currentGraph;

loadSampleGraph();

fileInput.addEventListener("change", handleFileSelection);
sampleButton.addEventListener("click", loadSampleGraph);
detailsButton.addEventListener("click", toggleGraphDetails);
closeDetailsButton.addEventListener("click", closeDetails);
resetButton.addEventListener("click", resetView);
searchInput.addEventListener("input", handleSearch);
graphCanvas.addEventListener("click", handleGraphClick);
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
  renderGraph(graphCanvas, graph);
  applyGraphFocus(graphCanvas, graph, null);
  applyGraphSearch(graphCanvas, graph, searchInput.value);
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

function handleGraphClick(event) {
  if (event.target === graphCanvas) {
    clearSelection();
    return;
  }

  const nodeElement = event.target.closest(".graph-node");
  if (!nodeElement || !currentGraph) {
    return;
  }

  const node = currentGraph.nodes.find((item) => item.id === nodeElement.dataset.nodeId);
  if (node) {
    showNodeDetails(node);
  }
}

function showNodeDetails(node) {
  const neighborhood = getNodeNeighborhood(currentGraph, node.id);
  applyGraphFocus(graphCanvas, currentGraph, node.id);
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

  applyGraphFocus(graphCanvas, currentGraph, null);
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
  if (currentGraph) {
    applyGraphFocus(graphCanvas, currentGraph, null);
    applyGraphSearch(graphCanvas, currentGraph, "");
  }
  selectedDetails.hidden = true;
  closeDetails();
}

function clearSelection() {
  if (!currentGraph) {
    return;
  }

  applyGraphFocus(graphCanvas, currentGraph, null);
  selectedDetails.hidden = true;
  closeDetails();
}

function handleSearch() {
  if (currentGraph) {
    applyGraphSearch(graphCanvas, currentGraph, searchInput.value);
  }
}

function handleKeyDown(event) {
  if (event.key === "Escape") {
    clearSelection();
  }
}

function getGraphMeta(graph, sourceName) {
  const format = `${graph.format.name} ${graph.format.version}`;
  const source = graph.meta?.source?.name ? ` · Quelle: ${graph.meta.source.name}` : "";
  return `${format}${source} · ${sourceName}`;
}

function formatDetailsCode(value) {
  return value && Object.keys(value).length > 0 ? JSON.stringify(value, null, 2) : "Keine Angaben";
}
