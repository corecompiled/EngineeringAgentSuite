# Skills

Drop company/team skills here (or install globally — see below). Kiro Agent Skills format:

- One folder per skill: `<skill-name>/SKILL.md`.
- `SKILL.md` requires YAML frontmatter with `name` and `description`. Only frontmatter loads at
  startup; the body loads when the description matches the task (progressive disclosure).
- Optional subfolders per skill: `scripts/`, `references/`, `assets/`.

All main agents in this workspace opt into skills from BOTH locations via
`skill://.kiro/skills/**/SKILL.md` and `skill://~/.kiro/skills/**/SKILL.md` — anything dropped
in either place is picked up on the next session. On a name conflict, the workspace copy wins
over the global one.

## Authoring rules (enforced by the `skill-creator` skill)

1. **Description = router.** It must say WHEN to use the skill and name the domain nouns a task
   would mention ("Use when analyzing items or answering questions about booking creation,
   modify flows, or PNR state transitions…"). This is what makes the skill load at the right
   moment — an encyclopedia-style description routes poorly.
2. **Pointer-first.** Paths, class names, flows, conventions, gotchas, trace recipes. Code
   snippets only when a pattern can't be named — a few lines max, stamped `(as of <date>)`.
   Pointers survive refactors; snippets rot.
3. **Lean SKILL.md** (< ~200 lines); deep material in `references/*.md`.
4. **Staleness stamp.** Every codebase skill ends with
   `Verified as of: <YYYY-MM-DD> against <branch/commit>`.
5. **One skill, one domain.** Extend an existing skill rather than creating an overlapping one.

To create, retrofit, or revalidate a skill, just ask any agent — the `skill-creator` skill
carries the full method, template, retrofit checklist, and revalidation workflow. Existing
AI-generated skills you already have: drop them in, then ask an agent to "retrofit skill <name>
using skill-creator".

`ado-assessment/` is a working example of the format.
