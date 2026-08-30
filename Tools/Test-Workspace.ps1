[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$requiredPaths = @(
    'Assets',
    'Packages/manifest.json',
    'ProjectSettings/ProjectVersion.txt',
    'Docs/PROJECT_STATUS.md',
    '.gitignore'
)

foreach ($relativePath in $requiredPaths) {
    $fullPath = Join-Path $repositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Required path is missing: $relativePath"
    }
}

$manifestPath = Join-Path $repositoryRoot 'Packages/manifest.json'
Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json | Out-Null

Push-Location $repositoryRoot
try {
    $ignoredPaths = @('Library/probe', 'Temp/probe', 'Logs/probe', 'UserSettings/probe')
    foreach ($ignoredPath in $ignoredPaths) {
        git check-ignore --no-index $ignoredPath | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Unity generated-folder ignore rule is missing for: $ignoredPath"
        }
    }
}
finally {
    Pop-Location
}

Write-Output 'Workspace structure, package manifest, and ignore rules passed.'
