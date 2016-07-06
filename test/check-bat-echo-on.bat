@lzmw -f "\.bat$" -rp %CD% -it "^\s*(@?echo)\s+on\b" -N 30 -c
@echo.
@echo to replace use following command:
@echo lzmw -f "\.bat$" -it "^\s*(@?echo)\s+on\b" -o "$1 off" -rp %CD% -N 30 -R
@echo.