@echo off
cls
Title Creating MediaPortal MyVideo Importer Installer

if "%programfiles(x86)%XXX"=="XXX" goto 32BIT
	:: 64-bit
	set PROGS=%programfiles(x86)%
	goto CONT
:32BIT
	set PROGS=%ProgramFiles%
:CONT

IF NOT EXIST "%PROGS%\Team MediaPortal\MediaPortal\" SET PROGS=C:

:: Get version from DLL
FOR /F "tokens=*" %%i IN ('Tools\sigcheck.exe /accepteula /nobanner /n "..\MyVideoImporter\bin\Release\MyVideoImporter.dll"') DO (SET version=%%i)

:: Temp xmp2 file
copy /Y MyVideoImporter.xmp2 MyVideoImporterTemp.xmp2

:: Build mpe1
"%PROGS%\Team MediaPortal\MediaPortal\MPEMaker.exe" MyVideoImporterTemp.xmp2 /B /V=%version% /UpdateXML

:: Cleanup
del MyVideoImporterTemp.xmp2
