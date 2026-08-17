---
inclusion: always
---

# Workspace Structure (fill in after cloning repos)

This is an umbrella workspace: multiple repos in one folder, analyzed together via `Everything.sln` (analysis-only, never shipped).

| Folder (repo) | What it is | Depends on |
|---|---|---|
| TODO/main-repo | TODO | TODO |
| TODO/dependent-repo-1 | TODO | TODO |

Other important paths:
- `~/Documents/Engineering Knowledge Base/architecture/` — curated architecture docs (external vault, see 00-knowledge-index.md)
- `~/Documents/Engineering Knowledge Base/knowledge/` — resolved investigation notes, commit-stamped (external vault, see 00-knowledge-index.md)
- `docs/api-surface/` — generated public API dumps, in-workspace (never hand-edit)
- `docs/templates/` — blank doc templates the agent fills in
- `tools/ApiSurfaceDumper/` — regenerates api-surface
- `prompts/` — reusable analysis prompts

Keep the table in sync when repos are added/removed.
