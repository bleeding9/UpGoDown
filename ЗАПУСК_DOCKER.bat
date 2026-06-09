@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === UpGoDown: Postgres + Seq + Keycloak + API ===
echo Swagger: http://localhost:5000/swagger
echo Seq:     http://localhost:5341
echo Keycloak: http://localhost:8180  (admin / admin)
docker compose up --build
pause
