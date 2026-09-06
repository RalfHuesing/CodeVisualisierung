# Konzept: C#-Adapter für das Graph-Universum

## Auftrag

Es entsteht eine eigenständige CLI-Anwendung für .NET 10, die eine C#-Solution
analysiert und ein JSON-Dokument im gemeinsamen `graph-universe`-Format
schreibt. Die Anwendung ist vollständig testbar und liefert genau die Daten,
die der quellenneutrale Viewer aus `apps/viewer/` laden kann.

Der Adapter ist eine Datenquelle. Er kennt weder Three.js noch HTML, CSS,
Browserzustände oder konkrete visuelle Effekte.

## Aktueller Umsetzungsstand

Die Grundlagen unter `adapters/csharp/` sind bereits angelegt:

- eine .NET-10-Solution mit einem CLI-Projekt und einem xUnit-Testprojekt,
- zentrale MSBuild-Vorgaben für Nullable, deterministische Builds und
  Warnungen als Fehler,
- zentral verwaltete NuGet-Versionen für Roslyn/MSBuild und xUnit,
- ein angepasstes AiNetLinter-Profil für die reine C#-/CLI-Solution,
- eine kleine CLI-Grenze mit `--help` und `--version`,
- eine verwaltete, repo-lokale Temp-Infrastruktur für spätere Test-Solutions.

Diese Grundlage ist noch kein Adapterverhalten: Es gibt noch keine
Roslyn-Analyse, keine Graph-/Contract-Projekte, keine Graphausgabe und keine
fachlichen CLI-Fehlerfälle. Die folgenden Abschnitte beschreiben deshalb den
verbindlichen Zielvertrag und die späteren Umsetzungsslices, nicht bereits
vorhandene Funktionalität.

Der Zielvertrag ist auf Solutions bis mindestens 180.000 Quellcodezeilen
ausgelegt. Vollständige Analyse bedeutet dabei vollständige fachlich relevante
Deklarationen und Beziehungen, nicht einen Node für jede lokale Variable oder
jedes Syntaxdetail. Die vollständige Datenmenge bleibt erhalten; die
Visualisierung arbeitet mit deklarativen Detailstufen und Projektionen.

## Zielbild

Ein Nutzer kann eine Solution lokal analysieren:

```text
codegraph-csharp path\to\application.slnx --output path\to\graph.json
```

Das Ergebnis des späteren vollständigen Laufs enthält unter anderem:

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

Die Grundsyntax enthält in v1 keine freien Analyseoptionen. Die Ausgabe ist
vollständig und enthält die festgelegten Metriken und Projektionen. Zusätzliche
Optionen werden erst in einem getrennten Folgeauftrag eingeführt.

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

### Schutz vor Vertragsdrift

Es gibt genau eine Schemaquelle: `contracts/graph-universe/schema/`.
Der C#-Adapter besitzt keine zweite lokale Schema- oder Kompatibilitätsschicht.
Jede Änderung am gemeinsamen Vertrag wird deshalb in demselben Änderungsschnitt
mit diesen Artefakten geprüft:

- Schema und gültige/ungültige Fixtures,
- allgemeine Graph- und Viewer-Tests,
- C#-Contract-Tests gegen exakt diese Schema-Datei,
- C#-semantische Adaptertests für Node-/Linktypen, Metriken, Profile,
  Projektionen, Identitäten und Determinismus.

Die Schema-Validierung ist notwendig, aber nicht ausreichend: Optionales neues
Schema kann altes JSON formal gültig lassen. Die semantischen C#-Contract-Tests
müssen deshalb die erwartete v1-Ausgabe ausdrücklich einfordern. Ändert sich
ein Feld, eine Bedeutung oder eine Pflichtprojektion, müssen diese Tests rot
werden, bis Adapter, Fixture und Dokumentation gemeinsam aktualisiert sind.
Es gibt keine stillschweigende Rückwärtskompatibilität und keine zweite
Schema-Version innerhalb dieses v1-Tasks.

