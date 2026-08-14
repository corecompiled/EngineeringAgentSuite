# Installs/updates the SeniorDeveloper agent suite at user level (~/.kiro) so agents and
# skills are available in EVERY Kiro workspace (IDE + CLI), including your real code repos.
#
# - Agents (JSON) + prompts: always synced (repo is the source of truth).
# - Skills: always synced.
# - Memory files: copied ONLY if missing at the target — runtime memory is never overwritten.
# - Workspace-level copies (a .kiro folder inside a repo you open) always override global ones.
#
# Idempotent: safe to re-run after every repo update.

$ErrorActionPreference = "Stop"
$repo = $PSScriptRoot
$kiroHome = Join-Path $HOME ".kiro"
$agentsSrc = Join-Path $repo ".kiro\agents"
$skillsSrc = Join-Path $repo ".kiro\skills"
$agentsDst = Join-Path $kiroHome "agents"
$skillsDst = Join-Path $kiroHome "skills"

New-Item -ItemType Directory -Force -Path $agentsDst, (Join-Path $agentsDst "prompts"), (Join-Path $agentsDst "memory"), $skillsDst | Out-Null

# Agent configs + prompts: overwrite (source of truth is the repo)
Copy-Item -Path (Join-Path $agentsSrc "*.json") -Destination $agentsDst -Force
Copy-Item -Path (Join-Path $agentsSrc "prompts\*") -Destination (Join-Path $agentsDst "prompts") -Recurse -Force
Write-Host "Agents + prompts synced to $agentsDst"

# Memory: only seed files that don't exist yet (never clobber runtime state)
$seeded = 0
Get-ChildItem -Path (Join-Path $agentsSrc "memory") -File | ForEach-Object {
    $target = Join-Path $agentsDst "memory\$($_.Name)"
    if (-not (Test-Path $target)) {
        Copy-Item $_.FullName $target
        $seeded++
    }
}
Write-Host "Memory: $seeded file(s) seeded (existing memory left untouched)"

# Skills: overwrite (source of truth is the repo); user-added skills in ~/.kiro/skills that
# don't exist in the repo are left alone
Copy-Item -Path (Join-Path $skillsSrc "*") -Destination $skillsDst -Recurse -Force
Write-Host "Skills synced to $skillsDst"

Write-Host ""
Write-Host "Done. Agents and skills are now available in every Kiro workspace."
Write-Host "Note: a workspace's own .kiro/agents or .kiro/skills entries override these on name conflict."
Write-Host "Verify with: kiro-cli agent list"
