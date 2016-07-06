@echo off
SetLocal EnableExtensions EnableDelayedExpansion

if "%1" == "" (
    echo Usage   : %0  Url                                Save-Directory         
    echo Example : %0  http://**/zookeeper-3.4.6.tar.gz   d:\tmp\zookeeper-3.4.6
    exit /b 0
)

set Url=%~1
set SAVE_DIR=%2

set ShellDir=%~dp0
if %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set WgetExe=%ShellDir%\wget.exe

call :CheckExist %WgetExe% "wget.exe"

if not exist %SAVE_DIR% md %SAVE_DIR%

%WgetExe% "%Url%" -P %SAVE_DIR%

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )

:End
    
