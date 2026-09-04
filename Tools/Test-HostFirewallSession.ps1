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
$output = Join-Path $projectRoot ('Logs\FirewallSession-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
if (-not ('FunGame.Networking.WindowsHostFirewall' -as [type])) {
    Add-Type -Path (Join-Path $projectRoot 'Assets\Game\Runtime\Networking\WindowsHostFirewall.cs')
}

# This test intentionally requests UAC. The game stays unelevated; only its scoped helper is elevated.
$player = Start-Process -FilePath $executable -ArgumentList @('-batchmode', '-nographics',
    '-logFile', ('"' + (Join-Path $output 'Player.log') + '"')) -WindowStyle Hidden -PassThru
$helper = $null
$ruleName = $null
try {
    $ticks = $player.StartTime.ToUniversalTime().Ticks
    $ruleName = [FunGame.Networking.WindowsHostFirewall]::RuleName($executable, $Port, $player.Id, $ticks)
    $script = [FunGame.Networking.WindowsHostFirewall]::BuildScript($executable, $Port, $player.Id, $ticks, $true)
    $tokens = $null
    $parseErrors = $null
    $null = [System.Management.Automation.Language.Parser]::ParseInput($script, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count -ne 0) { throw "Invalid helper syntax: $parseErrors" }
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
    $helper = Start-Process -FilePath "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" `
        -ArgumentList @('-NoLogo', '-NoProfile', '-NonInteractive', '-EncodedCommand', $encoded) `
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
    $check = [FunGame.Networking.WindowsHostFirewall]::BuildScript($executable, $Port, $player.Id, $ticks, $false)
    $checkEncoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($check))
    & "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -EncodedCommand $checkEncoded
    if ($LASTEXITCODE -ne 0) { throw "Production rule check failed: $LASTEXITCODE" }

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
