# Postman — UpGoDown API

## Импорт

**Import** → `docs/UpGoDown.postman_collection.json`

## Пользователь (один)

| Поле | Значение |
|------|----------|
| login | `player` |
| password | `123456` |

Создаётся сидером при старте API или через **POST Register**.

## Коллекция

**01 — Auth:** Register, Login  
**02 — Levels:** GET levels + try для уровней 1–5 (ур. 1 — готовый demo)  
**03 — Profile & Leaderboard**

Уровни проходятся **по порядку** (2 только после 1 и т.д.).

Swagger: http://localhost:5000/swagger
