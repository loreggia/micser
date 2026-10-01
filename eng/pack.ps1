# Builds the web UI, publishes engine and shell into one folder and packs a Velopack release into artifacts/releases.
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$Runtime = "win-x64",
    [string]$Channel = "win"
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
