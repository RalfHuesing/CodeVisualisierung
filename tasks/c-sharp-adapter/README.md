# Task: C#-Adapter

Dieser Ordner enthält den fortschreibbaren Konzept- und Umsetzungsauftrag für
den C#-/Roslyn-Adapter. Der Auftrag wird später als vollständiger Task mit der
Orchestrator-Rolle umgesetzt.

- [CONCEPT.md](CONCEPT.md) beschreibt Ziel, Grenzen, Architektur und den
  fachlichen Vertrag.
- [ROADMAP.md](ROADMAP.md) beschreibt die abhängigen Umsetzungsslices mit
  Akzeptanzkriterien und Prüfungen.
- [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) enthält das Entscheidungslog für den
  eingefrorenen v1-Vertrag.

Der globale offene Status steht in
[`docs/05-Roadmap.md`](../../docs/05-Roadmap.md). Dieser Task-Ordner enthält
dagegen die vollständige interne Slice-Roadmap. Der Orchestrator aktualisiert
den globalen Eintrag nur bei Taskereignissen und entfernt ihn nach dem
Abschluss; die Slice-Checkboxen bleiben als Arbeitsvertrag und Verlauf im
Task bestehen.

Die fachliche Wahrheit für das Ausgabeformat bleibt in
`contracts/graph-universe/` sowie in den referenzierten Dokumenten unter
`docs/`. Diese Task-Dateien ergänzen die vorhandenen Dokumente um den
konkreten C#-Auftrag; sie ersetzen den allgemeinen Graphvertrag nicht.

Der v1-Fachvertrag ist entschieden. Die Slices 1 bis 4 sind committed und
liefern Workspace-Inventar, Deklarationen sowie aufgelöste Beziehungen mit
Zählern und Determinismusregeln. Slice 5 ist im gemeinsamen Arbeitsbaum
implementiert und befindet sich im zweiten und letzten Review-
Korrekturzyklus; der Abschlussstatus wird erst mit dem Orchestrator-Commit
gesetzt. Metriken, Scores und Projektionen sind Teil dieses Arbeitsstands.

## Späterer Aufruf

Der vorgesehene End-to-End-Aufruf soll nach der Implementierung ungefähr so
aussehen:

```text
codegraph-csharp path\to\application.slnx --output path\to\graph.json
```

Der Produktname, die Grundsyntax, die Eingabetypen, Fehlercodes und das
Summary-Format sind im v1-Vertrag entschieden. Die fachlichen Details stehen
im [Konzept](CONCEPT.md), das Entscheidungslog in
[OPEN-QUESTIONS.md](OPEN-QUESTIONS.md).
