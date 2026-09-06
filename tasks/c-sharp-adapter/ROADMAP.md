# Umsetzungs-Roadmap: C#-Adapter

Diese Roadmap ist der interne Orchestrator-Arbeitsvertrag. Die Reihenfolge ist
verbindlich, bis eine dokumentierte fachliche Entscheidung sie ändert.

## Taskvertrag

```text
Task:
  Vollständigen .NET-10-C#-/Roslyn-CLI-Adapter bauen.

Task scope:
  Solution laden, C#-Code semantisch analysieren, Graph-Universe-JSON
  deterministisch erzeugen, gegen den gemeinsamen Vertrag prüfen und mit
  xUnit vollständig testen.

Explicit exclusions:
  Vieweränderungen einschließlich Layout-/Orbitlogik, Backend/Live-Modus,
  Git-Churn, externe Coverage-Reports, Agentenereignisse und nicht gemessene
  Performanceversprechen.

Task completion condition:
  CONCEPT.md ist umgesetzt, alle bereiten Slices sind abgeschlossen,
  dotnet test sowie npm run check und git diff --check bestehen, und alle
  Taskänderungen sind in fachlichen Commits enthalten.

Delegated roles:
  orchestrator, implementer, reviewer gemäß .agents/roles/.
```

## Externer Vorgänger: Viewer-Layoutvertrag

- [ ] Separater Viewer-/Graphvertrag-Task: generische Deklaration von
  Größenmetriken sowie Containment-, Orbit- und Gruppendistanzen.

Dieser Punkt ist ausdrücklich außerhalb des C#-Tasks und wird von dessen
Orchestrator nicht umgesetzt oder abgehakt. Der C#-Adapter muss die
verabschiedete Vertragsform später verwenden; bis dahin bleiben die
Containment- und Relevanzdaten im Adapter fachlich definiert.

## [ ] Slice 0 – Vertrag und Entscheidungen einfrieren

Ziel: Die offenen fachlichen Richtungen beantworten und den Adaptervertrag
konkret machen, bevor Produktionscode entsteht.

Erwartete Inhalte:

- kanonische CLI-Syntax und Name,
- unterstützte Solution-/Projektformate,
- Node-/Linkumfang des ersten vollständigen Tasks,
- Identitäts- und Pfadkonvention,
- deterministische Metadaten- und Fehlerpolicy,
- Ausschluss-Policy für externe, Framework- und generierte Artefakte,
- Metrikumfang und Compilerdiagnose-Policy,
- Abhängigkeit vom separaten Viewer-Layoutvertrag.

Erlaubte Pfade: `tasks/c-sharp-adapter/**`, bei expliziter Vertragsänderung
zusätzlich `docs/**` und `contracts/graph-universe/**`.

Akzeptanzkriterien:

- [ ] `OPEN-QUESTIONS.md` enthält keine ungelöste Richtungsentscheidung mehr,
  die den ersten Implementierungsslice blockiert.
- [ ] `CONCEPT.md` und die allgemeine Graphdokumentation widersprechen sich
  nicht.
- [ ] Alle folgenden Slices haben konkrete erlaubte Pfade und Prüfkriterien.
- [ ] Der C#-Task referenziert die vom separaten Viewer-Layouttask
  verabschiedete, quellenneutrale Layoutbeschreibung korrekt.

Checks: Dokumentenreview, `git diff --check`.

Stop: Eine Entscheidung würde den Graphvertrag oder den Viewerumfang
grundsätzlich ändern und kann nicht sicher aus den vorhandenen Dokumenten
abgeleitet werden.

## [ ] Slice 1 – .NET-Solution und CLI-Grenze

Ziel: Eine kleine, kompilierbare .NET-10-CLI mit verständlichem Prozessvertrag.

Erlaubte Pfade: `adapters/csharp/**`, notwendige Root-`.gitignore`-Ergänzung,
`tasks/c-sharp-adapter/**`.

Akzeptanzkriterien:

- [ ] Die C#-Solution und das Testprojekt bauen mit dem vereinbarten SDK.
- [ ] `--help`, `--version`, ungültige Argumente und fehlende Eingaben liefern
  stabile, dokumentierte Ergebnisse.
- [ ] Analyse, Graph und Contract sind nicht in eine God-Klasse gelegt.
- [ ] Der CLI-Test prüft den tatsächlichen Prozessvertrag, nicht nur eine interne
  Methode.

Checks: `dotnet build`, passende xUnit-/CLI-Tests, `dotnet test`.

Abhängigkeit: Slice 0.

## [ ] Slice 2 – Workspace laden und Inventargraph

Ziel: Eine reale kleine `.slnx`, `.sln` und `.csproj` laden und Solution,
Projekte, Dokumente, Assemblies, Module und Namespaces als vertragskonforme
Nodes mit Containment ausgeben.

Erlaubte Pfade: `adapters/csharp/**`, `contracts/graph-universe/**` nur für
neue C#-Fixtures, `tasks/c-sharp-adapter/**`.

Akzeptanzkriterien:

- [ ] Eine Test-Solution mit mindestens zwei Projekten wird ohne absolute
  Maschinenpfade in Identitäten analysiert.
- [ ] Die Ausgabe ist gegen exakt
  `contracts/graph-universe/schema/graph-universe.schema.json` validiert.
- [ ] Nodes und Links sind dedupliziert und stabil sortiert.
- [ ] Fehlende Projektdateien, Workspace-Fehler und relevante Diagnosen werden
  gemäß der festgelegten Policy verständlich behandelt.
- [ ] Eine gültige Eingabe erzeugt auch bei partiellen Roslyn-Problemen ein
  valides JSON und eine Konsolensummary mit Zählungen.

