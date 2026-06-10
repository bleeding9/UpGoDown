# Запуск на новом компьютере

## Ошибка NU1301 / nuget.org при `docker compose up --build`

```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json
```

**Причина:** внутри Docker-контейнера при сборке API не открывается **NuGet** (интернет, DNS, firewall, прокси универа).  
Образы Postgres/Seq/Keycloak могут скачаться, а `dotnet restore` в Dockerfile — упасть.

---

## Решение 1 — обходной путь (рекомендуется)

1. Установите **.NET 8 SDK** на Windows.
2. Дважды кликните **`ЗАПУСК_DOCKER_LOCAL.bat`**  
   или в PowerShell:

```powershell
cd UpGoDown
dotnet publish src\UpGoDown.Api\UpGoDown.Api.csproj -c Release -o publish
docker compose -f docker-compose.yml -f docker-compose.prebuilt.yml up --build -d
```

NuGet качается **на Windows**, Docker только упаковывает готовый `publish/`.

---

## Решение 2 — починить сеть Docker

Docker Desktop → **Settings** → **Docker Engine** — добавьте DNS:

```json
{
  "dns": ["8.8.8.8", "8.8.4.4"]
}
```

Apply & Restart. Затем снова:

```powershell
docker compose up --build
```

Также: другой Wi‑Fi, **hotspot с телефона**, отключить VPN.

---

## Решение 3 — с рабочего ПК (офлайн)

На компьютере, где уже собиралось:

```powershell
docker compose pull
docker compose up --build -d
docker save -o upgodown-images.tar postgres:16-alpine datalust/seq:latest quay.io/keycloak/keycloak:24.0 upgodown-api:latest
```

Флешка → на новом ПК:

```powershell
docker load -i upgodown-images.tar
docker compose up -d
```

(без `--build`)

---

## Проверка

- http://localhost:5000/swagger
- http://localhost:5341
- http://localhost:8180
