@echo off
@setlocal enabledelayedexpansion

set ShellDir=%~dp0
if %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set CommonToolDir=%ShellDir%\..\..\tools

set lzJar=%ShellDir%\target\TxtStreamTestOneJar.jar
if not exist %lzJar% (
    pushd %ShellDir% && call mvn package & popd
)

call :CheckExist %lzJar% || exit /b 1

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following:
    java -jar %lzJar%
    exit /b 0
)

set CodeRootDir=%ShellDir%\..\..\..
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir% || exit /b 1

call :CheckExist %SPARK_HOME%\bin\spark-submit.cmd || exit /b 1

set options=--name textStreamMobius --num-executors 8 --executor-cores 4 --executor-memory 8G --driver-memory 12G
set options=%options% --conf spark.streaming.nao.loadExistingFiles=true 
set options=%options% --conf spark.streaming.kafka.maxRetries=300 
set options=%options% --conf "spark.yarn.executor.memoryOverhead=18000"
set options=%options% --conf spark.streaming.kafka.maxRetries=20
set options=%options% --conf spark.mobius.streaming.kafka.CSharpReader.enabled=true
set options=%options% --jars %CodeRootDir%\build\dependencies\spark-streaming-kafka-assembly_2.10-1.6.1.jar

call %SPARK_HOME%\bin\spark-submit.cmd %options% --class lzTest.TxtStreamTest %lzJar% %AllArgs%


:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    goto :End

:End