## Zielumfang des vollständigen Adapter-Tasks

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
- vollständige Ausgabe des eigenen Sourcecodes auch für große Solutions; keine
  zufällige Top-Kürzung und kein stilles Pruning,
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

## CLI-Vertrag des Zielzustands

Die Grundsyntax und die unterstützten Eingabetypen sind entschieden:

```text
codegraph-csharp <input> --output <graph.json>
```

Für den v1-Zielzustand gelten folgende Grundregeln:

- Der Positionsparameter ist der Eingabepfad.
- `.slnx`, `.sln` und `.csproj` werden unterstützt. Bei einer Solution werden
  alle enthaltenen Projekte gemeinsam geladen; bei einem `.csproj` bildet das
  einzelne Projekt den vollständigen Analyseumfang.
- Die gesamte Eingabe wird für die Analyse in den Speicher geladen. Es gibt
  keinen dynamischen Nachlade- oder Streamingpfad.
- `--output` ist der Zielpfad und wird nicht stillschweigend durch einen
  Standardpfad ersetzt.
- Der Elternordner des Zielpfads muss existieren und beschreibbar sein; die CLI
  legt keine Verzeichnisstruktur implizit an.
- Die einzigen Optionen sind `<input>`, `--output <path>`, `--help` und
  `--version`. Es gibt in v1 keine Konfigurations-, Filter-, Metrik- oder
  Limitoptionen.
- `--help` und `--version` beenden erfolgreich, ohne Analyse zu starten.
- Die strukturierte Summary geht nach `stdout`, Diagnosen und Fehler gehen
  nach `stderr`; strukturierte Graphdaten gehen ausschließlich in die
  Ausgabedatei.
- Roslyn-, Workspace- und Compilerdiagnosen werden auf der Konsole gemeldet,
  nicht in den Graph verschoben. Die Analyse versucht mit dem verwertbaren
  Teil weiterzuarbeiten und schreibt am Ende ein valides, gegebenenfalls
  partielles Dokument. Die Zusammenfassung nennt erledigte, übersprungene und
  fehlgeschlagene Analyseschritte jeweils mit Anzahl.
- Ein vollständiger, valider Lauf endet mit Exit-Code `0`.
- Ein valider, aber partieller Lauf endet mit Exit-Code `1`. Die Ausgabe ist
  verwendbar, die Summary nennt alle ausgelassenen oder fehlgeschlagenen
  Schritte.
- Exit-Code `2` bezeichnet Argumentfehler, `3` nicht lesbare oder nicht
  auswertbare Eingaben, `4` einen fatalen Analyse- oder Vertragsfehler und `5`
  einen Ausgabe-/Dateisystemfehler. `--help` und `--version` verwenden `0`.
- Die Ausgabe wird erst nach erfolgreicher Analyse und Vertragsvalidierung
  geschrieben. Sie wird über eine temporäre Datei im Zielordner atomar ersetzt;
  ein vorhandenes Ziel bleibt bei Fehlern unverändert.
- `meta.createdAt` wird standardmäßig nicht geschrieben. Absolute lokale Pfade,
  Maschinenname, Prozess-ID und aktuelle Uhrzeit gehören nicht in die
  reproduzierbare Graphausgabe.

Die öffentliche CLI-Sprache ist Deutsch. Maschinenrelevante Feldnamen,
Metriknamen, Node-/Linktypen, IDs und Summary-Schlüssel bleiben Englisch.

Die Summary verwendet stabile `key=value`-Zeilen ohne Zeitstempel und ohne
absolute Pfade:

```text
status=complete|partial
input.kind=slnx|sln|csproj
projects.loaded=... projects.analyzed=... projects.skipped=... projects.failed=...
documents.analyzed=... documents.skipped=... documents.failed=...
nodes.emitted=... links.emitted=...
relations.externalDropped=... relations.unresolved=...
diagnostics.warnings=... diagnostics.errors=...
output.bytes=...
```

