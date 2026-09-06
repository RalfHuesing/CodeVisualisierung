# Orchestrierung und Subagenten

Diese Regeln gelten, wenn der Nutzer einen Task ausdrücklich als Orchestrator
ausführen lässt oder Subagenten, Rollen oder Reviewer verlangt.

## Zuständigkeit

- Der Hauptagent ist der Orchestrator und bleibt für Ziel, Reihenfolge,
  Integration, Roadmap und Abschluss verantwortlich.
- Rollen sind Arbeitsverträge, keine autonomen Projektleiter.
- Subagenten dürfen keine weiteren Subagenten starten.
- Nur der Orchestrator aktualisiert den globalen Roadmap-Status und erstellt Commits.

## Task-Lebenszyklus

- Der Nutzer beauftragt standardmäßig den vollständigen Task, nicht nur dessen
  ersten Slice.
- Vor der Delegation definiert der Orchestrator Scope, explizite Ausschlüsse,
  Abschlussbedingung und die Reihenfolge der bereiten Arbeit.
- Beim Start eines Tasks prüft der Orchestrator den globalen Roadmap-Eintrag
  und setzt ihn auf `active`, sofern der Task dort geführt wird.
- Ein Slice ist die interne Einheit für Delegation, Review, Checks und Commit.
  Mehrere Slices dürfen nacheinander in einem Task-Lauf bearbeitet werden.
- Nach jedem erfolgreichen Slice-Commit liest der Orchestrator Roadmap und
  Task-Scope erneut und arbeitet automatisch den nächsten bereiten Slice ab.
- Ein erfolgreicher Slice-Commit ist kein erfolgreicher Abschluss des Tasks.
- Der Task darf erst als abgeschlossen gemeldet werden, wenn keine offene
  In-Scope-Arbeit mehr existiert und die Abschlusschecks bestanden sind.
- Einen einzelnen Slice bearbeitet der Orchestrator nur dann isoliert, wenn
  der Nutzer dies ausdrücklich verlangt.

## Task-Slices

- Ein einzelner Arbeitsschritt bearbeitet genau einen fachlich
  zusammenhängenden Slice; ein vollständiger Task-Lauf kann mehrere solche
  Slices nacheinander enthalten.
- Vor der Delegation werden Ziel, erlaubte Dateien, Akzeptanzkriterien,
  Prüfungen und Stop-Bedingung schriftlich festgelegt.
- Unklare fachliche Richtungsentscheidungen werden vor Implementierung geklärt;
  Routineentscheidungen trifft der Orchestrator.
- Unabhängige Lese- oder Analyseaufgaben dürfen parallel laufen.
- Schreibende Subagenten arbeiten nicht parallel an denselben Dateien.

## Review-Gate

- Nach der Implementierung erfolgt mindestens ein read-only Review.
- Reviewer ändern keine Dateien und committen nicht.
- Ein Review liefert nur `pass`, konkrete Findings oder `blocked`.
- Es gibt höchstens zwei Korrektur-/Reviewzyklen pro Slice.
- Derselbe unveränderte Fehler beendet den Slice als blockiert und wird dem
  Nutzer berichtet; es wird keine Endlosschleife gestartet.

## Abschluss

- Der Orchestrator führt die relevanten Tests und `npm run check` für jeden
  Slice vor dessen Commit aus.
- Der globale Roadmap-Status wird nur bei einem nachweisbaren Taskereignis
  aktualisiert. Ein abgeschlossener Task wird aus der offenen Roadmap entfernt.
- Vor dem Commit werden `git diff`, `git diff --check` und `git status` geprüft.
- Der Orchestrator committet fachlich abgeschlossene Slices selbst mit einer
  Conventional-Commit-Nachricht und setzt danach den Task-Lauf fort.
- Nach dem letzten In-Scope-Slice wird zusätzlich geprüft, dass die
  Task-Abschlussbedingung erfüllt ist. Erst dann wird der Task erfolgreich
  beendet.
