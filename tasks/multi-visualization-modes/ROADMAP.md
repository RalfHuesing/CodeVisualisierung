# Umsetzungs-Roadmap: Mehrere Visualisierungsmodi

Diese Roadmap ist der interne Orchestrator-Arbeitsvertrag. Sie enthält die
Details der Slices; der globale Status steht ausschließlich in
`docs/05-Roadmap.md`.

## Taskvertrag

```text
Task:
  Mehrere Renderer und Visualisierungsmodi für dieselbe Graph-JSON bauen.

Task scope:
  Gemeinsame Renderer-Schnittstelle, Modusumschaltung, Stadtkarte,
  Universums-Regression und Evaluation einer biologischen Ansicht.

Explicit exclusions:
  C#-Logik, Backend/Live-Modus, neue Rohdatenmodelle und nicht begründete
  Laufzeitanimationen.

Task completion condition:
  CONCEPT.md ist umgesetzt, mindestens zwei Modi funktionieren mit derselben
  Graph-JSON, die gemeinsamen Zustände bleiben erhalten, relevante Checks
  bestehen und alle Taskänderungen sind committed.
```

## Slice 0 – Vertrag und Renderergrenze

Ziel: Semantische Eingaben, gemeinsame Renderer-Schnittstelle und
modusspezifische Zustände festlegen, ohne den Vertrag voreilig zu erweitern.

Akzeptanzkriterien:

- [ ] Gemeinsame Renderer-Operationen und Zustandsgrenzen sind dokumentiert.
- [ ] `viewProfiles` für Detailstufen und Visualisierungsmodi für Layouts sind
      klar getrennt.
- [ ] Ein optionaler Vertragsschritt wird erst nach einem konkreten Prototyp
      entschieden.

Checks: Dokumentenreview, `git diff --check`.

## Slice 1 – Modusumschaltung und gemeinsamer Zustand

Ziel: Der Viewer kann Renderer wechseln, ohne Graphdaten neu zu laden oder die
Auswahl zu verlieren.

Akzeptanzkriterien:

- [ ] Mindestens zwei Renderer können über eine gemeinsame Grenze registriert
      und ausgewählt werden.
- [ ] Auswahl, Suche, Filter, Metriken und Details bleiben beim Wechsel stabil.
- [ ] Renderer-spezifische Kamera- und Layoutzustände werden getrennt gehalten.

Checks: Unit- und Browser-Tests für Umschaltung, Auswahl und Reset.

Abhängigkeit: Slice 0.

## Slice 2 – Stadtkarte

Ziel: Eine stabile 2D-Architekturansicht für Hierarchie und Querbeziehungen
prototypisch umsetzen.

Akzeptanzkriterien:

- [ ] Containment-Gruppen sind räumlich stabil und sichtbar beschriftet.
- [ ] Querbeziehungen bleiben nachvollziehbar, ohne Containergrenzen zu
      zerstören.
- [ ] Detailstufen und Metriken besitzen eine verständliche Legende.
- [ ] Der Modus bleibt bei isolierten Nodes und unvollständiger Hierarchie
      nutzbar und zeigt Fallbacks sichtbar an.

Checks: deterministische Layout-Tests, Browser-Smoke-Tests und Messung der
relevanten Graphgrößen.

Abhängigkeit: Slice 1.

## Slice 3 – Universums-Regression

Ziel: Der bestehende 3D-Modus bleibt unverändert nutzbar und teilt nur die
gemeinsame Aufbereitung mit der Stadtkarte.

Akzeptanzkriterien:

- [ ] Bestehende Auswahl-, Filter-, Upload- und Detailtests bleiben grün.
- [ ] Die 3D-Ansicht täuscht durch Moduswechsel keine fachliche Position vor.
- [ ] Performance- und Vollmodusgrenzen bleiben dokumentiert.

Checks: vollständiger Viewer-Testumfang und `npm run check`.

Abhängigkeit: Slice 1.

## Slice 4 – Biologisches Profil

Ziel: Prüfen, ob Myzel oder Signalfluss einen verständlichen zusätzlichen
Modus ergibt.

Akzeptanzkriterien:

- [ ] Die biologische Metapher ist auf mindestens einer Fixture verständlich.
- [ ] Richtung, Beziehungstyp und Metrik bleiben erkennbar.
- [ ] Animation ist abschaltbar und fachlich begründet oder wird weggelassen.
- [ ] Der Modus wird nicht angeboten, wenn erforderliche Graphfähigkeiten
      fehlen.

Checks: Fixture-/Browser-Tests und read-only Fachreview.

Abhängigkeit: Slice 1.

## Slice 5 – Abschluss und Vertragsentscheidung

Ziel: Modi, Dokumentation und Vertrag auf den gemessenen Stand bringen.

Akzeptanzkriterien:

- [ ] Konzept-, Viewer- und Graphdokumentation widersprechen sich nicht.
- [ ] Es ist entschieden, ob `visualizationProfiles` im Graphvertrag benötigt
      werden oder eine feste Viewer-Registry genügt.
- [ ] Relevante Tests, `npm run check`, `git diff --check` und Review bestehen.
- [ ] Der Orchestrator entfernt den globalen Taskeintrag nach Abschluss.

Checks: vollständiger Repository-Check und Review gegen alle Slices.

Abhängigkeit: Slices 0–4.

## Orchestrator-Regel

Nur der Orchestrator aktualisiert den globalen Roadmap-Status und erstellt
Commits. Slice-Checkboxen dokumentieren den lokalen Arbeitsvertrag. Nach einem
erfolgreichen Slice-Commit wird der nächste bereite Slice bearbeitet; ein
Slice-Commit ist kein Taskabschluss.
