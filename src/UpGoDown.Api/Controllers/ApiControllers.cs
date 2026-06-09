using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpGoDown.Api.Data;
using UpGoDown.Api.Services;

namespace UpGoDown.Api.Controllers;

[ApiController]
[Route("register")]
public class RegisterController(AppDbContext db, AuthService auth) : ControllerBase
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
public class LevelsController : ControllerBase
{
    private static readonly (int Id, string Name, string Description)[] LevelMeta =
    [
        (1, "На своём стуле", "Актор сидит на своём стуле. Отправьте алгоритм обхода."),
        (2, "Случайный спавн", "Стулья фиксированы, актор появляется случайно."),
        (3, "Полный random", "Случайные стулья и актор."),
    ];

    [HttpGet]
    [Authorize]
    public IActionResult GetAll() =>
        Ok(LevelMeta.Select(l => new { id = l.Id, name = l.Name, description = l.Description, hasAccess = true }));

    [HttpGet("{id:int}")]
    [Authorize]
    public IActionResult GetById(int id)
    {
        if (id is < 1 or > 3) return NotFound(new { error = "Уровень не найден" });
        var level = LevelMeta[id - 1];

        return Ok(new
        {
            id,
            name = level.Name,
            description = level.Description,
            gridWidth = 14,
            gridHeight = 8,
            stateMapExample = id == 1
                ? new
                {
                    chairOwn = new[] { 2, 4 },
                    chairPartner = new[] { 9, 4 },
                    actor = new { x = 2, y = 4, angle = 0, sitting = true },
                }
                : (object)new { note = "Сцена генерируется при POST /levels/{id}/try" },
            allowedCommands = new[] { "встать", "идти", "повернуть_90", "повернуть_-90", "повернуть_180", "сесть" },
        });
    }

    [HttpPost("{id:int}/try")]
    [Authorize]
    public async Task<IActionResult> TryLevel(
        int id,
        [FromBody] TryLevelRequest? request,
        [FromServices] LevelScenarioService scenarios,
        [FromServices] GameEngine engine,
        [FromServices] AuthService auth,
        [FromServices] AppDbContext db)
    {
        request ??= new TryLevelRequest();
        if (request.Commands is null || request.Commands.Count == 0)
            return BadRequest(new { error = "Укажите commands — список команд алгоритма" });

        var build = scenarios.Build(id, request);
        if (!build.Ok)
            return BadRequest(new { error = build.Error });

        var result = engine.Run(build.Input!);
        var userId = auth.GetUserId(User);
        if (userId is null)
            return Unauthorized();

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
        db.LevelAttempts.Add(attempt);
        await db.SaveChangesAsync();

        return Ok(new
        {
            success = result.Success,
            stepsCount = result.StepsCount,
            pointsHistory = result.PointsHistory,
            scene = build.Scene,
            lines = result.Lines,
            error = result.Error,
            attemptId = attempt.Id,
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
            stats = new
            {
                totalAttempts = user.Attempts.Count,
                successCount = user.Attempts.Count(a => a.Success),
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
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { status = "ok", service = "UpGoDown API" });
}
