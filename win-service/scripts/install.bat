@echo off
setlocal EnableExtensions

REM ============================================================
REM Parental service installer / configurator
REM Usage:
REM   install.bat /a https://server.example.com /d DEVICE123
REM   install.bat /h
REM ============================================================

REM --- Handle help before elevation ---
call :ParseArgs %*
if errorlevel 2 (
    call :PrintHelp
    exit /b 0
)
if errorlevel 1 (
    exit /b 1
)

REM --- Elevate if needed ---
if /I not "%1"=="am_admin" (
    powershell -NoProfile -Command ^
      "Start-Process -Verb RunAs -FilePath '%~f0' -ArgumentList 'am_admin %*'"
    exit /b
)

REM --- Remove the am_admin marker and parse actual args again ---
shift
call :ParseArgs %*
if errorlevel 2 (
    call :PrintHelp
    exit /b 0
)
if errorlevel 1 (
    exit /b 1
)

REM --- Resolve binary path ---
pushd "%~dp0"
set "BIN_PATH=%CD%\..\publish\parental.exe"
popd

if not exist "%BIN_PATH%" (
    echo ERROR: Service binary not found:
    echo   %BIN_PATH%
    exit /b 1
)

echo.
echo ServerAddress = %SERVER_ADDRESS%
echo DeviceID      = %DEVICE_ID%
echo Binary        = %BIN_PATH%
echo.

REM --- Write config to registry (HKCU as requested) ---
reg add "HKEY_CURRENT_USER\Software\Parental" /v "ServerAddress" /t REG_SZ /d "%SERVER_ADDRESS%" /f >nul
if errorlevel 1 (
    echo ERROR: Failed to write ServerAddress to registry.
    exit /b 1
)

reg add "HKEY_CURRENT_USER\Software\Parental" /v "DeviceID" /t REG_SZ /d "%DEVICE_ID%" /f >nul
if errorlevel 1 (
    echo ERROR: Failed to write DeviceID to registry.
    exit /b 1
)

REM --- Reinstall service ---
sc.exe stop "Parental" >nul 2>&1
sc.exe delete "Parental" >nul 2>&1

sc.exe create "Parental" binPath= "\"%BIN_PATH%\"" start= auto
if errorlevel 1 (
    echo ERROR: Failed to create service "Parental".
    exit /b 1
)

sc.exe start "Parental"
if errorlevel 1 (
    echo ERROR: Failed to start service "Parental".
    exit /b 1
)

echo.
echo Done.
pause
exit /b 0


:ParseArgs
set "SERVER_ADDRESS="
set "DEVICE_ID="

:ParseLoop
if "%~1"=="" goto ParseDone

if /I "%~1"=="/h" exit /b 2
if /I "%~1"=="-h" exit /b 2
if /I "%~1"=="/?" exit /b 2

if /I "%~1"=="/a" (
    if "%~2"=="" (
        echo ERROR: Missing value for /a (server address). /h for help.
        exit /b 1
    )
    set "SERVER_ADDRESS=%~2"
    shift
    shift
    goto ParseLoop
)

if /I "%~1"=="/d" (
    if "%~2"=="" (
        echo ERROR: Missing value for /d (device ID). /h for help.
        exit /b 1
    )
    set "DEVICE_ID=%~2"
    shift
    shift
    goto ParseLoop
)

echo ERROR: Unknown argument: %~1
exit /b 1

:ParseDone
if not defined SERVER_ADDRESS (
    echo ERROR: /a ^<serverAddress^> is required. /h for help.
    exit /b 1
)
if not defined DEVICE_ID (
    echo ERROR: /d ^<deviceId^> is required. /h for help.
    exit /b 1
)
exit /b 0


:PrintHelp
echo.
echo Parental Service Installer
echo.
echo Usage:
echo   %~nx0 /a ^<serverAddress^> /d ^<deviceId^>
echo   %~nx0 /h
echo.
echo Parameters:
echo   /a   Server address (for example: https://parental.example.com)
echo   /d   Device ID (for example: f4bbec6f-db45-4418-982e-f5b2175ac8cd)
echo   /h   Show this help
echo.
echo Example:
echo   %~nx0 /a "https://parental.example.com" /d "f4bbec6f-db45-4418-982e-f5b2175ac8cd"
echo.
exit /b 0