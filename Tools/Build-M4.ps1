param(
    [string]$UnityEditorPath = 'D:\Unity\Hub\Editor\6000.0.82f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$env:ALLUSERSPROFILE = 'C:\ProgramData'
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Logs') -Force | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-Unity.ps1') -UnityEditorPath $UnityEditorPath -UnityArguments @(
    '-quit',
    '-executeMethod', 'FunGame.Editor.M4CoopIntegrationBootstrap.BuildWindowsDevelopment',
    '-logFile', (Join-Path $projectRoot 'Logs\M4-Build.log')
)
