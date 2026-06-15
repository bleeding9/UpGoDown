using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpGoDown.Api.Data;
using UpGoDown.Api.Services;

namespace UpGoDown.Api.Controllers;

[ApiController]
[Route("register")]
public class RegisterController(AuthService auth) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (ok, error, data) = await auth.RegisterAsync(request);
        if (!ok) return BadRequest(new { error });
        return Created("", data);
    }
}

[ApiController]
[Route("login")]
public class LoginController(AuthService auth) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (ok, error, data) = await auth.LoginAsync(request);
        if (!ok) return Unauthorized(new { error });
        return Ok(data);
    }
}

[ApiController]
[Route("levels")]
public class LevelsController(AppDbContext db, AuthService auth) : ControllerBase
{
    private static readonly (int Id, string Name, string Description)[] LevelMeta =
    [
        (1, "Базовый обход", "Фиксированная карта. Встать, обойти чужой стул, сесть на свой."),
        (2, "Случайные стулья", "Стулья спавнятся случайно."),
        (3, "Случайная карта", "Случайный размер поля + случайные стулья. За успех — скилл «идти_диагональ»."),
        (4, "Случайный спавн", "Все условия ур. 3 + актор появляется в случайной клетке."),
        (5, "Враг", "Все условия ур. 4 + враг преследует вас. HP = 3."),
    ];

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var userId = auth.GetUserId(User);
        if (userId is null) return Unauthorized();

        var hasDiagonalSkill = await LevelProgressService.HasDiagonalSkillAsync(db, userId.Value);
        var levels = new List<object>();

        foreach (var l in LevelMeta)
        {
            var hasAccess = await LevelProgressService.CanAccessLevelAsync(db, userId.Value, l.Id);
            var passed = await LevelProgressService.HasPassedLevelAsync(db, userId.Value, l.Id);
            levels.Add(new
            {
                id = l.Id,
                name = l.Name,
                description = l.Description,
                hasAccess,
                passed,
                lockedReason = hasAccess ? null : $"Сначала пройдите уровень {l.Id - 1}",
                hasEnemy = l.Id == 5,
            });
        }

        return Ok(new
        {
            skills = new
            {
                diagonalWalk = hasDiagonalSkill,
                diagonalCommand = LevelScenarioService.DiagonalCommand,
                description = hasDiagonalSkill
                    ? "Открыт после прохождения уровня 3"
                    : "Пройдите уровень 3",
            },
            levels,
        });
    }

    [HttpPost("{id:int}/try")]
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> TryLevel(
        int id,
        [FromBody] TryLevelRequest? request,
        [FromServices] LevelScenarioService scenarios,
        [FromServices] GameEngine engine,
        [FromServices] AppDbContext dbContext,
        [FromServices] AuthService authService)
    {
        request ??= new TryLevelRequest();
        if (id is < 1 or > LevelProgressService.MaxLevel)
            return NotFound(new { error = "Уровень не найден" });

        if (request.Commands is null || request.Commands.Count == 0)
            return BadRequest(new { error = "Укажите commands — список команд алгоритма" });

        var userId = authService.GetUserId(User);
        if (userId is null)
            return Unauthorized();

        if (!await LevelProgressService.CanAccessLevelAsync(dbContext, userId.Value, id))
            return BadRequest(new { error = $"Сначала пройдите уровень {id - 1}" });

        var hadDiagonalSkill = await LevelProgressService.HasDiagonalSkillAsync(dbContext, userId.Value);
        var skillForRun = hadDiagonalSkill && id >= 4;

        var build = scenarios.Build(id, request, skillForRun);
        if (!build.Ok)
            return BadRequest(new { error = build.Error });

        var result = engine.Run(build.Input!);

        var attempt = new LevelAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            LevelId = id,
            Success = result.Success,
            StepsCount = result.StepsCount,
            PointsHistoryJson = JsonSerializer.Serialize(result.PointsHistory),
            SceneJson = JsonSerializer.Serialize(build.Scene),
        };
        dbContext.LevelAttempts.Add(attempt);
        await dbContext.SaveChangesAsync();

        var skillGranted = id == LevelProgressService.SkillUnlockLevel && result.Success && !hadDiagonalSkill;

        return Ok(new
        {
            success = result.Success,
            stepsCount = result.StepsCount,
            pointsHistory = result.PointsHistory,
            scene = build.Scene,
            lines = result.Lines,
            error = result.Error,
            attemptId = attempt.Id,
            actorHp = result.ActorHp,
            maxActorHp = result.MaxActorHp,
            finalEnemy = result.FinalEnemy,
            skills = new
            {
                diagonalWalk = hadDiagonalSkill || skillGranted,
                skillGranted,
                diagonalCommand = LevelScenarioService.DiagonalCommand,
            },
        });
    }
}

