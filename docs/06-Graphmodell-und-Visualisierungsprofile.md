# Graphmodell und Visualisierungsprofile

## Zweck

Der Viewer visualisiert ein allgemeines Graph-JSON. Er kennt keine C#-Begriffe,
keine Roslyn-Symbole und keine fachliche Domäne. Namespace, Klasse, Person,
Unternehmen oder Familienmitglied sind für den Viewer lediglich vom Graph
definierte Node-Typen.

Dieses Dokument beschreibt die deklarativen Erweiterungen des kanonischen
`graph-universe`-Vertrags 1.0.

## Bausteine des Graphdokuments

Ein Graphdokument besteht weiterhin aus flachen `nodes` und `links`. Die
fachliche Bedeutung wird zusätzlich durch Definitionen im Dokument beschrieben:

- `nodeTypes` beschreibt mögliche Node-Typen, deren Label und optionale
  Visualisierungstokens.
- `linkTypes` beschreibt mögliche Beziehungstypen, Richtung und optionale
  Visualisierungstokens.
- `metricDefinitions` erklärt numerische Metriken, Einheiten und ihre
  Verwendungsmöglichkeiten.
- `facets` beschreibt Werte, nach denen der Viewer Filter erzeugen kann.
- `viewProfiles` beschreibt fachliche Detailstufen und sichtbare Typen.
- `layoutProfiles` beschreibt deklarative räumliche Abstände und
  containment-spezifische Regeln.
- `projections` beschreibt explizite oder ableitbare Beziehungen zwischen
  Detailstufen.
- `theme` beziehungsweise `visualTokens` beschreibt symbolische Farben,
  Formen, Linienarten und Hervorhebungen.

Die Definitionen sind Daten. Der Viewer erzeugt daraus Controls, Legenden,
Details und Darstellungsregeln. Begriffe wie `namespace` oder `calls` werden
nicht im JavaScript fest verdrahtet.

## Node-Typen

Ein Node referenziert einen definierten Typ, zum Beispiel:

```json
{
  "id": "type:orders",
  "typeId": "class",
  "label": "Orders",
  "groupId": "namespace:shop",
  "metrics": { "complexity": 12 },
  "attributes": { "external": false }
}
```

`typeId` ist eine ID aus `nodeTypes`. Weitere allgemeine Eigenschaften bleiben
erlaubt: Tags, benannte Metriken, Attribute, Gruppen, Quellenangaben und
optionale Positionen. Eine Hierarchie wird nicht durch verschachtelte JSON-
Objekte erzwungen. Sie wird über flache Nodes und deklarierte
Containment-Beziehungen modelliert.

`visualRole` und `baseSize` sind optionale typabhängige Visualisierungsdaten.
`visualRole` benennt die Rolle eines Typs, `baseSize` seine positive
Ausgangsgröße. Beides bleibt von Rohmetriken und konkreten Renderer-Effekten
getrennt.

## Link-Typen

Ein Link referenziert einen definierten Beziehungstyp:

```json
{
  "id": "call:orders-to-store",
  "source": "method:orders:list",
  "target": "method:store:query",
  "typeId": "calls",
  "directed": true,
  "metrics": { "callCount": 42 }
}
```

Der Viewer erhält damit Richtung, Bedeutung, Gewicht und Metriken, ohne den
Linktyp interpretieren zu müssen. Typische allgemeine Linkrollen sind
Containment, Abhängigkeit, Aufruf, Vererbung, Implementierung und Referenz.
Andere Domänen dürfen vollständig andere Typen liefern.

## Filter und Facetten

Filter werden aus den Graphdefinitionen erzeugt. Ein Graph kann zum Beispiel
Filter für `typeId`, `groupId`, Tags, Linktypen, boolesche Attribute oder
Metrikbereiche anmelden. Der Viewer stellt keine feste C#-Filterliste bereit.

Universelle Viewerfunktionen bleiben unabhängig davon erhalten:

- Suche nach stabiler ID und Label
- Auswahl und Nachbarschaft
- Sichtbarkeit und Ausblendung
- Reset der Ansicht
- Metrik- und Detailauswahl

Ein Filter verändert die Rohdaten nicht. Er verändert nur die sichtbare
Projektion und zeigt deren aktuellen Umfang an.

## Hierarchie und Projektion

Für komplexe Graphen reicht es nicht, beim Ausblenden von Methoden einfach alle
Methodenlinks zu entfernen. Wenn nur übergeordnete Nodes sichtbar sind, müssen
Beziehungen auf dieser Ebene weiterhin nachvollziehbar sein.

