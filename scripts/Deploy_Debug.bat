@echo off
cls
Title Deploying MediaPortal MyVideo Importer (DEBUG)
cd ..

if "%programfiles(x86)%XXX"=="XXX" goto 32BIT
	:: 64-bit
	set PROGS=%programfiles(x86)%
	goto CONT
:32BIT
	set PROGS=%ProgramFiles%
:CONT

copy /y "MyVideoImporter\bin\Debug\MyVideoImporter.dll" "%PROGS%\Team MediaPortal\MediaPortal\plugins\Windows\"
cd scripts
