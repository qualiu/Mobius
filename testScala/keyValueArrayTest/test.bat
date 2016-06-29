@setlocal enabledelayedexpansion
@set shellDir=%~dp0
@if %shellDir:~-1%==\ SET shellDir=%shellDir:~0,-1%

@for /f %%g in (' for /R %shellDir%\..\..\csharp\test\SourceLinesSocket %%f in ^(*.exe^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set SourceSocketExe=%%g
@set lzJar=%shellDir%\target\KeyValueArrayTestOneJar.jar

@set AllArgs=%*
@if "%1" == "" (
    echo No parameter, Usage as following:
    java -jar %lzJar%
    echo Example parameter : -p 9486 -r 30 -b 1 -w 3 -s 3 -v 50 -c d:\tmp\checkDir -d
    echo Parameters like host, port and validation are according to source socket tool : %SourceSocketExe%
    echo Source socket directory : %shellDir%\..\..\csharp\test\SourceLinesSocket
    exit /b 0
)

@call %shellDir%\set-sparkCLR-env.bat %shellDir%\..\..
@call :CheckExist %SourceSocketExe%
@call :CheckExist %SPARK_HOME%\bin\spark-submit.cmd
@call :CheckExist %ExePath%

set Port=9486
call :GetPort %*

start cmd /c "%SourceSocketExe%" -p %Port% -n 50

call %SPARK_HOME%\bin\spark-submit.cmd --class lzTest.KeyValueArrayTest %lzJar% %AllArgs%

@echo More source socket usages just run : %SourceSocketExe%
@echo Test tool Usage : java -jar %lzJar%

:GetPort
    if "%1" == ""  goto :End
    if "%1" == "-p" (
        set Port=%2
        goto :End
    )

    if "%1" == "-Port" (
        set Port=%2
        goto :End
    )
    shift
    goto :GetPort


:CheckExist
    @if not exist %1 echo Not exist %~1 & exit /b 1

:End
    
::| lzmw -it "error^|exception^|fail^|arg^|\w*count\s*="
:: test.cmd | lzmw -it "error|exception|fail|arg|\w*count|value = |(inverseSum)" -e Validation

:: validation will be error 
:: set AllArgs=-p 9486 -r 30 -b 1 -w 6 -s 2 -v 50 -c d:\tmp\checkDir -d

::%SPARK_HOME%\bin\spark-submit.cmd --class lzTest.KeyValueArrayTest %lzJar% %AllArgs% 2>&1