# Prompt 03 — Generate the System Overview

Run this LAST, after most module docs (prompt 01) and all repo contracts (prompt 02) exist. Paste everything below the line into Kiro. Review carefully — this becomes the entry point for every future question.

---

You are writing the top-level map of this multi-repo system. Its job: let a newcomer (human or AI) locate anything in under a minute.

TASK
Produce `system-overview.md` in the knowledge vault's `architecture/` folder (`~/Documents/Engineering Knowledge Base/architecture/`) following `docs/templates/system-overview-template.md` exactly.

SOURCES — in priority order:
1. The existing module docs in the vault's `architecture/*.md` and contracts in `architecture/contracts/*.md` — synthesize, don't re-derive.
2. `docs/api-surface/index.md` — use its dependency graph to draw the high-level architecture mermaid (collapse projects into modules/repos; keep <= 15 nodes).
3. Steering files `product.md` / `tech.md` / `structure.md` for business context.
4. Code, via Serena, only to resolve conflicts between the above.

RULES
- Every module listed must link to its doc; every repo must link to its contract doc. If a doc is missing, list it under a final section "Docs still to write".
- The "How a typical operation flows" narrative must name real types/methods with file paths (verify with Serena; do not invent).
- If module docs contradict each other, flag the contradiction explicitly rather than silently picking one.
- Max 3 pages.
- Start the file with YAML front matter: `generated:` (today) and `repo: (multi)` — the freshness checker tracks multi-repo docs by age.
- Write the file, then output: (a) the 3 most load-bearing modules by inbound dependencies, (b) any cycles in the dependency graph, (c) your top 3 uncertainty areas.
