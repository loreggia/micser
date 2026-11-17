# Restores the WDK NuGet packages and builds the cable driver package into
# src/CableDriver/bin/<platform>/<configuration>/Micser.CableDriver. -RestoreOnly only restores the packages.
param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Debug",
    [ValidateSet("x64", "ARM64")][string[]]$Platform = @("x64"),
    [string]$Version = "1.0.0.0",
    [switch]$RestoreOnly
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path $PSScriptRoot -Parent
$driverDir = Join-Path $root "src/CableDriver"

$nuget = (Get-Command nuget.exe -ErrorAction SilentlyContinue)?.Source
if (-not $nuget) {
    $nuget = Join-Path $root "artifacts/tools/nuget.exe"
    if (-not (Test-Path $nuget)) {
        New-Item -ItemType Directory -Force (Split-Path $nuget) | Out-Null
        Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile $nuget
    }
}
& $nuget restore (Join-Path $driverDir "packages.config") -PackagesDirectory (Join-Path $driverDir "packages") -NonInteractive
if ($RestoreOnly) {
    return
}

# the WDK NuGet packages only contain x64 and ARM64 host tools, so the build needs the 64-bit MSBuild
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\amd64\MSBuild.exe" | Select-Object -First 1
if (-not $msbuild) { throw "64-bit MSBuild not found" }

$wdkVersion = (([xml](Get-Content (Join-Path $driverDir "packages.config"))).packages.package | Where-Object id -eq "Microsoft.Windows.WDK.x64").version
$infVerif = (Resolve-Path (Join-Path $driverDir "packages/Microsoft.Windows.WDK.x64.$wdkVersion/c/tools/10.*/x64/infverif.exe")).Path

foreach ($p in $Platform) {
    & $msbuild (Join-Path $driverDir "Micser.CableDriver.vcxproj") -nologo -m -v:m `
        -p:Configuration=$Configuration -p:Platform=$p -p:CableDriverVersion=$Version

    # /w: declarative (DCH) driver rules
    & $infVerif /w (Join-Path $driverDir "bin/$p/$Configuration/Micser.CableDriver/MicserCable.inf")
}
