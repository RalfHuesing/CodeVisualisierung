# Orchestrator-Taskvertrag

```text
Task:

Task scope:

Explicit exclusions:

Task completion condition:

Roadmap items in dependency order:

Current slice:

Objective:

Context documents:

Allowed paths:

Forbidden or out-of-scope paths:

Acceptance criteria:
-

Required checks:
-

Delegated roles:
-

Dependencies:

Continue condition:

Slice stop condition:

Task-level stop condition:

Slice result:
pass | findings | blocked

Task result:
complete | blocked | in_progress
```

`Current slice` is the current internal checkpoint. After a successful slice
commit, the Orchestrator fills it with the next ready item and continues. The
task is `complete` only when its task-level completion condition is true.
