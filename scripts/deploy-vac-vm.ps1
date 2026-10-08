# Builds the VAC driver and installs it in a Hyper-V test VM (test signing on) over PowerShell Direct. With -TestSeconds, it then runs
# the AudioHarness latency measurement through cable 1 in the VM.
#
# The VM login is read from %USERPROFILE%\.micser-vm-cred.xml, created once with:
#   Get-Credential | Export-Clixml "$env:USERPROFILE\.micser-vm-cred.xml"
param(
    [string]$VMName = "DriverTesting",
    [ValidateSet("Debug", "Release")][string]$Configuration = "Debug",
    [switch]$NoBuild,
    [int]$TestSeconds = 0
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path $PSScriptRoot -Parent
$vacDir = Join-Path $root "src/Vac"
$binDir = Join-Path $vacDir "bin/x64/$Configuration"
$harnessDir = Join-Path $root "artifacts/harness"
$remoteDir = "C:\MicserVac"
$hardwareId = "ROOT\MicserVac"

if (-not $NoBuild) {
    & (Join-Path $PSScriptRoot "build-vac.ps1") -Configuration $Configuration -Platform x64
}

$wdkVersion = (([xml](Get-Content (Join-Path $vacDir "packages.config"))).packages.package | Where-Object id -eq "Microsoft.Windows.WDK.x64").version
$devcon = (Resolve-Path (Join-Path $vacDir "packages/Microsoft.Windows.WDK.x64.$wdkVersion/c/tools/10.*/x64/devcon.exe")).Path

if ($TestSeconds -gt 0) {
    dotnet publish (Join-Path $root "tools/AudioHarness") -c Release -r win-x64 --self-contained -o $harnessDir
}

$credential = Import-Clixml (Join-Path $env:USERPROFILE ".micser-vm-cred.xml")

function Open-VMSession {
    # waits until PowerShell Direct accepts the login, e.g. after a reboot
    for ($i = 0; $i -lt 60; $i++) {
        try {
            return New-PSSession -VMName $VMName -Credential $credential -ErrorAction Stop
        }
        catch {
            Start-Sleep -Seconds 5
        }
    }

    throw "PowerShell Direct into $VMName didn't come up"
}

$session = Open-VMSession
try {
    Invoke-Command -Session $session -ScriptBlock {
        param($dir)
        if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
        New-Item -ItemType Directory $dir | Out-Null
    } -ArgumentList $remoteDir

    Copy-Item (Join-Path $binDir "Micser.Vac") -Destination $remoteDir -ToSession $session -Recurse
    Copy-Item (Join-Path $binDir "MicserVac.cer"), $devcon -Destination $remoteDir -ToSession $session
    if ($TestSeconds -gt 0) {
        Copy-Item $harnessDir -Destination "$remoteDir\harness" -ToSession $session -Recurse
    }

    # Dev builds share one DriverVer, so PnP would keep using an older package from the driver store: remove the device first. If
    # something still holds it, the removal only completes with a reboot. Installing before that would start a second adapter.
    $removeExitCode = Invoke-Command -Session $session -ScriptBlock {
        param($dir, $hardwareId)
        & "$dir\devcon.exe" remove $hardwareId | Out-Null
        $LASTEXITCODE
    } -ArgumentList $remoteDir, $hardwareId

    if ($removeExitCode -eq 1) {
        Write-Host "Rebooting $VMName to finish removing the old device"
        Invoke-Command -Session $session -ScriptBlock { Restart-Computer -Force }
        Remove-PSSession $session
        Start-Sleep -Seconds 15
        $session = Open-VMSession

        # the VM signs in automatically (AutoAdminLogon); tests need that console user
        Invoke-Command -Session $session -ScriptBlock {
            for ($i = 0; $i -lt 24 -and -not (Get-CimInstance Win32_ComputerSystem).UserName; $i++) { Start-Sleep -Seconds 5 }
        }
    }
    elseif ($removeExitCode -gt 1) {
        throw "devcon remove failed with exit code $removeExitCode"
    }

    Invoke-Command -Session $session -ScriptBlock {
        param($dir, $hardwareId)
        $ErrorActionPreference = "Stop"

        # the WDK test certificate is self-signed, so it has to be both a trusted root and a trusted publisher
        # -f creates the TrustedPublisher store, which a fresh Windows doesn't have yet
        foreach ($store in "Root", "TrustedPublisher") {
            certutil -f -addstore $store "$dir\MicserVac.cer" | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "certutil -addstore $store failed with exit code $LASTEXITCODE" }
        }

        $drivers = pnputil /enum-drivers | Out-String
        foreach ($match in [regex]::Matches($drivers, "Published Name:\s+(oem\d+\.inf)\s+Original Name:\s+micser\.vac\.inf")) {
            pnputil /delete-driver $match.Groups[1].Value /force | Out-Null
        }

        & "$dir\devcon.exe" install "$dir\Micser.Vac\Micser.Vac.inf" $hardwareId
        # 1 means a reboot is needed
        if ($LASTEXITCODE -gt 1) { throw "devcon install failed with exit code $LASTEXITCODE" }
        if ($LASTEXITCODE -eq 1) { Write-Warning "Reboot the VM to finish the driver installation." }

        Get-PnpDevice -PresentOnly | Where-Object { $_.HardwareID -contains $hardwareId -or $_.FriendlyName -like "*Micser*" } |
            Format-Table Status, Class, FriendlyName -AutoSize | Out-String
    } -ArgumentList $remoteDir, $hardwareId

    if ($TestSeconds -gt 0) {
        Invoke-Command -Session $session -ScriptBlock {
            param($dir, $seconds)
            # Without a user at the console, the audio engine renders silence for this session's streams.
            if (-not (Get-CimInstance Win32_ComputerSystem).UserName) {
                throw "Sign in at the VM console (basic session) first: without a console user, all streams are silent."
            }

            & "$dir\harness\Micser.AudioHarness.exe" list
            & "$dir\harness\Micser.AudioHarness.exe" latency Micser Micser --seconds $seconds
        } -ArgumentList $remoteDir, $TestSeconds
    }
}
finally {
    Remove-PSSession $session
}
