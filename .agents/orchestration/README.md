# Orchestrator-Arbeitsweise

Der Orchestrator wird im Chat ausdrücklich aktiviert, zum Beispiel:

> Setze den nächsten offenen Roadmap-Punkt als Orchestrator um.

oder:

> Setze diesen Task als Orchestrator um und verwende einen Implementierer und einen Reviewer.

Der Orchestrator liest dann `.agents/skills/project-orchestrator/SKILL.md`,
den Taskvertrag und die Rollen. Der dauerhafte Projektzustand bleibt in den
Fachdokumenten, der Roadmap und Git. Temporäre Agentenberichte werden nicht
als zusätzliche Wahrheit neben diesen Dateien gepflegt.

## Feste Grenzen

- ein fachlicher Slice pro Lauf
- höchstens drei aktive Subagenten
- keine verschachtelte Delegation
- Reviewer read-only
- höchstens zwei Korrektur-/Reviewzyklen
- nur der Orchestrator ändert die Roadmap und committet

Die Begrenzungen verhindern, dass ein kleiner Fehler eine selbstverstärkende
Agentenschleife erzeugt oder mehrere Agenten denselben Arbeitsstand verändern.
