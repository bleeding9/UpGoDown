# API UpGoDown

| Метод | Путь | Auth | Описание |
|-------|------|------|----------|
| POST | `/register` | нет | `{ login, password, name }` |
| POST | `/login` | нет | `{ login, password }` → `{ token, name, login }` |
| GET | `/levels` | Bearer | Список 3 уровней |
| GET | `/levels/{id}` | Bearer | Описание уровня + пример карты |
| POST | `/levels/{id}/try` | Bearer | `{ gridWidth, gridHeight, chairOwn?, chairPartner?, seed?, commands[] }` → success, stepsCount, pointsHistory |
| GET | `/myProfile` | Bearer | Статистика пользователя |
| GET | `/leaderboard/levels/{id}` | нет | Топ-100 по уровню |
| GET | `/api/health` | нет | Проверка сервиса |

## Команды алгоритма

`встать`, `идти`, `повернуть_90`, `повернуть_-90`, `повернуть_180`, `сесть`

После **успешного прохождения уровня 1** — скилл: `идти_диагональ` (ход по диагонали вперёд-вправо относительно взгляда).

## Demo (уровень 3)

1. Сначала **пройдите уровень 1** (скилл `идти_диагональ`).
2. Используйте **`seed: 7`** и команды из [`demo-level3-try-seed7.json`](demo-level3-try-seed7.json).

> `seed: 100` даёт другую карту; старый demo для 100 был неверный. Seed 100 с врагом, по расчёту, **не проходится** — для demo берите **seed 7**.

## Успех

- Встал со стула
- Прошёл три линии у стула партнёра (снизу, сверху, справа)
- Вернулся на свой стул и сел

## Demo (уровень 1)

Готовый JSON для **POST /levels/1/try**: см. [`demo-level1-try.json`](demo-level1-try.json)  
Ожидаемый ответ: `"success": true`, `"stepsCount": 26`.

## Postman

Коллекция: [`UpGoDown.postman_collection.json`](UpGoDown.postman_collection.json) — инструкция в [`POSTMAN.md`](POSTMAN.md).
