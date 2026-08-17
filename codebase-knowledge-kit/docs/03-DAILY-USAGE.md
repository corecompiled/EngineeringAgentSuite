# Daily Usage Recipes

All recipes assume you're in the **umbrella workspace** in Kiro (IDE or CLI). Two ways to run them: copy-paste and fill placeholders, or reference the prompt file directly as context, e.g. `Follow the workflow in #prompts/04-analyze-ado-item.md for work item 48213`. For analysis-heavy sessions, switch to the `codebase-analyst` agent first — fewer permission prompts, and it writes only in the knowledge vault and under `docs/` (see `05-USING-IN-KIRO.md`).

---

## Recipe 1 — "How does functionality X work?"

```
Question: how does <FUNCTIONALITY, e.g. "invoice recalculation when a line item changes"> work end to end?

Method (follow strictly):
1. Check the knowledge vault's architecture/system-overview.md (~/Documents/Engineering Knowledge Base) and the relevant module doc(s) to locate where this lives.
2. Use Serena (find_symbol, get_symbols_overview, find_referencing_symbols) to trace the actual flow —
   entry point -> business logic -> data/external calls. Do not grep; do not guess.
3. Cross-check exposure against docs/api-surface/ (is any of this public API used by other repos/clients?).

Answer with: (a) a short narrative of the flow, (b) the exact call chain with file paths,
(c) anything the architecture doc got wrong or missing (list it so I can update the doc).
```

Point (c) is how the knowledge base improves itself over time.

## Recipe 2 — Analyze an ADO work item (impact analysis)

Use `prompts/04-analyze-ado-item.md` — it's the full structured version. Quick form:

```
Fetch ADO work item <ID>. Then, using the knowledge base (the vault's architecture/, workspace docs/api-surface)
and Serena reference tracing, produce an impact analysis:
affected modules/repos, exact entry points, downstream callers that could break
(especially anything in docs/api-surface marked public — external consumers),
risks, suggested tests, and open questions for the item's author.
Cite file paths for every claim.
```

## Recipe 3 — Triage a ServiceNow item

No connector needed to start — paste the ticket:

```
Below is a ServiceNow item. Using the knowledge base and Serena, identify:
(1) which module(s) most likely produce this behavior and why,
(2) the specific code paths to inspect first (file + symbol),
(3) what logs/inputs I should request from the reporter,
(4) whether this smells like config, data, or code — with reasoning.

--- SERVICENOW ITEM ---
<paste number, short description, full description, any error text>
```

If you later want direct integration, search the MCP ecosystem for a ServiceNow MCP server (community options exist; vet against company policy first) and add it to `.kiro/settings/mcp.json` exactly like the ADO entry.

## Recipe 4 — "What breaks if I change this method?"

```
I'm considering changing the signature/behavior of <Namespace.Type.Method>.
Use Serena find_referencing_symbols (transitively where sensible) to list every caller across all repos
in this workspace, grouped by repo and module. Flag callers reached via public API surface
(check docs/api-surface + the vault's architecture/contracts/) since external clients may depend on them.
End with a risk rating and a suggested rollout approach.
```

## Recipe 5 — Onboarding a teammate (or yourself after vacation)

```
Using the knowledge vault's architecture/system-overview.md as the spine, give me a 15-minute tour of this system:
what each repo is for, the 5 most important modules, how a typical request/operation flows,
and the 3 sharpest edges (gotchas) documented in the module docs.
```

## Recipe 6 — Adding a new dependent repo later

1. `git clone` it into the umbrella folder.
2. Re-add projects: `Get-ChildItem <new-repo> -Recurse -Filter *.csproj | ForEach-Object { dotnet sln Everything.sln add $_.FullName }`
3. Re-run the dumper (step 3 of setup).
4. Run prompt 01 for its modules and prompt 02 for its contract doc.
5. Ask Kiro to update `system-overview.md` with the new repo.

## Recipe 7 — Save the finding (end of any investigation)

When a question/ticket got resolved, spend one more minute in the same chat:

```
Run prompts/05-save-finding.md for what we just established.
```

That writes a stamped note to the vault's `knowledge/` folder (question, short answer, evidence paths, the commit it was verified against). Retrieval is automatic — steering rule 8 makes the agent search prior findings first and **re-verify them against current code if their watched files changed since the stamp**. You'll notice it: answers start with "a prior note from <date> covers this; re-verified, still accurate" instead of a full re-derivation.

---

## Habits that make this work well

- **Always ask for file-path citations.** Uncited claims are where hallucinations hide.
- **Scope big questions**: "in the Billing module, ..." beats "somewhere in the codebase, ...".
- **Correct the docs when the agent is wrong** — a 2-minute doc edit pays off in every future conversation (see `04-MAINTENANCE.md`).
- **New chat per task.** Steering reloads fresh context each time; giant ever-running chats degrade.
- In VS Code / Visual Studio, the same recipes work via the Copilot adapters — identical phrasing, and the kit prompts are available as `/`-commands or `#prompt:` references (see `06-USING-IN-VSCODE-AND-VISUAL-STUDIO.md`).
