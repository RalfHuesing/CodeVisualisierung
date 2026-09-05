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
