# C#-Referenzgraph für die spätere Datenquelle

## Zweck und Abgrenzung

Dieses Dokument ist ein fachlicher Zielentwurf für den späteren C#-/Roslyn-
Exporter. Es erweitert nicht den allgemeinen Viewer um C#-Wissen. Der Exporter
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
- External type oder External assembly

Nicht jede Ansicht zeigt alle Ebenen gleichzeitig. Eine Übersicht kann
Assemblies und Namespaces zeigen, eine Typansicht Klassen und Interfaces und
eine Detailansicht Methoden und Member.

## Beziehungen

Der Exporter soll unter anderem folgende `linkTypes` liefern können:

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
- `generated-from` für generierte Artefakte
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

Externe Assemblies und externe Typen werden nicht als fehlende Linkziele
behandelt. Sie können als Nodes mit allgemeinen Attributen wie `external`,
`assemblyName`, `packageName` oder `framework` geliefert werden. Der Viewer
kann sie standardmäßig dimmen oder über ein View-Profil ausblenden, ohne die
Beziehung aus dem Quelldokument zu entfernen.

## Metriken

Mögliche benannte Metriken sind:

- Methoden- und Typkomplexität
- Quellcodezeilen
- Fan-in und Fan-out
- Aufrufanzahl
- Abhängigkeitsstärke
- Sichtbarkeit oder API-Status
- Testabdeckung, sofern die Datenquelle sie liefert

Keine dieser Metriken ist Pflichtbestandteil des allgemeinen Graphschemas. Der
Exporter definiert sie mit Einheit und Beschreibung und bietet sie für
View-Profile und Legenden an.

## Projektionen für C#

Für den C#-Referenzgraphen werden Summary-Links auf mehreren Ebenen empfohlen:

```text
Methode A --calls--> Methode C
Klasse B --depends-on--> Klasse E
Namespace F --depends-on--> Namespace G
Assembly X --references--> Assembly Y
```

Die höher aggregierten Links müssen auf ihre Quellbeziehungen zurückführbar
sein, wenn das für Details oder Metriken relevant ist. Dadurch kann der Viewer
bei ausgeblendeten Methoden weiterhin Namespace- und Klassenbeziehungen
anzeigen, ohne die Fachlichkeit selbst zu rekonstruieren.

## Referenz-Fixture

Vor einem echten Roslyn-Adapter wird eine handgeschriebene oder deterministisch
erzeugte C#-Referenz-Fixture benötigt. Sie soll mindestens enthalten:

- mehrere Projekte und Assemblies
- interne und externe Assemblyreferenzen
- Namespaces mit Klassen, Interfaces, Records und Enums
- überladene und generische Methoden
- Vererbung, Interfaceimplementierung und Overrides
- Methodenaufrufe über mehrere Namespace- und Projektebenen
- Dateien, Partial Types und generierte Artefakte
- benannte Metriken und Summary-Links

Diese Fixture ist ein späterer Vertragstest. Sie ist kein Bestandteil der
aktuellen allgemeinen Viewer-Fixtures und setzt keine C#-Infrastruktur im
Browser voraus.
