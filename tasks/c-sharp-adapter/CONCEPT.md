# Konzept: C#-Adapter für das Graph-Universum

## Auftrag

Es entsteht eine eigenständige CLI-Anwendung für .NET 10, die eine C#-Solution
analysiert und ein JSON-Dokument im gemeinsamen `graph-universe`-Format
schreibt. Die Anwendung ist vollständig testbar und liefert genau die Daten,
die der quellenneutrale Viewer aus `apps/viewer/` laden kann.

Der Adapter ist eine Datenquelle. Er kennt weder Three.js noch HTML, CSS,
Browserzustände oder konkrete visuelle Effekte.

## Zielbild

Ein Nutzer kann eine Solution lokal analysieren:

```text
codegraph-csharp path\to\application.slnx --output path\to\graph.json
```

Das Ergebnis enthält, abhängig von den gewählten Analyseoptionen, unter
anderem:

- Solution-, Projekt-, Assembly-, Modul-, Namespace-, Datei-, Typ- und
  Member-Nodes,
- Containment-Beziehungen zwischen den Ebenen,
- semantisch aufgelöste Aufrufe, Vererbung, Implementierung, Überschreibung,
  Typverwendung und Projekt-/Assemblyreferenzen,
- benannte Metriken und Quellpositionen, soweit sie zuverlässig ermittelbar
  sind,
- ausschließlich eigene Quell-Symbole als Nodes und Links; Framework- und
  sonstige externe Abhängigkeiten werden aus dem Graphen ausgeschlossen,
- explizite Summary-Links für höhere Detailstufen,
- Definitionen und Profile, die der Viewer aus dem Graph-JSON lesen kann.

Nicht jede Ansicht muss alle Nodes gleichzeitig anzeigen. Der Adapter liefert
die Rohdaten und deklarierten Projektionen; der Viewer entscheidet über die
Darstellung.

## Quellen der Wahrheit

Es gelten folgende Prioritäten:

1. `AGENTS.md` und `.agents/rules/` für Repository-, Struktur-, Test- und
   Workflowregeln,
2. `contracts/graph-universe/schema/graph-universe.schema.json` für die
   maschinenprüfbare Ausgabe,
3. die gültigen und ungültigen Fixtures im selben Vertragsverzeichnis,
4. `docs/03-Graphformat.md`, `docs/06-Graphmodell-und-Visualisierungsprofile.md`
   und `docs/07-CSharp-Referenzgraph.md` für die fachliche Semantik,
5. diese Dateien für den konkreten CLI- und Umsetzungsscope.

Wenn sich das Schema ändert, wird der Adapter nicht mit einem zweiten,
unabhängig gepflegten Vertrag weiterentwickelt. Die C#-Tests validieren
Ausgaben gegen genau die Schema-Datei, die auch der Viewer verwendet. Eine
Schemaänderung wird deshalb immer als gemeinsamer Vertragsschritt mit Schema,
Fixture, Viewer-Tests und Adapter-Tests behandelt.

## Scope des ersten vollständigen Adapter-Tasks

### Enthalten

- eine sauber strukturierte .NET-10-Solution unter `adapters/csharp/`,
- eine veröffentlichbare CLI-Anwendung mit verständlicher Hilfe,
  Exit-Codes und Fehlermeldungen,
- Laden der vom Nutzer angegebenen Solution mit Roslyn und MSBuild,
- Analyse von Projekten, Dokumenten, Syntax und Semantik,
- deterministische Identitäten, Nodes, Links, Sortierung und JSON-Ausgabe,
- Validierung jeder erzeugten Ausgabe gegen den gemeinsamen Vertrag,
- mindestens ein eigenes xUnit-Testprojekt mit Unit-, Integrations-,
  Vertrags- und CLI-Tests,
- kleine, private Test-Solutions mit repräsentativem C#-Code,
- vollständige Aktualisierung der betroffenen Dokumentation, des
  Vertrags-README, der Adapter-README, der Roadmap und der Referenz-Docs,
  sobald sich Verhalten, Schema, Struktur oder Scope ändern,
