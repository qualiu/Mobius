@echo off
setlocal enabledelayedexpansion
set ShellDir=%~dp0
IF %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set SocketCodeDir=%ShellDir%\..\SourceLinesSocket
for /f %%g in (' for /R %SocketCodeDir% %%f in ^(*.exe^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set SourceSocketExe=%%g

for /f %%g in (' for /R %ShellDir% %%f in ^( *.exe ^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set TestExePath=%%g
for %%a in ("%TestExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
)

set CodeRootDir=%ShellDir%\..\..\..
set CommonToolDir=%ShellDir%\..\..\tools
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir%

call :CheckExist %SourceSocketExe% "SourceSocketExe"
call :CheckExist %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd "sparkclr-submit.cmd"
call :CheckExist %TestExePath% "TestExePath"
call :CheckExist %ExeDir% "ExeDir"

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following, run : %TestExePath%
    call %TestExePath%
    echo Example parameters : -p 9112 -e 1 -r 30 -b 1 -w 3 -s 3 -n 50 -c d:\tmp\checkDir -d 1 
    echo Parameters like host, port and validation are according to source socket tool : %SourceSocketExe%
    echo Test usage just run : %TestExePath%
    exit /b 0
)

set Port=9333
set LineCount=60
call :ExtractArgs %*

rem use cmd /k if you want to keep the window
start cmd /c "%SourceSocketExe%" -p %Port% -n %LineCount%

pushd %ExeDir%
set options=--executor-cores 2 --driver-cores 2 --executor-memory 1g --driver-memory 1g
call %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %options% --exe %ExeName% %CD% %AllArgs%
popd

echo ======================================================
echo More source socket usages just run : %SourceSocketExe%
echo Test tool usages just run : %TestExePath%

goto :End

:ExtractArgs
    if "%1" == ""  goto :End
    if "%1" == "-p" (
        set Port=%2
    )
    if "%1" == "-Port" (
        set Port=%2
    )
    if "%1" == "-n" (
        set LineCount=%2
    )
    if "%1" == "LineCount" (
        set LineCount=%2
    )
    shift
    goto :ExtractArgs


:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    
:End

