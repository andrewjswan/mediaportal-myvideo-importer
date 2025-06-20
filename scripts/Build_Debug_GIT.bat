@echo off
cls
Title Building MediaPortal MyVideo Importer (DEBUG)
cd ..

setlocal enabledelayedexpansion

:: Prepare version
for /f "tokens=*" %%a in ('git rev-list HEAD --count') do set REVISION=%%a 
set REVISION=%REVISION: =%
"scripts\Tools\sed.exe" -i "s/\$WCREV\$/%REVISION%/g" MyVideoImporter\Properties\AssemblyInfo.cs

:: Build
FOR %%p IN ("%PROGRAMFILES(x86)%" "%PROGRAMFILES%") DO (
  FOR %%s IN (2019 2022) DO (
    FOR %%e IN (Community Professional Enterprise BuildTools) DO (
      SET PF=%%p
      SET PF=!PF:"=!
      SET MSBUILD_PATH="!PF!\Microsoft Visual Studio\%%s\%%e\MSBuild\Current\Bin\MSBuild.exe"
      IF EXIST "!MSBUILD_PATH!" GOTO :BUILD
    )
  )
)

:BUILD

%MSBUILD_PATH% /target:Rebuild /property:Configuration=DEBUG /fl /flp:logfile=MyVideoImporter.log;verbosity=diagnostic MyVideoImporter.sln

:: Revert version
git checkout MyVideoImporter\Properties\AssemblyInfo.cs

cd scripts

pause

