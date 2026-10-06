# Restores the WDK NuGet packages and builds the VAC driver package into src/Vac/bin/<platform>/<configuration>/Micser.Vac.
# -RestoreOnly only restores the packages.
param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Debug",
    [ValidateSet("x64", "ARM64")][string[]]$Platform = @("x64"),
    [string]$Version = "1.0.0.0",
    [switch]$RestoreOnly
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path $PSScriptRoot -Parent
$vacDir = Join-Path $root "src/Vac"

$nuget = (Get-Command nuget.exe -ErrorAction SilentlyContinue)?.Source
if (-not $nuget) {
    $nuget = Join-Path $root "artifacts/tools/nuget.exe"
    if (-not (Test-Path $nuget)) {
        New-Item -ItemType Directory -Force (Split-Path $nuget) | Out-Null
        Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile $nuget
    }
}
& $nuget restore (Join-Path $vacDir "packages.config") -PackagesDirectory (Join-Path $vacDir "packages") -NonInteractive
if ($RestoreOnly) {
    return
}

# the WDK NuGet packages only contain x64 and ARM64 host tools, so the build needs the 64-bit MSBuild
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\amd64\MSBuild.exe" | Select-Object -First 1
if (-not $msbuild) { throw "64-bit MSBuild not found" }

$infVerif = Join-Path $vacDir "packages/Microsoft.Windows.WDK.x64.10.0.28000.2526/c/tools/10.0.28000.0/x64/infverif.exe"

foreach ($p in $Platform) {
    & $msbuild (Join-Path $vacDir "Micser.Vac.vcxproj") -nologo -m -v:m `
        -p:Configuration=$Configuration -p:Platform=$p -p:VacDriverVersion=$Version

    # /w: declarative (DCH) driver rules
    & $infVerif /w (Join-Path $vacDir "bin/$p/$Configuration/Micser.Vac/Micser.Vac.inf")
}
