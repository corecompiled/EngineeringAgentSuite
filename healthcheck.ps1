# Deterministic health check for the KiroPersonalAgents agent suite.
# Complements the agent-side habits (skills audit, memory pruning, revalidation):
#   1. Lints every agent JSON and hook file.
#   2. Verifies every prompt/resource path referenced by the agent JSONs resolves.
#   3. Verifies each main agent has a memory file.
#   4. Diffs repo prompts/skills against the ~/.kiro copies and reports drift
#      (drift = someone self-edited globally; sync back to the repo before re-running install.ps1).

$ErrorActionPreference = "Stop"
$repo = $PSScriptRoot
$agentsDir = Join-Path $repo ".kiro\agents"
$issues = 0

function Report($ok, $msg) {
    if ($ok) { Write-Host "OK   $msg" } else { Write-Host "FAIL $msg"; $script:issues++ }
}

# 1. JSON lint
Get-ChildItem -Recurse -File -Path (Join-Path $repo ".kiro") -Include *.json, *.kiro.hook | ForEach-Object {
    try { Get-Content $_.FullName -Raw | ConvertFrom-Json | Out-Null; Report $true "lint: $($_.Name)" }
    catch { Report $false "lint: $($_.FullName) - $($_.Exception.Message)" }
}

# 2. Resource/prompt references resolve (file:// only; skill:// globs and ~ paths are runtime-resolved)
Get-ChildItem -Path $agentsDir -Filter *.json | ForEach-Object {
    $cfg = Get-Content $_.FullName -Raw | ConvertFrom-Json
    $refs = @()
    if ($cfg.prompt -is [string] -and $cfg.prompt.StartsWith("file://")) { $refs += $cfg.prompt }
    if ($cfg.resources) { $refs += ($cfg.resources | Where-Object { $_ -is [string] -and $_.StartsWith("file://") -and -not $_.Contains("~") }) }
    foreach ($r in $refs) {
        $rel = $r.Substring(7)
        $p = if ($rel.StartsWith("./")) { Join-Path $agentsDir $rel.Substring(2) } else { Join-Path $repo $rel }
        Report (Test-Path $p) "ref: $($_.BaseName) -> $rel"
    }
}

# 3. Main agents have memory files (agents that declare a ./memory resource)
Get-ChildItem -Path $agentsDir -Filter *.json | ForEach-Object {
    $cfg = Get-Content $_.FullName -Raw | ConvertFrom-Json
    $memRef = $cfg.resources | Where-Object { $_ -is [string] -and $_ -like "file://./memory/*" }
    if ($memRef) {
        $p = Join-Path $agentsDir ($memRef.Substring(7).Substring(2))
        Report (Test-Path $p) "memory: $($_.BaseName)"
    }
}

# 4. Drift: repo vs ~/.kiro (prompts, agent JSONs, skills)
$pairs = @(
    @{ src = Join-Path $agentsDir "prompts"; dst = Join-Path $HOME ".kiro\agents\prompts"; label = "prompts" },
    @{ src = $agentsDir;                     dst = Join-Path $HOME ".kiro\agents";         label = "agents"; filter = "*.json" },
    @{ src = Join-Path $repo ".kiro\skills"; dst = Join-Path $HOME ".kiro\skills";         label = "skills" }
)
$drift = 0
foreach ($pair in $pairs) {
    if (-not (Test-Path $pair.dst)) { Write-Host "SKIP drift($($pair.label)): no global install"; continue }
    $filter = if ($pair.filter) { $pair.filter } else { "*" }
    Get-ChildItem -Recurse -File -Path $pair.src -Filter $filter | ForEach-Object {
        $relPath = $_.FullName.Substring($pair.src.Length + 1)
        $twin = Join-Path $pair.dst $relPath
        if (Test-Path $twin) {
            $a = (Get-FileHash $_.FullName).Hash; $b = (Get-FileHash $twin).Hash
            if ($a -ne $b) { Write-Host "DRIFT $($pair.label): $relPath (repo differs from ~/.kiro - reconcile, then re-run install.ps1)"; $script:drift++ }
        }
    }
}
if ($drift -eq 0) { Write-Host "OK   drift: repo and ~/.kiro copies match" }

Write-Host ""
if ($issues -eq 0) { Write-Host "Health check passed ($drift drifted file(s) to reconcile)." }
else { Write-Host "Health check found $issues issue(s) and $drift drifted file(s)."; exit 1 }
