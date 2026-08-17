---
inclusion: always
---

# Knowledge Base Index (read this before answering codebase questions)

This workspace is an **umbrella of multiple related repos** analyzed as one system. It has a curated knowledge base. Use it — do not answer structural/behavioral questions from raw guesses.

## The knowledge vault

The curated, human-readable knowledge lives in an **external Obsidian-compatible vault**:
`~/Documents/Engineering Knowledge Base` (Windows: `C:\Users\<user>\Documents\Engineering Knowledge Base`). This is THE canonical location — every other file in this kit refers to it as "the knowledge vault". The vault is shared with the personal agent suite, whose `investigations/` subfolder (ADO/PR/SNOW investigation notes) sits beside this kit's folders — read it when useful, never write there.

Citation convention across the vault/workspace boundary: links **between vault docs** are vault-relative markdown links (`architecture/<module>.md` — clickable in Obsidian); references **from vault docs to workspace files** (api-surface, source code) are plain-text workspace-relative paths (`docs/api-surface/projects/<Project>.md`, understood relative to the umbrella root — deliberately not links).

## Where knowledge lives

- `<vault>/architecture/system-overview.md` — the map: repos, modules, how they connect. **Start here for any "how/where/why" question.**
- `<vault>/architecture/<module>.md` — one doc per module: purpose, key types, entry points, callers, external touchpoints, gotchas.
- `<vault>/architecture/contracts/<repo>.md` — what each repo exposes publicly and who consumes it.
- `docs/api-surface/` — **stays in this workspace** (regenerated compiler output, never hand-edited). **GENERATED, compiler-verified** public API of every project (Roslyn). `index.md` has the project table + dependency graph. Treat as ground truth for what is public and what references what.
- `<vault>/knowledge/` — **resolved investigations** ("we answered this before"): question, short answer, evidence paths, stamped with the repo commit they were verified against. Search here before re-deriving an answer from scratch.

## Rules of engagement

1. For behavior/structure questions: consult `system-overview.md` → the relevant module doc → then code.
2. For symbol-level work (find a type, trace callers, impact of a change): **use Serena MCP tools** (`find_symbol`, `find_referencing_symbols`, `get_symbols_overview`) instead of text search. Grep only as a last resort.
3. For "is this exposed / who depends on this": check `docs/api-surface/` and the vault's `architecture/contracts/` — public members may have external consumers outside this workspace.
4. **Cite file paths** for every non-trivial claim about the code.
5. If code contradicts a doc in the vault's `architecture/`: trust the code, say so explicitly, and propose the doc correction.
6. If information is missing, say "not documented / not found" — do not fabricate.
7. For ADO work items: fetch via the Azure DevOps MCP server, then follow rules 1–4 to analyze impact.
8. Before starting an investigation, search the vault's `knowledge/` folder for a prior finding on the topic — use your file/grep tools, NOT Serena (Serena only indexes this workspace and cannot see the external vault). If one exists, check its front-matter stamp: if its `watch-paths` changed since its `source-commit` (or status is not `verified`), re-verify the trace with Serena before relying on it, then update the note. Say explicitly when an answer reuses a prior note.
9. When sources disagree, trust in this order: live code (via Serena) > `docs/api-surface/` (regenerate if old) > the vault's `architecture/` > the vault's `knowledge/` notes.
