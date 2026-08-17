---
generated: <YYYY-MM-DD>
repo: <repo folder name under the umbrella root; use (multi) if it spans repos>
source-commit: <output of: git -C <repo> rev-parse --short HEAD>
watch-paths: <comma-separated folders/files this finding depends on, relative to the repo>
tags: <comma-separated, e.g. billing, ado-48213, bug, perf>
status: verified
---

# Q: <the question or ticket, phrased as asked>

## Answer (short)
2–6 sentences. The conclusion someone should be able to act on without re-reading the trace.

## Evidence / trace
The proof, as paths and symbols (this is what makes the note re-verifiable later):
- `<repo>/<path>` — `<Type.Member>` — why it matters
- ...

## Related
- Ticket: ADO <id> / SNOW <id> (if any)
- Module doc(s): `architecture/<module>.md` (vault-relative link — clickable in Obsidian)
- Prior notes: `knowledge/<file>.md` (if this supersedes or extends one)

## Reuse guard
This note was verified against `source-commit` above. Before relying on it:
run `pwsh tools/Check-DocFreshness.ps1` **from the umbrella workspace root** (or check whether `watch-paths` changed since
`source-commit`). If DRIFTING/STALE → re-verify the trace with Serena, then update
`source-commit` and `status` here (or set `status: superseded` and link the new note).
