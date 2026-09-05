# Roadmap und technische Leitplanken

## Aktueller Lieferumfang

Der aktuelle Produktfokus ist die vollständige, allgemeingültige 3D-Visualisierung eines Graph-JSONs im Browser.

Zum aktuellen Ziel gehören:

- eine statisch hostbare Webseite ohne Backend,
- lokales Öffnen und Validieren von Graph-JSON,
- mehrere verständliche Beispiele von klein bis groß,
- eine untersuchbare 3D-Szene mit Auswahl, Suche, Filtern, Gruppen und Details,
- nachvollziehbare visuelle Kodierungen für Node- und Linkeigenschaften,
- deterministische Tests und messbare Grenzen für größere Graphen.

Nicht zum aktuellen Ziel gehören C#-, Roslyn- oder andere Datenquellen-Adapter. Sie dürfen den generischen Graphvertrag nicht mit Viewerlogik vermischen und werden erst nach Abschluss des Viewers begonnen.

## Definition of Done für den Viewer

Der Viewer ist für diesen Abschnitt fertig, wenn eine Person ohne technische Kenntnisse:

1. ein Beispiel oder eine lokale JSON-Datei öffnen kann,
2. auch bei einem größeren Graphen die Struktur in einer reinen 3D-Ansicht erkennt,
3. Node-Arten, Gruppen, Richtungen und aktive Metriken unterscheiden kann,
4. einen Node findet, fokussiert, seine Nachbarschaft und Details untersucht,
5. die Darstellung filtern, zurücksetzen und verständlich bedienen kann,
6. bei ungültigen Daten oder fehlender WebGL-Unterstützung eine klare Rückmeldung erhält.

Jede dieser Aussagen braucht mindestens einen automatisierten Test; Größen- und Performanceaussagen brauchen reproduzierbare Messungen.

## Reihenfolge

### Phase 0 – Vertrag und Fixtures

- [X] Graphformat 0.1 festlegen.
- [X] JSON-Schema anlegen.
- [X] Kleine, mittlere, große und absichtlich fehlerhafte Beispieldateien anlegen.
- [X] Die Beispiele fachlich wie einen Codegraphen modellieren: Namespaces, Klassen, Methoden, Dateien und typische Beziehungen.
- [X] Eine deterministisch erzeugte Belastungs-Fixture für Performance-Tests ergänzen.
- [X] Normalisierung und Validierung als reine Funktionen definieren.

**Ergebnis:** Wir können Datenqualität testen, ohne einen Renderer zu benötigen.

### Phase 1 – Statische Browser-App

- [X] HTML/CSS/JavaScript-App mit statischem Build.
- [X] Datei-Upload und Drag-and-drop.
- [X] JSON-Parsing, Schema-Validierung und Fehlermeldungen.
- [X] Beispieldaten laden.

**Ergebnis:** Eine statisch hostbare Seite kann ein lokales Graph-JSON öffnen.

### Phase 2 – Erste 3D-Ansicht

- [X] Three.js-basierter 3D-Renderer mit `3d-force-graph`.
- [X] Nodes, Links und Richtung in 3D darstellen.
- [X] Kamera- und Resize-Verhalten.
- [X] Einfache, erklärbare Größen- und Breiten-Mappings.
- [X] Lade-, Leer-, Fehler- und WebGL-Fehlerzustände vollständig als 3D-Viewer-Zustände behandeln.
- [ ] Renderer-Verhalten bei Node-, Link- und Gruppenzahlen aus den Ziel-Fixtures verifizieren.

**Ergebnis:** Der Graph ist navigierbar und nicht nur ein Screenshot.

### Phase 3 – Verstehen und Bedienen

- [X] Auswahl, Fokus und Nachbarschaftshervorhebung.
- [X] Detailbereich als HTML über der Szene.
- [X] Suche und Reset.
- [X] Mehrere mitgelieferte Beispiele über einen zentralen Beispielkatalog auswählen.
- [X] Optionale Filter nach Node-Art, Gruppe, Tag und Link-Art.
- [X] Legende und aktive Metrik sichtbar machen.
- [X] Eine zentrale, explizite Auswahl der Node- und Linkmetriken ermöglichen.
- [X] Node-Arten mit mehr als Farbe unterscheiden, sofern der Renderer das ohne unnötige Komplexität unterstützt.
- [X] Tastaturbedienung, Fokuszustände und sinnvolle zugängliche HTML-Alternativen für die 3D-Szene ergänzen.
- [ ] Lade-, leere, ungültige und nicht unterstützte Zustände im Browser testen.

**Ergebnis:** Die Visualisierung unterstützt konkrete Fragen an den Graphen.

### Phase 4 – Realistische Beispiele und semantische Orientierung

