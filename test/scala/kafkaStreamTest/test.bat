@echo off
@setlocal enabledelayedexpansion

set shellDir=%~dp0
if %shellDir:~-1%==\ SET shellDir=%shellDir:~0,-1%

set CommonToolDir=%shellDir%\..\..\tools

set SocketCodeDir=%shellDir%\..\..\csharp\SourceLinesSocket
for /f %%g in (' for /R %SocketCodeDir%  %%f in ^(*.exe^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set SourceSocketExe=%%g

set lzJar=%shellDir%\target\KafkaStreamTestOneJar.jar
if not exist %lzJar% (
    pushd %shellDir% && call mvn package & popd
)

call :CheckExist %lzJar%
set CodeRootDir=%shellDir%\..\..\..
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir%

call :CheckExist %SourceSocketExe%
call :CheckExist %SPARK_HOME%\bin\spark-submit.cmd

call %SPARK_HOME%\bin\spark-submit.cmd --class lzTest.KafkaStreamTest %lzJar% %*

goto :End

:CheckExist
    if not exist %1 (
        echo Not exist %1
        exit /b 1
    )


:End
    