# C# / Roslyn Adapter

Die veröffentlichbare .NET-10-CLI liegt in
[CodeVisualisierung.CSharp.slnx](CodeVisualisierung.CSharp.slnx). Sie enthält
ein CLI-Projekt und ein xUnit-Testprojekt. Slice 1 definiert die echte
Prozessgrenze; Slice 2 lädt reale Workspaces; Slice 3 ergänzt die semantische
Deklarationsauswertung; Slice 4 ergänzt semantisch aufgelöste Beziehungen.
Slice 5 ergänzt die globale Metrikberechnung, normalisierte Scores,
Partial-Footprints und deklarative Projektionen. Slice 6 härtet den
veröffentlichten Prozessvertrag und bleibt bis zum Orchestrator-Commit offen.

## CLI-Prozessvertrag

Aufruf:

```text
codegraph-csharp <input> --output <graph.json>
```

Unterstützte Eingaben sind `.slnx`, `.sln` und `.csproj`. `--help` und
`--version` schreiben ausschließlich nach `stdout` und beenden mit `0`. Ein
Argumentfehler, etwa ein fehlender Eingabepfad, beendet mit `2`; eine nicht
lesbare oder nicht unterstützte Eingabe mit `3`; ein fataler Analyse- oder Vertragsfehler mit `4`;
ein fehlendes Ausgabeverzeichnis oder anderer Ausgabe-/Dateisystemfehler mit
`5`. Ein valider partieller Lauf mit geschriebener Ausgabe verwendet `1`.
Diagnosen stehen auf Deutsch in `stderr`, Graphdaten werden
niemals auf `stdout` geschrieben.

Ein Release-Artefakt kann beispielsweise mit
`dotnet publish src/CodeVisualisierung.CSharp.Cli/CodeVisualisierung.CSharp.Cli.csproj`
erzeugt und anschließend von beliebiger `cwd` aus auf `.slnx`, `.sln` oder
`.csproj` angewendet werden. Die JSON-Datei wird nach Schema-Validierung über
eine temporäre Datei im Zielordner atomar ersetzt; ein vorhandenes Ziel bleibt
bei Fehlern unverändert.

Die CLI lädt die Eingabe über `MSBuildWorkspace`, verarbeitet eigene
`.cs`-Dokumente mit einem Roslyn-`SemanticModel` pro Dokument und schreibt
einen deterministisch sortierten Deklarationsgraphen. Neben dem Slice-2-
Inventar werden Klassen, Interfaces, Records, Structs, Enums, Delegates,
Methoden, Konstruktoren einschließlich Primary Constructors, Properties,
Felder, Events, Operatoren, lokale Funktionen und Typparameter emittiert.
Parameter und lokale Variablen bleiben bewusst Nicht-Nodes.
Typparameter verwenden für Node-ID, Index und Referenz dieselbe vollständige
Owner-/Ordinal-Identität.

Symbol-Nodes verwenden IDs der Form `<type>:<project-id>:<canonical-signature>`.
Die Signatur kommt aus `SymbolDisplayFormat.FullyQualifiedFormat` mit
Containing Type, Generic-Arity und Parametertypen. Attribute enthalten
`qualifiedName`, `signature`, `accessibility`, `containerId`, `source`,
`sourcePositions` und `declarationFiles`; Pfade sind relativ und verwenden `/`.
Partial Types werden je Projekt über ihre Symbol-ID zusammengeführt. Nur
tatsächlich partielle Named Types führen `partialDeclarationCount`; alle
Deklarationsstellen und Quelldateien werden berücksichtigt.

Die Scope-Policy bleibt strikt: Nur Symbole aus eigenen, nicht generierten
Quelldokumenten werden betrachtet. Framework-/externe Symbole, `obj`/`bin`,
`*.g.cs`, `*.designer.cs`, `*.generated.cs`, `AssemblyInfo.cs`,
`*.AssemblyAttributes.cs` und verlinkte Dateien außerhalb des Projektordners
werden weder als Nodes noch als Linkziele erzeugt. Symbole mit
`System.CodeDom.Compiler.GeneratedCodeAttribute` werden einschließlich ihrer
untergeordneten Symbole ebenfalls übersprungen.

