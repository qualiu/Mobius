if not exist "%~1" (
    echo Not exist %2: %1
    exit /b 1
)
exit /b 0