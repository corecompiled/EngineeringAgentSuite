# Using This Mechanism in VS Code and Visual Studio

The knowledge base itself (the knowledge vault's `architecture/` + `knowledge/` plus
workspace `docs/api-surface/`), the prompts, the dumper, and the freshness checker are
**tool-agnostic** — only thin per-tool adapters differ. This guide covers the GitHub Copilot
adapters for VS Code and Visual Studio. (Kiro: see `05-USING-IN-KIRO.md`. Claude Code: see
the README's last section.)

**One rule above all: the knowledge base — vault + `docs/api-surface/` — stays the single
source of truth.** Adapters point at it; they never duplicate its content. If you find
yourself copying knowledge into an adapter file, stop and link instead.

## The adapter map

| Concept | Kiro | VS Code (Copilot) | Visual Studio (Copilot) |
|---|---|---|---|
| Always-on context | `.kiro/steering/*` | `.github/copilot-instructions.md` | same file — enable in Tools > Options > GitHub > Copilot |
| Path-scoped context | steering `fileMatch` | `.github/instructions/*.instructions.md` (`applyTo` front matter) | same files |
| Reusable prompts | `prompts/*.md` via `#File` | `.github/prompts/*.prompt.md` (run as `/name` or attach) | same files via `#prompt:` in chat |
| MCP (Serena + ADO) | `.kiro/settings/mcp.json` | `.vscode/mcp.json` | `<solutiondir>\.mcp.json` — **also auto-detects `.vscode/mcp.json`** |
| Analyst persona | `.kiro/agents/codebase-analyst.json` | `.github/chatmodes/codebase-analyst.chatmode.md` (newer VS Code calls these "custom agents") | not available — use the prompts instead |
| Hooks | `.kiro/hooks/*` | recent VS Code has hooks too; not shipped in this kit | not available |
| Freshness check | opt-in SessionStart hook | run `pwsh tools/Check-DocFreshness.ps1` in the terminal | same |

The kit ships the VS Code / VS adapters under `vscode/` and `github/` — copy them per the
setup guide's step 2 table.

## VS Code setup (one-time, ~10 minutes)

1. Open the **umbrella folder** as the workspace (same folder Kiro uses).
2. Copy `vscode/mcp.json` → `.vscode/mcp.json`; replace the Serena project placeholder with
   the umbrella path. On first use, VS Code asks you to trust/start each server — start
   `serena`; start `azure-devops` **only after your policy check** (it prompts for your org
   name via the `inputs` mechanism).
3. Ensure the `.github/` files are at the umbrella root (and/or per repo):
   `copilot-instructions.md`, `instructions/`, `prompts/`, `chatmodes/`.
4. **Add the knowledge vault to the workspace** (File → Add Folder to Workspace →
   `~/Documents/Engineering Knowledge Base`) — Copilot can only read/attach files inside
   the workspace, and the curated docs live in the vault.
5. In Copilot Chat, switch to **Agent** mode — MCP tools only work there.

Day to day: chat in Agent mode as usual (the instructions file enforces the same rules as
Kiro's steering: docs first, Serena for tracing, cite paths, notes are dated evidence). Run
kit workflows via the prompt wrappers — type `/` and pick e.g.
`04-analyze-ado-item`, or attach it as context. For analysis sessions, select the
**codebase-analyst** mode from the chat mode/agent dropdown.

## Visual Studio setup (17.14+ or VS 2026)

Visual Studio is where the WinForms designer lives, so you'll likely keep coding here —
the mechanism follows you:

1. **Enable agent mode**: Tools > Options > GitHub > Copilot > Copilot Chat > enable agent
   mode; then in the chat window switch Ask → **Agent** (MCP requires it).
2. **Instructions**: in the same options area, enable *custom instructions loaded from
   `.github/copilot-instructions.md`*. Targeted `.instructions.md` files work too.
3. **MCP**: open `Everything.sln` (the umbrella) and VS auto-detects the umbrella's
   `.vscode/mcp.json` — or create `<solutiondir>\.mcp.json` with the same `servers` block
   next to whichever solution you actually open. Note VS ships MCP **tools disabled by
   default**: enable the Serena tools once in the Tools dropdown of the Copilot Chat panel.
4. **Prompts**: type `#prompt:` in chat to reference any `.github/prompts/*.prompt.md`.

Three practical VS notes: if `Everything.sln` is too heavy for daily work, code in a filtered
solution (`.slnf`) or the repo's own solution and keep the umbrella solution for analysis
sessions and the dumper. There's no custom-persona equivalent in VS — the
`copilot-instructions.md` rules plus the prompt files carry the discipline instead. And VS
has no multi-root workspaces, so reference vault docs by absolute path in chat
(`~/Documents/Engineering Knowledge Base/architecture/...`).

## What's honestly weaker outside Kiro

- **No hooks shipped**: the `.csproj` nudge, note-stamp guard, and session freshness check
  don't fire. Compensate with the scheduled `Check-DocFreshness.ps1` run (see
  `04-MAINTENANCE.md`) and the habit of ending investigations with the save-finding prompt.
- **Steering richness**: Kiro's foundation files (`product/tech/structure.md`) aren't read
  by Copilot. The important rules are mirrored in `copilot-instructions.md`; if you add a
  rule to steering, mirror it there (they're deliberately short files).
- **Knowledge-note stamps** must be filled by the prompt itself (prompt 05 already instructs
  this) since no guard hook verifies them here.

## Keeping the three editors aligned

- Change knowledge → edit the vault (or `docs/api-surface/` via the dumper) once; every tool sees it immediately.
- Change *rules* → edit in pairs: `.kiro/steering/00-knowledge-index.md` and
  `.github/copilot-instructions.md` (and the chatmode body if it's an analyst rule).
- Change MCP servers → edit in pairs: `.kiro/settings/mcp.json` and `.vscode/mcp.json`.
- New reusable workflow → add the canonical file under `prompts/`, then a 3-line wrapper
  under `github/prompts/` that points at it. Never fork the workflow text.

Authoritative docs if formats drift again: code.visualstudio.com/docs (agent customization,
MCP) and learn.microsoft.com/visualstudio (Copilot chat context, MCP servers).
