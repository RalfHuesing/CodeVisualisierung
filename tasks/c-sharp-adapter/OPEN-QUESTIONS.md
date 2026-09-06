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
- [X] Roslyn-/Compilerprobleme: auf der CLI-Konsole melden, nicht in den
  Graph schreiben, Analyse best effort fortsetzen und am Ende ein valides,
  gegebenenfalls partielles JSON mit Summary und Zählungen erzeugen.
- [X] Fehlendes SDK/Restore wird best effort behandelt: Alles Verwertbare wird
  analysiert, Probleme werden gemeldet und eine gültige, gegebenenfalls
  partielle JSON-Ausgabe wird trotzdem versucht.
- [X] Analysegrenze: Nur Symbole aus den eigenen, explizit geladenen
  Quellprojekten. Externe Abhängigkeiten, `System.*`, Framework-Assemblies und
  generierte Artefakte werden nicht exportiert.
- [X] Vollständigkeit: Das erste fertige Release deckt die vollständige
  fachliche Zielmenge des eigenen Sourcecodes aus
  `docs/07-CSharp-Referenzgraph.md` ab; externe/generierte Artefakte bleiben
  gemäß Scope-Policy ausgeschlossen. Ein kleiner MVP ist nur ein
  Zwischen-Slice.
- [X] Darstellung: Kurze Labels für Nodes; qualifizierter Name, Signatur,
  Projekt und solution-relative Quellposition mit Zeile/Spalte als sinnvolle
  Detaildaten zum Wiederfinden im Code. Keine absoluten Pfade in IDs oder
  sichtbaren Labels.
- [X] Metrikrichtung: LOC und Komplexität sind Detailwerte, keine primäre
  Größenmetrik. `fanIn`, `fanOut`, `callCount`/gewichtete Grade und PageRank
  bilden zunächst die Grundlage für `importance`; Betweenness bleibt optional
  und nachgelagert.
- [X] Schema-Kopplung: Die CLI validiert jede erzeugte Ausgabe selbst gegen
  `contracts/graph-universe/schema/graph-universe.schema.json`.
- [X] Teststruktur: zunächst ein Testprojekt mit fachlich getrennten
  Testordnern; eine Aufteilung in mehrere Projekte bleibt nur bei konkretem
  Bedarf erlaubt.

## Noch zu entscheiden

### 1. Workspace und Restore

- [X] Die CLI baut die analysierte Solution nicht und führt ihre Tests nicht
  aus.
- [X] Fehlendes Restore/SDK führt zu best effort: verwertbare Projekte und
  Dokumente werden verarbeitet, Fehler werden gezählt und ein partieller
  gültiger Graph wird ausgegeben, sofern die CLI die Ausgabe noch schreiben
  kann.

### 2. Viewer-Abhängigkeit außerhalb dieses Tasks

- [ ] Es gibt derzeit keinen separaten `tasks/viewer-layout`-Task. Er muss
  außerhalb dieses C#-Tasks angelegt werden und Größen-/Abstandsdaten im
  allgemeinen Graphvertrag definieren. Der C#-Task setzt diese Entscheidung
  später nur um.

### 3. Relevanz- und Größenmetriken

- [ ] Zu entscheiden: genaue Formel und Normalisierung für `importance`.
- [X] PageRank wird zunächst gegenüber Betweenness bevorzugt und mit `fanIn`,
  `fanOut` und Beziehungshäufigkeit ergänzt.
- [ ] Zu entscheiden: getrennte Berechnung für Methoden, Typen und Namespaces
  sowie die Aggregation über Summary-Links.
- [ ] Kandidaten, die der Adapter zusätzlich als benannte Detailwerte liefern
  kann: `loc`, Komplexität, direkte/gewichtete Grade, `callCount`, Anzahl
  referenzierender Projekte, Anzahl erreichbarer eigener Nodes,
  Zykluszugehörigkeit und Komponentengröße. Keiner dieser Werte wird ohne
  fachliche Definition zur visuellen Größe.

### 4. Contract-Validierung

- [X] Die einzige Schemaquelle bleibt
  `contracts/graph-universe/schema/graph-universe.schema.json`.
- [X] Die CLI darf eine kleine .NET-JSON-Schema-Validierungsabhängigkeit
  verwenden und prüft jede Ausgabe vor dem Schreiben; Contract-Tests und
  Repository-Checks bleiben zusätzlich bestehen.

### 5. CLI-Details

- [ ] Konkrete Exit-Code-Bereiche für Argument-, Eingabe-, Analyse- und
  Ausgabefehler festlegen.
- [ ] Konkretes Summary-Format festlegen, zum Beispiel getrennte Zähler für
  geladene, analysierte, übersprungene und fehlgeschlagene Projekte,
  Dokumente, Symbole und Beziehungen.
- [ ] Sprache der öffentlichen CLI-Texte festlegen; technische Namen und
  Graph-IDs bleiben englisch.
