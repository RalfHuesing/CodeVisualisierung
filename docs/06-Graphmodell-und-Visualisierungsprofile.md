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

Der Viewer darf zusätzliche freie Filter anbieten. Ein Profil legt eine
Darstellungsentscheidung fest, nicht eine neue Datenquelle und keine neue
Graphstruktur.

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
