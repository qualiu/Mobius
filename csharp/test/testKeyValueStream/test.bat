@setlocal enabledelayedexpansion
@set shellDir=%~dp0
@IF %shellDir:~-1%==\ SET shellDir=%shellDir:~0,-1%

start cmd /c d:\msgit\SourceLinesSocket.exe -p 9112 -n 50 
%shellDir%\test-array-kv-stream.bat -p 9112 -e 1 -r 30 -b 1 -w 3 -s 3 -v 50 -d 1 -c d:\tmp\checkDir

exit /b 0
:: error if slide=6 > window=1
start cmd /c d:\msgit\SourceLinesSocket.exe -p 9112 -n 50 
%shellDir%\test-array-kv-stream.bat -p 9112 -e 1 -r 30 -b 1 -w 1 -s 6 -v 50 -d 1 -c d:\tmp\checkDir

exit /b 0
start cmd /c d:\msgit\SourceLinesSocket.exe -p 9112 -n 50 
%shellDir%\test-array-kv-stream.bat -p 9112 -e 1 -r 30 -b 1 -w 4 -s 4 -v 50 -d 1 -c d:\tmp\checkDir

exit /b 0
start cmd /c d:\msgit\SourceLinesSocket.exe -p 9112 -n 50 -z 30 -q 0 -x 3
%shellDir%\test-array-kv-stream.bat -p 9112 -e 1 -r 30 -b 1 -w 4 -s 4 -v 50 -d 1 -c d:\tmp\checkDir -t 3

D:\msgit\lqmMobius\csharp\test\testKeyValueStream\test.bat 2>&1 | lzmw -it "error|exception|fail|arg|\w*count|value = |(inverseSum)" -e Validation

::rd /q /s %checkDir%
::%shellDir%\test-array-kv-stream.bat 127.0.0.1 9112 1 4 2 30 1 %checkDir% 0 1 0 1 | lzmw -it "error^|testKey\w*|exception^|arg^|\w*count\s*="


:: d:\msgit\SourceLinesSocket.exe -p 9111 -n 50 -x 3 -z 30 -q 0
:: D:\msgit\lqmMobius\csharp\test\testKeyValueStream\test-array-kv-stream.bat -p 9111 -e 1 -r 30 -t 3 -b 1 -w 4 -s 4 -v 50 -d 1 -c d:\tmp\checkDir