# Backend backlog

Parking lot for ideas we know we want but aren't building yet. Each file describes one piece of future work in enough detail that we (or `/implement-ticket`) can pick it up later without re-doing the thinking.

## Lifecycle

```
idea → planned → in-progress → done
                            \→ deferred (parked again)
```

- **idea** — captured, not yet committed to. Default for new entries.
- **planned** — next-up; ready for `/implement-ticket` to pull it.
- **in-progress** — actively being implemented (set by whoever is doing the work).
- **done** — shipped. Keep the file as a record, or delete — your call.
- **deferred** — was considered, decided not now. Include a "what changed our mind" note.

## File template

Every backlog file follows this shape. Copy from any existing file when adding a new one.

```markdown
---
status: idea | planned | in-progress | done | deferred
created: YYYY-MM-DD
last-updated: YYYY-MM-DD
---

# <Title>

## Why
What problem does this solve? Who needs it?

## Approach sketch
2–5 bullets on how we'd implement it. Not a full design.

## Dependencies / blockers
What needs to land before we can start? (e.g., "needs PR #5 detections")

## Ready to start when…
Concrete trigger condition.
("Frontend asks for live feed", "alert volume > X/day", etc.)

## Notes
Free-form — decisions, links, references, prior research.
```

## How to graduate a backlog file

1. Set `status: planned` and update `last-updated`.
2. Run `/implement-ticket "$(cat backend/.claude/backlog/00X-name.md)"` and let the model run the brainstorm → plan → execute loop with the file's content as context.
3. As work progresses, set `status: in-progress`.
4. When the PR merges: `status: done`.

## Numbering

Three-digit prefix, sequential. Don't reuse numbers — when something is `deferred` and replaced by a different approach, give the replacement a new number.
