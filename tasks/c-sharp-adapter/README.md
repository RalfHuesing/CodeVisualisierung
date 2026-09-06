# Task: C#-Adapter

Dieser Ordner enthält den fortschreibbaren Konzept- und Umsetzungsauftrag für
den C#-/Roslyn-Adapter. Der Auftrag wird später als vollständiger Task mit der
Orchestrator-Rolle umgesetzt.

- [CONCEPT.md](CONCEPT.md) beschreibt Ziel, Grenzen, Architektur und den
  fachlichen Vertrag.
- [ROADMAP.md](ROADMAP.md) beschreibt die abhängigen Umsetzungsslices mit
  Akzeptanzkriterien und Prüfungen.
- [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) enthält ungeklärte Entscheidungen.

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

## Späterer Aufruf

Der vorgesehene End-to-End-Aufruf soll nach der Implementierung ungefähr so
aussehen:

```text
codegraph-csharp path\to\application.slnx --output path\to\graph.json
```

Der Produktname und die Grundsyntax sind entschieden. Als Eingabe werden
`.slnx`, `.sln` und `.csproj` unterstützt. Detailentscheidungen zu
Fehlercodes, Summary und Layoutvertrag stehen weiterhin in
[OPEN-QUESTIONS.md](OPEN-QUESTIONS.md).
