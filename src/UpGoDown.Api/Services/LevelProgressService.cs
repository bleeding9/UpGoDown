using Microsoft.EntityFrameworkCore;
using UpGoDown.Api.Data;

namespace UpGoDown.Api.Services;

public static class LevelProgressService
{
    public const int MaxLevel = 5;
    public const int SkillUnlockLevel = 3;

    public static Task<bool> HasPassedLevelAsync(AppDbContext db, Guid userId, int levelId) =>
        db.LevelAttempts.AnyAsync(a => a.UserId == userId && a.LevelId == levelId && a.Success);

    public static async Task<bool> CanAccessLevelAsync(AppDbContext db, Guid userId, int levelId)
    {
        if (levelId is < 1 or > MaxLevel) return false;
        if (levelId == 1) return true;
        return await HasPassedLevelAsync(db, userId, levelId - 1);
    }

    public static Task<bool> HasDiagonalSkillAsync(AppDbContext db, Guid userId) =>
        HasPassedLevelAsync(db, userId, SkillUnlockLevel);
}