- relevante .NET-Prüfungen sowie der bestehende Repository-Check.

### Ausdrücklich nicht enthalten

- Änderungen an der Rendering- oder Bedienlogik des Viewers,
- ein Backend, ein eingebetteter Webserver, SignalR oder Live-Deltas,
- Dateiüberwachung und ein dauerhaft laufender Watch-Modus,
- Git-Historie, Churn, letzte Änderungen oder Testabdeckung aus externen
  Reports,
- automatische Qualitätsurteile, Farben, Geometrien, Glow oder Animationen,
- eine zweite C#-spezifische Graphstruktur neben dem allgemeinen Vertrag,
- unkontrolliertes Pruning als Ersatz für vollständige Analyse,
- eine feste Performancezusage für beliebige große Solutions, bevor Messungen
  mit realistischen Fixtures vorliegen.

Externe Abhängigkeiten, Framework-Assemblies (`System.*` und vergleichbare
Referenzen) und generierte Artefakte werden im ersten Task nicht als Nodes oder
Links exportiert. Roslyn darf deren Metadaten zum Auflösen einer Kompilation
verwenden; sie gehören aber nicht zum Analyseumfang und werden nicht rekursiv
geladen oder visualisiert. Exportiert werden nur Symbole und Beziehungen, deren
Ursprung in den ausdrücklich geladenen eigenen Projekten liegt.

Die im Archivdokument `docs/99-Grob-Konzept-Idee-Archiv.md` beschriebenen
Kestrel-, SignalR-, Git- und Live-Agenten-Ideen bleiben spätere, getrennte
Aufträge.

## CLI-Vertrag, vorläufig

Die Syntax ist bewusst als überprüfbarer Vorschlag formuliert:

```text
codegraph-csharp <solution.slnx> --output <graph.json>
```

Geplante Grundregeln:

- Der Positionsparameter ist die Eingabe-Solution.
- `.slnx`, `.sln` und `.csproj` werden unterstützt. Bei einer Solution werden
  alle enthaltenen Projekte gemeinsam geladen; bei einem `.csproj` bildet das
  einzelne Projekt den vollständigen Analyseumfang.
- Die gesamte Eingabe wird für die Analyse in den Speicher geladen. Es gibt
  keinen dynamischen Nachlade- oder Streamingpfad.
- `--output` ist der Zielpfad und wird nicht stillschweigend durch einen
  Standardpfad ersetzt.
- `--help` und `--version` beenden erfolgreich, ohne Analyse zu starten.
- Fehler gehen verständlich nach `stderr`; strukturierte Graphdaten gehen
  ausschließlich in die Ausgabedatei.
- Roslyn-, Workspace- und Compilerdiagnosen werden auf der Konsole gemeldet,
  nicht in den Graph verschoben. Die Analyse versucht mit dem verwertbaren
  Teil weiterzuarbeiten und schreibt am Ende ein valides, gegebenenfalls
  partielles Dokument. Die Zusammenfassung nennt erledigte, übersprungene und
  fehlgeschlagene Analyseschritte jeweils mit Anzahl.
- Ein erfolgreicher Lauf endet mit Exit-Code `0`.
- Ungültige Argumente, nicht lesbare Eingaben, Analysefehler und
  Ausgabefehler erhalten unterscheidbare, dokumentierte Exit-Code-Bereiche.
- Die Ausgabe wird erst nach erfolgreicher Analyse und Vertragsvalidierung
  geschrieben. Ein vorhandenes Ziel wird nicht durch ein fehlerhaftes
  Zwischenergebnis ersetzt.
- Flüchtige Werte wie die aktuelle Uhrzeit oder absolute lokale Pfade dürfen
  die standardmäßige Vergleichbarkeit der Ausgabe nicht unnötig zerstören.

Optionen für Konfiguration, Metrikumfang und die genaue Darstellung der
Konsolenzusammenfassung werden noch konkretisiert. Externe und generierte
Artefakte sind dagegen grundsätzlich außerhalb des Graphen.

