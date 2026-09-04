param([int]$Port = 17844, [int]$TimeoutSeconds = 110)

chcp 65001 | Out-Null
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $projectRoot 'Builds\M4-Coop-Windows\FunGame-M4-Coop.exe'
$output = Join-Path $projectRoot ('Logs\ModuleNetwork-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$processes = @{}
try {
    foreach ($role in @('host', 'client')) {
        $arguments = @('-batchmode', '-nographics', '-logFile', ('"' + (Join-Path $output ($role + '.log')) + '"'),
            ('--module-network-role=' + $role), ('"--module-network-output=' + $output + '"'), ('--module-network-port=' + $Port))
        $processes[$role] = Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory (Split-Path -Parent $executable) -WindowStyle Hidden -PassThru
    }
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (@($processes.Values | Where-Object { -not $_.HasExited }).Count -gt 0 -and [DateTime]::UtcNow -lt $deadline) {
        foreach ($role in @('host', 'client')) {
            if ($processes[$role].HasExited -and $processes[$role].ExitCode -ne 0) {
                throw "Network verification failed: $role. Logs: $output"
            }
        }
        Start-Sleep -Milliseconds 500
    }
    foreach ($role in @('host', 'client')) {
        if (-not $processes[$role].HasExited) { throw "Network verification timed out: $role. Logs: $output" }
        if ($processes[$role].ExitCode -ne 0) { throw "Network verification failed: $role. Logs: $output" }
        $text = Get-Content -LiteralPath (Join-Path $output ($role + '.log')) -Raw -Encoding UTF8
        if (-not $text.Contains('[ModuleNetworkCheck] PASS: ' + $role)) { throw "Missing PASS marker: $role. Logs: $output" }
    }
    Write-Host "PASS: late join, stable enemy IDs, incident state, disconnect/reconnect. Logs: $output"
} finally {
    foreach ($process in $processes.Values) { if (-not $process.HasExited) { $process.Kill() } }
}
