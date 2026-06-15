# Пайплайн сдачи UpGoDown

Пошаговый сценарий по требованиям защиты.

---

## 0. Перед защитой (дома)

```powershell
cd UpGoDown
git pull
docker compose down -v
docker compose up --build -d
```

> `down -v` — чистая БД для **миграций EF Core** (таблица `__EFMigrationsHistory`).

---

## 1. Миграции БД

При старте API автоматически: `db.Database.Migrate()`.

**DBeaver** → выполнить:

```sql
SELECT * FROM "__EFMigrationsHistory";
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';
```

Показать: миграция `20250610120000_InitialCreate`, таблицы `Users`, `LevelAttempts`.

Файлы миграций: `src/UpGoDown.Api/Migrations/`

---

## 2. Docker Compose

```powershell
docker compose up --build -d
docker ps
```

**Скрин Docker Desktop:** контейнеры `api`, `postgres`, `seq`, `keycloak`.

| URL | Назначение |
|-----|------------|
| http://localhost:5000/swagger | Swagger |
| http://localhost:5341 | Seq (логи Serilog) |
| http://localhost:8180 | Keycloak (admin/admin) |

---

## 3. DBeaver

Подключение: `localhost:5432`, db/user/pass = `upgodown`

Открыть **`docs/dbeaver-demo.sql`** — выполнить запросы по очереди.

Показать: пользователь `player`, попытки после игры.

---

## 4. Postman — register и login

Импорт: **`docs/UpGoDown.postman_collection.json`**

### Студент (Postman — обязательно по требованию)

1. **POST Register** — `{ "login": "player", "password": "123456", "name": "Игрок", "role": "Student" }`  
   (или сразу Login, если пользователь уже в БД)
2. **POST Login** → `{ "token", "role" }`

---

## 5. Swagger — сценарии ролей

### Swagger (Student token)

1. GET `/levels` — 5 уровней, `hasAccess` / `lockedReason`
2. POST `/levels/1/try` — body из `docs/demo-level1-try.json` → `success: true`
3. POST `/levels/2/try` … `/levels/5/try` — только после прохождения предыдущего
4. GET `/myProfile`

> Скилл `идти_диагональ` открывается после **уровня 3**. Враг — только на **уровне 5**.

### Swagger (Teacher token)

1. GET `/leaderboard/levels/1` — топ (можно и без token)

---

## 6. Serilog / Seq — TraceId

1. Сделать несколько запросов в Swagger/Postman
2. Открыть http://localhost:5341
3. Найти поле **TraceId** в событии HTTP-запроса
4. Показать: один TraceId на один запрос, фильтр по TraceId = «сессия» запроса

---

## 7. Keycloak

http://localhost:8180 → admin / admin → скрин консоли.

**Сказать:** «JWT в API сейчас через `/login`; Keycloak в compose для стека курса».

---

## 8. Стабильность

- Не вызывать методы без token там, где нужен Bearer
- `/levels/{id}/try` — только роль **Student**
- После ошибки — следующий запрос должен работать (не падать API)

---

## 9. GitHub

https://github.com/bleeding9/UpGoDown

---

## Шпаргалка «что говорить»

> UpGoDown — API игры «обход стула». Student проходит уровни алгоритмом, Teacher смотрит overview и leaderboard. PostgreSQL + EF migrations, Docker, Serilog→Seq с TraceId, JWT, Swagger и Postman.
