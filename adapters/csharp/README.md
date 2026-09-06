# C# / Roslyn Adapter

Die kompilierbare .NET-Grundlage liegt in
[CodeVisualisierung.CSharp.slnx](CodeVisualisierung.CSharp.slnx). Sie enthält
aktuell ein CLI-Projekt und ein xUnit-Testprojekt. Die Analysepipeline und die
Graphschichten werden in späteren Slices ergänzt; die Grundlage implementiert
bewusst noch keinen Adapter.

Die AiNetLinter-Integration wird über
[ainetlinter.project.json](ainetlinter.project.json) und [rules.json](rules.json)
adressiert. Der Test AiNetLinterTests führt die angegebene lokale Binary gegen
die Solution aus.

Vorgesehene Namespace-Verantwortungen innerhalb des CLI-Projekts:

- CodeVisualisierung.CSharp.Cli — Prozessgrenze und spätere CLI-Komposition
- CodeVisualisierung.CSharp.Analysis — Roslyn-/Workspace-Analyse
- CodeVisualisierung.CSharp.Graph — Zwischenmodell, IDs und Beziehungen
- CodeVisualisierung.CSharp.Contract — Graphvertrag, Serialisierung und Validierung

Voraussichtliche Aufgaben:

- Projekte und Solutions mit Roslyn einlesen
- Namespaces, Typen und Methoden als Nodes exportieren
- Aufrufe und Abhängigkeiten als Links exportieren
- Code-Metriken als benannte Metriken ergänzen
- Ausgabe gegen das gemeinsame Schema und Fixtures testen

Der Adapter gehört nicht in den Browser-Viewer und wird erst nach dem ersten funktionierenden Viewer umgesetzt.
