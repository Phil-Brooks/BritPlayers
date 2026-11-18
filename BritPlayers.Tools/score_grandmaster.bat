@echo off
setlocal

rem Generate Data\pgn_in\<slug>.pgn with Scid vs PC before running.
set "PYTHON=python"
set "PLAYER_SLUG=john-m-emms"
set "STOCKFISH_PATH=D:\scid\stockfish-windows-x86-64-universal\stockfish\stockfish-windows-x86-64-universal.exe"

pushd "%~dp0"
"%PYTHON%" "%~dp0score_pgns.py" --input-file "%~dp0..\BritPlayers\Data\pgn_in\%PLAYER_SLUG%.pgn" --output-file "%~dp0..\BritPlayers\Data\pgn_out\%PLAYER_SLUG%.pgn" --stockfish "%STOCKFISH_PATH%"
rem Add --restart to the command above only when you want to discard the checkpoint and score from game 1.
set "RESULT=%ERRORLEVEL%"
popd

if not "%RESULT%"=="0" echo Scoring stopped or failed with exit code %RESULT%. Run this batch file again to resume.
echo.
pause
exit /b %RESULT%
