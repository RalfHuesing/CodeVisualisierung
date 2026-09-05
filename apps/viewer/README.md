# Viewer

Hier entsteht die statische Browser-Anwendung.

Der Viewer darf nur vom Graphvertrag in `contracts/graph-universe/` abhängen. Er soll weder C#-Parser noch Git-Analyse enthalten.

Geplanter Einstieg:

- `index.html`
- `src/` für Anwendungscode
- `tests/` für Browser- und Interaktionstests
- `public/` für unveränderte statische Assets

Der Build erzeugt ein statisch hostbares Ergebnis in `dist/`.
