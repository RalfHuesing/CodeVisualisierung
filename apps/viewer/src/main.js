import sampleGraph from "../../../contracts/graph-universe/fixtures/minimal.json";
import { EXAMPLE_CATALOG, getExampleGraph } from "./domain/catalog.js";
import { normalizeGraph, parseGraphText, validateGraph } from "./domain/graph.js";
import { createGraphRenderer } from "./rendering/visualization.js";
import { isWebGLSupported } from "./rendering/webgl.js";
import {
  filterGraph,
  getFilterOptions,
  getMetricLabel,
  getMetricNames,
  getNodeKindColor,
  getNodeNeighborhood,
  findLinkMetric,
  findNodeMetric
} from "./rendering/graph-mapping.js";

const fileInput = document.querySelector("#graph-file");
const dropZone = document.querySelector("#drop-zone");
const sampleButton = document.querySelector("#load-sample");
const detailsButton = document.querySelector("#toggle-details");
const closeDetailsButton = document.querySelector("#close-details");
const resetButton = document.querySelector("#reset-view");
const searchInput = document.querySelector("#node-search");
const nodeSelect = document.querySelector("#node-select");
const exampleSelect = document.querySelector("#example-select");
const nodeMetricSelect = document.querySelector("#node-metric-select");
const linkMetricSelect = document.querySelector("#link-metric-select");
const filterControls = {
  groupId: document.querySelector("#group-filter"),
  kind: document.querySelector("#kind-filter"),
  linkKind: document.querySelector("#link-kind-filter"),
  tag: document.querySelector("#tag-filter")
};
const resetFiltersButton = document.querySelector("#reset-filters");
const zoomSelect = document.querySelector("#zoom-select");
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
const legendNodeMetric = document.querySelector("#legend-node-metric");
const legendLinkMetric = document.querySelector("#legend-link-metric");
const legendKinds = document.querySelector("#legend-kinds");
const accessibleNodes = document.querySelector("#accessible-nodes");
const nodeCount = document.querySelector("#node-count");
const linkCount = document.querySelector("#link-count");
const selectedDetails = document.querySelector("#selected-node-details");
const graphRenderer = createGraphRenderer(graphCanvas, handleNodeClick);

let currentGraph;
let viewOptions = { filters: {} };

populateExampleSelector();
loadSampleGraph();

fileInput.addEventListener("change", handleFileSelection);
sampleButton.addEventListener("click", loadSampleGraph);
detailsButton.addEventListener("click", toggleGraphDetails);
closeDetailsButton.addEventListener("click", closeDetails);
resetButton.addEventListener("click", resetView);
searchInput.addEventListener("input", handleSearch);
nodeSelect.addEventListener("change", handleNodeSelection);
exampleSelect.addEventListener("change", handleExampleSelection);
nodeMetricSelect.addEventListener("change", handleMetricChange);
linkMetricSelect.addEventListener("change", handleMetricChange);
zoomSelect.addEventListener("change", handleZoomChange);
Object.values(filterControls).forEach((control) => control.addEventListener("change", handleFilterChange));
resetFiltersButton.addEventListener("click", resetFilters);
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
  status.textContent = "Graphdaten werden geladen …";
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

  exampleSelect.value = "minimal";
  showGraph(normalizeGraph(sampleGraph), "Beispieldaten");
}

function handleExampleSelection(event) {
  const graph = getExampleGraph(event.target.value);
  if (graph) {
    showGraph(normalizeGraph(graph), `Beispiel: ${event.target.options[event.target.selectedIndex].text}`);
  }
}

