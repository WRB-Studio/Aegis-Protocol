[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(1, [int]::MaxValue)][int]$VersionCode,
    [string]$UnityPath,
    [switch]$ConfirmProduction
)

. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')

if (-not $ConfirmProduction) {
    throw 'Production upload blocked. Re-run with -ConfirmProduction only when this exact version is ready for Google Play review/release.'
}

$fastlanePath = (Get-Command fastlane, fastlane.bat -ErrorAction SilentlyContinue | Select-Object -First 1).Source
if (-not $fastlanePath) {
    $fastlanePath = Get-ChildItem -Path 'C:\Ruby*\bin\fastlane.bat' -File -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $fastlanePath) {
    throw 'fastlane is not installed or not on PATH. Install it first, then restart PowerShell.'
}

$build = Invoke-AegisAndroidBuild -Format aab -UnityPath $UnityPath -VersionCode $VersionCode
$config = Get-ReleaseConfig
& $fastlanePath supply --aab $build.ArtifactPath --track production --release_status completed --json_key $config.ServiceAccountJsonPath --package_name 'com.WRBStudio.AegisProtocol'
if ($LASTEXITCODE -ne 0) {
    throw "Google Play upload failed with exit code $LASTEXITCODE."
}

Write-Host "AAB uploaded to the production track: $($build.ArtifactPath)"
