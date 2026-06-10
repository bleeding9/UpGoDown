@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === UpGoDown: сборка API на Windows, Docker без NuGet ===
echo.
echo Нужен .NET 8 SDK: https://dotnet.microsoft.com/download
echo.
dotnet publish src\UpGoDown.Api\UpGoDown.Api.csproj -c Release -o publish
if errorlevel 1 (
  echo Ошибка dotnet publish. Проверьте интернет и установку .NET 8 SDK.
  pause
  exit /b 1
)
docker compose -f docker-compose.yml -f docker-compose.prebuilt.yml up --build -d
echo.
echo Swagger: http://localhost:5000/swagger
echo Seq:     http://localhost:5341
echo Keycloak: http://localhost:8180
pause
