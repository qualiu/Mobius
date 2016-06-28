@setlocal enabledelayedexpansion
@set shellDir=%~dp0
@IF %shellDir:~-1%==\ SET shellDir=%shellDir:~0,-1%

call d:\msgit\set-env-sparkCLR.bat d:\msgit\lqmMobius
start cmd /c d:\msgit\SourceLinesSocket.exe -p 9486 -n 50 

:: error 
::@set args=-p 9486 -r 30 -b 1 -w 1 -s 6 -v 50 -c d:\tmp\checkDir -d

set args=-p 9486 -r 30 -b 1 -w 3 -s 3 -v 50 -c d:\tmp\checkDir -d
%SPARK_HOME%\bin\spark-submit.cmd --class lzTest.KeyValueArrayTest %shellDir%\target\KeyValueArrayTestOneJar.jar %args% 2>&1 
::| lzmw -it "error^|exception^|fail^|arg^|\w*count\s*="
:: test.cmd | lzmw -it "error|exception|fail|arg|\w*count|value = |(inverseSum)" -e Validation