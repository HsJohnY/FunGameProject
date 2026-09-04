param(
    [string]$ExecutablePath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
    $ExecutablePath = Join-Path $projectRoot 'Builds\SinglePlayerDemo-Windows\FunGame-SinglePlayerDemo.exe'
}

if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
    throw "Single-player demo executable not found at '$ExecutablePath'. Run Build-SinglePlayerDemo.ps1 first."
}

$resolvedExecutable = (Resolve-Path -LiteralPath $ExecutablePath).Path
$workingDirectory = Split-Path -Parent $resolvedExecutable
$logDirectory = Join-Path $projectRoot 'Logs'
$resultDirectory = Join-Path $projectRoot 'TestResults'
$captureDirectory = Join-Path $workingDirectory 'VerificationCaptures'
New-Item -ItemType Directory -Force -Path $logDirectory, $resultDirectory | Out-Null
$playerLog = Join-Path $logDirectory 'SinglePlayerDemo-CompletionVerification.log'
$resultPath = Join-Path $resultDirectory 'SinglePlayerDemo-BuildCompletion.txt'

$process = Start-Process -FilePath $resolvedExecutable `
    -ArgumentList @(
        '--demo-verify-completion',
        '-screen-width', '1600',
        '-screen-height', '900',
        '-screen-fullscreen', '0',
        '-logFile', $playerLog
    ) `
    -WorkingDirectory $workingDirectory `
    -WindowStyle Hidden `
    -PassThru `
    -Wait

$completion = Select-String -LiteralPath $playerLog `
    -Pattern '\[DemoVerification\] result=completed .*cooling=2/2 relays=5/5 waves=5/5 secret325=True' `
    | Select-Object -Last 1
$requiredCaptures = @(
    '01-cooling-start.png',
    '02-relay-chapter.png',
    '03-storm-chapter.png',
    '03b-storm-calibration-console.png',
    '04-demo-completed.png'
)
$missingCaptures = @($requiredCaptures | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $captureDirectory $_) -PathType Leaf)
    })
$passed = $process.ExitCode -eq 0 -and $null -ne $completion -and $missingCaptures.Count -eq 0
$result = @(
    "status=$(if ($passed) { 'PASS' } else { 'FAIL' })",
    "playerExitCode=$($process.ExitCode)",
    "completionLogFound=$($null -ne $completion)",
    "missingCaptures=$($missingCaptures -join ',')",
    "playerLog=$playerLog",
    "captureDirectory=$captureDirectory"
)
[System.IO.File]::WriteAllLines($resultPath, $result, [System.Text.UTF8Encoding]::new($false))
$result
"Build completion result written to $resultPath"

if (-not $passed) {
    exit 2
}
