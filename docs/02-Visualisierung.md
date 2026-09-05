# Visualisierung – aktueller Fokus

## Zielbild

Die Anwendung ist eine statische Webseite. Es gibt keinen eigenen Server und keine projektspezifische Laufzeitumgebung.

Der Ablauf im Browser:

1. JSON-Datei auswählen oder in die Seite ziehen.
2. JSON parsen und gegen das Graphformat validieren.
3. Daten normalisieren und fehlende optionale Werte mit sicheren Defaults ergänzen.
4. Graph in einer interaktiven Szene darstellen.
5. Auswahl, Fokus und Detailinformationen anbieten.

Die Datei bleibt lokal im Browser. Ein späteres Laden einer mitgelieferten Beispieldatei ist möglich, aber kein notwendiger Bestandteil des Uploads.

## MVP-Schnitt

### MVP 0 – Daten sichtbar machen

- statische App-Hülle,
- Beispieldatensatz,
- Datei-Upload und Drag-and-drop,
- verständliche Fehlermeldungen bei ungültigem JSON,
- Anzeige von Node- und Linkanzahl.

### MVP 1 – Interaktiver Graph

- 3D-Force-Graph mit Kameraorbit, Zoom und Pan,
- Nodes als klar unterscheidbare Körper,
- Links als Linien,
- gerichtete Links mit Pfeilspitzen,
- Knotengröße aus einer ausgewählten numerischen Metrik,
- Linkbreite aus `weight` oder einer ausgewählten Linkmetrik,
- Legende für die aktive visuelle Kodierung.

### MVP 2 – Verstehen statt nur Betrachten

- Klick oder Tastaturauswahl eines Nodes,
- Detailbereich mit Label, Art, Tags, Metriken und Attributen,
- direkte Nachbarn hervorheben,
- nicht relevante Nodes und Links dimmen,
- „Kamera auf Auswahl fokussieren“,
- Ansicht zurücksetzen,
- Suche nach Label oder ID.

### Danach

- Gruppierung und semantischer Zoom,
- stabile lokale Orbits für Nodes mit gemeinsamer Gruppe,
- gespeicherte Ansichtsprofile,
- Zeitverlauf und Live-Deltas,
- visuelle Zustände wie Hitze, Testabdeckung oder Änderungsfrequenz.

## Visuelle Grammatik

Die erste Version verwendet wenige, nachvollziehbare Kanäle:

| Datenbedeutung | Darstellung | Regel |
|---|---|---|
| Node-Art oder Kategorie | Form oder dezente Farbe | Kategorien nicht nur über Farbe unterscheiden |
| Node-Metrik | Radius | lineare Skalierung nur bei passenden Wertebereichen, sonst robuste/sqrt-Skalierung |
| Node-Metrik | Farbe | maximal eine kontinuierliche Skala gleichzeitig |
| Linkrichtung | Pfeil oder bewegter Marker | Animation optional und abschaltbar |
| Linkstärke | Linienbreite oder Deckkraft | kleine Werte müssen trotzdem sichtbar bleiben |
| Auswahl/Fokus | Hervorhebung und Dimmen | keine Veränderung der zugrunde liegenden Metrik |

`mass`, `temperature`, `shieldActive` und ähnliche Begriffe sind keine festen Pflichtfelder des Graphformats. Sie können als benannte Metriken oder Attribute angeliefert und durch ein Ansichtsprofil interpretiert werden.

## 3D-Entscheidung

Der Viewer verwendet ausschließlich eine 3D-Ansicht. Ein 3D-Force-Graph ist sinnvoll, weil er Kamera- und Graphinteraktion sowie gerichtete Links als Ausgangspunkt anbietet. Die Graphdaten und die Visualisierungslogik werden hinter einer kleinen eigenen Abstraktion gehalten, damit wir den konkreten 3D-Renderer später austauschen können.

Die Physik ist eine Layout-Hilfe, keine fachliche Aussage. Ein Node ist nicht wirklich „schwer“, nur weil er groß dargestellt wird. Die Darstellung darf daher deterministische Positionen, Gruppen und feste Layoutregeln ergänzen.

## Bedienung

Pflichtinteraktionen für das MVP:

- Datei öffnen, Beispiel laden und Fehler verständlich anzeigen,
- Kamera drehen, zoomen und verschieben,
- Node auswählen und Detailbereich öffnen,
- Nachbarschaft des ausgewählten Nodes erkennen,
- Ansicht zurücksetzen,
- Animation reduzieren oder deaktivieren.

Die Anwendung soll auch ohne Hover verständlich bleiben. Eine WebGL-Szene allein ist nicht ausreichend: ausgewählte Daten müssen zusätzlich als zugängliche HTML-Information dargestellt werden.

## Skalierung

Die bisherige Idee „180.000 LOC in unter fünf Sekunden“ ist kein sinnvoller Viewer-Vertrag. LOC ist nicht gleich Nodeanzahl, und ein Browser kann nicht beliebig viele detaillierte 3D-Objekte interaktiv verwalten.

Wir messen stattdessen:

- maximale Graphgröße für den interaktiven Vollmodus,
- Ladezeit und Zeit bis zum ersten sichtbaren Bild,
- Bildrate beim Navigieren,
- Speicherbedarf,
- Verhalten bei fehlenden oder extremen Metrikwerten.

Für größere Graphen brauchen wir später Aggregation, Detailstufen und gegebenenfalls vorgegebene Positionen. Pruning darf nicht stillschweigend Daten löschen; es muss als Darstellungsentscheidung sichtbar und rückgängig machbar sein. Eine separate 2D-Ansicht ist nicht Bestandteil des Produkts.

Die aktuelle Übersicht blendet ausschließlich Nodes mit `kind: "method"` aus. Die Detailstufe ist sichtbar auswählbar und über „Detail“ vollständig rückgängig machbar; die Quelldaten bleiben dabei unverändert.

Der deterministische Aufbereitungs-Benchmark läuft mit `npm run benchmark` über feste Fixtures und 20 Wiederholungen. Der aktuelle Performance-Graph umfasst 684 Nodes und 1.260 Links; auf dem Entwicklungsrechner lag die Aufbereitung bei 7,94 ms pro Durchlauf. Das ist noch kein Versprechen für die WebGL-Bildrate, sondern ein reproduzierbarer Grenzwert für Filterung und visuelles Mapping.
