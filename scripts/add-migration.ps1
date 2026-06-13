#!/usr/bin/env pwsh
#Requires -Version 7
<#
.SYNOPSIS
    Adds an EF Core migration to every database-provider project in one command.

.DESCRIPTION
    EF Core migrations are provider-specific, so each Gw2Gizmos.Data.Provider.* project keeps its own
    migration set + model snapshot. This authors the same-named migration in all of them (using each
    project's design-time factory), keeping them in sync. New provider projects are discovered
    automatically — no edits to this script are needed when one is added.

.EXAMPLE
    ./scripts/add-migration.ps1 AddItemRarityColumn
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string]$Name
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$providerProjects = Get-ChildItem (Join-Path $repoRoot 'src') -Directory -Filter 'Gw2Gizmos.Data.Provider.*'

if (-not $providerProjects) {
    throw 'No Gw2Gizmos.Data.Provider.* projects found under src/.'
}

foreach ($project in $providerProjects) {
    Write-Host "==> $($project.Name): adding migration '$Name'" -ForegroundColor Cyan
    # Each provider project is its own startup: it carries Microsoft.EntityFrameworkCore.Design and an
    # IDesignTimeDbContextFactory, so the tools need no separate runnable host.
    dotnet ef migrations add $Name --project $project.FullName --startup-project $project.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef migrations add failed for $($project.Name)."
    }
}

Write-Host "Added migration '$Name' to $($providerProjects.Count) provider project(s)." -ForegroundColor Green
