# CodeVisualisierung

Quellenneutraler Browser-Viewer für vernetzte Daten: Ein versioniertes Graph-JSON wird lokal im Browser geladen und als interaktives Universum visualisiert.

> **Projekt eingestellt**
>
> Die Weiterentwicklung dieses Projekts wird beendet. Die Visualisierung sieht
> eindrucksvoll aus, liefert für große Codebasen aber keinen ausreichenden
> praktischen Überblick und hilft insbesondere nicht zuverlässig dabei,
> Architektur- oder Konzeptdrift zu erkennen. Dichte Graphen mit tausenden
> Nodes und Kanten sind als Gesamtansicht nicht sinnvoll auswertbar; gezielte
> MCP-Abfragen, Metriken und Audit-Reports sind für diese Fragen besser
> geeignet.
>
> Der bestehende Stand bleibt als technisches Experiment, Demo und historische
> Referenz erhalten. Ein Screenshot des letzten visuellen Stands ist unter
> [docs/screenshot.png](docs/screenshot.png) abgelegt.

Der Name des Projekts kommt aus dem ersten Anwendungsfall. Die Visualisierung soll jedoch nicht auf C# beschränkt bleiben.

## Aktueller Fokus

Zuerst entsteht die statische Visualisierungsanwendung:

- JSON per Datei-Upload oder Drag-and-drop laden
- Nodes und gerichtete oder ungerichtete Links darstellen
- Metriken nachvollziehbar auf Größe, Farbe und Linienbreite abbilden
- Nodes auswählen, fokussieren und ihre Nachbarschaft untersuchen
- Beispiele von minimal bis groß sowie semantische Detailstufen verwenden
- nach Node-Art, Gruppe, Tag und Link-Art filtern und aktive Metriken wechseln
- ohne Backend auf einfachem Webspace deploybar sein

## Lokaler Ablauf

```text
npm install
npm run check
npm run build
```

Der statische Build liegt danach unter `dist/viewer/`. Die deterministische
Aufbereitungs-Messung läuft mit `npm run benchmark`.

Der separate C#-/Roslyn-Adapter analysiert Code und erzeugt dasselbe
Graphformat als veröffentlichbares .NET-10-CLI-Artefakt.

## Struktur

```text
.
├── apps/
│   └── viewer/                 # Statische Browser-Anwendung
├── contracts/
│   └── graph-universe/         # Graph-Universe 1.0, Schema und Fixtures
├── adapters/
│   └── csharp/                 # .NET-10-C#-/Roslyn-Adapter
├── docs/                       # Vision, UX, Format, Quellen und Roadmap
└── .gitignore
```

## Dokumentation

Die Dokumentation beginnt bei [docs/README.md](docs/README.md).

## Grundregel

Der Viewer kennt nur den kanonischen Vertrag `graph-universe` 1.0. Eine Quelle
liefert neutrale Type-IDs, optionale `visualRole`-/`baseSize`-Hinweise,
benannte Metriken, `groupField`-Werte, Containment- und Summary-Links sowie
View- und Layoutprofile. Der Viewer bereitet daraus visuelle Größe und
deterministische Positionen auf. C#- oder andere Quellen erhalten keine
Sonderlogik im Viewer.