Die bevorzugte erste Lösung sind explizite Summary-Links im Graphen. Ein
Exporter oder eine vorgelagerte Graphaufbereitung kann beispielsweise sowohl
Methodenaufrufe als auch daraus aggregierte Klassen- und Namespacebeziehungen
liefern. Ein Summary-Link kann seine Herkunft über `derivedFrom` oder Metriken
dokumentieren.

Spätere `projections` dürfen solche Beziehungen deklarativ erzeugen. Die
Regeln müssen fachlich explizit sein und dürfen nicht aus unbekannten Feldnamen
oder beliebigen Pfaden erraten werden. Der Viewer darf eine Detailstufe daher
nie stillschweigend als Datenlöschung darstellen.

## View-Profile

Ein `viewProfile` kann eine verständliche Startansicht beschreiben:

```json
{
  "id": "overview",
  "label": "Übersicht",
  "visibleNodeTypes": ["namespace", "assembly"],
  "visibleLinkTypes": ["namespace-dependency", "assembly-reference"],
  "nodeMetric": "fanIn",
  "linkMetric": "dependencyWeight"
}
```

Das Profil mit der ID `overview` (bei domänenspezifischem Präfix `*-overview`)
ist die deklarierte Übersicht und damit die bevorzugte Standardansicht. Ein
höheres `detailLevel` macht ein Profil nicht automatisch zum Startprofil.

Der Viewer darf zusätzliche freie Filter anbieten. Ein Profil legt eine
Darstellungsentscheidung fest, nicht eine neue Datenquelle und keine neue
Graphstruktur.

## Spatial-Profile

Ein `layoutProfile` beschreibt räumliche Leitplanken, ohne einen konkreten
Renderer oder Algorithmus vorzuschreiben:

```json
{
  "id": "overview-space",
  "groupField": "groupId",
  "groupDistance": 48,
  "defaultDistance": 26,
  "containmentDistances": [
    { "parentTypeId": "group", "childTypeId": "item", "distance": 20 }
  ]
}
```

`groupField` ist ein Punktpfad am Node und fällt ohne Angabe auf `groupId`
zurück. `groupDistance`, `defaultDistance` und jede
`containmentDistances.distance` sind positive Zahlen. Ein `viewProfile` kann
mit `layoutProfileId` genau ein solches Profil referenzieren. Zusätzliche
optionale Layoutfelder bleiben für spätere fachliche Regeln offen und werden
vom generischen Vertrag toleriert.

## Referenzfixture `nested-universe.json`

`contracts/graph-universe/fixtures/nested-universe.json` prüft den Vertrag an
einer fünfstufigen, quellenneutralen Hierarchie:

```text
galaxy → system → star → planet → moon
```

Die Fixture enthält zwei unabhängige Gruppen, vier explizite
Containment-Regeln, zwei Layout- und View-Profile, benannte Node- und
Linkmetriken, gewichtete Referenzlinks und einen daraus abgeleiteten
Summary-Link. Sie ist damit ein kleiner Referenzfall für Größen, Abstände und
Zusammenhänge, ohne eine Quelle oder einen Parser vorauszusetzen.

Die vier Containment-Abstände sind deklarative Layout-Leitplanken. Der Viewer
setzt daraus deterministische Initialpositionen; die anschließende Force-Physik
kann diese Positionen zur Kollisions- und Linkoptimierung verfeinern. Ein
Abstand ist daher keine Zusage für eine dauerhafte Orbitbahn oder eine feste
geometrische Grenze.

## Visualisierungstokens

Fachliche JSON-Daten sollen keine Hexfarben in Node- oder Linktypen benötigen.
Typen referenzieren symbolische Tokens, zum Beispiel `group-blue`, `type-class`,
`relation-call` oder `shape-container`. Der Viewer löst diese Tokens über ein
Theme auf und kann dadurch dunkle, helle oder kontrastreiche Themes anbieten.

Unbekannte Tokens erhalten einen sichtbaren, dokumentierten Fallback und führen
nicht zum Absturz. Die Legende muss aktive Tokens und ihre Bedeutung erklären.

## Domänenneutralität

Ein Stammbaum kann Personen und Eltern-Kind-Beziehungen liefern. Ein
Firmengeflecht kann Unternehmen, Beteiligungen und Lieferbeziehungen liefern.
Ein C#-Graph kann Assemblies, Namespaces, Typen und Methoden liefern. Alle drei
verwenden dieselbe Viewerlogik: Definitionsdaten, flache Nodes, flache Links,
Filter, Projektionen und Visualisierungstokens.
