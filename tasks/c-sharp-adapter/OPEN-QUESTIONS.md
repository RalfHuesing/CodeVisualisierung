# Offene Entscheidungen: C#-Adapter

Diese Fragen sollen vor Slice 1 beantwortet werden. Vorschläge sind als solche
markiert und können im Chat geändert werden. Nach einer Entscheidung wird die
Antwort in `CONCEPT.md` übernommen; diese Datei bleibt als Entscheidungslog
erhalten.

## 1. Name und CLI-Syntax

Vorschlag:

```text
codegraph-csharp <solution.slnx> --output <graph.json>
```

Frage: Ist `codegraph-csharp` als ausführbarer Produktname in Ordnung? Soll es
zusätzlich ein Verb wie `analyze` geben, oder bleibt die CLI bewusst bei einem
einzigen Analysebefehl?

## 2. Eingabeumfang

Vorschlag: `.slnx` ist der primäre und zuerst unterstützte Eingang. `.sln` und
einzelne `.csproj` werden nicht stillschweigend versprochen und kommen erst in
einem separaten Scope, falls sie benötigt werden.

Frage: Soll der erste Adapter ausschließlich `.slnx` akzeptieren oder direkt
auch `.sln` und `.csproj` laden?

## 3. Analysezustand und Build

Vorschlag: Die CLI lädt die Solution mit Roslyn/MSBuild und verwendet die
vorhandenen Projekt-/Restoreinformationen. Sie baut keine Binärdateien und
führt keine Tests der analysierten Solution aus.

Frage: Soll ein fehlendes Restore/SDK als harter Fehler gelten? Sollen
Compilerdiagnosen in einer ansonsten ladbaren Solution die Analyse stoppen oder
als Graph-Metadaten/Diagnosen ausgegeben werden?

## 4. Umfang des vollständigen ersten Releases

Die Referenzdokumentation nennt viele Node- und Linktypen. Die Roadmap plant
deren Umsetzung in Slices.

Frage: Muss das erste fertige Release bereits alle in
`docs/07-CSharp-Referenzgraph.md` genannten Typen und Beziehungen liefern, oder
ist ein klar dokumentierter MVP mit Solution/Project/Namespace/Type/Method,
Containment, Calls und Referenzen der gewünschte erste Abschluss?

## 5. Externe und generierte Symbole

Vorschlag: Externe Assemblies und externe Typen werden als Nodes aufgenommen,
aber standardmäßig nicht rekursiv in ihre Quellen analysiert. Generierte
Dokumente werden kenntlich gemacht; ihre Aufnahme in die Node-Menge ist eine
explizite Option oder Policy.

Frage: Sollen externe Nodes standardmäßig enthalten sein? Sollen generierte
Dateien standardmäßig enthalten, standardmäßig ausgeschlossen oder nur über
eine CLI-Option steuerbar sein?

## 6. Pfade und reproduzierbare Ausgabe

Vorschlag: IDs enthalten keine absoluten Pfade. Detaildaten verwenden
Solution-relative Pfade mit `/` als Trennzeichen. `createdAt` wird in der
Standardausgabe weggelassen, damit identische Eingaben identische JSON-Dateien
erzeugen.

Frage: Ist diese Policy gewünscht, oder soll die Ausgabe absolute Pfade bzw.
immer einen Erstellungszeitpunkt enthalten?

## 7. Metriken

Vorschlag für den ersten vollständigen Scope: Quellcodezeilen,
zyklomatische Komplexität, Fan-in und Fan-out, jeweils nur dort, wo die
Berechnung belastbar ist. Git-Churn und Testabdeckung bleiben außerhalb.

Frage: Welche Metriken sind für den ersten nutzbaren Adapter zwingend? Reicht
dieser Vorschlag oder soll zunächst nur LOC geliefert werden?

## 8. Schema-Kopplung

Vorschlag: `contracts/graph-universe/schema/graph-universe.schema.json` bleibt
die einzige Schemaquelle. Der C#-Contract-Code wird daraus nicht als zweites
manuelles Schema gepflegt; jede CLI-Ausgabe wird zur Laufzeit bzw. im
Integrationspfad gegen diese Datei validiert, und Contract-Tests prüfen die
Parität zwischen Adapter und Viewer.

Frage: Ist eine kleine .NET-Validierungsabhängigkeit für JSON Schema in Ordnung,
oder soll die Validierung ausschließlich über einen Repository-Check außerhalb
der CLI erfolgen?

## 9. Teststruktur

Vorschlag: Ein Testprojekt `CodeVisualisierung.CSharp.Tests` mit klar getrennten
Ordnern für reine Logik, Roslyn-Integration, Contract und CLI. Mehrere
Produktionsprojekte dürfen intern fachlich sauber getrennt sein.

Frage: Ist ein gemeinsames Testprojekt gewünscht, oder sollen die Testgrenzen
als mehrere Testprojekte sichtbar werden?

## 10. Namensgebung und Sprache

Vorschlag: Produkt- und API-Namen englisch (`Graph`, `Node`, `Analysis`),
Dokumentation und CLI-Fehlermeldungen deutsch oder englisch konsistent nach
einer noch zu treffenden Entscheidung.

Frage: Welche Sprache sollen öffentliche CLI-Texte und Fehlermeldungen haben?
