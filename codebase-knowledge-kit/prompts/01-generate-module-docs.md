# Prompt 01 — Generate a Module Architecture Doc

Paste everything below the line into Kiro. Replace the two placeholders. Run once per module. **Review the output like a code review before committing.**

---

You are documenting one module of this codebase for a knowledge base that both humans and AI agents will rely on. Accuracy beats completeness.

TARGET MODULE: <MODULE NAME, e.g. "Billing core logic">
PRIMARY PROJECT(S)/FOLDER(S): <e.g. src/Billing/Billing.Core, and its csproj name>

TASK
Produce `<module-slug>.md` in the knowledge vault's `architecture/` folder (`~/Documents/Engineering Knowledge Base/architecture/`) following the template at `docs/templates/module-architecture-template.md` exactly (keep all its section headings).

METHOD — follow strictly:
1. Read `docs/api-surface/index.md` and `docs/api-surface/projects/<Project>.md` for the target project(s). This is compiler-verified ground truth for public types, members, and project dependencies.
2. Use Serena tools (`get_symbols_overview`, `find_symbol`, `find_referencing_symbols`) to identify entry points and to find which other projects call into this module. Do not rely on text search for this.
3. Skim only the source files needed to describe the 1–3 core flows; do not attempt to read everything.
4. Fill the template. Every factual claim about code must cite a file path (and member name where relevant).

RULES
- If something cannot be determined, write `UNKNOWN — needs human input` in the Open Questions section. Never guess.
- Maximum ~2 pages. Prefer tables and short bullets over prose.
- Mermaid diagrams: max 2, max ~12 nodes each.
- Public members that appear in the api-surface dump but that you cannot find internal callers for: flag them as "possibly consumed externally — verify in repo contract".
- Start the file with YAML front matter for the freshness checker: `generated:` (today), `repo:` (the repo folder name), `source-commit:` (run `git -C <repo> rev-parse --short HEAD`), `watch-paths:` (comma-separated folders this doc describes).
- Write the file to the vault's `architecture/` folder, then output a 5-line summary of what you're least confident about.
