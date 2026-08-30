[CmdletBinding()]
param(
    [string]$DestinationRoot
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $DestinationRoot) {
    $projectContainer = Split-Path $repositoryRoot -Parent
    $DestinationRoot = Join-Path $projectContainer '_Backups'
}

Push-Location $repositoryRoot
try {
    git rev-parse --is-inside-work-tree | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'The current directory is not a Git repository.' }

    $dirty = git status --porcelain
    if ($dirty) {
        throw 'The worktree is dirty. Commit the changes before creating a complete backup.'
    }

    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backupDirectory = Join-Path $DestinationRoot 'FunGameProject'
    New-Item -ItemType Directory -Force -Path $backupDirectory | Out-Null
    $bundlePath = Join-Path $backupDirectory "FunGameProject-$timestamp.bundle"

    git bundle create $bundlePath --all
    if ($LASTEXITCODE -ne 0) { throw 'Failed to create the Git bundle.' }
    git bundle verify $bundlePath
    if ($LASTEXITCODE -ne 0) { throw 'Git bundle verification failed.' }

    Write-Output "Backup created and verified: $bundlePath"
}
finally {
    Pop-Location
}
