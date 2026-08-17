# Setup Guide (do this at work)

Follow top to bottom. Commands are PowerShell (Windows); adjust paths if on macOS/Linux. Budget ~1–2 hours for steps 0–5, then doc generation (step 6) runs over a few days in the background of normal work.

---

## Step 0 — Prerequisites

Install / verify on your work machine:

- **.NET SDK 8+** — `dotnet --version`
- **PowerShell 7+ (`pwsh`)** — required by Serena's C# (Roslyn) language server on Windows — `pwsh --version`
- **uv** (Python tool manager, installs/runs Serena) — https://docs.astral.sh/uv/ — `uv --version`
- **Node.js 20+** (runs the ADO MCP server via `npx`) — `node --version`
- **Kiro** (IDE and/or CLI) — already in use
- **Azure CLI** logged in (`az login`) *or* an ADO Personal Access Token — for the ADO MCP server auth (see its repo for current options: https://github.com/microsoft/azure-devops-mcp)

> Policy check: confirm MCP usage and ADO tokens are allowed under your company's AI/security policy before step 4–5.

---

## Step 1 — Build the umbrella workspace

Goal: one folder containing **all related repos**, and one solution referencing **every project**, so tools can see across repo boundaries.

```powershell
mkdir C:\work\everything
cd C:\work\everything
git clone <main-repo-url>
git clone <dependent-repo-1-url>
git clone <dependent-repo-2-url>
# ...one clone per dependent repo

# Create the umbrella solution and add every csproj under this folder
dotnet new sln -n Everything
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object { dotnet sln Everything.sln add $_.FullName }
```

Notes:
- `Everything.sln` is **analysis-only**. Nobody builds or ships it; it exists so Roslyn/Serena resolve cross-repo references. Add it to `.gitignore` or keep it untracked.
- If some projects are legacy non-SDK-style (old WinForms-era `.csproj`), adding them may warn or fail — that's fine, continue; the dumper has a fallback (step 3).

## Step 2 — Copy the kit into place

Copy from this kit into `C:\work\everything\`:

```
tools\*                          -> C:\work\everything\tools\   (dumper + Check-DocFreshness.ps1)
kiro\steering\*                   -> C:\work\everything\.kiro\steering\
kiro\settings\mcp.json            -> C:\work\everything\.kiro\settings\mcp.json
kiro\hooks\*                      -> C:\work\everything\.kiro\hooks\
kiro\agents\*                     -> C:\work\everything\.kiro\agents\
templates\*                       -> C:\work\everything\docs\templates\
prompts\*                         -> C:\work\everything\prompts\
github\copilot-instructions.md    -> umbrella root and/or <each repo> -> .github\copilot-instructions.md
github\instructions,chatmodes,prompts -> same .github\ folder(s)          (VS Code / Visual Studio; see 06)
vscode\mcp.json                   -> C:\work\everything\.vscode\mcp.json  (VS Code + Visual Studio MCP)
```

Then create the output folders — the api-surface dumps stay in the workspace, while the
curated docs live in the external **knowledge vault** (`~\Documents\Engineering Knowledge
Base`, which may already exist if the personal agent suite is installed — its
`investigations\` notes share the same vault):

```powershell
mkdir C:\work\everything\docs\api-surface
New-Item -ItemType Directory -Force "$HOME\Documents\Engineering Knowledge Base\architecture\contracts"
New-Item -ItemType Directory -Force "$HOME\Documents\Engineering Knowledge Base\knowledge"
```

## Step 3 — Run the API surface dumper

```powershell
cd C:\work\everything
dotnet run --project tools\ApiSurfaceDumper -- Everything.sln docs\api-surface
```

- First run restores NuGet packages, then loading a big solution can take several minutes. Watch the per-project progress lines.
- Open `docs\api-surface\index.md` — you should see a project table and a mermaid dependency graph.
- **If loading fails or many projects show warnings** (common with legacy WinForms projects), use the fallback, which parses source directly:

```powershell
dotnet run --project tools\ApiSurfaceDumper -- --syntax-only . docs\api-surface
```

You can mix: semantic mode for the modern projects' solution, syntax-only for a legacy repo's folder into a second output dir (e.g. `docs\api-surface-legacy`).

- **If `dotnet build` errors on the tool itself**: it was authored offline against Roslyn 4.9; paste the errors into Kiro/Claude — fixes will be trivial (usually a package version bump).

## Step 4 — Wire up MCP (Serena + Azure DevOps)

1. Install Serena once:

```powershell
uv tool install -p 3.13 serena-agent@latest --prerelease=allow
serena --help   # sanity check
```

2. Edit `C:\work\everything\.kiro\settings\mcp.json` (already copied in step 2):
   - Replace `REPLACE_WITH_ABSOLUTE_WORKSPACE_PATH` with `C:/work/everything` (forward slashes are safest in JSON).
   - Replace `REPLACE_WITH_YOUR_ADO_ORG` with your Azure DevOps organization name.
   - The `azure-devops` entry ships with `"disabled": true` on purpose. Once your policy check (step 0) is cleared, set it to `false`.
3. Open the workspace in Kiro → Kiro panel → **MCP** → confirm Serena shows as connected — and Azure DevOps too, once you've enabled it (Kiro hot-reloads `mcp.json` on save). The same config is picked up by Kiro CLI.
4. First Serena run downloads the C# (Roslyn) language server; on a huge workspace the initial index takes a while — let it finish once.

> Flags and install commands for Serena change; if anything mismatches, follow https://github.com/oraios/serena (their README explicitly warns marketplace snippets go stale).
>
> Using VS Code or Visual Studio too? The equivalent MCP + instructions wiring is in `06-USING-IN-VSCODE-AND-VISUAL-STUDIO.md` — same servers, different config file locations.

## Step 5 — Smoke tests (verify each layer)

Ask Kiro, in the umbrella workspace, one question per layer:

1. **Steering**: "What knowledge base does this workspace have and when should you use it?" → It should describe the knowledge vault's `architecture/` + workspace `docs/api-surface` (proves steering loaded).
2. **API surface**: "According to docs/api-surface, which projects does <SomeProject> depend on?" → Answer should match `index.md`.
3. **Serena**: "Use Serena to find all references to <SomeWellKnownMethod> and list the calling types." → It should call `find_referencing_symbols`, not grep.
4. **ADO** (only after enabling the server in step 4): "Fetch work item <known-id> and summarize it." → It should return real ticket fields.
5. **Hooks & agent**: the Agent Hooks section of the Kiro panel should list the three kit hooks, and `codebase-analyst` should appear in the chat agent selector. (Save any `.csproj` and the reminder hook should fire.)

If any test fails, fix that layer before moving on — the layers are independent.

## Step 6 — Generate the knowledge base (the docs themselves)

Now the agent writes the architecture docs, module by module, using the templates:

1. Open `prompts\01-generate-module-docs.md`, paste it into Kiro, and run it for your **most important module first** (it names the module/project as a placeholder). Review the output like a code review — you know the system; fix what's wrong. Repeat per module (a big system might be 10–30 module docs; do a few per day).
2. Run `prompts\02-generate-repo-contract.md` once **per repo** → produces `<vault>\architecture\contracts\<repo>.md` (what each repo exposes, who consumes it — this is where your native API surface gets documented).
3. When most modules are done, run `prompts\03-generate-system-overview.md` → produces `<vault>\architecture\system-overview.md`, the map of everything. (Module docs from step 1 land in `<vault>\architecture\` too.)
4. Fill in the three foundation steering files (`product.md`, `tech.md`, `structure.md`) — either by hand or let Kiro's **Generate Steering Docs** button draft them, then trim.

**Order matters**: dumper (step 3) before doc generation (step 6), because the prompts tell the agent to ground every claim in the api-surface files.

## Step 7 — Commit & back up

Commit the workspace pieces — `docs/api-surface/`, `docs/templates/`, `.kiro/steering/`, `.kiro/hooks/`, `.kiro/agents/`, `prompts/`, and `tools/` — to whichever repo hosts your umbrella content (or a dedicated repo if the umbrella folder itself isn't one). `mcp.json` contains no secrets as shipped — but double-check before committing if you added tokens; prefer env vars for anything sensitive.

The **knowledge vault sits outside git**: give it its own backup/sync (making the vault folder its own git repo works well). Never commit vault content into a work repo.

Done. Move to `03-DAILY-USAGE.md`, and see `05-USING-IN-KIRO.md` for how all of this
surfaces inside the Kiro panel (steering, MCP, hooks, and the codebase-analyst agent).
