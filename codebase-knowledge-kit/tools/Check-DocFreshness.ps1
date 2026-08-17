<#
.SYNOPSIS
  Reports which knowledge-base docs and notes are stale, deterministically, using git.

.DESCRIPTION
  Every generated/curated doc carries YAML front matter like:

    ---
    generated: 2026-08-16
    repo: main-repo                         # repo FOLDER NAME under the umbrella root
    source-commit: abc1234                  # git -C <repo> rev-parse --short HEAD at writing time
    watch-paths: src/Billing, src/Shared    # comma-separated, relative to that repo
    ---

  For each doc this script asks git: "which files under watch-paths changed between
  source-commit and HEAD?" and rates the doc:

    FRESH             0 changed files
    DRIFTING          1..DriftThreshold changed files  -> skim the doc soon
    STALE             more than DriftThreshold         -> regenerate / re-verify
    AGE-ONLY          repo is (multi) (e.g. system-overview) -> age reported instead
    UNSTAMPED         no front matter                  -> add the stamp
    STAMP-INCOMPLETE  front matter missing source-commit
    REPO-NOT-FOUND    repo folder is not a git repo here
    GIT-ERROR         git rejected the range (commit no longer reachable?)

.USAGE
  From the umbrella root (repos must live under -Root for commit stamps to resolve):
    pwsh tools/Check-DocFreshness.ps1
    pwsh tools/Check-DocFreshness.ps1 -DriftThreshold 10
    pwsh tools/Check-DocFreshness.ps1 -KnowledgeBasePath "D:\some\other\vault"
    pwsh tools/Check-DocFreshness.ps1 -DocFolders docs/architecture,docs/knowledge   # legacy in-workspace layout
  By default the stamped docs are read from the external knowledge vault
  (~/Documents/Engineering Knowledge Base: architecture/ + knowledge/).
  Exit code is 1 if anything is STALE (handy for a weekly scheduled task/pipeline).
#>
[CmdletBinding()]
param(
    [string]$Root = (Get-Location).Path,   # umbrella workspace root: the repos live here
    [string]$KnowledgeBasePath = (Join-Path $HOME 'Documents\Engineering Knowledge Base'),
    [string[]]$DocFolders,                 # absolute entries used as-is; relative entries resolve against -Root
    [int]$DriftThreshold = 5
)

if (-not $DocFolders) {
    $DocFolders = @(
        (Join-Path $KnowledgeBasePath 'architecture'),
        (Join-Path $KnowledgeBasePath 'knowledge')
    )
}

$results = New-Object System.Collections.Generic.List[object]

function Add-Result([string]$Doc, [string]$Status, [string]$Changed, [string]$Details) {
    $results.Add([pscustomobject]@{ Doc = $Doc; Status = $Status; Changed = $Changed; Details = $Details })
}

# Prefix-strip relative path (works on Windows PowerShell 5.1 too, where
# [IO.Path]::GetRelativePath is unavailable).
function Get-RelPath([string]$Base, [string]$Full) {
    $b = $Base.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if ($Full.StartsWith($b, [StringComparison]::OrdinalIgnoreCase)) { return $Full.Substring($b.Length) }
    return $Full
}

foreach ($folder in $DocFolders) {
    $full = if ([IO.Path]::IsPathRooted($folder)) { $folder } else { Join-Path $Root $folder }
    if (-not (Test-Path $full)) { continue }

    foreach ($file in Get-ChildItem $full -Recurse -Filter *.md) {
        $rel = if ($file.FullName.StartsWith($KnowledgeBasePath, [StringComparison]::OrdinalIgnoreCase)) {
            'vault:' + (Get-RelPath $KnowledgeBasePath $file.FullName)
        } else {
            Get-RelPath $Root $file.FullName
        }
        $lines = @(Get-Content $file.FullName -TotalCount 40)

        if ($lines.Count -lt 3 -or $lines[0].Trim() -ne '---') {
            Add-Result $rel 'UNSTAMPED' '-' 'no YAML front matter'
            continue
        }

        # Minimal front-matter parse: "key: value" lines until the closing ---
        $fm = @{}
        for ($i = 1; $i -lt $lines.Count -and $lines[$i].Trim() -ne '---'; $i++) {
            if ($lines[$i] -match '^\s*([A-Za-z_\-]+)\s*:\s*(.*)$') {
                $fm[$Matches[1].ToLowerInvariant()] = $Matches[2].Trim()
            }
        }

        $generated = $fm['generated']
        $repo      = $fm['repo']
        $sha       = $fm['source-commit']
        $watch     = $fm['watch-paths']

        if ([string]::IsNullOrWhiteSpace($repo) -or $repo -in @('(multi)', 'multi')) {
            $ageText = 'multi-repo doc, no generated date'
            if ($generated) {
                try { $ageText = 'multi-repo doc, generated {0} day(s) ago' -f [int]((Get-Date) - [datetime]$generated).TotalDays } catch { }
            }
            Add-Result $rel 'AGE-ONLY' '-' $ageText
            continue
        }
        if ([string]::IsNullOrWhiteSpace($sha)) {
            Add-Result $rel 'STAMP-INCOMPLETE' '-' 'missing source-commit'
            continue
        }

        # Repos still live under the umbrella root, even though the docs come from the vault.
        $repoPath = Join-Path $Root $repo
        if (-not (Test-Path (Join-Path $repoPath '.git'))) {
            Add-Result $rel 'REPO-NOT-FOUND' '-' "no git repo at ./$repo"
            continue
        }

        $pathspecs = @('.')
        if (-not [string]::IsNullOrWhiteSpace($watch)) {
            $pathspecs = @($watch.Trim('[', ']') -split ',' |
                ForEach-Object { $_.Trim().Trim('"').Trim("'") } |
                Where-Object { $_ })
        }

        $changed = & git -C $repoPath diff --name-only "$sha..HEAD" -- @pathspecs 2>$null
        if ($LASTEXITCODE -ne 0) {
            Add-Result $rel 'GIT-ERROR' '-' "git diff failed for range $sha..HEAD"
            continue
        }

        $changedList = @($changed | Where-Object { $_ })
        $n = $changedList.Count
        $status = if ($n -eq 0) { 'FRESH' } elseif ($n -le $DriftThreshold) { 'DRIFTING' } else { 'STALE' }
        $details = if ($n -gt 0) { ($changedList | Select-Object -First 3) -join '; ' } else { "unchanged since $sha" }
        Add-Result $rel $status "$n" $details
    }
}

if ($results.Count -eq 0) {
    Write-Host "No .md docs found under: $($DocFolders -join ', ')  (run this from the umbrella root; vault default: $KnowledgeBasePath)"
    exit 0
}

$order = @{ 'STALE' = 0; 'GIT-ERROR' = 1; 'REPO-NOT-FOUND' = 2; 'STAMP-INCOMPLETE' = 3; 'UNSTAMPED' = 4; 'DRIFTING' = 5; 'AGE-ONLY' = 6; 'FRESH' = 7 }
$results | Sort-Object { $order[$_.Status] }, Doc | Format-Table -AutoSize

$staleCount = @($results | Where-Object Status -eq 'STALE').Count
$driftCount = @($results | Where-Object Status -eq 'DRIFTING').Count
$freshCount = @($results | Where-Object Status -eq 'FRESH').Count
Write-Host ''
Write-Host ("Summary: {0} docs | {1} STALE | {2} DRIFTING | {3} FRESH" -f $results.Count, $staleCount, $driftCount, $freshCount)
if ($staleCount -gt 0) { exit 1 }
