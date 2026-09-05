# Roadmap und technische Leitplanken

## Reihenfolge

### Phase 0 – Vertrag und Fixtures

- Graphformat 0.1 festlegen.
- JSON-Schema anlegen.
- kleine, mittlere und absichtlich fehlerhafte Beispieldateien anlegen.
- Normalisierung und Validierung als reine Funktionen definieren.

**Ergebnis:** Wir können Datenqualität testen, ohne einen Renderer zu benötigen.

### Phase 1 – Statische Browser-App

- HTML/CSS/TypeScript-App mit statischem Build.
- Datei-Upload und Drag-and-drop.
- JSON-Parsing, Schema-Validierung und Fehlermeldungen.
- Beispieldaten laden.

**Ergebnis:** Eine statisch hostbare Seite kann ein lokales Graph-JSON öffnen.

### Phase 2 – Erste 3D-Ansicht

- Three.js-basierter Renderer, zunächst mit `3d-force-graph`.
- Nodes, Links und Richtung darstellen.
- Kamera- und Resize-Verhalten.
- einfache, erklärbare Größen- und Breiten-Mappings.

**Ergebnis:** Der Graph ist navigierbar und nicht nur ein Screenshot.

### Phase 3 – Analyse der Ansicht

- Auswahl, Fokus und Nachbarschaftshervorhebung.
- Detailbereich als HTML neben der Szene.
- Suche, Reset und optionale Filter.
- Legende und aktive Metrik sichtbar machen.

**Ergebnis:** Die Visualisierung unterstützt konkrete Fragen an den Graphen.

### Phase 4 – Gruppen und Maßstab

- `groupId` für Cluster und semantischen Zoom verwenden.
- Nodes je nach Zoomstufe aggregieren oder ausblenden.
- Performance-Budgets mit realistischen Graphgrößen messen.
- 2D-Fallback oder Diagnoseansicht ergänzen.

**Ergebnis:** Größere Graphen bleiben untersuchbar.

### Phase 5 – C#-Exporter

- C#-Projekt analysieren.
- Klassen, Methoden und Beziehungen exportieren.
- Code-Metriken als benannte Metriken ergänzen.
- Export gegen dasselbe Schema und dieselben Fixtures testen.

**Ergebnis:** Der generische Viewer erhält eine erste reale Datenquelle.

### Phase 6 – Zeit und Livezustand

- Git-Metriken als zusätzliche Metriken.
- zeitabhängige Snapshots oder Deltas.
- Agenten- und Testlaufereignisse.
- Animation nur dort einsetzen, wo sie eine Änderung erklärt.

## Empfohlener Entwicklungsstack

- **TypeScript** für typisierte Datenmodelle und weniger Laufzeitfehler.
- **Vite** als Entwicklungs- und Buildwerkzeug; veröffentlicht wird nur das erzeugte statische `dist`-Verzeichnis.
- **Three.js** als 3D-Basis.
- **3d-force-graph** als erster Graph-Renderer, solange seine Abstraktion für das MVP ausreicht.
- **JSON Schema** für den Datenvertrag; eine kleine Browser-Validierungsschicht kann darauf aufbauen.
- **Vitest** für reine Logik und browsernahe Tests; zusätzlich ein kleiner End-to-End-Smoke-Test für Upload, Rendering und Auswahl.

Der Webspace braucht keinen Node-Prozess. Node ist nur eine mögliche Entwicklungsabhängigkeit für Build und Tests. Wenn auch lokal kein Node verwendet werden soll, kann später auf einen No-Build-Ansatz mit versionierten ES-Modulen umgestellt werden; das verschlechtert jedoch meist Reproduzierbarkeit und Testergonomie.

## Testpyramide

1. **Schema-Fixtures:** gültige Dokumente, fehlende Pflichtfelder, doppelte IDs, unbekannte Linkziele, extreme Werte.
2. **Pure Logic:** Normalisierung, Metric-Auswahl, Skalierung, Nachbarschaft und Filterung.
3. **Browser-Tests:** Datei laden, Fehlermeldung anzeigen, Szene initialisieren, Node auswählen, Reset ausführen.
4. **Visuelle Smoke-Checks:** WebGL-Kontext, Resize, leere Graphen, große aber noch unterstützte Fixtures.
5. **Performance-Messung:** dokumentierte Budgets statt pauschaler Versprechen über LOC.

## Offene Entscheidungen

- Wie viele Nodes muss der Vollmodus im ersten Zielbrowser flüssig darstellen?
- Ist 3D die Standardansicht und 2D nur Fallback, oder sind beide gleichwertige Modi?
- Soll `weight` standardmäßig die Linkbreite steuern oder nur als auswählbare Metrik gelten?
- Welche Gruppierungssemantik brauchen wir wirklich: nur `groupId`, oder später echte Container-Nodes?
- Werden Ansichtsprofile als Datei, URL-Fragment oder lokale Browsereinstellung gespeichert?
- Welche Browser und welche Mindest-WebGL-Fähigkeit gehören zum Supportziel?
