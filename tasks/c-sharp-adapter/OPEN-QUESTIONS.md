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
  Größenmetrik. Relevanz soll aus eigenen Beziehungen, gewichteten Graden und
  einem zu prüfenden Einflusswert wie PageRank entstehen.
- [X] Teststruktur: zunächst ein Testprojekt mit fachlich getrennten
  Testordnern; eine Aufteilung in mehrere Projekte bleibt nur bei konkretem
  Bedarf erlaubt.

## Noch zu entscheiden

### 1. Workspace und Restore

- [X] Die CLI baut die analysierte Solution nicht und führt ihre Tests nicht
  aus.
- [ ] Zu klären: Ist ein fehlendes Restore/SDK ein harter Prozessfehler, oder
  wird ein partieller Graph geschrieben, sofern noch verwertbare Projekte und
  Dokumente geladen werden konnten?

### 2. Generische Layoutbeschreibung

- [ ] Der gemeinsame Graphvertrag braucht eine quellenneutrale deklarative
  Beschreibung für Containment-basierte Nähe. Sie muss mindestens ausdrücken:
  Namespace-Zentrum, Typ-Orbit, Member-Orbit, Abstand innerhalb eines
  Containers und größeren Abstand zwischen Gruppen/Namespaces.
- [ ] Zu entscheiden ist die konkrete Vertragsform, zum Beispiel ein
  `layoutProfiles`-Abschnitt mit `orbitRules` und Gruppendistanzen. Der
  Adapter darf dafür keine C#-spezifischen Viewer-Regeln erfinden.

### 3. Relevanz- und Größenmetriken

- [ ] Zu entscheiden: genaue Formel und Normalisierung für `importance`.
- [ ] Zu entscheiden: PageRank, Betweenness oder eine bewusst einfachere
  Kombination aus `fanIn`, `fanOut` und Beziehungshäufigkeit.
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
- [ ] Zu entscheiden: Darf die CLI eine kleine .NET-JSON-Schema-
  Validierungsabhängigkeit verwenden, damit jede Ausgabe vor dem Schreiben
  geprüft wird, oder bleibt die Laufzeitprüfung in Contract-Tests und einem
  separaten Repository-Check?

### 5. CLI-Details

- [ ] Konkrete Exit-Code-Bereiche für Argument-, Eingabe-, Analyse- und
  Ausgabefehler festlegen.
- [ ] Konkretes Summary-Format festlegen, zum Beispiel getrennte Zähler für
  geladene, analysierte, übersprungene und fehlgeschlagene Projekte,
  Dokumente, Symbole und Beziehungen.
- [ ] Sprache der öffentlichen CLI-Texte festlegen; technische Namen und
  Graph-IDs bleiben englisch.
