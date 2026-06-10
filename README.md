# UpGoDown — API игры «обход стула»

Клиент-серверное API (ASP.NET Core 8, PostgreSQL, Docker, Serilog → Seq, JWT). Тестирование: **Swagger** и **Postman**.

## Структура

```
UpGoDown/
  UpGoDown.sln
  docker-compose.yml        ← Postgres + Seq + Keycloak + API
  Dockerfile
  ЗАПУСК.bat                ← dotnet run (нужен Postgres на :5432)
  ЗАПУСК_DOCKER.bat
  docs/
    API.md
    POSTMAN.md
    UpGoDown.postman_collection.json
    demo-level1-try.json    ← готовый body для успешного прохождения уровня 1
  src/UpGoDown.Api/
```

## Быстрый старт (без Docker)

1. Поднимите только Postgres:
   ```powershell
   cd "C:\Users\andre\OneDrive\Рабочий стол\UpGoDown"
   docker compose up postgres -d
   ```
2. Запуск API:
   ```powershell
   cd src\UpGoDown.Api
   dotnet restore
   dotnet run
   ```
3. Браузер: **http://localhost:5000/swagger**

## Полный стек (Docker)

```powershell
cd "C:\Users\andre\OneDrive\Рабочий стол\UpGoDown"
docker compose up --build
```

| Сервис     | URL |
|------------|-----|
| Swagger    | http://localhost:5000/swagger |
| Postman    | импорт `docs/UpGoDown.postman_collection.json` — см. [POSTMAN.md](docs/POSTMAN.md) |
| Seq (логи) | http://localhost:5341 |
| Keycloak   | http://localhost:8180 (admin / admin) |
| PostgreSQL | localhost:5432, user/pass/db = `upgodown` |

## DBeaver

- Host: `localhost`, Port: `5432`
- Database: `upgodown`, User: `upgodown`, Password: `upgodown`
- Таблицы: `Users`, `LevelAttempts`

## Сценарий для защиты (Swagger)

1. **POST /register** — `{ "login": "demo", "password": "123456", "name": "Демо" }`
2. **POST /login** — `{ "login": "demo", "password": "123456" }` → скопируйте **token**
3. Swagger → **Authorize** → `Bearer <token>` (без слова Bearer дважды)
4. **GET /levels** → **GET /levels/1**
5. **POST /levels/1/try** — вставьте body из файла **`docs/demo-level1-try.json`**  
   Ожидаемый ответ: `"success": true`, `"stepsCount": 26`, `"pointsHistory": [...]`
6. **GET /myProfile** — статистика (passed по уровню 1)
7. **GET /leaderboard/levels/1** — топ (без авторизации)
8. **GET /api/health** — проверка сервиса
9. DBeaver — показать строки в `Users` и `LevelAttempts`
10. Seq — показать логи HTTP-запросов с TraceId

### Уровни 2 и 3

- Уровень 2: `"seed": 42` для воспроизводимого спавна; алгоритм нужно подобрать под сцену из ответа `scene`.
- Уровень 3: `"seed": 100` — случайные стулья и актор.

## Авторизация

Сейчас API использует **собственный JWT** (`POST /register`, `POST /login`).  
**Keycloak** поднят в Docker (порт 8180) для соответствия стеку курса; интеграция с API — опционально, на защите достаточно показать JWT + Keycloak в compose.

## Допуск к защите (скриншоты)

Чеклист: Docker, Swagger, Seq, Keycloak — см. **[docs/DOPUSK-skreens.md](docs/DOPUSK-skreens.md)**

## Полный пайплайн сдачи

Пошаговый сценарий по требованиям преподавателя: **[docs/DEFENSE-PIPELINE.md](docs/DEFENSE-PIPELINE.md)**

## Git

```powershell
cd "C:\Users\andre\OneDrive\Рабочий стол\UpGoDown"
git init
git add .
git commit -m "UpGoDown API: игра обход стула, Docker, demo уровень 1"
git remote add origin https://github.com/<ваш-логин>/UpGoDown.git
git push -u origin main
```

Создайте **новый** репозиторий на GitHub (не форк курсового).

## Уровни

| id | Описание |
|----|----------|
| 1 | Актор на своём стуле, стулья в запросе |
| 2 | Стулья фиксированы, спавн random (seed опционально) |
| 3 | Стулья и актор random, «свой» стул — ближайший по Manhattan |

## Команды алгоритма

`встать`, `идти`, `повернуть_90`, `повернуть_-90`, `повернуть_180`, `сесть`

Успех: встал → прошёл три клетки у стула партнёра (снизу, сверху, справа) → вернулся на свой стул → сел.
