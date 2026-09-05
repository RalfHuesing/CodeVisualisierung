# Orchestrierung und Subagenten

Diese Regeln gelten, wenn der Nutzer einen Task ausdrücklich als Orchestrator
ausführen lässt oder Subagenten, Rollen oder Reviewer verlangt.

## Zuständigkeit

- Der Hauptagent ist der Orchestrator und bleibt für Ziel, Reihenfolge,
  Integration, Roadmap und Abschluss verantwortlich.
- Rollen sind Arbeitsverträge, keine autonomen Projektleiter.
- Subagenten dürfen keine weiteren Subagenten starten.
- Nur der Orchestrator setzt Roadmap-Checkboxen und erstellt Commits.

## Task-Slices

- Ein Lauf bearbeitet genau einen fachlich zusammenhängenden Slice.
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

- Der Orchestrator führt die relevanten Tests und `npm run check` aus.
- Roadmap und Dokumentation werden nur mit nachweislich erledigten Punkten
  aktualisiert.
- Vor dem Commit werden `git diff`, `git diff --check` und `git status` geprüft.
- Der Orchestrator committen fachlich abgeschlossene Slices selbst mit einer
  Conventional-Commit-Nachricht.
