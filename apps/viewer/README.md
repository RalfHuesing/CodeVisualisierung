# Viewer

Hier entsteht die statische Browser-Anwendung.

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
Filter begrenzen die sichtbare Teilmenge; „Übersicht“ blendet Methodennodes als
semantische Detailstufe aus. Der vollständige Graph bleibt im Browserzustand erhalten
und kann über „Filter löschen“ wieder sichtbar gemacht werden.

Die mitgelieferten JSON-Fixtures liegen unter `contracts/graph-universe/fixtures/`.
