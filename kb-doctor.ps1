# Deterministic contract check for the Engineering Knowledge Base vault.
# Validates the investigations/ notes against shared-conventions.md section 2:
# frontmatter contract, status values, type-based filename prefixes, duplicate
# items, wikilinks, stale in-progress notes, and overall vault structure.
# FAIL = contract violation (exit 1). WARN = worth a look (exit stays 0).
# Never renames or moves files - fixes are frontmatter edits in place, done by
# you or the investigation-reviewer agent.
# ASCII-only on purpose (PS 5.1 reads BOM-less UTF-8 as ANSI).

[CmdletBinding()]
param(
    [string]$KnowledgeBasePath = (Join-Path $HOME 'Documents\Engineering Knowledge Base'),
    [int]$StaleDays = 30
)

$ErrorActionPreference = 'Stop'
$failures = 0
$warnings = 0

function Report([string]$Level, [string]$Msg) {
    Write-Host ("{0,-4} {1}" -f $Level, $Msg)
    if ($Level -eq 'FAIL') { $script:failures++ }
    if ($Level -eq 'WARN') { $script:warnings++ }
}

# --- Structure -------------------------------------------------------------
if (-not (Test-Path $KnowledgeBasePath)) {
    Report 'FAIL' "vault not found: $KnowledgeBasePath"
    Write-Host ''
    Write-Host "KB doctor: 1 FAIL, 0 WARN."
    exit 1
}
Report 'OK' "vault: $KnowledgeBasePath"

$invDir = Join-Path $KnowledgeBasePath 'investigations'
if (-not (Test-Path $invDir)) {
    Report 'WARN' "investigations/ missing (agents create it on first write - fine if the vault is new)"
}

$oldVault = Join-Path $HOME 'Documents\NewSkies Knowledge Base'
if (Test-Path $oldVault) {
    Report 'WARN' "old vault folder still present: $oldVault (migration leftover - merge or remove)"
}

# Stray notes at vault root (README/Dashboard belong there; notes do not).
Get-ChildItem -Path $KnowledgeBasePath -File -Filter *.md | ForEach-Object {
    if ($_.Name -notin @('README.md', 'Dashboard.md')) {
        Report 'WARN' "stray note at vault root: $($_.Name) (notes belong in investigations/)"
    }
}

# --- Per-note checks -------------------------------------------------------
$requiredKeys = @('item', 'title', 'type', 'state', 'created', 'updated', 'status', 'confidence')
$validStatus  = @('in-progress', 'blocked-on-questions', 'completed')
# Type prefixes per shared-conventions s2 (ADO- accepted as legacy), PR, or native SNOW numbers.
$adoPattern  = '^(PBI|Feature|Bug|Task|Epic|ADO|PR)-[0-9]+-[a-z0-9-]+\.md$'
$snowPattern = '^[A-Z]{2,}[0-9]+-[a-z0-9-]+\.md$'
$itemsSeen = @{}
# Work-item prefixes share one ID space in ADO, so Bug-48211 and legacy ADO-48211
# would be the SAME item twice. PR ids live in a separate space and are excluded.
$workItemPrefixes = @('PBI', 'Feature', 'Bug', 'Task', 'Epic', 'ADO')
$workItemIdsSeen = @{}

$notes = @()
if (Test-Path $invDir) { $notes = @(Get-ChildItem -Path $invDir -File -Filter *.md) }

foreach ($file in $notes) {
    $name = $file.Name
    $lines = @(Get-Content $file.FullName)

    # Frontmatter fences + minimal key: value parse (same approach as the kit's
    # Check-DocFreshness.ps1).
    if ($lines.Count -lt 3 -or $lines[0].Trim() -ne '---') {
        Report 'FAIL' "${name}: no YAML frontmatter"
        continue
    }
    $fm = @{}
    $closed = $false
    for ($i = 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq '---') { $closed = $true; break }
        if ($lines[$i] -match '^\s*([A-Za-z_\-]+)\s*:\s*(.*)$') {
            $fm[$Matches[1].ToLowerInvariant()] = $Matches[2].Trim().Trim('"')
        }
    }
    if (-not $closed) {
        Report 'FAIL' "${name}: frontmatter never closed (missing second ---)"
        continue
    }

    $noteOk = $true
    foreach ($key in $requiredKeys) {
        if (-not $fm.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($fm[$key])) {
            Report 'FAIL' "${name}: missing frontmatter key '$key'"
            $noteOk = $false
        }
    }

    if ($fm.ContainsKey('status') -and $fm['status'] -and ($fm['status'] -notin $validStatus)) {
        Report 'FAIL' "${name}: invalid status '$($fm['status'])' (allowed: $($validStatus -join ' | '))"
        $noteOk = $false
    }

    if (($name -cnotmatch $adoPattern) -and ($name -cnotmatch $snowPattern)) {
        Report 'WARN' "${name}: filename does not match naming convention (type prefix / PR / native SNOW number)"
    }

    if ($fm.ContainsKey('item') -and $fm['item']) {
        $item = $fm['item']
        if ($itemsSeen.ContainsKey($item)) {
            Report 'FAIL' "${name}: duplicate item '$item' (also in $($itemsSeen[$item])) - one living note per item"
            $noteOk = $false
        } else {
            $itemsSeen[$item] = $name
        }
        if ($item -match '^([A-Za-z]+)-([0-9]+)$' -and ($Matches[1] -in $workItemPrefixes)) {
            $id = $Matches[2]
            if ($workItemIdsSeen.ContainsKey($id)) {
                Report 'FAIL' "${name}: work item id $id already covered by $($workItemIdsSeen[$id]) under a different prefix - one living note per item"
                $noteOk = $false
            } else {
                $workItemIdsSeen[$id] = $name
            }
        }
        # Filename prefix must match the item prefix (both sides before the first dash).
        if (($name.Contains('-')) -and ($item.Contains('-'))) {
            $filePrefix = ($name -split '-')[0]
            $itemPrefix = ($item -split '-')[0]
            if (($filePrefix -match '^[A-Za-z]+$') -and ($filePrefix -cne $itemPrefix)) {
                Report 'WARN' "${name}: frontmatter item '$item' prefix does not match filename prefix '$filePrefix'"
            }
        }
    }

    $body = ($lines | Select-Object -Skip ($i + 1)) -join "`n"
    if ($body -match '\[\[') {
        Report 'WARN' "${name}: contains [[wikilinks]] (plain markdown links only, per conventions)"
    }

    if ($fm.ContainsKey('updated') -and $fm['updated'] -and $fm['status'] -ne 'completed') {
        try {
            $age = ((Get-Date) - [datetime]$fm['updated']).TotalDays
            if ($age -gt $StaleDays) {
                Report 'WARN' "${name}: stale - not completed and untouched for $([int]$age) days"
            }
        } catch {
            Report 'WARN' "${name}: unparseable updated date '$($fm['updated'])'"
        }
    }

    if ($noteOk) { Report 'OK' "${name}" }
}

Write-Host ''
Write-Host ("KB doctor: {0} note(s) checked, {1} FAIL, {2} WARN." -f $notes.Count, $failures, $warnings)
if ($failures -gt 0) { exit 1 }
exit 0
