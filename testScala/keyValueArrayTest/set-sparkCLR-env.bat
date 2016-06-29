@if "%1" == "" (
    @echo Usage   : %0  MOBIUS_CODE_ROOT        [OVERWRITE_ENV : default = 0 not ]
    @echo Example : %0  d:\msgit\qualiuMobius    0
    @exit /b 0
)

@set MOBIUS_CODE_ROOT=%1
@set OVERWRITE_ENV=%2

call :CheckExist %MOBIUS_CODE_ROOT%

@for /F "tokens=*" %%d in (' dir /A:D /B %MOBIUS_CODE_ROOT%\build\tools\spark-* ') do set SparkDir=%MOBIUS_CODE_ROOT%\build\tools\%%d

@if "%OVERWRITE_ENV%" == "1" (
    set SPARK_HOME=%SparkDir%
    set HADOOP_HOME=%MOBIUS_CODE_ROOT%\build\tools\winutils
    set SPARKCLR_HOME=%MOBIUS_CODE_ROOT%\build\runtime
) else (
    if not exist "%SPARK_HOME%"  set SPARK_HOME=%SparkDir%
    if not exist "%HADOOP_HOME%" set HADOOP_HOME=%MOBIUS_CODE_ROOT%\build\tools\winutils
    if not exist "%SPARKCLR_HOME%" set SPARKCLR_HOME=%MOBIUS_CODE_ROOT%\build\runtime
)

:CheckExist
    @if not exist %1 echo Not exist %~1 & exit /b 1