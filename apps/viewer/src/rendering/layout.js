export const DEFAULT_LAYOUT_DISTANCE = 90;

const DEFAULT_GROUP_FIELD = "groupId";
const DEFAULT_GROUP_DISTANCE = DEFAULT_LAYOUT_DISTANCE;

export function prepareLayout(graph, viewProfile = null) {
  const layoutProfile = getActiveLayoutProfile(graph, viewProfile);
  const nodeMap = new Map(graph.nodes.map((node) => [node.id, node]));
  const positions = createDeterministicPositions(graph, layoutProfile);
  const links = graph.links.map((link) => ({
    ...link,
    distance: getLinkDistance(link, graph, layoutProfile, nodeMap)
  }));

  return {
    groupCount: new Set(graph.nodes.map((node) => getLayoutGroup(node, layoutProfile.groupField))).size,
    layoutProfileId: layoutProfile.id ?? "",
    links,
    nodes: graph.nodes.map((node) => ({
      ...node,
      ...positions.get(node.id)
    }))
  };
}

export function getActiveLayoutProfile(graph, viewProfile = null) {
  const profiles = graph.layoutProfiles ?? [];
  const requestedId = viewProfile?.layoutProfileId;
  return profiles.find((profile) => profile.id === requestedId)
    ?? profiles[0]
    ?? createDefaultLayoutProfile();
}

export function getLayoutGroup(node, groupField = DEFAULT_GROUP_FIELD) {
  const fieldValue = getPathValue(node, groupField);
  const value = fieldValue === undefined || fieldValue === null || fieldValue === "" ? node.groupId : fieldValue;
  return value === undefined || value === null || value === "" ? "ungrouped" : String(value);
}

export function getLinkDistance(link, graph, layoutProfile, nodeMap = new Map(graph.nodes.map((node) => [node.id, node]))) {
  const source = nodeMap.get(getEndpointId(link.source));
  const target = nodeMap.get(getEndpointId(link.target));
  if (!source || !target) {
    return DEFAULT_LAYOUT_DISTANCE;
  }

  if (isContainmentLink(link, graph)) {
    const containmentDistance = getContainmentDistance(link, source, target, layoutProfile);
    return getPositiveDistance(containmentDistance, getPositiveDistance(layoutProfile.defaultDistance, DEFAULT_LAYOUT_DISTANCE));
  }

  const sourceGroup = getLayoutGroup(source, layoutProfile.groupField);
  const targetGroup = getLayoutGroup(target, layoutProfile.groupField);
  if (sourceGroup !== targetGroup) {
    return getPositiveDistance(layoutProfile.groupDistance, DEFAULT_GROUP_DISTANCE);
  }

  return getPositiveDistance(layoutProfile.defaultDistance, DEFAULT_LAYOUT_DISTANCE);
}