Die aktuelle Grundlage implementiert aus diesem Zielvertrag nur `--help` und
`--version`; der Analysepfad und die Ausgabe sind ausdrücklich noch nicht
vorhanden.

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

Die Zielmenge der in v1 emittierten Beziehungen umfasst:

```text
contains, declares, calls, inherits, implements, overrides,
constructs, reads, writes, uses-type, returns-type, parameter-type,
references-assembly, project-reference, tests
```

`generated-from` bleibt als mögliche spätere Beziehung dokumentiert, wird in
v1 aber nicht emittiert, weil generierte Artefakte außerhalb des Graphen
liegen. `tests` wird nur emittiert, wenn Test- und Produktprojekt gemeinsam
als eigene Projekte geladen wurden.

Jeder Link referenziert vorhandene Node-IDs. Richtungen, Herkunft und
Aggregation werden explizit im Graph dokumentiert. Höhere Summary-Links sind
auf zugrunde liegende Beziehungen zurückführbar, wenn Details oder Metriken
das benötigen.

Ein aggregierter Summary-Link trägt mindestens `metrics.occurrences`,
`metrics.relationshipWeight` und `attributes.aggregation` mit Quell- und
Ziellevel. Die Detailbeziehungen bleiben im vollständigen Graph enthalten;
eine Summary-Projektion darf sie nur ausblenden.

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
- Jede Beziehung zwischen demselben Quell- und Zielknoten wird je Linktyp zu
  einem Link aggregiert. Wiederholungen stehen als benannte Metriken am Link;
  sie erzeugen keine Duplikatlinks.
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

Die kanonischen Identitätsregeln sind:

- Pfade verwenden `/`, sind relativ zum Eingaberoot und enthalten keine
  absoluten oder aufgelösten Maschinenpfade.
- Struktur-IDs folgen dem Muster `solution:root`, `project:<relative-csproj>`,
  `assembly:<project-id>:<assembly-name>`,
  `module:<project-id>:<module-name>`,
  `file:<project-id>:<relative-document>`,
  `namespace:<project-id>:<fully-qualified-name>` und
  `<type>:<project-id>:<canonical-symbol-signature>`.
- Link-IDs folgen dem Muster `link:<link-type>:<source-id>:<target-id>`.
- Symbolsignaturen verwenden eine vollqualifizierte, global eindeutige
  Darstellung mit Symbolart, Generic-Arity und Parametertypen. Overloads,
  Teiltypen und gleichnamige Member bleiben dadurch unterscheidbar.
- Namespaces und Symbole sind projektbezogen. Gleichnamige Namespaces in zwei
  Projekten werden nicht künstlich zu einem Node verschmolzen.
- Quellpositionen sind 1-basierte Zeile/Spalte mit solution-relativem
  Dokumentpfad. Sie sind Detaildaten, niemals Identitätsbestandteil.
- `label` bleibt kurz; qualifizierter Name, Signatur, Projekt und Position
  stehen in `attributes`.

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

Der Adapter liefert mindestens diese deklarativen v1-Detailstufen:

- `overview`: Solution, Project, Assembly, Module und Namespace mit
  Projekt-/Assemblyreferenzen und aggregierten Namespacebeziehungen,
- `architecture`: zusätzlich Dateien und Typen mit Containment sowie
  Typbeziehungen,
- `member-detail`: zusätzlich Member und semantische Beziehungen.

Der vollständige Graph bleibt unabhängig davon im Dokument erhalten. Eine
Detailstufe ist eine sichtbare Projektion, keine Datenlöschung. Der Viewer
startet bei großen Graphen mit `overview`; `member-detail` wird nicht als
ungefilterte Standardansicht verwendet.

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

Die v1-Entscheidung für Bedeutung und Größe ist strukturelle Relevanz aus dem
eigenen Graphen:

- `fanIn` und `fanOut` zählen direkte eingehende und ausgehende Beziehungen,
- `weightedFanIn` und `weightedFanOut` berücksichtigen Beziehungstyp und
  aggregierte Aufruf-/Referenzhäufigkeit,
