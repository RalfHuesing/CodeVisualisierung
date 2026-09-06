# C#-Codequalität und technische Schulden

Diese Regel gilt für C#-Code, insbesondere unter `adapters/csharp`. AiNetLinter-
Befunde zu `Dry`, `MagicValues` und `Deadcode` sind fachlich zu bewerten und
bei passender Gelegenheit proaktiv zu bereinigen. Die MCP-Werkzeugwahl und
Aufrufregeln stehen ausschließlich in `AiNetLinter-McpWorkflow.mdc`.

## Entscheidungsregeln

- `Dry`: Nur gleiche fachliche Bedeutung und gleiche Änderungsverantwortung
  zusammenführen. Zufällige Ähnlichkeit bleibt getrennt; keine Abstraktion nur
  zum Entfernen eines Befunds.
- `MagicValues`: Einen Wert benennen oder zentral konfigurieren, wenn dadurch
  seine fachliche Bedeutung, ein Protokollwert, ein Grenzwert oder eine echte
  Konfiguration sichtbar wird. Keine bedeutungslosen Konstanten und keine
  unnötigen Konfigurationsschichten erzeugen.
- `Deadcode`: Vor dem Entfernen semantische Referenzen, DI, Reflection,
  Serialisierung, Source Generators, öffentliche API und Test-/Build-Einstiege
  prüfen. Bestätigt unerreichbaren Code entfernen; bewusst frameworkseitig
  verwendeten Code erhalten und die Ausnahme eng begründen.

## Arbeitsweise

- Befunde im bearbeiteten oder neu geschriebenen Bereich werden bewertet;
  bestehende, außerhalb des Scopes liegende Befunde werden nicht ungefragt
  vollständig saniert.
- Eine Änderung darf keinen neuen Befund erzeugen. Unterdrückungen sind nur
  eng begrenzt, mit konkreter Begründung und ohne pauschale Regelabschaltung
  zulässig.
- Betrifft ein Finding beobachtbares oder fehlerhaftes Verhalten, zuerst einen
  fokussierten Rot-Test als dauerhaften Regressionstest ergänzen, dann den
  Befund beheben. Bei rein strukturellen Befunden keinen künstlichen Test
  erzeugen; vorhandene Tests sowie die semantische Linter-Prüfung müssen die
  Verhaltensgleichheit absichern.
- Nach der Änderung die betroffenen Befunde erneut prüfen und relevante Tests
  ausführen. Verbleibende Befunde werden im Abschluss mit ihrer fachlichen
  Begründung genannt.
