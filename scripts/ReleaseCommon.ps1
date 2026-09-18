Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ProjectRoot = Split-Path -Parent $PSScriptRoot
$script:ReleaseConfigPath = Join-Path $env:LOCALAPPDATA 'WRBStudio\AegisProtocol\release-secrets.xml'

function ConvertFrom-SecureStringPlainText {
    param([Parameter(Mandatory)][Security.SecureString]$Value)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Get-ReleaseConfig {
    if (-not (Test-Path -LiteralPath $script:ReleaseConfigPath)) {
        throw "Release configuration is missing. Run scripts\Set-ReleaseSecrets.ps1 once."
    }

    return Import-Clixml -LiteralPath $script:ReleaseConfigPath
}

function Resolve-UnityEditor {
    param([string]$UnityPath)

    $versionFile = Join-Path $script:ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    $unityVersion = [regex]::Match((Get-Content -LiteralPath $versionFile -Raw), 'm_EditorVersion: (.+)').Groups[1].Value.Trim()
    $candidate = if ($UnityPath) { $UnityPath } else { $env:UNITY_EDITOR_PATH }
    if (-not $candidate) {
        $installRoots = @("$env:ProgramFiles\Unity\Hub\Editor")
        $secondaryInstallPath = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'
        if (Test-Path -LiteralPath $secondaryInstallPath) {
            $installRoots += Get-Content -LiteralPath $secondaryInstallPath -Raw | ConvertFrom-Json
        }

        $candidate = $installRoots |
            ForEach-Object { Join-Path $_ "$unityVersion\Editor\Unity.exe" } |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
            Select-Object -First 1
    }
    if (-not $candidate) {
        throw "Unity $unityVersion was not found automatically. Run the script with -UnityPath 'C:\path\to\Unity.exe' or set UNITY_EDITOR_PATH."
    }
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "Unity executable not found: $candidate"
    }

    return (Resolve-Path -LiteralPath $candidate).Path
}

function Get-ProjectVersion {
    $settings = Get-Content -LiteralPath (Join-Path $script:ProjectRoot 'ProjectSettings\ProjectSettings.asset') -Raw
    $version = [regex]::Match($settings, '(?m)^  bundleVersion: (.+)$').Groups[1].Value.Trim()
    $versionCode = [regex]::Match($settings, '(?m)^  AndroidBundleVersionCode: (\d+)$').Groups[1].Value
    if (-not $version -or -not $versionCode) {
        throw 'Android version information could not be read from ProjectSettings.asset.'
    }

    return [PSCustomObject]@{ Version = $version; VersionCode = [int]$versionCode }
}

function Invoke-AegisAndroidBuild {
    param(
        [Parameter(Mandatory)][ValidateSet('apk', 'aab')][string]$Format,
        [string]$UnityPath,
        [int]$VersionCode
    )

    $config = Get-ReleaseConfig
    foreach ($path in @($config.KeystorePath, $config.ServiceAccountJsonPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required release file is missing: $path"
        }
    }

    $unity = Resolve-UnityEditor -UnityPath $UnityPath
    $extension = if ($Format -eq 'apk') { 'apk' } else { 'aab' }
    $artifactDirectory = Join-Path $script:ProjectRoot 'Builds\Android'
    $outputPath = Join-Path $artifactDirectory "AegisProtocol.$extension"
    $logPath = Join-Path $artifactDirectory "unity-$Format.log"
    $unityLockFile = Join-Path $script:ProjectRoot 'Temp\UnityLockfile'
    if (Test-Path -LiteralPath $unityLockFile) {
        throw 'Aegis Protocol is currently open in Unity. Close that Unity Editor window before starting an automated build.'
    }
    New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null

    $previousEnvironment = @{}
    $environmentValues = @{
        AEGIS_BUILD_OUTPUT = $outputPath
        AEGIS_BUILD_FORMAT = $Format
        AEGIS_KEYSTORE_PATH = $config.KeystorePath
        AEGIS_KEYSTORE_PASSWORD = ConvertFrom-SecureStringPlainText $config.KeystorePassword
        AEGIS_KEY_ALIAS = $config.KeyAlias
        AEGIS_KEY_ALIAS_PASSWORD = ConvertFrom-SecureStringPlainText $config.KeyAliasPassword
        AEGIS_VERSION_CODE = if ($VersionCode) { $VersionCode.ToString() } else { '' }
    }

    try {
        foreach ($name in $environmentValues.Keys) {
            $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
            [Environment]::SetEnvironmentVariable($name, $environmentValues[$name], 'Process')
        }

        $process = Start-Process -FilePath $unity -ArgumentList @(
            '-batchmode', '-nographics', '-quit',
            '-projectPath', $script:ProjectRoot,
            '-executeMethod', 'WRBStudio.AegisProtocol.Editor.AegisAndroidBuild.BuildFromEnvironment',
            '-logFile', $logPath
        ) -Wait -PassThru
        if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $outputPath)) {
            throw "Unity build failed. See $logPath"
        }
    }
    finally {
        foreach ($name in $environmentValues.Keys) {
            [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
        }
    }

    return [PSCustomObject]@{ ArtifactPath = $outputPath; Version = Get-ProjectVersion }
}
