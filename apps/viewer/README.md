# Viewer

Hier entsteht die statische Browser-Anwendung.

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
