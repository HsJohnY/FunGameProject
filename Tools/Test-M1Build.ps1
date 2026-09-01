param(
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$executablePath = Join-Path $projectRoot 'Builds\M1-Windows\FunGame-M1.exe'
$logPath = Join-Path $projectRoot 'Logs\M1-Player-Smoke.log'

if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "M1 executable not found at '$executablePath'. Run Tools/Build-M1.ps1 first."
}

$process = Start-Process -FilePath $executablePath `
    -ArgumentList @('-batchmode', '--m1-6-smoke', '-logFile', $logPath) `
    -WorkingDirectory (Split-Path -Parent $executablePath) `
    -WindowStyle Hidden `
    -PassThru

if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill()
    throw "M1 smoke run exceeded $TimeoutSeconds seconds and was terminated."
}

if ($process.ExitCode -ne 0) {
    throw "M1 smoke run exited with code $($process.ExitCode)."
}

$log = Get-Content -LiteralPath $logPath -Raw
if (-not $log.Contains('[Checkpoint] m1-6-graybox-candidate 冒烟检查通过，程序正常退出。')) {
    throw 'M1 smoke confirmation was not found in the player log.'
}

Write-Host 'M1 player smoke run passed.'
