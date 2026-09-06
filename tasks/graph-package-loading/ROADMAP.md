# Umsetzungs-Roadmap: Graph-Pakete für große lokale Projekte

Diese Roadmap ist der interne Orchestrator-Arbeitsvertrag. Der globale Status
steht ausschließlich in `docs/05-Roadmap.md`.

## Taskvertrag

```text
Task:
  Eine lokal erzeugte einzelne .graphpack-Datei im statischen Viewer lazy laden
  und daraus große Projekte über fachliche Teilgraphen erkundbar machen.

Task scope:
  Package-Manifest, CLI-Paketerzeugung, lokale Browser-Dateiauswahl,
  selektives Archivlesen, Overview- und Detail-Ladefluss, Sitzungscache,
  Status-/Fehlerdarstellung und relevante Tests.

Explicit exclusions:
  Backend, Netzwerk-Upload, automatische Browser-Aufteilung einer monolithischen
  JSON-Datei, gleichzeitiges Rendern des vollständigen Großgraphen,
  neue C#-Analysefeatures und dauerhafte Browserablage ohne gesonderte Entscheidung.

Task completion condition:
  Ein einzelner .graphpack-Upload aus einem Großprojekt zeigt den Overview-
  Graphen und lädt mindestens eine weitere Detailstufe nach Bedarf. Die gesamte
  Roh-JSON wird im Viewer nicht als ein Graph geladen. Vertrag, Nicht-Ziele,
  Tests und Messgrenzen sind konsistent dokumentiert.
```

## Slice 0 – Paket- und Teilgraphvertrag

Ziel: Archivformat, Manifest, Teilgraphgrenzen, Kompatibilität und
Beziehungsgrenzen dokumentieren, bevor CLI oder Viewer implementiert werden.

Akzeptanzkriterien:

- [x] Manifestfelder und Paketversion sind als Vertragsumfang festgelegt.
- [x] ZIP-/`.graphpack`-Verhalten ist festgelegt.
- [x] Teilgraphgranularität und globale ID-Regeln sind festgelegt.
- [x] Verhalten von Links an Teilgraphgrenzen ist festgelegt.
- [x] Kompatibilität mit kleinen direkten `.json`-Dateien ist entschieden.
- [x] OPEN-QUESTIONS.md dokumentiert die geschlossenen Entscheidungen.

Checks: Dokumentenreview, `git diff --check`.

## Slice 1 – CLI-Paketerzeugung

Ziel: Der C#-Adapter erzeugt aus einer Solution ein einzelnes validiertes
`.graphpack`-Artefakt mit Manifest und mindestens Overview-/Detaildaten.

Akzeptanzkriterien:

- [ ] Paketexport ist über einen dokumentierten CLI-Aufruf möglich.
- [ ] Ausgabe wird atomar und ohne halbfertiges Zielpaket geschrieben.
- [ ] Analyse- und Diagnosestatus werden im Manifest erhalten.
- [ ] IDs, Metriken, Profile und Beziehungen bleiben deterministisch.
- [ ] Bestehende JSON-Ausgabe bleibt gemäß der Entscheidung kompatibel.
- [ ] Kleine deterministische Paket-Fixtures und CLI-Tests decken den Vertrag ab.

Checks: betroffene .NET-Tests, Schema-/Fixtureprüfung, Paketinspektion.

Abhängigkeit: Slice 0.

## Slice 2 – Viewer-Paketloader

Ziel: Der statische Viewer kann eine lokal ausgewählte `.graphpack`-Datei öffnen,
Manifest und Overview lesen und verständliche Ladefehler anzeigen.

Akzeptanzkriterien:

- [ ] Der Benutzer wählt genau eine Paketdatei aus.
- [ ] Die Webseite verwendet keinen Upload-Endpoint.
- [ ] Manifest und Overview werden selektiv gelesen.
- [ ] Eine vollständige monolithische JSON-Interpretation ist für den
      Paketpfad ausgeschlossen.
- [ ] Der direkte JSON-Pfad bleibt gemäß der Entscheidung funktionsfähig.

Checks: Unit-Tests für Manifest-/Loaderlogik, Browser-Smoke-Tests mit kleiner
Paket-Fixture und `npm run check`.

Abhängigkeit: Slice 1.

## Slice 3 – Lazy Detailnavigation

Ziel: Der Viewer lädt weitere Teilgraphen bei fachlicher Navigation und hält
bereits geladene Teilgraphen innerhalb der Sitzung wiederverwendbar.

Akzeptanzkriterien:

- [ ] Overview-Navigation kann mindestens eine weitere Detailstufe laden.
- [ ] Ladezustände, fehlende Einträge und inkompatible Pakete werden sichtbar.
- [ ] Globale Node-IDs und Auswahlzustände bleiben nachvollziehbar.
- [ ] Der Sitzungscache verhindert unnötiges erneutes Lesen.
- [ ] Der Viewer verspricht keine Vollansicht außerhalb der gemessenen Grenzen.

Checks: Browser-Tests für Navigation, Cache und Fehlerfälle; Messung mit einer
repräsentativen großen Paketstruktur.

Abhängigkeit: Slice 2 und Slice 0 für Teilgraphgrenzen.

## Slice 4 – Großprojektvalidierung und Abschluss

Ziel: Das Paketmodell wird mit den realen Größenordnungen validiert, ohne die
großen Quelldateien oder Graphdateien in Fixtures zu committen.

Akzeptanzkriterien:

- [ ] Package-Erzeugung und Overview-Laden werden mit dem 400-MB-Ausgangsfall
      beziehungsweise einer reproduzierbaren lokalen Messung geprüft.
- [ ] Speicher- und Zeitverhalten der selektiven Ladepfade sind dokumentiert.
- [ ] Keine Build-, Cache-, Coverage- oder privaten Graphartefakte werden
      committed.
- [ ] Konzept, CLI-Dokumentation, Viewer-Dokumentation und Roadmap widersprechen
      sich nicht.
- [ ] Relevante Tests, `npm run check`, `git diff --check` und Review bestehen.
- [ ] Der globale Taskeintrag wird nach Abschluss entfernt.

Checks: vollständiger Repository-Check, lokale Messung und read-only Review.

Abhängigkeit: Slices 0–3.

## Orchestrator-Regel

Nur der Orchestrator aktualisiert den globalen Roadmap-Status und erstellt
Commits. Ein Slice-Commit ist ein Checkpoint und nicht automatisch der
Taskabschluss.
