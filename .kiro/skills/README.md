# Skills

Drop company/team skills here. Kiro Agent Skills format:

- One folder per skill: `.kiro/skills/<skill-name>/SKILL.md`.
- `SKILL.md` requires YAML frontmatter with `name` and `description`. The description decides
  when agents load the skill body (progressive disclosure: only frontmatter is loaded at
  startup; the body loads when relevant) — make it say WHEN to use the skill, not just what it
  is.
- Optional subfolders per skill: `scripts/`, `references/`, `assets/`.

All main agents in this workspace opt into skills via
`"resources": ["skill://.kiro/skills/**/SKILL.md"]` in their JSON — anything you drop here is
picked up automatically on the next session.

`ado-assessment/` is a working example of the format.
