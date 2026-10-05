# Builds the web UI, publishes engine and shell into one folder and packs a Velopack release into artifacts/releases.
# With -DriverPackage (a folder with the signed driver's INF, SYS and CAT), the release also carries the virtual audio cable driver and
# DriverUtility in its driver folder; without it, the app hides the driver settings.
# The version's section in CHANGELOG.md ("## X.Y.Z") becomes the release notes: in the package (vpk upload github uses them as the GitHub
# release body, and the shell shows them for a downloaded update) and as ReleaseNotes.md in the app (shown for the installed version).
# Without a section, the release has no notes, unless -RequireReleaseNotes makes that an error.
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$Runtime = "win-x64",
    [string]$Channel = "win",
    [string]$DriverPackage,
    [switch]$RequireReleaseNotes
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path $PSScriptRoot -Parent
$publishDir = Join-Path $root "artifacts/publish"
$releasesDir = Join-Path $root "artifacts/releases"

# the lines after "## <Version>" (optionally "## [<Version>]", followed by e.g. a date) up to the next "## " heading
function Get-ReleaseNotes {
    $lines = Get-Content (Join-Path $root "CHANGELOG.md")
    $start = 0..($lines.Count - 1) | Where-Object { $lines[$_] -match "^## \[?$([regex]::Escape($Version))\]?(\s|$)" } | Select-Object -First 1
    if ($null -eq $start) { return $null }

    $notes = foreach ($line in $lines | Select-Object -Skip ($start + 1)) {
        if ($line -match "^## ") { break }
        $line
    }
    $text = ($notes -join "`n").Trim()
    if ($text) { $text } else { $null }
}

Push-Location $root
try {
    $releaseNotes = Get-ReleaseNotes
    if (-not $releaseNotes) {
        if ($RequireReleaseNotes) { throw "CHANGELOG.md has no section for $Version." }
        Write-Warning "CHANGELOG.md has no section for $Version; the release has no release notes."
    }

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

    $notesArgs = @()
    if ($releaseNotes) {
        $notesPath = Join-Path $publishDir "ReleaseNotes.md"
        Set-Content $notesPath $releaseNotes -Encoding utf8NoBOM
        $notesArgs = "--releaseNotes", $notesPath
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
        --outputDir $releasesDir `
        @notesArgs
}
finally {
    Pop-Location
}
