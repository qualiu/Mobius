@setlocal enabledelayedexpansion
@set shellDir=%~dp0
@IF %shellDir:~-1%==\ SET shellDir=%shellDir:~0,-1%
::start cmd /k d:\msgit\SourceLinesSocket.exe 9111 100 15 127.0.0.1 60
start cmd /c d:\msgit\SourceLinesSocket.exe 9111 100 15 127.0.0.1 60
@set checkDir=d:\tmp\checkDir
rd /q /s %checkDir%
::%shellDir%\test-array-kv-stream.bat 127.0.0.1 9111 1 4 2 30 1 %checkDir% 0 1 0 1 | lzmw -it "error^|testKey\w*|exception^|arg^|\w*count\s*="
%shellDir%\test-array-kv-stream.bat 127.0.0.1 9111 1 4 2 30 1 %checkDir%  0 1 0 1 0 1
