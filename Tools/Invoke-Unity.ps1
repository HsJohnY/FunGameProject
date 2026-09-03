param(
    [Parameter(Mandatory = $true)]
    [string[]]$UnityArguments,

    [string]$UnityEditorPath = 'D:\Unity\Hub\Editor\6000.0.82f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) {
    throw "Unity Editor not found at '$UnityEditorPath'. Pass -UnityEditorPath explicitly."
}

$commonArguments = @(
    '-batchmode',
    '-nographics',
    '-projectPath', $projectRoot
)

$process = Start-Process -FilePath $UnityEditorPath `
    -ArgumentList ($commonArguments + $UnityArguments) `
    -WorkingDirectory $projectRoot `
    -WindowStyle Hidden `
    -Wait `
    -PassThru

if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). Review the requested log file."
}
