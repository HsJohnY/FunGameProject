param(
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$executablePath = Join-Path $projectRoot 'Builds\SinglePlayerDemo-Windows\FunGame-SinglePlayerDemo.exe'
$logPath = Join-Path $projectRoot 'Logs\SinglePlayerDemo-Player-Smoke.log'

if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Single-player demo executable not found at '$executablePath'. Run Tools/Build-SinglePlayerDemo.ps1 first."
}

$process = Start-Process -FilePath $executablePath `
    -ArgumentList @('-batchmode', '--singleplayer-demo-smoke', '-logFile', $logPath) `
    -WorkingDirectory (Split-Path -Parent $executablePath) `
    -WindowStyle Hidden `
    -PassThru

if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill()
    throw "Single-player demo smoke run exceeded $TimeoutSeconds seconds and was terminated."
}

if ($process.ExitCode -ne 0) {
    throw "Single-player demo smoke run exited with code $($process.ExitCode)."
}

$log = Get-Content -LiteralPath $logPath -Raw -Encoding UTF8
if (-not $log.Contains('[Checkpoint] singleplayer-three-chapter-demo 冒烟检查通过，程序正常退出。')) {
    throw 'Single-player demo smoke confirmation was not found in the player log.'
}

Write-Host 'Single-player demo smoke run passed.'
