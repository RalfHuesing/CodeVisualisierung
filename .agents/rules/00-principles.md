# Grundsätze

## Einfachheit

- Bevorzuge direkte Datenstrukturen und kurze Funktionen.
- Vermeide vorzeitige Generalisierung.
- Eine Funktion soll eine erkennbare Aufgabe haben.
- Eine Datei soll eine erkennbare Verantwortung haben.
- Gemeinsamer Code ist nur dann sinnvoll, wenn mindestens zwei reale Nutzer dieselbe Logik benötigen.
- Keine Wrapper, Factorys, Eventbusse oder Service-Layer als Platzhalter für mögliche Zukunft.

## Explizitheit

- Benenne Daten, Zustände und Transformationen konkret.
- Verwende keine magischen Strings oder Zahlen, wenn eine benannte Konstante die Bedeutung klärt.
- Konfigurierbare Werte stehen an einer benannten, zentralen Stelle und werden nicht mehrfach im Code wiederholt.
- Verlasse dich nicht auf implizite globale Zustände.
- Fehlerfälle werden sichtbar behandelt und verständlich gemeldet.

## Agentenlesbarkeit

- Klare Namen sind wichtiger als maximale Kürze.
- Kleine, lineare Abläufe sind verschachtelten Cleverness-Konstruktionen vorzuziehen.
- Kommentare erklären nur Kontext, Entscheidung oder Randfall; sie wiederholen keinen Code.
- Keine unnötig kompakten Einzeiler, keine Metaprogrammierung und keine versteckten Seiteneffekte.

## Änderungssicherheit

- Änderungen bleiben auf den angeforderten Bereich begrenzt.
- Bestehende Nutzerdateien und Daten werden nicht stillschweigend gelöscht oder überschrieben.
- Neue Bibliotheken müssen einen konkreten Nutzen und eine nachvollziehbare Wartungsbegründung haben.
- Keine Magic Values: fachliche Konstanten werden benannt, technische Grenzwerte zentral konfiguriert.
- DRY gilt für gleiche Bedeutung; ähnliche, aber unabhängige Abläufe müssen nicht künstlich vereinheitlicht werden.
- Dead Code, unreferenzierte Exporte, ungenutzte Styles und auskommentierte Altimplementierungen werden entfernt.

## Regelqualität

- Regeln bleiben kurz, konkret und prüfbar.
- Keine Wiederholung derselben Vorgabe in mehreren Dateien ohne zusätzlichen Geltungsbereich.
- Beispiele nur aufnehmen, wenn sie eine häufige Fehlinterpretation verhindern.