## Fachliches Datenmodell

### Ebenen und Node-Typen

Die fachliche Zielmenge stammt aus `docs/07-CSharp-Referenzgraph.md`:

```text
solution, project, assembly, module, namespace, file,
class, interface, record, struct, enum, delegate,
method, constructor, property, field, event, operator,
local-function, type-parameter
```

Die letzten beiden im allgemeinen Referenzdokument genannten Typen
`external-type` und `external-assembly` sind für den Adapter bewusst keine
auszugebenden Nodes. Die Scope-Policy dieses Konzepts hat Vorrang: Der Graph
beschreibt den eigenen Sourcecode, nicht das Framework oder fremde Packages.

Die Ausgabe verwendet ausschließlich die im aktuellen Vertrag gültige Form,
insbesondere `typeId` für deklarierte Node-Typen. `kind` darf nicht als
parallele, anders semantische C#-Sonderwelt eingeführt werden. Fixtures sichern
die kanonische Adapterausgabe ab.

### Beziehungen

Die Zielmenge der Beziehungen umfasst:

```text
contains, declares, calls, inherits, implements, overrides,
constructs, reads, writes, uses-type, returns-type, parameter-type,
references-assembly, project-reference, generated-from, tests
```

Jeder Link referenziert vorhandene Node-IDs. Richtungen, Herkunft und
Aggregation werden explizit im Graph dokumentiert. Höhere Summary-Links sind
auf zugrunde liegende Beziehungen zurückführbar, wenn Details oder Metriken
das benötigen.

`references-assembly` und `project-reference` werden nur für eigene,
ausdrücklich geladene Projekte ausgegeben. Beziehungen zu externen Assemblies
und `generated-from`-Beziehungen zu ausgeschlossenen Artefakten werden nicht
emittiert.

### Identität und Determinismus

- Symbol-IDs beruhen auf einer kanonischen, voll qualifizierten Signatur und
  unterscheiden Overloads, Generics, Teiltypen und gleichnamige Member.
- IDs enthalten keine maschinenabhängigen absoluten Pfade.
- Datei-, Projekt- und Solution-Identitäten verwenden eine explizit
  dokumentierte, normalisierte Darstellung relativ zur Solution oder eine
  andere deterministische fachliche Identität.
- Nodes und Links werden vor der Serialisierung dedupliziert und stabil
  sortiert.
- Quellpositionen und lokale Pfade sind Diagnose-/Detaildaten, nicht die
  Identität eines Symbols.
- Das sichtbare `label` bleibt kurz und menschenlesbar, zum Beispiel
  `OrderService` oder `ProcessPayment()`. Detaildaten enthalten den
  vollqualifizierten Namen, die kanonische Signatur und eine auffindbare
  Quellposition mit solution-relativem Dokumentpfad, Zeile und Spalte. Diese
  Angaben helfen, das Element im Code wiederzufinden, ohne die Node optisch
  mit langen Pfaden zu überladen.
- Ein Partial Type erhält genau einen fachlichen Typ-Node. Mehrere
  Deklarationsdateien und Quellpositionen werden als Detaildaten bzw. mehrere
  zulässige Containment-Belege erhalten, nicht als künstlich verschiedene
  Klassen.
- Die Standardausgabe ist byteweise reproduzierbar, soweit die Quelldaten und
  verwendeten Projektauflösungen gleich sind. Flüchtige Metadaten werden nur
  auf explizite Anforderung aufgenommen.

Die genaue kanonische Signatur und die Behandlung von Solution-/Projekt-
Identitäten werden vor dem ersten Implementierungsslice als Entscheidung
festgeschrieben.

## Räumliche Semantik

Die C#-Struktur soll in einer geeigneten Ansicht eine verständliche räumliche
Nähe ergeben:

```text
Galaxie / Namespace
└── Sonne / Namespace-Zentrum
    └── Planet / Klasse oder anderer Typ
        └── Mond / Methode, Property, Feld oder anderer Member
```

