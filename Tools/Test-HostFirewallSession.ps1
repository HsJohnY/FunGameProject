#requires -Version 7.0
param(
    [int]$Port = 4848,
    [int]$TimeoutSeconds = 60
)

chcp 65001 | Out-Null
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $projectRoot 'Builds\M4-Coop-Windows\FunGame-M4-Coop.exe'
$helperPath = Join-Path (Split-Path -Parent $executable) 'FunGame.Firewall.exe'
$output = Join-Path $projectRoot ('Logs\FirewallSession-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
if (-not ('FunGame.Networking.HostFirewallIdentity' -as [type])) {
    Add-Type -Path (Join-Path $projectRoot 'Assets\Game\Runtime\Networking\HostFirewallIdentity.cs')
}
if (-not (Test-Path -LiteralPath $helperPath)) { throw 'Build the bundled firewall helper first.' }

# This test intentionally requests UAC. The game stays unelevated; only its scoped helper is elevated.
$player = Start-Process -FilePath $executable -ArgumentList @('-batchmode', '-nographics',
    '-logFile', ('"' + (Join-Path $output 'Player.log') + '"')) -WindowStyle Hidden -PassThru
$helper = $null
$ruleName = $null
try {
    $ticks = $player.StartTime.ToUniversalTime().Ticks
    $ruleName = [FunGame.Networking.HostFirewallIdentity]::RuleName($executable, $Port, $player.Id, $ticks)
    $arguments = [FunGame.Networking.HostFirewallIdentity]::Arguments($true, $Port, $player.Id, $ticks)
    $helper = Start-Process -FilePath $helperPath -ArgumentList $arguments `
        -Verb RunAs -WindowStyle Hidden -PassThru

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $rule = $null
    do {
        $rule = Get-NetFirewallRule -PolicyStore ActiveStore -Name $ruleName -ErrorAction SilentlyContinue
        if ($null -ne $rule) { break }
        if ($helper.HasExited) { throw "Firewall helper exited before creating its rule ($($helper.ExitCode))." }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    if ($null -eq $rule) { throw 'Session rule did not become ready.' }
    if ($rule.PolicyStoreSourceType -ne 'Dynamic') { throw 'Rule is not dynamic.' }
    if (Get-NetFirewallRule -PolicyStore PersistentStore -Name $ruleName -ErrorAction SilentlyContinue) {
        throw 'Session rule unexpectedly persisted.'
    }
    $rule | Select-Object Name,PolicyStoreSourceType,Profile,Action,Direction |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'Rule.json') -Encoding utf8
    $checkArguments = [FunGame.Networking.HostFirewallIdentity]::Arguments($false, $Port, $player.Id, $ticks)
    $checkError = Join-Path $output 'CheckError.log'
    $check = Start-Process -FilePath $helperPath -ArgumentList $checkArguments -WindowStyle Hidden -PassThru -Wait -RedirectStandardError $checkError
    if ($check.ExitCode -ne 0) { throw "Production rule check failed: $($check.ExitCode). $(Get-Content -Raw $checkError)" }

    # Force termination also covers the case where Unity cannot run its own exit callbacks.
    $player.Kill()
    if (-not $helper.WaitForExit(15000)) { throw 'Helper did not exit after the player ended.' }
    if (Get-NetFirewallRule -PolicyStore ActiveStore -Name $ruleName -ErrorAction SilentlyContinue) {
        throw 'Rule was not removed after player termination.'
    }
    "PASS: dynamic rule, exact application/port, production readiness check, cleanup after forced player exit. $output" |
        Tee-Object -FilePath (Join-Path $output 'Result.txt')
} finally {
    if (-not $player.HasExited) { $player.Kill() }
    if ($null -ne $helper -and -not $helper.HasExited) { $null = $helper.WaitForExit(15000) }
}
