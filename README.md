# SeniorDeveloper — Kiro Agent Suite

A portable suite of Kiro (IDE + CLI) agents for day-to-day senior-developer work, sharing one
knowledge base (`~/Documents/NewSkies Knowledge Base`), one skills directory (`.kiro/skills/`),
and common patterns:
tool-agnostic MCP with a first-run bootstrap, persistent per-agent memory, self-maintenance
(agents update their own prompts/memory when you ask them to change behavior), and a universal
95%-confidence gate — every agent stops and asks questions instead of proceeding on shaky
understanding.

## The agents

| Agent | Shortcut | What it does |
|---|---|---|
| `ado-item-analyst` | Ctrl+Alt+1 | Analyzes an ADO work item: fetches item + full discussion, recalls prior investigations, produces a structured assessment (background, analysis, proposed solution + confidence, dev notes, manual test scenarios, next steps) in chat and in the knowledge base. |
| `snow-item-analyst` | Ctrl+Alt+2 | Same pattern for ServiceNow records (RITM/INC/Case/TASK). Reads the activity stream (work notes vs customer comments), calls out what's needed from the team, and ends with an iteratively-refined client-facing response draft (never auto-posted). |
| `codebase-qa` | Ctrl+Alt+3 | Answers free-form codebase questions from code + skills + investigation notes + memory. Clear cited answers, honest uncertainty. Read-only. |
| `investigation-reviewer` | Ctrl+Alt+4 | Lists in-progress investigation notes, discusses conclusions with you, captures a Resolution section, and sets `status: completed` (in place — files never move) only when you both agree they're resolved. Can also reopen completed notes. |
| `ado-pr-reviewer` | Ctrl+Alt+5 | Lists pending ADO PR reviews assigned to you (or takes a specific PR/item), then delivers a severity-ranked initial review with draft comments and a verdict + confidence. Never posts to ADO unless explicitly asked. |

Plus two read-only research delegates (`ado-item-researcher`, `snow-item-researcher`) the main
agents spawn as subagents.

## Quick launch

CLI:

```
kiro-cli chat --agent ado-item-analyst
kiro-cli chat --agent snow-item-analyst
kiro-cli chat --agent codebase-qa
kiro-cli chat --agent investigation-reviewer
kiro-cli chat --agent ado-pr-reviewer
```

Optional PowerShell profile aliases:

```powershell
function ado     { kiro-cli chat --agent ado-item-analyst }
function snow    { kiro-cli chat --agent snow-item-analyst }
function askcode { kiro-cli chat --agent codebase-qa }
function inv     { kiro-cli chat --agent investigation-reviewer }
function prs     { kiro-cli chat --agent ado-pr-reviewer }
```

IDE: pick the agent from the agent picker (or the Ctrl+Alt+1..5 shortcuts), or use the one-click
hooks in the Agent Hooks panel ("Analyze ADO Item", "Analyze SNOW Item", "Ask the Codebase",
"Review Investigations", "Review My PRs"). The hooks use the `.kiro.hook` manual-trigger format;
if your Kiro version doesn't show them in the panel, recreate them there with the prompt text
inside each hook file — and select the matching agent first so the right write fences apply.

## Setting up on a new PC

1. Install Kiro (IDE and/or CLI) and sign in.
2. Clone this repo (once a private remote exists) or copy the whole folder; open it as the
   workspace.