- `pageRank` bewertet Nodes, die von vielen wichtigen eigenen Nodes erreicht
  werden,
- `betweenness` kann Brücken zwischen ansonsten getrennten Bereichen
  sichtbar machen,
- `importance` ist ein benannter, dokumentierter abgeleiteter Wert, den ein
  View-Profil auf die visuelle Größe abbilden darf.

Die Berechnung erfolgt getrennt für Member, Typen und Namespaces. Für Typen
werden Memberbeziehungen auf Typbeziehungen aggregiert; für Namespaces werden
Typbeziehungen auf Namespacebeziehungen aggregiert. Eine Methode wird nicht
automatisch nur deshalb groß, weil ihre Klasse wichtig ist. Die Werte sind
innerhalb einer Node-Ebene vergleichbar, aber nicht als absolute Größen über
verschiedene Ebenen hinweg zu lesen.

Für die v1 gilt folgende deterministische Berechnung:

- `calls` und `constructs` erhalten das Beziehungsgewicht `3`.
- `inherits`, `implements` und `overrides` erhalten das Beziehungsgewicht `2`.
- `reads`, `writes`, `uses-type`, `returns-type` und `parameter-type` erhalten
  das Beziehungsgewicht `1`.
- Wiederholte Aufrufe oder Zugriffe erhöhen die aggregierte Beziehungshäufigkeit.
- `contains`, `declares`, `tests`, Summary-Links, Projekt- und
  Assemblyreferenzen beeinflussen die Relevanz nicht.
- Der Ebenenwert `importance` ist `0.7 * pageRankNormalized + 0.3 *
  weightedFanInNormalized` und liegt im Intervall `[0, 1]`.
- `weightedFanInNormalized` basiert auf `log1p(weightedFanIn)`. Beide Anteile
  werden je Node-Ebene robust über das 5. und 95. Perzentil auf `[0, 1]`
  geklemmt. Bei fehlender Streuung erhalten gleichartige Werte den neutralen
  Wert `0.5`.

Der vollständige v1-Metriksatz ist festgelegt:

- Nodes: `loc`, `fanIn`, `fanOut`, `weightedFanIn`, `weightedFanOut`,
  `pageRank`, `importance` und `footprint`, soweit die Ebene fachlich dafür
  geeignet ist.
- Methoden und lokale Funktionen zusätzlich: `cyclomaticComplexity`.
- Container zusätzlich: `fileCount`, `typeCount`, `memberCount`, soweit die
  enthaltene Ebene definiert ist.
- Partial Types zusätzlich: `partialDeclarationCount` und die Liste ihrer
  Deklarationsstellen als Detaildaten.
- Links zusätzlich: `occurrences` für wiederholte Aufrufe/Zugriffe und
  `relationshipWeight` für das gewichtete Aggregat.

`loc` zählt eindeutige, nichtleere Quelltextzeilen innerhalb des jeweiligen
Deklarations- oder Dokumentbereichs; bei Containern werden überlappende
Bereiche nicht doppelt gezählt. `cyclomaticComplexity` startet bei `1` und
zählt die syntaktisch erkennbaren Verzweigungspunkte. Betweenness, erreichbare Node-Anzahl,
Komponentengröße, Zykluskennzeichnung und Testabdeckung sind nicht Bestandteil
des v1-Exports.

`footprint` wird aus den logarithmierten, fachlich vorhandenen Größen `loc`,
`memberCount`, `fileCount` und `partialDeclarationCount` gebildet. Jede Größe
wird je Node-Ebene normalisiert; der Footprint ist der Mittelwert der
vorhandenen Teilwerte und wird anschließend auf `[0, 1]` geklemmt. Damit kann
eine Klasse durch Quellumfang, Memberzahl oder Fragmentierung sichtbar groß
werden, ohne dass dafür mehrere künstliche Nodes entstehen.

