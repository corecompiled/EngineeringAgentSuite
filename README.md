# KiroPersonalAgents — Kiro Agent Suite

**This README is the single guide to the suite: what it is, how it works, how to use it
day-to-day, how to set it up, and how it maintains itself.**

A portable suite of Kiro (IDE + CLI) agents for day-to-day senior-developer work. Every agent
shares one knowledge base (`~/Documents/NewSkies Knowledge Base`), one skills library, and one
conventions document. The suite is deliberately self-sustaining: agents remember what they
learn, update their own instructions when you change how you want them to work, and never
proceed on shaky understanding — every agent stops and asks questions unless it is ≥95%
confident it understands the task.

---

## 1. How it works

### The agents

| Agent | Shortcut | What it does |
|---|---|---|
| `ado-item-analyst` | Ctrl+Alt+1 | Analyzes an ADO work item: fetches item + full discussion, recalls prior investigations, produces a structured assessment (background, analysis, proposed solution + confidence, dev notes, manual test scenarios, next steps) in chat and in the knowledge base. |
| `snow-item-analyst` | Ctrl+Alt+2 | Same pattern for ServiceNow records (RITM/INC/Case/TASK). Reads the activity stream (work notes vs customer comments), calls out what's needed from the team, and ends with an iteratively-refined client-facing response draft (never auto-posted). |
| `codebase-qa` | Ctrl+Alt+3 | Answers free-form codebase questions from code + skills + investigation notes + memory. Clear cited answers, honest uncertainty. Read-only. |
| `investigation-reviewer` | Ctrl+Alt+4 | Lists in-progress investigation notes (flagging stale ones), discusses conclusions with you, captures a Resolution section, and sets `status: completed` in place — files never move — only when you both agree. Can also reopen completed notes. |
| `ado-pr-reviewer` | Ctrl+Alt+5 | Lists pending ADO PR reviews assigned to you (or takes a specific PR/item), then delivers a severity-ranked initial review with draft comments and a verdict + confidence. Never posts to ADO unless explicitly asked. |
| `skill-manager` | Ctrl+Alt+6 | Owns the skill lifecycle: creates a skill from a functionality + focus areas (default), and extends, retrofits, audits ("audit all skills"), or revalidates existing skills. |

Plus three read-only research delegates the main agents spawn as subagents for parallel
deep-dives: `ado-item-researcher` (ADO items/PRs via MCP), `snow-item-researcher` (ServiceNow
records via MCP), and `code-researcher` (workspace code, no MCP). The analysts, PR reviewer,
codebase-qa, and skill-manager can all delegate; `investigation-reviewer` deliberately can't —
its work is conversational.

### The shared conventions (`.kiro/agents/prompts/shared-conventions.md`)

Loaded by every agent; the rules that make the suite coherent:

- **95%-confidence gate** — before its core workflow, every agent evaluates a role-specific
  checklist; anything uncertain means it stops, states its confidence %, and asks up to five
  precise questions. Asking early is a success condition.
- **Memory** — each agent has a markdown memory file (`.kiro/agents/memory/*.memory.md`,
  auto-loaded every session) holding its MCP setup, your identity/preferences, learned facts,
  and a changelog of its own evolution. Corrections you make get captured as durable
  preferences; overgrown memory gets consolidated with your confirmation.
- **MCP bootstrap** — agents never assume tool names. On first run they inspect what's
  available (workspace or global `mcp.json`), ask which server to use, then remember it.
- **Self-maintenance** — "from now on, do X differently" makes the agent edit its own prompt
  (or the shared conventions for suite-wide rules), log the change, and apply it from the next
  session. A sync-back rule keeps repo and global copies from diverging (see §5).
- **Discretionary delegation** — agents may spawn the read-only researcher subagents when it
  buys efficiency (parallel research threads, digesting large material in a separate context,
  parallel dimensions like per-skill audit checks) and are told when NOT to (single lookups,
  context-dependent work); all writes stay with the parent agent.
- **Never mutate external systems** — nothing is written to ADO or ServiceNow unless you
  explicitly ask, and even then it's approval-gated.

### The knowledge base (`~/Documents/NewSkies Knowledge Base`)

- One folder, ADR-style: files never move or get renamed; a note's state is its frontmatter
  `status` (`in-progress | blocked-on-questions | completed`). Close-out and reopen are status
  flips with a Session Log line. `completed` notes are settled, citable knowledge; everything
  else is cited as "not final".
- Naming: `ADO-<id>-<slug>.md`, `PR-<id>-<slug>.md`, native ServiceNow numbers
  (`INC0012345-<slug>.md`). One living note per item, updated in place across sessions.
- Frontmatter includes `ns_version` (e.g. `"NS 4.10"`) — extracted automatically whenever a
  NewSkies version is mentioned anywhere in the item, discussion, or by you — so the dashboard
  shows which NS version each investigation targeted.
