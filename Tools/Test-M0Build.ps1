param(
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$executablePath = Join-Path $projectRoot 'Builds\M0-Windows\FunGame-M0.exe'
$logPath = Join-Path $projectRoot 'Logs\M0-Player-Smoke.log'

if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "M0 executable not found at '$executablePath'. Run Tools/Build-M0.ps1 first."
}

$process = Start-Process -FilePath $executablePath `
    -ArgumentList @('-batchmode', '--m0-smoke', '-logFile', $logPath) `
    -WorkingDirectory (Split-Path -Parent $executablePath) `
    -WindowStyle Hidden `
    -PassThru

if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill()
    throw "M0 smoke run exceeded $TimeoutSeconds seconds and was terminated."
}

if ($process.ExitCode -ne 0) {
    throw "M0 smoke run exited with code $($process.ExitCode)."
}

$log = Get-Content -LiteralPath $logPath -Raw
if (-not $log.Contains('[M0] Runtime smoke check passed; exiting normally.')) {
    throw 'M0 smoke confirmation was not found in the player log.'
}

Write-Host 'M0 player smoke run passed.'
