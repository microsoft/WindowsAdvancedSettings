@echo off

powershell -ExecutionPolicy Unrestricted -NoLogo -NoProfile -File %~dp0\scripts\Build.ps1 %*

exit /b %ERRORLEVEL%