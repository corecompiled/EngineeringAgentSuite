# Prompt 04 — Analyze an Azure DevOps Work Item Against the Code

Paste everything below the line into Kiro, set the work item ID. (For ServiceNow or any pasted ticket: replace step 1 with "Here is the ticket text: ..." — the rest is identical.)

---

Perform a code-grounded impact analysis of an Azure DevOps work item.

WORK ITEM ID: <ID>

METHOD — follow strictly, in order:
1. Fetch the work item via the Azure DevOps MCP server: title, type, state, description, acceptance criteria, repro steps, comments, linked items. Restate it in 3–5 bullets to confirm understanding.
2. Locate: using the knowledge vault's `architecture/system-overview.md` (`~/Documents/Engineering Knowledge Base/architecture/`) and the module docs, identify which module(s)/repo(s) this concerns. State your reasoning in one or two lines.
3. Trace: with Serena (`find_symbol`, `find_referencing_symbols`, `get_symbols_overview`), find the concrete entry points and the code paths involved. Follow callers across projects/repos in this workspace.
4. Exposure check: against `docs/api-surface/` and the vault's `architecture/contracts/`, determine whether anything in the affected paths is public surface with possible external consumers.

OUTPUT — exactly these sections:
## Work item summary
## Affected areas
(table: repo | module | key types/methods | file paths)
## Call-chain notes
(who calls what; where behavior would change; UI-thread implications if WinForms code is touched)
## External exposure & compatibility risk
## Risks & unknowns
(each risk rated Low/Med/High with one-line justification)
## Suggested tests
(existing tests to run + new tests to add, with locations)
## Questions for the item author
(only questions the code genuinely cannot answer)

RULES
- Every code claim cites a file path. No path, no claim.
- Docs vs. code conflict → trust code, note the doc correction needed.
- If the work item is too vague to locate confidently, say so after step 2 and ask the clarifying questions instead of guessing onward.
