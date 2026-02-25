# install.ps1
# Parental Windows Service Installer / Configurator
#
# Usage:
#   .\install.ps1 -ServerAddress "http://10.211.55.2:5000" -DeviceId "f4bbec6f-db45-4418-982e-f5b2175ac8cd"
#   .\install.ps1 -Help
#
# Notes:
# - Stores config in HKLM:\Software\Parental (recommended for Windows services)
# - Self-elevates to Administrator if needed
# - Writes a log to $env:TEMP\parental-install.log

[CmdletBinding()]
param(
    [Alias('a')]
    [string]$ServerAddress,

    [Alias('d')]
    [string]$DeviceId,

    [Alias('h')]
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Script:LogFile = Join-Path $env:TEMP 'parental-install.log'

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'), $Message
    Add-Content -Path $Script:LogFile -Value $line
}

function Show-Help {
    $scriptName = Split-Path -Leaf $PSCommandPath
    @"
Parental Service Installer

Usage:
  .\$scriptName -ServerAddress <serverAddress> -DeviceId <deviceId>
  .\$scriptName -a <serverAddress> -d <deviceId>
  .\$scriptName -Help

Parameters:
  -ServerAddress, -a   Server address (example: http://10.211.55.2:5000)
  -DeviceId, -d        Device ID (example: f4bbec6f-db45-4418-982e-f5b2175ac8cd)
  -Help, -h            Show this help

Notes:
  - Requires administrator rights (will prompt for elevation)
  - Writes config to HKLM:\Software\Parental
  - Service name: Parental

Log file:
  $Script:LogFile
"@ | Write-Host
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Ensure-Elevated {
    param(
        [string]$ServerAddress,
        [string]$DeviceId
    )

    if (Test-IsAdministrator) {
        Write-Log "Already elevated."
        return
    }

    Write-Log "Not elevated. Relaunching as Administrator."

    if (-not $PSCommandPath) {
        throw "Cannot self-elevate because PSCommandPath is empty. Save script to a .ps1 file and run it."
    }

    # Rebuild argument list safely
    $argList = @(
        '-NoProfile'
        '-ExecutionPolicy', 'Bypass'
        '-File', ('"{0}"' -f $PSCommandPath)
        '-ServerAddress', ('"{0}"' -f $ServerAddress)
        '-DeviceId', ('"{0}"' -f $DeviceId)
    )

    $p = Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $argList -PassThru
    Write-Log ("Started elevated PowerShell process PID={0}. Exiting current process." -f $p.Id)
    exit 0
}

function Get-ServiceBinaryPath {
    # Expected layout:
    #   ...\win-service\scripts\install.ps1
    #   ...\win-service\publish\parental.exe
    $scriptDir = Split-Path -Parent $PSCommandPath
    $binPath = Join-Path $scriptDir '..\publish\parental.exe'
    $resolved = [System.IO.Path]::GetFullPath($binPath)
    return $resolved
}

function Ensure-RegistryConfig {
    param(
        [Parameter(Mandatory=$true)][string]$ServerAddress,
        [Parameter(Mandatory=$true)][string]$DeviceId
    )

    $regPath = 'HKLM:\Software\Parental'
    Write-Log "Writing registry config to $regPath"

    if (-not (Test-Path $regPath)) {
        New-Item -Path $regPath -Force | Out-Null
    }

    New-ItemProperty -Path $regPath -Name 'ServerAddress' -PropertyType String -Value $ServerAddress -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'DeviceID' -PropertyType String -Value $DeviceId -Force | Out-Null

    # Verify (helps debugging)
    $serverValue = (Get-ItemProperty -Path $regPath -Name 'ServerAddress').ServerAddress
    $deviceValue = (Get-ItemProperty -Path $regPath -Name 'DeviceID').DeviceID
    Write-Log "Registry verification: ServerAddress=$serverValue ; DeviceID=$deviceValue"
}

function Invoke-Sc {
    param(
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [switch]$IgnoreErrors
    )

    Write-Log ("sc.exe " + ($Arguments -join ' '))
    $output = & sc.exe @Arguments 2>&1
    $exitCode = $LASTEXITCODE

    if ($output) {
        $output | ForEach-Object { Write-Log "  $_" }
    }
    Write-Log "sc.exe exit code: $exitCode"

    if (-not $IgnoreErrors -and $exitCode -ne 0) {
        throw "sc.exe failed with exit code $exitCode. Args: $($Arguments -join ' ')"
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output   = $output
    }
}

try {
    New-Item -ItemType File -Path $Script:LogFile -Force | Out-Null
    Write-Log "============================================================"
    Write-Log ("Script start. ServerAddress='{0}', DeviceId='{1}', Help={2}" -f $ServerAddress, $DeviceId, $Help)

    if ($Help) {
        Show-Help
        exit 0
    }

    if ([string]::IsNullOrWhiteSpace($ServerAddress)) {
        throw "Missing required parameter: -ServerAddress (or -a). Use -Help for usage."
    }
    if ([string]::IsNullOrWhiteSpace($DeviceId)) {
        throw "Missing required parameter: -DeviceId (or -d). Use -Help for usage."
    }

    Ensure-Elevated -ServerAddress $ServerAddress -DeviceId $DeviceId

    Write-Log "Running elevated installer."
    Write-Log "ServerAddress=$ServerAddress"
    Write-Log "DeviceId=$DeviceId"

    $binPath = Get-ServiceBinaryPath
    Write-Log "Resolved binary path: $binPath"

    if (-not (Test-Path $binPath -PathType Leaf)) {
        throw "Service binary not found: $binPath"
    }

    Write-Host ""
    Write-Host "Installing Parental service..."
    Write-Host "  ServerAddress: $ServerAddress"
    Write-Host "  DeviceID:      $DeviceId"
    Write-Host "  Binary:        $binPath"
    Write-Host ""

    Ensure-RegistryConfig -ServerAddress $ServerAddress -DeviceId $DeviceId

    # Stop and delete existing service if present (ignore errors)
    Invoke-Sc -Arguments @('stop', 'Parental') -IgnoreErrors | Out-Null
    Start-Sleep -Milliseconds 500
    Invoke-Sc -Arguments @('delete', 'Parental') -IgnoreErrors | Out-Null
    Start-Sleep -Milliseconds 500

    # Create service
    # sc.exe requires a very particular syntax for parameters with spaces:
    #   binPath= "<path>"
    #   start= auto
    # We pass each as a single argument string.
    Invoke-Sc -Arguments @(
        'create', 'Parental',
        "binPath= `"$binPath`"",
        'start= auto'
    ) | Out-Null

    # Start service
    Invoke-Sc -Arguments @('start', 'Parental') | Out-Null

    Write-Log "Install completed successfully."
    Write-Host "Done."
    Write-Host "Log file: $Script:LogFile"
    exit 0
}
catch {
    $msg = $_.Exception.Message
    Write-Log "ERROR: $msg"
    Write-Host ""
    Write-Host "ERROR: $msg" -ForegroundColor Red
    Write-Host "Log file: $Script:LogFile"
    exit 1
}