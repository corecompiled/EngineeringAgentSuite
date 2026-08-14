---
name: skill-creator
description: Use when creating, updating, retrofitting, or revalidating a codebase skill (SKILL.md) — enforces this suite's authoring method (routing descriptions, pointer-first content, verified-as-of stamps).
---

# Skill Creator

The method for authoring codebase skills in this suite. A skill is **procedural knowledge that
routes an agent into a functionality area fast** — not documentation. If a quick code search
answers it, it doesn't belong in a skill.

## When to create a skill

- One skill = ONE functionality/domain (booking creation, payment auth, schedule import, …).
- Create one when a domain has non-obvious structure: scattered entry points, tricky flows,
  conventions, or gotchas that repeatedly cost time.
- Do NOT create skills that restate what code trivially shows, or that overlap an existing
  skill — extend the existing one instead.

## Step 1 — Interview

Ask the requester (max 5 questions, skip what you already know):
1. Functionality name and one-sentence purpose.
2. Focus areas / code areas that MUST be covered (paths, projects, classes).
3. Known gotchas or "wish I'd known" facts.
4. Related ADO/SNOW items or knowledge-base notes.
5. What tasks should this skill make faster (analyzing items? answering questions? reviews?).

## Step 2 — Explore before writing

Walk the named code areas in the live codebase. Every pointer you write must be one you
verified exists. Note the actual flow between components — don't trust the requester's memory
over the code.

## Step 3 — Write the SKILL.md

**Description rule (the router).** The frontmatter `description` decides when agents load the
skill. It must state WHEN to use it and name the domain nouns a task would mention:
- Bad: `Overview of the booking engine.`
- Good: `Use when analyzing items, reviewing PRs, or answering questions about booking
  creation, modify/cancel flows, or PNR state transitions; covers entry points, flow, and
  gotchas.`

**Pointer-first policy.** Content is paths, class/component names, flow, conventions, gotchas,
and trace recipes. Code snippets only when a pattern cannot be named — a few lines max, each
individually stamped `(as of <date>)`. Pointers survive refactors; snippets rot silently.

**Size rule.** Keep SKILL.md under ~200 lines. Deep material (long flows, edge-case catalogs,
data models) goes into `references/*.md` in the same skill folder — loaded only when needed.

**Template:**

```markdown
---
name: <kebab-case-name>
description: Use when <tasks> about <domain nouns>; covers <what it gives the agent>.
---

# <Functionality Name>

## Overview
<2-4 sentences: what this functionality does and where it sits in the system.>

## Key Code Areas
| Area | Path | Role |
|---|---|---|
| <entry point> | <path or project/class> | <why you'd start here> |

## How It Works
<The flow between the components above, numbered. Name classes/methods; don't paste them.>

## Conventions & Gotchas
- <non-obvious rule, trap, or historical quirk — the expensive-to-rediscover stuff>

## Common Tasks
### <Trace/diagnose/extend X>
<Short recipe: where to start, what to search for, what to check.>

## Related Knowledge
- <KB notes (by filename), ADO/SNOW items, other skills>

---
Verified as of: <YYYY-MM-DD> against <branch or commit>
```

## Step 4 — Quality checklist (before finishing)

- [ ] Description says WHEN, with searchable domain nouns.
- [ ] Every path/class pointer verified against the live codebase.
- [ ] No snippet longer than a few lines; each stamped.
- [ ] Under ~200 lines; overflow moved to `references/`.
- [ ] No overlap with an existing skill (check `.kiro/skills/` and `~/.kiro/skills/`).
- [ ] `Verified as of` stamp present.

## Retrofitting an existing (generated) skill

1. Rewrite the description to the "Use when…" form.
2. Convert code snippets to pointers (path + class/method name); keep only irreplaceable
   snippets, stamped.
3. Restructure into the template sections; move overflow to `references/`.
4. Verify every pointer against the live code; fix or delete dead ones.
5. Add the `Verified as of` stamp; dedupe against sibling skills.

## Revalidating a skill ("revalidate skill X")

1. Resolve EVERY pointer (paths, classes, methods) against the current codebase.
2. Re-check the How It Works flow against the code — not just existence, but order and
   responsibilities.
3. Fix mismatches (or flag them if the fix needs user input); prune dead pointers.
4. Update the `Verified as of` stamp only after the walk is complete.
5. Report: what still held, what changed, what was fixed.
