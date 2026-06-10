namespace UpGoDown.Api.Services;

public sealed class LevelScenarioService
{
    private static readonly int[] DefaultChairOwn = [2, 4];
    private static readonly int[] DefaultChairPartner = [9, 4];
    private static readonly int[] Angles = [0, 90, 180, 270];

    public static readonly string[] BaseCommands =
        ["встать", "идти", "повернуть_90", "повернуть_-90", "повернуть_180", "сесть"];

    public static readonly string DiagonalCommand = "идти_диагональ";

    public static IReadOnlyList<string> AllowedCommands(bool hasDiagonalSkill)
    {
        if (!hasDiagonalSkill) return BaseCommands;
        return [..BaseCommands.Take(2), DiagonalCommand, ..BaseCommands.Skip(2)];
    }

    public ScenarioBuildResult Build(int levelId, TryLevelRequest request, bool hasDiagonalSkill)
    {
        var w = request.GridWidth is > 0 ? request.GridWidth : 14;
        var h = request.GridHeight is > 0 ? request.GridHeight : 8;
        var rng = new Random(request.Seed ?? Environment.TickCount);

        return levelId switch
        {
            1 => BuildLevel1(w, h, request),
            2 => BuildLevel2(w, h, request, rng, hasDiagonalSkill),
            3 => BuildLevel3(w, h, request, rng, hasDiagonalSkill),
            _ => ScenarioBuildResult.Failure("Неизвестный уровень"),
        };
    }

    private static ScenarioBuildResult BuildLevel1(int w, int h, TryLevelRequest request)
    {
        var own = Coords(request.ChairOwn, DefaultChairOwn);
        var partner = Coords(request.ChairPartner, DefaultChairPartner);
        if (!ValidateChairs(w, h, own, partner, out var err))
            return ScenarioBuildResult.Failure(err!);

        var angle = GameEngine.AngleStandFromChair(w, h, own[0], own[1], partner[0], partner[1]);
        var scene = new SceneDto(w, h, own, partner,
            new ActorDto(own[0], own[1], angle, true),
            "Уровень 1: актор на своём стуле. За успех — скилл «ход по диагонали».");

        var input = new SimulationInput
        {
            GridWidth = w,
            GridHeight = h,
            ChairOwnX = own[0],
            ChairOwnY = own[1],
            ChairPartnerX = partner[0],
            ChairPartnerY = partner[1],
            SpawnX = own[0],
            SpawnY = own[1],
            SpawnAngle = angle,
            SittingAtStart = true,
            LevelId = 1,
            DiagonalSkillEnabled = false,
            Commands = request.Commands,
        };
        return ScenarioBuildResult.Success(scene, input);
    }

    private static ScenarioBuildResult BuildLevel2(int w, int h, TryLevelRequest request, Random rng, bool hasDiagonalSkill)
    {
        var own = Coords(request.ChairOwn, DefaultChairOwn);
        var partner = Coords(request.ChairPartner, DefaultChairPartner);
        if (!ValidateChairs(w, h, own, partner, out var err))
            return ScenarioBuildResult.Failure(err!);

        var occupied = new HashSet<(int, int)> { (own[0], own[1]), (partner[0], partner[1]) };
        var spawn = RandomFreeCell(w, h, occupied, rng);
        var angle = Angles[rng.Next(Angles.Length)];

        var scene = new SceneDto(w, h, own, partner,
            new ActorDto(spawn.Item1, spawn.Item2, angle, false),
            hasDiagonalSkill
                ? $"Уровень 2: случайный спавн ({spawn.Item1}, {spawn.Item2}). Доступен скилл: {DiagonalCommand}."
                : $"Уровень 2: случайный спавн ({spawn.Item1}, {spawn.Item2}), угол {angle}°");

        return ScenarioBuildResult.Success(scene, new SimulationInput
        {
            GridWidth = w,
            GridHeight = h,
            ChairOwnX = own[0],
            ChairOwnY = own[1],
            ChairPartnerX = partner[0],
            ChairPartnerY = partner[1],
            SpawnX = spawn.Item1,
            SpawnY = spawn.Item2,
            SpawnAngle = angle,
            SittingAtStart = false,
            LevelId = 2,
            DiagonalSkillEnabled = hasDiagonalSkill,
            Commands = request.Commands,
        });
    }

