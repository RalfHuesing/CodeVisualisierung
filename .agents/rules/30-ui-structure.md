# HTML, CSS und Browser-Code

## HTML

- Semantische Elemente verwenden: `main`, `header`, `nav`, `section`, `aside`, `button`, `label`.
- Jede interaktive Funktion muss per Tastatur erreichbar sein.
- Keine Inline-Skripte und keine Inline-Styles.
- IDs werden nur für echte Beziehungen oder eindeutige Ziele verwendet.
- Der HTML-Einstieg bleibt eine schlanke Dokumenthülle; dynamische Inhalte entstehen in klar benannten JavaScript-Modulen.

## CSS

- CSS nach Verantwortung strukturieren, zum Beispiel Basis, Layout, Komponenten und Viewer-Szene.
- Eine Datei wird aufgeteilt, bevor sie die Größenprüfung verletzt oder mehrere unabhängige Verantwortungen enthält.
- Keine globalen Selektor-Kaskaden, die unbeteiligte Bereiche beeinflussen.
- Klassen und Custom Properties erhalten sprechende Namen.
- Keine stylespezifischen Ausnahmen als Ersatz für eine klare Struktur.
- Stylelint ist verbindlich.

## JavaScript

- DOM-Zugriffe und Eventverkabelung bleiben nahe am UI-Modul.
- Datenparsing, Validierung, Normalisierung und Metrik-Mapping bleiben ohne DOM-Abhängigkeit.
- Keine versteckten Singletons oder globalen veränderlichen Daten.
- Fehler werden an der UI-Grenze in verständliche Zustände übersetzt.
- Animationen dürfen keine fachliche Bedeutung vortäuschen, die nicht aus den Daten folgt.

## WebGL

- Rendering-Code wird von Datenaufbereitung und Bedienlogik getrennt.
- Der Renderer bekommt bereits validierte und normalisierte Daten.
- Auswahlzustand und Detailansicht bleiben zusätzlich als HTML verfügbar.
- Bewegungen und Effekte müssen abschaltbar oder bei reduzierter Bewegung eingeschränkt sein.
