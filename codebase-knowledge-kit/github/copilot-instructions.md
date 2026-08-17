<!-- Place this file at: <repo>/.github/copilot-instructions.md
     GitHub Copilot automatically reads it as repository custom instructions.
     Adjust the relative paths below if this repo is NOT the one hosting docs/
     (e.g., point to the umbrella folder). The curated knowledge lives in the
     external vault ~/Documents/Engineering Knowledge Base — Copilot can only
     attach files inside the workspace, so add that folder to the VS Code
     workspace (multi-root) to make the vault readable. -->

# Repository AI Instructions

This codebase has a curated knowledge base. The human-curated part lives in the knowledge vault `~/Documents/Engineering Knowledge Base` (add it to the workspace so it's readable); the generated part lives in this workspace. Use it before answering structural or behavioral questions:

- `<vault>/architecture/system-overview.md` — the system map. Start here for any "how/where/why" question.
- `<vault>/architecture/<module>.md` — per-module deep docs (purpose, entry points, callers, gotchas).
- `<vault>/architecture/contracts/<repo>.md` — what each repo exposes publicly and who consumes it.
- `docs/api-surface/` — in this workspace. GENERATED, compiler-verified (Roslyn) public API dumps and the project dependency graph. Ground truth for what is public and what references what. Never edit by hand.
- `<vault>/knowledge/` — resolved investigations with freshness stamps (repo + commit + watched paths). Useful prior findings, but dated: if their watched paths changed since the stamp, re-verify against current code first.

Rules:
1. Consult the overview and relevant module doc before proposing changes; cite file paths for claims about code.
2. Changing anything public? Check the repo contract doc and the api-surface dump first — external clients may consume it.
3. If code contradicts a doc, trust the code and note the needed doc correction.
4. If information is missing, say so — do not fabricate.
5. Prior findings in the vault's `knowledge/` folder are dated evidence, not ground truth — current code wins on any conflict.
6. This is a C#/.NET codebase with WinForms UI: respect UI-thread rules (no blocking calls on the UI thread; marshal via Invoke/BeginInvoke) and existing conventions in `.kiro/steering/tech.md` if present.
