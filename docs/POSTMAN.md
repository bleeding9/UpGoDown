# Postman — UpGoDown API

На защите **Postman только для register и login**. Остальные запросы — в **Swagger**: http://localhost:5000/swagger

## Импорт коллекции

1. Установите [Postman](https://www.postman.com/downloads/)
2. **Import** → **`docs/UpGoDown.postman_collection.json`**
3. Запустите проект: `docker compose up -d` (или `ЗАПУСК_DOCKER_LOCAL.bat`)

## Сценарий на защите

1. **POST Register** — login `demo1` (новый пользователь) → **201** + token  
2. **POST Login Student** — `student` / `123456` → **200** + `"role": "Student"`  
3. **POST Login Teacher** — `teacher` / `123456` → **200** + `"role": "Teacher"`  

Token сохраняется в переменную `token` — скопируйте в Swagger → **Authorize**.

## Переменные

| Переменная | Значение |
|------------|----------|
| `baseUrl` | `http://localhost:5000` |
| `token` | после Login/Register |

## Скрин для допуска

Postman: **POST Login** с ответом **200** и JSON `{ token, role }`.
