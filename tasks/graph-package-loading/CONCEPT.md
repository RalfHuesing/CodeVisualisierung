# Konzept: Graph-Pakete für große lokale Projekte

## Auftrag

Der C#-Adapter und der statische Browser-Viewer werden so erweitert, dass auch
große Solutions mit Graphausgaben im Bereich von mehreren hundert Megabytes
nutzbar bleiben.

Der Benutzer erzeugt lokal aus seiner Solution genau ein Viewer-Paket, zum
Beispiel:

```text
codegraph-csharp path\to\application.slnx --output platform.graphpack
```

Er wählt anschließend diese eine Datei auf der statischen Webseite aus. Der
Viewer liest zunächst nur ein kleines Manifest und den Overview-Graphen. Beim
Navigieren werden weitere Teilgraphen aus demselben lokalen Archiv gelesen.

Die zentrale Intention lautet:

> Das gesamte Projekt soll erkundbar sein, ohne dass der Browser die gesamte
> Rohdatenmenge als einen einzigen Graphen laden und rendern muss.

## Ausgangslage

Die aktuelle CLI schreibt eine einzelne Graph-Universe-JSON-Datei. Bei realen
Solutions entstehen unter anderem:

- etwa 140 MB für rund 10.600 Nodes und 95.000 Links,
- etwa 443 MB für rund 25.300 Nodes und 209.000 Links.

Der aktuelle Viewer liest eine ausgewählte JSON-Datei vollständig, validiert
und normalisiert sie und übergibt die sichtbare Teilmenge anschließend an den
Renderer. Ein Filter- oder View-Profil verhindert daher nicht, dass die große
Ausgangsdatei zunächst vollständig verarbeitet werden muss.

Die vorhandenen Profile liefern bereits eine sinnvolle fachliche Staffelung:

- `overview` für Solution, Projekte, Assemblies, Module und Namespaces,
- `architecture` für Dateien und Typen,
- `member-detail` für Methoden und Member.

Diese Staffelung soll für die Ladegrenze nutzbar werden, nicht nur für die
nachträgliche Renderer-Filterung.

## Ziele

- Der Benutzer wählt weiterhin genau eine Datei aus und muss kein Verzeichnis
  mit vielen Einzeldateien verwalten.
- Die Datei bleibt lokal. Der statische Webserver benötigt keinen Upload-
  Endpoint, keine Datenbank und keinen projektspezifischen Backend-Service.
- Die CLI erzeugt ein Paket mit Manifest und getrennt ladbaren Graphteilen.
- Der Viewer lädt zuerst nur Manifest und Overview.
- Weitere Graphteile werden erst bei einer passenden Benutzeraktion gelesen.
- Bereits geladene Teilgraphen können innerhalb der Sitzung wiederverwendet
  werden.
- Paket-, Analyse- und Diagnosestatus bleiben sichtbar und werden nicht durch
  Lazy Loading verschleiert.
- Die fachliche Graph-Universe-Bedeutung bleibt erhalten; das Paket ist eine
  Ladehülle und ersetzt nicht den bestehenden Graphvertrag.
- Kleine bestehende JSON-Dateien bleiben als direkter Viewer-Eingang nutzbar.

## Explizite Nicht-Ziele

- Keine automatische Aufteilung einer beliebigen 400-MB-JSON-Datei im Browser.
  Die Aufteilung erfolgt vor der Nutzung durch die CLI beziehungsweise einen
  lokalen Export-Schritt.
- Kein Backend auf dem Hetzner-Webspace.
- Kein automatisches Hochladen, Speichern oder Teilen privater Quellgraphen.
- Keine Pflicht für den Benutzer, hunderte Einzeldateien auszuwählen.
- Kein Versprechen, alle Nodes und Links eines Großprojekts gleichzeitig in
  einer Force-Graph-Szene darzustellen.
- Keine neue C#- oder Roslyn-Analysefunktion nur für den Viewer.
- Keine stillschweigende Entfernung von Nodes oder Links. Reduktion erfolgt
  nur über deklarierte Profile, Projektionen oder bewusst definierte
  Teilgraphgrenzen.
- Keine dauerhafte Browserablage in der ersten Ausbaustufe.
- Keine fachliche Neubewertung von `relations.externalDropped`,
  `relations.unresolved` oder Compilerdiagnosen in diesem Task.

## Benutzerablauf

