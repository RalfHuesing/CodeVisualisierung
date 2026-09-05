---
name: project-orchestrator
description: Orchestrate one bounded repository task with explicit subagents, read-only review, verification, and a controlled commit. Use when the user asks to execute a task or roadmap slice as an orchestrator.
---

# Project Orchestrator

Use this repository-local procedure when the user says to execute a task “als
Orchestrator”, asks for subagents, or requests an implementation loop with
reviewers. It is general-purpose; it is not specific to the viewer or C#.

## Before delegation

1. Read `AGENTS.md`, all applicable `.agents/rules/`, the relevant role files,
   and the task's referenced documents.
2. Check `git status`, branch, recent commits, existing user changes,
   `package.json`, and the relevant tests.
3. Select exactly one coherent slice. Do not start a second slice in the same
   run merely because the first one was small.
4. Write a compact task contract with objective, allowed paths, acceptance
   criteria, checks, dependencies, and stop condition.

## Delegation

- Use at most three active subagents for one slice.
- Parallelize only independent read-only analysis or disjoint work.
- Keep writing agents sequential when files or decisions overlap.
- Assign the `implementer` role for scoped changes and the `reviewer` role for
  read-only review. Subagents never delegate further.
- Give every subagent its task contract and require a concise result.

## Controlled loop

1. Orchestrator defines the slice.
2. Implementer performs the scoped work.
3. Orchestrator inspects the diff and runs targeted tests.
4. Reviewer returns `pass`, findings, or blocked without editing.
5. Orchestrator fixes findings, at most twice, then repeats review.
6. Orchestrator runs `npm run check`, updates only completed roadmap items,
   checks `git diff --check` and `git status`, and commits.

Stop immediately after a successful commit. Stop as `blocked` after two failed
review cycles, repeated identical failures, missing authority, or an unresolved
directional decision. Never keep the loop alive to search for more work.

## General tasks

If no roadmap exists, use the user's task as the objective and the repository's
tests and documentation as acceptance context. If a roadmap exists, choose the
next open item only when its prerequisites are complete; otherwise report the
prerequisite instead of skipping ahead.

The detailed task contract is in `task-template.md`. The repository role
contracts are under `.agents/roles/`, and the invariants are under
`.agents/rules/50-orchestration.md`.