Die Begriffe sind eine Visualisierungsanalogie, keine zusätzlichen C#-Felder.
Fachlich entstehen die Abstände aus:

- expliziten `contains`-Links und einer deklarierten Containment-Hierarchie,
- View-Profilen für Namespace-, Typ- und Member-Detailstufen,
- generischen Layoutregeln, die Entfernung zwischen Container und Kind sowie
  zwischen verschiedenen Gruppen/Namespaces beschreiben,
- expliziten Summary-Links, wenn eine Detailstufe untergeordnete Nodes
  ausblendet.

Der Namespace ist damit das Zentrum seines Bereichs. Ein Typ liegt näher an
seinem Namespace als ein Typ aus einem anderen Namespace; ein Member liegt
näher an seinem deklarierenden Typ als an fremden Typen. Referenzen zwischen
Klassen bilden Verbindungen zwischen Sonnensystemen, Referenzen zwischen
Namespaces Verbindungen zwischen Galaxien. Der Adapter liefert dafür die
Containment- und Beziehungsdaten sowie `layoutProfiles`; der Viewer berechnet
daraus die konkrete 3D-Position. Ein Profil kann mit `groupField`,
`groupDistance`, `defaultDistance` und `containmentDistances` Gruppen-,
Standard- und Eltern-Kind-Abstände deklarieren. Der Adapter liefert diese
neutralen Vertragsdaten später als Quelle; er implementiert keine
Viewer-Layoutlogik und berechnet keine Positionen selbst.

## Abgrenzung zur bestehenden Visualisierung

Die aktuelle Visualisierung kann eine Node-Größe aus benannten Metriken,
`visualRole` und `baseSize` ableiten. `metricDefinitions` beschreibt die
fachliche Bedeutung von Namen wie `importance`; der Viewer skaliert die Werte
und wendet die Grundgröße an. Der Adapter liefert später solche neutralen
Metriken, ohne ein C#-spezifisches Größenfeld zu erfinden.

Der Graphvertrag enthält bereits `layoutProfiles`. Der Viewer wählt das
angeforderte Profil, fällt bei unbekannter Auswahl auf das erste Profil und bei
fehlenden Profilen auf interne Defaults zurück. Aus `groupField`-Werten,
Containment-/Summary-Links und den deklarierten Abständen bereitet er
deterministische Initialpositionen vor. Fehlende Gruppenwerte, Abstände,
Metriken und Tokens werden durch stabile neutrale Defaults ergänzt. Die
spätere Adapterausgabe muss diese Vertragsdaten nur liefern und validieren;
ihre Nutzung im C#-Exporter ist ein nachgelagerter Umsetzungsschritt, keine
zusätzliche Layoutimplementierung.

## Metriken und visuelle Größe

`loc` und zyklomatische Komplexität bleiben nützliche Detailinformationen,
sind aber keine primären Größenmetriken. Die bestehenden Lintergrenzen machen
beide Werte in diesem Projekt absichtlich wenig unterscheidend. Eine Methode
oder Klasse soll daher nicht allein wegen mehr Zeilen oder etwas höherer
Komplexität größer erscheinen.

Der erste fachliche Kandidat für Bedeutung und Größe ist strukturelle
Relevanz aus dem eigenen Graphen:

- `fanIn` und `fanOut` zählen direkte eingehende und ausgehende Beziehungen,
- `weightedFanIn` und `weightedFanOut` berücksichtigen Beziehungstyp und
  aggregierte Aufruf-/Referenzhäufigkeit,
- `pageRank` oder ein vergleichbarer Einflusswert bewertet Nodes, die von
  vielen wichtigen eigenen Nodes erreicht werden,
- `betweenness` kann Brücken zwischen ansonsten getrennten Bereichen
  sichtbar machen,
- `importance` ist ein benannter, dokumentierter abgeleiteter Wert, den ein
  View-Profil auf die visuelle Größe abbilden darf.