```text
C#-Solution lokal analysieren
        ↓
eine platform.graphpack-Datei erzeugen
        ↓
statische Webseite öffnen
        ↓
eine lokale Datei auswählen
        ↓
Browser liest manifest.json aus dem Archiv
        ↓
Browser liest overview.json
        ↓
Benutzer navigiert in Projekte, Namespaces und Details
        ↓
passende Teilgraphen werden aus demselben Archiv gelesen
```

Die Dateiauswahl erteilt der Webseite nur für die vom Benutzer ausgewählte
Datei einen kontrollierten Lesezugriff. Das File-Objekt wird nicht mit einem
Netzwerk-Upload gleichgesetzt.

## Zielarchitektur

### Paket

Das erste Ziel ist ein ZIP-kompatibles Einzelarchiv mit der Endung
`.graphpack`. Die konkrete Paketstruktur wird im Vertragsslice festgelegt. Als
fachliche Orientierung ist folgende Struktur vorgesehen:

```text
platform.graphpack
├── manifest.json
├── graphs/overview.json
├── graphs/architecture/<group>.json
└── graphs/detail/<group>.json
```

`manifest.json` beschreibt mindestens Paketversion, Graphformat, Analyse-
status, Diagnosen, verfügbare Profile, Teilgraph-IDs, Pfade und Größen. IDs
bleiben global stabil, damit Auswahl, Beziehungen und Detailnavigation über
Teilgraphen hinweg nachvollziehbar bleiben.

Jeder Teilgraph ist ein eigenständiges gültiges Graph-Universe-Dokument. Ein
Teilgraph enthält nur Links, deren beide Endpunkte in diesem Teilgraphen
liegen. Beziehungen über die Grenze werden nicht als dangling Links ausgegeben,
sondern als explizite Boundary-/Summary-Information im Paket beschrieben.
Vorhandene Summary-Link-Semantik wird dafür wiederverwendet, soweit sie die
Beziehung fachlich korrekt beschreibt.

### CLI

Die Analyse bleibt lokal und erzeugt das Paket atomar. Der direkte CLI-Aufruf
mit einer `.graphpack`-Zieldatei erzeugt ein einzelnes fertiges Artefakt. Der
Export darf intern mehrere Dateien schreiben, muss dem Benutzer aber keine
Einzeldateien präsentieren. Der bestehende JSON-Ausgabepfad und die
bestehenden Summary-/Exitcode-Regeln bleiben für `.json` erhalten und werden
nur so weit erweitert, wie es für den neuen Ausgabetyp erforderlich ist.

Ein partieller Lauf bleibt als solcher erkennbar. Ein Paket mit Diagnosen darf
nicht so aussehen, als sei die Analyse vollständig fehlerfrei.

### Viewer

Der Viewer erhält einen Paket-Ladepfad neben dem direkten JSON-Ladepfad:

1. lokale `File`-Auswahl entgegennehmen,
2. Archiv öffnen, ohne die gesamte Datei als einen JSON-String zu lesen,
3. Manifest validieren,
4. Overview laden und anzeigen,
5. Teilgraphen bei Bedarf laden,
6. geladene Teilgraphen innerhalb der Sitzung cachen,
7. Lade-, Paket- und Datenfehler verständlich anzeigen.

Das Lesen und Entpacken großer Einträge darf später in einen Web Worker
verschoben werden. Es ist jedoch kein Ersatz für die fachliche Aufteilung des
Graphen.

## Architekturgrenzen

- `adapters/csharp/` erzeugt das Paket und kennt keine Browser- oder
  Rendererlogik.
- `apps/viewer/` liest das Paket und kennt keine C#-/Roslyn-Semantik.
- `contracts/graph-universe/` bleibt der Vertrag für Graphdaten. Ein
  package-spezifischer Manifestvertrag wird davon getrennt dokumentiert.
- Die statische Hostingumgebung liefert nur HTML, JavaScript, CSS und optional
  öffentlich bereitgestellte Dateien. Für den lokalen Upload ist kein Server-
  Zustand erforderlich.

## Abschlussbild

Der Task ist fachlich abgeschlossen, wenn ein Benutzer den aus einer großen
Solution erzeugten einzelnen `.graphpack`-Upload im statischen Viewer öffnen,
den Overview-Graphen sehen und mindestens eine weitere Detailstufe laden kann,
ohne dass die gesamte Roh-JSON als ein Graph geladen oder gerendert werden
muss. Die relevanten CLI-, Paket-, Viewer- und Browser-Tests bestehen; der
Vertrag, die Nicht-Ziele und die gemessenen Größenlimits sind dokumentiert.
