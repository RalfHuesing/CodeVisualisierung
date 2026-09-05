# C# / Roslyn Adapter

Dieser Adapter wird später C#-Projekte analysieren und Graph-JSON im Vertrag aus `contracts/graph-universe/` erzeugen.

Voraussichtliche Aufgaben:

- Projekte und Solutions mit Roslyn einlesen
- Namespaces, Typen und Methoden als Nodes exportieren
- Aufrufe und Abhängigkeiten als Links exportieren
- Code-Metriken als benannte Metriken ergänzen
- Ausgabe gegen das gemeinsame Schema und Fixtures testen

Der Adapter gehört nicht in den Browser-Viewer und wird erst nach dem ersten funktionierenden Viewer umgesetzt.
