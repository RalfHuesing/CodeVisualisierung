# Konzept: Roadmap als autonomer Orchestrator-Task

## Auftrag

Der allgemeine 3D-Graph-Viewer wird schrittweise bis zum dokumentierten
Viewer-Abschluss umgesetzt. Der Viewer liest quellenneutrales Graph-JSON,
visualisiert Nodes und Links in 3D und leitet Filter, Detailstufen, Legenden
und Visualisierungstokens aus dem Graphvertrag ab.

Die spätere C#-Datenquelle ist ein Referenzfall für den Vertrag, aber kein Teil
der aktuellen Viewer-Implementierung.

## Quellen der Wahrheit

Die Orchestrierung ergänzt diese Quellen, ersetzt sie aber nicht:

1. `AGENTS.md` und `.agents/rules/` für Repository- und Arbeitsregeln
2. `docs/05-Roadmap.md` für Reihenfolge und Erledigungsstatus
3. die von der Roadmap referenzierten Fachdokumente für fachliche Entscheidungen
4. Tests und Fixtures für nachweisbares Verhalten
5. Git-Commits für den integrierten technischen Arbeitsstand

Dieses Konzept enthält keine zweite Kopie der Graphfachlichkeit. Wenn Konzept
und Roadmap abweichen, wird angehalten und die Richtungsentscheidung geklärt.

## Task-Scope und Abschluss

