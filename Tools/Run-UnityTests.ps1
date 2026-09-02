param(
    [ValidateSet('EditMode', 'PlayMode', 'All')]
    [string]$Mode = 'All',

    [string]$UnityEditorPath = 'E:\Unity\UnityEditor\6000.0.82f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$modes = if ($Mode -eq 'All') { @('EditMode', 'PlayMode') } else { @($Mode) }

foreach ($testMode in $modes) {
    $resultPath = Join-Path $projectRoot "TestResults\$testMode.xml"
    $logPath = Join-Path $projectRoot "Logs\Tests-$testMode.log"

    & (Join-Path $PSScriptRoot 'Invoke-Unity.ps1') `
        -UnityEditorPath $UnityEditorPath `
        -UnityArguments @(
            '-runTests',
            '-testPlatform', $testMode,
            '-testResults', $resultPath,
            '-logFile', $logPath
        )
}
