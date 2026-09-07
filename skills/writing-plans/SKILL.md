---
name: writing-plans
description: Turn requirements or an approved design into an actionable implementation plan grounded in the current repository. Use when asked to write an implementation plan or when transitioning from design to multi-step implementation. Scale the plan to the work; ordinary one-line fixes do not need a standalone plan.
---

# Writing Plans

Produce a plan that another developer can execute using the repository and the document, without needing the preceding conversation.

## Establish the starting point

Read applicable repository instructions and inspect the relevant code, build tools, and tests. Preserve the user's requirements, approved design, constraints, and earlier decisions. Distinguish verified facts from assumptions. If a design is already approved, proceed without reopening approval.

Resolve routine choices from project conventions. Ask only for missing information that materially changes the outcome; continue independent planning while waiting. Do not invent paths, APIs, installed tools, or successful checks. Identify files to create as new, and confirm existing paths before referencing them.

## Write the plan

Use the user's requested location or the repository convention. Otherwise save substantial plans to `docs/plans/YYYY-MM-DD-<topic>-implementation.md`. For small tasks, a concise in-conversation plan is enough unless a file is requested.

Include the detail needed for the actual work:

- Intended behavior and acceptance criteria, including concrete user-visible examples when helpful.
- Architecture or approach, key constraints, and any unresolved assumptions.
- Ordered tasks with specific existing or proposed file paths, the change to make, dependencies, and an observable completion condition.
- Verification appropriate to the risk: commands, meaningful behaviors, and expected outcomes. State prerequisites when commands cannot run in the current environment.
- Migration, rollback, data preservation, or packaging steps only when relevant.

Prefer cohesive tasks that each deliver a testable result. Separate independent tasks only when it helps execution. Do not automatically create agents, worktrees, branches, commits, deployments, or new dependencies. Avoid full speculative implementations inside the plan; use small examples only to clarify a contract or a tricky invariant.

Make tests verify behavior rather than the wording or structure of the implementation. Account for failures that materially affect the task, such as corrupt persisted user data. Do not add unrelated audits, test suites, or features.

## Review and hand off

Check that every requested behavior has an implementation task and a way to verify it, and that the sequence respects dependencies. Reconcile the plan with available tools and update it when implementation reveals a necessary change.

If the user requested planning only, deliver the plan and unresolved questions without implementing. If implementation is already authorized, continue into the work after writing the plan; do not introduce an extra approval gate. Report actual verification results separately from planned checks. Keep the original objective active when planning is an intermediate step.
