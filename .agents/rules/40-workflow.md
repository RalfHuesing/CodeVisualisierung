# Arbeitsablauf und Git

## Vor einer Änderung

1. Relevante `AGENTS.md`-Dateien und Regeln lesen.
2. Bestehende Dateien, Tests und Scripts prüfen.
3. Ziel und kleinsten sinnvollen Änderungsschnitt festlegen.
4. Nur benötigte Abhängigkeiten und Verzeichnisse anlegen.

## Während einer Änderung

- Eine Änderung soll eine erkennbare fachliche oder technische Absicht haben.
- Keine unaufgeforderten Großumbauten.
- Bei Abhängigkeiten `package.json` und `package-lock.json` gemeinsam aktualisieren.
- Build- und Testartefakte bleiben lokal und werden ignoriert.
- Dokumentation aktualisieren, wenn Verhalten, Vertrag oder Struktur geändert werden.

## Vor Commit oder Übergabe

- Betroffene Tests ausführen.
- `npm run check` ausführen.
- `git diff` und `git status` prüfen.
- Commit-Nachrichten beschreiben die Änderung knapp und konkret.
- Keine Secrets, `.env`-Dateien, privaten Graphdaten oder lokalen IDE-Zustände committen.

## Automatische Commits

- Jede fachlich abgeschlossene Änderung wird nach erfolgreichen relevanten Checks automatisch committed.
- Ein Commit enthält genau eine zusammengehörige Absicht; unabhängige Änderungen bleiben getrennt.
- Commit-Nachrichten verwenden Conventional Commits: `type(scope): kurze imperative Beschreibung`.
- Erlaubte Typen sind mindestens `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `build`, `ci` und `perf`.
- Keine normalen Commits für kaputte, unvollständige oder nur temporäre Zwischenstände.
- Bereits veröffentlichte Commits werden nicht nachträglich umgeschrieben.
- Pushes erfolgen nur, wenn sie vom Auftrag umfasst oder ausdrücklich angefordert sind.
