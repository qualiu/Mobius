@echo off
setlocal enabledelayedexpansion
set ShellDir=%~dp0
IF %ShellDir:~-1%==\ SET ShellDir=%ShellDir:~0,-1%

set KafkaToolDir=%ShellDir%\..\ReadWriteKafka
for /f %%g in (' for /R %KafkaToolDir% %%f in ^(*.exe^) do @echo %%f ^| findstr /I /C:vshost /V ^| findstr /I /C:obj /V ') do set KafkaToolExe=%%g

echo KafkaToolExe = %KafkaToolExe%
call :CheckExist %KafkaToolExe% "kafka tool exe" || exit /b 1

%KafkaToolExe% -IsWrite true -TopicIdUser id_user_1 -TopicIdCount id_count_1 -BrokerList http://localhost:9092 -Interval 0 
%KafkaToolExe% -IsWrite true -TopicIdUser id_user_2 -TopicIdCount id_count_2 -BrokerList http://localhost:9092 -Interval 0

goto :End

:CheckExist
    if not exist "%~1" (
        echo Not exist %2 : %1
        exit /b 1
    )
    goto :End
    
:End
    