function showGraph(graph, sourceName) {
  currentGraph = graph;
  viewOptions = { filters: {}, linkMetric: findLinkMetric(graph), nodeMetric: findNodeMetric(graph) };
  zoomSelect.value = "detail";
  errorsPanel.hidden = true;
  errorList.replaceChildren();
  dropHint.hidden = true;
  selectedDetails.hidden = true;
  populateMetricSelectors(graph);
  populateFilterSelectors(graph);
  if (!isWebGLSupported(document)) {
    showErrors([{ kind: "webgl", path: "$", message: "Dieser Browser stellt keine unterstützte WebGL-Ansicht bereit." }]);
    return;
  }

  try {
    graphRenderer.render(graph, viewOptions);
  } catch {
    showErrors([{ kind: "webgl", path: "$", message: "Die 3D-Ansicht konnte nicht initialisiert werden." }]);
    return;
  }
  graphRenderer.focus(null);
  graphRenderer.search(searchInput.value);
  updateNodeSelector(graph);
  graphTitle.textContent = graph.meta?.title ?? sourceName;
  graphMeta.textContent = getGraphMeta(graph, sourceName);
  nodeCount.textContent = String(graph.nodes.length);
  linkCount.textContent = String(graph.links.length);
  updateLegend();
  updateAccessibleNodes();
  status.textContent = graph.nodes.length === 0 ? `${sourceName} geladen: Der Graph ist leer.` : `${sourceName} erfolgreich geladen.`;
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

function handleMetricChange(event) {
  viewOptions[event.target === nodeMetricSelect ? "nodeMetric" : "linkMetric"] = event.target.value || null;
  refreshGraphView();
}

function handleFilterChange() {
  const hiddenKinds = viewOptions.filters.hiddenKinds ?? [];
  viewOptions.filters = Object.fromEntries(
    Object.entries(filterControls).map(([name, control]) => [name, control.value]).filter(([, value]) => value)
  );
  if (hiddenKinds.length > 0) {
    viewOptions.filters.hiddenKinds = hiddenKinds;
  }
  refreshGraphView();
}

function handleZoomChange(event) {
  viewOptions.filters = { ...viewOptions.filters, hiddenKinds: event.target.value === "overview" ? ["method"] : [] };
  refreshGraphView();
}

function resetFilters() {
  Object.values(filterControls).forEach((control) => {
    control.value = "";
  });
  viewOptions.filters = {};
  zoomSelect.value = "detail";
  refreshGraphView();
}

function refreshGraphView() {
  if (!currentGraph) {
    return;
  }

  graphRenderer.updateOptions(viewOptions);
  const visibleGraph = filterGraph(currentGraph, viewOptions.filters);
  updateNodeSelector(visibleGraph);
  updateLegend();
  updateAccessibleNodes(visibleGraph);
  const zoomLabel = zoomSelect.value === "overview" ? "Übersicht" : "Detail";
  status.textContent = `${visibleGraph.nodes.length} von ${currentGraph.nodes.length} Nodes sichtbar · ${zoomLabel}.`;
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

function populateExampleSelector() {
  exampleSelect.replaceChildren();
  EXAMPLE_CATALOG.forEach((example) => {
    const option = document.createElement("option");
    option.value = example.id;
    option.textContent = `${example.label} · ${example.summary}`;
    exampleSelect.append(option);
  });
}

function populateMetricSelectors(graph) {
  populateSelect(nodeMetricSelect, getMetricNames(graph, "node"), findNodeMetric(graph), (metric) => getMetricLabel(graph, metric));
  populateSelect(linkMetricSelect, ["weight", ...getMetricNames(graph, "link")], findLinkMetric(graph), (metric) => getMetricLabel(graph, metric));
}

function populateFilterSelectors(graph) {
  const options = getFilterOptions(graph);
  populateSelect(filterControls.kind, options.kinds, "", (value) => value, "Alle Arten");
  populateSelect(filterControls.groupId, options.groups, "", (value) => value, "Alle Gruppen");
  populateSelect(filterControls.tag, options.tags, "", (value) => value, "Alle Tags");
  populateSelect(filterControls.linkKind, options.linkKinds, "", (value) => value, "Alle Link-Arten");
}

function populateSelect(select, values, selectedValue, label, emptyLabel = "Keine Auswahl") {
  select.replaceChildren();
  const defaultOption = document.createElement("option");
  defaultOption.value = "";
  defaultOption.textContent = emptyLabel;
  select.append(defaultOption);
  values.forEach((value) => {
    const option = document.createElement("option");
    option.value = value;
    option.textContent = label(value);
    option.selected = value === selectedValue;
    select.append(option);
  });
}

function updateLegend() {
  if (!currentGraph) {
    return;
  }

  legendNodeMetric.textContent = `Node-Größe: ${getMetricLabel(currentGraph, viewOptions.nodeMetric)}`;
  legendLinkMetric.textContent = `Linkbreite: ${getMetricLabel(currentGraph, viewOptions.linkMetric)}`;
  legendKinds.replaceChildren();
  [...new Set(currentGraph.nodes.map((node) => node.kind))].forEach((kind) => {
    const item = document.createElement("li");
    const sample = document.createElement("span");
    sample.className = "legend-swatch";
    sample.style.backgroundColor = getNodeKindColor({ kind }, currentGraph);
    item.append(sample, document.createTextNode(`${kind} · ${getShapeLabel(kind)}`));
    legendKinds.append(item);
  });
}

function getShapeLabel(kind) {
  return { class: "Würfel", file: "Zylinder", method: "Oktaeder", namespace: "Kugel" }[kind] ?? "Tetraeder";
}

function updateAccessibleNodes(graph = currentGraph) {
  accessibleNodes.replaceChildren();
  graph?.nodes.forEach((node) => {
    const item = document.createElement("li");
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = `${node.label} · ${node.kind}`;
    button.addEventListener("click", () => showNodeDetails(node));
    item.append(button);
    accessibleNodes.append(item);
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
