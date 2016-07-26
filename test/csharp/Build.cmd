@setlocal
@ECHO off

set BuildConfig=%1
if "%2" == "nocpp" set CppDll=NoCpp

SET ShellDir=%~dp0
@REM Remove trailing backslash \
set ShellDir=%ShellDir:~0,-1%

set CodeRoot=%ShellDir%\..\..
set RioCodeDir=%CodeRoot%\cpp

call where nuget.exe 2>nul
if ERRORLEVEL 1 ( 
    if exist "%CodeRoot%\build\tools\nuget.exe" ( 
       set "PATH=%PATH%;%CodeRoot%\build\tools"
    ) else (
        echo You'd better build the main code first, then nuget will in: %CodeRoot%\build\tools\nuget.exe
        if exist "%CodeRoot%\build\Build.cmd" echo %CodeRoot%\build\Build.cmd
        goto :EOF
    )
)

if not "%CppDll%" == "NoCpp" if exist %RioCodeDir% xcopy /Y /I /S /D %RioCodeDir% %ShellDir%\..\cpp

@REM Set msbuild location.
SET VisualStudioVersion=14.0
if EXIST "%VS140COMNTOOLS%" SET VisualStudioVersion=14.0

@REM Set Build OS
if not defined CppDll SET CppDll=HasCpp
SET VCBuildTool="%VS120COMNTOOLS:~0,-14%VC\bin\cl.exe"
if EXIST "%VS140COMNTOOLS%" SET VCBuildTool="%VS140COMNTOOLS:~0,-14%VC\bin\cl.exe"
if NOT EXIST %VCBuildTool% SET CppDll=NoCpp


SET MSBUILDEXEDIR=%programfiles(x86)%\MSBuild\%VisualStudioVersion%\Bin
if NOT EXIST "%MSBUILDEXEDIR%\." SET MSBUILDEXEDIR=%programfiles%\MSBuild\%VisualStudioVersion%\Bin
if NOT EXIST "%MSBUILDEXEDIR%\." GOTO :ErrorMSBUILD

SET MSBUILDEXE=%MSBUILDEXEDIR%\MSBuild.exe
SET MSBUILDOPT=/verbosity:minimal /p:WarningLevel=3

if "%builduri%" == "" set builduri=Build.cmd

cd "%ShellDir%"
@cd

set PROJ_NAME=allSubmitingTest
set PROJ=%ShellDir%\%PROJ_NAME%.sln

@echo ===== Building %PROJ% =====

@echo Restore NuGet packages ===================
SET STEP=NuGet-Restore

nuget restore "%PROJ%"

@if ERRORLEVEL 1 GOTO :ErrorStop

if "%BuildConfig%" == "" set BuildDebug=1
if not "%BuildConfig%" == "" if "%BuildConfig%" == "Debug" set BuildDebug=1
if "%BuildDebug%" == "1" call :BuildByConfig Debug

if "%BuildConfig%" == "" set BuildRelease=1
if not "%BuildConfig%" == "" if "%BuildConfig%" == "Release" set BuildRelease=1
if "%BuildRelease%" == "1" call :BuildByConfig Release

if EXIST %PROJ_NAME%.nuspec (
  @echo ===== Build NuGet package for %PROJ% =====
  SET STEP=NuGet-Pack

  powershell -f %ShellDir%\..\build\localmode\nugetpack.ps1
  @if ERRORLEVEL 1 GOTO :ErrorStop
  @echo NuGet package ok for %PROJ%
)

@echo ===== Build succeeded for %PROJ% =====

@GOTO :EOF

:BuildByConfig
	SET Configuration=%1
	@echo Build %Configuration% ============================
	"%MSBUILDEXE%" /p:Configuration=%Configuration%;AllowUnsafeBlocks=true %MSBUILDOPT% "%PROJ%"
	@if ERRORLEVEL 1 GOTO :ErrorStop
	@echo BUILD ok for %Configuration% %PROJ%
	goto :EOF

:ErrorMSBUILD
	set RC=1
	@echo ===== Build FAILED due to missing MSBUILD.EXE. =====
	@echo ===== Mobius requires "Developer Command Prompt for VS2013" and above =====
	exit /B %RC%

:ErrorStop
	set RC=%ERRORLEVEL%
	if "%STEP%" == "" set STEP=%CONFIGURATION%
	@echo ===== Build FAILED for %PROJ% -- %STEP% with error %RC% - CANNOT CONTINUE =====
	exit /B %RC%
	
:EOF
