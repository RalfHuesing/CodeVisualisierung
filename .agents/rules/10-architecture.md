# Architektur und Verzeichnisstruktur

## Bereiche

```text
apps/viewer/                 Statische Browser-Anwendung
contracts/graph-universe/    JSON-Schema und Test-Fixtures
adapters/csharp/             Späterer C#-/Roslyn-Exporter
docs/                        Produkt- und Technikdokumentation
scripts/                     Kleine deterministische Entwicklungschecks
tests/                       Bereichsübergreifende Unit-Tests
```

## Abhängigkeiten

- `apps/viewer` darf vom Graphvertrag abhängen.
- `adapters/csharp` erzeugt Graphdaten und darf nicht von Browser-Code abhängen.
- `contracts/graph-universe` bleibt quellenneutral und enthält keine Three.js- oder C#-Logik.
- Der Viewer darf nicht wissen, ob ein Graph aus C#, Git, einer Datenbank oder einer Handdatei stammt.
- Frontend- und C#-Abhängigkeiten bleiben technisch getrennt.

## Datenvertrag

- Änderungen am Graphformat beginnen mit einer Anpassung des Schemas und einer Fixture.
- Jede neue fachliche Zahl erhält einen Namen; `weight` allein erklärt keine Bedeutung.
- Rohdaten und visuelle Mappings bleiben getrennt.
- Unbekannte optionale Felder dürfen den Viewer nicht zum Absturz bringen.

## Technologieentscheidungen

- Zuerst plain modernes JavaScript, HTML und CSS ohne UI-Framework.
- Vite darf als Entwicklungs- und Buildwerkzeug verwendet werden; der Betrieb bleibt rein statisch.
- Three.js oder ein darauf basierender Graph-Renderer wird hinter einer kleinen Viewer-internen Schnittstelle verwendet.
- Eine zusätzliche Architektur-Schicht ist nur erlaubt, wenn sie konkret Testbarkeit, Austauschbarkeit oder Verständlichkeit verbessert.
