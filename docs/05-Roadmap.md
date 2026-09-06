# Roadmap

Diese Datei ist der schlanke Arbeitsindex. Fachliche Entscheidungen und
Akzeptanzkriterien stehen in den verlinkten Dokumenten und werden hier nicht
dupliziert.

## Leitplanken

- Der aktuelle Lieferumfang ist ein allgemeiner, statisch hostbarer 3D-Viewer.
- Der Viewer kennt keine C#-, Roslyn- oder andere Quelllogik.
- 2D-Ansichten und 2D-Fallbacks gehören nicht zum Produkt.
- Der aktuelle Viewer-MVP ist funktionsfähig; der allgemeine Vertrag ist noch
  nicht auf dem geplanten Stand 0.2.

Grundlagen: [Vision](01-Vision.md), [Visualisierung](02-Visualisierung.md),
[Graphformat](03-Graphformat.md), [Graphmodell und Visualisierungsprofile](06-Graphmodell-und-Visualisierungsprofile.md).

## Phase 0 – Bestand und Grundlagen

- [X] Produktvision und 3D-Leitentscheidung dokumentieren → [01](01-Vision.md), [02](02-Visualisierung.md)
- [X] Graphformat 0.1, Schema und reine Validierung anlegen → [03](03-Graphformat.md)
- [X] Statischen Viewer-MVP mit Upload, 3D-Szene, Auswahl und Details liefern → [Viewer](../apps/viewer/README.md)
- [X] Deterministische Tests und Größenlimits einrichten → [Viewer-Tests](../apps/viewer/tests/viewer.spec.js)

## Phase 1 – Allgemeiner Graphvertrag 0.2

- [X] `nodeTypes` und `linkTypes` als deklarative Definitionen festlegen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] Facetten, Filterquellen und benannte Metriken festlegen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] View-Profile und Detailstufen festlegen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] Hierarchie, Containment und Summary-Links festlegen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] Visualisierungstokens und Theme-Auflösung festlegen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] Schema, gültige Fixtures und ungültige Fixtures für 0.2 ergänzen → [03](03-Graphformat.md), [06](06-Graphmodell-und-Visualisierungsprofile.md)

## Phase 2 – Domänenneutrale Referenzdaten

- [X] Kleine, mittlere, große und deterministische Belastungs-Fixtures pflegen → [Fixtures](../contracts/graph-universe/fixtures)
- [X] Edge Cases für leere, isolierte, parallele und unvollständige Graphen pflegen → [Edge Fixture](../contracts/graph-universe/fixtures/edge-cases.json)
- [X] Einen Familienstammbaum als Nicht-Code-Graph ergänzen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] Ein Firmen- oder Beteiligungsgeflecht als Nicht-Code-Graph ergänzen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [ ] Eine C#-Referenz-Fixture für den späteren Exporter spezifizieren und anlegen → [07](07-CSharp-Referenzgraph.md)

## Phase 3 – Schema-gesteuerter Viewer

- [X] Mehrere Beispiele, Upload, Suche, Auswahl, Reset und Grundfilter anbieten → [Viewer](../apps/viewer/README.md)
- [X] Node-Arten mit Geometrie, Farbe, Metrik und sichtbarer Legende darstellen → [02](02-Visualisierung.md)
- [X] Filter und Facetten vollständig aus dem Graph-JSON erzeugen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] View-Profile aus dem Graph-JSON laden und auswählbar machen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] Summary-Links und Projektionen bei jeder Detailstufe korrekt darstellen → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] Unbekannte Typen und Visualisierungstokens mit dokumentiertem Fallback behandeln → [06](06-Graphmodell-und-Visualisierungsprofile.md)
- [X] C#-Begriffe vollständig aus dem Viewer-Code entfernen → [06](06-Graphmodell-und-Visualisierungsprofile.md)

## Phase 4 – Skalierung und Qualitätsgrenzen

- [X] Deterministische Aufbereitungs-Benchmarks für alle Ziel-Fixtures ausführen → [Visualisierung](02-Visualisierung.md)
- [X] Statische Produktionsausgabe bauen und Smoke-Tests gegen den Build ausführen → [Viewer](../apps/viewer/README.md)
- [X] Fehler-, Leer-, WebGL-, Resize- und große-Graph-Zustände testen → [Viewer-Tests](../apps/viewer/tests/viewer.spec.js)
- [ ] Zeit bis zum ersten sichtbaren Bild, Interaktionslatenz, FPS und Speicher im Zielbrowser messen → [Visualisierung](02-Visualisierung.md)
- [ ] Unterstützten interaktiven Vollmodus und Verhalten darüber festlegen → [Visualisierung](02-Visualisierung.md)
- [ ] Aggregation oder weitere Detailstufen nur aus den Messungen ableiten → [06](06-Graphmodell-und-Visualisierungsprofile.md)

## Phase 5 – Viewer-Abschluss

- [X] Bedienung, Graphformat, Beispiele und bekannte Grenzen dokumentieren → [Dokumentationsindex](README.md)
- [ ] Viewer-Version und fachlichen Meilenstein festlegen → [Visualisierung](02-Visualisierung.md)
- [ ] Alle offenen Punkte aus Phase 1, 3 und 4 abschließen → dieses Dokument

## Phase 6 – Spätere Datenquellen

- [ ] C#-/Roslyn-Exporter implementieren → [07](07-CSharp-Referenzgraph.md)
- [ ] C#-Graphen gegen Vertrag, Referenz-Fixture und Projektionen prüfen → [07](07-CSharp-Referenzgraph.md)
- [ ] Weitere Datenquellenprofile ergänzen, ohne den Viewer zu ändern → [06](06-Graphmodell-und-Visualisierungsprofile.md)

## Außerhalb des aktuellen Umfangs

- Git-Metriken und Zeitverläufe
- Live-Deltas und Agentenereignisse
- Animationen ohne erklärende fachliche Bedeutung
- Backend- oder Upload-Server

Diese Themen werden erst nach dem Viewer-Abschluss separat priorisiert.
