@echo off
:: Get version from DLL
FOR /F "tokens=1-3" %%i IN ('Tools\sigcheck.exe -accepteula -q "..\MyVideoImporter\bin\Release\MyVideoImporter.dll"') DO ( IF "%%i %%j"=="File version:" SET version=%%k )

:: Trim Version
SET version=%version:~0,-1%

:: Show Version
ECHO %version%
