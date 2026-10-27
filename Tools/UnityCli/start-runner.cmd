@echo off
title Army-System Unity CLI runner
echo Starting the Unity CLI runner (watch mode). Close this window to stop it.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0unity-cli.ps1" watch
pause
