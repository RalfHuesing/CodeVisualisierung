# Graphformat

## Zweck

`graph-universe` 1.0 ist der verbindliche Vertrag zwischen Datenquellen und
Viewer. Das Dokument beschreibt einen flachen Graphen: Nodes stehen in einer
Liste und Links verweisen über stabile IDs aufeinander. Eine Hierarchie ist
optional und wird über deklarierte Typen und Beziehungen beschrieben.

## Kanonischer Rahmen

Jedes Graphdokument enthält mindestens:

```json
{
  "format": {
    "name": "graph-universe",
    "version": "1.0"
  },
  "nodes": [],
  "links": []
}
```

`format.version` ist die Version des Datenvertrags, nicht die Version des
Viewers. `nodeTypes` und `linkTypes` sind, sofern vorhanden, ausschließlich
Arrays deklarativer Definitionen. Jede Definition trägt ihre ID im Feld `id`.

## Semantik

### Format und Metadaten

- `format.name` ist immer `graph-universe`.
- `format.version` ist immer `1.0`.
- `meta.title` und `meta.description` sind optionale Präsentationsmetadaten.
- `meta.source` dokumentiert den Erzeuger, ohne ihn zur Voraussetzung des
  Viewers zu machen.
- `metricDefinitions` erklärt optionale Metriknamen, Einheiten und Typen.

### Nodes

- `id` ist innerhalb des Dokuments eindeutig und stabil.
- `typeId` referenziert eine Definition aus `nodeTypes`, sofern das Dokument
  Node-Typen deklariert.
- `label` ist die menschenlesbare Beschriftung; fehlt sie, verwendet der
  Viewer die ID.
- `groupId`, `tags`, `metrics` und `attributes` sind optionale allgemeine
  Eigenschaften.

### Links

- `source` und `target` referenzieren Node-IDs.
- Die Richtung ist `source` nach `target`; `directed` darf für ungerichtete
  Beziehungen `false` sein.
- `typeId` referenziert eine Definition aus `linkTypes`, sofern das Dokument
  Beziehungstypen deklariert.
- `weight`, `metrics`, `summary` und `derivedFrom` dokumentieren optionale
  Stärke, Herkunft und Aggregation einer Beziehung.

### Optionale deklarative Bereiche

Das Schema unterstützt zusätzlich `facets`, `filterSources`, `viewProfiles`,
`projections`, `containmentRules`, `hierarchy`, `visualTokens` und `theme`.
Diese Bereiche beschreiben Daten und Darstellungsregeln explizit. Der Viewer
errät keine fachliche Bedeutung aus unbekannten Feldnamen.

## Invarianten für die Validierung

1. Das Dokument ist ein JSON-Objekt mit `format`, `nodes` und `links`.
2. `format.name` ist `graph-universe` und `format.version` ist `1.0`.
3. `nodes` und `links` sind Arrays.
4. Node-IDs sind nicht leer und eindeutig.
5. Jeder Link verweist mit `source` und `target` auf vorhandene Nodes.
6. Definierte Node- und Linktypen sind Arrays mit eindeutigen `id`-Feldern.
7. Alle numerischen Metriken und Gewichte sind endlich.
8. Fehlende optionale Werte sind nicht dasselbe wie `0`.

## Bewusste Abgrenzungen

- Es gibt keine Pflicht zu verschachtelten Objekten oder `parentId`.
- Die Datenquelle legt keine konkrete Farbe, Geometrie oder Animation fest.
- Rohmetriken und normalisierte Anzeigegrößen bleiben getrennt.
- Ein JSON-Dokument ist die Eingabegrenze; Streaming-Deltas gehören nicht zum
  aktuellen Vertrag.

Die fachlichen Regeln für Profile, Projektionen und Visualisierungstokens
stehen in [06 – Graphmodell und Visualisierungsprofile](06-Graphmodell-und-Visualisierungsprofile.md).
