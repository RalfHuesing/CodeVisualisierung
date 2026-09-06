# Task: C#-Adapter

Dieser Ordner enthält den fortschreibbaren Konzept- und Umsetzungsauftrag für
den C#-/Roslyn-Adapter. Der Auftrag wird später als vollständiger Task mit der
Orchestrator-Rolle umgesetzt.

- [CONCEPT.md](CONCEPT.md) beschreibt Ziel, Grenzen, Architektur und den
  fachlichen Vertrag.
- [ROADMAP.md](ROADMAP.md) beschreibt die abhängigen Umsetzungsslices mit
  Akzeptanzkriterien und Prüfungen.
- [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) enthält ungeklärte Entscheidungen.

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

Der endgültige Produktname, die genaue Optionensyntax und die unterstützten
Eingabeformate sind in [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) markiert und
werden vor der Implementierung entschieden.
