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
  getNodeTypeLabel,
  getNodeVisualStyle,
  getNodeNeighborhood,
  getDefaultViewProfile,
  getViewProfile,
  getViewProfiles,
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
const filterControlsContainer = document.querySelector("#filter-controls");
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
const FULL_MODE_LIMIT = Object.freeze({ nodes: 248, links: 448 });

let currentGraph;
let filterControls = new Map();
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
zoomSelect.addEventListener("change", handleProfileChange);
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
  const profile = getDefaultViewProfile(graph);
  viewOptions = {
    filters: {},
    linkMetric: profile?.linkMetric ?? findLinkMetric(graph),
    nodeMetric: profile?.nodeMetric ?? findNodeMetric(graph),
    profile,
    profileId: profile?.id ?? ""
  };
  zoomSelect.value = profile?.id ?? "";
  errorsPanel.hidden = true;
  errorList.replaceChildren();
  dropHint.hidden = true;
  selectedDetails.hidden = true;
  populateProfileSelector(graph);
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
  status.textContent = getLoadStatus(graph, sourceName);
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
  viewOptions.filters = Object.fromEntries(
    [...filterControls.entries()].map(([name, control]) => [name, control.value]).filter(([, value]) => value)
  );
  refreshGraphView();
}

function handleProfileChange(event) {
  const profile = getViewProfile(currentGraph, event.target.value);
  viewOptions = {
    ...viewOptions,
    linkMetric: profile?.linkMetric ?? findLinkMetric(currentGraph),
    nodeMetric: profile?.nodeMetric ?? findNodeMetric(currentGraph),
    profile,
    profileId: profile?.id ?? ""
  };
  populateMetricSelectors(currentGraph);
  refreshGraphView();
}

function resetFilters() {
  [...filterControls.values()].forEach((control) => {
    control.value = "";
  });
  viewOptions.filters = {};
  refreshGraphView();
}

function refreshGraphView() {
  if (!currentGraph) {
    return;
  }

  graphRenderer.updateOptions(viewOptions);
  const visibleGraph = filterGraph(currentGraph, viewOptions.filters, viewOptions.profile);
  const selectedNodeId = nodeSelect.value;
  updateNodeSelector(visibleGraph);
  if (selectedNodeId && !visibleGraph.nodes.some((node) => node.id === selectedNodeId)) {
    clearSelection();
  }
  updateLegend();
  updateAccessibleNodes(visibleGraph);
  const profileLabel = viewOptions.profile?.label ?? "Standard";
  status.textContent = `${visibleGraph.nodes.length} von ${currentGraph.nodes.length} Nodes sichtbar · ${profileLabel}.${getScaleStatus(currentGraph)}`;
}

function handleKeyDown(event) {
  if (event.key === "Escape") {
    clearSelection();
  }
}

function updateNodeSelector(graph) {
  const selectedNodeId = nodeSelect.value;
  const defaultOption = document.createElement("option");
  defaultOption.value = "";
  defaultOption.textContent = "Node auswählen";
  nodeSelect.replaceChildren(defaultOption);
  graph.nodes.forEach((node) => {
    const option = document.createElement("option");
    option.value = node.id;
    option.textContent = node.label ?? node.id;
    option.selected = node.id === selectedNodeId;
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
  populateSelect(nodeMetricSelect, getMetricNames(graph, "node"), viewOptions.nodeMetric, (metric) => getMetricLabel(graph, metric));
  populateSelect(linkMetricSelect, ["weight", ...getMetricNames(graph, "link")], viewOptions.linkMetric, (metric) => getMetricLabel(graph, metric));
}

function populateFilterSelectors(graph) {
  filterControls = new Map();
  filterControlsContainer.replaceChildren();
  getFilterOptions(graph).sources.forEach((source) => {
    const label = document.createElement("label");
    label.className = "filter-picker";
    label.htmlFor = `${source.id}-filter`;
    label.textContent = source.label;
    const select = document.createElement("select");
    select.id = `${source.id}-filter`;
    select.setAttribute("aria-label", source.label);
    populateSelect(select, source.values, "", (value) => value, `Alle ${source.label}`);
    select.addEventListener("change", handleFilterChange);
    label.append(select);
    filterControlsContainer.append(label);
    filterControls.set(source.id, select);
  });
}

function populateProfileSelector(graph) {
  populateSelect(
    zoomSelect,
    getViewProfiles(graph).map((profile) => profile.id),
    viewOptions.profileId,
    (profileId) => getViewProfile(graph, profileId)?.label ?? profileId,
    "Ansicht auswählen"
  );
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
  const types = new Map(currentGraph.nodes.map((node) => [node.typeId ?? node.kind, node]));
  types.forEach((node) => {
    const item = document.createElement("li");
    const sample = document.createElement("span");
    sample.className = "legend-swatch";
    sample.style.backgroundColor = getNodeKindColor(node, currentGraph);
    item.append(sample, document.createTextNode(`${getNodeTypeLabel(node, currentGraph)} · ${getShapeLabel(node)}`));
    legendKinds.append(item);
  });
}

function getShapeLabel(node) {
  return getNodeVisualStyle(node, currentGraph).shape;
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
  return `${format}${source} · ${sourceName}${getScaleStatus(graph)}`;
}

function getLoadStatus(graph, sourceName) {
  if (graph.nodes.length === 0) {
    return `${sourceName} geladen: Der Graph ist leer.`;
  }
  return `${sourceName} erfolgreich geladen.${getScaleStatus(graph)}`;
}

function getScaleStatus(graph) {
  const exceedsFullMode = graph.nodes.length > FULL_MODE_LIMIT.nodes || graph.links.length > FULL_MODE_LIMIT.links;
  if (exceedsFullMode) {
    return ` Außerhalb des geprüften interaktiven Vollmodus (Grenze: ${FULL_MODE_LIMIT.nodes} Nodes / ${FULL_MODE_LIMIT.links} Links); weiterhin ladbar.`;
  }
  if (graph.nodes.length === FULL_MODE_LIMIT.nodes && graph.links.length === FULL_MODE_LIMIT.links) {
    return ` Interaktiver Vollmodus geprüft (Grenze: ${FULL_MODE_LIMIT.nodes} Nodes / ${FULL_MODE_LIMIT.links} Links).`;
  }
  return "";
}

function formatDetailsCode(value) {
  return value && Object.keys(value).length > 0 ? JSON.stringify(value, null, 2) : "Keine Angaben";
}