- **The location is provisional.** Claude/agents must explicitly ASK before ever changing it;
  changing it means updating shared-conventions.md, all prompts, agent JSON permission rules,
  the review hook, and this README together — never piecemeal.
- **Obsidian (optional):** open the folder as a vault; with the Dataview community plugin,
  `Dashboard.md` renders live tables (open / blocked / completed, with NS Version columns).
  Everything stays plain markdown; nothing breaks without Obsidian.

### Skills (`.kiro/skills/` + `~/.kiro/skills/`)

Skills are routed context: only their frontmatter loads at startup, and the body loads when the
`description` matches the task — so descriptions are written as triggers ("Use when…"). The
authoring standard (enforced by the `skill-creator` skill, owned by the `skill-manager` agent):
pointer-first content (paths, flows, gotchas — not code dumps), lean bodies with `references/`
for depth, and `Verified as of` stamps. Skills are context, not ground truth — live code wins,
and agents flag drift when they see it. See `.kiro/skills/README.md` for the rules.

---

## 2. Daily usage

Start an agent, then just talk. Examples of what to say:

| You want | Agent | Say |
|---|---|---|
| Analyze an ADO item | `ado-item-analyst` | `Analyze work item 48211` (or paste the URL) |
| Analyze a SNOW record | `snow-item-analyst` | `Analyze INC0012345` / `Analyze RITM0045678` |
| Refine the client draft | `snow-item-analyst` | `Make the draft shorter and add the ETA` |
| Ask about the code | `codebase-qa` | `How does <functionality> handle <case>?` |
| Review open notes | `investigation-reviewer` | `Review my investigations` → pick, discuss, close or reopen |
| See waiting PRs | `ado-pr-reviewer` | `What PRs are waiting on me?` |
| Review a specific PR | `ado-pr-reviewer` | `Review PR 9182` (or give an ADO item — it finds the linked PR) |
| Create a skill | `skill-manager` | `Create a skill for <functionality>, focus on <code areas>` |
| Extend a skill | `skill-manager` | `Extend skill <name> with <new area/gotcha>` |
| Check all skills conform | `skill-manager` | `Audit all skills` → report → approve retrofits |
| Re-check a skill vs live code | `skill-manager` | `Revalidate skill <name>` (run inside the code workspace) |
| Change agent behavior | any agent | `From now on, always <rule>` → it updates its own prompt |

### Launching

CLI: `kiro-cli chat --agent <name>`. Optional PowerShell profile aliases:

```powershell
function ado     { kiro-cli chat --agent ado-item-analyst }
function snow    { kiro-cli chat --agent snow-item-analyst }
function askcode { kiro-cli chat --agent codebase-qa }
function inv     { kiro-cli chat --agent investigation-reviewer }
function prs     { kiro-cli chat --agent ado-pr-reviewer }
function skills  { kiro-cli chat --agent skill-manager }
```

IDE: the agent picker, the Ctrl+Alt+1..6 shortcuts, or the one-click hooks in the Agent Hooks
panel ("Analyze ADO Item", "Analyze SNOW Item", "Ask the Codebase", "Review Investigations",
"Review My PRs", "Manage Skills"). Hooks use the `.kiro.hook` manual-trigger format; if your
Kiro version doesn't show them, recreate them in the panel with the prompt text inside each
hook file — and select the matching agent first so the right write fences apply.

---

## 3. Setup

### Install globally (recommended)

Run `install.ps1` from this repo. It syncs agents (+ prompts) to `~/.kiro/agents/` and skills
to `~/.kiro/skills/` — Kiro treats these as **user-level**, available in EVERY workspace, IDE
and CLI, including your real code repos. That's what lets `codebase-qa` and the skills work
against the live codebase: open the code repo as the workspace and the suite is just there.

- Memory files are only seeded if missing — re-running never overwrites what agents learned.
- Workspace-level `.kiro` copies override global ones on name conflict; team repos stay
  unaffected.
- "Agents and skills are per-project only" is outdated — current Kiro supports
  `~/.kiro/agents/` and `~/.kiro/skills/` (kiro.dev docs: custom agents, skills).
- Hooks are the exception (workspace-level): copy `.kiro/hooks/` into a specific repo if you
  want the buttons there.

### New PC checklist

1. Install Kiro (IDE and/or CLI) and sign in.
2. Clone this repo — `git clone https://github.com/corecompiled/KiroPersonalAgents.git` (once
   pushed) — or copy the folder.
3. Knowledge base: create `~/Documents/NewSkies Knowledge Base` (or copy the existing one to
   keep its notes/README/Dashboard; agents create a bare folder on first use otherwise).
4. Edit `.kiro/settings/mcp.json`: replace the `REPLACE_ME` values for the `ado` and `snow`
   servers (`"type": "http"` + `url` for remote servers); set `"disabled": false`; keep the
   keys named `ado`/`snow` so pre-approved tool patterns match; credentials via env vars
   (`ADO_PAT`, `SNOW_TOKEN`) — never in the file. Globally-defined servers in
   `~/.kiro/settings/mcp.json` also work; the agents' first-run bootstrap finds them and
   remembers where.
