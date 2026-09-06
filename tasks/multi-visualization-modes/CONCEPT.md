# Konzept: Mehrere Visualisierungsmodi

## Auftrag

Der generische Viewer soll dieselbe validierte Graph-JSON in mehreren
Darstellungsmodi anzeigen können. Der Nutzer kann zwischen einer Stadtkarte,
dem bestehenden 3D-Universum und einer optionalen biologischen
Netzwerkansicht wechseln.

Die Modi sind alternative Renderer und Layouts, keine zusätzlichen
Datenquellen und keine separaten Graphmodelle.

## Quellen der Wahrheit

1. `AGENTS.md` und `.agents/rules/` für Repository- und Workflow-Regeln
2. `docs/08-Mehrere-Visualisierungsmodi.md` für die fachliche Zielidee
3. `contracts/graph-universe/` für den gemeinsamen Datenvertrag
4. diese Task-Dateien für Scope, Slices und Abschlusskriterien
5. Tests und Git-Commits für nachweisbares Verhalten

## Scope

- gemeinsame Renderer-Schnittstelle
- Auswahl und Umschaltung eines Visualisierungsmodus
- stabile Übernahme von Auswahl, Suche, Filtern und Metriken
- prototypische Stadtkarte als hierarchische 2D-Ansicht
- Erhalt und Regressionstest des bestehenden Universumsmodus
- Evaluation eines biologischen Myzel-/Signalprofils
- eigene Legende und verständliche Einschränkungen pro Modus

## Explizite Ausschlüsse

- keine C#-, Roslyn- oder quellenbezogene Logik im Viewer
- keine Änderung der Rohdatenstruktur nur für einen Renderer
- kein renderer-spezifischer JavaScript-Code in Graph-JSON
- keine automatische Ableitung unbekannter fachlicher Bedeutungen
- keine Animation, die statische Daten als Laufzeitaktivität ausgibt
- keine ungemessenen Versprechen für beliebig große Graphen

## Zielarchitektur

```text
Graph-JSON
  → Validierung und Normalisierung
  → gemeinsame semantische Graphaufbereitung
  → gewähltes Visualisierungsprofil
  → Layout und Renderer
```

Der aktuelle Renderer definiert bereits die wesentlichen Operationen:

```text
render(graph, options)
updateOptions(options)
focus(nodeId)
reset()
destroy()
```

Die gemeinsame Aufbereitung liefert validierte Nodes, Links, Rollen, Metriken,
Filter und stabile IDs. Renderer-spezifische Zustände wie Kamera, 2D-Positionen
und Kantenführung bleiben getrennt.

## Fachliche Modi

### Stadtkarte

Containment-Beziehungen bilden stabile, verschachtelte Stadtteile. Projekte,
Assemblies oder Namespaces werden gruppiert; Typen erscheinen als Gebäude.
Querbeziehungen werden als Straßen oder Transitlinien außerhalb der Container
dargestellt. Die Ansicht ist primär für Architektur und Orientierung gedacht.

### Universum

Der bestehende 3D-Force-Renderer bleibt als freie Explorationsansicht erhalten.
Räumliche Nähe ist weiterhin keine fachliche Beziehung.

### Biologie

Eine organische Ansicht kann Cluster und Kopplung als Myzel darstellen oder
gerichtete Call-Flows als Nervensignale. Sie ist optional und muss ihre
Metapher sowie alle aktiven Metriken in einer Legende erklären.

## Abschlussbedingung

Der Task ist abgeschlossen, wenn:

- mindestens zwei Modi dieselbe Graph-JSON ohne Duplizierung visualisieren,
- Auswahl, Suche, Filter, Metriken und Details zwischen Modi erhalten bleiben,
- der Universumsmodus weiterhin funktioniert,
- die Stadtkarte Hierarchie und Querbeziehungen verständlich darstellt,
- jeder Modus eigene visuelle Kodierungen und eine Legende besitzt,
- ungeeignete oder eingeschränkte Modi sichtbar erklärt werden,
- relevante Tests, `npm run check` und `git diff --check` erfolgreich sind.
