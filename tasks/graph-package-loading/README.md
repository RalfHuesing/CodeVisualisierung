# Task: Graph-Pakete für große lokale Projekte

Dieser Ordner beschreibt den Umsetzungsauftrag für große Graphen, die ein
Benutzer lokal aus seiner C#-Solution erzeugt und anschließend in den statisch
gehosteten Viewer lädt.

Der Benutzer soll weiterhin genau eine Datei auswählen. Diese Datei ist ein
`.graphpack`-Archiv mit Manifest, Overview und lazy ladbaren Teilgraphen. Der
Browser liest die ausgewählten Einträge lokal über die File-API; die Daten
werden nicht an den Hetzner-Webspace hochgeladen.

- [CONCEPT.md](CONCEPT.md) beschreibt Intention, Scope, Nicht-Ziele und
  Zielarchitektur.
- [ROADMAP.md](ROADMAP.md) ist der spätere Orchestrator-Arbeitsvertrag mit
  Slices, Abhängigkeiten und Abschlusskriterien.
- [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) enthält die noch zu entscheidenden
  fachlichen Richtungen.

Der Konzeptvertrag ist freigegeben; die Umsetzung ist noch nicht gestartet.
Die geschlossenen Richtungsentscheidungen stehen im
[Entscheidungslog](OPEN-QUESTIONS.md).

## Späterer Aufruf

> Setze `tasks/graph-package-loading` als Orchestrator um und bearbeite den
> vollständigen Task.
