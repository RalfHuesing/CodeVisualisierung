# Entscheidungslog: C#-Adapter

Alle richtungsentscheidenden Fragen für den v1-Task sind entschieden. Diese
Datei bleibt als kompakter Arbeitsvertrag erhalten. Neue Optionen oder
Erweiterungen benötigen einen eigenen Scope-Entscheid und dürfen v1 nicht
still erweitern.

## Scope und Skalierung

- [X] Der Adapter analysiert den vollständigen eigenen Sourcecode der
  geladenen `.slnx`, `.sln` oder `.csproj`-Eingabe.
- [X] Der v1-Zielwert umfasst Solutions bis mindestens 180.000 LOC.
- [X] Vollständig bedeutet alle fachlich relevanten Deklarationen und
  auflösbaren Beziehungen, nicht lokale Variablen oder jedes Syntaxdetail.
- [X] Es gibt keine zufällige Top-Kürzung und kein stilles Pruning. Der
  vollständige Graph bleibt im JSON; der Viewer nutzt Projektionen.
- [X] Externe Abhängigkeiten, Framework-Assemblies und generierte Artefakte
  werden nicht als Nodes oder Links exportiert. Verworfenes wird gezählt.
- [X] Die CLI baut die analysierte Solution nicht und führt ihre Tests nicht
  aus. Fehlendes Restore/SDK wird best effort behandelt.
- [X] Semantic Models werden dokumentweise verarbeitet und danach freigegeben.
  All-Pairs-Analysen sind ausgeschlossen.

## Eingabe, Identität und Determinismus

- [X] Produktname und Syntax: `codegraph-csharp <input> --output <graph.json>`
  ohne zusätzliches `analyze`-Verb.
- [X] Es gibt in v1 nur `<input>`, `--output`, `--help` und `--version`.
- [X] IDs verwenden normalisierte relative `/`-Pfade, kanonische,
  vollqualifizierte Symbolsignaturen und das Linkmuster
  `link:<link-type>:<source-id>:<target-id>`. Absolute Maschinenpfade sind
  verboten.
- [X] Namespaces und Symbole bleiben projektbezogen; Partial Types bilden
  genau einen Typ-Node je Projekt.
- [X] Zeilen und Spalten sind 1-basiert und gehören zu den Detaildaten, nicht
  zur Identität.
- [X] Nodes und Links werden dedupliziert und stabil sortiert. Gleiche
  Quell-/Zielbeziehungen werden je Linktyp aggregiert.
- [X] `meta.createdAt`, Maschinenname, Prozess-ID und aktuelle Uhrzeit werden
  nicht in die Standardausgabe geschrieben.

## Graphumfang und Projektionen

- [X] v1 exportiert Solution, Project, Assembly, Module, Namespace, File,
  Class, Interface, Record, Struct, Enum, Delegate, Method, Constructor,
  Property, Field, Event, Operator, Local function und Type parameter.
- [X] Parameter und lokale Variablen sind keine Nodes; ihre Typbeziehungen
  werden an deklarierenden Membern erfasst.
- [X] v1 emittiert `contains`, `declares`, `calls`, `inherits`, `implements`,
  `overrides`, `constructs`, `reads`, `writes`, `uses-type`, `returns-type`,
  `parameter-type`, `references-assembly`, `project-reference` und `tests`.
- [X] `generated-from` bleibt für einen späteren externen/generierten Scope
  reserviert und wird in v1 nicht emittiert.
- [X] Die v1-Profile sind `overview`, `architecture` und `member-detail`.
  `overview` ist die große Standardansicht; `member-detail` ist keine
  ungefilterte Standardansicht.
- [X] Summary-Links werden aus Detailbeziehungen aggregiert, bleiben über
  Linkmetriken und eine dokumentierte Aggregationsregel nachvollziehbar und
  verändern die Rohdaten nicht.

## Metriken

- [X] `loc`, `fanIn`, `fanOut`, `weightedFanIn`, `weightedFanOut`, `pageRank`
  und `importance` werden geliefert, soweit die Node-Ebene dafür geeignet ist.
- [X] Methoden und lokale Funktionen liefern zusätzlich
  `cyclomaticComplexity`.
- [X] Container liefern zusätzlich `fileCount`, `typeCount` und `memberCount`,
  soweit die enthaltene Ebene definiert ist.
- [X] Links liefern `occurrences` und `relationshipWeight`, wenn Beziehungen
  aggregiert werden.
- [X] `importance` wird getrennt für Member, Typen und Namespaces berechnet:
  `0.7 * normalizedPageRank + 0.3 * normalizedWeightedFanIn`.
- [X] Beziehungsgewichte: `calls`/`constructs` = 3,
  `inherits`/`implements`/`overrides` = 2,
  `reads`/`writes`/`uses-type`/`returns-type`/`parameter-type` = 1.
- [X] `contains`, `declares`, `tests`, Summary-Links, Projekt- und
  Assemblyreferenzen beeinflussen `importance` nicht.
- [X] Die Normalisierung nutzt `log1p(weightedFanIn)` und je Ebene das 5./95.
  Perzentil mit Klemmung auf `[0, 1]`; ohne Streuung gilt `0.5`. PageRank
  startet gleichverteilt, verteilt Dangling-Masse gleichverteilt und prüft die
  maximale absolute Wertänderung.
- [X] PageRank nutzt Dämpfung `0.85`, maximal 50 Iterationen und
  Abbruchgrenze `1e-8`.
- [X] Betweenness, erreichbare Node-Anzahl, Komponentengröße,
  Zykluskennzeichnung und Testabdeckung sind nicht v1.

## CLI und Fehlervertrag

- [X] Vollständiger valider Lauf: Exit-Code `0`.
- [X] Valider partieller Lauf mit geschriebener Ausgabe: Exit-Code `1`.
- [X] Argumentfehler: `2`; nicht lesbare/nicht auswertbare Eingabe: `3`;
  fataler Analyse- oder Vertragsfehler: `4`; Ausgabe-/Dateisystemfehler: `5`.
- [X] `stdout` enthält stabile `key=value`-Summary-Zeilen; `stderr` enthält
  deutschsprachige Diagnosen. Graphdaten stehen ausschließlich in der Datei.
- [X] Die Summary enthält mindestens Status, Inputtyp, Projekt-/Dokument-
  zähler, Node-/Linkzahlen, verworfene externe/ungelöste Beziehungen,
  Diagnosezahlen und Ausgabegröße.
- [X] Ausgabe erfolgt erst nach Analyse und Schema-Validierung über eine
  temporäre Datei im Zielordner atomar. Ein vorhandenes Ziel bleibt bei Fehlern
  unverändert.

## Vertrags- und Qualitätsgrenze

- [X] Die einzige Schemaquelle bleibt
  `contracts/graph-universe/schema/graph-universe.schema.json`.
- [X] Die CLI validiert jede Ausgabe selbst; Contract-, Viewer-, xUnit- und
  Repository-Checks bleiben zusätzlich verpflichtend.
- [X] Der Adapter darf keine Viewer-, Webserver-, Git-, Watch- oder Live-
  Infrastruktur einführen.
