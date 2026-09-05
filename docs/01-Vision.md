# Vision

## Kurzfassung

Das Graph-Universum ist ein quellenneutraler Viewer für vernetzte Daten. Er nimmt einen Graphen aus Nodes und Links entgegen und macht dessen Struktur, Beziehungen und ausgewählte Messwerte räumlich erfahrbar.

Die erste Inszenierung ist ein interaktives 3D-Universum. Die astronomische Metapher darf Orientierung und Atmosphäre geben, darf aber nicht die Datenbedeutung verstecken oder die Bedienbarkeit verschlechtern.

## Problem

In großen vernetzten Systemen liegen wichtige Informationen nicht nur in einzelnen Elementen, sondern in ihren Beziehungen:

- Welche Elemente sind besonders zentral?
- Wo bündeln sich Abhängigkeiten?
- Welche Beziehungen sind stark oder häufig?
- Welche Bereiche sind isoliert, dicht verknüpft oder auffällig?
- Welche Eigenschaften unterscheiden zwei ansonsten ähnliche Nodes?

Eine Tabelle kann einzelne Werte zeigen, aber die Struktur des Gesamtsystems nur schwer vermitteln. Der Viewer soll zuerst Muster und Zusammenhänge sichtbar machen und danach die Details eines ausgewählten Elements erklären.

## Produktprinzipien

1. **Quelle ist austauschbar.** Der Viewer kennt keine C#-Syntax, keine Roslyn-Symbole und keine Git-Kommandos.
2. **Das Graphformat ist stabiler als die Darstellung.** Eine neue Ansicht darf kein neues Datenmodell erzwingen.
3. **Metriken bleiben benannt.** Ein allgemeines `weight` ohne Bedeutung reicht nicht als Erklärung. Zahlen brauchen Namen, Einheiten oder eine dokumentierte Interpretation.
4. **Interaktion vor Dekoration.** Auswahl, Fokus, Navigation und Detailverständnis sind wichtiger als Glow, Partikel und Effekte.
5. **Progressive Komplexität.** Ein kleiner Graph muss sofort verständlich sein; ein großer Graph braucht Aggregation, Filterung und Detailstufen.
6. **Browser-first und lokal.** Die JSON-Datei wird im Browser verarbeitet. Daten müssen für den Viewer nicht auf einen Server hochgeladen werden.
7. **Darstellung bleibt überprüfbar.** Jede visuelle Kodierung muss eine sichtbare Legende oder erklärende Detailansicht haben.

## Was der Viewer nicht ist

- kein C#-Parser,
- kein Git- oder Qualitätsanalysewerkzeug,
- kein Backend,
- keine automatische Wahrheit über Architekturqualität,
- zunächst kein Echtzeit-Agentenmonitor.

Diese Fähigkeiten können später Datenquellen oder Erweiterungen liefern. Sie gehören nicht in den Kern des Viewers.

## Erfolgskriterium für die erste Version

Eine Person kann eine gültige JSON-Datei per Drag-and-drop öffnen, innerhalb weniger Sekunden eine verständliche Graphansicht sehen, einen Node auswählen, dessen Eigenschaften und Nachbarschaft prüfen und die Darstellung ohne technische Kenntnisse wieder zurücksetzen.
