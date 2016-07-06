@echo off
rem to fix problems of bat (such as cannot find lable of sub funciton) : unix2dos *.bat
:: lzmw -f "\.bat$" -it "^\s*(@?echo)\s+off\b" -o "$1 on" -N 9 -R -p .
:: lzmw -f "\.bat$" -it "^\s*(@?echo)\s+on\b" -o "$1 off" -N 9 -R -p .
SetLocal EnableExtensions EnableDelayedExpansion
set ShellDir=%~dp0
if %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set AppDir=%ShellDir%\apps
dir /A:D /b %ShellDir%\apps\kafka* 2>nul
if %errorlevel% NEQ 0 ( 
    call %ShellDir%\download-kafka-zookeeper.bat %AppDir% 
    sleep 2
)

for /F "tokens=*" %%d in (' dir /A:D /B %AppDir%\kafka* ') do set KafkaRoot=%AppDir%\%%d
call :CheckExist %KafkaRoot%  "kafka"

echo ========= start zookeeper and Kafka in %KafkaRoot% ======

pushd %KafkaRoot%
set KafkaBin=%KafkaRoot%\bin\windows
start %KafkaBin%\zookeeper-server-start.bat config\zookeeper.properties
sleep 2
start %KafkaBin%\kafka-server-start.bat config\server.properties
popd

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    goto :End
    
:End

