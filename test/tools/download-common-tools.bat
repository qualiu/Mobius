@echo off
rem to fix problems of bat (such as cannot find lable of sub funciton) : unix2dos *.bat
SetLocal EnableExtensions EnableDelayedExpansion

if "%1" == "-h"     set ToShowUsage=1
if "%1" == "--help" set ToShowUsage=1
if "%ToShowUsage%" == "1" (
    echo Usage   : %0  [OVERWRITE : default = 0 not ]
    echo Example : %0   0
    exit /b 0
)

set OVERWRITE=%1

set ShellDir=%~dp0
if %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set TarTool=%ShellDir%\tar.exe
set WGetTool=%ShellDir%\wget.exe

set DownloadTool=%ShellDir%\download-file.bat
call :CheckExist %DownloadTool% "download-file.bat"

icacls %TarTool% /grant %USERNAME%:RX
icacls %WGetTool% /grant %USERNAME%:RX

call :DownloadToolsByPathAndUrl %ShellDir%\lzmw.exe "https://github.com/qualiu/lzmw/blob/master/tools/lzmw.exe?raw=true" 
call :DownloadToolsByPathAndUrl %ShellDir%\psall.bat "https://github.com/qualiu/lzmw/blob/master/tools/psall.bat?raw=true"
call :DownloadToolsByPathAndUrl %ShellDir%\pskill.bat "https://github.com/qualiu/lzmw/blob/master/tools/pskill.bat?raw=true"
call :DownloadToolsByPathAndUrl %ShellDir%\not-in-later.exe "https://github.com/qualiu/lzmw/blob/master/tools/in-later/not-in-later.exe?raw=true"
rem for /f "tokens=*" %%c in (' not-in-later.exe ^| lzmw -it "copy\s+/y\b"  -PAC ') do cmd /c %%c

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    goto :End

:DownloadToolsByPathAndUrl
    setlocal
    set NeedDownload=0
    if not exist "%~1" ( 
        set NeedDownload=1
    ) else ( 
        if [%OVERWRITE%] == [1] (
        set NeedDownload=1
        )
    )
    if [%NeedDownload%] == [1] (
        wget -O %1 %2
        icacls %1 /grant %USERNAME%:RX
    )
    endlocal
    
    goto :End

:End

