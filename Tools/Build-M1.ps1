param(
    [string]$UnityEditorPath = 'E:\Unity\UnityEditor\6000.0.38f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$logPath = Join-Path $projectRoot 'Logs\M1-Build.log'

& (Join-Path $PSScriptRoot 'Invoke-Unity.ps1') `
    -UnityEditorPath $UnityEditorPath `
    -UnityArguments @(
        '-quit',
        '-executeMethod', 'FunGame.Editor.M1GrayboxBootstrap.BuildWindowsDevelopment',
        '-logFile', $logPath
    )
