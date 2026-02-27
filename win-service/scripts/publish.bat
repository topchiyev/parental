@echo off
setlocal

pushd "%~dp0\..\..\win-service-helper" || exit /b 1
call scripts\publish.bat || (popd & exit /b 1)
popd

pushd "%~dp0\.." || exit /b 1
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true -o ./publish || (popd & exit /b 1)
del publish\*.pdb
popd

endlocal
