param(
    [string]$UnityEditorPath = 'D:\Unity\Hub\Editor\6000.0.82f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$logPath = Join-Path $projectRoot 'Logs\Build-SinglePlayerDemo.log'
$env:ALLUSERSPROFILE = 'C:\ProgramData'

& (Join-Path $PSScriptRoot 'Invoke-Unity.ps1') `
    -UnityEditorPath $UnityEditorPath `
    -UnityArguments @(
        '-quit',
        '-executeMethod', 'FunGame.Editor.SinglePlayerDemoBootstrap.BuildWindowsDevelopment',
        '-logFile', $logPath
    )