PageRank behandelt Zyklen nach der üblichen iterativen Berechnung mit
Dämpfungsfaktor `0.85`, maximal `50` Iterationen und Abbruchgrenze `1e-8`.
Der Startvektor ist gleichverteilt; Dangling-Nodes verteilen ihre Masse
gleichverteilt. Konvergenz wird über die maximale absolute Wertänderung
geprüft. Die Reihenfolge der Eingaben und Iterationen ist stabil. Der Adapter
liefert die Roh- und abgeleiteten Metriken mit Definitionen, der Viewer
entscheidet über die Darstellung.

`importance` und `footprint` sind bereits normierte Score-Metriken im Bereich
`[0, 1]`. Rohmetriken wie `loc`, `fanIn` oder `memberCount` bleiben separat
erhalten. Der Adapter berechnet alle Scores erst nach vollständiger Analyse in
einem eigenen Aggregationsschritt; kein Node erhält einen vorläufigen Score
auf Basis des bisher gesehenen Maximums. Der Viewer wendet auf Scores nur noch
seine allgemeine `baseSize`-Darstellung an und normalisiert sie nicht erneut
über die gerade sichtbare Teilmenge.

## Technische Zielarchitektur

Die Aufteilung folgt fachlichen Verantwortungen, nicht einzelnen Methoden.
Der Zielzuschnitt ist:

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

Die aktuelle Grundlage besteht bewusst nur aus CLI und Testprojekt. Weitere
Projekte werden erst angelegt, wenn die jeweilige Verantwortung tatsächlich
benötigt wird; keine Schicht wird als Platzhalter für eine mögliche Zukunft
angelegt. Pro Quellverzeichnis gelten die zentrale Dateigrenze aus
`scripts/quality-config.mjs` und die allgemeinen Regeln aus `.agents/rules/`;
große Klassen und Methoden werden nach Verantwortung geteilt.

## Roslyn-Analyse

Die Analyse nutzt `Microsoft.CodeAnalysis` und die C#-Workspace-/MSBuild-
Integration. Die Pipeline bleibt nachvollziehbar:

1. Argumente und Pfade prüfen.
2. Die Solution laden und Projekte samt Dokumenten bestimmen.
3. Kompilationen und Semantic Models kontrolliert anfordern; Semantic Models
   werden dokumentweise verarbeitet und danach freigegeben.
4. Deklarationen in einer stabilen Reihenfolge in Nodes überführen.
5. Containment- und Referenzbeziehungen ergänzen.
6. Aufruf- und Memberzugriffsbeziehungen aus Syntax plus Semantic Model
   auflösen.
7. Externe, Framework- oder nicht auflösbare Symbole entsprechend der
   Scope-Policy aus dem Graphen fernhalten und in der Konsolensummary zählen.
8. Metriken und Summary-Links berechnen.
9. Graphvertrag validieren und atomar schreiben.

Teure globale Referenzsuche in Schleifen ist zu vermeiden. Die Analyse muss
linear in Dokumenten und Symbolen sowie annähernd linear in Beziehungen
arbeiten; All-Pairs-Vergleiche sind nicht zulässig. PageRank läuft mit dem
festgelegten Dämpfungsfaktor, einer maximalen Iterationszahl von `50` und der
Abbruchgrenze `1e-8`. Parallelisierung ist erst nach einem korrekten
sequenziellen Referenzpfad und einer Messung der Deterministik zulässig.

## Tests und Qualität

Die vorhandene Grundlage weist derzeit nur Solution-Struktur, Temp-Verzeichnis-
Lebenszyklus und Linter-Kompatibilität nach. Der vollständige Adapter ist erst
fertig, wenn mindestens folgende Verhalten nachgewiesen sind:

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
- 180.000 LOC als Skalierungsprüfung ohne zufällige Top-Kürzung und ohne
  quadratische Analysephase,
- deklarierte Übersicht-, Architektur- und Member-Detailstufen für große
  Graphen,
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
- `docs/05-Roadmap.md` bei einem klaren globalen Taskereignis wie Start,
  Blockierung, Scopeänderung oder Abschluss,
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
