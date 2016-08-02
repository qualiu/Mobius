@echo off
setlocal EnableDelayedExpansion
set ShellDir=%~dp0
IF %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

if "%TestExePath%"=="" for /f %%g in (' for /R %ShellDir% %%f in ^( *.exe ^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set TestExePath=%%g
for %%a in ("%TestExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
)

set options=--name %ExeName% --num-executors 8 --executor-cores 4 --executor-memory 8G --driver-memory 12G
set options=%options% --conf spark.streaming.nao.loadExistingFiles=true 
set options=%options% --conf spark.streaming.kafka.maxRetries=300 
set options=%options% --conf "spark.yarn.executor.memoryOverhead=18000"
set options=%options% --conf spark.streaming.kafka.maxRetries=20
set options=%options% --jars %CodeRootDir%\build\dependencies\spark-streaming-kafka-assembly_2.10-1.6.1.jar
set options=%options% --conf spark.mobius.streaming.kafka.CSharpReader.enabled=true
if "%HasRIO%" == "1" set options=%options% --conf spark.mobius.CSharp.socketType=Rio

echo ### You can set SparkOptions to avoid default local mode setting. Examples : 
echo ### Cluster Mode : set SparkOptions=--master yarn-cluster --num-executors 100 --executor-cores 28 --executor-memory 30G --driver-memory 32G --conf spark.python.worker.connectionTimeoutMs=3000000 --conf spark.streaming.nao.loadExistingFiles=true --conf spark.streaming.kafka.maxRetries=300 --conf "spark.yarn.executor.memoryOverhead=18000" --conf spark.streaming.kafka.maxRetries=20  --conf spark.mobius.streaming.kafka.CSharpReader.enabled=true 
echo.
echo ### Local Mode : set SparkOptions=%options%
echo.
echo ### You can set TestExePath to avoid detected: %TestExePath% 
echo ### You can set HasRIO=1 to enable RIO socket 

rem set default SparkOptions if not empty %SparkOptions%
echo ##%SparkOptions% | findstr /I /R "[0-9a-z]" >nul || set SparkOptions=%options%

rem set spark.app.name to easy lookup from cluster and debug
if not "%spark.app.name%" == "" (
    echo %SparkOptions% | lzmw -ix "--name" -PAC && set SparkOptions=%SparkOptions% --name %spark.app.name%
    echo %SparkOptions% | lzmw -ix "--name" -PAC || for /F "tokens=*" %%a in ('echo %SparkOptions% ^| lzmw -it "(--name)\s+\S+" -o "$1 %spark.app.name%" -PAC ') do set SparkOptions=%%a
)

set CodeRootDir=%ShellDir%\..\..\..
set CommonToolDir=%ShellDir%\..\..\tools

if "%SPARK_HOME%" == "" (
    echo Not set SPARK_HOME, treat as local mode
    call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir% || exist /b 1
)

call :CheckExist %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd "sparkclr-submit.cmd" || exit /b 1
call :CheckExist %TestExePath% "TestExePath" || exit /b 1

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following, run : %TestExePath%
    call %TestExePath%
    exit /b 0
)

pushd %ExeDir%
echo %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %SparkOptions% --exe %ExeName% %CD% %AllArgs%
call %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %SparkOptions% --exe %ExeName% %CD% %AllArgs%
popd

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    goto :End
    
:End
    
