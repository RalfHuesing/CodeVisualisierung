# C# / Roslyn Adapter

Die kompilierbare .NET-Grundlage liegt in
[CodeVisualisierung.CSharp.slnx](CodeVisualisierung.CSharp.slnx). Sie enthält
aktuell ein CLI-Projekt und ein xUnit-Testprojekt. Die Analysepipeline und die
Graphschichten werden in späteren Slices ergänzt; die Grundlage implementiert
bewusst noch keinen Adapter.

Gemeinsame MSBuild-Einstellungen für beide Projekte liegen in
[Directory.Build.props](Directory.Build.props). Zentrale NuGet-Versionen liegen
in [Directory.Packages.props](Directory.Packages.props); Projektdateien nennen
nur noch die tatsächlich verwendeten Pakete.

Die AiNetLinter-Integration wird über
[ainetlinter.project.json](ainetlinter.project.json) und [rules.json](rules.json)
adressiert. Der Test AiNetLinterTests führt die angegebene lokale Binary gegen
die Solution aus.

## Roslyn-Testinfrastruktur

Die Testinfrastruktur trennt zwei Anwendungsfälle:

- [RoslynTestSolutionFactory.cs](tests/CodeVisualisierung.CSharp.Tests/Infrastructure/RoslynTestSolutionFactory.cs)
  erstellt kleine mehrprojektige `AdhocWorkspace`-Solutions direkt im Speicher.
  `PreparedSolutionFixture` verwaltet deren Lebensdauer über die Testklasse.
- [CSharpReferenceMini](tests/Fixtures/CSharpReferenceMini) ist eine physische
  Contract-/Application-/Test-Solution für spätere `MSBuildWorkspace`- und
  Projektdatei-Tests. Sie wird absichtlich nicht in die produktive Adapter-
  Solution aufgenommen.

Neue Roslyn-Tests sollen zuerst die In-Memory-Spec verwenden. Die physische
Fixture ist für Tests gedacht, die echte `.slnx`-, `.csproj`- oder
`ProjectReference`-Auflösung benötigen.

Vorgesehene Namespace-Verantwortungen innerhalb des CLI-Projekts:

- CodeVisualisierung.CSharp.Cli — Prozessgrenze und spätere CLI-Komposition
- CodeVisualisierung.CSharp.Analysis — Roslyn-/Workspace-Analyse
- CodeVisualisierung.CSharp.Graph — Zwischenmodell, IDs und Beziehungen
- CodeVisualisierung.CSharp.Contract — Graphvertrag, Serialisierung und Validierung

## AiNetLinter-Profil

Die [rules.json](rules.json) ist kein unverändertes AiNetLinter-Template:

- C#-Qualität, Nullable, ASCII-Namen, semantische Benennung, Namespace-Pfadmapping,
  Immutability und Agent-Kontextgrenzen bleiben aktiv.
- Web-, JavaScript-, Razor- und UI-spezifische Prüfungen sind für die reine
  CLI-/Roslyn-Solution deaktiviert.
- AiNetLinter-eigene Pfad-, Typ- und TestKit-Ausnahmen sind entfernt.
- Tests dürfen sealed-/Immutability-Regeln gezielt lockern und erhalten eigene,
  aber weiterhin begrenzte Methodenlimits.
- XML-Dokumentation für öffentliche APIs, keine blockierenden Task-Zugriffe und
  eine maximale LINQ-Kettenlänge von vier bleiben Teil des Adapterprofils.

Voraussichtliche Aufgaben:

- Projekte und Solutions mit Roslyn einlesen
- Namespaces, Typen und Methoden als Nodes exportieren
- Aufrufe und Abhängigkeiten als Links exportieren
- Code-Metriken als benannte Metriken ergänzen
- Ausgabe gegen das gemeinsame Schema und Fixtures testen

Der Adapter gehört nicht in den Browser-Viewer und wird erst nach dem ersten funktionierenden Viewer umgesetzt.
