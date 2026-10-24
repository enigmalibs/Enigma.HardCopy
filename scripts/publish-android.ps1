<#
.SYNOPSIS
    Builds the release APK of HardCopy Recover, the Android recovery app.

.DESCRIPTION
    Produces artifacts/Enigma.HardCopy.Recover-<version>.apk, signed with the project's release key.

    The signing key is the app's identity on every phone that installs it: Android only accepts an update
    signed by the same certificate as the app already installed. So every release is signed with the one
    release key, created once by the maintainer and kept outside the repository (docs/android.md, "Release
    APK") — never with the per-machine debug key a plain `dotnet publish` falls back to. Without the key
    this script refuses to build, so a debug-signed APK cannot be shipped by mistake.

    Requires the .NET SDK pinned in global.json, the .NET android workload, an Android SDK and a JDK. The
    PowerShell twin of scripts/publish-android.sh; either script produces the same APK, so a release can be
    cut from Windows or from Linux.

    Environment:
      HARDCOPY_ANDROID_KEYSTORE           the release keystore file (required)
      HARDCOPY_ANDROID_KEYSTORE_PASSWORD  its password, which is also the key's (required)
      HARDCOPY_ANDROID_KEY_ALIAS          the key's alias in the keystore (default: hardcopy-recover)

.EXAMPLE
    $env:HARDCOPY_ANDROID_KEYSTORE = "$HOME\keys\hardcopy-recover.keystore"
    $env:HARDCOPY_ANDROID_KEYSTORE_PASSWORD = Read-Host -MaskInput 'Keystore password'
    ./scripts/publish-android.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$defaultKeyAlias = 'hardcopy-recover'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/Enigma.HardCopy.Recover.Android/Enigma.HardCopy.Recover.Android.csproj'
$artifacts = Join-Path $root 'artifacts'

# The version lives in exactly one place — Directory.Build.props — so the APK's name cannot drift from the
# version it reports to Android (its versionName, and the versionCode derived from it in the csproj).
$props = [xml](Get-Content -Path (Join-Path $root 'Directory.Build.props') -Raw)
$version = $props.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'No <Version> found in Directory.Build.props.'
}

$keystore = $env:HARDCOPY_ANDROID_KEYSTORE
$keyAlias = if ([string]::IsNullOrWhiteSpace($env:HARDCOPY_ANDROID_KEY_ALIAS)) { $defaultKeyAlias } else { $env:HARDCOPY_ANDROID_KEY_ALIAS }

if ([string]::IsNullOrWhiteSpace($keystore) -or [string]::IsNullOrWhiteSpace($env:HARDCOPY_ANDROID_KEYSTORE_PASSWORD)) {
    throw (@(
        'No release signing key is configured, so nothing was built.'
        'Set HARDCOPY_ANDROID_KEYSTORE to the release keystore and HARDCOPY_ANDROID_KEYSTORE_PASSWORD to its'
        'password (and HARDCOPY_ANDROID_KEY_ALIAS if the key''s alias is not "hardcopy-recover"). Creating the key'
        'is a one-time step, described in docs/android.md under "Release APK".'
    ) -join [Environment]::NewLine)
}

if (-not (Test-Path -Path $keystore -PathType Leaf)) {
    throw "HARDCOPY_ANDROID_KEYSTORE is '$keystore', which is not a file."
}

# MSBuild would resolve a relative path against the project directory, not against the caller's.
$keystore = (Resolve-Path -Path $keystore).Path

$name = "Enigma.HardCopy.Recover-$version"
$stage = Join-Path $artifacts "publish/$name"
$apk = Join-Path $artifacts "$name.apk"

Write-Host "HardCopy Recover $version"
Write-Host ''

foreach ($stale in @($stage, $apk)) {
    if (Test-Path -Path $stale) { Remove-Item -Path $stale -Recurse -Force }
}

# The passwords go in as env: references, which the Android build resolves itself — so they appear neither
# on this command line, nor in the process list, nor in the build log.
dotnet publish $project `
    --configuration Release `
    --framework net10.0-android `
    -p:AndroidPackageFormat=apk `
    -p:AndroidKeyStore=true `
    "-p:AndroidSigningKeyStore=$keystore" `
    "-p:AndroidSigningKeyAlias=$keyAlias" `
    -p:AndroidSigningStorePass=env:HARDCOPY_ANDROID_KEYSTORE_PASSWORD `
    -p:AndroidSigningKeyPass=env:HARDCOPY_ANDROID_KEYSTORE_PASSWORD `
    --output $stage
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

# The build leaves the unsigned package beside the signed one; only the signed one can be installed.
$signed = @(Get-ChildItem -Path $stage -Filter '*-Signed.apk' -File)
if ($signed.Count -ne 1) {
    throw "Expected exactly one *-Signed.apk in $stage."
}

Copy-Item -Path $signed[0].FullName -Destination $apk

$size = '{0:N0} MB' -f ((Get-Item -Path $apk).Length / 1MB)
Write-Host ''
Write-Host "    $apk ($size)"
Write-Host ''
Write-Host "Done. Verify the signature before publishing: apksigner verify --print-certs `"$apk`""
