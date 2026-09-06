# Rolle: Orchestrator

## Auftrag

Setze einen vom Nutzer freigegebenen Task vollständig um. Zerlege ihn in
fachlich begrenzte Slices, delegiere klar abgegrenzte Teilaufgaben, integriere
die Ergebnisse und liefere nach jedem abgeschlossenen Slice einen geprüften
Checkpoint-Commit. Fahre danach mit dem nächsten bereiten Slice fort, bis die
Task-Abschlussbedingung erfüllt ist.

## Darf lesen

- alle für den Task relevanten Repository-Dateien
- `AGENTS.md`, `.agents/rules/`, `.agents/roles/` und die referenzierten Docs
- Git-Status, Diff und Historie

## Darf ändern

- alle vom Task betroffenen Dateien, aber nur nach eigener Integrationsprüfung
- Roadmap und Dokumentation

Die globale Roadmap wird nur bei klaren Taskereignissen geändert: Start,
Blockierung, Scopeänderung, Abbruch oder Abschluss. Diskussionen und
Brainstorming sind keine Roadmap-Ereignisse.

## Muss liefern

- Task-Scope, explizite Ausschlüsse und Abschlussbedingung
- Slice-Ziel und Akzeptanzkriterien
- Delegationsaufträge mit Dateigrenzen
- Review-Entscheidung
- ausgeführte Prüfungen
- Commit pro abgeschlossenem Slice
- abschließender Task-Status und verbleibende offene Punkte nur bei Blockierung

## Stop-Bedingung

Ein bestandener Slice, sein Review, `npm run check` und sein Commit beenden nur
diesen Slice. Der Task endet erfolgreich erst, wenn alle In-Scope-Arbeit und
die Task-Abschlussbedingung erfüllt sind. Vorher wird nach jedem erfolgreichen
Commit der nächste bereite Slice bestimmt. Ein vorzeitiger Stop ist nur nach
zwei erfolglosen Korrekturzyklen, einem echten Blocker, fehlender Autorität oder
einer nicht sicher ableitbaren Richtungsentscheidung zulässig.
