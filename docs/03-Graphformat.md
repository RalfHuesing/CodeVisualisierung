# Graphformat

## Zweck

Das Format ist der Vertrag zwischen jedem Datenquellen-Adapter und dem Viewer. Es beschreibt einen flachen Graphen: Nodes stehen unabhängig voneinander in einer Liste und verweisen über Links aufeinander. Eine Hierarchie ist optional und wird nicht vorausgesetzt.

Der erste Entwurf erhält eine Versionsnummer und darf sich weiterentwickeln. Der Viewer soll unbekannte optionale Felder tolerieren, aber unbekannte Pflichtsemantik nicht erraten.

## Entwurf `graph-universe` 0.1

```json
{
  "format": {
    "name": "graph-universe",
    "version": "0.1"
  },
  "meta": {
    "title": "Example system",
    "description": "Small demonstration graph",
    "source": {
      "name": "example-exporter",
      "version": "0.1.0"
    },
    "createdAt": "2026-09-05T12:00:00Z"
  },
  "metricDefinitions": {
    "loc": {
      "label": "Lines of code",
      "unit": "lines",
      "valueType": "number"
    },
    "complexity": {
      "label": "Cyclomatic complexity",
      "unit": "score",
      "valueType": "number"
    },
    "coverage": {
      "label": "Test coverage",
      "unit": "ratio",
      "valueType": "number"
    }
  },
  "nodes": [
    {
      "id": "orders.process-payment",
      "label": "ProcessPayment",
      "kind": "method",
      "groupId": "orders",
      "tags": ["public", "entrypoint"],
      "metrics": {
        "loc": 120,
        "complexity": 8,
        "coverage": 0.95
      },
      "attributes": {
        "visibility": "public"
      }
    }
  ],
  "links": [
    {
      "id": "orders.controller->orders.process-payment",
      "source": "orders.controller",
      "target": "orders.process-payment",
      "kind": "calls",
      "directed": true,
      "weight": 12,
      "metrics": {
        "callCount": 12
      },
      "attributes": {
        "crossGroup": true
      }
    }
  ]
}
```

## Semantik

### Format und Metadaten

- `format.name` identifiziert das Format.
- `format.version` ist die Version des Datenvertrags, nicht die Version der Webseite.
- `meta.title` und `meta.description` sind Präsentationsmetadaten.
- `meta.source` dokumentiert den Erzeuger, ohne ihn zur Voraussetzung des Viewers zu machen.
- `metricDefinitions` erklärt optionale Metriknamen, Einheiten und Datentypen.

### Nodes

- `id` ist innerhalb des Dokuments eindeutig und stabil.
- `label` ist die menschenlesbare Beschriftung; wenn sie fehlt, darf der Viewer die ID verwenden.
- `kind` beschreibt die fachliche Art des Nodes, zum Beispiel `class`, `method`, `file`, `service` oder `person`.
- `groupId` ist ein optionaler Gruppierungsschlüssel. Er ist keine implizite Verschachtelung.
- `tags` sind kurze kategorische Merkmale.
- `metrics` enthält benannte numerische Messwerte.
- `attributes` enthält zusätzliche, nicht zwingend numerische Eigenschaften.

### Links

- `source` und `target` referenzieren Node-IDs.
- Die Richtung ist immer `source` nach `target`; `directed` darf für ungerichtete Beziehungen `false` sein.
- `kind` beschreibt die Beziehung, zum Beispiel `calls`, `depends-on`, `contains` oder `related-to`.
- `weight` ist eine optionale allgemeine Beziehungsstärke. Wenn seine Bedeutung nicht aus dem Kontext klar ist, muss zusätzlich eine benannte Metrik verwendet werden.
- `metrics` enthält konkrete Beziehungswerte wie `callCount`, `bytes` oder `distance`.

## Invarianten für die Validierung

1. Das Dokument ist ein JSON-Objekt.
2. `format.name` und `format.version` sind vorhanden.
3. `nodes` und `links` sind Arrays.
4. Node-IDs sind nicht leer und eindeutig.
5. Jeder Link verweist mit `source` und `target` auf vorhandene Nodes.
6. Alle numerischen Metriken und Gewichte sind endlich; `NaN` und `Infinity` sind in JSON ohnehin nicht zulässig.
7. Ein fehlender optionaler Wert ist nicht dasselbe wie `0`.
8. Der Viewer darf keine fachliche Bedeutung aus dem Namen eines unbekannten Feldes ableiten.

## Bewusste Abgrenzungen

- Es gibt zunächst keine Pflicht zu `parentId` oder verschachtelten Objekten.
- Die Datenquelle legt keine konkrete Farbe, Geometrie oder Animation fest.
- Rohmetriken und normalisierte Anzeigegrößen bleiben getrennt.
- Ansichtsprofile gehören später in eine eigene Konfiguration oder in lokale Viewer-Einstellungen.
- Für die erste Version reicht ein JSON-Dokument; Streaming-Deltas kommen später.
