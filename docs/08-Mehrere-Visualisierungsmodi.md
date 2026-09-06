# Mehrere Visualisierungsmodi

## Status und Ziel

Dieses Dokument beschreibt ein späteres Feature. Der aktuelle Viewer bleibt im
Version-1.0-Umfang ein statischer 3D-Viewer. Die Graphdatenbasis soll künftig
mit mehreren Darstellungsarten nutzbar sein, ohne dass Datenquelle oder
Graphstruktur gewechselt werden müssen.

Mögliche Modi sind:

- **Stadtkarte:** stabile 2D-Architekturkarte mit Stadtteilen, Gebäuden und
  Verkehrswegen.
- **Universum:** die bestehende freie 3D-Exploration im Kraftfeld.
- **Biologie:** eine optionale organische Netzwerkansicht, zum Beispiel als
  Myzel oder Nervensystem.

Die Modi sind visuelle Perspektiven auf denselben Graphen, keine verschiedenen
Graphmodelle.

## Fachliche Leitentscheidung

Die Eingabe bleibt ein einziges `graph-universe`-JSON:

```text
Graph-JSON
  → Validierung und Normalisierung
  → semantische Graphaufbereitung
  → Visualisierungsmodus
  → konkretes Layout und Rendering
```

Die Datenquelle liefert Nodes, Links, Rollen, Hierarchie und Metriken. Der
Viewer entscheidet, wie diese Informationen in einem gewählten Modus sichtbar
werden. Renderer-spezifischer JavaScript-Code gehört nicht in die JSON-Datei.

## Gemeinsame und modusspezifische Zustände

Diese Zustände bleiben beim Umschalten erhalten:

- Auswahl über stabile Node-ID
- Suche und Filter
- sichtbares `viewProfile` und die Detailstufe
- aktive Node- und Linkmetriken
- ausgeblendete oder sichtbare Typen und Beziehungen

Diese Zustände sind pro Modus getrennt:

- Kameraposition und 3D-Orbit
- 2D-Positionen und Stadtteilgrenzen
- berechnete Kantenführung und Routing
- biologische Animationen oder organische Geometrien

Damit kann ein Nutzer zum Beispiel in der Stadtkarte eine Klasse auswählen,
auf den Universumsmodus wechseln und dieselbe Klasse dort fokussiert sehen.

## Beispielhafte semantische Abbildung

| Graphbedeutung | Stadtkarte | Universum | Biologie |
|---|---|---|---|
| Container | Stadtteil oder Gebäude | großer Körper | Organ oder Zellverband |
| Klasse oder Typ | Gebäude | Node-Körper | Zelle |
| Methode oder Member | Raum, erst bei Detailzoom | kleiner Node | Zellbestandteil |
| Dependency | Straße oder Transitlinie | gerichteter Link | Hyphen oder Nervenbahn |
| `calls` | Verkehrsfluss | Pfeilbeziehung | Signalfluss |
| externe Abhängigkeit | außerhalb der Stadt | fremdes System | Umwelt |
| generiertes Artefakt | markierter Baubereich | eigener Node-Typ | künstliches Gewebe |

Die Metaphern erklären die Ansicht, ersetzen aber nicht die fachliche Legende.
Beziehungstyp, Richtung und aktive Metrik müssen weiterhin sichtbar bleiben.

## Geplante Renderer

### Stadtkarte

Die Stadtkarte ist der bevorzugte Kandidat für eine spätere Architekturansicht.
Containment-Beziehungen bestimmen stabile, verschachtelte Bereiche. Projekte,
Assemblies oder Namespaces werden als Stadtteile gruppiert; Typen erscheinen
als Gebäude. Querbeziehungen werden als Linien oder Transitstrecken außerhalb
der Container gezeichnet.

Die Position soll dadurch fachlich stabiler sein als im Kraftfeld. Ein Wechsel
der Metrik darf nicht die gesamte Karte unnötig neu mischen.

### Universum

Der bestehende 3D-Modus bleibt als freie Übersicht und für räumliche
Exploration erhalten. Die Physik ist weiterhin nur eine Layout-Hilfe. Die
räumliche Nähe darf keine fachliche Beziehung vortäuschen.

### Biologie