Die v1-Beziehungen werden aus Syntax und `SemanticModel` aufgelöst. `declares`
zeigt vom jeweiligen deklarierenden Container auf den eigenen deklarierten
Node. `calls`,
`constructs`, `reads`, `writes`, `uses-type`, `returns-type`,
`parameter-type`, `inherits`, `implements` und `overrides` zeigen immer von
einem eigenen emittierten Node auf einen eigenen emittierten Node.
`constructs` zielt auf den konstruierten Typ und bleibt dadurch auch bei
impliziten Konstruktoren aussagekräftig. Wiederholte gleiche Beziehungen
werden je Linktyp und Endpunkten mit `occurrences` und
`relationshipWeight` aggregiert. Nur erkannte Testmethoden erhalten zusätzlich
`tests`-Links zu verwendeten eigenen Zielen aus anderen Projekten; Helper-
Methoden und Typen erzeugen keine solchen Links. `out` erzeugt nur `writes`,
`ref` `reads` und `writes`, `in` nur `reads`. Externe,
generierte und unauflösbare Ziele erzeugen keine Links; sie werden in der
CLI-Summary gezählt.
Die aktuelle Workspace-Policy analysiert alle geladenen Projekte; deshalb bleibt
`projects.skipped` leer. Workspace- und Compilerdiagnosen werden unabhängig von
der Prozesskultur mit deutscher Klassifikation und Detailmeldung auf `stderr`
ausgegeben und in ihrer klassifizierten Severity in der Summary gezählt; IDs
und Pfade dürfen als technische Details erscheinen. Der verwertbare Graph wird
als valides `partial`-Dokument geschrieben.

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
  Contract-/Application-/Test-Solution für echte `MSBuildWorkspace`- und
  Projektdatei-Tests. Sie wird absichtlich nicht in die produktive Adapter-
  Solution aufgenommen.

Neue Roslyn-Tests sollen zuerst die In-Memory-Spec verwenden. Die physische
Fixture ist für Tests gedacht, die echte `.slnx`-, `.csproj`- oder
`ProjectReference`-Auflösung benötigen.

`MsBuildWorkspaceTestHost` lädt die physische Fixture als xUnit-v3-
Assembly-Fixture genau einmal. Der Host registriert MSBuild thread-sicher,
besitzt den `MSBuildWorkspace` bis zur Assembly-Entsorgung und stellt eine
lesbare Momentaufnahme aller `WorkspaceFailed`-Diagnosen bereit. Die
`RoslynSemanticFixtureMatrix` beschreibt die kleinen, graphfreien Fälle für
Partial Types, Überladungen/Generics, Vererbung/Interfaces/Overrides,
Records/Structs/Enums/Delegates sowie Aufrufe, Konstruktionen, Lese-/Schreib-
zugriffe und Typverwendungen.

Ein generisches `Result<T>` ist in dieser Testinfrastruktur nicht erforderlich:
Die Produktionsgrenze verwendet `AnalysisResult` für Analysefehler, Warnungen
und partielle Ergebnisse. Solution- und Workspace-Diagnosen bleiben am
konkreten Testhost explizit.

Vorgesehene Namespace-Verantwortungen innerhalb des CLI-Projekts:

- CodeVisualisierung.CSharp.Cli — Prozessgrenze und CLI-Komposition
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

Umfang des implementierten Adapters:

- Projekte und Solutions mit Roslyn einlesen
- Namespaces, Typen und Methoden als Nodes exportieren
- Aufrufe und Abhängigkeiten als Links exportieren
- Code-Metriken als benannte Metriken ergänzen
- Ausgabe gegen das gemeinsame Schema und Fixtures testen

Der Adapter gehört nicht in den Browser-Viewer. Er ist als veröffentlichbare
.NET-10-CLI unabhängig vom Viewer nutzbar.
