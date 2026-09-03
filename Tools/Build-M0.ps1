param(
    [string]$UnityEditorPath = 'D:\Unity\Hub\Editor\6000.0.82f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$logPath = Join-Path $projectRoot 'Logs\M0-Build.log'

& (Join-Path $PSScriptRoot 'Invoke-Unity.ps1') `
    -UnityEditorPath $UnityEditorPath `
    -UnityArguments @(
        '-quit',
        '-executeMethod', 'FunGame.Editor.M0ProjectBootstrap.BuildWindowsDevelopment',
        '-logFile', $logPath
    )