export function createDeterministicPositions(graph, layoutProfile) {
  const positions = new Map();
  const groups = [...new Set(graph.nodes.map((node) => getLayoutGroup(node, layoutProfile.groupField)))].sort();
  const groupDistance = getPositiveDistance(layoutProfile.groupDistance, DEFAULT_GROUP_DISTANCE);
  const localSpacing = Math.min(
    getPositiveDistance(layoutProfile.defaultDistance, DEFAULT_LAYOUT_DISTANCE) / 3,
    groupDistance / 4
  );
  const groupCenters = new Map(groups.map((group, index) => [
    group,
    { x: (index - (groups.length - 1) / 2) * groupDistance, y: 0, z: 0 }
  ]));

  const basePositions = new Map();
  groups.forEach((group) => {
    graph.nodes
      .filter((node) => getLayoutGroup(node, layoutProfile.groupField) === group)
      .sort(compareNodes)
      .forEach((node, index) => {
        basePositions.set(node.id, getGridPosition(groupCenters.get(group), index, localSpacing));
      });
  });

  const containmentLinks = graph.links
    .filter((link) => isContainmentLink(link, graph))
    .sort(compareLinks);
  const parentLinks = new Map();
  containmentLinks.forEach((link) => {
    const childId = getEndpointId(link.target);
    const existing = parentLinks.get(childId);
    if (!existing || compareLinks(link, existing) < 0) {
      parentLinks.set(childId, link);
    }
  });
  const siblingIndexes = new Map();
  new Map([...parentLinks.values()].map((link) => [getEndpointId(link.source), []]))
    .forEach((children, parentId) => {
      [...parentLinks.entries()]
        .filter(([, link]) => getEndpointId(link.source) === parentId)
        .sort(([, left], [, right]) => compareLinks(left, right))
        .forEach(([childId], index) => siblingIndexes.set(`${parentId}\u0000${childId}`, index));
    });

  const nodeMap = new Map(graph.nodes.map((node) => [node.id, node]));
  const resolvePosition = (nodeId, path = new Set()) => {
    if (positions.has(nodeId)) {
      return positions.get(nodeId);
    }

    const basePosition = basePositions.get(nodeId);
    const parentLink = parentLinks.get(nodeId);
    if (!parentLink || path.has(nodeId)) {
      positions.set(nodeId, basePosition);
      return basePosition;
    }

    const parentId = getEndpointId(parentLink.source);
    const parentPosition = resolvePosition(parentId, new Set([...path, nodeId]));
    if (!parentPosition) {
      positions.set(nodeId, basePosition);
      return basePosition;
    }

    const parent = nodeMap.get(parentId);
    const child = nodeMap.get(nodeId);
    const distance = getPositiveDistance(
      getContainmentDistance(parentLink, parent, child, layoutProfile),
      getPositiveDistance(layoutProfile.defaultDistance, DEFAULT_LAYOUT_DISTANCE)
    );
    const index = siblingIndexes.get(`${parentId}\u0000${nodeId}`) ?? 0;
    const position = offsetPosition(parentPosition, distance, index);
    positions.set(nodeId, position);
    return position;
  };

  [...graph.nodes].sort(compareNodes).forEach((node) => resolvePosition(node.id));

  return positions;
}

function getContainmentDistance(link, source, target, layoutProfile) {
  if (!source || !target) {
    return undefined;
  }

  return (layoutProfile.containmentDistances ?? []).find((entry) => (
    entry.parentTypeId === getNodeType(source) && entry.childTypeId === getNodeType(target)
  ))?.distance;
}

function createDefaultLayoutProfile() {
  return {
    groupDistance: DEFAULT_GROUP_DISTANCE,
    groupField: DEFAULT_GROUP_FIELD,
    id: null
  };
}

function getGridPosition(center, index, spacing) {
  const column = index % 3 - 1;
  const row = Math.floor(index / 3);
  return {
    x: center.x + column * spacing,
    y: center.y + row * spacing,
    z: center.z + (index % 2 === 0 ? -spacing / 2 : spacing / 2)
  };
}

function offsetPosition(parent, distance, index) {
  const angle = (index % 8) * (Math.PI / 4);
  const depth = distance / 3;
  const radius = Math.sqrt(Math.max(0, distance ** 2 - depth ** 2));
  return {
    x: parent.x + Math.cos(angle) * radius,
    y: parent.y + Math.sin(angle) * radius,
    z: parent.z + (index % 2 === 0 ? depth : -depth)
  };
}

function isContainmentLink(link, graph) {
  return (graph.hierarchy?.containmentLinkTypes ?? []).includes(link.typeId ?? link.kind);
}

function getNodeType(node) {
  return node?.typeId ?? node?.kind;
}

function getEndpointId(endpoint) {
  return typeof endpoint === "object" ? endpoint.id : endpoint;
}

function getPathValue(value, path) {
  return path.split(".").reduce((current, segment) => current?.[segment], value);
}

function getPositiveDistance(value, fallback) {
  return Number.isFinite(value) && value > 0 ? value : fallback;
}

function compareNodes(left, right) {
  return left.id.localeCompare(right.id);
}

function compareLinks(left, right) {
  return (left.id ?? `${left.source}-${left.target}`).localeCompare(right.id ?? `${right.source}-${right.target}`);
}
