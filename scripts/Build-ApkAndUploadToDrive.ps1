[CmdletBinding()]
param(
    [string]$UnityPath,
    [int]$VersionCode,
    [string]$DriveDirectory = 'G:\Meine Ablage\GameDev\Aegis Protocol'
)

. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')
if (-not (Test-Path -LiteralPath $DriveDirectory -PathType Container)) {
    throw "Google Drive folder not found: $DriveDirectory"
}

$build = Invoke-AegisAndroidBuild -Format apk -UnityPath $UnityPath -VersionCode $VersionCode
$actualVersionCode = if ($VersionCode) { $VersionCode } else { $build.Version.VersionCode }
$fileName = "AegisProtocol-$($build.Version.Version)-$actualVersionCode.apk"
$destination = Join-Path $DriveDirectory $fileName
Copy-Item -LiteralPath $build.ArtifactPath -Destination $destination -Force
Write-Host "APK copied to Google Drive: $destination"
