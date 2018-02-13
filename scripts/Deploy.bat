@echo off

Title Deploying MediaPortal MyVideo Importer (RELEASE)
cd ..

if "%programfiles(x86)%XXX"=="XXX" goto 32BIT
	:: 64-bit
	set PROGS=%programfiles(x86)%
	goto CONT
:32BIT
	set PROGS=%ProgramFiles%
:CONT
IF NOT EXIST "%PROGS%\Team MediaPortal\MediaPortal\" SET PROGS=C:

copy /y "MyVideoImporter\bin\Release\MyVideoImporter.dll" "%PROGS%\Team MediaPortal\MediaPortal\plugins\Windows\"

cd scripts