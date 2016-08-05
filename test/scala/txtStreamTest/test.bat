@echo off
@SetLocal EnableDelayedExpansion

set ShellDir=%~dp0
if %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set CodeRootDir=%ShellDir%\..\..\..
set CommonToolDir=%ShellDir%\..\..\tools

set lzJar=%ShellDir%\target\TxtStreamTestOneJar.jar
if not exist %lzJar% (
    pushd %ShellDir% && call mvn package & popd
)

call %CommonToolDir%\bat\check-exist-path.bat %lzJar% || exit /b 1

if not "%spark.app.name%" == "" (
    set appNameOption=--name %spark.app.name%
) else (
    if not "%ExeName%" == "" set appNameOption=--name %ExeName%
)

set DefaultOptions=--num-executors 8 --executor-cores 4 --executor-memory 8G --driver-memory 10G --conf spark.streaming.nao.loadExistingFiles=true --conf spark.streaming.kafka.maxRetries=300 --conf spark.yarn.executor.memoryOverhead=18000 --conf spark.streaming.kafka.maxRetries=20 --jars %CodeRootDir%\build\dependencies\spark-streaming-kafka-assembly_2.10-1.6.1.jar --conf spark.mobius.streaming.kafka.CSharpReader.enabled=true %appNameOption%

echo ### You can set SparkOptions to avoid default local mode setting. Examples :
echo ### Cluster Mode : set SparkOptions=--master yarn-cluster --num-executors 100 --executor-cores 28 --executor-memory 30G --driver-memory 32G --conf spark.python.worker.connectionTimeoutMs=3000000 --conf spark.streaming.nao.loadExistingFiles=true --conf spark.streaming.kafka.maxRetries=300 --conf spark.yarn.executor.memoryOverhead=18000 --conf spark.streaming.kafka.maxRetries=20  --conf spark.mobius.streaming.kafka.CSharpReader.enabled=true %appNameOption%
echo.
echo ### Local Mode : set SparkOptions=%DefaultOptions%
echo.

rem set default SparkOptions if not empty %SparkOptions%
echo ##%SparkOptions% | findstr /I /R "[0-9a-z]" >nul || set SparkOptions=%DefaultOptions%

echo ## You can set spark.app.name to easy lookup from cluster and debug , current : %spark.app.name%
if not "%spark.app.name%" == "" (
    rem for /F "tokens=*" %%a in ('echo %SparkOptions% ^| lzmw -it "--name\s+(\S+|"[^\"]+")" -o "" -PAC ') do set SparkOptions=%%
    for /F "tokens=*" %%a in ('echo %SparkOptions% ^| lzmw -it "--name\s+(\S+)" -o "" -PAC ') do set SparkOptions=%%a
    call set SparkOptions=!SparkOptions! --name %spark.app.name%
)

echo SparkOptions=%SparkOptions%

if "%SPARK_HOME%" == "" (
    echo Not set SPARK_HOME, treat as local mode
    call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir% || exist /b 1
)

call %CommonToolDir%\bat\check-exist-path.bat %SPARK_HOME%\bin\spark-submit.cmd || exit /b 1

call %SPARK_HOME%\bin\spark-submit.cmd %SparkOptions% --class lzTest.TxtStreamTest %lzJar% %*
