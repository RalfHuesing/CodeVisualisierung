# CodeVisualisierung

Quellenneutraler Browser-Viewer für vernetzte Daten: Ein versioniertes Graph-JSON wird lokal im Browser geladen und als interaktives Universum visualisiert.

Der Name des Projekts kommt aus dem ersten Anwendungsfall. Die Visualisierung soll jedoch nicht auf C# beschränkt bleiben.

## Aktueller Fokus

Zuerst entsteht die statische Visualisierungsanwendung:

- JSON per Datei-Upload oder Drag-and-drop laden
- Nodes und gerichtete oder ungerichtete Links darstellen
- Metriken nachvollziehbar auf Größe, Farbe und Linienbreite abbilden
- Nodes auswählen, fokussieren und ihre Nachbarschaft untersuchen
- ohne Backend auf einfachem Webspace deploybar sein

Später folgt ein separater C#-/Roslyn-Exporter, der Code analysiert und dasselbe Graphformat erzeugt.

## Struktur

```text
.
├── apps/
│   └── viewer/                 # Statische Browser-Anwendung
├── contracts/
│   └── graph-universe/         # Versioniertes JSON-Format, Schema und Fixtures
├── adapters/
│   └── csharp/                 # Späterer C#-/Roslyn-Exporter
├── docs/                       # Vision, UX, Format, Quellen und Roadmap
└── .gitignore
```

## Dokumentation

Die Dokumentation beginnt bei [docs/README.md](docs/README.md).

## Grundregel

Der Viewer kennt nur den Graphvertrag. Datenquellen liefern Nodes, Links, Metriken und Attribute; die Anwendung entscheidet, wie diese Signale dargestellt werden.
