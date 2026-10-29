#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes Enigma HardCopy (Enigma.HardCopy) for Windows x64.

.DESCRIPTION
    Runs, from wherever it is started:

        dotnet publish .\src\Enigma.HardCopy.Desktop\ -c Release -r win-x64 -p:PublishReadyToRun=true

    A Release build for win-x64, compiled ReadyToRun so it starts faster, which runs on an installed
    .NET 10 runtime. The result lands in

        src\Enigma.HardCopy.Desktop\bin\Release\net10.0\win-x64\publish\

    and dotnet's last line names that folder. Start Enigma.HardCopy.Desktop.exe in it.

.EXAMPLE
    .\packaging\windows\publish.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File packaging\windows\publish.ps1

    Windows PowerShell's default policy refuses to run local scripts; this runs this one without
    changing the policy.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolved from this script rather than the current directory, so it runs from anywhere.
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..\src\Enigma.HardCopy.Desktop')).Path

dotnet publish $project -c Release -r win-x64 -p:PublishReadyToRun=true

# A native command's failure is not a PowerShell error: pass its exit code on, so a failed publish
# fails the script.
exit $LASTEXITCODE
