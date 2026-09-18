[CmdletBinding()]
param(
    [string]$KeystorePath = 'G:\Meine Ablage\GameDev\WRB.Studio\Keystore\user.keystore',
    [string]$KeyAlias = 'aegis protocol',
    [string]$ServiceAccountJsonPath = 'G:\Meine Ablage\GameDev\WRB.Studio\Secrets\wrb-studio-play-releases-36ea7f9dfc77.json'
)

. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')

if (-not (Test-Path -LiteralPath $KeystorePath -PathType Leaf)) { throw "Keystore not found: $KeystorePath" }
if (-not (Test-Path -LiteralPath $ServiceAccountJsonPath -PathType Leaf)) { throw "Service-account JSON not found: $ServiceAccountJsonPath" }

$keystorePassword = Read-Host 'Keystore password' -AsSecureString
$keyAliasPassword = Read-Host 'Key-alias password' -AsSecureString
$configDirectory = Split-Path -Parent $script:ReleaseConfigPath
New-Item -ItemType Directory -Force -Path $configDirectory | Out-Null

[PSCustomObject]@{
    KeystorePath = $KeystorePath
    KeyAlias = $KeyAlias
    KeystorePassword = $keystorePassword
    KeyAliasPassword = $keyAliasPassword
    ServiceAccountJsonPath = $ServiceAccountJsonPath
} | Export-Clixml -LiteralPath $script:ReleaseConfigPath -Force

Write-Host "Release configuration saved for this Windows account: $script:ReleaseConfigPath"
