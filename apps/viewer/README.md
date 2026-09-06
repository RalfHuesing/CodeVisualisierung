# Viewer

Die statische Browser-Anwendung lädt und visualisiert Graph-Universe-1.0-Daten.

## Abgeschlossener Meilenstein

Der Viewer steht in Version **1.0.0** als schema-gesteuerter, statisch hostbarer
3D-Viewer bereit. Der fachliche Umfang umfasst den Graph-Universe-Vertrag 1.0,
deklarative View-Profile und Projektionen, domänenneutrale Referenz-Fixtures,
Browser-Qualitätstests sowie dokumentierte Skalierungs- und Vollmodusgrenzen.

Die Version 1.0.0 bezeichnet die abgeschlossene Viewer-Lieferung. Die Version
`1.0` in den Graphdaten bezeichnet die kanonische Version des Graph-Universe-
Datenvertrags.

Der Viewer darf nur vom Graphvertrag in `contracts/graph-universe/` abhängen. Er soll weder C#-Parser noch Git-Analyse enthalten.

Geplanter Einstieg:

- `index.html`
- `src/main.js` als Anwendungseinstieg
- `src/domain/` für quellenneutrale Graphlogik
- `src/rendering/` für die Darstellung und ihre Berechnungen
- `src/styles/` für nach Verantwortung getrennte Stylesheets
- `tests/` für Browser- und Interaktionstests
- `public/` für unveränderte statische Assets

Der Build erzeugt ein statisch hostbares Ergebnis in `dist/`.

## Bedienung

Die Seite startet mit dem minimalen Graphen. Über „Beispiel laden“ stehen kleine,
mittlere, große und deterministische Belastungsgraphen zur Verfügung. Eine lokale
Graph-JSON-Datei kann über „JSON laden“ oder per Drag-and-drop geöffnet werden.

Node- und Linkmetriken steuern Größe und Breite. Art-, Gruppen-, Tag- und Linkart-
Filter begrenzen die sichtbare Teilmenge; View-Profile bestimmen generisch über
sichtbare Type-IDs und Link-Type-IDs die semantische Detailstufe. Der vollständige
Graph bleibt im Browserzustand erhalten und kann über „Filter löschen“ wieder
sichtbar gemacht werden.

## Vertragsaufbereitung

Die Quelle liefert neutrale Node- und Linktypen, Rollen, benannte Metriken,
Gruppenwerte, Containment-/Summary-Links und deklarative Profile. Ein Node-Typ
kann mit `visualRole` und `baseSize` seine fachliche Rolle und eine relative
Grundgröße beschreiben; `metricDefinitions` benennt Metriken wie `importance`,
deren konkrete Werte an Nodes oder Links liegen. Der Viewer wählt die aktive
Metrik, skaliert sie auf visuelle Größe bzw. Linkbreite und wendet die
`baseSize` an.

Ein Layoutprofil legt über `groupField`, `groupDistance`, `defaultDistance` und
`containmentDistances` fest, wie Gruppen, normale Links und hierarchische
Containment-Beziehungen initial angeordnet werden. Der Viewer erzeugt daraus
deterministische `x`-/`y`-/`z`-Initialpositionen; die anschließende Physik darf
die Szene nur weiter verfeinern. Ein angefordertes unbekanntes Layoutprofil
fällt auf das erste deklarierte Profil zurück. Fehlen Profile, Gruppenwerte,
Abstände, Metriken oder visuelle Tokens, verwendet der Viewer stabile neutrale
Defaults statt quellen- oder domänenspezifischer Sonderfälle.

Die mitgelieferten JSON-Fixtures liegen unter `contracts/graph-universe/fixtures/`.