- [X] Eine kleine Fixture mit wenigen Namespace-, Klassen- und Methoden-Nodes als Referenzbeispiel pflegen.
- [X] Eine mittlere Fixture mit mehreren Bereichen und gerichteten Abhängigkeiten pflegen.
- [X] Eine große Fixture mit vielen Methoden, Klassen, Namespaces, Tags und benannten Metriken pflegen.
- [X] `groupId` für Cluster und semantische Orientierung verwenden, ohne verschachtelte JSON-Strukturen vorauszusetzen.
- [X] Gruppen sichtbar und ein-/ausblendbar machen.
- [X] Semantischen Zoom mit klarer UI-Rückmeldung ergänzen.
- [X] Je nach Zoomstufe Nodes aggregieren oder ausblenden, ohne Daten stillschweigend zu löschen.
- [ ] Ausgewählte Beziehungen und Gruppen auch in dichter 3D-Darstellung nachvollziehbar halten.

**Ergebnis:** Größere Graphen bleiben untersuchbar.

### Phase 5 – Skalierung und Robustheit

- [ ] Performance-Budgets mit den kleinen, mittleren, großen und deterministischen Belastungs-Fixtures messen.
- [ ] Zeit bis zum ersten sichtbaren Bild, Interaktionslatenz, Bildrate und Speicherverhalten dokumentieren.
- [ ] Einen unterstützten interaktiven Vollmodus und das Verhalten darüber hinaus festlegen.
- [ ] Aggregation, Detailstufen oder vorgegebene Positionen für große Graphen einsetzen, falls die Messungen es erfordern.
- [ ] Extremwerte, fehlende Metriken, isolierte Nodes, parallele Links und leere Graphen verlässlich darstellen.
- [ ] Browser-Smoke-Tests für Resize, WebGL-Kontext, große Fixture und Reset stabilisieren.

**Ergebnis:** Der Viewer hat nachvollziehbare Grenzen und verhält sich auch bei ungewöhnlichen Daten zuverlässig.

### Phase 6 – Viewer-Abschluss

- [ ] Statische Produktionsausgabe bauen und auf einem einfachen Webspace verifizieren.
- [ ] Alle Nutzerpfade aus der Definition of Done als End-to-End-Szenarien abdecken.
- [ ] Dokumentation für Graphformat, Beispiele, Bedienung und bekannte Grenzen vervollständigen.
- [ ] Abhängigkeiten und Bundlegröße prüfen; nur bei messbarem Mehrwert optimieren.
- [ ] Viewer-Version als fachlich abgeschlossenen Meilenstein markieren.

**Ergebnis:** Die allgemeingültige 3D-Visualisierung ist als eigenständiges Produkt nutzbar.

### Phase 7 – Spätere Datenquellen, außerhalb des aktuellen Ziels

- [ ] C#-Projekt analysieren.
- [ ] Klassen, Methoden und Beziehungen exportieren.
- [ ] Code-Metriken als benannte Metriken ergänzen.
- [ ] Export gegen dasselbe Schema und dieselben Fixtures testen.

**Ergebnis:** Der generische Viewer erhält eine erste reale Datenquelle.

### Phase 6 – Zeit und Livezustand

- [ ] Git-Metriken als zusätzliche Metriken.
- [ ] Zeitabhängige Snapshots oder Deltas.
- [ ] Agenten- und Testlaufereignisse.
- [ ] Animation nur dort einsetzen, wo sie eine Änderung erklärt.

## Empfohlener Entwicklungsstack

- **Plain modernes JavaScript** für einen kleinen, gut lesbaren Einstieg. Typisierung wird erst ergänzt, wenn der konkrete Code davon profitiert.
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

## Offene Entscheidungen und Defaults

- [ ] Wie viele Nodes muss der Vollmodus im ersten Zielbrowser flüssig darstellen? Bis zur Messung gelten kleine, mittlere und große Fixtures als getrennte Zielklassen; es wird kein pauschales LOC-Versprechen abgegeben.
- [X] 3D ist die einzige Produktansicht; einen 2D-Fallback bauen wir nicht.
- Als Default steuert `weight` zunächst die Linkbreite; benannte Linkmetriken werden auswählbar, sobald die UI dafür existiert.
- Die Gruppierung basiert zunächst nur auf `groupId`; echte Container-Nodes sind nicht erforderlich.
- Ansichtsprofile werden nicht vor dem Viewer-Abschluss persistiert; ein Speicherformat wird erst bei einem konkreten Bedarf entschieden.
- Der erste Supportkorridor ist ein aktueller Desktop-Browser mit WebGL2; die konkrete Browsermatrix wird vor Phase 6 festgeschrieben.
