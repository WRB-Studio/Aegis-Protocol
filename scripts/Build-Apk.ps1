[CmdletBinding()]
param(
    [string]$UnityPath,
    [int]$VersionCode
)

. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')
Invoke-AegisAndroidBuild -Format apk -UnityPath $UnityPath -VersionCode $VersionCode
