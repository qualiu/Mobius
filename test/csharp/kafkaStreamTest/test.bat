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

call :CheckExist %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd "sparkclr-submit.cmd"
call :CheckExist %ExePath% "ExePath"
call :CheckExist %ExeDir% "ExeDir"

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following, run : %ExePath%
    call %ExePath%
    echo.
    echo Example-1 : %0 -topic test -d 1 -w 5 -s 1
    echo Example-2 : %0 -d 1 -topic test  2^>^&1 ^| lzmw -it "args.\d+|sumcount|exception"
    exit /b 0
)

pushd %ExeDir%

call :FindJarInDir %shellDir%\lib
call :FindJarInDir %CodeRootDir%\build\dependencies

if not "%JarOption%" == "" (
    set JarOption=--jars %JarOption%
) else (
    echo Not found spark-streaming-kafka-xx.jar , if not in your spark common settings, 
    echo please download it from web, such as : http://repo2.maven.org/maven2/org/apache/spark/spark-streaming-kafka-assembly_2.10/1.6.1/spark-streaming-kafka-assembly_2.10-1.6.1.jar 
    echo and put into %shellDir%\lib or %CodeRootDir%\build\dependencies
    echo.
    sleep 3
)
echo =============== run sparkclr-submit ===================================
echo %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %options% %JarOption% --exe %ExeName% %CD% %AllArgs%
call %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %options% %JarOption% --exe %ExeName% %CD% %AllArgs%
popd

echo ======================================================
echo Test tool usages just run : %ExePath%

goto :End

:CheckExist
    setlocal
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    endlocal
    goto :End
    
:FindJarInDir
    if exist %1 (
        for /F "tokens=*" %%f in (' dir /B %1\*.jar ') do set "JarOption=%1\%%f,!JarOption!"
    )
    goto :End
    
:End
    
