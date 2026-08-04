<#
.SYNOPSIS
    Builds the Enigma.HardCopy release artifacts.

.DESCRIPTION
    Produces one self-contained, single-file build of the desktop app per runtime identifier, each
    zipped under artifacts/.

    Self-contained means the .NET runtime travels inside the executable — the machine that runs the
    artifact needs nothing installed. That is the point for a tool whose whole purpose is recovering
    data years from now: the fewer moving parts between a printed sheet and the file it holds, the
    better.

    Requires the .NET SDK pinned in global.json. The PowerShell twin of scripts/publish.sh; either
    script produces identical artifacts, so a release can be cut from Windows or from Linux.

.PARAMETER Rid
    The runtime identifiers to build. Defaults to every supported RID.

.EXAMPLE
    ./scripts/publish.ps1
    Builds every RID.

.EXAMPLE
    ./scripts/publish.ps1 win-x64
    Builds just the Windows artifact.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]] $Rid = @('win-x64', 'linux-x64')
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/Enigma.HardCopy.Desktop/Enigma.HardCopy.Desktop.csproj'
$artifacts = Join-Path $root 'artifacts'

# The version lives in exactly one place — Directory.Build.props — so the artifact names cannot drift
# from what the executable reports about itself.
$props = [xml](Get-Content -Path (Join-Path $root 'Directory.Build.props') -Raw)
$version = $props.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'No <Version> found in Directory.Build.props.'
}

Write-Host "Enigma.HardCopy $version"
Write-Host ''

foreach ($currentRid in $Rid) {
    $name = "Enigma.HardCopy-$version-$currentRid"
    $stage = Join-Path $artifacts "publish/$name"
    $archive = Join-Path $artifacts "$name.zip"

    Write-Host "==> $currentRid"
    foreach ($stale in @($stage, $archive)) {
        if (Test-Path -Path $stale) { Remove-Item -Path $stale -Recurse -Force }
    }

    # GenerateDocumentationFile is off for the artifact: the XML doc file is IntelliSense material for
    # someone referencing Enigma.HardCopy.Core as a library, and means nothing to someone running the
    # app. The managed .pdb files are kept on purpose — they are what turns a crash report into a file
    # and a line number. (The far larger *native* symbol files are dropped by the csproj; see the
    # TrimNativeSymbolsFromPublish target.)
    dotnet publish $project `
        --configuration Release `
        --runtime $currentRid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:GenerateDocumentationFile=false `
        --output $stage
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $currentRid." }

    # Shipped alongside the binary so an unpacked artifact is self-explanatory years later, when
    # whoever holds the printout may no longer have the repository.
    Copy-Item -Path (Join-Path $root 'README.md'), (Join-Path $root 'LICENSE.md') -Destination $stage

    # Compressed from the staging directory itself so the archive expands into its own folder rather
    # than scattering a 100 MB executable across the user's Downloads folder.
    #
    # Compress-Archive does not carry the Unix executable bit, so a linux-x64 artifact built on
    # Windows needs `chmod +x` after unzipping — the README says so. scripts/publish.sh, which uses
    # Info-ZIP, preserves it.
    Compress-Archive -Path $stage -DestinationPath $archive -CompressionLevel Optimal

    $size = '{0:N0} MB' -f ((Get-Item -Path $archive).Length / 1MB)
    Write-Host "    $archive ($size)"
    Write-Host ''
}

Write-Host "Done. Artifacts in $artifacts."
