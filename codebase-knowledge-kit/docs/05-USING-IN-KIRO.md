# Using This Mechanism Inside Kiro (IDE and CLI)

This is the "what do I actually click and type" guide. It assumes setup (docs/01) is done.
Written against Kiro IDE 1.x / CLI 3.x — hook and agent formats changed in those versions,
and this kit ships the *current* formats.

## What you'll see when you open the umbrella folder

Open `C:\work\everything\` as the workspace. In the Kiro panel:

- **Steering** lists the files from `.kiro/steering/` — the knowledge index and foundation
  files are injected into every conversation automatically. Nothing to click.
- **MCP** shows `serena` (connected) and `azure-devops` (enable after your policy check).
- **Agent Hooks** shows the three hooks from `.kiro/hooks/knowledge-base-hooks.json`
  (you can toggle each one there).
- **Agents**: the `codebase-analyst` custom agent from `.kiro/agents/` appears in the
  agent selector in chat.

Kiro CLI in the same folder picks up all four automatically — steering, MCP, hooks, and
agents are shared configs, so IDE and terminal behave the same.

## Everyday flows

**Ask how something works** — just chat. Steering already tells the agent to consult the
docs and use Serena. For best results use Recipe 1 phrasing from `docs/03-DAILY-USAGE.md`.

**Run a kit prompt without copy-pasting** — reference the file as context:

```
Follow the workflow in #prompts/04-analyze-ado-item.md for work item 48213.
```

(`#File`/`#Folder` attach workspace files to the message; works for all five prompts.)

**Use the analyst agent** — pick `codebase-analyst` from the agent selector in IDE chat,
or select it via the agent command/flag in Kiro CLI. What it changes versus the default
agent: it preloads the knowledge index + system overview as resources, pre-approves the
safe shell commands this kit uses (git, the freshness checker, the dumper) so you're not
spammed with permission prompts, and its instructions confine writes to the knowledge
vault (`architecture/`, `knowledge/`) and workspace `docs/` — it
analyzes and documents, it doesn't touch source. Use the default agent when you want code
edited; use the analyst for questions, ticket analysis, and knowledge upkeep.

**Save a finding** — end of an investigation:

```
Run #prompts/05-save-finding.md for what we just established.
```

Prompt 05's step 1 makes the stamp mandatory; the `knowledge-note-front-matter-guard`
hook double-checks it, but only fires if the vault folder is attached to the workspace
(Kiro file hooks watch the open workspace, and the vault lives outside it).

**Implement (not just analyze) an ADO item** — do the analysis first, then start a
**Spec** and paste the impact analysis into the requirements phase. The module docs and
contracts make the design phase dramatically better because Kiro can cite real
constraints instead of inventing them.

## The three shipped hooks

| Hook | Fires when | What it does |
|---|---|---|
| `csproj-change-api-surface-reminder` | any `.csproj` is saved | One-paragraph nudge: api-surface may be stale; offers the dumper command and a contract-doc review. Rare but high-signal. |
| `knowledge-note-front-matter-guard` | a new file is created in the vault's `knowledge/` folder | Checks the note's front matter against the template; fixes missing stamps (runs `git rev-parse` itself). Only fires if the vault is attached to the workspace — prompt 05's mandatory stamp step is the primary guard. |
| `session-start-freshness-check` | new session starts (**disabled by default**) | Runs `Check-DocFreshness.ps1` quietly; speaks up only if something is STALE. Enable it in the Agent Hooks panel (or set `"enabled": true`) once you're comfortable with the small per-session cost. |

A deliberate design choice: **no hooks on `*.cs` saves** — they'd fire constantly and
burn attention/credits. Staleness is caught by the csproj hook, the checker script, and
steering rule 5 (agent flags doc drift whenever it notices it) instead.

Note for anyone reading older Kiro tutorials: "manual button" hooks (`userTriggered`)
were removed in IDE 1.0 — manual workflows are now done via prompts-as-files (like this
kit's `prompts/`) or manual steering files (`inclusion: manual` front matter, pulled in
on demand).

## Growing it later (optional, in order of payoff)

1. **Scheduled freshness**: a weekly Windows scheduled task or ADO pipeline step running
   the dumper + `Check-DocFreshness.ps1` (its exit code 1 on STALE makes it gate-able),
   opening a PR with the api-surface diff.
2. **Agent Skills**: Kiro supports skills in `.kiro/skills/` — the five prompts can
   graduate into skills so they trigger by name/description instead of `#File` references.
3. **Sub-agents**: Kiro can invoke custom agents as sub-agents — e.g., delegate "trace
   callers of X across repos" to `codebase-analyst` while a coding agent keeps working.
4. **Package as a Power**: Kiro Powers bundle steering + MCP config into a shareable,
   on-demand package — the natural way to hand this whole mechanism to teammates once
   it's proven on your machine.

When in doubt about current syntax, the authoritative pages are kiro.dev/docs/hooks,
kiro.dev/docs/custom-agents, kiro.dev/docs/steering, and kiro.dev/docs/mcp.

Working in VS Code or Visual Studio instead of Kiro? Same knowledge base, different
adapters — see `06-USING-IN-VSCODE-AND-VISUAL-STUDIO.md`.
