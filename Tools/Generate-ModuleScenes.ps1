param(
    [string]$UnityEditorPath = 'D:\Unity\Hub\Editor\6000.0.82f1\Editor\Unity.exe'
)

chcp 65001 | Out-Null
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$env:ALLUSERSPROFILE = 'C:\ProgramData'
& (Join-Path $PSScriptRoot 'Invoke-Unity.ps1') -UnityEditorPath $UnityEditorPath -UnityArguments @(
    '-quit', '-executeMethod', 'FunGame.Editor.ModularContentBuilder.GenerateEnvironmentScenes',
    '-logFile', (Join-Path $projectRoot 'Logs\Module-Scenes.log')
)
