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
        var rng = new Random(request.Seed ?? Environment.TickCount);

        return levelId switch
        {
            1 => BuildLevel1(request),
            2 => BuildLevel2(request, rng),
            3 => BuildLevel3(request, rng),
            4 => BuildLevel4(request, rng, hasDiagonalSkill),
            5 => BuildLevel5(request, rng, hasDiagonalSkill),
            _ => ScenarioBuildResult.Failure("Неизвестный уровень"),
        };
    }

    /// <summary>Уровень 1: фиксированная карта и стулья, актор сидит на своём.</summary>
    private static ScenarioBuildResult BuildLevel1(TryLevelRequest request)
    {
        const int w = 14;
        const int h = 8;
        var own = Coords(request.ChairOwn, DefaultChairOwn);
        var partner = Coords(request.ChairPartner, DefaultChairPartner);
        if (!ValidateChairs(w, h, own, partner, out var err))
            return ScenarioBuildResult.Failure(err!);

        return BuildSittingOnOwn(w, h, own, partner, request.Commands, levelId: 1,
            "Уровень 1: встать, обойти чужой стул, сесть на свой.");
    }

    /// <summary>Уровень 2: + случайные стулья.</summary>
    private static ScenarioBuildResult BuildLevel2(TryLevelRequest request, Random rng)
    {
        const int w = 14;
        const int h = 8;
        var (own, partner) = RandomChairs(w, h, rng);
        if (!ValidateChairs(w, h, own, partner, out var err))
            return ScenarioBuildResult.Failure(err!);

        return BuildSittingOnOwn(w, h, own, partner, request.Commands, levelId: 2,
            $"Уровень 2: случайные стулья — свой ({own[0]},{own[1]}), чужой ({partner[0]},{partner[1]}).");
    }

    /// <summary>Уровень 3: + случайный размер карты.</summary>
    private static ScenarioBuildResult BuildLevel3(TryLevelRequest request, Random rng)
    {
        var (w, h) = ResolveGrid(request, rng, randomize: true);
        var (own, partner) = RandomChairs(w, h, rng);
        if (!ValidateChairs(w, h, own, partner, out var err))
            return ScenarioBuildResult.Failure(err!);

        return BuildSittingOnOwn(w, h, own, partner, request.Commands, levelId: 3,
            $"Уровень 3: карта {w}×{h}, случайные стулья. За успех — скилл «{DiagonalCommand}».");
    }

    /// <summary>Уровень 4: + случайный спавн актора.</summary>
    private static ScenarioBuildResult BuildLevel4(TryLevelRequest request, Random rng, bool hasDiagonalSkill)
    {
        var (w, h) = ResolveGrid(request, rng, randomize: true);
        var (own, partner) = RandomChairs(w, h, rng);
        if (!ValidateChairs(w, h, own, partner, out var err))
            return ScenarioBuildResult.Failure(err!);

        var occupied = new HashSet<(int, int)> { (own[0], own[1]), (partner[0], partner[1]) };
        var spawn = RandomFreeCell(w, h, occupied, rng);
        var angle = Angles[rng.Next(Angles.Length)];

        var scene = new SceneDto(w, h, own, partner,
            new ActorDto(spawn.Item1, spawn.Item2, angle, false),
            hasDiagonalSkill
                ? $"Уровень 4: карта {w}×{h}, случайный спавн ({spawn.Item1},{spawn.Item2}), скилл {DiagonalCommand}."
                : $"Уровень 4: карта {w}×{h}, случайный спавн ({spawn.Item1},{spawn.Item2}), угол {angle}°.");

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
            LevelId = 4,
            DiagonalSkillEnabled = hasDiagonalSkill,
            Commands = request.Commands,
        });
    }

    /// <summary>Уровень 5: + враг, у актора 3 HP.</summary>
    private static ScenarioBuildResult BuildLevel5(TryLevelRequest request, Random rng, bool hasDiagonalSkill)
    {
        for (var attempt = 0; attempt < 400; attempt++)
        {
            var (w, h) = ResolveGrid(request, rng, randomize: true);
            var cells = AllCells(w, h);
            Shuffle(cells, rng);
            if (cells.Count < 4)
                return ScenarioBuildResult.Failure("Поле слишком мало для уровня с врагом");

            var spawn = cells[0];
            var c1 = cells[1];
            var c2 = cells[2];
            var enemySpawn = cells[3];
            var (own, partner) = NearestChair(spawn, c1, c2);
            var angle = Angles[rng.Next(Angles.Length)];

            var scene = new SceneDto(w, h, [own.X, own.Y], [partner.X, partner.Y],
                new ActorDto(spawn.X, spawn.Y, angle, false),
                $"Уровень 5: карта {w}×{h}, враг ({enemySpawn.X},{enemySpawn.Y}). У вас 3 HP — враг преследует; зашёл на вашу клетку → −1 HP.",
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
                LevelId = 5,
                DiagonalSkillEnabled = hasDiagonalSkill,
                EnemySpawnX = enemySpawn.X,
                EnemySpawnY = enemySpawn.Y,
                ActorHp = 3,
                Commands = request.Commands,
            });
        }

        return ScenarioBuildResult.Failure("Не удалось сгенерировать сцену — смените seed");
    }

    private static ScenarioBuildResult BuildSittingOnOwn(
        int w, int h, int[] own, int[] partner, List<string>? commands, int levelId, string description)
    {
        var angle = GameEngine.AngleStandFromChair(w, h, own[0], own[1], partner[0], partner[1]);
        var scene = new SceneDto(w, h, own, partner,
            new ActorDto(own[0], own[1], angle, true), description);

        return ScenarioBuildResult.Success(scene, new SimulationInput
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
            LevelId = levelId,
            DiagonalSkillEnabled = false,
            Commands = commands,
        });
    }

    private static (int W, int H) ResolveGrid(TryLevelRequest request, Random rng, bool randomize)
    {
        if (randomize)
            return (rng.Next(12, 17), rng.Next(6, 11));

        if (request.GridWidth is > 0 and var gw && request.GridHeight is > 0 and var gh)
            return (gw, gh);

        return (14, 8);
    }

    private static int[] Coords(int[]? fromRequest, int[] defaults) =>
        fromRequest is { Length: 2 } ? fromRequest : defaults;

    private static (int[] Own, int[] Partner) RandomChairs(int w, int h, Random rng)
    {
        var free = AllCells(w, h);
        Shuffle(free, rng);
        var a = free[0];
        var b = free[1];
        return ([a.X, a.Y], [b.X, b.Y]);
    }

    private static List<(int X, int Y)> AllCells(int w, int h)
    {
        var cells = new List<(int X, int Y)>(w * h);
        for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
                cells.Add((x, y));
        return cells;
    }

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
    public int? GridWidth { get; set; }
    public int? GridHeight { get; set; }
    public int[]? ChairOwn { get; set; }
    public int[]? ChairPartner { get; set; }
    public int? Seed { get; set; }
    public List<string>? Commands { get; set; }
}
