---
name: project-orchestrator
description: Orchestrate a complete bounded repository task as a sequence of explicit slices with subagents, read-only review, verification, and controlled commits. Use when the user asks to execute a task or roadmap slice as an orchestrator.
---

# Project Orchestrator

Use this repository-local procedure when the user says to execute a task “als
Orchestrator”, asks for subagents, or requests an implementation loop with
reviewers. It is general-purpose; it is not specific to the viewer or C#.

The default unit of execution is the complete user-approved task. A slice is
an internal safety, review, and commit boundary. A task may contain multiple
slices and must continue automatically after each successful slice until its
task-level completion condition is true. Only an explicit user request for a
single slice changes this default.

## Before delegation

1. Read `AGENTS.md`, all applicable `.agents/rules/`, the relevant role files,
   and the task's referenced documents.
2. Check `git status`, branch, recent commits, existing user changes,
   `package.json`, and the relevant tests.
3. Determine the task scope, explicit exclusions, terminal completion
   condition, and dependency order. Do not infer that the first slice is the
   complete task.
4. Select the next ready coherent slice within that task scope.
5. Write a compact task contract with task-level objective and completion
   condition plus slice-level allowed paths, acceptance criteria, checks,
   dependencies, and stop condition.

## Delegation

- Use at most three active subagents for one slice.
- Parallelize only independent read-only analysis or disjoint work.
- Keep writing agents sequential when files or decisions overlap.
- Assign the `implementer` role for scoped changes and the `reviewer` role for
  read-only review. Subagents never delegate further.
- Give every subagent its task contract and require a concise result.

## Controlled loop

Repeat the following sequence until the task-level completion condition is
true or a real blocker is reached:

1. Orchestrator defines the next ready slice.
2. Implementer performs the scoped work.
3. Orchestrator inspects the diff and runs targeted tests.
4. Reviewer returns `pass`, findings, or blocked without editing.
5. Orchestrator fixes findings, at most twice, then repeats review.
6. Orchestrator runs `npm run check`, updates the global roadmap only for a
   documented status event, checks `git diff --check` and `git status`, and
   commits the completed slice.
7. After a successful commit, Orchestrator re-reads the task scope and roadmap,
   resolves the next ready slice, and continues automatically.
8. When no in-scope work remains, Orchestrator performs the final completion
   checks and reports the task as complete.

A successful slice commit is a checkpoint, never by itself a task-level stop
condition. Stop as `blocked` after two failed review cycles, repeated
identical failures, missing authority, an unresolved directional decision, or
an unmet prerequisite that cannot be resolved within the task. If execution
is externally interrupted, do not report success; report the last committed
checkpoint and the remaining task scope so the task can resume safely.

## General tasks

If no roadmap exists, use the user's task as the objective and the repository's
tests and documentation as acceptance context. Continue through all acceptance
criteria and deliverables in the user's task. If a roadmap exists, process
ready open items in dependency order until all items inside the explicit task
scope are complete; do not silently include excluded or later-phase work.

## Global roadmap governance

`docs/05-Roadmap.md` is a short index of explicitly committed open work. It is
not a history log and does not contain completed checkboxes. Normal discussion,
brainstorming, or a concept document alone does not change it.

The main agent records a new item when the user clearly commits to a future
feature or creates a task for it. The Orchestrator owns subsequent status
changes: `planned`, `active`, and `blocked`. When a task is complete, cancelled,
or removed from scope, the Orchestrator removes the item instead of marking it
complete. Task-local ROADMAP files retain slice details and acceptance
criteria; the global roadmap only links to them.

If the user explicitly requests only one slice, apply the same slice procedure
but stop after that slice's successful commit.

The detailed task contract is in `task-template.md`. The repository role
contracts are under `.agents/roles/`, and the invariants are under
`.agents/rules/50-orchestration.md`.
