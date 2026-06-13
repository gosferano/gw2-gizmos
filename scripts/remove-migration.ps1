#!/usr/bin/env pwsh
#Requires -Version 7
<#
.SYNOPSIS
    Removes the most recent EF Core migration from every database-provider project.

.DESCRIPTION
    The mirror of add-migration.ps1: rolls back the last migration in each Gw2Gizmos.Data.Provider.*
    project so they stay in sync. Use -Force to remove a migration that has already been applied to a
    database.

.EXAMPLE
    ./scripts/remove-migration.ps1

.EXAMPLE
    ./scripts/remove-migration.ps1 -Force
#>
[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$providerProjects = Get-ChildItem (Join-Path $repoRoot 'src') -Directory -Filter 'Gw2Gizmos.Data.Provider.*'

if (-not $providerProjects) {
    throw 'No Gw2Gizmos.Data.Provider.* projects found under src/.'
}

foreach ($project in $providerProjects) {
    Write-Host "==> $($project.Name): removing last migration" -ForegroundColor Cyan
    # Each provider project is its own startup (it carries EF Core Design + a design-time factory).
    $efArgs = @('ef', 'migrations', 'remove', '--project', $project.FullName, '--startup-project', $project.FullName)
    if ($Force) { $efArgs += '--force' }
    dotnet @efArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef migrations remove failed for $($project.Name)."
    }
}

Write-Host "Removed the last migration from $($providerProjects.Count) provider project(s)." -ForegroundColor Green
