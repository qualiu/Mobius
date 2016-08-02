@echo off
@setlocal enabledelayedexpansion

set ShellDir=%~dp0
if %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set CommonToolDir=%ShellDir%\..\..\tools

set lzJar=%ShellDir%\target\KafkaStreamTestOneJar.jar
if not exist %lzJar% (
    pushd %ShellDir% && call mvn package & popd
)

call :CheckExist %lzJar% || exit /b 1
set CodeRootDir=%ShellDir%\..\..\..
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir% || exit /b 1

call :CheckExist %SPARK_HOME%\bin\spark-submit.cmd || exit /b 1

call %SPARK_HOME%\bin\spark-submit.cmd --class lzTest.KafkaStreamTest %lzJar% %*

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    goto :End

:End

