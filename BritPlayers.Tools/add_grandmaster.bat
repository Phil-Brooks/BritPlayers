@echo off
setlocal

rem Edit these values before running. Add --match-name / --photo to the Python command below if needed.
set "PYTHON=python"
set "PLAYER_NAME=Example Name"
set "PLAYER_SLUG=example-name"
set "PLAYER_DATES=b. 19xx"
set "STOCKFISH_PATH=D:\scid\stockfish-windows-x86-64-universal\stockfish\stockfish-windows-x86-64-universal.exe"

pushd "%~dp0"
"%PYTHON%" "%~dp0add_grandmaster.py" --name "%PLAYER_NAME%" --slug "%PLAYER_SLUG%" --dates "%PLAYER_DATES%" --stockfish "%STOCKFISH_PATH%"
set "RESULT=%ERRORLEVEL%"
popd

if not "%RESULT%"=="0" echo Workflow stopped or failed with exit code %RESULT%.
echo.
echo Press Ctrl+C during scoring to stop safely. Run this batch file again to resume.
pause
exit /b %RESULT%