[ApiController]
[Route("myProfile")]
[Authorize]
public class MyProfileController(AppDbContext db, AuthService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = auth.GetUserId(User);
        if (userId is null) return Unauthorized();

        var user = await db.Users
            .Include(u => u.Attempts)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound();

        var hasDiagonalSkill = user.Attempts.Any(a =>
            a.LevelId == LevelProgressService.SkillUnlockLevel && a.Success);

        var levels = user.Attempts
            .GroupBy(a => a.LevelId)
            .Select(g => new
            {
                levelId = g.Key,
                attempts = g.Count(),
                passed = g.Any(a => a.Success),
                bestSteps = g.Where(a => a.Success).Select(a => a.StepsCount).DefaultIfEmpty().Min(),
            })
            .OrderBy(x => x.levelId)
            .ToList();

        return Ok(new
        {
            login = user.Login,
            name = user.Name,
            role = user.Role,
            stats = new
            {
                totalAttempts = user.Attempts.Count,
                successCount = user.Attempts.Count(a => a.Success),
            },
            skills = new
            {
                diagonalWalk = hasDiagonalSkill,
                description = hasDiagonalSkill
                    ? "Ход по диагонали (команда «идти_диагональ»)"
                    : "Пройдите уровень 3",
            },
            levels,
        });
    }
}

[ApiController]
[Route("leaderboard/levels")]
public class LeaderboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("{levelId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetForLevel(int levelId)
    {
        if (levelId is < 1 or > LevelProgressService.MaxLevel)
            return NotFound(new { error = "Уровень не найден" });

        var best = await db.LevelAttempts
            .Where(a => a.LevelId == levelId && a.Success)
            .Include(a => a.User)
            .GroupBy(a => a.UserId)
            .Select(g => new
            {
                userId = g.Key,
                name = g.First().User.Name,
                stepsCount = g.Min(a => a.StepsCount),
            })
            .OrderBy(x => x.stepsCount)
            .Take(100)
            .ToListAsync();

        var ranked = best.Select((x, i) => new
        {
            rank = i + 1,
            x.name,
            stepsCount = x.stepsCount,
            passed = true,
        });

        return Ok(ranked);
    }
}

[ApiController]
[Route("teacher")]
[Authorize(Roles = UserRoles.Teacher)]
[ApiExplorerSettings(IgnoreApi = true)]
public class TeacherController(AppDbContext db) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var students = await db.Users
            .Where(u => u.Role == UserRoles.Student)
            .Include(u => u.Attempts)
            .OrderBy(u => u.Login)
            .Select(u => new
            {
                u.Login,
                u.Name,
                attempts = u.Attempts.Count,
                passedLevels = u.Attempts.Where(a => a.Success).Select(a => a.LevelId).Distinct().Count(),
            })
            .ToListAsync();

        return Ok(new
        {
            role = UserRoles.Teacher,
            studentsCount = students.Count,
            totalAttempts = await db.LevelAttempts.CountAsync(),
            students,
        });
    }
}

[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { status = "ok", service = "UpGoDown API" });
}
