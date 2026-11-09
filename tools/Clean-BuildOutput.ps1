<#
    Clean-BuildOutput.ps1 - removes regenerable build output so the workspace stays small.

    bin/, obj/ and dist/ are the only things this touches. Sources, docs, snapshots and
    artifacts/ (the last shipped zip) are left alone. Nothing here is needed to build again:
    `dotnet build` and Publish-FollowerForge.ps1 recreate all of it.
#>
[CmdletBinding()]
param([switch]$WhatIf)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot | Split-Path -Parent
if (-not $root) { $root = (Get-Location).Path }

$targets = Get-ChildItem -Path $root -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @('bin', 'obj', 'dist') -and $_.FullName -notmatch '\\artifacts\\' }

$total = 0
foreach ($dir in $targets) {
    $bytes = (Get-ChildItem $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue |
        Measure-Object -Property Length -Sum).Sum
    if (-not $bytes) { $bytes = 0 }
    $total += $bytes
    if ($WhatIf) {
        Write-Host ("would remove {0,8:N1} MB  {1}" -f ($bytes / 1MB), $dir.FullName)
    } else {
        Remove-Item $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
}
Get-ChildItem -Path $root -Filter 'publish.log' -Recurse -Force -ErrorAction SilentlyContinue |
    ForEach-Object { if ($WhatIf) { Write-Host "would remove publish.log $_" } else { Remove-Item $_.FullName -Force } }

Write-Host ("{0}: {1} folder(s), {2:N1} MB" -f ($(if ($WhatIf) { 'would free' } else { 'freed' }), $targets.Count, ($total / 1MB))) -ForegroundColor Green
