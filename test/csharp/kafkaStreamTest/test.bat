@echo off
@setlocal enabledelayedexpansion

set shellDir=%~dp0
if %shellDir:~-1%==\ SET shellDir=%shellDir:~0,-1%
for /f %%g in (' for /R %shellDir% %%f in ^( *.exe ^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set ExePath=%%g
for %%a in ("%ExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
)

set CodeRootDir=%shellDir%\..\..\..
set CommonToolDir=%shellDir%\..\..\tools
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir%

call :CheckExist "sparkclr-submit.cmd" %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd
call :CheckExist "ExePath" %ExePath%
call :CheckExist "ExeDir" %ExeDir%

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following, run : %ExePath%
    call %ExePath%
    echo.
    echo Example : %0 -topic test -d 1 -w 5 -s 1
    exit /b 0
)

pushd %ExeDir%
set options=--executor-cores 2 --driver-cores 2 --executor-memory 1g --driver-memory 1g
set JarDir=%shellDir%\lib
for /F "tokens=*" %%f in (' dir /B %JarDir%\*.jar ') do set "JarOption=%JarDir%\%%f,!JarOption!"
echo JarOption = %JarOption%

if not "%JarOption%" == "" set JarOption=--jars %JarOption%

call %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %options% %JarOption% --exe %ExeName% %CD% %AllArgs%
popd

echo ======================================================
echo Test tool usages just run : %ExePath%

goto :End

:CheckExist
    if not exist %2 (
        echo Not exist %1 : %2
        exit /b 1
    )


:End
    