Der Task endet mit dem dokumentierten Viewer-Abschluss. Dafür arbeitet der
Orchestrator offene Punkte der Phasen 1, 3, 4 und 5 in der Roadmap ab, sofern
ihre Voraussetzungen erfüllt sind. Phase 6 (C#-Vorbereitung und Exporter) ist
als späterer, separater Auftrag ausdrücklich ausgeschlossen.

Der Task ist erst abgeschlossen, wenn:

- alle offenen Punkte innerhalb dieses Scopes erledigt und sachlich geprüft
  sind,
- die relevanten Tests und `npm run check` erfolgreich sind,
- `git diff --check` bestanden ist und alle vom Task erzeugten Änderungen
  committed sind.

Ein Label wie `A/1` bezeichnet höchstens einen internen Teilslice. Es ist keine
Task-Abschlussbedingung. Nach `A/1` folgt der nächste bereite Teilslice oder
Roadmap-Punkt.

## Ausführungsprinzip

Ein Orchestrator-Lauf bearbeitet den vollständigen Task. Slices begrenzen die
Delegation, die Review und die Commits, aber nicht den Task-Lauf:

1. Repository, Regeln, Roadmap und relevante Fachdokumente lesen
2. Task-Scope, Ausschlüsse und Abschlussbedingung bestimmen
3. nächsten offenen und voraussetzungsbereiten Slice bestimmen
4. Slice-Vertrag mit Ziel, Dateigrenzen, Akzeptanzkriterien und Checks anlegen
5. höchstens drei klar abgegrenzte Rollen beauftragen
6. Schreibaufgaben sequenziell integrieren
7. read-only Review durchführen
8. Findings höchstens zweimal korrigieren
9. vollständige Checks ausführen
10. nur erledigte Roadmap-Punkte markieren
11. einen fachlich eindeutigen Slice-Commit erstellen
12. Roadmap und Task-Scope erneut lesen und automatisch fortsetzen
13. erst nach Erfüllung der Task-Abschlussbedingung den Lauf beenden

Subagenten starten keine Subagenten, ändern keine Roadmap und committen nicht.
Reviewer ändern keine Dateien.

## Rollen

- `orchestrator`: Ziel, Reihenfolge, Delegation, Integration, Roadmap und Commit
- `implementer`: begrenzte Änderung innerhalb der erlaubten Dateien
- `reviewer`: read-only Prüfung gegen Auftrag, Architektur und Tests

Die Rollenverträge liegen unter `.agents/roles/`. Weitere Rollen werden nur
ergänzt, wenn eine konkrete fachliche Verantwortung wiederholt benötigt wird.

## Slices in Reihenfolge

### Slice A – Allgemeiner Graphvertrag 0.2

Deklarative `nodeTypes`, `linkTypes`, Facetten, Metrikdefinitionen,
View-Profile, Hierarchie-/Containment-Regeln, Summary-Links und
Visualisierungstokens in Schema, Fixtures und reiner Logik festlegen.

Referenz: `docs/03-Graphformat.md` und
`docs/06-Graphmodell-und-Visualisierungsprofile.md`.

### Slice B – Domänenneutrale Referenzgraphen

Familienstammbaum und Firmengeflecht als Nicht-Code-Graphen ergänzen und gegen
denselben Vertrag prüfen. Der bestehende C#-Referenzgraph bleibt separat.

Referenz: `docs/06-Graphmodell-und-Visualisierungsprofile.md`.

### Slice C – Schema-gesteuerte Viewer-Controls

Die aktuell vorhandenen Grundfilter, Metriken, Legenden und Detailstufen aus
den Graphdefinitionen erzeugen. Keine C#-Typen in den Viewer-Code einführen.

Referenz: `docs/02-Visualisierung.md` und
`docs/06-Graphmodell-und-Visualisierungsprofile.md`.

### Slice D – Projektionen und Summary-Links

Sichtbare Beziehungen bei ausgeblendeten Detailknoten korrekt erhalten. Die
erste Lösung bevorzugt explizite Summary-Links aus dem Graphen; automatische
Ableitung wird nur über deklarierte Regeln erlaubt.

Referenz: `docs/06-Graphmodell-und-Visualisierungsprofile.md`.

### Slice E – Qualitäts- und Skalierungsabschluss

Zielbrowser, Vollmodus, Zeit bis zum ersten Bild, Interaktionslatenz, FPS,
Speicherverhalten und Verhalten oberhalb der Grenze messen und dokumentieren.

Referenz: `docs/02-Visualisierung.md` und `docs/05-Roadmap.md`.

### Slice F – C#-Vorbereitung, später

Eine C#-Referenz-Fixture mit Assemblies, Projekten, Namespaces, Dateien, Typen,
Membern, externen Assemblies, Generics, Overloads und Summary-Links anlegen.
Ein Roslyn-Exporter folgt erst danach als eigener Auftrag.

Referenz: `docs/07-CSharp-Referenzgraph.md`.

## Review-Gates

Jeder Slice braucht vor dem Commit:

- fachliche Vollständigkeit gegen das referenzierte Dokument
- Architekturprüfung gegen `.agents/rules/`
- relevante Unit-, Fixture- und Browser-Tests
- `npm run check`
- `git diff --check` und sauberen `git status`

Ein Slice ist nur dann abgeschlossen, wenn alle Akzeptanzkriterien erfüllt
sind. Eine vorhandene Datei oder ein Teilfeature reicht nicht aus.

Ein Task ist nur dann abgeschlossen, wenn alle In-Scope-Slices und
Roadmap-Punkte erledigt sind. Ein erfolgreicher Slice-Commit ist lediglich ein
wiederaufnehmbarer Checkpoint.

## Stop-Bedingungen

Der Orchestrator beendet den Lauf erfolgreich erst nach Erfüllung der
Task-Abschlussbedingung. Er stoppt als blockiert, wenn:

- eine fachliche Richtungsentscheidung fehlt,
- ein externer Zugriff oder eine Berechtigung fehlt,
- derselbe Fehler nach zwei Reviewzyklen unverändert besteht,
- ein notwendiger Vorgänger-Slice noch nicht abgeschlossen ist.

Ein notwendiger Vorgänger beendet den Task nicht automatisch als blockiert,
wenn er innerhalb desselben Tasks noch bearbeitet werden kann. Nach jedem
erfolgreichen Slice startet der Orchestrator automatisch den nächsten bereiten
Slice. Nur ein nicht auflösbarer Vorgänger oder ein echter fachlicher Blocker
beendet den Lauf vorzeitig.
