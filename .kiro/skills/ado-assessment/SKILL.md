---
name: ado-assessment
description: Section-by-section quality bar for writing or reviewing work-item investigation notes (ADO or ServiceNow). Use when producing, revising, or judging an assessment note in the Engineering Knowledge Base.
---

# Writing a high-quality assessment

## Background & Intent
Good: names who raised the item, when, and the business/technical driver; quotes or cites the
discussion turns (author + date) that shaped the current stance; states where the discussion
stands NOW. Bad: paraphrasing the item description back.

## Initial Assessment
Good: states what is actually being asked for (which may differ from the title); bounds the
scope; lists ambiguities and marks each resolved (how) or open. For ServiceNow: an explicit
"Needed from the team" list with owners. Bad: restating requirements without judgment.

## Proposed Solution
Good: one recommended approach with rationale; alternatives only when genuinely viable, as
Option A/B with trade-offs; always ends with "Confidence in proposal: NN% — <one-line
justification>". Bad: a menu of options with no recommendation, or confidence with no reason.

## Dev Notes
Good: concrete file/service/config names likely touched; ordering and dependency constraints;
migration/rollout/feature-flag concerns; known gotchas. Bad: generic advice ("write tests").

## Manual Dev-Testing Scenarios
Good: each row is executable by a developer without asking questions — concrete inputs, steps,
and an observable expected result; includes at least one negative/edge scenario. Bad: "test the
happy path".

## Recommendations & Next Steps
Good: numbered, actionable, each with an implied or named owner; "None" is an acceptable answer.
Bad: vague futures ("consider improving performance").

## Resolution (at close-out)
Good: what was concluded, what was actually done, how it was verified, and the date. A reader a
year from now should trust it without re-opening the item.
