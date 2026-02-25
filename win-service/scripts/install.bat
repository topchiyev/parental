@echo off
if not "%1"=="am_admin" (
    powershell -Command "Start-Process -Verb RunAs -FilePath '%0' -ArgumentList 'am_admin'"
    exit /b
)

set BIN_PATH=""
pushd %~dp0
set BIN_PATH="%CD%\..\publish\parental.exe"
popd

set /p SERVER_ADDRESS=What is the server address?
set /p DEVICE_ID=What is the device ID?

reg add "HKEY_CURRENT_USER\Software\Parental" /v "ServerAddress" /t REG_SZ /d "%SERVER_ADDRESS%" /f
reg add "HKEY_CURRENT_USER\Software\Parental" /v "DeviceID" /t REG_SZ /d "%DEVICE_ID%" /f

sc.exe stop "Parental"
sc.exe delete "Parental"
sc.exe create "Parental" binpath="%BIN_PATH%" start=auto
sc.exe start "Parental"

pause