5. Run `install.ps1`.
6. Skills: drop your existing skill md files into `~/.kiro/skills/` (one folder per skill),
   then run `Audit all skills` with `skill-manager` and approve retrofits. New skills: see the
   Daily usage table.
7. Run the verification pass (below).

### Verification pass

1. `kiro-cli agent list` → all nine agents appear (a missing one = JSON syntax error; Kiro
   hides broken agents silently — `python -m json.tool <file>` to find it).
2. `kiro-cli chat --agent ado-item-analyst` → welcome shows; `/tools` lists read/write/
   subagent + `@ado/...`; `/mcp` shows the server connected.
3. First run → the agent asks which MCP server/config to use, records it in its memory file;
   restart → it doesn't ask again.
4. Give a deliberately vague item → it stops at the confidence gate with questions (every
   agent should do this on vague input).
5. Answer → it writes `~/Documents/NewSkies Knowledge Base/ADO-<id>-<slug>.md`; later updates
   edit the same file. (If your Kiro build doesn't match `~/...` permission patterns, each KB
   write asks once — functionally fine.)
6. `snow-item-analyst` with a real record → work notes vs customer comments distinguished,
   "Needed from the team" called out, client-facing draft at the end, revisions update the
   draft section in place, nothing posted to ServiceNow.
7. `codebase-qa` → cited answers (`path:line`, note filenames), in-progress notes flagged "not
   final", honest "can't determine" on unanswerables.
8. `investigation-reviewer` → lists open notes with NS version, flags stale (30+ days) ones,
   states its own resolved/not-resolved view, flips status only on mutual agreement, can
   reopen.
9. `ado-pr-reviewer` → remembers your ADO identity from first run; lists pending reviews; a
   planted defect appears in findings with `file:line` + draft comment; nothing posted to ADO.
10. `skill-manager` → `Audit all skills` produces the conformance report; retrofit + revalidate
    behave as documented.
11. `.\healthcheck.ps1` → lint, reference, memory, and drift checks all pass.
12. Self-maintenance: "from now on, always include a rollback section in Dev Notes" → the
    agent edits its own prompt, logs the change, applies it next session.

---

## 4. Maintenance (self-learning, self-correcting, self-sustaining)

- **Self-learning**: memory files accumulate MCP setups, identities, preferences, and learned
  codebase facts; your corrections are captured as durable preferences; a session-end
  reflection persists anything worth keeping.
- **Self-correcting**: live code beats skills — agents flag and offer to fix drifted skills;
  `Revalidate skill <name>` re-walks every pointer; completed investigations can be reopened;
  wrong memories get corrected in place, and overgrown memory files get consolidated with your
  confirmation.
- **Self-sustaining**: behavior changes edit the prompts themselves (repo copy first — the
  **sync-back rule**: if an agent must edit its global `~/.kiro` copy instead, it records a
  "needs sync-back" flag so `install.ps1` never silently overwrites the change). Investigations
  that surface reusable gotchas get suggested as skill extensions so knowledge compounds.
- **Health check**: `.\healthcheck.ps1` lints all configs, verifies every referenced file
  exists, confirms memory files, and reports repo-vs-global drift. Run it after edits and
  before/after `install.ps1`.
- **Changelog policy**: git history is the changelog for repo files (no per-file changelog
  sections); agent memory Changelogs record behavior changes; KB notes carry Session Logs.

---

## 5. Git & publishing

Branch `main`; commits authored as `corecompiled@gmail.com`. Remote is configured:
**https://github.com/corecompiled/KiroPersonalAgents** (keep it **private**). Nothing has been
pushed yet.

Publish checklist (when ready):

1. `git push -u origin main` (authenticated as the `corecompiled` account).
2. Update the GitHub **About**:
   - Description: `Personal Kiro agent suite — ADO work-item analysis, ServiceNow triage with
     client-facing drafts, PR reviews, codebase Q&A, and a shared investigations knowledge base.`
   - Topics: `kiro`, `ai-agents`, `azure-devops`, `servicenow`, `mcp`, `code-review`,
     `knowledge-base`
   - One command:
     ```
     gh repo edit corecompiled/KiroPersonalAgents --description "Personal Kiro agent suite - ADO work-item analysis, ServiceNow triage with client-facing drafts, PR reviews, codebase Q&A, and a shared investigations knowledge base." --add-topic kiro --add-topic ai-agents --add-topic azure-devops --add-topic servicenow --add-topic mcp --add-topic code-review --add-topic knowledge-base
     ```
3. Confirm visibility is Private: `gh repo view corecompiled/KiroPersonalAgents --json visibility`.
4. This README is the repo front page — keep it current as the suite evolves.
