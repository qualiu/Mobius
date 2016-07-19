@echo off
@setlocal enabledelayedexpansion

set ShellDir=%~dp0
if %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set CommonToolDir=%ShellDir%\..\..\tools

set lzJar=%ShellDir%\target\KafkaStreamTestOneJar.jar
if not exist %lzJar% (
    pushd %ShellDir% && call mvn package & popd
)

call :CheckExist %lzJar%
set CodeRootDir=%ShellDir%\..\..\..
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir%

call :CheckExist %SourceSocketExe%
call :CheckExist %SPARK_HOME%\bin\spark-submit.cmd

call %SPARK_HOME%\bin\spark-submit.cmd --class lzTest.KafkaStreamTest %lzJar% %*

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %1
        exit /b 1
    )

:End

