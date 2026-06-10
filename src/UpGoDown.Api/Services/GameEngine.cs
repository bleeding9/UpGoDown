namespace UpGoDown.Api.Services;

public sealed class GameEngine
{
    private enum StepResult { Wait, Success, Error }

    public SimulationResult Run(SimulationInput input)
    {
        var w = input.GridWidth;
        var h = input.GridHeight;
        var chairOwn = (input.ChairOwnX, input.ChairOwnY);
        var chairPartner = (input.ChairPartnerX, input.ChairPartnerY);
        var commands = input.Commands ?? [];

        var actor = new ActorState(input.SpawnX, input.SpawnY, input.SpawnAngle);
        var state = new EngineState(w, h, chairOwn, chairPartner, input.SittingAtStart)
        {
            DiagonalSkillEnabled = input.DiagonalSkillEnabled,
            LevelId = input.LevelId,
            ActorHp = input.LevelId == 3 ? input.ActorHp : 0,
        };

        if (input.LevelId == 3 && input.EnemySpawnX is int ex && input.EnemySpawnY is int ey)
            state.Enemy = new ActorState(ex, ey, 0);

        state.Path.Add((actor.X, actor.Y));
        state.History.Add((actor.X, actor.Y));

        var index = 0;
        while (true)
        {
            if (state.HasError)
                return BuildResult(false, state, actor, "Ошибка выполнения команд");

            if (state.LevelId == 3 && state.ActorHp <= 0)
                return BuildResult(false, state, actor, "Актор побеждён — HP = 0");

            if (index >= commands.Count)
            {
                var ok = state.StoodUp && state.Sitting
                    && actor.X == chairOwn.Item1 && actor.Y == chairOwn.Item2
                    && state.LineYellow && state.LineOrange && state.LineGreen
                    && (state.LevelId != 3 || state.ActorHp > 0);
                return BuildResult(ok, state, actor, ok ? null : "Условия успеха не выполнены");
            }

            var cmd = commands[index++];
            var step = ExecuteCommand(cmd, actor, state, chairOwn);
            if (step == StepResult.Error)
                return BuildResult(false, state, actor, $"Ошибка на команде: {cmd}");

            if (state.LevelId == 3 && state.Enemy is not null)
            {
                MoveEnemy(state, actor);
                if (state.ActorHp <= 0)
                    return BuildResult(false, state, actor, "Актор побеждён — HP = 0");
            }
        }
    }

    private static SimulationResult BuildResult(bool success, EngineState state, ActorState actor, string? error) =>
        new()
        {
            Success = success,
            StepsCount = state.CommandSteps,
            PointsHistory = state.History.Select(p => new[] { p.X, p.Y }).ToList(),
            Error = error,
            Lines = new LineFlags(state.LineYellow, state.LineOrange, state.LineGreen),
            FinalActor = new ActorDto(actor.X, actor.Y, actor.Angle, state.Sitting),
            FinalEnemy = state.Enemy is null
                ? null
                : new ActorDto(state.Enemy.X, state.Enemy.Y, state.Enemy.Angle, false),
            ActorHp = state.LevelId == 3 ? state.ActorHp : null,
            MaxActorHp = state.LevelId == 3 ? 3 : null,
        };

    private static StepResult ExecuteCommand(string cmd, ActorState actor, EngineState state, (int X, int Y) chairOwn)
    {
        return cmd switch
        {
            "встать" => StandUp(actor, state),
            "идти" => StepForward(actor, state, diagonal: false) ? StepResult.Wait : StepResult.Error,
            "идти_диагональ" => StepDiagonal(actor, state),
            "повернуть_90" => Turn(actor, state, 90),
            "повернуть_-90" => Turn(actor, state, -90),
            "повернуть_180" => Turn(actor, state, 180),
            "сесть" => SitDown(actor, state, chairOwn),
            _ => Error(state),
        };
    }

    private static StepResult StepDiagonal(ActorState actor, EngineState state)
    {
        if (!state.DiagonalSkillEnabled) return Error(state);
        return StepForward(actor, state, diagonal: true) ? StepResult.Wait : StepResult.Error;
    }

    private static StepResult StandUp(ActorState actor, EngineState state)
    {
        if (!state.Sitting) return Error(state);
        state.Sitting = false;
        state.StoodUp = true;
        return StepForward(actor, state, diagonal: false) ? StepResult.Wait : StepResult.Error;
    }

    private static StepResult SitDown(ActorState actor, EngineState state, (int X, int Y) chairOwn)
    {
        if (actor.X != chairOwn.X || actor.Y != chairOwn.Y) return Error(state);
        state.Sitting = true;
        state.CommandSteps++;
        return StepResult.Wait;
    }

    private static StepResult Turn(ActorState actor, EngineState state, int degrees)
    {
        actor.Angle = NormAngle(actor.Angle + degrees);
        state.CommandSteps++;
        return StepResult.Wait;
    }

    private static bool StepForward(ActorState actor, EngineState state, bool diagonal)
    {
        var (dx, dy) = diagonal ? DiagonalDelta(actor.Angle) : Delta(actor.Angle);
        var nx = actor.X + dx;
        var ny = actor.Y + dy;
        if (nx < 0 || nx >= state.Width || ny < 0 || ny >= state.Height)
        {
            state.HasError = true;
            return false;
        }
        actor.X = nx;
        actor.Y = ny;
        state.Path.Add((nx, ny));
        state.History.Add((nx, ny));
        MarkTour(state, nx, ny);
        state.CommandSteps++;
        return true;
    }

