@echo off
SetLocal EnableDelayedExpansion
set ShellDir=%~dp0
if %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%
set CodeRootDir=%ShellDir%\..\..\..
set CommonToolDir=%ShellDir%\..\..\tools

if "%TestExePath%"=="" for /f %%g in (' for /R %ShellDir% %%f in ^( *.exe ^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set TestExePath=%%g
for %%a in ("%TestExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
)

set DefaultOptions=--num-executors 8 --executor-cores 4 --executor-memory 8G --driver-memory 10G --conf spark.streaming.nao.loadExistingFiles=true --conf spark.streaming.kafka.maxRetries=300 --conf spark.yarn.executor.memoryOverhead=18000 --conf spark.streaming.kafka.maxRetries=20 --jars %CodeRootDir%\build\dependencies\spark-streaming-kafka-assembly_2.10-1.6.1.jar --conf spark.mobius.streaming.kafka.CSharpReader.enabled=true

if "%HasRIO%" == "1" set DefaultOptions=%DefaultOptions% --conf spark.mobius.CSharp.socketType=Rio

echo ### You can set SparkOptions to avoid default local mode setting. Examples :
echo ### Cluster Mode : set SparkOptions=--master yarn-cluster --num-executors 100 --executor-cores 28 --executor-memory 30G --driver-memory 32G --conf spark.python.worker.connectionTimeoutMs=3000000 --conf spark.streaming.nao.loadExistingFiles=true --conf spark.streaming.kafka.maxRetries=300 --conf spark.yarn.executor.memoryOverhead=18000 --conf spark.streaming.kafka.maxRetries=20  --conf spark.mobius.streaming.kafka.CSharpReader.enabled=true
echo.
echo ### Local Mode : set SparkOptions=%DefaultOptions%
echo.
echo ### You can set TestExePath to avoid detected: %TestExePath% 
echo ### You can set HasRIO=1 to enable RIO socket 

rem set default SparkOptions if not empty %SparkOptions%
echo ##%SparkOptions% | findstr /I /R "[0-9a-z]" >nul || set SparkOptions=%DefaultOptions%
rem append name to make application name more clear
for /F "tokens=*" %%a in ('echo %SparkOptions% ^| lzmw -it "--([a-z])\w+-(\w)\w+\S*\s+(\d+\w)\w*" -o "-$1$2-$3" -PAC ^| lzmw -it "--\S+\s+\S+(\s*\w+)" -o "" -PAC -a ^| lzmw -t "\s+" -o "_" -PAC') do set appendName=%%a

if not "%spark.app.name%" == "" (
    set appNameOption=--name %spark.app.name%
) else (
    if not "%ExeName%" == "" set appNameOption=--name %ExeName%__%appendName%
)

echo ## You can set spark.app.name to easy lookup from cluster and debug , current : spark.app.name=%spark.app.name%  ;  %appNameOption%
if not "%spark.app.name%" == "" (
    rem for /F "tokens=*" %%a in ('echo %SparkOptions% ^| lzmw -it "--name\s+(\S+|"[^\"]+")" -o "" -PAC ') do set SparkOptions=%%
    for /F "tokens=*" %%a in ('echo %SparkOptions% ^| lzmw -it "--name\s+(\S+)" -o "" -PAC ') do set SparkOptions=%%a
    call set SparkOptions=!SparkOptions! --name %spark.app.name%
)

echo %SparkOptions% | findstr /I /R "\s*--name[^a-z0-9_-]" >nul || set SparkOptions=%SparkOptions% %appNameOption%

echo Current SparkOptions=%SparkOptions%

if "%SPARK_HOME%" == "" (
    echo Not set SPARK_HOME, treat as local mode
    call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir% || exist /b 1
)

call %CommonToolDir%\bat\check-exist-path.bat %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd "sparkclr-submit.cmd" || exit /b 1
call %CommonToolDir%\bat\check-exist-path.bat %TestExePath% "TestExePath" || exit /b 1

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
