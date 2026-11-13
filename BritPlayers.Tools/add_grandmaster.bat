@echo off
setlocal

rem Edit these values before running. This starts from a Scid-exported PGN and skips the all.pgn scan.
rem Add --match-name / --photo to the Python command below if needed.
set "PYTHON=python"
set "PLAYER_NAME=Keith C Arkell"
set "PLAYER_SLUG=keith-c-arkell"
set "PLAYER_DATES=b. 1961"
set "INPUT_PGN=%~dp0..\BritPlayers\Data\pgn_in\keith-c-arkell.pgn"
set "STOCKFISH_PATH=D:\scid\stockfish-windows-x86-64-universal\stockfish\stockfish-windows-x86-64-universal.exe"

pushd "%~dp0"
"%PYTHON%" "%~dp0add_grandmaster.py" --name "%PLAYER_NAME%" --slug "%PLAYER_SLUG%" --dates "%PLAYER_DATES%" --input-pgn "%INPUT_PGN%" --stockfish "%STOCKFISH_PATH%"
set "RESULT=%ERRORLEVEL%"
popd

if not "%RESULT%"=="0" echo Workflow stopped or failed with exit code %RESULT%.
echo.
echo Press Ctrl+C during scoring to stop safely. Run this batch file again to resume.
pause
exit /b %RESULT%
