# Offene Entscheidungen: C#-Adapter

Die Antworten aus dem Konzeptgespräch sind als `[X]` festgehalten. Offene
Punkte bleiben `[ ]` und werden vor dem jeweils betroffenen Slice entschieden.
Nach einer Entscheidung wird `CONCEPT.md` aktualisiert; diese Datei bleibt
als Entscheidungslog erhalten.

## Bereits entschieden

- [X] Produktname und Grundsyntax: `codegraph-csharp <input> --output
  <graph.json>` ohne zusätzliches `analyze`-Verb.
- [X] Eingaben: `.slnx`, `.sln` und `.csproj`. Eine Solution wird vollständig
  geladen; ein `.csproj` ist ein vollständiger Einzelprojekt-Graph.
- [X] Speicher-/Lademodell: Die komplette Eingabe wird in den Speicher
  geladen. Kein dynamisches Nachladen und kein Streaming.
- [X] Die CLI baut die analysierte Solution nicht und führt ihre Tests nicht
  aus.
- [X] Roslyn-/Compilerprobleme werden auf der CLI-Konsole gemeldet, nicht in
  den Graph geschrieben. Die Analyse arbeitet best effort weiter und erzeugt
  nach Möglichkeit ein valides, gegebenenfalls partielles JSON mit Summary und
  Zählungen.
- [X] Fehlendes Restore/SDK wird best effort behandelt: Verwertbare Projekte
  und Dokumente werden verarbeitet, Probleme werden gemeldet und eine gültige,
  gegebenenfalls partielle JSON-Ausgabe wird trotzdem versucht.
- [X] Analysegrenze: Nur Symbole aus den eigenen, ausdrücklich geladenen
  Quellprojekten werden exportiert. Externe Abhängigkeiten, `System.*`,
  Framework-Assemblies und generierte Artefakte bleiben außerhalb des Graphen.
- [X] Vollständigkeit: Das erste fertige Release deckt die vollständige
  fachliche Zielmenge des eigenen Sourcecodes aus
  `docs/07-CSharp-Referenzgraph.md` ab; externe/generierte Artefakte bleiben
  gemäß Scope-Policy ausgeschlossen. Ein kleiner MVP ist nur ein
  Zwischen-Slice.
- [X] Darstellung: Kurze Labels für Nodes; qualifizierter Name, Signatur,
  Projekt und solution-relative Quellposition mit Zeile/Spalte als sinnvolle
  Detaildaten zum Wiederfinden. Keine absoluten Pfade in IDs oder sichtbaren
  Labels.
- [X] Metrikrichtung: LOC und Komplexität sind Detailwerte, keine primäre
  Größenmetrik. `fanIn`, `fanOut`, `callCount`/gewichtete Grade und PageRank
  bilden zunächst die Grundlage für `importance`; Betweenness bleibt optional
  und nachgelagert.
- [X] Schema-Kopplung: Die CLI validiert jede erzeugte Ausgabe selbst gegen
  `contracts/graph-universe/schema/graph-universe.schema.json`.
- [X] Teststruktur: zunächst ein Testprojekt mit fachlich getrennten
  Testordnern; eine Aufteilung in mehrere Projekte bleibt nur bei konkretem
  Bedarf erlaubt.
- [X] Der allgemeine Graphvertrag 1.0 ist der verabschiedete externe
  Vorgänger. Ein separater Viewer-/Layout-Task ist keine Voraussetzung für den
  C#-Adapter; der Adapter nutzt die quellenneutralen Vertragsfelder später.

## Noch zu entscheiden

### 1. Relevanz- und Größenmetriken

- [ ] Genaue Formel und Normalisierung für `importance` festlegen.
- [X] PageRank wird zunächst gegenüber Betweenness bevorzugt und mit `fanIn`,
  `fanOut` und Beziehungshäufigkeit ergänzt.
- [ ] Getrennte Berechnung für Methoden, Typen und Namespaces sowie die
  Aggregation über Summary-Links festlegen.
- [ ] Entscheiden, welche zusätzlichen Detailwerte im ersten vollständigen
  Release geliefert werden: etwa `loc`, Komplexität, direkte/gewichtete Grade,
  `callCount`, Anzahl referenzierender Projekte, Anzahl erreichbarer eigener
  Nodes, Zykluszugehörigkeit und Komponentengröße. Keiner dieser Werte wird
  ohne fachliche Definition zur visuellen Größe.

### 2. Contract-Validierung

- [X] Die einzige Schemaquelle bleibt
  `contracts/graph-universe/schema/graph-universe.schema.json`.
- [X] Die CLI darf eine kleine .NET-JSON-Schema-Validierungsabhängigkeit
  verwenden und prüft jede Ausgabe vor dem Schreiben; Contract-Tests und
  Repository-Checks bleiben zusätzlich bestehen.

### 3. CLI-Details

- [ ] Konkrete Exit-Code-Bereiche für Argument-, Eingabe-, Analyse- und
  Ausgabefehler festlegen.
- [ ] Konkretes Summary-Format festlegen, zum Beispiel getrennte Zähler für
  geladene, analysierte, übersprungene und fehlgeschlagene Projekte,
  Dokumente, Symbole und Beziehungen.
- [ ] Sprache der öffentlichen CLI-Texte festlegen; technische Namen und
  Graph-IDs bleiben englisch.
