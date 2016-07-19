@echo off
setlocal enabledelayedexpansion
set ShellDir=%~dp0
IF %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

for /f %%g in (' for /R %ShellDir% %%f in ^( *.exe ^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set TestExePath=%%g
for %%a in ("%TestExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
)

set CodeRootDir=%ShellDir%\..\..\..
set CommonToolDir=%ShellDir%\..\..\tools
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir%

call :CheckExist %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd "sparkclr-submit.cmd"
call :CheckExist %TestExePath% "TestExePath"

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following, run : %TestExePath%
    call %TestExePath%
    echo Example parameters : -p 9112 -e 1 -r 30 -b 1 -w 3 -s 3 -v 50 -c d:\tmp\checkDir -d 1
    echo Test usage just run : %TestExePath%
    exit /b 0
)

pushd %ExeDir%
set options=--executor-cores 2 --driver-cores 2 --executor-memory 1g --driver-memory 1g
call %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %options% --exe %ExeName% %CD% %*
popd

echo ======================================================
echo Test tool usages just run : %TestExePath%

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
