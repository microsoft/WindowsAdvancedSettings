@echo off

powershell -ExecutionPolicy Unrestricted -NoLogo -NoProfile -File %~dp0\scripts\Test.ps1 %*

exit /b %ERRORLEVEL%