Checks: Unit-Tests für Identitäten/Sortierung, Workspace-Integrationstest,
Schema-Validierung, `dotnet test`.

Abhängigkeit: Slice 1.

## [ ] Slice 3 – Typen und Member

Ziel: Roslyn-Symbole für Typen und Member vollständig und unterscheidbar in
den Graph überführen.

Erlaubte Pfade: `adapters/csharp/**`, C#-Testfixtures und gegebenenfalls
`docs/07-CSharp-Referenzgraph.md`, `tasks/c-sharp-adapter/**`.

Akzeptanzkriterien:

- [ ] Klassen, Interfaces, Records, Structs, Enums, Delegates, Methoden,
  Konstruktoren, Properties, Felder, Events, Operatoren, lokale Funktionen
  und relevante Typparameter werden nach der beschlossenen Policy erkannt.
- [ ] Overloads, Generics, Teiltypen und gleichnamige Symbole erhalten eindeutige
  kanonische IDs.
- [ ] Sichtbarkeit, Quellposition, qualifizierter Name und Container stehen als
  vertragskonforme Detaildaten zur Verfügung.
- [ ] Externe, Framework- und generierte Symbole werden ausgeschlossen und
  nicht als Linkziele erzeugt.

Checks: reine ID-/Mapping-Unit-Tests, Referenz-Solution-Integrationstest,
Schema-Validierung, `dotnet test`.

Abhängigkeit: Slice 2.

## [ ] Slice 4 – Semantische Beziehungen

Ziel: Beziehungen aus Syntax und Semantic Model auflösen und korrekt
referenzieren.

Akzeptanzkriterien:

- [ ] `calls`, `constructs`, `inherits`, `implements`, `overrides`, `reads`,
  `writes`, Typverwendungen und Projekt-/Assemblyreferenzen werden gemäß der
  beschlossenen Zielmenge geliefert.
- [ ] Mehrere Aufrufe derselben Beziehung werden dedupliziert oder über eine
  benannte Metrik aggregiert; die Semantik ist dokumentiert.
- [ ] Nicht auflösbare oder compilerbedingt unvollständige Symbole werden nicht
  in ungültige Links umgewandelt.
- [ ] Jedes Linkziel existiert und die Richtung ist fachlich korrekt.

Erlaubte Pfade: `adapters/csharp/**`, `contracts/graph-universe/**` nur für
Fixtures, `tasks/c-sharp-adapter/**`.

Checks: semantische Unit-Tests mit kleinen Codebeispielen,
Mehrprojekt-Integrationstests, Invarianten- und Schema-Tests, `dotnet test`.

Abhängigkeit: Slice 3.

## [ ] Slice 5 – Metriken, externe/generierte Artefakte und Projektionen

Ziel: Die fachlich vereinbarten Zusatzdaten vervollständigen, ohne den
Viewer mit C#-Sonderlogik zu belasten.

Akzeptanzkriterien:

- [ ] LOC-/Komplexitäts-/Fan-in-/Fan-out-Metriken sind benannt,
  reproduzierbar und mit fehlenden Werten sauber unterschieden.
- [ ] Generierte Dateien und externe Assemblies sind nach der beschlossenen
  Policy ausgeschlossen; Partial Types bleiben als zusammengehörige eigene
  Typdeklarationen nachvollziehbar.
- [ ] Summary-Links und ihre Herkunft sind explizit und gegen die
  Detailbeziehungen prüfbar.
- [ ] `metricDefinitions`, `nodeTypes`, `linkTypes`, Facetten, Profile und
  Layoutregeln bleiben mit dem gemeinsamen Vertrag kompatibel, sobald der
  externe Viewer-Layouttask den Vertrag erweitert hat.

Erlaubte Pfade: `adapters/csharp/**`, `contracts/graph-universe/**` für
Vertrag/Fixtures, `docs/**` bei erforderlicher Vertragsdokumentation,
`tasks/c-sharp-adapter/**`.

Checks: Metrik-Unit-Tests, Fixture-/Projektionstests, Schema- und Viewer-
Vertragstests, `dotnet test`, `npm run check`.

Abhängigkeit: Slice 4.

## [ ] Slice 6 – End-to-End-Härtung und Abschluss

Ziel: Die Anwendung ist als CLI nutzbar, vollständig dokumentiert und gegen
Regressionen abgesichert.

Akzeptanzkriterien:

- [ ] Ein veröffentlichbares CLI-Artefakt kann eine Test-Solution aus einem
  beliebigen Arbeitsverzeichnis analysieren.
- [ ] Erfolg, Fehler, deterministische Wiederholung und vorhandene Zieldateien
  sind als Prozessverhalten getestet.
- [ ] README, C#-Adapterdokumentation, Schema-Referenz-Fixture und Taskstatus
  stimmen überein.
- [ ] Keine Architektur-/Dateigrößenregel des Repositorys ist verletzt.

Erlaubte Pfade: `adapters/csharp/**`, zugehörige Tests/Fixtures,
`contracts/graph-universe/**`, `docs/**`, `README.md`,
`tasks/c-sharp-adapter/**`, notwendige Check-/Ignore-Konfiguration.

Checks: `dotnet test`, `dotnet build`, `npm run check`, `git diff --check`,
Review gegen alle Slices und `git status`.

Abhängigkeit: Slices 0–5.

## Orchestrator-Regel

Ein Slice wird erst nach Implementierung, read-only Review, erfolgreichen
Checks und einem fachlich eindeutigen Conventional Commit als abgeschlossen
markiert. Nur der Orchestrator aktualisiert Roadmap-Checkboxen und erstellt
Commits. Nach einem erfolgreichen Commit wird automatisch der nächste bereite
Slice bearbeitet; ein Slice-Commit ist kein Taskabschluss.
