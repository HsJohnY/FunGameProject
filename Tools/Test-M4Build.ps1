param([int]$TimeoutSeconds = 90)

chcp 65001 | Out-Null
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $projectRoot 'Builds\M4-Coop-Windows\FunGame-M4-Coop.exe'
$output = Join-Path $projectRoot ('Logs\M4-PlayerCheck-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$logPath = Join-Path $output 'Player.log'
$process = Start-Process -FilePath $executable -ArgumentList @(
    ('"--m4-check-output=' + $output + '"'), '-logFile', ('"' + $logPath + '"'),
    '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720'
) -WorkingDirectory (Split-Path -Parent $executable) -WindowStyle Hidden -PassThru
try {
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) { throw "Player verification timed out. Logs: $output" }
    if ($process.ExitCode -ne 0) { throw "Player verification failed. Logs: $output" }
    $text = Get-Content -LiteralPath $logPath -Raw -Encoding UTF8
    if (-not $text.Contains('[M4BuildCheck] PASS:')) { throw "Player PASS marker missing. Logs: $output" }
    Write-Host "PASS: host, tools, solo, settings, mode reload and additive scene cleanup. Screenshots: $output"
} finally {
    if (-not $process.HasExited) { $process.Kill() }
}
