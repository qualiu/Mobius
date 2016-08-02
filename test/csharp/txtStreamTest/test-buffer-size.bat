@echo off
setlocal EnableDelayedExpansion
set ShellDir=%~dp0
IF %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%
set CallBat=%ShellDir%\test.bat
call :CheckExist %CallBat%

if "%1" == "" (
    echo #################### Usage of %CallBat% ##################################
    call %CallBat%
    echo ############################################################################
    echo.
    echo #################### Usage of this : %0 #####################################
    echo Usage :   csv-data-directory  initial-buffer-size [increase-times: default 1]   [increasement : default : initial-buffer-size] 
    echo Example : D:\csv-2015-10-01   1024                 1
    echo Example : hdfs:///common/AdsData/MUID  1024
    exit /b 0
)

set DataDirectory=%1
set InitBufferSize=%2
set TestTimes=%3
set BufferIncrease=%4

if "%BufferIncrease%" == "" set BufferIncrease=%InitBufferSize%
if "%TestTimes%" == "" set TestTimes=1

if "%TestExePath%"=="" for /f %%g in (' for /R %ShellDir% %%f in ^( *.exe ^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set TestExePath=%%g
call :CheckExist "%TestExePath%" "TestExePath" || exit /b 1

for %%a in ("%TestExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
)

IF %ExeDir:~-1%==\ SET ExeDir=%ExeDir:~0,-1%

set configFile=%ExeDir%\CSharpWorker.exe.config
call :CheckExist %configFile% || exit /b 1
set configBackup=%configFile%-lzbackup
copy /y %configFile% %configBackup%

set bufferSize=%InitBufferSize%
for /L %%k in (1,1, %TestTimes%) do (
    set spark.app.name=%ExeName%-buffer-!bufferSize!
    lzmw -p %configFile% -it "(key=\Wspark.mobius.network.buffersize\W\s+value=\W)(\d+)" -o "${1}!bufferSize!" -R
    call %CallBat% %DataDirectory% || copy /y %configBackup% %configFile% & exit /b 1
    set /a bufferSize=!bufferSize!+%BufferIncrease%
)

copy /y %configBackup% %configFile%
goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    goto :End
    
:End
    
