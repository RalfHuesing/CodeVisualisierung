# Offene Roadmap

## Zweck

Diese Datei ist der kurze globale Arbeitsindex des Projekts. Sie enthält nur
Vorhaben, deren Umsetzung ausdrücklich beschlossen wurde und die noch offen
sind. Abgeschlossene Arbeit wird entfernt und nicht als dauerhafte Checkliste
geführt.

Die fachlichen Entscheidungen stehen in den verlinkten Dokumenten. Die
Umsetzungsdetails, Slices, Akzeptanzkriterien und Prüfungen stehen in den
jeweiligen `tasks/<name>/`-Ordnern.

## Status

- `planned`: beschlossen, aber noch nicht gestartet
- `active`: aktuell in Umsetzung durch einen Orchestrator-Task
- `blocked`: offen, aber durch eine dokumentierte Richtungsentscheidung,
  Abhängigkeit oder externe Voraussetzung blockiert

## Reihenfolge der offenen Vorhaben

### 1. C#-/Roslyn-Adapter — `active`

Eine quellennahe Datenquelle soll aus C#-Solutions vertragskonformes
Graph-JSON erzeugen.

- [Taskvertrag](../tasks/c-sharp-adapter/README.md)
- [Umsetzungs-Roadmap](../tasks/c-sharp-adapter/ROADMAP.md)
- [C#-Referenzgraph](07-CSharp-Referenzgraph.md)

Abhängigkeit: Der gemeinsame Graphvertrag 1.0 bleibt maßgeblich. Ein
separater Viewer-Layoutvertrag darf nicht in den Adapter gezogen werden.

### 2. Mehrere Visualisierungsmodi — `planned`

Eine Graph-JSON soll später zwischen Stadtkarte, Universum und biologischer
Netzwerkansicht wechseln können.

- [Taskvertrag](../tasks/multi-visualization-modes/README.md)
- [Konzept](08-Mehrere-Visualisierungsmodi.md)

Abhängigkeit: Gemeinsame semantische Graphaufbereitung und Renderer-
Schnittstelle müssen vor zusätzlichen Modi geklärt werden.

## Governance

Die Roadmap wird nicht bei unverbindlichen Ideen, Fragen oder normalem
Brainstorming geändert. Ein Eintrag entsteht bei einer klaren
Umsetzungsentscheidung oder beim Anlegen eines neuen Tasks.

Der Orchestrator aktualisiert den Status bei Start, Blockierung,
Scopeänderung, Abbruch oder Abschluss. Nach erfolgreichem Abschluss wird der
Eintrag entfernt. Die Task-eigene Roadmap darf ihre Slice-Checklisten und den
Arbeitsverlauf behalten; diese werden nicht in diese Datei kopiert.
