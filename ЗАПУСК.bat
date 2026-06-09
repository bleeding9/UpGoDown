@echo off
chcp 65001 >nul
cd /d "%~dp0src\UpGoDown.Api"
echo === UpGoDown: локальный запуск (нужен PostgreSQL на localhost:5432) ===
dotnet run
pause
