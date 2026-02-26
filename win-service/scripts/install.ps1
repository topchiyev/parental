# install.ps1
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

$Script:LogFile = Join-Path $env:TEMP 'wsprnsvc-install.log'
$Script:ServiceName = 'wsprnsvc'

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'), $Message
    Add-Content -Path $Script:LogFile -Value $line
}

function Wait-IfInteractive {
    try {
        if ($Host.Name -match 'ConsoleHost') {
            Write-Host ""
            Write-Host "Press any key to continue..."
            $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
        }
    } catch {
        # ignore if host doesn't support ReadKey
    }
}

function Show-Help {
    $scriptName = Split-Path -Leaf $PSCommandPath
    @"
Parental Service Installer

Usage:
  .\$scriptName -a <serverAddress> -d <deviceId>
  .\$scriptName -Help

Example:
  .\$scriptName -a "http://10.211.55.2:5000" -d "f4bbec6f-db45-4418-982e-f5b2175ac8cd"

Notes:
  - Requires Administrator rights (will prompt for elevation)
  - Writes config to HKLM:\Software\Parental
  - Service name: $Script:ServiceName

Log file:
  $Script:LogFile
"@ | Write-Host
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-Elevated {
    param(
        [Parameter(Mandatory)][string]$ServerAddress,
        [Parameter(Mandatory)][string]$DeviceId
    )

    if (Test-IsAdministrator) {
        Write-Log "Already elevated."
        return
    }

    Write-Log "Not elevated. Relaunching as Administrator."

    if (-not $PSCommandPath) {
        throw "Cannot self-elevate because PSCommandPath is empty. Save script to a .ps1 file and run it."
    }

    $argList = @(
        '-NoProfile'
        '-ExecutionPolicy', 'Bypass'
        '-File', ('"{0}"' -f $PSCommandPath)
        '-ServerAddress', ('"{0}"' -f $ServerAddress)
        '-DeviceId', ('"{0}"' -f $DeviceId)
    )

    $p = Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $argList -PassThru
    Write-Log ("Started elevated PowerShell process PID={0}. Exiting non-elevated process." -f $p.Id)
    exit 0
}

function Get-ServiceBinaryPath {
    $scriptDir = Split-Path -Parent $PSCommandPath
    $binPath = Join-Path $scriptDir '..\publish\wsprnsvc.exe'
    return [System.IO.Path]::GetFullPath($binPath)
}

function Set-RegistryConfig {
    param(
        [Parameter(Mandatory)][string]$ServerAddress,
        [Parameter(Mandatory)][string]$DeviceId
    )

    $regPath = 'HKLM:\Software\Parental'
    Write-Log "Writing registry config to $regPath"

    if (-not (Test-Path $regPath)) {
        New-Item -Path $regPath -Force | Out-Null
    }

    New-ItemProperty -Path $regPath -Name 'ServerAddress' -PropertyType String -Value $ServerAddress -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'DeviceID' -PropertyType String -Value $DeviceId -Force | Out-Null

    $serverValue = (Get-ItemProperty -Path $regPath -Name 'ServerAddress').ServerAddress
    $deviceValue = (Get-ItemProperty -Path $regPath -Name 'DeviceID').DeviceID
    Write-Log "Registry verification: ServerAddress=$serverValue ; DeviceID=$deviceValue"
}

function Remove-ServiceIfExists {
    param([Parameter(Mandatory)][string]$Name)

    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Log "Service '$Name' does not exist. Nothing to remove."
        return
    }

    Write-Log "Service '$Name' exists. Status=$($svc.Status). Removing."
    if ($svc.Status -ne 'Stopped') {
        try {
            Stop-Service -Name $Name -Force -ErrorAction Stop
            Write-Log "Stopped service '$Name'."
        } catch {
            Write-Log "Stop-Service failed: $($_.Exception.Message)"
        }
    }

    # Use sc.exe delete for deletion because Remove-Service is not available in Windows PowerShell 5.1
    $deleteOutput = & sc.exe delete $Name 2>&1
    $deleteCode = $LASTEXITCODE
    if ($deleteOutput) { $deleteOutput | ForEach-Object { Write-Log "sc delete: $_" } }
    Write-Log "sc delete exit code: $deleteCode"

    # Give SCM a moment
    Start-Sleep -Seconds 1
}

function Install-Service {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$BinaryPath
    )

    Write-Log "Creating service '$Name' with binary '$BinaryPath'"

    # New-Service is much more reliable than sc.exe create from PowerShell
    New-Service -Name $Name `
                -BinaryPathName ('"{0}"' -f $BinaryPath) `
                -DisplayName $Name `
                -StartupType Automatic `
                -Description 'wsprnsvc'

    Write-Log "New-Service created successfully."
}

try {
    New-Item -ItemType File -Path $Script:LogFile -Force | Out-Null
    Write-Log "============================================================"
    Write-Log ("Script start. ServerAddress='{0}', DeviceId='{1}', Help={2}" -f $ServerAddress, $DeviceId, $Help)

    if ($Help) {
        Show-Help
        Wait-IfInteractive
        exit 0
    }

    if ([string]::IsNullOrWhiteSpace($ServerAddress)) {
        throw "Missing required parameter: -ServerAddress (or -a). Use -Help for usage."
    }
    if ([string]::IsNullOrWhiteSpace($DeviceId)) {
        throw "Missing required parameter: -DeviceId (or -d). Use -Help for usage."
    }

    Assert-Elevated -ServerAddress $ServerAddress -DeviceId $DeviceId

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

    Set-RegistryConfig -ServerAddress $ServerAddress -DeviceId $DeviceId

    Remove-ServiceIfExists -Name $Script:ServiceName
    Install-Service -Name $Script:ServiceName -BinaryPath $binPath

    Write-Log "Starting service '$Script:ServiceName'"
    Start-Service -Name $Script:ServiceName -ErrorAction Stop

    # Verify status
    $svc = Get-Service -Name $Script:ServiceName -ErrorAction Stop
    Write-Log "Service '$Script:ServiceName' status after start: $($svc.Status)"

    Write-Host "Done. Service '$Script:ServiceName' status: $($svc.Status)" -ForegroundColor Green
    Write-Host "Log file: $Script:LogFile"
    Write-Log "Install completed successfully."

    Wait-IfInteractive
    exit 0
}
catch {
    $msg = $_.Exception.Message
    Write-Log "ERROR: $msg"
    Write-Host ""
    Write-Host "ERROR: $msg" -ForegroundColor Red
    Write-Host "Log file: $Script:LogFile"

    # Try to surface deeper info if present
    if ($_.InvocationInfo) {
        Write-Log ("At: {0}" -f $_.InvocationInfo.PositionMessage)
    }

    Wait-IfInteractive
    exit 1
}