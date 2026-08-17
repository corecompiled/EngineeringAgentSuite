# Snapshot backup for the Engineering Knowledge Base vault.
# The vault lives OUTSIDE any code repo by design, so this script gives it its own
# git history: initializes a repo inside the vault on first run, then commits a
# snapshot of everything (notes, README/Dashboard, Obsidian settings) on each run.
# Safe to run repeatedly (no-op commit is skipped) and from a Windows scheduled task:
#   schtasks /Create /TN "KB Backup" /SC WEEKLY /D SUN /TR "powershell -NoProfile -ExecutionPolicy Bypass -File <repo>\Backup-KnowledgeBase.ps1"
# ASCII-only on purpose (PS 5.1 reads BOM-less UTF-8 as ANSI).

[CmdletBinding()]
param(
    [string]$KnowledgeBasePath = (Join-Path $HOME 'Documents\Engineering Knowledge Base'),
    [string]$Message = ''
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $KnowledgeBasePath)) {
    Write-Host "FAIL vault not found: $KnowledgeBasePath"
    exit 1
}

function Invoke-Git {
    param([string[]]$GitArgs)
    & git -C $KnowledgeBasePath @GitArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAIL git $($GitArgs -join ' ') (exit $LASTEXITCODE)"
        exit 1
    }
}

# First run: turn the vault into its own git repo and ignore Obsidian's churny
# window-state files (the rest of .obsidian/ IS backed up so settings survive).
if (-not (Test-Path (Join-Path $KnowledgeBasePath '.git'))) {
    Invoke-Git @('init')
    Write-Host "OK   initialized git repo in vault"
    $gi = Join-Path $KnowledgeBasePath '.gitignore'
    if (-not (Test-Path $gi)) {
        ".obsidian/workspace*.json" | Set-Content -Path $gi -Encoding Ascii
        Write-Host "OK   created vault .gitignore (.obsidian/workspace*.json)"
    }
}

# Snapshots must work even where no git identity is configured (fresh machine,
# scheduled-task context): give the vault repo a local identity if none resolves.
& git -C $KnowledgeBasePath config user.email *> $null
if ($LASTEXITCODE -ne 0) {
    Invoke-Git @('config', 'user.name', 'KB Backup')
    Invoke-Git @('config', 'user.email', 'kb-backup@local')
    Write-Host "OK   set repo-local git identity (KB Backup <kb-backup@local>)"
}

Invoke-Git @('add', '-A')

# Anything staged?
& git -C $KnowledgeBasePath diff --cached --quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "OK   no changes since last backup"
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Message)) {
    $Message = 'KB backup ' + (Get-Date -Format 'yyyy-MM-dd HH:mm')
}
Invoke-Git @('commit', '-q', '-m', $Message)

$hash = (& git -C $KnowledgeBasePath rev-parse --short HEAD)
$stat = (& git -C $KnowledgeBasePath show --stat --oneline HEAD | Select-Object -Last 1)
Write-Host "OK   backup committed: $hash ($($stat.Trim()))"
exit 0
