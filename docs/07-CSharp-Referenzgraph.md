# C#-Referenzgraph für die C#-Datenquelle

## Zweck und Abgrenzung

Dieses Dokument beschreibt den fachlichen Vertrag des C#-/Roslyn-Exporters. Es
erweitert nicht den allgemeinen Viewer um C#-Wissen. Der Exporter
liefert ein Graph-JSON nach dem allgemeinen Modell aus
[Graphmodell und Visualisierungsprofile](06-Graphmodell-und-Visualisierungsprofile.md).

Die C#-Datenquelle gehört ausdrücklich nicht zur aktuellen Viewer-Umsetzung.
Sie dient jetzt als anspruchsvollster Referenzfall für Vertrag, Fixtures,
Projektionen und Performance.

## Knotenebenen

Der Referenzgraph sollte mindestens diese frei definierbaren `nodeTypes`
abbilden können:

- Solution
- Project
- Assembly
- Module
- Namespace
- File
- Class
- Interface
- Record
- Struct
- Enum
- Delegate
- Method
- Constructor
- Property
- Field
- Event
- Operator
- Local function
- Type parameter
- External type oder External assembly (allgemeiner Vertragsfall, nicht im
  C#-Adapter-v1-Export)

Nicht jede Ansicht zeigt alle Ebenen gleichzeitig. Eine Übersicht kann
Assemblies und Namespaces zeigen, eine Typansicht Klassen und Interfaces und
eine Detailansicht Methoden und Member.

## Beziehungen

Der implementierte Exporter liefert unter anderem folgende `linkTypes`:

- `contains` für Solution, Project, Namespace, File, Type und Member
- `declares` für deklarierende Beziehungen
- `calls` für Methodenaufrufe
- `inherits` für Basistypen
- `implements` für Interfaces
- `overrides` für überschreibende Member
- `constructs` für Konstruktoraufrufe
- `reads` und `writes` für Feld- und Propertyzugriffe
- `uses-type`, `returns-type` und `parameter-type`
- `references-assembly` und `project-reference`
- `generated-from` ist im allgemeinen Vertrag reserviert, wird im C#-Adapter-v1
  wegen der ausgeschlossenen generierten Artefakte jedoch nicht emittiert.
- `tests` für Test- und Produktbeziehungen

Richtung, Herkunft, Metriken und Belege gehören in die Daten. Ein unbekannter
oder nicht unterstützter Beziehungstyp muss trotzdem als Link sichtbar bleiben.

## Identität

Node-IDs müssen innerhalb eines Graphdokuments stabil und eindeutig sein. Für
C# sind vollständig qualifizierte Signaturen notwendig, damit Namespaces,
überladene Methoden, generische Typen, Teilklassen und gleichnamige Member
unterscheidbar bleiben.

Der menschenlesbare `label` darf kürzer sein als die ID. Die Detailansicht
benötigt zusätzlich qualifizierten Namen, Assembly, Projekt, Datei und – falls
vorhanden – Quellposition.

## Externe Assemblies

Der allgemeine Graphvertrag kann externe Assemblies und externe Typen als Nodes
mit Attributen wie `external`, `assemblyName`, `packageName` oder `framework`
führen. Der C#-Adapter-v1-Task nutzt diese Möglichkeit bewusst nicht: Externe,
Framework- und generierte Artefakte bleiben außerhalb des exportierten Graphen.
Ihre verworfenen Beziehungen werden in der CLI-Summary gezählt. Externe Nodes
gehören nicht zum v1-Export; die v1-Identitäten und die Viewerlogik bleiben
dadurch quellenneutral.

## Metriken

Der implementierte Adapter liefert folgende benannte Metriken:

- Methoden- und Typkomplexität
- Quellcodezeilen
- Fan-in und Fan-out
- Aufrufanzahl
- Abhängigkeitsstärke
- Sichtbarkeit oder API-Status
- globale Scores und Summary-Projektionen

Diese Metriken sind keine Pflichtbestandteile des allgemeinen Graphschemas. Der
Exporter definiert die gelieferten Metriken mit Einheit und Beschreibung und
bietet sie für View-Profile und Legenden an.

Der v1-Adapter liefert `loc`, `cyclomaticComplexity`, `fanIn`, `fanOut`,
`weightedFanIn`, `weightedFanOut`, `pageRank`, `fileCount`, `typeCount`,
`memberCount` und – nur bei tatsächlich partiellen Named Types –
`partialDeclarationCount` als Rohmetriken. `importance` und `footprint` sind dagegen mit
`valueKind: "normalized-score"` und `range: [0, 1]` deklarierte, nach der
vollständigen Analyse berechnete Scores. Fehlende fachlich nicht anwendbare
Metriken werden ausgelassen; sie werden nicht durch `0` ersetzt.

## Projektionen für C#

Für den C#-Referenzgraphen werden Summary-Links auf mehreren Ebenen empfohlen:

```text
Methode A --calls--> Methode C
Klasse B --depends-on--> Klasse E
Namespace F --depends-on--> Namespace G
Assembly X --references--> Assembly Y
```

Die höher aggregierten Links müssen auf ihre Quellbeziehungen zurückführbar
sein, wenn das für Details oder Metriken relevant ist. Das gilt auch für
Member→Typ-Beziehungen wie `uses-type`, `returns-type`, `parameter-type` und
`constructs`, damit Architekturprofile keine Abhängigkeiten verlieren.
Dadurch kann der Viewer
bei ausgeblendeten Methoden weiterhin Namespace- und Klassenbeziehungen
anzeigen, ohne die Fachlichkeit selbst zu rekonstruieren. Der Adapter schreibt
diese Links vollständig ins JSON. Ihre Herkunft steht in `derivedFrom` und
`attributes.aggregation`; `occurrences` und `relationshipWeight` werden aus
den Detailbeziehungen summiert. Partial Types bleiben ein Node, während LOC
und Physical Footprint alle eigenen Quelldateien der Teildeklarationen
berücksichtigen.

## Referenz-Fixture

Die handgeschriebene, deterministische JSON-Fixture dient als gemeinsamer
Graph-Universe-Vertragstest. Die tatsächlichen C#-CLI-E2E-Tests verwenden die
physische `CSharpReferenceMini`-Solution. Die JSON-Fixture enthält mindestens:

- mehrere Projekte und Assemblies
- interne und externe Assemblyreferenzen
- Namespaces mit Klassen, Interfaces, Records und Enums
- überladene und generische Methoden
- Vererbung, Interfaceimplementierung und Overrides
- Methodenaufrufe über mehrere Namespace- und Projektebenen
- Dateien, Partial Types und generierte Artefakte
- benannte Metriken und Summary-Links

Diese Fixture ist ein aktiver allgemeiner Vertragstest. Sie ist kein Bestandteil
der CLI-Ausgabe und kein Bestandteil der allgemeinen Viewer-Fixtures; sie setzt
keine C#-Infrastruktur im Browser voraus.

Die konkrete deterministische Fixture liegt unter
[`contracts/graph-universe/fixtures/csharp-reference.json`](../contracts/graph-universe/fixtures/csharp-reference.json).
Sie wird bewusst nicht im allgemeinen Viewer-Katalog registriert. Der
deterministische Vertragstest in
[`tests/graph-validation.test.mjs`](../tests/graph-validation.test.mjs) prüft
Schema- und Referenzgültigkeit sowie die geforderten Projekt-, Assembly-, Typ-,
Methoden-, Metrik- und mehrstufigen Summary-Beispiele.