    private static ScenarioBuildResult BuildLevel3(int w, int h, TryLevelRequest request, Random rng, bool hasDiagonalSkill)
    {
        for (var attempt = 0; attempt < 400; attempt++)
        {
            var cells = new List<(int X, int Y)>();
            for (var x = 0; x < w; x++)
                for (var y = 0; y < h; y++)
                    cells.Add((x, y));
            Shuffle(cells, rng);
            if (cells.Count < 4) return ScenarioBuildResult.Failure("Поле слишком мало");

            var spawn = cells[0];
            var c1 = cells[1];
            var c2 = cells[2];
            var enemySpawn = cells[3];
            var (own, partner) = NearestChair(spawn, c1, c2);
            var angle = Angles[rng.Next(Angles.Length)];

            var scene = new SceneDto(w, h, [own.X, own.Y], [partner.X, partner.Y],
                new ActorDto(spawn.X, spawn.Y, angle, false),
                $"Уровень 3: актор ({spawn.X},{spawn.Y}), враг ({enemySpawn.X},{enemySpawn.Y}), HP=3. Враг идёт к вам; совпадение клетки −1 HP.",
                new ActorDto(enemySpawn.X, enemySpawn.Y, 0, false),
                ActorHp: 3,
                MaxActorHp: 3);

            return ScenarioBuildResult.Success(scene, new SimulationInput
            {
                GridWidth = w,
                GridHeight = h,
                ChairOwnX = own.X,
                ChairOwnY = own.Y,
                ChairPartnerX = partner.X,
                ChairPartnerY = partner.Y,
                SpawnX = spawn.X,
                SpawnY = spawn.Y,
                SpawnAngle = angle,
                SittingAtStart = false,
                LevelId = 3,
                DiagonalSkillEnabled = hasDiagonalSkill,
                EnemySpawnX = enemySpawn.X,
                EnemySpawnY = enemySpawn.Y,
                ActorHp = 3,
                Commands = request.Commands,
            });
        }
        return ScenarioBuildResult.Failure("Не удалось сгенерировать сцену — смените seed или увеличьте поле");
    }

    private static int[] Coords(int[]? fromRequest, int[] defaults) =>
        fromRequest is { Length: 2 } ? fromRequest : defaults;

    private static bool ValidateChairs(int w, int h, int[] own, int[] partner, out string? error)
    {
        if (own[0] < 0 || own[0] >= w || own[1] < 0 || own[1] >= h
            || partner[0] < 0 || partner[0] >= w || partner[1] < 0 || partner[1] >= h)
        {
            error = "Координаты стульев вне поля";
            return false;
        }
        if (own[0] == partner[0] && own[1] == partner[1])
        {
            error = "Стулы не должны совпадать";
            return false;
        }
        error = null;
        return true;
    }

    private static (int X, int Y) RandomFreeCell(int w, int h, HashSet<(int, int)> occupied, Random rng)
    {
        var free = new List<(int, int)>();
        for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
                if (!occupied.Contains((x, y)))
                    free.Add((x, y));
        return free[rng.Next(free.Count)];
    }

    private static ((int X, int Y) Own, (int X, int Y) Partner) NearestChair(
        (int X, int Y) pos, (int X, int Y) a, (int X, int Y) b)
    {
        int Dist((int X, int Y) c) => Math.Abs(c.X - pos.X) + Math.Abs(c.Y - pos.Y);
        var da = Dist(a);
        var db = Dist(b);
        if (da < db) return (a, b);
        if (db < da) return (b, a);
        return Compare(a, b) <= 0 ? (a, b) : (b, a);
    }

    private static int Compare((int X, int Y) a, (int X, int Y) b) =>
        a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public sealed class ScenarioBuildResult
{
    public bool Ok { get; init; }
    public string? Error { get; init; }
    public SceneDto? Scene { get; init; }
    public SimulationInput? Input { get; init; }

    public static ScenarioBuildResult Success(SceneDto scene, SimulationInput input) =>
        new() { Ok = true, Scene = scene, Input = input };

    public static ScenarioBuildResult Failure(string error) =>
        new() { Ok = false, Error = error };
}

public sealed class TryLevelRequest
{
    public int GridWidth { get; set; } = 14;
    public int GridHeight { get; set; } = 8;
    public int[]? ChairOwn { get; set; }
    public int[]? ChairPartner { get; set; }
    public int? Seed { get; set; }
    public List<string>? Commands { get; set; }
}
