# Entscheidungslog: Graph-Pakete für große lokale Projekte

Die richtungsentscheidenden Fragen dieses Konzepts sind geschlossen.
Routineentscheidungen wie die konkrete ZIP-Bibliothek trifft der Orchestrator
innerhalb des freigegebenen Vertrags.

## 1. Erzeugt die CLI das Paket direkt?

Empfehlung: Ja. Die CLI soll bei einer Ausgabe wie

```text
--output platform.graphpack
```

direkt ein fertiges Einzelpaket schreiben. Ein separater lokaler Splitter wäre
für Benutzer schwerer verständlich und würde eine zusätzliche Übergabestelle
schaffen.

Entscheidung: geschlossen. Die CLI erzeugt `.graphpack` direkt.

## 2. Wie werden Teilgraphen geschnitten?

Empfehlung für die erste Version:

- ein kleiner `overview`-Graph,
- Architektur-Teilgraphen mindestens pro Projekt,
- Detail-Teilgraphen pro Projekt oder Namespace,
- keine Datei pro Methode oder Member.

Damit bleibt die Anzahl der Archiveinträge überschaubar, während die
Detailstufe lokal genug nachgeladen werden kann.

Entscheidung: geschlossen. Die v1 verwendet einen Overview-Graphen,
Architektur-Teilgraphen pro Projekt und Detail-Teilgraphen pro Projekt oder
Namespace. Es gibt keine Datei pro Methode oder Member.

## 3. Welche Beziehungen gehören in einen Teilgraphen?

Optionen:

- Teilgraph enthält nur Links, deren beide Endpunkte im Teilgraphen liegen.
- Teilgraph enthält zusätzlich Boundary-Stubs für externe Endpunkte.
- Teilgraph enthält aggregierte Summary-Links an der Grenze.

Empfehlung: zunächst vollständige Links innerhalb der Teilgraphgrenze plus
explizit dokumentierte Boundary-/Summary-Information. Verwaiste Links sollen
nicht stillschweigend als vollständige Beziehung erscheinen.

Entscheidung: geschlossen. Teilgraphen enthalten nur Links mit zwei enthaltenen
Endpunkten. Grenzbeziehungen werden als explizite Boundary-/Summary-
Information beschrieben und nicht als dangling Links ausgegeben.

## 4. Bleibt der direkte JSON-Upload erhalten?

Empfehlung: Ja, für kleine und mittlere Graphen bleibt `.json` kompatibel. Für
große monolithische JSON-Dateien zeigt der Viewer eine verständliche Grenze
und verweist auf den `.graphpack`-Export.

Entscheidung: geschlossen. Kleine und mittlere `.json`-Dateien bleiben direkt
ladbar. Große monolithische JSON-Dateien werden nicht im Browser aufgeteilt,
sondern auf den `.graphpack`-Export verwiesen.

## 5. Wird das Paket über Sitzungen hinweg gespeichert?

Empfehlung: Nein in der ersten Version. Zunächst bleiben das ausgewählte
File-Objekt und geladene Teilgraphen nur während der Sitzung verfügbar. Eine
spätere IndexedDB-/OPFS-Ablage braucht eigene Lösch-, Quoten- und
Berechtigungsregeln.

Entscheidung: geschlossen. v1 verwendet nur einen Sitzungscache im Browser.
Eine dauerhafte IndexedDB-/OPFS-Ablage ist ein späterer separater Auftrag.

## 6. Ist die erste Ausbaustufe nur lokaler Upload?

Empfehlung: Ja. Der Task soll zunächst den vom Benutzer lokal erzeugten
`.graphpack`-Upload abdecken. Das Laden eines öffentlich bereitgestellten
Pakets per URL und HTTP-Range-Requests bleibt ein späterer, separater Pfad.

Entscheidung: geschlossen. v1 unterstützt den lokalen Datei-Upload. Das Laden
von Paketen per URL und HTTP-Range-Requests ist nicht Bestandteil dieses Tasks.

## Ergebnis

Slice 0 ist fachlich entschieden. Die konkrete ZIP-Bibliothek, interne
Dateinamen und technische Hilfsfunktionen bleiben Implementierungsdetails.
