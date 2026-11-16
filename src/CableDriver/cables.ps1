# Shows or changes the cables of the Micser Cable driver through its control device, without DriverUtility (for testing).
#   cables.ps1                    shows the cables
#   cables.ps1 count 3            three cables
#   cables.ps1 layout 2 6         cable 2 in 5.1 (channels: 2, 6 or 8)
param(
    [ValidateSet("show", "count", "layout")][string]$Action = "show",
    [int]$Value1,
    [int]$Value2
)

$ErrorActionPreference = "Stop"

Add-Type -TypeDefinition @"
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class MicserCable
{
    // public.h
    const uint GetConfigCode = 0x80006000;
    const uint SetConfigCode = 0x8000A004;
    public const int MaxCables = 16;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(SafeFileHandle device, uint code, uint[] input, int inputSize, uint[] output, int outputSize, out int returned, IntPtr overlapped);

    // CableCount, Channels[16], ActiveCables
    static uint[] Call(uint code, uint[] input)
    {
        // GENERIC_READ | GENERIC_WRITE, OPEN_EXISTING
        using (var device = CreateFile(@"\\.\MicserCable", 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero))
        {
            if (device.IsInvalid) throw new Win32Exception();
            var output = new uint[MaxCables + 2];
            int returned;
            if (!DeviceIoControl(device, code, input, input == null ? 0 : input.Length * 4, output, output.Length * 4, out returned, IntPtr.Zero))
                throw new Win32Exception();
            return output;
        }
    }

    public static uint[] GetConfig() { return Call(GetConfigCode, null); }
    public static void SetConfig(uint[] config) { Call(SetConfigCode, config); }
}
"@

$config = [MicserCable]::GetConfig()
switch ($Action) {
    "count" { $config[0] = $Value1; [MicserCable]::SetConfig($config) }
    "layout" { $config[$Value1] = $Value2; [MicserCable]::SetConfig($config) }
}

$config = [MicserCable]::GetConfig()
foreach ($n in 1..$config[0]) {
    $active = ($config[17] -band (1 -shl ($n - 1))) -ne 0
    # Windows PowerShell 5.1 has no ?: operator
    "Cable {0}: {1} channels{2}" -f $n, $config[$n], $(if ($active) { "" } else { " (missing)" })
}
