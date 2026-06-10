-- UpGoDown: запросы для DBeaver (защита)
-- Подключение: localhost:5432, database/user/password = upgodown

-- 1. Миграции EF Core
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";

-- 2. Схема таблиц
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
ORDER BY table_name;

-- 3. Пользователи (роли Student / Teacher)
SELECT "Id", "Login", "Name", "Role", "CreatedAt"
FROM "Users"
ORDER BY "Role", "Login";

-- 4. Все попытки прохождения
SELECT a."Id", u."Login", u."Role", a."LevelId", a."Success", a."StepsCount", a."CreatedAt"
FROM "LevelAttempts" a
JOIN "Users" u ON u."Id" = a."UserId"
ORDER BY a."CreatedAt" DESC;

-- 5. Успешные прохождения по уровням
SELECT a."LevelId", COUNT(*) AS successes
FROM "LevelAttempts" a
WHERE a."Success" = true
GROUP BY a."LevelId"
ORDER BY a."LevelId";

-- 6. Топ студентов (как leaderboard)
SELECT u."Name", MIN(a."StepsCount") AS best_steps, a."LevelId"
FROM "LevelAttempts" a
JOIN "Users" u ON u."Id" = a."UserId"
WHERE a."Success" = true AND u."Role" = 'Student'
GROUP BY u."Name", a."LevelId", a."UserId"
ORDER BY a."LevelId", best_steps;
