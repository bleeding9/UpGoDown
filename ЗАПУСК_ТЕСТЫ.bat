@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === UpGoDown: запуск unit-тестов ===
dotnet test UpGoDown.sln --verbosity normal
pause
