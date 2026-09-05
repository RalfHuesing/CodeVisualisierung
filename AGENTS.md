# AGENTS.md

Diese Datei gilt für das gesamte Repository. Sie ist die kurze Einstiegsebene für alle Coding-Agenten. Detaillierte Regeln liegen unter `.agents/rules/`.

## Arbeitsprinzip

Der Nutzer bevorzugt einfachen, expliziten und gut lesbaren Code. Weniger Code ist besser, solange die Lösung dadurch nicht unklar oder untestbar wird.

- Erst die bestehende Struktur und die betroffenen Dateien lesen.
- Die kleinste Lösung wählen, die das Problem vollständig löst.
- Keine Abstraktion, kein Framework und keine zusätzliche Abhängigkeit ohne konkreten Mehrwert.
- Datenfluss und Seiteneffekte explizit halten.
- Reine Logik von DOM-, WebGL- und Datei-I/O trennen, wenn das die Tests vereinfacht.
- Keine C#- oder Backend-Infrastruktur in den Browser-Viewer ziehen.

## Verbindliche Regeln

- [Grundsätze](.agents/rules/00-principles.md)
- [Architektur und Verzeichnisstruktur](.agents/rules/10-architecture.md)
- [Tests und Größenlimits](.agents/rules/20-testing-and-quality.md)
- [HTML, CSS und Browser-Code](.agents/rules/30-ui-structure.md)
- [Arbeitsablauf und Git](.agents/rules/40-workflow.md)

## Pflicht vor Abschluss einer Änderung

1. Betroffene Tests ausführen.
2. `npm run check` ausführen, sobald die JavaScript-/Frontend-Infrastruktur installiert ist.
3. Bei Änderungen am Graphvertrag Schema und Fixtures prüfen.
4. Keine Build-, Coverage-, Cache- oder lokalen Konfigurationsdateien committen.
5. Im Abschluss kurz nennen, was geändert und wie es geprüft wurde.

Benutzeranweisungen haben Vorrang. Bei unklarer Reichweite zuerst die bereits autorisierte, reversible Arbeit erledigen und nur bei einer echten Richtungsentscheidung nachfragen.
