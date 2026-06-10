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

## Уровень 3

- Второй актор (враг) спавнится случайно и после каждой вашей команды делает шаг к вам.
- У вашего актора **3 HP**; если враг входит в вашу клетку — **−1 HP**.
- При **HP = 0** — поражение.

## Успех

- Встал со стула
- Прошёл три линии у стула партнёра (снизу, сверху, справа)
- Вернулся на свой стул и сел

## Demo (уровень 1)

Готовый JSON для **POST /levels/1/try**: см. [`demo-level1-try.json`](demo-level1-try.json)  
Ожидаемый ответ: `"success": true`, `"stepsCount": 26`.
