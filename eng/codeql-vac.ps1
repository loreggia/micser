# Runs Microsoft's CodeQL driver checks (the WHCP "mustfix" and "recommended" suites) on the VAC driver and fails on any finding in
# its own code. The SARIF file is written to artifacts/codeql/vac.sarif.
param(
    [string]$CodeQL
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path $PSScriptRoot -Parent
$vacDir = Join-Path $root "src/Vac"
$outDir = Join-Path $root "artifacts/codeql"
$database = Join-Path $outDir "vac-db"
$sarif = Join-Path $outDir "vac.sarif"
$driversPack = "microsoft/windows-drivers@1.10.0"

# Findings that don't apply, by rule ID.
$excludedRules = @(
    # PcAddAdapterDevice creates the FDO and clears DO_DEVICE_INITIALIZING itself; the driver never sees the FDO.
    "cpp/drivers/init-not-cleared"
)

if (-not $CodeQL) {
    $CodeQL = (Get-Command codeql -ErrorAction SilentlyContinue)?.Source
}
if (-not $CodeQL) {
    $CodeQL = Join-Path $root "artifacts/tools/codeql/codeql.exe"
    if (-not (Test-Path $CodeQL)) {
        $zip = Join-Path $root "artifacts/tools/codeql-win64.zip"
        New-Item -ItemType Directory -Force (Split-Path $zip) | Out-Null
        Invoke-WebRequest https://github.com/github/codeql-cli-binaries/releases/latest/download/codeql-win64.zip -OutFile $zip
        Expand-Archive $zip (Join-Path $root "artifacts/tools") -Force
    }
}

# restores the WDK packages and checks that the driver builds
& (Join-Path $PSScriptRoot "build-vac.ps1") -Configuration Release -Platform x64

# the driver suites reference microsoft/cpp-queries, which isn't downloaded with them
& $CodeQL pack download $driversPack microsoft/cpp-queries@0.0.5

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\amd64\MSBuild.exe" | Select-Object -First 1
$project = Join-Path $vacDir "Micser.Vac.vcxproj"
New-Item -ItemType Directory -Force $outDir | Out-Null

& $CodeQL database create $database --overwrite --language=cpp --source-root $vacDir `
    --command="`"$msbuild`" `"$project`" /t:Rebuild /p:Configuration=Release /p:Platform=x64 /nologo /v:m"

& $CodeQL database analyze $database `
    "${driversPack}:windows-driver-suites/mustfix.qls" `
    "${driversPack}:windows-driver-suites/recommended.qls" `
    --format=sarifv2.1.0 --output=$sarif --threads=0

# the restored WDK headers are under the source root, but their findings aren't ours
$results = (Get-Content $sarif -Raw | ConvertFrom-Json).runs[0].results | Where-Object {
    $_.locations[0].physicalLocation.artifactLocation.uri -notlike "packages/*" -and $_.ruleId -notin $excludedRules
}

foreach ($result in $results) {
    $location = $result.locations[0].physicalLocation
    Write-Host "$($location.artifactLocation.uri):$($location.region.startLine): $($result.ruleId): $($result.message.text)"
}

if ($results) {
    throw "CodeQL found $(@($results).Count) issue(s) in the driver"
}

Write-Host "CodeQL: no findings in the driver code"
