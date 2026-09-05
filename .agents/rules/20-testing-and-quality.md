# Tests und Größenlimits

## Grundregel

Jede nichttriviale JavaScript-Funktion muss isoliert testbar sein. DOM-, Datei- und WebGL-Code wird an dünnen Grenzen gehalten; Berechnung, Normalisierung, Filterung und Mapping werden als reine Funktionen geschrieben, wo es sinnvoll ist.

Tests müssen Verhalten prüfen. Ein Test, der nur die Implementierungszeilen wiederholt, ist kein ausreichender Test.

## Deterministische Checks

Vor dem Abschluss:

```text
npm run check
```

Der Check umfasst:

- `npm run check:code-size`
- den Strukturtest für die maximale Dateianzahl pro Codeverzeichnis
- ESLint für JavaScript
- Stylelint für CSS
- HTML-Validate für HTML
- Vitest für Unit-Tests

## Größenlimits

Diese Werte sind obere Sicherheitsgrenzen, keine Zielwerte:

| Artefakt | Harte Grenze |
|---|---:|
| JavaScript-/TypeScript-Datei | 500 physische Zeilen |
| CSS-Datei | 400 physische Zeilen |
| HTML-Datei | 300 physische Zeilen |
| JavaScript-/TypeScript-Funktion | 80 Codezeilen |

Die physischen Dateigrenzen werden mit `scripts/check-code-size.mjs` geprüft. Die Funktionsgrenze wird durch ESLint geprüft. Leerzeilen und Kommentare dürfen nicht verwendet werden, um eine zu große Implementierung zu verstecken.

Wird eine Grenze erreicht, wird nach Verantwortung aufgeteilt. Die Lösung ist nicht, Code künstlich zu komprimieren oder in schwer lesbare Hilfsdateien zu verschieben.

Die Werte stehen ausschließlich in `scripts/quality-config.mjs`. Tests und Prüfskripte verwenden diese gemeinsame Konfiguration.

## Testumfang

- Schema- und Normalisierungslogik erhält Unit-Tests mit gültigen und ungültigen Fixtures.
- Metrikskalierung, Nachbarschaft und Filterung erhalten deterministische Tests.
- Upload, Fehleranzeige, Auswahl und Reset erhalten Browser-Smoke-Tests, sobald der Viewer existiert.
- HTML und CSS werden bei jeder Änderung statisch validiert.
- Ein Architekturtest wird rot, wenn ein Codeverzeichnis mehr Dateien enthält als die zentrale Grenze erlaubt.
- Coverage wird beobachtet, aber nicht durch sinnlose Prozentziele optimiert.

## Testdaten

- Fixtures sind klein, lesbar und deterministisch.
- Zufall benötigt einen expliziten Seed oder wird im Test ersetzt.
- Zeit, Netzwerk, WebGL und Dateisystem werden an Testgrenzen kontrolliert.
- Keine privaten Kunden-, Firmen- oder Quellcodedaten in Fixtures.
