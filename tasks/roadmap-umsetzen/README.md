# Task: Roadmap umsetzen

Dieser Ordner ist der dauerhafte Orchestrationsauftrag für die Umsetzung der
Viewer-Roadmap.

- Das Konzept steht in [CONCEPT.md](CONCEPT.md).
- Die globale Übersicht offener Vorhaben bleibt in
  [docs/05-Roadmap.md](../../docs/05-Roadmap.md); fachliche Wahrheit und
  Slice-Details bleiben in den jeweils referenzierten Dokumenten und Tasks.
- Der technische Arbeitsstand bleibt in Git-Commits und Tests.
- Der Orchestrator bearbeitet den vollständigen Task in fachlich
  zusammenhängenden Slices. Jeder Slice ist ein Review- und Commit-Checkpoint;
  nach einem erfolgreichen Checkpoint wird automatisch fortgesetzt.
- Der Scope dieses Tasks reicht bis zum dokumentierten Viewer-Abschluss: offene
  Punkte der Phasen 1, 3, 4 und 5. Phase 6 und die spätere C#-/Roslyn-Quelle
  bleiben ausdrücklich außerhalb dieses Tasks.

Aufruf im Chat:

> Setze `tasks/roadmap-umsetzen` als Orchestrator um und bearbeite den nächsten offenen Slice.

Der vollständige Aufruf lautet:

> Setze `tasks/roadmap-umsetzen` als Orchestrator vollständig bis zum
> dokumentierten Viewer-Abschluss um.

Der bisherige Aufruf für genau einen Slice bleibt nur als explizite
Einzel-Slice-Anweisung zulässig.
