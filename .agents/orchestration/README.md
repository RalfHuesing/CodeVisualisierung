# Orchestrator-Arbeitsweise

Der Orchestrator wird im Chat ausdrücklich aktiviert, zum Beispiel:

> Setze den nächsten offenen Roadmap-Punkt als Orchestrator um.

oder:

> Setze diesen Task als Orchestrator um und verwende einen Implementierer und einen Reviewer.

Der Orchestrator liest dann `.agents/skills/project-orchestrator/SKILL.md`,
den Taskvertrag und die Rollen. Standardmäßig arbeitet er den vollständigen
freigegebenen Task ab. Der dauerhafte Projektzustand bleibt in den
Fachdokumenten, der Roadmap und Git. Temporäre Agentenberichte werden nicht
als zusätzliche Wahrheit neben diesen Dateien gepflegt.

`docs/05-Roadmap.md` ist dabei nur der aktuelle Index beschlossener, offener
Vorhaben. Der Orchestrator setzt einen gestarteten Task auf `active`, hält echte
Blockierungen als `blocked` fest und entfernt den Eintrag nach Abschluss. Er
markiert keine erledigten Punkte als dauerhafte Historie.

## Feste Grenzen

- ein fachlicher Slice pro Delegations-/Review-/Commit-Zyklus
- mehrere solcher Zyklen pro vollständigem Task-Lauf
- höchstens drei aktive Subagenten
- keine verschachtelte Delegation
- Reviewer read-only
- höchstens zwei Korrektur-/Reviewzyklen
- nur der Orchestrator ändert die Roadmap und committet

Die Begrenzungen verhindern, dass ein kleiner Fehler eine selbstverstärkende
Agentenschleife erzeugt oder mehrere Agenten denselben Arbeitsstand verändern.
Sie begrenzen die einzelnen Slices, verhindern aber nicht die automatische
Fortsetzung des Tasks nach einem erfolgreichen Checkpoint.