    private static void MoveEnemy(EngineState state, ActorState player)
    {
        var enemy = state.Enemy!;
        var prev = (enemy.X, enemy.Y);
        var best = prev;
        var bestDist = Manhattan(enemy, player);

        foreach (var (nx, ny) in Neighbors4(enemy.X, enemy.Y))
        {
            if (nx < 0 || nx >= state.Width || ny < 0 || ny >= state.Height) continue;
            var dist = Math.Abs(nx - player.X) + Math.Abs(ny - player.Y);
            if (dist < bestDist || (dist == bestDist && CompareCell((nx, ny), best) < 0))
            {
                bestDist = dist;
                best = (nx, ny);
            }
        }

        enemy.X = best.X;
        enemy.Y = best.Y;

        if (enemy.X == player.X && enemy.Y == player.Y && (prev.X != enemy.X || prev.Y != enemy.Y))
            state.ActorHp--;
    }

    private static IEnumerable<(int X, int Y)> Neighbors4(int x, int y)
    {
        yield return (x + 1, y);
        yield return (x - 1, y);
        yield return (x, y + 1);
        yield return (x, y - 1);
    }

    private static int Manhattan(ActorState a, ActorState b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static int CompareCell((int X, int Y) a, (int X, int Y) b) =>
        a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);

    private static void MarkTour(EngineState state, int x, int y)
    {
        var (px, py) = state.ChairPartner;
        if (x == px && y == py + 1) state.LineYellow = true;
        if (x == px && y == py - 1) state.LineOrange = true;
        if (x == px + 1 && y == py) state.LineGreen = true;
    }

    private static StepResult Error(EngineState state)
    {
        state.HasError = true;
        return StepResult.Error;
    }

    public static int AngleStandFromChair(int w, int h, int ax, int ay, int px, int py)
    {
        foreach (var ang in new[] { 0, 90, 180, 270 })
        {
            var (dx, dy) = Delta(ang);
            var tx = ax + dx;
            var ty = ay + dy;
            if (tx >= 0 && tx < w && ty >= 0 && ty < h
                && !(tx == ax && ty == ay) && !(tx == px && ty == py))
                return ang;
        }
        return 0;
    }

    private static (int Dx, int Dy) Delta(int angle) =>
        NormAngle(angle) switch
        {
            0 => (1, 0),
            90 => (0, 1),
            180 => (-1, 0),
            270 => (0, -1),
            _ => (0, 0),
        };

    private static (int Dx, int Dy) DiagonalDelta(int angle)
    {
        var (fx, fy) = Delta(angle);
        var (px, py) = Delta(NormAngle(angle + 90));
        return (fx + px, fy + py);
    }

    private static int NormAngle(int a) => ((a % 360) + 360) % 360;

    private sealed class ActorState(int x, int y, int angle)
    {
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
        public int Angle { get; set; } = angle;
    }

    private sealed class EngineState(int width, int height, (int X, int Y) chairOwn, (int X, int Y) chairPartner, bool sitting)
    {
        public int Width { get; } = width;
        public int Height { get; } = height;
        public (int X, int Y) ChairOwn { get; } = chairOwn;
        public (int X, int Y) ChairPartner { get; } = chairPartner;
        public bool Sitting { get; set; } = sitting;
        public bool StoodUp { get; set; }
        public bool LineYellow { get; set; }
        public bool LineOrange { get; set; }
        public bool LineGreen { get; set; }
        public bool HasError { get; set; }
        public int CommandSteps { get; set; }
        public bool DiagonalSkillEnabled { get; init; }
        public int LevelId { get; init; }
        public int ActorHp { get; set; } = 3;
        public ActorState? Enemy { get; set; }
        public HashSet<(int X, int Y)> Path { get; } = [];
        public List<(int X, int Y)> History { get; } = [];
    }
}

public sealed class SimulationInput
{
    public int GridWidth { get; init; } = 14;
    public int GridHeight { get; init; } = 8;
    public int ChairOwnX { get; init; }
    public int ChairOwnY { get; init; }
    public int ChairPartnerX { get; init; }
    public int ChairPartnerY { get; init; }
    public int SpawnX { get; init; }
    public int SpawnY { get; init; }
    public int SpawnAngle { get; init; }
    public bool SittingAtStart { get; init; }
    public int LevelId { get; init; }
    public bool DiagonalSkillEnabled { get; init; }
    public int? EnemySpawnX { get; init; }
    public int? EnemySpawnY { get; init; }
    public int ActorHp { get; init; } = 3;
    public List<string>? Commands { get; init; }
}

public sealed class SimulationResult
{
    public bool Success { get; init; }
    public int StepsCount { get; init; }
    public List<int[]> PointsHistory { get; init; } = [];
    public string? Error { get; init; }
    public LineFlags? Lines { get; init; }
    public ActorDto? FinalActor { get; init; }
    public ActorDto? FinalEnemy { get; init; }
    public int? ActorHp { get; init; }
    public int? MaxActorHp { get; init; }
}

public sealed record LineFlags(bool Yellow, bool Orange, bool Green);
public sealed record ActorDto(int X, int Y, int Angle, bool Sitting);
public sealed record SceneDto(
    int GridWidth,
    int GridHeight,
    int[] ChairOwn,
    int[] ChairPartner,
    ActorDto Actor,
    string Description,
    ActorDto? Enemy = null,
    int? ActorHp = null,
    int? MaxActorHp = null);
