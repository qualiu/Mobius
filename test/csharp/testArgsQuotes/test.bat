@echo off
setlocal enabledelayedexpansion
set ShellDir=%~dp0
IF %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

for /f %%g in (' for /R %ShellDir% %%f in ^( *.exe ^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set ExePath=%%g
for %%a in ("%ExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
)

set CodeRootDir=%ShellDir%\..\..\..
set CommonToolDir=%ShellDir%\..\..\tools
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir%

call :CheckExist %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd "sparkclr-submit.cmd"
call :CheckExist %ExePath% "ExePath"
call :CheckExist %ExeDir% "ExeDir"

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following, run : %ExePath%
    call %ExePath%
    exit /b 0
)

pushd %ExeDir%
set options=--executor-cores 2 --driver-cores 2 --executor-memory 1g --driver-memory 1g
call %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %options% --exe %ExeName% %CD% %AllArgs%
popd

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    goto :End
    
:End
    
