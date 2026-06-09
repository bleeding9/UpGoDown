# Допуск к итоговой защите — скриншоты

Чеклист для предварительного подтверждения, что проект запускается.  
Все скрины можно сделать **за один запуск** Docker.

## Подготовка

```powershell
cd UpGoDown
docker compose up --build -d
```

Подождите 1–2 минуты, пока поднимутся все сервисы.

| Сервис     | URL |
|------------|-----|
| Swagger    | http://localhost:5000/swagger |
| Seq (логи) | http://localhost:5341 |
| Keycloak   | http://localhost:8180 |
| PostgreSQL | localhost:5432 (user/pass/db = `upgodown`) |

---

## 1. Docker-контейнеры

```powershell
docker ps
```

**Скрин:** в списке видны контейнеры:
- `upgodown-api-1`
- `upgodown-postgres-1`
- `upgodown-seq-1`
- `upgodown-keycloak-1`

Статус — `Up`.

---

## 2. Swagger

1. Откройте http://localhost:5000/swagger
2. **POST /register** — `{ "login": "demo", "password": "123456", "name": "Демо" }`
3. **POST /login** — скопируйте `token`
4. **Authorize** → `Bearer <token>`
5. **POST /levels/1/try** — body из [`demo-level1-try.json`](demo-level1-try.json)

**Скрин (любой из двух):**
- общий вид Swagger со списком методов, **или**
- ответ **POST /levels/1/try** с `"success": true` и `"stepsCount": 26`

---

## 3. Seq (логи Serilog)

Serilog пишет логи в **Seq** — это веб-интерface для просмотра.

1. Сделайте несколько запросов в Swagger (health, login, try)
2. Откройте http://localhost:5341

**Скрин:** список событий с HTTP-запросами (`HTTP GET /api/health`, `HTTP POST /login` и т.д.)

---

## 4. Keycloak

1. Откройте http://localhost:8180
2. Войдите: **admin** / **admin**

**Скрин:** главная страница админ-консоли Keycloak после входа.

> Keycloak в compose для стека курса. Авторизация в API — через JWT (`/register`, `/login`).

---

## 5. (Опционально) PostgreSQL / DBeaver

Не в минимальном списке допуска, но полезно для защиты:

- Host: `localhost`, Port: `5432`
- Database / User / Password: `upgodown`
- Таблицы: `Users`, `LevelAttempts`

**Скрин:** строки в таблицах после успешного try.

---

## Быстрый порядок «все скрины за 10 минут»

1. `docker compose up --build -d`
2. `docker ps` → скрин
3. Swagger → register, login, try level 1 → скрин
4. Seq → скрин логов
5. Keycloak → admin/admin → скрин

---

## Если что-то не работает

| Проблема | Решение |
|----------|---------|
| Swagger не открывается | Подождать 1–2 мин, `docker logs upgodown-api-1` |
| Seq пустой | Сначала запросы в Swagger, потом обновить Seq |
| Seq не стартует | `docker compose up seq -d` (в compose уже настроен `SEQ_FIRSTRUN_NOAUTHENTICATION`) |
| Порт занят | `docker compose down`, перезапустить Docker Desktop |

---

## GitHub

Репозиторий: https://github.com/bleeding9/UpGoDown
