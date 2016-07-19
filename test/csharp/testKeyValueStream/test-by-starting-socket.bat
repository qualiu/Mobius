@echo off
setlocal enabledelayedexpansion
set ShellDir=%~dp0
IF %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set SocketCodeDir=%ShellDir%\..\SourceLinesSocket
for /f %%g in (' for /R %SocketCodeDir% %%f in ^(*.exe^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set SourceSocketExe=%%g

for /f %%g in (' for /R %ShellDir% %%f in ^( *.exe ^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set TestExePath=%%g
for %%a in ("%TestExePath%") do ( 
    set ExeDir=%%~dpa
    set ExeName=%%~nxa
)

set CodeRootDir=%ShellDir%\..\..\..
set CommonToolDir=%ShellDir%\..\..\tools
call %CommonToolDir%\set-sparkCLR-env.bat %CodeRootDir%

call :CheckExist %SourceSocketExe% "SourceSocketExe"
call :CheckExist %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd "sparkclr-submit.cmd"
call :CheckExist %TestExePath% "TestExePath"
call :CheckExist %ExeDir% "ExeDir"

set AllArgs=%*
if "%1" == "" (
    echo No parameter, Usage as following, run : %TestExePath%
    call %TestExePath%
    echo Example parameters : -p 9112 -e 1 -r 30 -b 1 -w 3 -s 3 -v 50 -c d:\tmp\checkDir -d 1 
    echo Parameters like host, port and validation are according to source socket tool : %SourceSocketExe%
    echo Test usage just run : %TestExePath%
    exit /b 0
)

set Port=9333
set ValidationLines=60
call :ExtractArgs %*

rem use cmd /k if you want to keep the window
start cmd /c "%SourceSocketExe%" -p %Port% -n %ValidationLines%

pushd %ExeDir%
set options=--executor-cores 2 --driver-cores 2 --executor-memory 1g --driver-memory 1g
call %SPARKCLR_HOME%\scripts\sparkclr-submit.cmd %options% --exe %ExeName% %CD% %AllArgs%
popd

echo ======================================================
echo More source socket usages just run : %SourceSocketExe%
echo Test tool usages just run : %TestExePath%

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
        echo Not exist %2 : %1
        exit /b 1
    )
    


    
 
:: ======== examples ==================================================
exit /b 0

start cmd /c %SourceSocketExe% -p 9112 -n 50 
%ShellDir%\test-array-kv-stream.bat -p 9112 -e 1 -r 30 -b 1 -w 3 -s 3 -v 50 -d 1 -c d:\tmp\checkDir

:: error if slide=6 > window=1
start cmd /c %SourceSocketExe% -p 9112 -n 50 
%ShellDir%\test-array-kv-stream.bat -p 9112 -e 1 -r 30 -b 1 -w 1 -s 6 -v 50 -d 1 -c d:\tmp\checkDir

start cmd /c %SourceSocketExe% -p 9112 -n 50 
%ShellDir%\test-array-kv-stream.bat -p 9112 -e 1 -r 30 -b 1 -w 4 -s 4 -v 50 -d 1 -c d:\tmp\checkDir


start cmd /c %SourceSocketExe% -p 9112 -n 50 -z 30 -q 0 -x 3
%ShellDir%\test-array-kv-stream.bat -p 9112 -e 1 -r 30 -b 1 -w 4 -s 4 -v 50 -d 1 -c d:\tmp\checkDir -t 3

D:\msgit\lqmMobius\csharp\test\testKeyValueStream\test.bat 2>&1 | lzmw -it "error|exception|fail|arg|\w*count|value = |(inverseSum)" -e Validation

cd d:\msgit\lqmMobius\test\csharp\testKeyValueStream\bin\Debug
d:\msgit\lqmMobius\test\tools\set-sparkCLR-env.bat d:\msgit\lqmMobius
%SPARKCLR_HOME%\scripts\sparkclr-submit.cmd --exe testKeyValueStream.exe %CD% -c d:\tmp\checkDir -d 1 -p 9112 -r 360 -e 3 -b 1 -w 4 -s 1  2>&1 | lzmw --nt "input args:" -it "error|exception|fail|arg|\w*count|value = |(inverseSum)|END_OF_DATA_SECTION|unexpected valueLength" -e Validation


::=== following are test commands example ==========================================
exit /b 0
:: Enable/Disable echo in *.cmd files for debug : use -R to replace ; Without -R to preview
lzmw -f "\.cmd$" -d "tools|scripts|localmode" -it "^(\s*?\s*echo\s+)off" -o "$1 on" -rp d:\msgit\lqmMobius  -R
lzmw -f "\.cmd$" -d "tools|scripts|localmode" -it "^(\s*?\s*echo\s+)on" -o "$1 off" -rp d:\msgit\lqmMobius  -R

:: Start source stream socket and test
d:\msgit\lqmMobius\csharp\test\SourceLinesSocket\bin\Debug\SourceLinesSocket.exe 9111 100 0
d:\msgit\lqmMobius\csharp\test\testKeyValueStream\test-array-kv-stream.bat -p 9111 -b 1 -r 30 -t 20 -c checkDir -e 10485760  2>d:\tmp\logKvError.log > d:\tmp\logKvOut.log

:: Find log in directory with above logs in directory d:\tmp
lzmw -p d:\tmp -f "^logKv.*\.log$" -it "cannot|fail|error|exception" --nt "sleep interrupted|goto|echo\s+" -U 9 -D 9 -c
lzmw -p d:\tmp -f logKv -it "thread st\w+|released [1-9]\d*|alive objects|used \d+|(begin|end) of|dispose"

:: Find latest log ignore file name under Cygwin on Windows in directory /cygdrive/d/tmp/ 
lzmw -c --w1 "$(lzmw -l --wt -T 2 -PIC 2>/dev/null | head -n 1 | awk -F '\t' '{print $1}' )" -it "cannot|fail|error|exception" --nt "sleep interrupted|goto|echo\s+" -U 9 -D 9
lzmw -c --w1 "$(lzmw -l --wt -T 2 -PIC 2>/dev/null | head -n 1 | awk -F '\t' '{print $1}' )" -it "thread st\w+|dispose|released [1-9]\d*|finished all" -e "\d+ alive"
lzmw -c --w1 "$(lzmw -l --wt -T 2 -PIC 2>/dev/null | head -n 1 | awk -F '\t' '{print $1}' )" -it "JVMObjectTracker" -H 3 -T 3
lzmw -c --w1 "$(lzmw -l --wt -T 2 -PIC 2>/dev/null | head -n 1 | awk -F '\t' '{print $1}' )" -it "thread st\w+|released [1-9]\d*|alive objects|used \d+|(begin|end) of|dispose"

:: Kill test process under Cygwin on Windows
for pid in $(wmic process get processid, name | lzmw -it '^.*testKeyValueStream.exe\s+(\d+).*$' -o '$1' -PAC); do taskkill /f /pid $pid ; done

:End

