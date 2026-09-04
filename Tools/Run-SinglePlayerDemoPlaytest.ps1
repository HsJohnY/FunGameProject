param(
    [string]$ExecutablePath,
    [int]$MinimumMinutes = 25,
    [int]$MaximumMinutes = 35
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
    $ExecutablePath = Join-Path $projectRoot 'Builds\SinglePlayerDemo-Windows\FunGame-SinglePlayerDemo.exe'
}

if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
    throw "Single-player demo executable not found at '$ExecutablePath'. Run Build-SinglePlayerDemo.ps1 first."
}

if ($MinimumMinutes -le 0 -or $MaximumMinutes -lt $MinimumMinutes) {
    throw 'The timing window must use positive minutes and MaximumMinutes must not be lower than MinimumMinutes.'
}

$resolvedExecutable = (Resolve-Path -LiteralPath $ExecutablePath).Path
$workingDirectory = Split-Path -Parent $resolvedExecutable
$logDirectory = Join-Path $projectRoot 'Logs'
$resultDirectory = Join-Path $projectRoot 'TestResults'
New-Item -ItemType Directory -Force -Path $logDirectory, $resultDirectory | Out-Null
$playerLog = Join-Path $logDirectory 'SinglePlayerDemo-ManualPlaytest.log'
$resultPath = Join-Path $resultDirectory 'SinglePlayerDemo-ManualTiming.txt'

$wallClock = [System.Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath $resolvedExecutable `
    -ArgumentList @('-screen-fullscreen', '0', '-logFile', $playerLog) `
    -WorkingDirectory $workingDirectory `
    -PassThru
$process.WaitForExit()
$wallClock.Stop()

$completionMatch = $null
if (Test-Path -LiteralPath $playerLog -PathType Leaf) {
    $completionMatch = Select-String -LiteralPath $playerLog `
        -Pattern '\[Demo\] result=completed duration=(?<duration>\d+:\d{2})' `
        | Select-Object -Last 1
}

$completed = $null -ne $completionMatch
$gameplaySeconds = $null
$gameplayDuration = 'not-completed'
$coolingDuration = 'unavailable'
$relayDuration = 'unavailable'
$stormDuration = 'unavailable'
if ($completed -and $completionMatch.Matches.Count -gt 0) {
    $durationText = $completionMatch.Matches[0].Groups['duration'].Value
    $parts = $durationText.Split(':')
    $gameplaySeconds = [int]$parts[0] * 60 + [int]$parts[1]
    $gameplayDuration = $durationText

    $breakdownMatch = Select-String -LiteralPath $playerLog `
        -Pattern '\[Demo\] result=completed .*cooling=(?<cooling>\d+:\d{2}) relay=(?<relay>\d+:\d{2}) storm=(?<storm>\d+:\d{2})' `
        | Select-Object -Last 1
    if ($null -ne $breakdownMatch -and $breakdownMatch.Matches.Count -gt 0) {
        $coolingDuration = $breakdownMatch.Matches[0].Groups['cooling'].Value
        $relayDuration = $breakdownMatch.Matches[0].Groups['relay'].Value
        $stormDuration = $breakdownMatch.Matches[0].Groups['storm'].Value
    }
}

$minimumSeconds = $MinimumMinutes * 60
$maximumSeconds = $MaximumMinutes * 60
$withinTimingWindow = $completed -and $gameplaySeconds -ge $minimumSeconds -and $gameplaySeconds -le $maximumSeconds
$status = if (-not $completed) { 'NOT_COMPLETED' } elseif ($withinTimingWindow) { 'PASS' } else { 'OUTSIDE_TARGET' }
$result = @(
    "status=$status",
    "completed=$completed",
    "gameplayDuration=$gameplayDuration",
    "coolingChapterDuration=$coolingDuration",
    "relayChapterDuration=$relayDuration",
    "stormChapterDuration=$stormDuration",
    "wallClockDuration=$($wallClock.Elapsed.ToString('hh\:mm\:ss'))",
    "targetMinutes=$MinimumMinutes-$MaximumMinutes",
    "playerExitCode=$($process.ExitCode)",
    "playerLog=$playerLog"
)
[System.IO.File]::WriteAllLines($resultPath, $result, [System.Text.UTF8Encoding]::new($false))
$result
"Playtest result written to $resultPath"

if (-not $completed) {
    exit 2
}

if (-not $withinTimingWindow) {
    exit 3
}