2b. Knowledge base: create `~/Documents/NewSkies Knowledge Base` on the new PC (or copy the
   existing one if you want its notes; or just let the agents create it on first use — the
   folder's README/Dashboard come along only if you copy).
3. Edit `.kiro/settings/mcp.json`:
   - Replace the `REPLACE_ME` values for the `ado` and `snow` servers with your company MCP
     command/URL (`"type": "http"` + `url` for remote servers).
   - Set `"disabled": false` once filled in (they ship disabled so placeholders don't error).
   - Keep the server keys named `ado` and `snow` — the agents' pre-approved tool patterns match
     those names. Different names still work; you'll just get one-time approval prompts.
   - Set credentials as environment variables (`ADO_PAT`, `SNOW_TOKEN`); never put secrets in
     the file. If your MCP servers are defined globally in `~/.kiro/settings/mcp.json` instead,
     that's fine — the agents' first-run bootstrap will find them and remember where.
4. Drop your company skills into `.kiro/skills/` (see `.kiro/skills/README.md`).
5. Run the verification pass below.

## Verification pass

1. `kiro-cli agent list` → all seven agents appear. If one is missing, its JSON has a syntax
   error (Kiro hides broken agents silently): `python -m json.tool .kiro/agents/<name>.json`.
2. `kiro-cli chat --agent ado-item-analyst` → welcome message shows; `/tools` lists read/write/
   subagent + `@ado/...`; `/mcp` shows the server connected.
3. First-run bootstrap: the agent should ask which MCP server/config to use, then record it in
   `.kiro/agents/memory/ado-item-analyst.memory.md`. Restart the session — it must not ask again.
4. Give it a deliberately ambiguous work item → it must stop at the confidence gate with its
   confidence % and numbered questions instead of proceeding.
5. Answer the questions → it analyzes and writes
   `~/Documents/NewSkies Knowledge Base/ADO-<id>-<slug>.md`; further updates edit the same file
   (bumped `updated`, Session Log line). Note: if your Kiro build doesn't match the `~/...`
   pattern in the agents' permission rules, each knowledge-base write asks for approval once —
   functionally fine, just noisier.
6. Repeat for `snow-item-analyst` with a real record → work notes vs customer comments
   distinguished; "Needed from the team" called out; client-facing draft at the end; draft
   revisions update section 7 in place; nothing posted to ServiceNow.
7. `codebase-qa` → cited answers (`path:line`, note filenames); in-progress notes flagged "not
   final"; honest "can't determine" on an unanswerable question.
8. `investigation-reviewer` → lists only notes with `status != completed`; states its own
   resolved/not-resolved view; flips a note to `status: completed` in place only after you both
   agree; can reopen a completed note (status back to `in-progress` + Session Log entry).
9. `ado-pr-reviewer` → first run asks and remembers your ADO identity; lists your pending
   reviews; a known small defect shows up in the findings with `file:line` + draft comment;
   nothing posted to ADO.
10. Self-maintenance: tell any agent "from now on, always include a rollback section in Dev
    Notes" → it edits its own prompt file, logs the change in its memory Changelog, and the
    change takes effect next session.

## How the suite maintains itself

- Ask any agent to change its behavior → it edits its own prompt under `.kiro/agents/prompts/`
  (or `shared-conventions.md` for suite-wide rules) and logs the change in its memory file.
  Changes take effect the next session.
- Durable facts (MCP setup, your ADO identity, preferences) live in
  `.kiro/agents/memory/*.memory.md` — readable, editable markdown. Correct them by hand anytime.
- The shared knowledge base lives OUTSIDE this repo at `~/Documents/NewSkies Knowledge Base`
  (see below); the agent-facing contract is in
  `.kiro/agents/prompts/shared-conventions.md` §2, and the folder's own README covers it for
  human readers.

## Knowledge base — NewSkies Knowledge Base

- Location: `~/Documents/NewSkies Knowledge Base` (Windows:
  `C:\Users\<you>\Documents\NewSkies Knowledge Base`). Agents create it on first use if missing.
- **This location is provisional.** Revisit it later; Claude/agents must explicitly ASK before
  ever changing it. Changing it means updating `shared-conventions.md`, all seven agent prompts,
  the agent JSON permission rules, the review hook, and this README together — never piecemeal.
- Lifecycle is ADR-style: ONE folder, no subfolders, files never move or get renamed. A note's
  state is its frontmatter `status` (`in-progress | blocked-on-questions | completed`);
  close-out and reopen are status flips with a Session Log line. `completed` notes are settled,
  citable knowledge; everything else is cited as "not final".
- History: notes carry their own Session Log; the folder is deliberately NOT a git repo.

## Obsidian (optional)

The knowledge base doubles as an Obsidian vault: in Obsidian choose "Open folder as vault" and
pick `NewSkies Knowledge Base`. Installing the community plugin **Dataview** makes
`Dashboard.md` render live status tables (open / blocked / completed) straight from the
frontmatter the agents already write. Everything stays plain markdown — agents don't use
Obsidian-specific syntax, and nothing breaks if you never open Obsidian.

## Git & changelog policy

This repo is under git (branch `main`); commits are authored as `corecompiled@gmail.com`. To
publish it later, create a **private** remote under the account associated with
`corecompiled@gmail.com` (GitHub private repo or Azure DevOps) and:

```
git remote add origin <private-remote-url>
git push -u origin main
```

Changelog policy: git history is the changelog for everything in this repo (prompts, agents,
skills, hooks) — no per-file changelog sections. Agent behavior changes are additionally logged
in `.kiro/agents/memory/*.memory.md` Changelogs (agents read those at session start), and
investigation notes carry their own Session Logs.
