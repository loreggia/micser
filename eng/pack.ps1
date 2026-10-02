# Builds the web UI, publishes engine and shell into one folder and packs a Velopack release into artifacts/releases.
# With -DriverPackage (a folder with the signed driver's INF, SYS and CAT), the release also carries the virtual audio cable driver and
# DriverUtility in its driver folder; without it, the app hides the driver settings.
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$Runtime = "win-x64",
    [string]$Channel = "win",
    [string]$DriverPackage
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path $PSScriptRoot -Parent
$publishDir = Join-Path $root "artifacts/publish"
$releasesDir = Join-Path $root "artifacts/releases"

Push-Location $root
try {
    npm run build

    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

    # both apps are self-contained with the same runtime, so they share its files in one folder
    foreach ($project in "src/Engine/Micser.Engine.csproj", "src/Shell/Micser.Shell.csproj") {
        dotnet publish $project -c Release -r $Runtime --self-contained -p:Version=$Version -o $publishDir
    }

    if ($DriverPackage) {
        $driverDir = Join-Path $publishDir "driver"
        New-Item -ItemType Directory -Force $driverDir | Out-Null
        Get-ChildItem $DriverPackage -File | Where-Object Extension -in ".inf", ".sys", ".cat" | Copy-Item -Destination $driverDir

        # Native AOT needs the MSVC linker, which it finds through vswhere
        $env:PATH = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer;$env:PATH"
        $utilityDir = Join-Path $root "artifacts/driverutility"
        dotnet publish src/DriverUtility/Micser.DriverUtility.csproj -c Release -r $Runtime -p:Version=$Version -o $utilityDir
        Copy-Item (Join-Path $utilityDir "Micser.DriverUtility.exe") $driverDir
    }

    dotnet tool restore
    dotnet vpk pack `
        --packId Micser `
        --packVersion $Version `
        --packTitle Micser `
        --packAuthors "Lucas Loreggia" `
        --packDir $publishDir `
        --mainExe Micser.Shell.exe `
        --runtime $Runtime `
        --icon gfx/logo.ico `
        --channel $Channel `
        --outputDir $releasesDir
}
finally {
    Pop-Location
}
