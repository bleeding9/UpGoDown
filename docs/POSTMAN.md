# Postman — UpGoDown API

Параллельно со **Swagger** (http://localhost:5000/swagger) можно тестировать API в Postman.

## Импорт коллекции

1. Установите [Postman](https://www.postman.com/downloads/)
2. **Import** → выберите файл **`docs/UpGoDown.postman_collection.json`**
3. Запустите проект: `docker compose up` или `ЗАПУСК_DOCKER_LOCAL.bat`

## Быстрый сценарий

1. **POST Login** (или Register) — token сохранится автоматически в переменную `token`
2. **GET Levels** — список уровней и скиллов
3. **POST Level 1 Try (demo)** — `"success": true`, `"stepsCount": 26`
4. **GET My Profile** — статистика и скилл `diagonalWalk`
5. **GET Leaderboard Level 1** — топ (без auth)

## Переменные коллекции

| Переменная | Значение по умолчанию |
|------------|------------------------|
| `baseUrl` | `http://localhost:5000` |
| `token` | заполняется после Login/Register |

Если API на другом порту — измените `baseUrl` в коллекции (⋯ → Edit → Variables).

## Скрин для допуска

Достаточно скрина **POST Login** или **POST Level 1 Try** с ответом **200 OK** и JSON-телом.
