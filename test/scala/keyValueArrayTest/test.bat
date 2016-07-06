@echo off
@setlocal enabledelayedexpansion

set shellDir=%~dp0
if %shellDir:~-1%==\ SET shellDir=%shellDir:~0,-1%

set CommonToolDir=%shellDir%\..\..\tools

set SocketCodeDir=%shellDir%\..\..\csharp\SourceLinesSocket
for /f %%g in (' for /R %SocketCodeDir%  %%f in ^(*.exe^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set SourceSocketExe=%%g

set lzJar=%shellDir%\target\KeyValueArrayTestOneJar.jar
if not exist %lzJar% (
    pushd %shellDir% && call mvn package & popd
)

call :CheckExist %lzJar%

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following:
    java -jar %lzJar%
    echo Example parameter : -p 9486 -r 30 -b 1 -w 3 -s 3 -v 50 -c d:\tmp\checkDir -d
    echo Parameters like host, port and validation are according to source socket tool : %SourceSocketExe%
    echo Source socket directory : %SocketCodeDir%
    exit /b 0
)

set CodeRootDir=%shellDir%\..\..\..
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir%

call :CheckExist %SourceSocketExe%
call :CheckExist %SPARK_HOME%\bin\spark-submit.cmd


set Port=9486
set ValidationLines=60
call :ExtractArgs %*

start cmd /k "%SourceSocketExe%" -p %Port% -n %ValidationLines%

call %SPARK_HOME%\bin\spark-submit.cmd --class lzTest.KeyValueArrayTest %lzJar% %AllArgs%

echo ======================================================
echo More source socket usages just run : %SourceSocketExe%
echo Test tool Usage just run : java -jar %lzJar%

goto :End


:ExtractArgs
    if "%1" == ""  goto :End
    if "%1" == "-p" (
        set Port=%2
    )
    if "%1" == "-Port" (
        set Port=%2
    )
    if "%1" == "-v" (
        set ValidationLines=%2
    )
    if "%1" == "ValidateCount" (
        set ValidationLines=%2
    )
    shift
    goto :ExtractArgs
    

:CheckExist
    if not exist "%~1" (
        echo Not exist %1
        exit /b 1
    )
    goto :End


    
    
::| lzmw -it "error^|exception^|fail^|arg^|\w*count\s*="
:: test.cmd | lzmw -it "error|exception|fail|arg|\w*count|value = |(inverseSum)" -e Validation

:: validation will be error 
:: set AllArgs=-p 9486 -r 30 -b 1 -w 6 -s 2 -v 50 -c d:\tmp\checkDir -d

::%SPARK_HOME%\bin\spark-submit.cmd --class lzTest.KeyValueArrayTest %lzJar% %AllArgs% 2>&1

:End

