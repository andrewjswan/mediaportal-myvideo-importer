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
FOR /F "tokens=1-3" %%i IN ('Tools\sigcheck.exe -accepteula -nobanner "..\MyVideoImporter\bin\Release\MyVideoImporter.dll"') DO ( IF "%%i %%j"=="File version:" SET version=%%k )

:: trim version
SET version=%version:~0,-1%

:: Temp xmp2 file
copy /Y MyVideoImporter.xmp2 MyVideoImporterTemp.xmp2

:: Sed "MyVideoImporter-{VERSION}.xml" from xmp2 file
Tools\sed.exe -i "s/MyVideoImporter-{VERSION}.xml/MyVideoImporter-%version%.xml/g" MyVideoImporterTemp.xmp2

:: Build mpe1
"%PROGS%\Team MediaPortal\MediaPortal\MPEMaker.exe" MyVideoImporterTemp.xmp2 /B /V=%version% /UpdateXML

:: Cleanup
del MyVideoImporterTemp.xmp2

:: Sed "MyVideoImporter-{VERSION}.mpe1" from MyVideoImporter.xml
Tools\sed.exe -i "s/MyVideoImporter-{VERSION}.mpe1/MyVideoImporter-%version%.mpe1/g" MyVideoImporter-%version%.xml

:: Parse version (Might be needed in the futute)
FOR /F "tokens=1-4 delims=." %%i IN ("%version%") DO ( 
	SET major=%%i
	SET minor=%%j
	SET build=%%k
	SET revision=%%l
)

:: Rename mpe1
if exist "..\builds\MyVideoImporter-%major%.%minor%.%build%.%revision%.mpe1" del "..\builds\MyVideoImporter-%major%.%minor%.%build%.%revision%.mpe1"
rename ..\builds\MyVideoImporter-MAJOR.MINOR.BUILD.REVISION.mpe1 "MyVideoImporter-%major%.%minor%.%build%.%revision%.mpe1"