Diese Werte müssen getrennt nach fachlicher Ebene sinnvoll aggregiert werden:
Die Relevanz einer Klasse entsteht aus ihren Memberbeziehungen und direkten
Typbeziehungen; die Relevanz eines Namespace entsteht aus den enthaltenen
Typen und den Summary-Links. Eine Methode wird nicht automatisch nur deshalb
groß, weil ihre Klasse wichtig ist. Der Adapter liefert Roh- und abgeleitete
Metriken mit Definitionen, der Viewer entscheidet über die Darstellung.

Die konkrete Formel, Normalisierung und Behandlung von Zyklen werden als
eigene Entscheidung mit kleinen Referenzgraphen getestet. Ein einzelner
unbenannter `weight`-Wert ist dafür nicht ausreichend.

## Technische Zielarchitektur

Die Aufteilung folgt fachlichen Verantwortungen, nicht einzelnen Methoden.
Ein möglicher, vorläufiger Zuschnitt ist:

```text
adapters/csharp/
├── CodeVisualisierung.CSharp.slnx
├── src/
│   ├── CodeVisualisierung.CSharp.Cli/
│   ├── CodeVisualisierung.CSharp.Analysis/
│   ├── CodeVisualisierung.CSharp.Graph/
│   └── CodeVisualisierung.CSharp.Contract/
└── tests/
    └── CodeVisualisierung.CSharp.Tests/
```

Verantwortungen:

- `Cli`: Argumente, Exit-Codes, Prozessgrenze und Ausgabe-I/O.
- `Analysis`: Solution-/Workspace-Laden, Kompilationen, Dokumente, Syntax und
  semantische Roslyn-Abfragen.
- `Graph`: fachliches Zwischenmodell, Identitäten, Deduplizierung,
  Aggregation, Metriken und Projektionen.
- `Contract`: Graph-Universe-Datenstrukturen, JSON-Serialisierung und
  Schema-Validierung gegen den Repositoryvertrag.
- `Tests`: reine Unit-Tests sowie kontrollierte End-to-End-Szenarien mit
  kleinen Test-Solutions.

Die Projekte dürfen nur in dieser Richtung voneinander abhängen:

```text
Cli → Analysis → Graph → Contract
Tests → alle benötigten Produktionsprojekte
```

Die genaue Projektzahl bleibt eine Strukturentscheidung des ersten Slices.
Keine Schicht wird als Platzhalter für eine mögliche Zukunft angelegt. Pro
Quellverzeichnis gelten die zentrale Dateigrenze aus
`scripts/quality-config.mjs` und die allgemeinen Regeln aus `.agents/rules/`;
große Klassen und Methoden werden nach Verantwortung geteilt.

## Roslyn-Analyse

Die Analyse nutzt `Microsoft.CodeAnalysis` und die C#-Workspace-/MSBuild-
Integration. Die Pipeline bleibt nachvollziehbar:

1. Argumente und Pfade prüfen.
2. Die Solution laden und Projekte samt Dokumenten bestimmen.
3. Kompilationen und Semantic Models kontrolliert anfordern.
4. Deklarationen in einer stabilen Reihenfolge in Nodes überführen.
5. Containment- und Referenzbeziehungen ergänzen.
6. Aufruf- und Memberzugriffsbeziehungen aus Syntax plus Semantic Model
   auflösen.
7. Externe, Framework- oder nicht auflösbare Symbole entsprechend der
   Scope-Policy aus dem Graphen fernhalten und in der Konsolensummary zählen.
8. Metriken und Summary-Links berechnen.
9. Graphvertrag validieren und atomar schreiben.

Teure globale Referenzsuche in Schleifen ist zu vermeiden. Lokale Dokument-
Analysen werden, sobald Semantik und Determinismus das erlauben, gebündelt
und gemessen. Parallelisierung ist erst nach einem korrekten sequenziellen
Referenzpfad zulässig.

## Tests und Qualität

Der Adapter ist erst fertig, wenn mindestens folgende Verhalten nachgewiesen
sind:

- CLI-Hilfe, erfolgreiche Ausgabe und alle wesentlichen Fehlerfälle,
- ungültige oder nicht vorhandene Solutionpfade,
- mehrere Projekte und Projekt-/Assemblyreferenzen,
- Namespaces, Teiltypen, Überladungen, Generics, Vererbung, Interfaces und
  Overrides,
- Aufrufe sowie Lese-/Schreibzugriffe mit vorhandenen Linkzielen,
- der Ausschluss externer, Framework- und generierter Artefakte,
- Konsolensummary mit Erfolgs-, Warn- und Übersprungen-Zählungen,
- deklarierte Containment-Hierarchie und räumliche Layoutregeln,
- stabile IDs, Deduplizierung, Sortierung und reproduzierbare Ausgabe,
- Metrikgrenzen und fehlende optionale Werte,
- Ausgabevalidierung gegen die exakte Viewer-Schema-Datei,
- ein C#-Referenzgraph, der die Anforderungen aus `docs/07-...` abdeckt.

Reine Identitäts-, Mapping-, Sortier- und Validierungslogik erhält isolierte
Unit-Tests. Roslyn- und CLI-Grenzen erhalten gezielte Integrations- bzw.
Prozesstests. Für den Adapter ist `dotnet test` der primäre Check; `npm run
check` bleibt wegen der Repositoryregeln zusätzlich verpflichtend, sobald die
Frontend-Infrastruktur installiert ist.

## Dokumentationspflicht

Dokumentation ist ein Bestandteil der Implementierung und kein nachgelagerter
Aufräumschritt. Jede fachliche oder technische Änderung aktualisiert im selben
Slice alle betroffenen Quellen, insbesondere:

- `contracts/graph-universe/schema/graph-universe.schema.json` und Fixtures bei
  Vertragsänderungen,
- `contracts/graph-universe/README.md` bei Änderungen am Vertragsgebrauch,
- `docs/03-Graphformat.md` und
  `docs/06-Graphmodell-und-Visualisierungsprofile.md` bei Änderungen an
  allgemeiner Graphsemantik,
- `docs/07-CSharp-Referenzgraph.md` bei Änderungen an der C#-Semantik,
- `docs/05-Roadmap.md` bei erledigten oder geänderten Roadmap-Punkten,
- `adapters/csharp/README.md` bei Änderungen am CLI-Aufruf, Setup oder
  Adapterumfang,
- diese Task-Dateien bei Änderungen an Scope, Entscheidungen, Slices oder
  Abschlusskriterien.

Eine Änderung ist nicht abgeschlossen, wenn Code, Schema, Fixture und
Dokumentation unterschiedliche Verträge beschreiben. Der Orchestrator prüft
das vor jedem Slice-Commit.

## Abschlussbedingung des späteren Orchestrator-Tasks

Der Task ist erst abgeschlossen, wenn:

- die vollständige CLI sowie das Testprojekt implementiert sind,
- alle akzeptierten Node-/Link-Semantiken und offenen Richtungsentscheidungen
  aus den Task-Dateien umgesetzt oder ausdrücklich aus dem Scope genommen
  sind,
- Schema, Fixtures, allgemeine Graphdokumentation, C#-Referenzdokumentation,
  Adapter-README und Roadmap den implementierten Stand widerspiegeln,
- jede erzeugte Ausgabe gegen den gemeinsamen Vertrag validiert wird,
- `dotnet test` und die relevanten Repositorychecks erfolgreich sind,
- `git diff --check` bestanden ist,
- jeder fachlich abgeschlossene Slice separat und ausschließlich durch den
  Orchestrator committed wurde,
- keine Build-, Coverage-, Cache- oder lokalen Konfigurationsdateien im
  Repository landen.

Ein erfolgreicher Slice-Commit beendet nicht den Task. Nach jedem Commit wird
die nächste abhängige Arbeit aus [ROADMAP.md](ROADMAP.md) fortgesetzt.