Der biologische Modus ist ein optionales visuelles Profil. Ein Myzel eignet
sich für Cluster, Kopplung und zentrale Nodes. Ein Nervensystem eignet sich
besser für gerichtete Call- oder Datenflüsse. Beides darf nur dann animiert
werden, wenn die zugrunde liegende Metrik oder ein Zeitverlauf diese Bewegung
fachlich begründet.

Nicht jeder Graph ist für jede biologische Darstellung geeignet. Fehlen
Richtung, Gruppen oder Hierarchie, muss der Viewer den Modus einschränken oder
mit einem sichtbaren Fallback darstellen.

## Technische Zielarchitektur

Der Viewer benötigt eine kleine gemeinsame Renderer-Schnittstelle. Der aktuelle
3D-Renderer besitzt bereits die wesentlichen Operationen:

```text
render(graph, options)
updateOptions(options)
focus(nodeId)
reset()
destroy()
```

Künftige Renderer können diese Schnittstelle ebenfalls verwenden:

```js
const visualizationModes = {
  city: createCityRenderer,
  universe: createUniverseRenderer,
  biology: createBiologyRenderer
};
```

Die gemeinsame Graphaufbereitung bleibt von den Renderern getrennt. Sie liefert
validierte Nodes, Links, Rollen, Metriken, Auswahlzustand und Filter. Der
jeweilige Renderer berechnet daraus sein Layout.

## Vertrag und Profile

Der bestehende Vertrag enthält bereits wichtige Grundlagen:

- `nodeTypes` und `linkTypes` für deklarative Typen und Rollen
- `viewProfiles` für Detailstufen und sichtbare Typen
- `layoutProfiles` für räumliche Leitplanken
- `visualTokens` und `theme` für sichtbare Bedeutungen
- Summary-Links für aggregierte Detailstufen

`viewProfiles` und Visualisierungsmodi sollten fachlich getrennt bleiben:

- Ein `viewProfile` beantwortet: **Welche Detailstufe und welche Daten sind
  sichtbar?**
- Ein Visualisierungsmodus beantwortet: **Wie werden diese Daten angeordnet
  und dargestellt?**

Bei der ersten Umsetzung ist eine feste Registry eingebauter Modi ausreichend.
Falls Graphdateien später passende Modi deklarieren sollen, kann der Vertrag
um einen optionalen, rein deklarativen Bereich wie `visualizationProfiles`
erweitert werden. Eine Schemaänderung ist erst sinnvoll, wenn ein konkreter
Renderer und seine benötigten Felder feststehen.

## Technische Grenzen

- Ein Modus darf keine Rohdaten entfernen; er erzeugt nur eine sichtbare
  Projektion.
- Auswahl, Filter und Details müssen in jedem Modus verfügbar bleiben.
- Fehlende semantische Rollen führen zu dokumentierten Fallbacks, nicht zu
  geratenen Bedeutungen.
- Layouts dürfen bei großen Graphen aggregieren, müssen Umfang und Detailstufe
  aber sichtbar anzeigen.
- Animationen bleiben abschaltbar und dürfen keine Laufzeitaktivität
  vortäuschen.

## Spätere Umsetzungsscheiben

1. Semantische Fähigkeiten des Graphen explizit für Renderer auswerten.
2. Gemeinsame Renderer-Schnittstelle und Modusumschaltung einführen.
3. Stadtkarte mit stabiler Hierarchie und Querbeziehungen prototypisch bauen.
4. Gemeinsame Auswahl-, Filter- und Detailzustände zwischen Modi prüfen.
5. Biologischen Modus als optionales Myzel-/Signalprofil evaluieren.
6. Erst danach entscheiden, ob `visualizationProfiles` Teil des Vertrages wird.

## Akzeptanzkriterien für das spätere Feature

- Eine Graph-JSON kann ohne Duplizierung in mindestens zwei Modi geöffnet
  werden.
- Ein ausgewählter Node bleibt beim Moduswechsel ausgewählt.
- Filter, Suche, Metriken und Details behalten ihre Bedeutung.
- Jeder Modus besitzt eine eigene Legende für seine visuellen Kodierungen.
- Der Viewer kennzeichnet nicht unterstützte oder eingeschränkte Modi
  verständlich.
- Der bestehende Universumsmodus bleibt unverändert nutzbar.
