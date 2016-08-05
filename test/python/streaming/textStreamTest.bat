@echo off
SetLocal EnableDelayedExpansion

set localModeOptions=--num-executors 8 --executor-cores 8 --executor-memory 8G --driver-memory 8G  --conf spark.yarn.executor.memoryOverhead=18000
echo ### You can set SparkOptions to avoid default like : 
echo ### Cluster Mode : set SparkOptions=--master yarn-cluster --num-executors 100 --executor-cores 28 --executor-memory 30G --driver-memory 32G --conf spark.python.worker.connectionTimeoutMs=3000000 --conf spark.streaming.kafka.maxRetries=300 --conf spark.yarn.executor.memoryOverhead=18000 --conf spark.streaming.kafka.maxRetries=20 --conf spark.yarn.appMasterEnv.PYSPARK_PYTHON=d:/data/anaconda2/python.exe
echo.
echo ### Local Mode : Set SparkOptions=%localModeOptions%
rem set default SparkOptions if not empty %SparkOptions%
echo ##%SparkOptions% | findstr /I /R "[0-9a-z]" >nul || set SparkOptions=%localModeOptions%

set ShellDir=%~dp0
if %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set CodeRootDir=%ShellDir%\..\..\..
set CommonToolDir=%ShellDir%\..\..\tools

if "%SPARK_HOME%" == "" (
    echo Not set SPARK_HOME, treat as local mode
    call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir% || exit /b 1
    if not "%PythonExe%" == "" for /F "tokens=*" %%f in ('where python.exe 2^>nul ') do set PythonExe=%%f
)

set TestExePath=%ShellDir%\textStreamTest.py
for %%a in ("%TestExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
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
echo %SPARK_HOME%\bin\spark-submit.cmd %SparkOptions% %ExeName% %AllArgs%
call %SPARK_HOME%\bin\spark-submit.cmd %SparkOptions% %ExeName% %AllArgs%
popd

