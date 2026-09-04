param(
    [string]$UnityEditorPath = 'D:\Unity\Hub\Editor\6000.0.82f1\Editor\Unity.exe'
)

chcp 65001 | Out-Null
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$mergePath = Join-Path (Split-Path -Parent $UnityEditorPath) 'Data\Tools\UnityYAMLMerge.exe'
if (-not (Test-Path -LiteralPath $mergePath -PathType Leaf)) { throw "UnityYAMLMerge not found: $mergePath" }
$driver = '"' + $mergePath.Replace('\', '/') + '" merge -p "%O" "%B" "%A" "%A"'
git -C $projectRoot config --local merge.unityyamlmerge.name 'Unity YAML semantic merge'
if ($LASTEXITCODE -ne 0) { throw 'Could not configure merge driver name.' }
git -C $projectRoot config --local merge.unityyamlmerge.driver $driver
if ($LASTEXITCODE -ne 0) { throw 'Could not configure merge driver command.' }
Write-Host 'Configured repository-local UnityYAMLMerge driver. Read AGENTS.md before editing.'
