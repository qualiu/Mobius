@echo off
setlocal enabledelayedexpansion
echo ### you can set TestExePath to avoid decttion ###
echo ### you can set HasRIO=1 to enable RIO socket ###
set ShellDir=%~dp0
IF %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

if "%TestExePath%"=="" for /f %%g in (' for /R %ShellDir% %%f in ^( *.exe ^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set TestExePath=%%g
for %%a in ("%TestExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
)

set CodeRootDir=%ShellDir%\..\..\..
set CommonToolDir=%ShellDir%\..\..\tools

call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir% || exist /b 1

call :CheckExist %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd "sparkclr-submit.cmd" || exit /b 1
call :CheckExist %TestExePath% "TestExePath" || exit /b 1
call :CheckExist %ExeDir% "ExeDir" || exit /b 1

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following, run : %TestExePath%
    call %TestExePath%
    exit /b 0
)

pushd %ExeDir%
::set options=--executor-cores 2 --driver-cores 2 --executor-memory 2g --driver-memory 2g
rem call %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %options% --exe %ExeName% %CD% %AllArgs%

set options=--name textStreamMobius --num-executors 8 --executor-cores 4 --executor-memory 8G --driver-memory 12G
set options=%options% --conf spark.streaming.nao.loadExistingFiles=true 
set options=%options% --conf spark.streaming.kafka.maxRetries=300 
set options=%options% --conf "spark.yarn.executor.memoryOverhead=18000"
set options=%options% --conf spark.streaming.kafka.maxRetries=20
set options=%options% --conf spark.mobius.streaming.kafka.CSharpReader.enabled=true
set options=%options% --jars %CodeRootDir%\build\dependencies\spark-streaming-kafka-assembly_2.10-1.6.1.jar
if "%HasRIO%" == "1" set options=%options% --conf spark.mobius.CSharp.socketType=Rio

call %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %options% --exe %ExeName% %CD% %AllArgs%

rem --master yarn-cluster --jars D:\Spark\Mobius\dependencies\spark-streaming-kafka-assembly_2.10-1.6.1.jar ^
rem --num-executors 100 --executor-cores 28 --executor-memory 30G --driver-memory 32G ^

popd

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    goto :End
    
:End
    
