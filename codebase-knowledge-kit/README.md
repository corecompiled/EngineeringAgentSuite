# Codebase Knowledge Kit

A portable "mechanism" that gives an AI coding agent (Kiro first, GitHub Copilot and Claude Code too) a **deep, persistent understanding of a large multi-repo C#/.NET codebase**, so you can:

- ask random "how does functionality X work?" questions and get traced, accurate answers,
- analyze an **Azure DevOps work item** against the actual code (impact analysis),
- triage a **ServiceNow item** with real code context.

Everything in this kit is generic text and one small tool — **no proprietary code is included or required**. You copy it into your work environment and run it there.

---

## Read these first (in order)

| File | What it gives you |
|---|---|
| `docs/02-HOW-IT-WORKS.md` | **Start here.** Plain-language explanation of the whole mechanism — the mental model. |
| `docs/01-SETUP-GUIDE.md` | Step-by-step installation at work (umbrella workspace, dumper, Kiro, Serena, ADO MCP). |
| `docs/03-DAILY-USAGE.md` | Copy-paste recipes for daily work (ADO items, "how does X work", ServiceNow). |
| `docs/05-USING-IN-KIRO.md` | What this looks like *inside Kiro*: panel walkthrough, the shipped hooks, the analyst agent, prompts via `#File`. |
| `docs/06-USING-IN-VSCODE-AND-VISUAL-STUDIO.md` | The same mechanism in VS Code and Visual Studio via GitHub Copilot: adapter map, `.vscode/mcp.json`, instructions/prompt/chat-mode files, and what's weaker outside Kiro. |
| `docs/04-MAINTENANCE.md` | Keeping the knowledge base fresh with minimal effort. |

---

## What's in the box

```
codebase-knowledge-kit/
├── README.md                  <- you are here
├── docs/                      <- the guides (how to use this mechanism)
│   ├── 01-SETUP-GUIDE.md
│   ├── 02-HOW-IT-WORKS.md
│   ├── 03-DAILY-USAGE.md
│   ├── 04-MAINTENANCE.md
│   ├── 05-USING-IN-KIRO.md
│   └── 06-USING-IN-VSCODE-AND-VISUAL-STUDIO.md
├── tools/
│   ├── ApiSurfaceDumper/      <- Roslyn console tool: dumps public API surface -> markdown
│   └── Check-DocFreshness.ps1 <- git-based staleness report for all stamped docs/notes
├── kiro/
│   ├── steering/              <- steering file templates (Kiro's persistent context)
│   ├── hooks/                 <- agent hooks: csproj nudge, note guard, opt-in freshness check
│   ├── agents/                <- "codebase-analyst" custom agent (analysis-only persona)
│   └── settings/mcp.json      <- Serena + Azure DevOps MCP configuration template
├── templates/                 <- blank architecture-doc templates the agent fills in
│   ├── system-overview-template.md
│   ├── module-architecture-template.md
│   ├── repo-contract-template.md
│   └── knowledge-note-template.md
├── prompts/                   <- ready-made prompts you paste into Kiro
│   ├── 01-generate-module-docs.md
│   ├── 02-generate-repo-contract.md
│   ├── 03-generate-system-overview.md
│   ├── 04-analyze-ado-item.md
│   └── 05-save-finding.md
├── vscode/mcp.json            <- Serena + ADO MCP config for VS Code AND Visual Studio (both read it)
└── github/                    <- GitHub Copilot adapters (VS Code + Visual Studio)
    ├── copilot-instructions.md          <- always-on rules (same discipline as Kiro steering)
    ├── instructions/domain-EXAMPLE...   <- path-scoped rules (applyTo), mirror of domain steering
    ├── chatmodes/codebase-analyst...    <- the analyst persona for VS Code chat
    └── prompts/*.prompt.md              <- thin wrappers pointing at the canonical prompts/
```

## Where each piece goes at work

Assume you create an umbrella folder, e.g. `C:\work\everything\`, containing all your repos (setup guide, step 1).

| From the kit | To your work machine |
|---|---|
| `tools/` (dumper + freshness script) | `C:\work\everything\tools\` |
| `kiro/steering/*` | `C:\work\everything\.kiro\steering\` |
| `kiro/settings/mcp.json` | `C:\work\everything\.kiro\settings\mcp.json` |
| `kiro/hooks/*` | `C:\work\everything\.kiro\hooks\` |
| `kiro/agents/*` | `C:\work\everything\.kiro\agents\` |
| `templates/` | `C:\work\everything\docs\templates\` |
| `prompts/` | `C:\work\everything\prompts\` |
| `github/copilot-instructions.md` | umbrella root and/or `<each repo>` → `.github\copilot-instructions.md` |
| `github/instructions,chatmodes,prompts` | same `.github\` folder(s) as above |
| `vscode/mcp.json` | `C:\work\everything\.vscode\mcp.json` |
| (generated later, in-workspace) | `C:\work\everything\docs\api-surface\` — Roslyn dumps, regenerated compiler output |
| (agent-written, external vault) | `~\Documents\Engineering Knowledge Base\architecture\` and `...\knowledge\` — create the vault if it doesn't exist (it is shared with the personal agent suite's `investigations\` notes) |

## The mechanism in one sentence

> Generate a compact, compiler-verified **knowledge base as plain markdown** — curated architecture docs and reusable finding notes in an external Obsidian-compatible vault (`~/Documents/Engineering Knowledge Base`, shared with the investigation-notes suite), api-surface dumps inside the workspace — tell the agent it exists via **Kiro steering files**, give the agent **IDE-grade code navigation via Serena (MCP)** for anything the docs don't cover, and pipe **tickets in via the Azure DevOps MCP server** — so every question is answered from docs + traced code instead of guesses.
> Resolved findings are saved as **commit-stamped notes** the agent reuses (after re-verifying),
> and **hooks + a git-based freshness checker** keep the whole thing honest as the code moves.

## Honest notes

1. **ApiSurfaceDumper was written offline** against stable Roslyn 4.9 APIs but could not be compiled in the environment where this kit was authored (no package restore available). If `dotnet build` complains, paste the errors into Kiro or Claude — fixes will be one-liners.
2. Serena, Kiro, and the ADO MCP server evolve quickly. Exact flags/paths are current as of authoring; if something doesn't match, check each tool's official docs (linked in the setup guide).
3. **Check your company policy** before connecting MCP servers or entering ADO credentials. Serena runs entirely on your machine; the ADO MCP server talks to your Azure DevOps org — which is why the kit ships it `"disabled": true` in `mcp.json`. Flip it on after the check.

## Using with Claude Code instead of Kiro (optional)

The knowledge base is plain markdown, so porting is trivial: create a `CLAUDE.md` at the workspace root containing the same content as `kiro/steering/00-knowledge-index.md`, and register Serena/ADO MCP servers with `claude mcp add`. Nothing else changes.
